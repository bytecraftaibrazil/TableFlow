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

        public ReservationResponse? Update(int id, UpdateReservationRequest request)
        {
            var reservationIndex = Reservations.FindIndex(r => r.Id == id);

            if (reservationIndex == -1)
                return null;

            var currentReservation = Reservations[reservationIndex];

            var updatedReservation = new ReservationResponse
            (
                currentReservation.Id,
                request.RestaurantId,
                request.TableId,
                request.CustomerName.Trim(),
                request.ReservationDate,
                request.PartySize,
                currentReservation.Status
            );

            Reservations[reservationIndex] = updatedReservation;

            return updatedReservation;
        }

        public ReservationResponse? Cancel(int id)
        {
            var reservationIndex = Reservations.FindIndex(r => r.Id == id);

            if (reservationIndex == -1)
                return null;

            var currentReservation = Reservations[reservationIndex];

            if (currentReservation.Status == "Cancelled")
                return currentReservation;

            var cancellReservation = currentReservation with
            {
                Status = "Cancelled"
            };

            Reservations[reservationIndex] = cancellReservation;

            return cancellReservation;
        }

        public ReservationResponse? Confirm(int id)
        {
            var reservationIndex = Reservations.FindIndex(r => r.Id == id);

            if (reservationIndex == -1)
                return null;

            var currentReservation = Reservations[reservationIndex];

            if (currentReservation.Status == "Confirmed")
                return currentReservation;

            var cancellReservation = currentReservation with
            {
                Status = "Confirmed"
            };

            Reservations[reservationIndex] = cancellReservation;

            return cancellReservation;
        }
    }
}