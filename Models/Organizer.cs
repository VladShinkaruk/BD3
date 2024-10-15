using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebCityEvents.Models
{
    public class Organizer
    {
        public int OrganizerID { get; set; }
        public string FullName { get; set; }
        public string Post { get; set; }
        public ICollection<Event> Events { get; set; }

        public Organizer()
        {
            Events = new List<Event>();
        }
    }
}