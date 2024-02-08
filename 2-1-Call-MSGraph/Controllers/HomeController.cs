using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using WebApp_OpenIDConnect_DotNet_graph.Models;
using Microsoft.Graph.Me.CalendarView;
using System.Linq;
using Microsoft.Extensions.Hosting.Internal;

namespace WebApp_OpenIDConnect_DotNet_graph.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly GraphServiceClient _graphServiceClient;

        private readonly MicrosoftIdentityConsentAndConditionalAccessHandler _consentHandler;

        private readonly string[] _graphScopes;

        public HomeController(ILogger<HomeController> logger,
                            IConfiguration configuration,
                            GraphServiceClient graphServiceClient,
                            MicrosoftIdentityConsentAndConditionalAccessHandler consentHandler)
        {
            _logger = logger;
            _graphServiceClient = graphServiceClient;
            _consentHandler = consentHandler;

            // Capture the Scopes for Graph that were used in the original request for an Access token (AT) for MS Graph as
            // they'd be needed again when requesting a fresh AT for Graph during claims challenge processing
            _graphScopes = configuration.GetValue<string>("DownstreamApi:Scopes")?.Split(' ');
        }

        [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
        public IActionResult Index()
        {
            return View();
        }

        [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
        public async Task<IActionResult> Profile()
        {
            User currentUser = null;

            try
            {
                currentUser = await _graphServiceClient.Me.GetAsync();
            }
            // Catch CAE exception from Graph SDK
            catch (ServiceException svcex) when (svcex.Message.Contains("Continuous access evaluation resulted in claims challenge"))
            {
                try
                {
                    Console.WriteLine($"{svcex}");
                    string claimChallenge = WwwAuthenticateParameters.GetClaimChallengeFromResponseHeaders(svcex.ResponseHeaders);
                    _consentHandler.ChallengeUser(_graphScopes, claimChallenge);
                    return new EmptyResult();
                }
                catch (Exception ex2)
                {
                    _consentHandler.HandleException(ex2);
                }
            }

            try
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

                // Get user photo
                using (var photoStream = await _graphServiceClient.Me.Photo.Content.GetAsync())
                {
                    byte[] photoByte = ((MemoryStream)photoStream).ToArray();
                    ViewData["Photo"] = Convert.ToBase64String(photoByte);
                }
            }
            catch (Exception pex)
            {
                Console.WriteLine($"{pex.Message}");
                ViewData["Photo"] = null;

                var t = new Task();
                
            }

            ViewData["Me"] = currentUser;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private string GetSecretFromKeyVault()
        {
            string uri = Environment.GetEnvironmentVariable("KEY_VAULT_URI");
            SecretClient client = new SecretClient(new Uri(uri), new DefaultAzureCredential());

            Response<KeyVaultSecret> secret = client.GetSecretAsync("Graph-App-Secret").Result;

            return secret.Value.Value;
        }
    }
}