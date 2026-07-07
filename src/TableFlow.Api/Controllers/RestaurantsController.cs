using Microsoft.AspNetCore.Mvc;
using TableFlow.Api.DTOs;

namespace TableFlow.Api.Controllers
{
    [ApiController]
    [Route("restaurants")]
    public class RestaurantsController : ControllerBase
    {
        private static readonly List<RestaurantResponse> Restaurants =
        [
            new(1, "Ocean Grill", "Seafood", "Rio de Janeiro", true),
            new(2, "Pasta House", "Italian", "São Paulo", true),
            new(3, "Green Garden", "Vegetarian", "Curitiba", false),
            new(4, "Sunset Bistro", "Contemporary", "Florianópolis", true)
        ];

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(Restaurants);
        }

        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Restaurant id must be greater than zero.");
            }

            var restaurant = Restaurants.FirstOrDefault(r => r.Id == id);
            if (restaurant is null)
                return NotFound();

            return Ok(restaurant);
        }

        [HttpGet("city/{city}")]
        public IActionResult GetByCity(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
                return BadRequest("City is required.");

            var restaurants = Restaurants.Where(r => r.City.Equals(city, StringComparison.OrdinalIgnoreCase)).ToList();

            if (restaurants.Count == 0)
                return NotFound();

            return Ok(restaurants);
        }
    }
}
