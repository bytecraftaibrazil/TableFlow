namespace TableFlow.Api.DTOs
{
    public record CreateTableRequest
    (
        int RestaurantId,
        int Number,
        int Capacity,
        bool IsActive
    );
}