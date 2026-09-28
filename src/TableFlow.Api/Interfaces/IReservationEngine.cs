using TableFlow.Api.Models;

namespace TableFlow.Api.Interfaces
{
    public interface IReservationEngine
    {
        Task<ReservationValidationResult> ValidateAsync(
            ReservationCandidate candidate);
    }
}