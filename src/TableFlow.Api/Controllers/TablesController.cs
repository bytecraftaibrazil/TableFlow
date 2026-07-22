using Microsoft.AspNetCore.Mvc;
using TableFlow.Api.DTOs;
using TableFlow.Api.Interfaces;

namespace TableFlow.Api.Controllers
{
    [ApiController]
    [Route("tables")]
    public class TablesController : ControllerBase
    {
        private readonly ITableService _tableService;
        public TablesController(ITableService tableService)
        {
            _tableService = tableService;
        }

        #region Get

        [HttpGet]
        [ProducesResponseType(
            typeof(IReadOnlyList<TableResponse>),
            StatusCodes.Status200OK
        )]
        public ActionResult<IReadOnlyList<TableResponse>> GetAll()
        {
            var tables = _tableService.GetAll();
            return Ok(tables);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(
            typeof(TableResponse),
            StatusCodes.Status200OK
        )]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status400BadRequest
        )]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status404NotFound
        )]
        public ActionResult<TableResponse> GetById(int id)
        {
            if (id <= 0)
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid table id",
                    detail: "Table id must be greater than zero."
                );

            var table = _tableService.GetById(id);

            if (table is null)
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Table not found",
                    detail: $"Table with id {id} was not found."
                );

            return Ok(table);
        }

        [HttpGet("restaurant/{restaurantId:int}")]
        [ProducesResponseType(
            typeof(IReadOnlyList<TableResponse>),
            StatusCodes.Status200OK
        )]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status400BadRequest
        )]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status404NotFound
        )]

        public ActionResult<IReadOnlyList<TableResponse>> GetByRestaurantId(int restaurantId)
        {
            if (restaurantId <= 0)
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid restaurant id",
                    detail: "Restaurant id must be greater than zero."
                );

            var tables = _tableService.GetByRestaurantId(restaurantId);

            if (tables.Count == 0)
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Tables not found",
                    detail: $"No tables were found for restaurant with id {restaurantId}."
                );

            return Ok(tables);
        }

        [HttpGet("active")]
        [ProducesResponseType(
            typeof(IReadOnlyList<TableResponse>),
            StatusCodes.Status200OK
        )]
        public ActionResult<IReadOnlyList<TableResponse>> GetActive()
        {
            var tables = _tableService.GetActive();

            return Ok(tables);
        }
        #endregion

        #region Post
        [HttpPost]
        [ProducesResponseType(
            typeof(TableResponse),
            StatusCodes.Status201Created
        )]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status400BadRequest
        )]
        public ActionResult<TableResponse> Create(CreateTableRequest request)
        {
            var validationError = ValidateTableInput(
                request.RestaurantId,
                request.Number,
                request.Capacity
            );

            if (validationError is not null)
                return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid table data",
                detail: validationError
            );

            var table = _tableService.Create(request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = table.Id },
                table
            );
        }

        #endregion

        #region Put
        [HttpPut("{id:int}")]
        [ProducesResponseType(
            typeof(TableResponse),
            StatusCodes.Status200OK
        )]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status400BadRequest
        )]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status404NotFound
        )]
        public ActionResult<TableResponse> Update(int id, UpdateTableRequest request)
        {
            if (id <= 0)
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid table id",
                    detail: "Table id must be greater than zero."
                );

            var validationError = ValidateTableInput(
                request.RestaurantId,
                request.Number,
                request.Capacity
            );

            if (validationError is not null)
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid table data",
                    detail: validationError
                );

            var table = _tableService.Update(id, request);

            if (table is null)
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Table not found",
                    detail: $"Table with id {id} was not found."
                );

            return Ok(table);
        }

        #endregion

        #region Delete
        [HttpDelete("{id:int}")]
        [ProducesResponseType(
            StatusCodes.Status204NoContent
        )]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status400BadRequest
        )]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status404NotFound
        )]
        public IActionResult Delete(int id)
        {
            if (id <= 0)
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid table id",
                    detail: "Table id must be greater than zero."
                );

            var deleted = _tableService.Delete(id);

            if (!deleted)
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Table not found",
                    detail: $"Table with id {id} was not found."
                );

            return NoContent();
        }

        #endregion

        private static string? ValidateTableInput(
            int restaurantId,
            int number,
            int capacity
        )
        {
            if (restaurantId <= 0)
                return "Restaurant id must be greater than zero.";

            if (number <= 0)
                return "Table number must be greater than zero.";

            if (capacity <= 0)
                return "Table capacity must be greater than zero.";

            return null;
        }
    }
}