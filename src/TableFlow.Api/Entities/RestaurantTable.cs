namespace TableFlow.Api.Entities
{
    public class RestaurantTable
    {
        public int Id { get; set; }

        public int RestaurantId { get; set; }

        public int Number { get; set; }

        public int Capacity { get; set; }

        public bool IsActive { get; set; }

        public Restaurant Restaurant { get; set; }  = null!;

        public ICollection<Reservation> Reservations{ get; set; }
            = new List<Reservation>();
    }
}