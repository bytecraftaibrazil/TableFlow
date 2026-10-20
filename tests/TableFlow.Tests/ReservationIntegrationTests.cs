using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TableFlow.Api.Data;
using TableFlow.Api.DTOs;
using TableFlow.Api.Entities;
using TableFlow.Api.Models;
using TableFlow.Api.Services;

namespace TableFlow.Tests;

[Trait("Category", "Integration")]
public class ReservationIntegrationTests(SqlServerFixture fixture) : IClassFixture<SqlServerFixture>
{
    private HttpClient Client() => fixture.Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
    private static ReservationService Service(TableFlowDbContext db) => new(db, new ReservationEngine(db));

    private async Task<(int Restaurant, int Table)> SetupAsync(bool restaurantActive = true, bool tableActive = true)
    {
        await using var db = fixture.OpenDb();
        var restaurant = new Restaurant { Name = "Test restaurant", City = "Test city", CuisineType = "Test cuisine", IsActive = restaurantActive };
        var table = new RestaurantTable { Restaurant = restaurant, Capacity = 4, Number = 1, IsActive = tableActive };
        db.Tables.Add(table);
        await db.SaveChangesAsync();
        return (restaurant.Id, table.Id);
    }

    private static CreateReservationRequest Request((int Restaurant, int Table) ids)
        => new(ids.Restaurant, ids.Table, "Customer Test", DateTime.UtcNow.AddDays(30), 90, 4);

    [SqlServerFact]
    public async Task RulesCapacityActivityOwnershipDurationAndDates()
    {
        var ids = await SetupAsync();
        var inactiveRestaurant = await SetupAsync(restaurantActive: false);
        var inactiveTable = await SetupAsync(tableActive: false);
        await using var db = fixture.OpenDb();
        var engine = new ReservationEngine(db);
        var candidate = new ReservationCandidate(ids.Restaurant, ids.Table, 4, DateTime.UtcNow.AddDays(1), 90);
        Assert.Equal(ReservationValidationStatus.Success, (await engine.ValidateAsync(candidate)).Status);
        Assert.Equal(ReservationValidationStatus.PartySizeExceedsTableCapacity, (await engine.ValidateAsync(candidate with { PartySize = 5 })).Status);
        Assert.Equal(ReservationValidationStatus.RestaurantInactive, (await engine.ValidateAsync(candidate with { RestaurantId = inactiveRestaurant.Restaurant, TableId = inactiveRestaurant.Table })).Status);
        Assert.Equal(ReservationValidationStatus.TableInactive, (await engine.ValidateAsync(candidate with { RestaurantId = inactiveTable.Restaurant, TableId = inactiveTable.Table })).Status);
        Assert.Equal(ReservationValidationStatus.TableDoesNotBelongToRestaurant, (await engine.ValidateAsync(candidate with { TableId = inactiveTable.Table })).Status);
        Assert.Equal(ReservationValidationStatus.RestaurantNotFound, (await engine.ValidateAsync(candidate with { RestaurantId = int.MaxValue })).Status);
        Assert.Equal(ReservationValidationStatus.TableNotFound, (await engine.ValidateAsync(candidate with { TableId = int.MaxValue })).Status);
        foreach (var duration in new[] { 0, -1, int.MaxValue })
            Assert.Equal(ReservationValidationStatus.ReservationDurationInvalid, (await engine.ValidateAsync(candidate with { DurationMinutes = duration, ReservationDate = DateTime.SpecifyKind(DateTime.MaxValue.AddDays(-1), DateTimeKind.Utc) })).Status);
        foreach (var date in new[] { DateTime.UtcNow.AddDays(-1), DateTime.SpecifyKind(DateTime.UtcNow.AddDays(1), DateTimeKind.Unspecified) })
            Assert.Equal(ReservationValidationStatus.ReservationInputInvalid, (await engine.ValidateAsync(candidate with { ReservationDate = date })).Status);
    }

