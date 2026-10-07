namespace TableFlow.Api.Models;

public record ReservationAvailabilityRequest(
    int RestaurantId,
    int PartySize,
    DateTime ReservationDate,
    int DurationMinutes)
{
    public DateTime ReservationEnd => ReservationDate.AddMinutes(DurationMinutes);
}
