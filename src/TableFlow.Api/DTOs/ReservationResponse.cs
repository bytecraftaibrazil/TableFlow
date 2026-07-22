namespace TableFlow.Api.DTOs
{
    public record ReservationResponse(
        int Id,
        int RestaurantId,
        int TableId,
        string CustomeName,
        DateTime ReservationDate,
        int PartySize,
        string Status
    );
}