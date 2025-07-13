using BlazorKCOidcBff.ApiService;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Net;
using static System.Net.Mime.MediaTypeNames;

var builder = WebApplication.CreateBuilder(args);


var keycloakAuthority = builder.Configuration["Keycloak:Authority"] ?? "http://localhost:8080/realms/ata/";
var keycloakAudience = builder.Configuration["Keycloak:Audience"] ?? "ata-api1";
Console.WriteLine($"Keycloak Settings: Authority={keycloakAuthority} | Audience={keycloakAudience}");
var requireHttps = false;
System.Console.WriteLine($"The Keycloak URL: {keycloakAuthority}");

const string authenticationSchemeConst = "OAuth2";

builder.Services.AddAuthentication()
    .AddJwtBearer(authenticationSchemeConst, jwtOptions =>
{
    jwtOptions.Authority = keycloakAuthority;
    jwtOptions.Audience = keycloakAudience;
    jwtOptions.RequireHttpsMetadata = requireHttps;

    // Add these validation parameters
    jwtOptions.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = keycloakAuthority,
        ValidAudiences = new[] { "ata-api1", "account" } // All valid audiences
    };
});
builder.Services.AddAuthorization();

#region Swagger + OAuth Integration

builder.Services.AddOpenApi(options =>
{
    var openApiSecurityScheme = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Description = "OAuth2 authentication using Keycloak.",
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri(builder.Configuration["Keycloak:Authority"]),
                TokenUrl = new Uri($"{keycloakAuthority}protocol/openid-connect/token"),
                Scopes = new Dictionary<string, string>
                {
                    { "openid", "openid" },
                    { "profile", "profile" }
                }
            }
        },
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = authenticationSchemeConst
        }
    };

    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Components ??= new();
        document.Components.SecuritySchemes.Add(authenticationSchemeConst, openApiSecurityScheme);
        return Task.CompletedTask;
    });    
    
    options.AddOperationTransformer((operation, context, ct) =>
    {
        if (context.Description.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any())
        {
            operation.Security = new List<OpenApiSecurityRequirement>
            {
                new()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Id = authenticationSchemeConst,
                                Type = ReferenceType.SecurityScheme
                            }
                        },
                        ["openid", "profile"]
                    }
                }
            };
        }

        return Task.CompletedTask;
    });
});



builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "API 1", Version = "v1" });

    options.AddSecurityDefinition("oauth", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"{keycloakAuthority}protocol/openid-connect/auth"),
                TokenUrl = new Uri($"{keycloakAuthority}protocol/openid-connect/token"),
                Scopes = new Dictionary<string, string>
                {
                    { "openid", "OpenID Connect" },
                    { "profile", "User profile" }
                }
            }
        }
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth" },
                Scheme = "oauth",
                Name = "oauth",
                In = ParameterLocation.Header,
            },
            ["openid", "profile"]
        }
    });

    options.CustomOperationIds(apiDesc => apiDesc.TryGetMethodInfo(out var methodInfo) ? methodInfo.Name : null);
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // Add Traefik IP here. You can inspect this by checking `docker network inspect`
    // or just allow all for now (use with caution in internal environments):
    options.KnownNetworks.Clear(); // Clear any preconfigured networks
    options.KnownProxies.Clear();  // Clear any preconfigured proxies

    // 👇 Allow all for testing — safe if you're in a controlled environment
    options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("0.0.0.0"), 0));
});

#endregion

var app = builder.Build();

// ✅ This tells ASP.NET Core to respect X-Forwarded-Proto and X-Forwarded-Host headers (sent by Traefik)
app.UseForwardedHeaders();

app.Use(async (context, next) =>
{
    Console.WriteLine($"🔍 Effective URL: {context.Request.Scheme}://{context.Request.Host}{context.Request.Path}");
    await next();
});

var redirectUrl = builder.Configuration["Keycloak:ScalarRedirectUri"];
Console.WriteLine($"Redirect Scalar URL: {redirectUrl}");

#region Swagger + OAuth Integration

app.UseSwagger();
//app.UseSwaggerUI(c =>
//{
//    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API 1");
//    c.OAuthClientId(builder.Configuration["Keycloak:ClientId"]);
//    c.OAuthClientSecret(builder.Configuration["Keycloak:ClientSecret"]);
//    c.OAuthAppName("API 1 OAuth - Swagger");
//    c.OAuthUsePkce();
//    c.EnablePersistAuthorization();
//    c.ExposeSwaggerDocumentUrlsRoute = true;
//    c.OAuth2RedirectUrl("https://api1.farshaddavoudi.ir/swagger/oauth2-redirect.html");

//    // Explicitly configure the authorization URL

//    c.ConfigObject.AdditionalItems["oauth2RedirectUrl"] = "https://api1.farshaddavoudi.ir/swagger/oauth2-redirect.html";
//});

#endregion

#region Scalar + OAuth Integration

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options
        .WithTheme(ScalarTheme.Solarized)
        .WithLayout(ScalarLayout.Modern)
        .WithFavicon("https://scalar.com/logo-light.svg")
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);

    options.AddPreferredSecuritySchemes(authenticationSchemeConst);

    options.AddAuthorizationCodeFlow(authenticationSchemeConst, flow =>
        {
            flow.ClientId = builder.Configuration["Keycloak:ClientId"];
            flow.ClientSecret = builder.Configuration["Keycloak:ClientSecret"];
            flow.AuthorizationUrl = $"{keycloakAuthority}protocol/openid-connect/auth";
            flow.Pkce = Pkce.Sha256;
            flow.TokenUrl = $"{keycloakAuthority}protocol/openid-connect/token";
            flow.RedirectUri = builder.Configuration["Keycloak:ScalarRedirectUri"];
            flow.SelectedScopes = ["openid", "profile"];
        })
        .AddDefaultScopes(authenticationSchemeConst, ["openid", "profile"]);
});

#endregion

app.MapWeatherApi();

app.Run();

