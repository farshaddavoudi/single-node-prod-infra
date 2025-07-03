using Client1;
using Client1.Client.Weather;
using Client1.Server;
using Client1.Server.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using ShopNet.Portal.Extensions;

const string KC_OIDC_SCHEME = "KeycloakOidc";

var builder = WebApplication.CreateBuilder(args);

// https://github.com/dotnet/eShop/blob/main/src/WebApp/Extensions/Extensions.cs
JsonWebTokenHandler.DefaultInboundClaimTypeMap.Remove("sub");

builder.Services.Configure<KeycloakOptions>(
    builder.Configuration.GetSection("Keycloak"));

builder.Services
    .AddAuthentication(KC_OIDC_SCHEME)
        .AddOpenIdConnect(KC_OIDC_SCHEME, oidcOptions =>
    {
        oidcOptions.RequireHttpsMetadata = false; // Change over http calls to Keycloak, set to true in production

        var keycloakConfig = builder.Configuration
            .GetSection("Keycloak")
            .Get<KeycloakOptions>();

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
builder.Services.AddHttpClient<IWeatherForecaster, ServerWeatherForecaster>(httpClient =>
{
    httpClient.BaseAddress = new("http://localhost:5336");
});

builder.Services.AddReverseProxy();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.MapStaticAssets();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Client1.Client._Imports).Assembly);

app.MapEndpoints(builder.Configuration);

app.Run();