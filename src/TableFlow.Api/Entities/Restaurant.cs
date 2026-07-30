namespace TableFlow.Api.Entities
{
    public class Restaurant
    {
        public int Id {get; set;}

        public string Name {get; set;} = string.Empty;

        public string CuisineType { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;
        
    public bool IsActive { get; set; }

    public ICollection<RestaurantTable> Tables { get; set; }
        = new List<RestaurantTable>();

    public ICollection<Reservation> Reservations { get; set; }
        = new List<Reservation>();

    }
}