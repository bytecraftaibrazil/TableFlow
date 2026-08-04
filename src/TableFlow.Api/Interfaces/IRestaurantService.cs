using TableFlow.Api.DTOs;

namespace TableFlow.Api.Interfaces
{
    public interface IRestaurantService
    {
        Task<IReadOnlyList<RestaurantResponse>> GetAllAsync();

        Task<RestaurantResponse?> GetByIdAsync(int id);

        Task<IReadOnlyList<RestaurantResponse>> GetByCityAsync(string city);

        Task<IReadOnlyList<RestaurantResponse>> GetActiveAsync();

        Task<IReadOnlyList<RestaurantResponse>> GetByCuisineTypeAsync(string cuisineType);

        Task<RestaurantResponse> CreateAsync(CreateRestaurantRequest request);

        Task<RestaurantResponse?> UpdateAsync(int id, UpdateRestaurantRequest request);

        Task<bool> DeleteAsync(int id);
    }
}