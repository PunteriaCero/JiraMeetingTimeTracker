using JiraDriver;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace CallMSGraph.Filters
{
    public class JiraExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is JiraAuthException)
            {
                context.HttpContext.Response.Cookies.Delete("JiraToken");

                context.Result = new RedirectToRouteResult(new RouteValueDictionary
                {
                    { "controller", "Jira" },
                    { "action", "Index" },
                    { "message", context.Exception.Message }
                });
                
            };
        }
    }
}
