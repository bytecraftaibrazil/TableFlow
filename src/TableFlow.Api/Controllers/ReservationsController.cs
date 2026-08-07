using Microsoft.AspNetCore.Mvc;
using TableFlow.Api.DTOs;
using TableFlow.Api.Interfaces;
using TableFlow.Api.Models;

namespace TableFlow.Api.Controllers
{
    [ApiController]
    [Route("reservations")]
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationService _reservationService;

        public ReservationsController(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        #region Get
        [HttpGet]
        [ProducesResponseType(
            typeof(IReadOnlyList<ReservationResponse>),
            StatusCodes.Status200OK
        )]

        public async Task<ActionResult<IReadOnlyList<ReservationResponse>>> GetAll()
        {
            var reservations = await _reservationService.GetAllAsync();

            return Ok(reservations);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(
            typeof(ReservationResponse),
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

        public async Task<ActionResult<ReservationResponse>> GetById(int id)
        {
            if (id <= 0)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid reservation id",
                    detail: "Reservation id must be greater than zero."
                );
            }

            var reservation = await _reservationService.GetByIdAsync(id);

            if (reservation is null)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Reservation not found",
                    detail: $"Reservation with id {id} was not found."
                );
            }

            return Ok(reservation);
        }

        [HttpGet("restaurant/{restaurantId:int}")]
        [ProducesResponseType(
            typeof(IReadOnlyList<ReservationResponse>),
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
        public async Task<ActionResult<IReadOnlyList<ReservationResponse>>> GetByRestaurantId(int restaurantId)
        {
            if (restaurantId <= 0)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid restaurant id",
                    detail: "Restaurant id must be greater than zero."
                );
            }

            var reservations = await _reservationService.GetByRestaurantIdAsync(restaurantId);

            if (reservations is null)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Reservations not found",
                    detail:
                        $"No reservations were found for restaurant with id {restaurantId}."
                );
            }

            return Ok(reservations);
        }

        [HttpGet("table/{tableId:int}")]
        [ProducesResponseType(
            typeof(IReadOnlyList<ReservationResponse>),
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
        public async Task<ActionResult<IReadOnlyList<ReservationResponse>>> GetByTableId(int tableId)
        {
            if (tableId <= 0)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid table id",
                    detail: "Table id must be greater than zero."
                );
            }

            var reservations = await _reservationService.GetByTableIdAsync(tableId);

            if (reservations is null)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Reservations not found",
                    detail: $"No reservations were found for table with id {tableId}."
                );
            }

            return Ok(reservations);
        }

        [HttpGet("status")]
        [HttpGet("status/{status}")]
        [ProducesResponseType(
            typeof(IReadOnlyList<ReservationResponse>),
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
        public async Task<ActionResult<IReadOnlyList<ReservationResponse>>> GetByStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid status filter",
                    detail: "Status type is required."
                );

            var reservations = await _reservationService.GetByStatusAsync(status);

            if (reservations is null)
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Reservations not found",
                    detail: $"No reservations were found with status '{status}'."
                );

            return Ok(reservations);
        }

        [HttpGet("upcoming")]
        [ProducesResponseType(
            typeof(IReadOnlyList<ReservationResponse>),
            StatusCodes.Status200OK
        )]
        public async Task<ActionResult<IReadOnlyList<ReservationResponse>>> GetFutureReservationsAsync()
        {
            var reservations = await _reservationService.GetFutureReservationsAsync();

            return Ok(reservations);
        }

        #endregion

        #region Post
        [HttpPost]
        [ProducesResponseType(
            typeof(ReservationResponse),
            StatusCodes.Status201Created
        )]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status400BadRequest
        )]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status404NotFound
        )]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status409Conflict
        )]
        public async Task<ActionResult<ReservationResponse>> Create(CreateReservationRequest request)
        {
            var validationError = ValidateReservationInput(
                request.RestaurantId,
                request.TableId,
                request.CustomerName,
                request.ReservationDate,
                request.PartySize
            );

            if (validationError is not null)
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid reservation data",
                    detail: validationError
                );

            var result = await _reservationService.CreateAsync(request);

            if (result.Status == ReservationOperationStatus.RestaurantNotFound)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Restaurant not found",
                    detail: $"Restaurant with id "
                        + $"{request.RestaurantId} "
                        + "was not found."
                );
            }

            if (result.Status == ReservationOperationStatus.TableNotFound)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Table not found",
                    detail: $"Table with id "
                        + $"{request.TableId} "
                        + "was not found."
                );
            }

            if (result.Status == ReservationOperationStatus.TableDoesNotBelongToRestaurant)
            {
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Invalid table relationship",
                    detail: $"Table with id "
                        + $"{request.TableId} "
                        + "does not belong to restaurant "
                        + $"{request.RestaurantId}."
                );
            }

            var reservation = result.Reservation!;

            return CreatedAtAction(
                nameof(GetById),
                new { id = reservation.Id },
                reservation
            );
        }
        #endregion

        #region Put
        [HttpPut("{id:int}")]
        [ProducesResponseType(
            typeof(ReservationResponse),
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
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status409Conflict
        )]
        public async Task<ActionResult<ReservationResponse>> Update(int id, UpdateReservationRequest request)
        {
            if (id <= 0)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid reservation id",
                    detail: "Reservation id must be greater than zero."
                );
            }

            var validationError = ValidateReservationInput(
                request.RestaurantId,
                request.TableId,
                request.CustomerName,
                request.ReservationDate,
                request.PartySize
            );

            if (validationError is not null)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid reservation data",
                    detail: validationError
                );
            }

            var result = await _reservationService.UpdateAsync(id, request);

            if (result.Status == ReservationOperationStatus.ReservationNotFound)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Reservation not found",
                    detail: $"Reservation with id {id} was not found."
                );
            }

            if (result.Status == ReservationOperationStatus.RestaurantNotFound)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Restaurant not found",
                    detail: $"Restaurant with id {request.RestaurantId} was not found."
                );
            }

            if (result.Status == ReservationOperationStatus.TableNotFound)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Table not found",
                    detail: $"Table with id {request.TableId} was not found."
                );
            }

            if (result.Status == ReservationOperationStatus.CancelledReservationCannotBeUpdated)
            {
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Invalid Operation",
                    detail: "A cancelled reservation cannot be updated."
                );
            }

            if (result.Status == ReservationOperationStatus.TableDoesNotBelongToRestaurant)
            {
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Invalid table relationship",
                    detail:
                        $"Table with id {request.TableId} does not belong "
                        + $"to restaurant {request.RestaurantId}."
                );
            }

            return Ok(result.Reservation);
        }

        [HttpPut("{id:int}/cancel")]
        [ProducesResponseType(
            typeof(ReservationResponse),
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
        public async Task<ActionResult<ReservationResponse>> Cancel(int id)
        {
            if (id <= 0)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid reservation id",
                    detail: "Reservation id must be greater than zero."
                );
            }

            var result = await _reservationService.CancelAsync(id);

            if (result.Status == ReservationOperationStatus.ReservationNotFound)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Reservation not found",
                    detail: $"Reservation with id {id} was not found."
                );
            }

            return Ok(result.Reservation);
        }

        [HttpPut("{id:int}/confirm")]
        [ProducesResponseType(
            typeof(ReservationResponse),
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
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status409Conflict
        )]
        public async Task<ActionResult<ReservationResponse>> Confim(int id)
        {
            if (id <= 0)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid reservation id",
                    detail: "Reservation id must be greater than zero."
                );
            }

            var result = await _reservationService.ConfirmAsync(id);

            if (result.Status == ReservationOperationStatus.ReservationNotFound)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Reservation not found",
                    detail: $"Reservation with id {id} was not found."
                );
            }

            if (result.Status == ReservationOperationStatus.InvalidStatusTransition)
            {
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Invalid reservation status transition",
                    detail: "A cancelled reservation cannot be confirmed."
                );
            }

            return Ok(result.Reservation);
        }
        #endregion

        private static string? ValidateReservationInput(
            int restaurantId,
            int tableId,
            string? customerName,
            DateTime reservationDate,
            int partySize
        )
        {
            if (restaurantId <= 0)
                return "Restaurant id must be greater than zero.";

            if (tableId <= 0)
                return "Table id must be greater than zero.";

            if (string.IsNullOrWhiteSpace(customerName))
                return "Customer name is required.";

            if (customerName.Trim().Length < 3)
                return "Customer name must have at least 3 characters.";

            if (reservationDate <= DateTime.Now)
                return "Reservation date must be in the future.";

            if (partySize <= 0)
                return "Party size must be greater than zero.";

            return null;
        }
    }
}