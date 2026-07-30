namespace TableFlow.Api.Entities
{
    public class Reservation
    {
        public int Id { get; set; }

        public int RestaurantId { get; set; }

        public int TableId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public DateTime ReservationDate { get; set; }

        public int PartySize { get; set; }

        public string Status { get; set; } = "Pending";

        public Restaurant Restaurant { get; set; } = null!;

        public RestaurantTable Table { get; set; } = null!;
    }
}