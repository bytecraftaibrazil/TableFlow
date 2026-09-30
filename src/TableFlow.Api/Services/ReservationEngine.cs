using Microsoft.EntityFrameworkCore;
using TableFlow.Api.Data;
using TableFlow.Api.Interfaces;
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
    ReservationCandidate candidate)
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

            return new ReservationValidationResult(
                ReservationValidationStatus.Success
            );
        }
    }
}