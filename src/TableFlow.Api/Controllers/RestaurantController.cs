using Microsoft.AspNetCore.Mvc;
using TableFlow.Api.DTOs;

namespace TableFlow.Api.Controllers
{
    [ApiController]
    [Route("restaurants")]
    public class RestaurantController : ControllerBase
    {
        private static readonly List<RestaurantResponse> Restaurants =
        [
            new(1, "Ocean Grill", "Seafood", "Rio de Janeiro", true),
            new(2, "Pasta House", "Italian", "São Paulo", true),
            new(3, "Green Garden", "Vegetarian", "Curitiba", false)
        ];

        [HttpGet] public IActionResult GetAll()
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
    }
}
