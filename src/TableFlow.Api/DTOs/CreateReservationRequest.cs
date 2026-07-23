namespace TableFlow.Api.DTOs
{
    public record CreateReservationRequest
    (
        int RestaurantId,
        int TableId,
        string CustomerName,
        DateTime ReservationDate,
        int PartySize
    );
}