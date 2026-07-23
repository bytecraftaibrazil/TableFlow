using TableFlow.Api.DTOs;
using TableFlow.Api.Interfaces;

namespace TableFlow.Api.Services
{
    public class ReservationService : IReservationService
    {
        private static readonly List<ReservationResponse> Reservations = [
            new(
            1,
            1,
            1,
            "Ana Souza",
            new DateTime(2026, 7, 25, 19, 0, 0),
            4,
            "Confirmed"
            ),
            new(
                2,
                1,
                2,
                "Carlos Lima",
                new DateTime(2026, 7, 25, 20, 30, 0),
                2,
                "Pending"
            ),
            new(
                3,
                2,
                4,
                "Mariana Alves",
                new DateTime(2026, 7, 26, 18, 30, 0),
                4,
                "Confirmed"
            ),
            new(
                4,
                3,
                6,
                "Lucas Rocha",
                new DateTime(2026, 7, 27, 21, 0, 0),
                2,
                "Cancelled"
            ),
            new(
                5,
                2,
                5,
                "Fernanda Costa",
                new DateTime(2026, 7, 28, 20, 0, 0),
                6,
                "Pending"
            )
        ];

        public IReadOnlyList<ReservationResponse> GetAll()
        {
            return Reservations;
        }

        public ReservationResponse? GetById(int id)
        {
            return Reservations.FirstOrDefault(r => r.Id == id);
        }

        public IReadOnlyList<ReservationResponse> GetByRestaurantId(int restaurantId)
        {
            return Reservations.Where(r => r.RestaurantId == restaurantId).ToList();
        }

        public ReservationResponse Create(CreateReservationRequest request)
        {
            var nextId = Reservations.Count == 0 ? 1 : Reservations.Max(r => r.Id) + 1;

            var reservation = new ReservationResponse(
                nextId,
                request.RestaurantId,
                request.TableId,
                request.CustomerName.Trim(),
                request.ReservationDate,
                request.PartySize,
                "Peding"
            );

            Reservations.Add(reservation);

            return reservation;
        }
    }
}