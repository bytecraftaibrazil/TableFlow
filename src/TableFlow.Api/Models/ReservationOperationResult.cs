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
        PartySizeExceedsTableCapacity,
        InvalidStatusTransition,
        CancelledReservationCannotBeUpdated,
        ReservationDurationInvalid
    }

    public record ReservationOperationResult(
        ReservationOperationStatus Status,
        ReservationResponse? Reservation = null
    );
}