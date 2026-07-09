using TableFlow.Api.DTOs;
using TableFlow.Api.Interfaces;

namespace TableFlow.Api.Services
{
    public class RestaurantService : IRestaurantService
    {
        private static readonly List<RestaurantResponse> Restaurants =
        [
            new(1, "Ocean Grill", "Seafood", "Rio de Janeiro", true),
            new(2, "Pasta House", "Italian", "São Paulo", true),
            new(3, "Green Garden", "Vegetarian", "Curitiba", false),
            new(4, "Sunset Bistro", "Contemporary", "Florianópolis", true)
        ];

        public IReadOnlyList<RestaurantResponse> GetAll()
        {
            return Restaurants;
        }

        public RestaurantResponse? GetById(int id)
        {
            return Restaurants.FirstOrDefault(r => r.Id == id);
        }

        public IReadOnlyList<RestaurantResponse> GetByCity(string city)
        {
            return Restaurants.Where(r => r.City.Equals(city, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public IReadOnlyList<RestaurantResponse> GetActive()
        {
            return Restaurants.Where(r => r.IsActive).ToList();
        }

        public IReadOnlyList<RestaurantResponse> GetByCuisineType(string cuisineType)
        {
            return Restaurants.Where(r => r.CuisineType.Equals(cuisineType, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public RestaurantResponse Create(CreateRestaurantRequest request)
        {
            var nextId = Restaurants.Count == 0
            ? 1
            : Restaurants.Max(r => r.Id) + 1;

            var restaurant = new RestaurantResponse(
                nextId,
                request.Name!,
                request.CuisineType!,
                request.City!,
                request.IsActive
            );

            Restaurants.Add(restaurant);

            return restaurant;
        }
    }
}