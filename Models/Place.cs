using System.Collections.Generic;

namespace WebCityEvents.Models
{
    public class Place
    {
        public int PlaceID { get; set; }
        public string PlaceName { get; set; }
        public string Geolocation { get; set; }
        public ICollection<Event> Events { get; set; }

        public Place()
        {
            Events = new List<Event>();
        }
    }
}