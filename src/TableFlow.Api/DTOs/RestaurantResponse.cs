namespace TableFlow.Api.DTOs
{
    public record RestaurantResponse(
        int Id,
        string Name,
        string CuisineType,
        string City,
        bool IsActive);
}
