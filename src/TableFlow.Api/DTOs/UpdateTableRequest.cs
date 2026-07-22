namespace TableFlow.Api.DTOs
{
    public record UpdateTableRequest
    (
        int RestaurantId,
        int Number,
        int Capacity,
        bool IsActive
    );
}