using Microsoft.EntityFrameworkCore;
using TableFlow.Api.Data;
using TableFlow.Api.Interfaces;
using TableFlow.Api.Entities;
using TableFlow.Api.Models;

namespace TableFlow.Api.Services
{
    public class ReservationEngine : IReservationEngine
    {
        private readonly TableFlowDbContext _dbContext;

        public ReservationEngine(TableFlowDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ReservationValidationResult> ValidateAsync(
            ReservationCandidate candidate, int? excludedReservationId = null)
        {
            var restaurantIsActive = await _dbContext.Restaurants
                .AsNoTracking()
                .Where(restaurant =>
                    restaurant.Id == candidate.RestaurantId
                )
                .Select(restaurant =>
                    (bool?)restaurant.IsActive
                )
                .FirstOrDefaultAsync();

            if (restaurantIsActive is null)
            {
                return new ReservationValidationResult(
                    ReservationValidationStatus.RestaurantNotFound
                );
            }

            if (!restaurantIsActive.Value)
            {
                return new ReservationValidationResult(
                    ReservationValidationStatus.RestaurantInactive
                );
            }

            var tableData = await _dbContext.Tables
                .AsNoTracking()
                .Where(table =>
                    table.Id == candidate.TableId
                )
                .Select(table => new
                {
                    table.RestaurantId,
                    table.IsActive,
                    table.Capacity
                })
                .FirstOrDefaultAsync();

            if (tableData is null)
            {
                return new ReservationValidationResult(
                    ReservationValidationStatus.TableNotFound
                );
            }

            if (tableData.RestaurantId != candidate.RestaurantId)
            {
                return new ReservationValidationResult(
                    ReservationValidationStatus.TableDoesNotBelongToRestaurant
                );
            }

            if (!tableData.IsActive)
            {
                return new ReservationValidationResult(
                    ReservationValidationStatus.TableInactive
                );
            }

            if (candidate.PartySize > tableData.Capacity)
            {
                return new ReservationValidationResult(
                    ReservationValidationStatus.PartySizeExceedsTableCapacity
                );
            }

            if (candidate.DurationMinutes <= 0
                || candidate.DurationMinutes > (DateTime.MaxValue - candidate.ReservationDate).TotalMinutes)
            {
                return new ReservationValidationResult(
                    ReservationValidationStatus.ReservationDurationInvalid
                );
            }

            if (await HasConflictAsync(candidate, excludedReservationId))
                return new ReservationValidationResult(ReservationValidationStatus.ReservationConflict);

            return new ReservationValidationResult(
                ReservationValidationStatus.Success
            );
        }

        public async Task<SuggestedTable?> SuggestTableAsync(int restaurantId, int partySize)
        {
            if (restaurantId <= 0 || partySize <= 0)
                return null;

            return await BuildSuitableTableQuery(restaurantId, partySize).FirstOrDefaultAsync();
        }

        private IQueryable<SuggestedTable> BuildSuitableTableQuery(int restaurantId, int partySize)
        {
            return _dbContext.Tables
                .AsNoTracking()
                .Where(table =>
                    table.RestaurantId == restaurantId
                    && table.Restaurant.IsActive
                    && table.IsActive
                    && table.Capacity >= partySize)
                .OrderBy(table => table.Capacity)
                .ThenBy(table => table.Number)
                .ThenBy(table => table.Id)
                .Select(table => new SuggestedTable(
                    table.Id,
                    table.Number,
                    table.Capacity
                ));
        }

        public async Task<ReservationAvailabilityResult> GetAvailabilityAsync(
            ReservationAvailabilityRequest request)
        {
            var candidates = await BuildSuitableTableQuery(request.RestaurantId, request.PartySize)
                .ToListAsync();

            if (candidates.Count == 0)
                return new ReservationAvailabilityResult(
                    request.RestaurantId, request.PartySize, request.ReservationDate,
                    request.DurationMinutes, candidates, Array.Empty<SuggestedTable>());

            var candidateIds = candidates.Select(table => table.Id).ToList();
            var blockingReservations = await BuildBlockingReservationQuery(request.ReservationEnd)
                .Where(reservation => candidateIds.Contains(reservation.TableId))
                .Select(reservation => new
                {
                    reservation.TableId, reservation.ReservationDate, reservation.DurationMinutes
                })
                .ToListAsync();
            var conflictingTableIds = blockingReservations
                .Where(reservation => Overlaps(
                    reservation.ReservationDate,
                    reservation.ReservationDate.AddMinutes(reservation.DurationMinutes),
                    request.ReservationDate, request.ReservationEnd))
                .Select(reservation => reservation.TableId)
                .ToHashSet();
            var availableTables = candidates.Where(table => !conflictingTableIds.Contains(table.Id)).ToList();

            return new ReservationAvailabilityResult(
                request.RestaurantId, request.PartySize, request.ReservationDate,
                request.DurationMinutes, candidates, availableTables);
        }

        private IQueryable<Reservation> BuildBlockingReservationQuery(DateTime requestedEnd)
        {
            return _dbContext.Reservations.AsNoTracking()
                .Where(reservation =>
                    (reservation.Status == "Pending" || reservation.Status == "Confirmed")
                    && reservation.ReservationDate < requestedEnd);
        }

        private async Task<bool> HasConflictAsync(
            ReservationCandidate candidate, int? excludedReservationId)
        {
            var requestedEnd = candidate.ReservationDate.AddMinutes(candidate.DurationMinutes);
            var intervals = await BuildBlockingReservationQuery(requestedEnd)
                .Where(reservation => reservation.TableId == candidate.TableId
                    && (!excludedReservationId.HasValue || reservation.Id != excludedReservationId.Value))
                .Select(reservation => new { reservation.ReservationDate, reservation.DurationMinutes })
                .ToListAsync();

            return intervals.Any(interval => Overlaps(
                interval.ReservationDate, interval.ReservationDate.AddMinutes(interval.DurationMinutes),
                candidate.ReservationDate, requestedEnd));
        }

        // Half-open intervals allow one reservation to start exactly when another ends.
        private static bool Overlaps(
            DateTime existingStart, DateTime existingEnd,
            DateTime requestedStart, DateTime requestedEnd)
        {
            return existingStart < requestedEnd && requestedStart < existingEnd;
        }
    }
}
