using System;

namespace CallMSGraph.Controllers
{
    public class JiraWorkUpdateModel
    {
        public string Action { get; set; }
        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
        public string Issue { get; set; }
        public string Subject { get; set; }
    }
}
