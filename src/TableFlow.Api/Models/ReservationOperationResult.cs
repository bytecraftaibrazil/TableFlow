using TableFlow.Api.DTOs;

namespace TableFlow.Api.Models
{
    public enum ReservationOperationStatus
    {
        Success,
        ReservationNotFound,
        RestaurantNotFound,
        RestaurantInactive,
        TableNotFound,
        TableDoesNotBelongToRestaurant,
        TableInactive,
        InvalidStatusTransition,
        CancelledReservationCannotBeUpdated
    }

    public record ReservationOperationResult(
        ReservationOperationStatus Status,
        ReservationResponse? Reservation = null
    );
}