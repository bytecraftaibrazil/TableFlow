using TableFlow.Api.Entities;
using TableFlow.Api.Models;
using TableFlow.Api.Services;

namespace TableFlow.Tests.Unit;

[Trait("Category", "Unit")]
public class ReservationEngineTests
{
    private static MockDb AvailableTable()
    {
        var db = new MockDb();
        var restaurant = new Restaurant { Id = 1, IsActive = true };
        db.Restaurants.Add(restaurant);
        db.Tables.Add(new RestaurantTable { Id = 10, RestaurantId = 1, Restaurant = restaurant, Capacity = 4, Number = 1, IsActive = true });
        return db;
    }

    private static ReservationCandidate Candidate() => new(1, 10, 4, DateTime.UtcNow.AddDays(10), 90);

    [Fact]
    public async Task ValidCandidateAtCapacitySucceeds()
    {
        using var db = AvailableTable();
        Assert.Equal(ReservationValidationStatus.Success, (await new ReservationEngine(db.Context.Object).ValidateAsync(Candidate())).Status);
        db.VerifyNotSaved();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task NonpositivePartySizeIsRejectedBeforeDatabaseAccess(int partySize)
    {
        using var db = new MockDb();
        Assert.Equal(ReservationValidationStatus.ReservationInputInvalid,
            (await new ReservationEngine(db.Context.Object).ValidateAsync(Candidate() with { PartySize = partySize })).Status);
        db.Context.Verify(context => context.Set<Restaurant>(), Moq.Times.Never);
    }

    [Theory]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local)]
    public async Task NonUtcDateIsRejectedBeforeDatabaseAccess(DateTimeKind kind)
    {
        using var db = new MockDb();
        var candidate = Candidate();
        Assert.Equal(ReservationValidationStatus.ReservationInputInvalid,
            (await new ReservationEngine(db.Context.Object).ValidateAsync(candidate with { ReservationDate = DateTime.SpecifyKind(candidate.ReservationDate, kind) })).Status);
        db.Context.Verify(context => context.Set<Restaurant>(), Moq.Times.Never);
    }

    [Fact]
    public async Task PastDateIsRejected()
    {
        using var db = new MockDb();
        Assert.Equal(ReservationValidationStatus.ReservationInputInvalid,
            (await new ReservationEngine(db.Context.Object).ValidateAsync(Candidate() with { ReservationDate = DateTime.UtcNow.AddDays(-1) })).Status);
    }

    [Theory]
    [InlineData("missingRestaurant", ReservationValidationStatus.RestaurantNotFound)]
    [InlineData("inactiveRestaurant", ReservationValidationStatus.RestaurantInactive)]
    [InlineData("missingTable", ReservationValidationStatus.TableNotFound)]
    [InlineData("wrongRestaurant", ReservationValidationStatus.TableDoesNotBelongToRestaurant)]
    [InlineData("inactiveTable", ReservationValidationStatus.TableInactive)]
    [InlineData("capacity", ReservationValidationStatus.PartySizeExceedsTableCapacity)]
    public async Task InvalidAllocationReturnsSpecificFailure(string scenario, ReservationValidationStatus expected)
    {
        using var db = AvailableTable();
        switch (scenario)
        {
            case "missingRestaurant": db.Restaurants.Clear(); break;
            case "inactiveRestaurant": db.Restaurants[0].IsActive = false; break;
            case "missingTable": db.Tables.Clear(); break;
            case "wrongRestaurant": db.Tables[0].RestaurantId = 2; break;
            case "inactiveTable": db.Tables[0].IsActive = false; break;
            case "capacity": db.Tables[0].Capacity = 3; break;
        }
        Assert.Equal(expected, (await new ReservationEngine(db.Context.Object).ValidateAsync(Candidate())).Status);
        db.VerifyNotSaved();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-90)]
    public async Task NonpositiveDurationIsRejected(int duration)
    {
        using var db = AvailableTable();
        Assert.Equal(ReservationValidationStatus.ReservationDurationInvalid,
            (await new ReservationEngine(db.Context.Object).ValidateAsync(Candidate() with { DurationMinutes = duration })).Status);
    }

    [Fact]
    public async Task DurationOverflowIsRejected()
    {
        using var db = AvailableTable();
        var date = DateTime.SpecifyKind(DateTime.MaxValue.AddMinutes(-10), DateTimeKind.Utc);
        Assert.Equal(ReservationValidationStatus.ReservationDurationInvalid,
            (await new ReservationEngine(db.Context.Object).ValidateAsync(Candidate() with { ReservationDate = date, DurationMinutes = 11 })).Status);
    }

    [Theory]
    [InlineData("Pending", -89, ReservationValidationStatus.ReservationConflict)]
    [InlineData("Confirmed", 89, ReservationValidationStatus.ReservationConflict)]
    [InlineData("Pending", 0, ReservationValidationStatus.ReservationConflict)]
    [InlineData("Cancelled", 0, ReservationValidationStatus.Success)]
    [InlineData("Pending", -90, ReservationValidationStatus.Success)]
    [InlineData("Confirmed", 90, ReservationValidationStatus.Success)]
    public async Task OverlapUsesBlockingStatusesAndHalfOpenIntervals(string status, int offset, ReservationValidationStatus expected)
    {
        using var db = AvailableTable();
        var candidate = Candidate();
        db.Reservations.Add(new Reservation { Id = 7, TableId = 10, ReservationDate = candidate.ReservationDate.AddMinutes(offset), DurationMinutes = 90, Status = status });
        Assert.Equal(expected, (await new ReservationEngine(db.Context.Object).ValidateAsync(candidate)).Status);
    }

    [Fact]
    public async Task UpdatingOwnReservationExcludesItButStillChecksOtherReservations()
    {
        using var db = AvailableTable();
        var candidate = Candidate();
        db.Reservations.Add(new Reservation { Id = 7, TableId = 10, ReservationDate = candidate.ReservationDate, DurationMinutes = 90, Status = "Confirmed" });
        var engine = new ReservationEngine(db.Context.Object);
        Assert.Equal(ReservationValidationStatus.Success, (await engine.ValidateAsync(candidate, 7)).Status);
        db.Reservations.Add(new Reservation { Id = 8, TableId = 10, ReservationDate = candidate.ReservationDate, DurationMinutes = 90, Status = "Pending" });
        Assert.Equal(ReservationValidationStatus.ReservationConflict, (await engine.ValidateAsync(candidate, 7)).Status);
    }

    [Fact]
    public async Task ReservationOnAnotherTableDoesNotConflict()
    {
        using var db = AvailableTable();
        var candidate = Candidate();
        db.Reservations.Add(new Reservation { Id = 7, TableId = 99, ReservationDate = candidate.ReservationDate, DurationMinutes = 90, Status = "Pending" });
        Assert.Equal(ReservationValidationStatus.Success, (await new ReservationEngine(db.Context.Object).ValidateAsync(candidate)).Status);
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(1, 0)]
    [InlineData(-1, -1)]
    public async Task SuggestInvalidInputReturnsNullWithoutReadingTables(int restaurantId, int partySize)
    {
        using var db = new MockDb();
        Assert.Null(await new ReservationEngine(db.Context.Object).SuggestTableAsync(restaurantId, partySize));
        db.Context.Verify(context => context.Set<RestaurantTable>(), Moq.Times.Never);
    }

    [Fact]
    public async Task SuggestUsesSmallestActiveSuitableTableWithStableTieBreak()
    {
        using var db = AvailableTable();
        var restaurant = db.Restaurants[0];
        db.Tables[0].Capacity = 8;
        db.Tables.AddRange([
            new RestaurantTable { Id = 14, RestaurantId = 1, Restaurant = restaurant, Number = 3, Capacity = 4, IsActive = true },
            new RestaurantTable { Id = 13, RestaurantId = 1, Restaurant = restaurant, Number = 2, Capacity = 4, IsActive = true },
            new RestaurantTable { Id = 12, RestaurantId = 1, Restaurant = restaurant, Number = 2, Capacity = 4, IsActive = true },
            new RestaurantTable { Id = 11, RestaurantId = 1, Restaurant = restaurant, Number = 1, Capacity = 4, IsActive = false },
            new RestaurantTable { Id = 15, RestaurantId = 1, Restaurant = restaurant, Number = 1, Capacity = 2, IsActive = true }
        ]);
        Assert.Equal(new SuggestedTable(12, 2, 4), await new ReservationEngine(db.Context.Object).SuggestTableAsync(1, 4));
    }

    [Fact]
    public async Task SuggestForInactiveRestaurantReturnsNull()
    {
        using var db = AvailableTable();
        db.Restaurants[0].IsActive = false;
        Assert.Null(await new ReservationEngine(db.Context.Object).SuggestTableAsync(1, 4));
    }

    [Fact]
    public async Task AvailabilityKeepsSuitableCandidatesButRemovesBlockedTables()
    {
        using var db = AvailableTable();
        var date = Candidate().ReservationDate;
        db.Tables.Add(new RestaurantTable { Id = 11, RestaurantId = 1, Restaurant = db.Restaurants[0], Number = 2, Capacity = 6, IsActive = true });
        db.Reservations.AddRange([
            new Reservation { TableId = 10, ReservationDate = date, DurationMinutes = 90, Status = "Pending" },
            new Reservation { TableId = 11, ReservationDate = date, DurationMinutes = 90, Status = "Cancelled" }
        ]);
        var result = await new ReservationEngine(db.Context.Object).GetAvailabilityAsync(new(1, 4, date, 90));
        Assert.Equal(new[] { 10, 11 }, result.CandidateTables.Select(table => table.Id));
        Assert.Equal(11, Assert.Single(result.AvailableTables).Id);
        Assert.Equal(date.AddMinutes(90), result.ReservationEnd);
        db.VerifyNotSaved();
    }

    [Fact]
    public async Task AvailabilityWithoutSuitableTablesReturnsEmptyLists()
    {
        using var db = AvailableTable();
        var result = await new ReservationEngine(db.Context.Object).GetAvailabilityAsync(new(1, 5, Candidate().ReservationDate, 90));
        Assert.Empty(result.CandidateTables);
        Assert.Empty(result.AvailableTables);
        db.Context.Verify(context => context.Set<Reservation>(), Moq.Times.Never);
    }
}
