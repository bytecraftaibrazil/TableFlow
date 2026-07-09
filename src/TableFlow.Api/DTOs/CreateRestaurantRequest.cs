namespace TableFlow.Api.DTOs
{
    public record CreateRestaurantRequest
    (
        string? Name,
        string? CuisineType,
        string? City,
        bool IsActive
    );
}