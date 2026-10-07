namespace TableFlow.Api.DTOs;

public record ReservationAvailabilityResponse(
    int RestaurantId,
    int PartySize,
    DateTime ReservationDate,
    int DurationMinutes,
    DateTime ReservationEnd,
    IReadOnlyList<SuggestedTableResponse> CandidateTables,
    IReadOnlyList<SuggestedTableResponse> AvailableTables);
