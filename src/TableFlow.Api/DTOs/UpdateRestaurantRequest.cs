namespace TableFlow.Api.DTOs
{
    public record UpdateRestaurantRequest(
        string? Name,
        string? CuisineType,
        string? City,
        bool IsActive
    );
}