    [SqlServerFact]
    public async Task OverlapAdjacentUpdateSelfAndStatusTransitions()
    {
        var ids = await SetupAsync();
        var request = Request(ids);
        await using var db = fixture.OpenDb();
        var service = Service(db);
        var created = await service.CreateAsync(request);
        Assert.Equal(ReservationOperationStatus.Success, created.Status);
        var id = created.Reservation!.Id;
        Assert.Equal(ReservationOperationStatus.ReservationConflict, (await service.CreateAsync(request with { ReservationDate = request.ReservationDate.AddMinutes(89) })).Status);
        var adjacent = await service.CreateAsync(request with { ReservationDate = request.ReservationDate.AddMinutes(90) });
        Assert.Equal(ReservationOperationStatus.Success, adjacent.Status);
        var update = new UpdateReservationRequest(ids.Restaurant, ids.Table, request.CustomerName, request.ReservationDate, 90, 4);
        Assert.Equal(ReservationOperationStatus.Success, (await service.UpdateAsync(id, update)).Status);
        Assert.Equal(ReservationOperationStatus.ReservationConflict, (await service.UpdateAsync(id, update with { DurationMinutes = 91 })).Status);
        Assert.Equal("Confirmed", (await service.ConfirmAsync(id)).Reservation!.Status);
        Assert.Equal("Confirmed", (await service.ConfirmAsync(id)).Reservation!.Status);
        Assert.Equal("Cancelled", (await service.CancelAsync(id)).Reservation!.Status);
        Assert.Equal("Cancelled", (await service.CancelAsync(id)).Reservation!.Status);
        Assert.Equal(ReservationOperationStatus.InvalidStatusTransition, (await service.ConfirmAsync(id)).Status);
        Assert.Equal(ReservationOperationStatus.CancelledReservationCannotBeUpdated, (await service.UpdateAsync(id, update)).Status);
        Assert.Equal(ReservationOperationStatus.Success, (await service.CreateAsync(request)).Status);
    }

