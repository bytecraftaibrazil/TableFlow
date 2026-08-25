namespace TableFlow.Api.DTOs
{
    public record ReservationFilterRequest(
        string? Status,
        int? RestaurantId,
        int? TableId,
        DateTime? FromDate,
        DateTime? ToDate,
        int? MinimumPartySize,
        bool Descending = false,
        int PageNumber = 1,
        int PageSize = 20
    );
}