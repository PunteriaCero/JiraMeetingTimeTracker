using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace CallMSGraph.Models
{
    public class VisorCalendarioModel
    {
        
        public string Prefix { get; set; }

        public DateTime From { get; set; }

        public DateTime To { get; set; }

        public IList<SelectListItem> Projects { get; set; }

        public IList<CalendarioModel> Calendar { get; set; }
    }

}
