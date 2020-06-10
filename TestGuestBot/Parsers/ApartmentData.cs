using System.Collections.Generic;

namespace TestGuestBot.Parsers
{
    public class ApartmentData
    {
        public string PhotoUrl { get; }

        public uint RoomsCount { get; }

        public string District { get; }

        public string Address { get; }

        public uint Cost { get; }

        //public string BerthCount { get; }

        //public List<string> Equipment { get; }

        public ApartmentData(string photoUrl, uint roomsCount, string district, string address, uint cost)
        {
            PhotoUrl = photoUrl;
            RoomsCount = roomsCount;
            District = district;
            Address = address;
            Cost = cost;
            //BerthCount = berthCount;
            //Equipment = equipment;
        }
    }
}