    [SqlServerFact]
    public async Task ConcurrentHttpCreatesHaveExactlyOneWinner()
    {
        var ids = await SetupAsync();
        var request = Request(ids);
        // Independent HTTP requests/scopes/connections against the same SQL Server.
        using var client = Client();
        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => client.PostAsJsonAsync("/reservations", request)));
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Equal(5, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
        await using var db = fixture.OpenDb();
        Assert.Equal(1, await db.Reservations.CountAsync(r => r.TableId == ids.Table && r.Status != "Cancelled"));
        foreach (var response in responses.Where(r => r.StatusCode == HttpStatusCode.Conflict))
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [SqlServerFact]
    public async Task ConcurrentUpdatesMovingToOneTableHaveOneWinner()
    {
        var first = await SetupAsync();
        var second = await SetupAsync();
        var target = await SetupAsync();
        var request = Request(first);
        using var client = Client();
        var a = await (await client.PostAsJsonAsync("/reservations", request)).Content.ReadFromJsonAsync<ReservationResponse>();
        var b = await (await client.PostAsJsonAsync("/reservations", request with { RestaurantId = second.Restaurant, TableId = second.Table })).Content.ReadFromJsonAsync<ReservationResponse>();
        var update = new UpdateReservationRequest(target.Restaurant, target.Table, request.CustomerName, request.ReservationDate, 90, 4);
        var results = await Task.WhenAll(client.PutAsJsonAsync($"/reservations/{a!.Id}", update), client.PutAsJsonAsync($"/reservations/{b!.Id}", update));
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.Conflict);
        await using var db = fixture.OpenDb();
        Assert.Equal(1, await db.Reservations.CountAsync(r => r.TableId == target.Table));
    }

    [SqlServerFact]
    public async Task AvailabilitySearchPaginationAndUtcRoundTrip()
    {
        var ids = await SetupAsync();
        var request = Request(ids);
        using var client = Client();
        var date = Uri.EscapeDataString(request.ReservationDate.ToString("O"));
        var url = $"/reservations/availability?restaurantId={ids.Restaurant}&partySize=4&reservationDate={date}&durationMinutes=90";
        var available = await client.GetFromJsonAsync<ReservationAvailabilityResponse>(url);
        Assert.Single(available!.AvailableTables);
        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/reservations", request with { ReservationDate = request.ReservationDate.AddDays(i) })).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<ReservationAvailabilityResponse>(url))!.AvailableTables);
        var page = await client.GetFromJsonAsync<PagedResult<ReservationResponse>>($"/reservations/search?restaurantId={ids.Restaurant}&pageSize=2&pageNumber=1&status=Pending");
        Assert.Equal(3, page!.TotalCount);
        Assert.Equal(2, page.TotalPages);
        Assert.True(page.HasNextPage);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(DateTimeKind.Utc, page.Items[0].ReservationDate.Kind);
        var next = await client.GetFromJsonAsync<PagedResult<ReservationResponse>>($"/reservations/search?restaurantId={ids.Restaurant}&pageSize=2&pageNumber=2");
        Assert.Single(next!.Items);
        Assert.False(next.HasNextPage);
        Assert.DoesNotContain(next.Items[0].Id, page.Items.Select(r => r.Id));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/reservations/search?fromDate=2030-01-01T12:00:00")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/reservations/search?pageSize=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(url.Replace(date, "2030-01-01T12:00:00"))).StatusCode);
        var ambiguous = JsonSerializer.Serialize(request, new JsonSerializerOptions(JsonSerializerDefaults.Web)).Replace(request.ReservationDate.ToString("O"), "2030-01-01T12:00:00");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/reservations", new StringContent(ambiguous, Encoding.UTF8, "application/json"))).StatusCode);
        await using var db = fixture.OpenDb();
        await Service(db).CancelAsync(page.Items[0].Id);
        Assert.Single((await client.GetFromJsonAsync<ReservationAvailabilityResponse>(url))!.AvailableTables);
    }

    [SqlServerFact]
    public async Task HttpOffsetsNormalizeBeforePersistenceAndAvailability()
    {
        var ids = await SetupAsync();
        var request = Request(ids);
        var offset = new DateTimeOffset(request.ReservationDate).ToOffset(TimeSpan.FromHours(-3));
        using var client = Client();
        var response = await client.PostAsJsonAsync("/reservations", new
        {
            request.RestaurantId,
            request.TableId,
            request.CustomerName,
            ReservationDate = offset.ToString("O"),
            request.DurationMinutes,
            request.PartySize
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var reservation = await response.Content.ReadFromJsonAsync<ReservationResponse>();
        Assert.Equal(request.ReservationDate, reservation!.ReservationDate);
        Assert.Equal(DateTimeKind.Utc, reservation.ReservationDate.Kind);
        var date = Uri.EscapeDataString(offset.ToOffset(TimeSpan.FromHours(2)).ToString("O"));
        var result = await client.GetFromJsonAsync<ReservationAvailabilityResponse>(
            $"/reservations/availability?restaurantId={ids.Restaurant}&partySize=4&reservationDate={date}&durationMinutes=90");
        Assert.Empty(result!.AvailableTables);
        Assert.Equal(request.ReservationDate, result.ReservationDate);
        var page = await client.GetFromJsonAsync<PagedResult<ReservationResponse>>(
            $"/reservations/search?restaurantId={ids.Restaurant}&fromDate={date}&toDate={date}&minimumPartySize=4&descending=true");
        Assert.Single(page!.Items);
        Assert.Equal(reservation.Id, page.Items[0].Id);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/reservations", request with { DurationMinutes = 0 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/reservations", request with { ReservationDate = DateTime.UtcNow.AddDays(-1) })).StatusCode);
    }

    [SqlServerFact]
    public async Task RelatedDeletesReturnProblem409AndUnreferencedDeletes204()
    {
        var ids = await SetupAsync();
        using var client = Client();
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/restaurants/{ids.Restaurant}")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/reservations", Request(ids))).StatusCode);
        var conflict = await client.DeleteAsync($"/tables/{ids.Table}");
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("application/problem+json", conflict.Content.Headers.ContentType!.MediaType);
        var empty = await SetupAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/tables/{empty.Table}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/restaurants/{empty.Restaurant}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/tables/{empty.Table}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/restaurants/{empty.Restaurant}")).StatusCode);
    }


    private sealed class BeforeSave(Func<Task> action) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await action();
            return result;
        }
    }

    [SqlServerFact]
    public async Task ForeignKeyRaceBetweenCheckAndDeleteReturnsConflict()
    {
        var ids = await SetupAsync();
        var interceptor = new BeforeSave(async () =>
        {
            await using var other = fixture.OpenDb();
            Assert.Equal(ReservationOperationStatus.Success, (await Service(other).CreateAsync(Request(ids))).Status);
        });
        await using (var deleting = new TableFlowDbContext(new DbContextOptionsBuilder<TableFlowDbContext>()
            .UseSqlServer(fixture.ConnectionString).AddInterceptors(interceptor).Options))
        {
            await Assert.ThrowsAsync<ResourceConflictException>(() => new TableService(deleting).DeleteAsync(ids.Table));
        }
        var empty = await SetupAsync();
        await using (var other = fixture.OpenDb())
        {
            Assert.True(await new TableService(other).DeleteAsync(empty.Table));
        }
        var restaurantInterceptor = new BeforeSave(async () =>
        {
            await using var other = fixture.OpenDb();
            other.Tables.Add(new RestaurantTable { RestaurantId = empty.Restaurant, Number = 2, Capacity = 4, IsActive = true });
            await other.SaveChangesAsync();
        });
        await using var restaurantDb = new TableFlowDbContext(new DbContextOptionsBuilder<TableFlowDbContext>()
            .UseSqlServer(fixture.ConnectionString).AddInterceptors(restaurantInterceptor).Options);
        await Assert.ThrowsAsync<ResourceConflictException>(() => new RestaurantService(restaurantDb).DeleteAsync(empty.Restaurant));
    }

    [SqlServerFact]
    public async Task ConcurrentConfirmAndCancelCannotResurrectCancelledReservation()
    {
        var ids = await SetupAsync();
        using var client = Client();
        var created = await (await client.PostAsJsonAsync("/reservations", Request(ids))).Content.ReadFromJsonAsync<ReservationResponse>();
        var results = await Task.WhenAll(client.PutAsync($"/reservations/{created!.Id}/cancel", null), client.PutAsync($"/reservations/{created.Id}/confirm", null));
        Assert.All(results, response => Assert.Contains(response.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }));
        Assert.Equal("Cancelled", (await client.GetFromJsonAsync<ReservationResponse>($"/reservations/{created.Id}"))!.Status);
    }

    [SqlServerFact]
    public async Task LegacyDatetime2ValuesAreNeverShiftedAutomatically()
    {
        var ids = await SetupAsync();
        await using var db = fixture.OpenDb();
        // Simulate an audited old row whose wall-clock value is already UTC.
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Reservations (RestaurantId, TableId, CustomerName, ReservationDate, DurationMinutes, PartySize, Status) VALUES ({ids.Restaurant}, {ids.Table}, N'Legacy audited UTC', '2030-01-01T12:00:00', 90, 4, 'Pending')");
        var row = await db.Reservations.SingleAsync(r => r.TableId == ids.Table);
        Assert.Equal(new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc), row.ReservationDate);
        Assert.Equal(DateTimeKind.Utc, row.ReservationDate.Kind);
        var ambiguous = Request(ids) with { ReservationDate = new DateTime(2030, 1, 2, 12, 0, 0) };
        Assert.Equal(ReservationOperationStatus.ReservationInputInvalid, (await Service(db).CreateAsync(ambiguous)).Status);
    }

}
