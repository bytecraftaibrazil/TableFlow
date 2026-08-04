using Microsoft.EntityFrameworkCore;
using TableFlow.Api.Data;
using TableFlow.Api.DTOs;
using TableFlow.Api.Entities;
using TableFlow.Api.Interfaces;

namespace TableFlow.Api.Services
{
    public class RestaurantService : IRestaurantService
    {
        private readonly TableFlowDbContext _dbContext;

        public RestaurantService(
            TableFlowDbContext dbContext
        )
        {
            _dbContext = dbContext;
        }

        private static RestaurantResponse ToResponse(Restaurant restaurant)
        {
            return new RestaurantResponse(
                restaurant.Id,
                restaurant.Name,
                restaurant.CuisineType,
                restaurant.City,
                restaurant.IsActive
            );
        }

        public async Task<IReadOnlyList<RestaurantResponse>> GetAllAsync()
        {
            return await _dbContext.Restaurants
            .AsNoTracking()
            .OrderBy(restaurant => restaurant.Id)
            .Select(restaurant =>
                new RestaurantResponse(
                restaurant.Id,
                restaurant.Name,
                restaurant.CuisineType,
                restaurant.City,
                restaurant.IsActive
                )
            ).ToListAsync();
        }

        public async Task<RestaurantResponse?> GetByIdAsync(int id)
        {
            return await _dbContext.Restaurants
                .AsNoTracking()
                .Where(restaurant => restaurant.Id == id)
                .Select(restaurant =>
                    new RestaurantResponse(
                        restaurant.Id,
                        restaurant.Name,
                        restaurant.CuisineType,
                        restaurant.City,
                        restaurant.IsActive
                )
            ).FirstOrDefaultAsync();
        }

        public async Task<IReadOnlyList<RestaurantResponse>> GetByCityAsync(string city)
        {
            var normalizedCity = city.Trim();

            return await _dbContext.Restaurants
                .AsNoTracking()
                .Where(restaurant => restaurant.City == normalizedCity)
                .OrderBy(restaurant => restaurant.Name)
                .Select(restaurant =>
                    new RestaurantResponse(
                        restaurant.Id,
                        restaurant.Name,
                        restaurant.CuisineType,
                        restaurant.City,
                        restaurant.IsActive
                    )
                ).ToListAsync();
        }

        public async Task<IReadOnlyList<RestaurantResponse>> GetByCuisineTypeAsync(string cuisineType)
        {
            var normalizedCuisineType =
                cuisineType.Trim();

            return await _dbContext.Restaurants
                .AsNoTracking()
                .Where(restaurant =>
                    restaurant.CuisineType
                        == normalizedCuisineType
                )
                .OrderBy(restaurant =>
                    restaurant.Name
                )
                .Select(restaurant =>
                    new RestaurantResponse(
                        restaurant.Id,
                        restaurant.Name,
                        restaurant.CuisineType,
                        restaurant.City,
                        restaurant.IsActive
                    )
                ).ToListAsync();
        }

        public async Task<IReadOnlyList<RestaurantResponse>> GetActiveAsync()
        {
            return await _dbContext.Restaurants
                .AsNoTracking()
                .Where(restaurant =>
                    restaurant.IsActive
                )
                .OrderBy(restaurant =>
                    restaurant.Name
                )
                .Select(restaurant =>
                    new RestaurantResponse(
                        restaurant.Id,
                        restaurant.Name,
                        restaurant.CuisineType,
                        restaurant.City,
                        restaurant.IsActive
                    )
                )
                .ToListAsync();
        }

        public async Task<RestaurantResponse> CreateAsync(CreateRestaurantRequest request)
        {
            var restaurant = new Restaurant
            {
                Name = request.Name!.Trim(),
                CuisineType = request.CuisineType!.Trim(),
                City = request.City!.Trim(),
                IsActive = request.IsActive
            };

            await _dbContext.Restaurants.AddAsync(restaurant);

            await _dbContext.SaveChangesAsync();

            return ToResponse(restaurant);
        }

        public async Task<RestaurantResponse?> UpdateAsync(int id, UpdateRestaurantRequest request)
        {
            var restaurant = await _dbContext.Restaurants.FindAsync(id);
            if (restaurant is null)
                return null;


            restaurant.Name = request.Name!.Trim();
            restaurant.CuisineType = request.CuisineType!.Trim();
            restaurant.City = request.City!.Trim();
            restaurant.IsActive = request.IsActive;

            await _dbContext.SaveChangesAsync();

            return ToResponse(restaurant);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var restaurant = await _dbContext.Restaurants.FindAsync(id);

            if (restaurant is null)
                return false;

            _dbContext.Restaurants.Remove(restaurant);

            await _dbContext.SaveChangesAsync();

            return true;
        }
    }
}