using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebCityEvents.Models
{
    public class TicketOrder
    {
        public int OrderID { get; set; }
        public int EventID { get; set; }
        public int CustomerID { get; set; }
        public DateTime OrderDate { get; set; }
        public int TicketCount { get; set; }
        public Event Event { get; set; }
        public Customer Customer { get; set; }
    }
}