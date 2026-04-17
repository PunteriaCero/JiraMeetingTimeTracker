using JiraDriver;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Threading.Tasks;

namespace CallMSGraph.Filters
{
    public class JiraAuthFilter : IAsyncAuthorizationFilter
    {
        private readonly IJiraServiceDecorator jiraServiceDecorator;

        public JiraAuthFilter(IJiraServiceDecorator jiraServiceDecorator)
        {
            this.jiraServiceDecorator = jiraServiceDecorator;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var cookies = context.HttpContext.Request.Cookies;
            var jiraToken = cookies["JiraToken"];
            var jiraUser = cookies["JiraUser"];
            var jiraUrl = cookies["JiraUrl"];

            if (jiraUser.IsNullOrEmpty() || jiraToken.IsNullOrEmpty() || jiraUrl.IsNullOrEmpty()) {
                context.Result = new RedirectToRouteResult(new RouteValueDictionary
                {
                    { "controller", "Jira" },
                    { "action", "Index" },
                });

                return;
            }

            await jiraServiceDecorator.Init(jiraUser, jiraToken, jiraUrl);
        }
    }
}
