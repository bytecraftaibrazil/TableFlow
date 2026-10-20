using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using TableFlow.Api.Data;
using TableFlow.Api.Entities;

namespace TableFlow.Tests.Unit;

// Only service decisions are tested here. These mocks do not validate SQL translation,
// foreign keys, transaction isolation or the behavior of sp_getapplock.
internal sealed class MockDb : IDisposable
{
    public Mock<TableFlowDbContext> Context { get; } = new(
        MockBehavior.Strict, new DbContextOptionsBuilder<TableFlowDbContext>().Options);
    public List<Restaurant> Restaurants { get; } = [];
    public List<RestaurantTable> Tables { get; } = [];
    public List<Reservation> Reservations { get; } = [];
    public Mock<DbSet<Restaurant>> RestaurantSet { get; }
    public Mock<DbSet<RestaurantTable>> TableSet { get; }
    public Mock<DbSet<Reservation>> ReservationSet { get; }

    public MockDb()
    {
        RestaurantSet = BuildSet(Restaurants, r => r.Id);
        TableSet = BuildSet(Tables, t => t.Id);
        ReservationSet = BuildSet(Reservations, r => r.Id);
        Context.Setup(db => db.Set<Restaurant>()).Returns(RestaurantSet.Object);
        Context.Setup(db => db.Set<RestaurantTable>()).Returns(TableSet.Object);
        Context.Setup(db => db.Set<Reservation>()).Returns(ReservationSet.Object);
        Context.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        Context.Setup(db => db.Dispose());
    }

    private static Mock<DbSet<T>> BuildSet<T>(List<T> rows, Func<T, int> id) where T : class
    {
        var set = rows.BuildMockDbSet();
        set.Setup(s => s.FindAsync(It.IsAny<object[]>()))
            .Returns((object[] keys) => ValueTask.FromResult(rows.SingleOrDefault(row => id(row) == (int)keys[0])));
        set.Setup(s => s.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .Callback<T, CancellationToken>((row, _) => rows.Add(row));
        set.Setup(s => s.Remove(It.IsAny<T>())).Callback<T>(row => rows.Remove(row));
        return set;
    }

    public void VerifyNotSaved() => Context.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    public void VerifySavedOnce() => Context.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    public void Dispose() => Context.Object.Dispose();
}
