using TableFlow.Api.DTOs;
using TableFlow.Api.Models;

namespace TableFlow.Api.Interfaces
{
    public interface IReservationService
    {
        Task<IReadOnlyList<ReservationResponse>> GetAllAsync();

        Task<ReservationResponse?> GetByIdAsync(int id);

        Task<IReadOnlyList<ReservationResponse>> GetByRestaurantIdAsync(int restaurantId);

        Task<IReadOnlyList<ReservationResponse>> GetByTableIdAsync(int tableId);

        Task<IReadOnlyList<ReservationResponse>> GetFutureReservationsAsync();

        Task<ReservationOperationResult> CreateAsync(CreateReservationRequest request);

        Task<ReservationResponse?> UpdateAsync(int id, UpdateReservationRequest request);

        Task<ReservationResponse?> CancelAsync(int id);

        Task<ReservationResponse?> ConfirmAsync(int id);

        Task<IReadOnlyList<ReservationResponse>> GetByStatusAsync(string status);
    }
}