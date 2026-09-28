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
            var restaurantExists = await _dbContext.Restaurants
                .AnyAsync(restaurant => restaurant.Id == candidate.RestaurantId);

            if (!restaurantExists)
            {
                return new ReservationValidationResult(
                    ReservationValidationStatus.RestaurantNotFound
                );
            }

            var tableRestaurantId = await _dbContext.Tables
                .AsNoTracking()
                .Where(table =>
                    table.Id == candidate.TableId
                )
                .Select(table =>
                    (int?)table.RestaurantId
                )
                .FirstOrDefaultAsync();

            if (tableRestaurantId is null)
            {
                return new ReservationValidationResult(
                    ReservationValidationStatus.TableNotFound
                );
            }

            if (tableRestaurantId.Value != candidate.RestaurantId)
            {
                return new ReservationValidationResult(
                    ReservationValidationStatus.TableDoesNotBelongToRestaurant
                );
            }

            return new ReservationValidationResult(
                ReservationValidationStatus.Success
            );
        }
    }
}