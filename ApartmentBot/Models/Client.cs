using System.Collections.Generic;

namespace ApartmentBot.Models
{
    public class Client
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string Phone { get; set; }

        public List<Apartment> Apartments { get; set; } = new List<Apartment>();
    }
}
