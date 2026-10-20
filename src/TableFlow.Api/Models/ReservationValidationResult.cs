namespace TableFlow.Api.Models
{
    public enum ReservationValidationStatus
    {
        Success,
        ReservationInputInvalid,
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