using Microsoft.EntityFrameworkCore;

namespace TableFlow.Tests;

[Trait("Category", "Integration")]
public class SeedIntegrationTests(SqlServerFixture fixture) : IClassFixture<SqlServerFixture>
{
    [SqlServerFact]
    public async Task SeedWorksAfterMigrationsAndDoesNotDuplicate()
    {
        await using var db = fixture.OpenDb();
        var sql = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "scripts", "seed-large.sql"));
        Assert.Empty(await db.Reservations.ToListAsync());
        await db.Database.ExecuteSqlRawAsync(sql);
        var counts = (await db.Restaurants.CountAsync(), await db.Tables.CountAsync(), await db.Reservations.CountAsync());
        Assert.Equal((10, 80, 480), counts);
        Assert.Equal(480, await db.Reservations.CountAsync(r => r.CustomerName.StartsWith("Carga TF ")));
        Assert.False(await db.Reservations.AnyAsync(r => r.DurationMinutes <= 0));
        await db.Database.ExecuteSqlRawAsync(sql);
        Assert.Equal(counts, (await db.Restaurants.CountAsync(), await db.Tables.CountAsync(), await db.Reservations.CountAsync()));
    }
}
