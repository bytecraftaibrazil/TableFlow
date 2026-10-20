using Microsoft.EntityFrameworkCore;
using Moq;
using TableFlow.Api.Data;
using TableFlow.Api.Entities;
using TableFlow.Api.Models;
using TableFlow.Api.Services;

namespace TableFlow.Tests.Unit;

[Trait("Category", "Unit")]
public class TableServiceTests
{
    [Fact]
    public async Task CreateMissingRestaurantRejectsWithoutSaving()
    {
        using var db = new MockDb();
        var result = await new TableService(db.Context.Object).CreateAsync(new(99, 1, 4, true));
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
        var result = await new TableService(db.Context.Object).CreateAsync(new(1, 2, 4, true));
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
        var result = await new TableService(db.Context.Object).CreateAsync(new(1, 3, 4, true));
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
        var result = await new TableService(db.Context.Object).UpdateAsync(99, new(1, 2, 4, true));
        Assert.Equal(TableOperationStatus.TableNotFound, result.Status);
        db.VerifyNotSaved();
    }

    [Fact]
    public async Task UpdateMissingRestaurantDoesNotChangeTable()
    {
        using var db = new MockDb();
        var table = new RestaurantTable { Id = 10, RestaurantId = 1, Number = 2, Capacity = 4 };
        db.Tables.Add(table);
        var result = await new TableService(db.Context.Object).UpdateAsync(10, new(99, 3, 6, true));
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
        var result = await new TableService(db.Context.Object).UpdateAsync(10, new(1, 2, 6, true));
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
        var result = await new TableService(db.Context.Object).UpdateAsync(10, new(1, 2, 6, false));
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
        Assert.False(await new TableService(db.Context.Object).DeleteAsync(99));
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
        await Assert.ThrowsAsync<ResourceConflictException>(() => new TableService(db.Context.Object).DeleteAsync(10));
        db.TableSet.Verify(set => set.Remove(It.IsAny<RestaurantTable>()), Times.Never);
        db.VerifyNotSaved();
    }

    [Fact]
    public async Task DeleteWithoutDependenciesRemovesAndSaves()
    {
        using var db = new MockDb();
        var table = new RestaurantTable { Id = 10 };
        db.Tables.Add(table);
        Assert.True(await new TableService(db.Context.Object).DeleteAsync(10));
        db.TableSet.Verify(set => set.Remove(table), Times.Once);
        db.VerifySavedOnce();
    }

    [Fact]
    public async Task DeletePropagatesUnrelatedDatabaseFailure()
    {
        using var db = new MockDb();
        db.Tables.Add(new RestaurantTable { Id = 10 });
        var failure = new DbUpdateException("Unrelated database failure");
        db.Context.Setup(context => context.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        Assert.Same(failure, await Assert.ThrowsAsync<DbUpdateException>(() => new TableService(db.Context.Object).DeleteAsync(10)));
    }
}
