using Microsoft.Graph;
using Microsoft.Identity.Client;

// Replace with your own values
string clientId = "your-client-id";
string clientSecret = "f998Q~SOI3Tmy_xrU4Ud3exJ1ZH2KB~9htji_cV2";
string tenantId = "your-tenant-id";

var client = new GraphServiceClient(
    new DelegateAuthenticationProvider(
        async (requestMessage) =>
        {
            var accessToken = await GetAccessTokenForApplication();
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);
        }));

var subscription = new Subscription
{
    ChangeType = "created,updated,deleted",
    NotificationUrl = "your-notification-url",
    Resource = $"me/events",
    ExpirationDateTime = DateTime.UtcNow.AddHours(2),
    ClientState = "SecretClientState",
};

await client.Subscriptions.Request().AddAsync(subscription);


async Task<string> GetAccessTokenForApplication()
{
    var authConfig = ConfidentialClientApplicationBuilder
        .Create(clientId)
        .WithTenantId(tenantId)
        .WithClientSecret(clientSecret)
        .Build();

    var authResult = await authConfig.AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" }).ExecuteAsync();

    return authResult.AccessToken;
}