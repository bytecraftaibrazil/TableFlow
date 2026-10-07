using TableFlow.Api.Models;

namespace TableFlow.Api.Interfaces
{
    public interface IReservationEngine
    {
        Task<ReservationValidationResult> ValidateAsync(
            ReservationCandidate candidate, int? excludedReservationId = null);

        Task<SuggestedTable?> SuggestTableAsync(
            int restaurantId, int partySize);
        Task<ReservationAvailabilityResult> GetAvailabilityAsync(
            ReservationAvailabilityRequest request);
    }
}