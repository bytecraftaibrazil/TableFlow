namespace TableFlow.Api.DTOs
{
    public record UpdateReservationRequest
    (
        int RestaurantId,
        int TableId,
        string CustomerName,
        DateTime ReservationDate,
        int PartySize
    );
}