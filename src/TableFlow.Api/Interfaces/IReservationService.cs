using TableFlow.Api.DTOs;

namespace TableFlow.Api.Interfaces
{
    public interface IReservationService
    {
        IReadOnlyList<ReservationResponse> GetAll();

        ReservationResponse? GetById(int id);

        IReadOnlyList<ReservationResponse> GetByRestaurantId(int restaurantId);
    }
}