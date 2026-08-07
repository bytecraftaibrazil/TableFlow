using TableFlow.Api.DTOs;

namespace TableFlow.Api.Models
{
    public enum ReservationOperationStatus
    {
        Success,
        ReservationNotFound,
        RestaurantNotFound,
        TableNotFound,
        TableDoesNotBelongToRestaurant,
        InvalidStatusTransition,
        CancelledReservationCannotBeUpdated
    }

    public record ReservationOperationResult(
        ReservationOperationStatus Status,
        ReservationResponse? Reservation = null
    );
}