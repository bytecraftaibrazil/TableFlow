namespace TableFlow.Api.Models
{
    public enum ReservationValidationStatus
    {
        Success,
        RestaurantNotFound,
        TableNotFound,
        TableDoesNotBelongToRestaurant
    }

    public record ReservationValidationResult(
        ReservationValidationStatus Status)
    {
        public bool IsSuccess =>
            Status == ReservationValidationStatus.Success;
    }
}