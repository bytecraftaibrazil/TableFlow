using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TableFlow.Api.Interfaces;
using Moq;
using TableFlow.Api.Data;
using TableFlow.Api.Entities;
using TableFlow.Api.Models;
using TableFlow.Api.Services;

namespace TableFlow.Tests.Unit;

[Trait("Category", "Unit")]
public class TableServiceTests
{
    private static TableService Service(MockDb db, Mock<IReservationWriteSession>? session = null)
    {
        if (session is null)
        {
            session = new Mock<IReservationWriteSession>(MockBehavior.Strict);
            var transaction = new Mock<IDbContextTransaction>(MockBehavior.Strict);
            transaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            transaction.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);
            session.Setup(s => s.BeginAsync()).ReturnsAsync(transaction.Object);
            session.Setup(s => s.LockTablesAsync(It.IsAny<int[]>())).Returns(Task.CompletedTask);
        }
        return new TableService(db.Context.Object, session.Object);
    }

    [Fact]
    public async Task CreateMissingRestaurantRejectsWithoutSaving()
    {
        using var db = new MockDb();
        var result = await Service(db).CreateAsync(new(99, 1, 4, true));
        Assert.Equal(TableOperationStatus.RestaurantNotFound, result.Status);
        Assert.Null(result.Table);
        Assert.Empty(db.Tables);
        db.VerifyNotSaved();
    }

    [Fact]
    public async Task CreateDuplicateNumberInSameRestaurantRejectsWithoutSaving()
    {
        using var db = new MockDb();
        db.Restaurants.Add(new Restaurant { Id = 1 });
        db.Tables.Add(new RestaurantTable { Id = 10, RestaurantId = 1, Number = 2 });
        var result = await Service(db).CreateAsync(new(1, 2, 4, true));
        Assert.Equal(TableOperationStatus.DuplicateNumber, result.Status);
        Assert.Single(db.Tables);
        db.VerifyNotSaved();
    }

    [Fact]
    public async Task CreateAllowsSameNumberInDifferentRestaurant()
    {
        using var db = new MockDb();
        db.Restaurants.Add(new Restaurant { Id = 1 });
        db.Tables.Add(new RestaurantTable { Id = 10, RestaurantId = 2, Number = 3 });
        var result = await Service(db).CreateAsync(new(1, 3, 4, true));
        Assert.Equal(TableOperationStatus.Success, result.Status);
        Assert.Equal(1, result.Table!.RestaurantId);
        Assert.Equal(3, result.Table.Number);
        Assert.Equal(4, result.Table.Capacity);
        Assert.True(result.Table.IsActive);
        Assert.Equal(2, db.Tables.Count);
        db.VerifySavedOnce();
    }

    [Fact]
    public async Task UpdateMissingTableDoesNotSave()
    {
        using var db = new MockDb();
        var result = await Service(db).UpdateAsync(99, new(1, 2, 4, true));
        Assert.Equal(TableOperationStatus.TableNotFound, result.Status);
        db.VerifyNotSaved();
    }

    [Fact]
    public async Task UpdateMissingRestaurantDoesNotChangeTable()
    {
        using var db = new MockDb();
        var table = new RestaurantTable { Id = 10, RestaurantId = 1, Number = 2, Capacity = 4 };
        db.Tables.Add(table);
        var result = await Service(db).UpdateAsync(10, new(99, 3, 6, true));
        Assert.Equal(TableOperationStatus.RestaurantNotFound, result.Status);
        Assert.Equal(1, table.RestaurantId);
        Assert.Equal(2, table.Number);
        db.VerifyNotSaved();
    }

    [Fact]
    public async Task UpdateDuplicateNumberDoesNotChangeTable()
    {
        using var db = new MockDb();
        db.Restaurants.Add(new Restaurant { Id = 1 });
        var table = new RestaurantTable { Id = 10, RestaurantId = 1, Number = 1 };
        db.Tables.AddRange([table, new RestaurantTable { Id = 11, RestaurantId = 1, Number = 2 }]);
        var result = await Service(db).UpdateAsync(10, new(1, 2, 6, true));
        Assert.Equal(TableOperationStatus.DuplicateNumber, result.Status);
        Assert.Equal(1, table.Number);
        db.VerifyNotSaved();
    }

    [Fact]
    public async Task UpdateExcludesOwnNumberAndSavesNewValues()
    {
        using var db = new MockDb();
        db.Restaurants.Add(new Restaurant { Id = 1 });
        var table = new RestaurantTable { Id = 10, RestaurantId = 1, Number = 2, Capacity = 4, IsActive = true };
        db.Tables.Add(table);
        var result = await Service(db).UpdateAsync(10, new(1, 2, 6, false));
        Assert.Equal(TableOperationStatus.Success, result.Status);
        Assert.Equal(10, result.Table!.Id);
        Assert.Equal(6, table.Capacity);
        Assert.False(table.IsActive);
        db.VerifySavedOnce();
    }

    [Fact]
    public async Task DeleteMissingTableDoesNotSave()
    {
        using var db = new MockDb();
        Assert.False(await Service(db).DeleteAsync(99));
        db.VerifyNotSaved();
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Confirmed")]
    [InlineData("Cancelled")]
    public async Task TransferWithAnyLinkedReservationRejectsWithoutChangingTable(string status)
    {
        using var db = new MockDb();
        db.Restaurants.Add(new Restaurant { Id = 2 });
        var table = new RestaurantTable { Id = 10, RestaurantId = 1, Number = 1, Capacity = 4, IsActive = true };
        db.Tables.Add(table);
        db.Reservations.Add(new Reservation { Id = 100, RestaurantId = 1, TableId = 10, Status = status });

        var result = await Service(db).UpdateAsync(10, new(2, 3, 6, false));

        Assert.Equal(TableOperationStatus.TableReassignmentConflict, result.Status);
        Assert.Null(result.Table);
        Assert.Equal(1, table.RestaurantId);
        Assert.Equal(1, table.Number);
        Assert.Equal(4, table.Capacity);
        Assert.True(table.IsActive);
        Assert.Equal(1, db.Reservations[0].RestaurantId);
        db.VerifyNotSaved();
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Confirmed")]
    [InlineData("Cancelled")]
    public async Task ExistingReservationsStillAllowNumberCapacityAndActivityEdits(string status)
    {
        using var db = new MockDb();
        db.Restaurants.Add(new Restaurant { Id = 1 });
        var table = new RestaurantTable { Id = 10, RestaurantId = 1, Number = 1, Capacity = 4, IsActive = true };
        db.Tables.Add(table);
        db.Reservations.Add(new Reservation { RestaurantId = 1, TableId = 10, Status = status });

        var result = await Service(db).UpdateAsync(10, new(1, 3, 6, false));

        Assert.Equal(TableOperationStatus.Success, result.Status);
        Assert.Equal(1, table.RestaurantId);
        Assert.Equal(3, table.Number);
        Assert.Equal(6, table.Capacity);
        Assert.False(table.IsActive);
        db.VerifySavedOnce();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TransferWithoutLinkedReservationsIsAllowed(bool otherTableHasReservation)
    {
        using var db = new MockDb();
        db.Restaurants.Add(new Restaurant { Id = 2 });
        var table = new RestaurantTable { Id = 10, RestaurantId = 1, Number = 1, Capacity = 4, IsActive = true };
        db.Tables.Add(table);
        if (otherTableHasReservation) db.Reservations.Add(new Reservation { TableId = 99, Status = "Pending" });

        var result = await Service(db).UpdateAsync(10, new(2, 1, 4, true));

        Assert.Equal(TableOperationStatus.Success, result.Status);
        Assert.Equal(2, table.RestaurantId);
        Assert.Equal(2, result.Table!.RestaurantId);
        db.VerifySavedOnce();
    }

    [Fact]
    public async Task TransferChecksReservationsAfterAcquiringSharedTableLockAndDoesNotCommitConflict()
    {
        using var db = new MockDb();
        db.Restaurants.Add(new Restaurant { Id = 2 });
        db.Tables.Add(new RestaurantTable { Id = 10, RestaurantId = 1, Number = 1, Capacity = 4 });
        var transaction = new Mock<IDbContextTransaction>(MockBehavior.Strict);
        transaction.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var session = new Mock<IReservationWriteSession>(MockBehavior.Strict);
        var sequence = new MockSequence();
        session.InSequence(sequence).Setup(s => s.BeginAsync()).ReturnsAsync(transaction.Object);
        session.InSequence(sequence).Setup(s => s.LockTablesAsync(It.Is<int[]>(ids => ids.SequenceEqual(new[] { 10 }))))
            .Callback(() => db.Reservations.Add(new Reservation { RestaurantId = 1, TableId = 10, Status = "Pending" }))
            .Returns(Task.CompletedTask);

        var result = await Service(db, session).UpdateAsync(10, new(2, 1, 4, true));

        Assert.Equal(TableOperationStatus.TableReassignmentConflict, result.Status);
        transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        transaction.Verify(t => t.DisposeAsync(), Times.Once);
        Assert.Equal(1, db.Tables[0].RestaurantId);
        db.VerifyNotSaved();
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Confirmed")]
    [InlineData("Cancelled")]
    public async Task DeleteWithReservationRejectsRegardlessOfStatus(string status)
    {
        using var db = new MockDb();
        db.Tables.Add(new RestaurantTable { Id = 10 });
        db.Reservations.Add(new Reservation { TableId = 10, Status = status });
        await Assert.ThrowsAsync<ResourceConflictException>(() => Service(db).DeleteAsync(10));
        db.TableSet.Verify(set => set.Remove(It.IsAny<RestaurantTable>()), Times.Never);
        db.VerifyNotSaved();
    }

    [Fact]
    public async Task DeleteWithoutDependenciesRemovesAndSaves()
    {
        using var db = new MockDb();
        var table = new RestaurantTable { Id = 10 };
        db.Tables.Add(table);
        Assert.True(await Service(db).DeleteAsync(10));
        db.TableSet.Verify(set => set.Remove(table), Times.Once);
        db.VerifySavedOnce();
    }

    [Theory]
    [InlineData("Pending", 60, 90, 6, 4, false)]
    [InlineData("Confirmed", 60, 90, 6, 4, false)]
    [InlineData("Pending", -30, 90, 6, 4, false)]
    [InlineData("Confirmed", -30, 90, 6, 4, false)]
    [InlineData("Cancelled", 60, 90, 6, 4, true)]
    [InlineData("Cancelled", -30, 90, 6, 4, true)]
    [InlineData("Pending", -180, 90, 6, 4, true)]
    [InlineData("Confirmed", -180, 90, 6, 4, true)]
    [InlineData("Pending", -90, 90, 6, 4, true)]
    [InlineData("Pending", 60, 90, 4, 4, true)]
    [InlineData("Confirmed", -30, 90, 4, 4, true)]
    [InlineData("Pending", 60, 90, 6, 10, true)]
    [InlineData("Confirmed", -30, 90, 6, 10, true)]
    [InlineData("Confirmed", 60, 90, 6, 8, true)]
    public async Task CapacityUpdateChecksOnlyUnendedActiveReservations(
        string status, int startOffset, int duration, int partySize, int capacity, bool allowed)
    {
        using var db = new MockDb();
        db.Restaurants.Add(new Restaurant { Id = 1 });
        var table = new RestaurantTable { Id = 10, RestaurantId = 1, Number = 1, Capacity = 8, IsActive = true };
        db.Tables.Add(table);
        db.Reservations.Add(new Reservation
        {
            TableId = 10,
            RestaurantId = 1,
            Status = status,
            PartySize = partySize,
            ReservationDate = DateTime.UtcNow.AddMinutes(startOffset),
            DurationMinutes = duration
        });
        var transaction = new Mock<IDbContextTransaction>(MockBehavior.Strict);
        transaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        transaction.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var session = new Mock<IReservationWriteSession>(MockBehavior.Strict);
        session.Setup(s => s.BeginAsync()).ReturnsAsync(transaction.Object);
        session.Setup(s => s.LockTablesAsync(It.Is<int[]>(ids => ids.SequenceEqual(new[] { 10 }))))
            .Returns(Task.CompletedTask);

        var result = await Service(db, session).UpdateAsync(10, new(1, 2, capacity, false));

        Assert.Equal(allowed ? TableOperationStatus.Success : TableOperationStatus.TableCapacityConflict, result.Status);
        Assert.Equal(allowed ? capacity : 8, table.Capacity);
        Assert.Equal(allowed ? 2 : 1, table.Number);
        Assert.Equal(!allowed, table.IsActive);
        Assert.Equal(1, table.RestaurantId);
        if (allowed) Assert.Equal(capacity, result.Table!.Capacity);
        else Assert.Null(result.Table);
        db.Context.Verify(context => context.SaveChangesAsync(It.IsAny<CancellationToken>()), allowed ? Times.Once : Times.Never);
        transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), allowed ? Times.Once : Times.Never);
        transaction.Verify(t => t.DisposeAsync(), Times.Once);
        session.Verify(s => s.LockTablesAsync(It.Is<int[]>(ids => ids.SequenceEqual(new[] { 10 }))), Times.Once);
    }

    [Fact]
    public async Task CapacityReductionWithoutReservationsOrWithOnlyOtherTableReservationsSucceeds()
    {
        using var db = new MockDb();
        db.Restaurants.Add(new Restaurant { Id = 1 });
        db.Tables.Add(new RestaurantTable { Id = 10, RestaurantId = 1, Number = 1, Capacity = 8 });
        var service = Service(db);
        Assert.Equal(TableOperationStatus.Success, (await service.UpdateAsync(10, new(1, 1, 6, true))).Status);
        db.Reservations.Add(new Reservation
        {
            TableId = 99,
            Status = "Confirmed",
            PartySize = 8,
            ReservationDate = DateTime.UtcNow.AddDays(1),
            DurationMinutes = 90
        });
        Assert.Equal(TableOperationStatus.Success, (await service.UpdateAsync(10, new(1, 1, 4, true))).Status);
        Assert.Equal(4, db.Tables[0].Capacity);
    }

    [Fact]
    public async Task CapacityReductionChecksEveryRelevantReservationAfterTheTableLock()
    {
        using var db = new MockDb();
        db.Restaurants.Add(new Restaurant { Id = 1 });
        db.Tables.Add(new RestaurantTable { Id = 10, RestaurantId = 1, Number = 1, Capacity = 8 });
        db.Reservations.Add(new Reservation
        {
            TableId = 10,
            Status = "Pending",
            PartySize = 2,
            ReservationDate = DateTime.UtcNow.AddDays(1),
            DurationMinutes = 90
        });
        var transaction = new Mock<IDbContextTransaction>(MockBehavior.Strict);
        transaction.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var session = new Mock<IReservationWriteSession>(MockBehavior.Strict);
        session.Setup(s => s.BeginAsync()).ReturnsAsync(transaction.Object);
        session.Setup(s => s.LockTablesAsync(It.IsAny<int[]>())).Callback(() => db.Reservations.Add(new Reservation
        {
            TableId = 10,
            Status = "Confirmed",
            PartySize = 6,
            ReservationDate = DateTime.UtcNow.AddDays(2),
            DurationMinutes = 90
        })).Returns(Task.CompletedTask);

        Assert.Equal(TableOperationStatus.TableCapacityConflict,
            (await Service(db, session).UpdateAsync(10, new(1, 1, 4, true))).Status);
        Assert.Equal(8, db.Tables[0].Capacity);
        db.VerifyNotSaved();
        transaction.Verify(t => t.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task DeletePropagatesUnrelatedDatabaseFailure()
    {
        using var db = new MockDb();
        db.Tables.Add(new RestaurantTable { Id = 10 });
        var failure = new DbUpdateException("Unrelated database failure");
        db.Context.Setup(context => context.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        Assert.Same(failure, await Assert.ThrowsAsync<DbUpdateException>(() => Service(db).DeleteAsync(10)));
    }
}
