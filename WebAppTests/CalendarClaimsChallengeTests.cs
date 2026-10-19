using System.Net;
using System.Reflection;
using System.Text;
using CallMSGraph.Controllers;
using CallMSGraph.Models;
using JiraDriver;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Kiota.Abstractions.Authentication;
using NUnit.Framework;

namespace WebAppTests;

public class CalendarClaimsChallengeTests
{
    private const string Claims = "{\"access_token\":{\"acrs\":{\"essential\":true,\"value\":\"test\"}}}";

    [Test]
    public async Task CalendarUnauthorizedWithClaimsRequiresInteractiveAuthentication()
    {
        using var handler = new GraphChallengeHandler(Claims);
        using var client = new HttpClient(handler);
        var graph = new GraphServiceClient(client, new AnonymousAuthenticationProvider());
        var jira = DispatchProxy.Create<IJiraServiceDecorator, JiraProjectsProxy>();
        var controller = new VisorCalendarioController(graph, jira)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Path = "/VisorCalendario";
        controller.Request.QueryString = new QueryString("?Prefix=TEST&From=2026-10-19&To=2026-10-20");

        var result = await controller.Index(new VisorCalendarioModel
        {
            Prefix = "TEST", From = DateTime.UtcNow, To = DateTime.UtcNow.AddDays(1)
        });

        Assert.That(result, Is.TypeOf<ChallengeResult>());
        var challenge = (ChallengeResult)result;
        Assert.That(challenge.AuthenticationSchemes, Does.Contain(OpenIdConnectDefaults.AuthenticationScheme));
        Assert.That(challenge.Properties!.Items["claims"], Is.EqualTo(Claims));
        Assert.That(challenge.Properties.GetParameter<List<string>>("scope"), Does.Contain("Calendars.Read"));
        Assert.That(challenge.Properties.RedirectUri, Is.EqualTo(
            "/VisorCalendario?Prefix=TEST&From=2026-10-19&To=2026-10-20&calendarClaimsChallenge=1"));
    }

    [Test]
    public async Task RepeatedClaimsChallengeReturnsForbiddenInsteadOfRedirectingAgain()
    {
        using var handler = new GraphChallengeHandler(Claims);
        using var client = new HttpClient(handler);
        var controller = CreateController(client, "?Prefix=TEST&calendarClaimsChallenge=1");

        var result = await controller.Index(CreateModel());

        Assert.That(result, Is.TypeOf<ContentResult>());
        var content = (ContentResult)result;
        Assert.That(content.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
        Assert.That(content.Content, Does.Not.Contain(Claims));
    }

    [TestCase(null, HttpStatusCode.Unauthorized)]
    [TestCase("not-base64!", HttpStatusCode.Unauthorized)]
    [TestCase(null, HttpStatusCode.Forbidden)]
    public void ResponsesWithoutUsableClaimsKeepTheOriginalGraphError(string? rawHeader, HttpStatusCode status)
    {
        using var handler = new GraphChallengeHandler(null, status, rawHeader);
        using var client = new HttpClient(handler);
        var controller = CreateController(client);

        var exception = Assert.ThrowsAsync<Microsoft.Graph.Models.ODataErrors.ODataError>(async () =>
            await controller.Index(CreateModel()));

        Assert.That(exception!.ResponseStatusCode, Is.EqualTo((int)status));
    }

    [Test]
    public async Task SuccessfulCalendarRequestReturnsTheView()
    {
        using var handler = new GraphChallengeHandler(null, HttpStatusCode.OK);
        using var client = new HttpClient(handler);
        var controller = CreateController(client);

        var result = await controller.Index(CreateModel());

        Assert.That(result, Is.TypeOf<ViewResult>());
        Assert.That(((VisorCalendarioModel)((ViewResult)result).Model!).Calendar, Is.Empty);
    }

    [Test]
    public async Task MicrosoftIdentityWebSendsClaimsInTheAuthorizationRequest()
    {
        using var handler = new GraphChallengeHandler(Claims);
        using var client = new HttpClient(handler);
        var controller = CreateController(client);
        var challenge = (ChallengeResult)await controller.Index(CreateModel());
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
            ["AzureAd:TenantId"] = "0e2eb8d8-b8c9-4ee3-97e0-07f405398b85",
            ["AzureAd:ClientId"] = "test-client",
            ["AzureAd:ClientSecret"] = "test-only-not-a-real-secret"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApp(configuration.GetSection("AzureAd"));
        services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.Configuration = new OpenIdConnectConfiguration
            {
                AuthorizationEndpoint = "https://login.example.test/authorize",
                TokenEndpoint = "https://login.example.test/token",
                Issuer = "https://login.example.test/"
            };
        });
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("calendar.example.test");

        await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, challenge.Properties);

        var redirect = new Uri(context.Response.Headers.Location.ToString());
        var parameters = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(redirect.Query);
        Assert.That(redirect.Host, Is.EqualTo("login.example.test"));
        Assert.That(parameters["claims"].ToString(), Is.EqualTo(Claims));
        Assert.That(parameters["scope"].ToString().Split(' '), Does.Contain("Calendars.Read"));
    }

    private static VisorCalendarioController CreateController(HttpClient client, string query = "?Prefix=TEST")
    {
        var graph = new GraphServiceClient(client, new AnonymousAuthenticationProvider());
        var jira = DispatchProxy.Create<IJiraServiceDecorator, JiraProjectsProxy>();
        var controller = new VisorCalendarioController(graph, jira)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Path = "/VisorCalendario";
        controller.Request.QueryString = new QueryString(query);
        return controller;
    }

    private static VisorCalendarioModel CreateModel() => new()
    {
        Prefix = "TEST", From = DateTime.UtcNow, To = DateTime.UtcNow.AddDays(1)
    };

    public class JiraProjectsProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IJiraServiceDecorator.GetProjects))
            {
                return Task.FromResult<IDictionary<string, string>>(new Dictionary<string, string>());
            }

            throw new NotSupportedException(targetMethod?.Name);
        }
    }

    private sealed class GraphChallengeHandler(string? claims, HttpStatusCode status = HttpStatusCode.Unauthorized,
        string? rawHeader = null) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(
                    status == HttpStatusCode.OK ? "{\"value\":[]}" :
                        "{\"error\":{\"code\":\"InvalidAuthenticationToken\",\"message\":\"Continuous access evaluation requires interaction.\"}}",
                    Encoding.UTF8, "application/json")
            };
            if (claims != null || rawHeader != null)
            {
                var encodedClaims = claims == null ? rawHeader : Convert.ToBase64String(Encoding.UTF8.GetBytes(claims));
                response.Headers.TryAddWithoutValidation("www-authenticate",
                    $"Bearer error=\"insufficient_claims\", claims=\"{encodedClaims}\"");
            }
            return Task.FromResult(response);
        }
    }
}