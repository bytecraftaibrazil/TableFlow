using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using TableFlow.Api.DTOs;
using TableFlow.Api.Entities;
using TableFlow.Api.Interfaces;
using TableFlow.Api.Models;
using TableFlow.Api.Services;

namespace TableFlow.Tests.Unit;

[Trait("Category", "Unit")]
public class ReservationServiceTests : IDisposable
{
    private readonly MockDb db = new();
    private readonly Mock<IReservationEngine> engine = new(MockBehavior.Strict);
    private readonly Mock<IReservationWriteSession> session = new(MockBehavior.Strict);
    private readonly Mock<IDbContextTransaction> transaction = new(MockBehavior.Strict);

    public ReservationServiceTests()
    {
        session.Setup(s => s.BeginAsync()).ReturnsAsync(transaction.Object);
        session.Setup(s => s.LockReservationAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        session.Setup(s => s.LockTablesAsync(It.IsAny<int[]>())).Returns(Task.CompletedTask);
        session.Setup(s => s.ReloadAsync(It.IsAny<Reservation>())).Returns(Task.CompletedTask);
        transaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        transaction.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);
    }

    private ReservationService Service() => new(db.Context.Object, engine.Object, session.Object);
    private static CreateReservationRequest Request() => new(1, 10, "  Ana Silva  ", DateTime.UtcNow.AddDays(10), 90, 4);
    private static UpdateReservationRequest UpdateRequest(CreateReservationRequest r)
        => new(r.RestaurantId, r.TableId, r.CustomerName, r.ReservationDate, r.DurationMinutes, r.PartySize);
    private Reservation Existing(string status = "Pending")
    {
        var reservation = new Reservation { Id = 7, RestaurantId = 1, TableId = 10, CustomerName = "Original", ReservationDate = DateTime.UtcNow.AddDays(5), DurationMinutes = 60, PartySize = 2, Status = status };
        db.Reservations.Add(reservation);
        return reservation;
    }

    private void Validation(ReservationValidationStatus status)
        => engine.Setup(e => e.ValidateAsync(It.IsAny<ReservationCandidate>(), It.IsAny<int?>()))
            .ReturnsAsync(new ReservationValidationResult(status));

