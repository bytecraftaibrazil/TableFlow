using Microsoft.AspNetCore.Mvc;
using TableFlow.Api.Interfaces;

namespace TableFlow.Api.Controllers
{
    [ApiController]
    [Route("restaurants")]
    public class RestaurantsController : ControllerBase
    {
        private readonly IRestaurantService _restaurantService;

        public RestaurantsController(IRestaurantService restaurantService)
        {
            _restaurantService = restaurantService;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var restaurants = _restaurantService.GetAll();
            return Ok(restaurants);
        }

        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Restaurant id must be greater than zero.");
            }

            var restaurant = _restaurantService.GetById(id);

            if (restaurant is null)
                return NotFound();

            return Ok(restaurant);
        }

        [HttpGet("city/{city}")]
        public IActionResult GetByCity(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
                return BadRequest("City is required.");

            var restaurants = _restaurantService.GetByCity(city);

            if (restaurants.Count == 0)
                return NotFound();

            return Ok(restaurants);
        }

        [HttpGet("cuisine/{cuisineType}")]
        public IActionResult GetByCuisineType(string cuisineType)
        {
            if (string.IsNullOrWhiteSpace(cuisineType))
                return BadRequest("CuisineType is required.");

            var restaurants = _restaurantService.GetByCuisineType(cuisineType);

            if (restaurants.Count == 0)
                return NotFound();

            return Ok(restaurants);
        }

        [HttpGet("active")]
        public IActionResult GetActive()
        {
            var restaurants = _restaurantService.GetActive();

            return Ok(restaurants);
        }
    }
}
