using TableFlow.Api.DTOs;

namespace TableFlow.Api.Interfaces
{
    public interface IRestaurantService
    {
        IReadOnlyList<RestaurantResponse> GetAll();

        RestaurantResponse? GetById(int id);

        IReadOnlyList<RestaurantResponse> GetByCity(string city);

        IReadOnlyList<RestaurantResponse> GetActive();

        IReadOnlyList<RestaurantResponse> GetByCuisineType(string cuisineType);

        RestaurantResponse Create(CreateRestaurantRequest request);
    }
}