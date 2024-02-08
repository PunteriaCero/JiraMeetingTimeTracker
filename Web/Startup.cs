using Azure.Identity;
using Azure;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Web.Data;
using Web.Models;
using Azure.Security.KeyVault.Secrets;

namespace Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>();

            builder.Services.AddIdentityServer()
                .AddApiAuthorization<ApplicationUser, ApplicationDbContext>();

            builder.Services.AddAuthentication()
                .AddIdentityServerJwt();

            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseIdentityServer();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.MapFallbackToFile("index.html");

            app.Run();



            string[] initialScopes = Configuration.GetValue<string>("DownstreamApi:Scopes")?.Split(' ');

            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(Configuration)
                .EnableTokenAcquisitionToCallDownstreamApi(initialScopes)
                .AddMicrosoftGraph(Configuration.GetSection("DownstreamApi"))
                .AddInMemoryTokenCaches();

            // uncomment the following 3 lines to get ClientSecret from KeyVault
            //string tenantId = Configuration.GetValue<string>("AzureAd:TenantId");
            //services.Configure<MicrosoftIdentityOptions>(
            //   options => { options.ClientSecret = GetSecretFromKeyVault(tenantId, "ENTER_YOUR_SECRET_NAME_HERE"); });

            services.AddControllersWithViews(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            });

            services.AddRazorPages()
                  .AddMicrosoftIdentityUI();

            // Add the UI support to handle claims challenges
            services.AddServerSideBlazor()
               .AddMicrosoftIdentityConsentHandler();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }

        /// Gets the secret from key vault via an enabled Managed Identity.
        /// </summary>
        /// <remarks>https://github.com/Azure-Samples/app-service-msi-keyvault-dotnet/blob/master/README.md</remarks>
        /// <returns></returns>
        private string GetSecretFromKeyVault(string tenantId, string secretName)
        {
            // this should point to your vault's URI, like https://<yourkeyvault>.vault.azure.net/
            string uri = Environment.GetEnvironmentVariable("KEY_VAULT_URI");
            DefaultAzureCredentialOptions options = new DefaultAzureCredentialOptions();

            // Specify the tenant ID to use the dev credentials when running the app locally
            options.VisualStudioTenantId = tenantId;
            options.SharedTokenCacheTenantId = tenantId;
            SecretClient client = new SecretClient(new Uri(uri), new DefaultAzureCredential(options));

            // The secret name, for example if the full url to the secret is https://<yourkeyvault>.vault.azure.net/secrets/ENTER_YOUR_SECRET_NAME_HERE
            Response<KeyVaultSecret> secret = client.GetSecretAsync(secretName).Result;

            return secret.Value.Value;
        }
    }
}
