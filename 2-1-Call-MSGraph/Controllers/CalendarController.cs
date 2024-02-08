using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CallMSGraph.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CalendarController : ControllerBase
    {
        private readonly GraphServiceClient _graphServiceClient;

        public CalendarController(
            GraphServiceClient graphServiceClient)
        {
            this._graphServiceClient = graphServiceClient;
        }

        [HttpGet]
        public async Task<IEnumerable<object>> Get()
        {
            var result = await _graphServiceClient.Me.CalendarView.GetAsync((requestConfiguration) =>
            {
                string[] select = { "subject", "organizer", "attendees", "start", "end" };
                requestConfiguration.QueryParameters.Select = select;
                requestConfiguration.QueryParameters.Filter = "startsWith(subject,'SCT310-')";
                requestConfiguration.QueryParameters.Top = 50;
                requestConfiguration.QueryParameters.StartDateTime = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-ddThh:mm:ss");
                requestConfiguration.QueryParameters.EndDateTime = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-ddThh:mm:ss");
                requestConfiguration.QueryParameters.Count = true;
            });


            var lista = result.Value.Select(s => new { s.Subject, s.Organizer.EmailAddress, atte = s.Attendees.Select(a => a.EmailAddress).ToList(), s.Start, s.End }).ToList();

            return lista;
        }

        // GET api/<ValuesController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<ValuesController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<ValuesController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<ValuesController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
