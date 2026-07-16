namespace TableFlow.Api.DTOs
{
    public record TableResponse(
    int Id,
    int RestaurantId,
    int Number,
    int Capacity,
    bool IsActive
    );
}