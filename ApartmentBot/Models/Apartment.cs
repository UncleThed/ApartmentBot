namespace ApartmentBot.Models
{
    public class Apartment
    {
        public int Id { get; set; }
        public string PhotoUrl { get; set; }
        public uint RoomsCount { get; set; }
        public string District { get; set; }
        public string Address { get; set; }
        public uint Cost { get; set; }
        public Client Client { get; set; }
    }
}
