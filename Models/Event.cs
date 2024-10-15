using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebCityEvents.Models
{
    public class Event
    {
        public int EventID { get; set; }
        public string EventName { get; set; }
        public int PlaceID { get; set; }
        public DateTime EventDate { get; set; }
        public float TicketPrice { get; set; }
        public int TicketAmount { get; set; }
        public int OrganizerID { get; set; }
        public Place Place { get; set; }
        public Organizer Organizer { get; set; }
        public ICollection<TicketOrder> TicketOrders { get; set; }

        public Event()
        {
            TicketOrders = new List<TicketOrder>();
        }
    }
}