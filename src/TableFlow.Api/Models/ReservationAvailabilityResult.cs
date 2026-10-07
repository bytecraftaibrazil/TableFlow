namespace TableFlow.Api.Models;

public record ReservationAvailabilityResult(
    int RestaurantId,
    int PartySize,
    DateTime ReservationDate,
    int DurationMinutes,
    IReadOnlyList<SuggestedTable> CandidateTables,
    IReadOnlyList<SuggestedTable> AvailableTables)
{
    public DateTime ReservationEnd => ReservationDate.AddMinutes(DurationMinutes);
}
