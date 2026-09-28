namespace TableFlow.Api.Models
{
    public record ReservationCandidate(
        int RestaurantId,
        int TableId,
        int PartySize,
        DateTime ReservationDateF
    );
}