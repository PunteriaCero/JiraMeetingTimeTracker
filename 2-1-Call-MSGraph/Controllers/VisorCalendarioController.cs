using CallMSGraph.Filters;
using CallMSGraph.Models;
using JiraDriver;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using NuGet.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CallMSGraph.Controllers
{
    public class VisorCalendarioController : Controller
    {
        private const string ACTION_REMOVE = "REMOVE";
        private readonly GraphServiceClient _graphServiceClient;
        private readonly IJiraServiceDecorator class1;

        public VisorCalendarioController(GraphServiceClient graphServiceClient, IJiraServiceDecorator class1)
        {
            this._graphServiceClient = graphServiceClient;
            this.class1 = class1;
        }

        // GET: VisorCalendario/Details/5
        [TypeFilter(typeof(JiraAuthFilter))]
        [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
        public async Task<ActionResult> Index(VisorCalendarioModel model, CancellationToken token = default)
        {
            var projects = await class1.GetProjects(token);
            model.Projects = projects.Select(s => new SelectListItem(s.Value, s.Key)).ToArray();

            if (model.Prefix.IsNullOrEmpty()) 
            {
                model.From = DateTime.UtcNow;
                model.To = DateTime.UtcNow.AddDays(7);
                model.Prefix = string.Empty;
                return View(model);
            }

            var lista = GetEventsWithJira(model.Prefix, model.From, model.To);
            model.Calendar = lista.ToBlockingEnumerable(token).ToArray();
             
            return View(model);
        }

        [TypeFilter(typeof(JiraAuthFilter))]
        [HttpPost]
        public async Task<JsonResult> Create(JiraWorkUpdateModel model)
        {
            var startDate = new DateTime(model.StartDate, DateTimeKind.Utc);
            var time = TimeString(startDate, new DateTime(model.EndDate, DateTimeKind.Utc));
            var work = await class1.RegisterWork(model.Issue, time, startDate, model.Subject);
            return Json(work);
        }

        [TypeFilter(typeof(JiraAuthFilter))]
        [HttpPost]
        public async Task<JsonResult> Delete(JiraWorkUpdateModel model)
        {
            var startDate = new DateTime(model.StartDate, DateTimeKind.Utc);
            await class1.DeleteWork(model.Issue, startDate);
            return Json(new {});
        }

        private string TimeString(DateTime startDate, DateTime endDate)
        {
            var timeSpan = endDate - startDate;
            var cultureFr = new System.Globalization.CultureInfo("fr-fr");
            var hours = CustomRound(timeSpan.TotalHours).ToString(cultureFr);
            var result = $"{hours}h";
            return result;
        }

        static double CustomRound(double value)
        {
            double rounded = Math.Round(value, MidpointRounding.ToZero);
            double diff = value - rounded;

            if (diff > 0.5)
            {
                return rounded + 1;
            }
            else if (diff > 0)
            {
                return rounded + 0.5;
            } 
            else
            {
                return rounded;
            }
            
        }

        public async Task<JiraWorkLog?> GetWork(string key, DateTime startDate, CancellationToken token = default)
        {
            return await class1.GetWork(key, startDate, token);
        }

        public async IAsyncEnumerable<CalendarioModel> GetEventsWithJira(string prefix, DateTime from, DateTime to)
        {
            var events = await GetEvents(prefix, from, to);
            foreach (var evento in events)
            {
                var work = await GetWork(evento.Issue, evento.StartDate);
                evento.JiraWorkLog = work;
                yield return evento;
            }
        }

        private string ParseIssue(string prefix, string subject)
        {
            var pattern = prefix + "-[0-9]+";

            var match = Regex.Match(subject, pattern);
            return match.Value;
        }


        public async Task<IEnumerable<CalendarioModel>> GetEvents(string prefix, DateTime from, DateTime to, CancellationToken token = default)
        {
            var result = await _graphServiceClient.Me.CalendarView.GetAsync((requestConfiguration) =>
            {
                string[] select = { "subject", "organizer", "attendees", "start", "end" };
                requestConfiguration.QueryParameters.Select = select;
                //requestConfiguration.QueryParameters.Filter = "contains(subject,'" + prefix + "')";
                requestConfiguration.QueryParameters.Top = 200;
                requestConfiguration.QueryParameters.StartDateTime = from.ToString("yyyy-MM-ddThh:mm:ss");
                requestConfiguration.QueryParameters.EndDateTime = to.ToString("yyyy-MM-ddThh:mm:ss");
                //requestConfiguration.QueryParameters.Count = true;
            });


            var lista = result.Value.Select(s => new { s.Subject, s.Organizer.EmailAddress, atte = s.Attendees.Select(a => a.EmailAddress).ToList(), s.Start, s.End });
            var model = lista.Select(s =>
            new CalendarioModel()
            {
                Subject = s.Subject,
                StartDate = s.Start.ToDateTime().ToUniversalTime(),
                EndDate = s.End.ToDateTime().ToUniversalTime(),
                Issue = ParseIssue(prefix, s.Subject)
            });
            model = model.Where(s => !s.Issue.IsNullOrEmpty());
            model = model.OrderBy(x => x.StartDate);
            return model;
        }

    }
}
