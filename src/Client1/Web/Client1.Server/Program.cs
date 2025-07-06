using Client1;
using Client1.Client.Weather;
using Client1.Server;
using Client1.Server.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using ShopNet.Portal.Extensions;
using System.Text.Json;

const string KC_OIDC_SCHEME = "KeycloakOidc";

Console.WriteLine("Hello From Client1.Server");

var builder = WebApplication.CreateBuilder(args);

// https://github.com/dotnet/eShop/blob/main/src/WebApp/Extensions/Extensions.cs
JsonWebTokenHandler.DefaultInboundClaimTypeMap.Remove("sub");

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDataProtection()
        .DisableAutomaticKeyGeneration()
        .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "aspnet-keys")));
}
else
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo("/app/keys")) // For Docker containers
        .SetApplicationName("Client1.Server")
        .SetDefaultKeyLifetime(TimeSpan.FromDays(90)); // Optional: Set key lifetime
}

builder.Services.Configure<KeycloakOptions>(
    builder.Configuration.GetSection("Keycloak"));

var keycloakConfig = builder.Configuration
    .GetSection("Keycloak")
    .Get<KeycloakOptions>();

string kcConfig = JsonSerializer.Serialize(keycloakConfig);

builder.Services
    .AddAuthentication(KC_OIDC_SCHEME)
        .AddOpenIdConnect(KC_OIDC_SCHEME, oidcOptions =>
    {
        oidcOptions.RequireHttpsMetadata = false; // Change over http calls to Keycloak, set to true in production

        // Configure Keycloak integration
        oidcOptions.Authority = keycloakConfig?.Authority;  // Your authority (keycloak realm)
        oidcOptions.ClientId = keycloakConfig?.ClientId;    // Your client id (keycloak realms client)
        oidcOptions.ClientSecret = keycloakConfig?.ClientSecret; // Your client secret (keycloak realms client secret)

        // Add required scopes - in Keycloak, create a client scope with 
        // Category Token mapper and type Audience
        foreach (var scope in keycloakConfig?.Scopes ?? Array.Empty<string>())
        {
            oidcOptions.Scope.Add(scope);
        }

        oidcOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        oidcOptions.ResponseType = OpenIdConnectResponseType.Code;
        oidcOptions.MapInboundClaims = false;
        oidcOptions.TokenValidationParameters.NameClaimType = "name";
        oidcOptions.TokenValidationParameters.RoleClaimType = "roles";
        oidcOptions.PushedAuthorizationBehavior = PushedAuthorizationBehavior.UseIfAvailable;

        // Event handling for post-authentication actions
        oidcOptions.Events = new OpenIdConnectEvents
        {
            OnPushAuthorization = context =>
            {
                Console.WriteLine("Pushing authorization requests");
                return Task.CompletedTask;
            },

            OnTokenValidated = context =>
            {
                // UserLoggedIn event
                Console.WriteLine("Token validated");
                return Task.CompletedTask;
            },

            OnAuthenticationFailed = context =>
            {
                // UserLoggedOut event
                Console.WriteLine("Failerd to authenticate user");
                return Task.CompletedTask;
            }
        };
    })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);

builder.Services.ConfigureCookieOidcRefresh(CookieAuthenticationDefaults.AuthenticationScheme, KC_OIDC_SCHEME);

builder.Services.AddAuthorization();

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

builder.Services.AddHttpContextAccessor();
var api1Url = Environment.GetEnvironmentVariable("WEATHER_API_URL") ?? builder.Configuration["ApiUrls:WeatherApi"] ?? "http://api1:8080";
System.Console.WriteLine($"######### API 1 URL is: {api1Url}");
System.Console.WriteLine($"Keycloak Configuration: {kcConfig}");
builder.Services.AddHttpClient<IWeatherForecaster, ServerWeatherForecaster>(httpClient =>
{
    httpClient.BaseAddress = new(
        api1Url
    );
});

builder.Services.AddReverseProxy();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseHttpsRedirection();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.MapStaticAssets();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Client1.Client._Imports).Assembly);

app.MapEndpoints(builder.Configuration);

app.Run();