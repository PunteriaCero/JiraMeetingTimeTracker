using System;

namespace CallMSGraph.Controllers
{
    public class JiraWorkUpdateModel
    {
        public string Action { get; set; }
        public long StartDate { get; set; }

        public long EndDate { get; set; }
        public string Issue { get; set; }
        public string Subject { get; set; }
    }
}
