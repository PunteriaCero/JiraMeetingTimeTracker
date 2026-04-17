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
            jiraUser.Url = Request.Cookies["JiraUrl"];

            return View(jiraUser);
        }

        // POST: JiraController/Login
        [HttpPost]
        public async Task<ActionResult> Login(JiraUser jiraUser)
        {
            Response.Cookies.Append("JiraUser", jiraUser.User);
            Response.Cookies.Append("JiraToken", jiraUser.Token);
            Response.Cookies.Append("JiraUrl", jiraUser.Url);

            await jira.Init(jiraUser.User, jiraUser.Token, jiraUser.Url);

            ViewBag.Name = jira.Name;
            return View();
        }
    }
}
