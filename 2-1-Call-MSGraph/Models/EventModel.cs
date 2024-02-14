using System;

namespace CallMSGraph.Models
{
    public class EventModel
    {
        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string Issue { set; get; }
    }
}