    private void VerifyNotCommitted()
    {
        db.VerifyNotSaved();
        transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        transaction.Verify(t => t.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateValidatesAfterLockThenSavesPendingReservationAndCommits()
    {
        var request = Request();
        var order = new List<string>();
        session.Setup(s => s.BeginAsync()).Callback(() => order.Add("begin")).ReturnsAsync(transaction.Object);
        session.Setup(s => s.LockTablesAsync(It.Is<int[]>(ids => ids.SequenceEqual(new[] { 10 }))))
            .Callback(() => order.Add("lock")).Returns(Task.CompletedTask);
        engine.Setup(e => e.ValidateAsync(new ReservationCandidate(1, 10, 4, request.ReservationDate, 90), null))
            .Callback(() => order.Add("validate")).ReturnsAsync(new ReservationValidationResult(ReservationValidationStatus.Success));
        db.Context.Setup(context => context.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => order.Add("save")).ReturnsAsync(1);
        transaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Callback(() => order.Add("commit")).Returns(Task.CompletedTask);

        var result = await Service().CreateAsync(request);

        Assert.Equal(ReservationOperationStatus.Success, result.Status);
        var saved = Assert.Single(db.Reservations);
        Assert.Equal("Ana Silva", saved.CustomerName);
        Assert.Equal("Pending", saved.Status);
        Assert.Equal(request.ReservationDate, saved.ReservationDate);
        Assert.Equal(90, saved.DurationMinutes);
        Assert.Equal(4, saved.PartySize);
        Assert.Equal(1, result.Reservation!.RestaurantId);
        Assert.Equal(10, result.Reservation.TableId);
        Assert.Equal(new[] { "begin", "lock", "validate", "save", "commit" }, order);
        transaction.Verify(t => t.DisposeAsync(), Times.Once);
    }

    public static TheoryData<ReservationValidationStatus, ReservationOperationStatus> ValidationFailures => new()
    {
        { ReservationValidationStatus.ReservationInputInvalid, ReservationOperationStatus.ReservationInputInvalid },
        { ReservationValidationStatus.RestaurantNotFound, ReservationOperationStatus.RestaurantNotFound },
        { ReservationValidationStatus.RestaurantInactive, ReservationOperationStatus.RestaurantInactive },
        { ReservationValidationStatus.TableNotFound, ReservationOperationStatus.TableNotFound },
        { ReservationValidationStatus.TableDoesNotBelongToRestaurant, ReservationOperationStatus.TableDoesNotBelongToRestaurant },
        { ReservationValidationStatus.TableInactive, ReservationOperationStatus.TableInactive },
        { ReservationValidationStatus.PartySizeExceedsTableCapacity, ReservationOperationStatus.PartySizeExceedsTableCapacity },
        { ReservationValidationStatus.ReservationDurationInvalid, ReservationOperationStatus.ReservationDurationInvalid },
        { ReservationValidationStatus.ReservationConflict, ReservationOperationStatus.ReservationConflict }
    };

    [Theory]
    [MemberData(nameof(ValidationFailures))]
    public async Task CreateMapsValidationFailureWithoutAddingSavingOrCommitting(ReservationValidationStatus validation, ReservationOperationStatus expected)
    {
        Validation(validation);
        var result = await Service().CreateAsync(Request());
        Assert.Equal(expected, result.Status);
        Assert.Null(result.Reservation);
        Assert.Empty(db.Reservations);
        db.ReservationSet.Verify(set => set.AddAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNotCommitted();
    }

    [Theory]
    [MemberData(nameof(ValidationFailures))]
    public async Task UpdateMapsValidationFailureAndPreservesExistingValues(ReservationValidationStatus validation, ReservationOperationStatus expected)
    {
        var reservation = Existing();
        var originalDate = reservation.ReservationDate;
        Validation(validation);
        var result = await Service().UpdateAsync(7, UpdateRequest(Request()));
        Assert.Equal(expected, result.Status);
        Assert.Equal("Original", reservation.CustomerName);
        Assert.Equal(originalDate, reservation.ReservationDate);
        Assert.Equal(60, reservation.DurationMinutes);
        VerifyNotCommitted();
    }

    [Fact]
    public async Task UpdateLocksOldAndNewTablesAndExcludesOwnReservationFromValidation()
    {
        var reservation = Existing("Confirmed");
        var request = UpdateRequest(Request() with { RestaurantId = 2, TableId = 20 });
        Validation(ReservationValidationStatus.Success);

        var result = await Service().UpdateAsync(7, request);

        Assert.Equal(ReservationOperationStatus.Success, result.Status);
        Assert.Equal(2, reservation.RestaurantId);
        Assert.Equal(20, reservation.TableId);
        Assert.Equal("Ana Silva", reservation.CustomerName);
        Assert.Equal("Confirmed", reservation.Status);
        Assert.Equal(request.ReservationDate, reservation.ReservationDate);
        Assert.Equal(90, reservation.DurationMinutes);
        Assert.Equal(4, reservation.PartySize);
        session.Verify(s => s.LockReservationAsync(7), Times.Once);
        session.Verify(s => s.ReloadAsync(reservation), Times.Once);
        session.Verify(s => s.LockTablesAsync(It.Is<int[]>(ids => ids.SequenceEqual(new[] { 10, 20 }))), Times.Once);
        engine.Verify(e => e.ValidateAsync(new ReservationCandidate(2, 20, 4, request.ReservationDate, 90), 7), Times.Once);
        db.VerifySavedOnce();
        transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("update")]
    [InlineData("confirm")]
    [InlineData("cancel")]
    public async Task MissingReservationDoesNotValidateSaveOrCommit(string operation)
    {
        var service = Service();
        var result = operation switch
        {
            "update" => await service.UpdateAsync(99, UpdateRequest(Request())),
            "confirm" => await service.ConfirmAsync(99),
            _ => await service.CancelAsync(99)
        };
        Assert.Equal(ReservationOperationStatus.ReservationNotFound, result.Status);
        engine.VerifyNoOtherCalls();
        VerifyNotCommitted();
    }

    [Fact]
    public async Task UpdateReloadsStatusBeforeDecidingWhetherCancelledReservationCanChange()
    {
        var reservation = Existing();
        session.Setup(s => s.ReloadAsync(reservation)).Callback(() => reservation.Status = "Cancelled").Returns(Task.CompletedTask);
        var result = await Service().UpdateAsync(7, UpdateRequest(Request()));
        Assert.Equal(ReservationOperationStatus.CancelledReservationCannotBeUpdated, result.Status);
        Assert.Equal("Original", reservation.CustomerName);
        engine.VerifyNoOtherCalls();
        VerifyNotCommitted();
    }

    [Theory]
    [InlineData("Pending", "confirm", "Confirmed", true, ReservationOperationStatus.Success)]
    [InlineData("Confirmed", "confirm", "Confirmed", false, ReservationOperationStatus.Success)]
    [InlineData("Cancelled", "confirm", "Cancelled", false, ReservationOperationStatus.InvalidStatusTransition)]
    [InlineData("Pending", "cancel", "Cancelled", true, ReservationOperationStatus.Success)]
    [InlineData("Confirmed", "cancel", "Cancelled", true, ReservationOperationStatus.Success)]
    [InlineData("Cancelled", "cancel", "Cancelled", false, ReservationOperationStatus.Success)]
    public async Task StatusTransitionsAndIdempotentCallsRespectCurrentState(string initial, string operation, string final, bool saves, ReservationOperationStatus expected)
    {
        var reservation = Existing(initial);
        var result = operation == "confirm" ? await Service().ConfirmAsync(7) : await Service().CancelAsync(7);
        Assert.Equal(expected, result.Status);
        Assert.Equal(final, reservation.Status);
        if (expected == ReservationOperationStatus.Success) Assert.Equal(final, result.Reservation!.Status);
        else Assert.Null(result.Reservation);
        db.Context.Verify(context => context.SaveChangesAsync(It.IsAny<CancellationToken>()), saves ? Times.Once : Times.Never);
        transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), saves ? Times.Once : Times.Never);
        session.Verify(s => s.LockReservationAsync(7), Times.Once);
        session.Verify(s => s.ReloadAsync(reservation), Times.Once);
        session.Verify(s => s.LockTablesAsync(It.Is<int[]>(ids => ids.SequenceEqual(new[] { 10 }))), Times.Once);
        engine.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveFailureDoesNotCommitAndStillDisposesTransaction()
    {
        Validation(ReservationValidationStatus.Success);
        var failure = new DbUpdateException("Simulated write failure");
        db.Context.Setup(context => context.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        Assert.Same(failure, await Assert.ThrowsAsync<DbUpdateException>(() => Service().CreateAsync(Request())));
        transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        transaction.Verify(t => t.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task LockFailureDoesNotValidateOrSaveAndDisposesTransaction()
    {
        var failure = new TableFlow.Api.Data.ResourceConflictException("Simulated lock timeout");
        session.Setup(s => s.LockTablesAsync(It.IsAny<int[]>())).ThrowsAsync(failure);
        Assert.Same(failure, await Assert.ThrowsAsync<TableFlow.Api.Data.ResourceConflictException>(() => Service().CreateAsync(Request())));
        engine.VerifyNoOtherCalls();
        VerifyNotCommitted();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SuggestTableForwardsArgumentsAndEngineResult(bool found)
    {
        SuggestedTable? expected = found ? new(10, 1, 4) : null;
        engine.Setup(e => e.SuggestTableAsync(1, 4)).ReturnsAsync(expected);
        Assert.Same(expected, await Service().SuggestTableAsync(1, 4));
        engine.Verify(e => e.SuggestTableAsync(1, 4), Times.Once);
        session.VerifyNoOtherCalls();
        db.VerifyNotSaved();
    }

    [Fact]
    public async Task AvailabilityForwardsUnchangedRequestAndResult()
    {
        var request = new ReservationAvailabilityRequest(1, 4, Request().ReservationDate, 90);
        var expected = new ReservationAvailabilityResult(1, 4, request.ReservationDate, 90, [new(10, 1, 4)], []);
        engine.Setup(e => e.GetAvailabilityAsync(request)).ReturnsAsync(expected);
        Assert.Same(expected, await Service().GetAvailabilityAsync(request));
        engine.Verify(e => e.GetAvailabilityAsync(request), Times.Once);
        session.VerifyNoOtherCalls();
        db.VerifyNotSaved();
    }

    public void Dispose() => db.Dispose();
}
