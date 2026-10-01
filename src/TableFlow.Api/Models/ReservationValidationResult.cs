namespace TableFlow.Api.Models
{
    public enum ReservationValidationStatus
    {
        Success,
        RestaurantNotFound,
        RestaurantInactive,
        TableNotFound,
        TableDoesNotBelongToRestaurant,
        TableInactive,
        PartySizeExceedsTableCapacity,
        ReservationDurationInvalid
    }

    public record ReservationValidationResult(
        ReservationValidationStatus Status)
    {
        public bool IsSuccess =>
            Status == ReservationValidationStatus.Success;
    }
}