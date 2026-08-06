using TableFlow.Api.DTOs;

namespace TableFlow.Api.Models
{
    public enum ReservationOperationStatus
    {
        Success,
        RestaurantNotFound,
        TableNotFound,
        TableDoesNotBelongToRestaurant
    }

    public record ReservationOperationResult(
        ReservationOperationStatus Status,
        ReservationResponse? Reservation = null
    );
}