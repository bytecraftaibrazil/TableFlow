using Microsoft.AspNetCore.Mvc;
using TableFlow.Api.Interfaces;
using TableFlow.Api.DTOs;

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

        #region GET

        [HttpGet]
        [ProducesResponseType(
            typeof(IReadOnlyList<RestaurantResponse>),
            StatusCodes.Status200OK
        )]
        public ActionResult<IReadOnlyList<RestaurantResponse>> GetAll()
        {
            var restaurants = _restaurantService.GetAll();
            return Ok(restaurants);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(
            typeof(RestaurantResponse),
            StatusCodes.Status200OK
        )]
        [ProducesResponseType(
            typeof(string),
            StatusCodes.Status400BadRequest
        )]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<RestaurantResponse> GetById(int id)
        {
            if (id <= 0)
                return BadRequest("Restaurant id must be greater than zero.");

            var restaurant = _restaurantService.GetById(id);

            if (restaurant is null)
                return NotFound();

            return Ok(restaurant);
        }

        [HttpGet("city")]
        [HttpGet("city/{city}")]
        [ProducesResponseType(
        typeof(IReadOnlyList<RestaurantResponse>),
            StatusCodes.Status200OK
        )]
        [ProducesResponseType(
            typeof(string),
            StatusCodes.Status400BadRequest
        )]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<IReadOnlyList<RestaurantResponse>> GetByCity(string? city)
        {
            if (string.IsNullOrWhiteSpace(city))
                return BadRequest("City is required.");

            var restaurants = _restaurantService.GetByCity(city);

            if (restaurants.Count == 0)
                return NotFound();

            return Ok(restaurants);
        }

        [HttpGet("cuisine")]
        [HttpGet("cuisine/{cuisineType}")]
        [ProducesResponseType(
            typeof(IReadOnlyList<RestaurantResponse>),
            StatusCodes.Status200OK
        )]
        [ProducesResponseType(
            typeof(string),
            StatusCodes.Status400BadRequest
        )]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<IReadOnlyList<RestaurantResponse>> GetByCuisineType(string? cuisineType)
        {
            if (string.IsNullOrWhiteSpace(cuisineType))
                return BadRequest("Cuisine type is required.");

            var restaurants = _restaurantService.GetByCuisineType(cuisineType);

            if (restaurants.Count == 0)
                return NotFound();

            return Ok(restaurants);
        }

        [HttpGet("active")]
        [ProducesResponseType(
            typeof(IReadOnlyList<RestaurantResponse>),
            StatusCodes.Status200OK
        )]
        public ActionResult<IReadOnlyList<RestaurantResponse>> GetActive()
        {
            var restaurants = _restaurantService.GetActive();

            return Ok(restaurants);
        }

        #endregion

        #region POST
        [HttpPost]
        [ProducesResponseType(
            typeof(RestaurantResponse),
            StatusCodes.Status201Created
        )]
        [ProducesResponseType(
            typeof(string),
            StatusCodes.Status400BadRequest
        )]
        public ActionResult<RestaurantResponse> Create(CreateRestaurantRequest request)
        {
            var validationError = ValidateRestaurantInput(
                request.Name,
                request.CuisineType,
                request.City
            );

            if (validationError is not null)
                return BadRequest(validationError);

            var restaurant = _restaurantService.Create(request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = restaurant.Id },
                restaurant
            );
        }
        #endregion

        #region PUT
        [HttpPut("{id:int}")]
        [ProducesResponseType(
            typeof(RestaurantResponse),
            StatusCodes.Status200OK
        )]
        [ProducesResponseType(
            typeof(string),
            StatusCodes.Status400BadRequest
        )]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<RestaurantResponse> Update(int id, UpdateRestaurantRequest request)
        {
            if (id <= 0)
                return BadRequest("Restaurant id must be greater than zero.");

            var validationError = ValidateRestaurantInput(
               request.Name,
               request.CuisineType,
               request.City
           );

            if (validationError is not null)
                return BadRequest(validationError);

            var restaurant = _restaurantService.Update(id, request);

            if (restaurant is null)
                return NotFound();

            return Ok(restaurant);
        }
        #endregion

        #region DELETE
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(
            typeof(string),
            StatusCodes.Status400BadRequest
        )]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Delete(int id)
        {
            if (id <= 0)
                return BadRequest("Restaurant id must be greater than zero");

            var deleted = _restaurantService.Delete(id);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
        #endregion
        private static string? ValidateRestaurantInput(string? name, string? cuisineType, string? city)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Name is required.";

            if (name.Trim().Length < 3)
                return "Name must have at least 3 characters.";

            if (string.IsNullOrWhiteSpace(cuisineType))
                return "Cuisine type is required.";

            if (cuisineType.Trim().Length < 3)
                return "Cuisine type must have at least 3 characters.";

            if (string.IsNullOrWhiteSpace(city))
                return "City is required.";

            if (city.Trim().Length < 3)
                return "City must have at least 3 characters.";

            return null;
        }
    }
}
