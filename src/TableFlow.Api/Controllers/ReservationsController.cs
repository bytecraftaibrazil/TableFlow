using Microsoft.AspNetCore.Mvc;
using TableFlow.Api.DTOs;
using TableFlow.Api.Interfaces;

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
        public ActionResult<IReadOnlyList<ReservationResponse>> GetAll()
        {
            var reservations = _reservationService.GetAll();

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
        public ActionResult<ReservationResponse> GetById(int id)
        {
            if (id <= 0)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid reservation id",
                    detail: "Reservation id must be greater than zero."
                );
            }

            var reservation = _reservationService.GetById(id);

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
        public ActionResult<IReadOnlyList<ReservationResponse>>
        GetByRestaurantId(int restaurantId)
        {
            if (restaurantId <= 0)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid restaurant id",
                    detail: "Restaurant id must be greater than zero."
                );
            }

            var reservations = _reservationService.GetByRestaurantId(restaurantId);

            if (reservations.Count == 0)
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
        public ActionResult<ReservationResponse> Create(CreateReservationRequest request)
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

            var reservation = _reservationService.Create(request);

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
        public ActionResult<ReservationResponse> Update(
            int id,
            UpdateReservationRequest request
        )
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

            var reservation = _reservationService.Update(id, request);

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
        public ActionResult<ReservationResponse> Cancel(int id)
        {
            if (id <= 0)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid reservation id",
                    detail: "Reservation id must be greater than zero."
                );
            }

            var reservation = _reservationService.Cancel(id);

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
        public ActionResult<ReservationResponse> Confim(int id)
        {
            if (id <= 0)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid reservation id",
                    detail: "Reservation id must be greater than zero."
                );
            }

            var reservation = _reservationService.Confirm(id);

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