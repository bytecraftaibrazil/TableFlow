using Microsoft.EntityFrameworkCore;
using Moq;
using TableFlow.Api.Data;
using TableFlow.Api.DTOs;
using TableFlow.Api.Entities;
using TableFlow.Api.Services;

namespace TableFlow.Tests.Unit;

[Trait("Category", "Unit")]
public class RestaurantServiceTests
{
    [Fact]
    public async Task CreateTrimsFieldsAndSavesRestaurant()
    {
        using var db = new MockDb();
        var result = await new RestaurantService(db.Context.Object)
            .CreateAsync(new("  Bistro Central  ", "  Brasileira  ", "  Recife  ", true));

        var saved = Assert.Single(db.Restaurants);
        Assert.Equal("Bistro Central", saved.Name);
        Assert.Equal("Brasileira", saved.CuisineType);
        Assert.Equal("Recife", saved.City);
        Assert.True(saved.IsActive);
        Assert.Equal(new RestaurantResponse(saved.Id, saved.Name, saved.CuisineType, saved.City, true), result);
        db.VerifySavedOnce();
    }

    [Fact]
    public async Task UpdateChangesExistingRestaurantAndTrimsFields()
    {
        using var db = new MockDb();
        var restaurant = new Restaurant { Id = 7, Name = "Old", IsActive = true };
        db.Restaurants.Add(restaurant);

        var result = await new RestaurantService(db.Context.Object)
            .UpdateAsync(7, new("  New Bistro  ", "  Italian  ", "  Santos  ", false));

        Assert.Equal(new RestaurantResponse(7, "New Bistro", "Italian", "Santos", false), result);
        Assert.Equal("New Bistro", restaurant.Name);
        Assert.False(restaurant.IsActive);
        db.VerifySavedOnce();
    }

    [Fact]
    public async Task UpdateMissingRestaurantDoesNotSave()
    {
        using var db = new MockDb();
        Assert.Null(await new RestaurantService(db.Context.Object).UpdateAsync(99, new("Bistro", "Italian", "Recife", true)));
        db.VerifyNotSaved();
    }

    [Fact]
    public async Task DeleteMissingRestaurantDoesNotSave()
    {
        using var db = new MockDb();
        Assert.False(await new RestaurantService(db.Context.Object).DeleteAsync(99));
        db.VerifyNotSaved();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeleteWithRelatedRecordsRejectsWithoutRemovingRestaurant(bool hasTable)
    {
        using var db = new MockDb();
        db.Restaurants.Add(new Restaurant { Id = 7 });
        if (hasTable) db.Tables.Add(new RestaurantTable { RestaurantId = 7 });
        else db.Reservations.Add(new Reservation { RestaurantId = 7, Status = "Cancelled" });

        await Assert.ThrowsAsync<ResourceConflictException>(() => new RestaurantService(db.Context.Object).DeleteAsync(7));

        Assert.Single(db.Restaurants);
        db.RestaurantSet.Verify(set => set.Remove(It.IsAny<Restaurant>()), Times.Never);
        db.VerifyNotSaved();
    }

    [Fact]
    public async Task DeleteWithoutDependenciesRemovesAndSaves()
    {
        using var db = new MockDb();
        var restaurant = new Restaurant { Id = 7 };
        db.Restaurants.Add(restaurant);
        Assert.True(await new RestaurantService(db.Context.Object).DeleteAsync(7));
        db.RestaurantSet.Verify(set => set.Remove(restaurant), Times.Once);
        db.VerifySavedOnce();
    }

    [Fact]
    public async Task DeletePropagatesUnrelatedDatabaseFailure()
    {
        using var db = new MockDb();
        db.Restaurants.Add(new Restaurant { Id = 7 });
        var failure = new DbUpdateException("Unrelated database failure");
        db.Context.Setup(context => context.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        var actual = await Assert.ThrowsAsync<DbUpdateException>(() => new RestaurantService(db.Context.Object).DeleteAsync(7));
        Assert.Same(failure, actual);
    }
}
