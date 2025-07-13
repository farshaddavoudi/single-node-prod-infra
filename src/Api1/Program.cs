using BlazorKCOidcBff.ApiService;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Net;

var builder = WebApplication.CreateBuilder(args);


var keycloakAuthority = builder.Configuration["Keycloak:Authority"] ?? "http://localhost:8080/realms/ata/";
var keycloakAudience = builder.Configuration["Keycloak:Audience"] ?? "ata-api1";
Console.WriteLine($"Keycloak Settings: Authority={keycloakAuthority} | Audience={keycloakAudience}");
var requireHttps = false;
System.Console.WriteLine($"The Keycloak URL: {keycloakAuthority}");

builder.Services.AddAuthentication()
    .AddJwtBearer("Bearer", jwtOptions =>
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

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "API 1", Version = "v1" });

    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
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
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" },
                Scheme = "oauth2",
                Name = "oauth2",
                In = ParameterLocation.Header,
            },
            ["openid", "profile"]
        }
    });
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

var redirectUrl = builder.Configuration["Keycloak:SwaggerRedirectUri"];
Console.WriteLine($"Redirect Swagger URL: {redirectUrl}");

#region Swagger + OAuth Integration

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.OAuth2RedirectUrl(redirectUrl);
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API 1");
    c.OAuthClientId(builder.Configuration["Keycloak:ClientId"]);
    c.OAuthClientSecret(builder.Configuration["Keycloak:ClientSecret"]);
    c.OAuthAppName("API 1 OAuth - Swagger");
    c.OAuthUsePkce();
    c.EnablePersistAuthorization();
});

#endregion

app.MapWeatherApi();

app.Run();

