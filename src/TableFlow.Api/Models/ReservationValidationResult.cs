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
        ReservationDurationInvalid,
        ReservationConflict
    }

    public record ReservationValidationResult(
        ReservationValidationStatus Status)
    {
        public bool IsSuccess =>
            Status == ReservationValidationStatus.Success;
    }
}