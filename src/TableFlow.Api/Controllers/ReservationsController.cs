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
    }
}