using CallMSGraph.Models;
using JiraDriver;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CallMSGraph.Controllers
{
    public class JiraController : Controller
    {
        private readonly IJiraServiceDecorator jira;

        public JiraController(IJiraServiceDecorator jira)
        {
            this.jira = jira;
        }

        // GET: JiraController
        public ActionResult Index(JiraUser jiraUser)
        {
            jiraUser.User = Request.Cookies["JiraUser"];
            jiraUser.Token = Request.Cookies["JiraToken"];

            return View(jiraUser);
        }

        // GET: JiraController/Create
        [HttpPost]
        public async Task<ActionResult> Login(JiraUser jiraUser)
        {
            Response.Cookies.Append("JiraUser", jiraUser.User);

            await jira.Init(jiraUser.User, jiraUser.Token);
            
            Response.Cookies.Append("JiraToken", jiraUser.Token);

            ViewBag.Name = jira.Name;
            return View();
        }
    }
}
