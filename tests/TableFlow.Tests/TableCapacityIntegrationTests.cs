using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TableFlow.Api.DTOs;
using TableFlow.Api.Entities;

namespace TableFlow.Tests;

[Trait("Category", "Integration")]
public class TableCapacityIntegrationTests(SqlServerFixture fixture) : IClassFixture<SqlServerFixture>
{
    private HttpClient Client() => fixture.Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

    private async Task<(int Restaurant, int Table)> SetupAsync(
        string? status = null, int startOffset = 60, int partySize = 6)
    {
        await using var db = fixture.OpenDb();
        var restaurant = new Restaurant { Name = "Capacity test", CuisineType = "Test cuisine", City = "Test city", IsActive = true };
        var table = new RestaurantTable { Restaurant = restaurant, Number = 1, Capacity = 8, IsActive = true };
        db.Tables.Add(table);
        if (status is not null)
            db.Reservations.Add(new Reservation
            {
                Restaurant = restaurant,
                Table = table,
                CustomerName = "Capacity customer",
                ReservationDate = DateTime.UtcNow.AddMinutes(startOffset),
                DurationMinutes = 90,
                PartySize = partySize,
                Status = status
            });
        await db.SaveChangesAsync();
        return (restaurant.Id, table.Id);
    }

    [SqlServerFact]
    public async Task InsufficientReductionRejectsFutureAndOngoingActiveReservationsWithoutChangingTable()
    {
        using var client = Client();
        foreach (var status in new[] { "Pending", "Confirmed" })
            foreach (var offset in new[] { 60, -30 })
            {
                var ids = await SetupAsync(status, offset);
                var response = await client.PutAsJsonAsync($"/tables/{ids.Table}", new UpdateTableRequest(ids.Restaurant, 2, 4, false));

                Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
                Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
                var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
                Assert.Equal(409, problem!.Status);
                Assert.Equal("Table capacity conflict", problem.Title);
                await using var db = fixture.OpenDb();
                var table = await db.Tables.SingleAsync(t => t.Id == ids.Table);
                Assert.Equal(8, table.Capacity);
                Assert.Equal(1, table.Number);
                Assert.True(table.IsActive);
                Assert.Equal(ids.Restaurant, table.RestaurantId);
                Assert.Equal(6, (await db.Reservations.SingleAsync(r => r.TableId == ids.Table)).PartySize);
            }
    }

    [SqlServerFact]
    public async Task ReductionIgnoresCancelledAndEndedReservations()
    {
        using var client = Client();
        foreach (var scenario in new[]
        {
            (Status: "Cancelled", Offset: 60), (Status: "Cancelled", Offset: -30),
            (Status: "Pending", Offset: -180), (Status: "Confirmed", Offset: -180),
            (Status: "Confirmed", Offset: -90)
        })
        {
            var ids = await SetupAsync(scenario.Status, scenario.Offset);
            var response = await client.PutAsJsonAsync($"/tables/{ids.Table}", new UpdateTableRequest(ids.Restaurant, 1, 4, true));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            await using var db = fixture.OpenDb();
            Assert.Equal(4, (await db.Tables.SingleAsync(t => t.Id == ids.Table)).Capacity);
            Assert.Equal(6, (await db.Reservations.SingleAsync(r => r.TableId == ids.Table)).PartySize);
        }
    }

    [SqlServerFact]
    public async Task ValidReductionsIncreasesAndUnchangedCapacityWorkNormally()
    {
        using var client = Client();
        foreach (var scenario in new[]
        {
            (Status: "Pending", Offset: 60, Party: 4, Capacity: 4),
            (Status: "Confirmed", Offset: -30, Party: 4, Capacity: 4),
            (Status: "Pending", Offset: 60, Party: 6, Capacity: 10),
            (Status: "Confirmed", Offset: -30, Party: 6, Capacity: 10),
            (Status: "Confirmed", Offset: 60, Party: 6, Capacity: 8)
        })
        {
            var ids = await SetupAsync(scenario.Status, scenario.Offset, scenario.Party);
            var response = await client.PutAsJsonAsync($"/tables/{ids.Table}", new UpdateTableRequest(ids.Restaurant, 2, scenario.Capacity, false));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<TableResponse>();
            Assert.Equal(scenario.Capacity, result!.Capacity);
            await using var db = fixture.OpenDb();
            var table = await db.Tables.SingleAsync(t => t.Id == ids.Table);
            Assert.Equal(scenario.Capacity, table.Capacity);
            Assert.Equal(2, table.Number);
            Assert.False(table.IsActive);
        }
    }

    [SqlServerFact]
    public async Task ReductionChecksAllReservationsButIgnoresReservationsOnOtherTables()
    {
        using var client = Client();
        var ids = await SetupAsync("Pending", partySize: 2);
        var other = await SetupAsync("Confirmed", partySize: 8);
        var request = new UpdateTableRequest(ids.Restaurant, 1, 4, true);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/tables/{ids.Table}", request)).StatusCode);

        var blocked = await SetupAsync("Pending", partySize: 2);
        await using (var db = fixture.OpenDb())
        {
            db.Reservations.Add(new Reservation
            {
                RestaurantId = blocked.Restaurant,
                TableId = blocked.Table,
                CustomerName = "Larger customer",
                ReservationDate = DateTime.UtcNow.AddDays(2),
                DurationMinutes = 90,
                PartySize = 6,
                Status = "Confirmed"
            });
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/tables/{blocked.Table}", request with { RestaurantId = blocked.Restaurant })).StatusCode);
        await using var verify = fixture.OpenDb();
        Assert.Equal(8, (await verify.Tables.SingleAsync(t => t.Id == blocked.Table)).Capacity);
        Assert.Equal(8, (await verify.Tables.SingleAsync(t => t.Id == other.Table)).Capacity);
        var empty = await SetupAsync();
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/tables/{empty.Table}", request with { RestaurantId = empty.Restaurant })).StatusCode);
    }

    [SqlServerFact]
    public async Task ConcurrentReservationAndCapacityReductionCannotPersistIncompatibleValues()
    {
        using var client = Client();
        for (var attempt = 0; attempt < 4; attempt++)
        {
            var ids = await SetupAsync();
            var reservation = new CreateReservationRequest(ids.Restaurant, ids.Table, "Concurrent customer", DateTime.UtcNow.AddDays(1), 90, 6);
            var update = new UpdateTableRequest(ids.Restaurant, 1, 4, true);
            var bookingTask = client.PostAsJsonAsync("/reservations", reservation);
            var updateTask = client.PutAsJsonAsync($"/tables/{ids.Table}", update);
            await Task.WhenAll(bookingTask, updateTask);
            var bookingStatus = (await bookingTask).StatusCode;
            var updateStatus = (await updateTask).StatusCode;
            Assert.True(
                (bookingStatus == HttpStatusCode.Created && updateStatus == HttpStatusCode.Conflict)
                || (bookingStatus == HttpStatusCode.Conflict && updateStatus == HttpStatusCode.OK),
                $"Unexpected booking/update results: {bookingStatus}/{updateStatus}");

            await using var db = fixture.OpenDb();
            var capacity = (await db.Tables.SingleAsync(t => t.Id == ids.Table)).Capacity;
            Assert.False(await db.Reservations.AnyAsync(r => r.TableId == ids.Table
                && (r.Status == "Pending" || r.Status == "Confirmed") && r.PartySize > capacity));
        }
    }
}
