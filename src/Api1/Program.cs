using BlazorKCOidcBff.ApiService;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);


var keycloakAuthority = builder.Configuration["Keycloak:Authority"] ?? "http://localhost:8080/realms/ata/";
var keycloakAudience = builder.Configuration["Keycloak:Audience"] ?? "ata-api1";
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

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "API 1", Version = "v1" });

    // Add OAuth2 security definition
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"{builder.Configuration["Keycloak:Authority"]}/protocol/openid-connect/auth"),
                TokenUrl = new Uri($"{builder.Configuration["Keycloak:Authority"]}/protocol/openid-connect/token"),
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

var app = builder.Build();

app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    //c.RoutePrefix = string.Empty;
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API 1");

    // Configure OAuth for Swagger UI
    c.OAuthClientId(builder.Configuration["Keycloak:ClientId"]);
    c.OAuthClientSecret(builder.Configuration["Keycloak:ClientSecret"]);
    c.OAuthAppName("API 1 OAuth - Swagger");
    c.OAuthUsePkce();
    //c.OAuthScopeSeparator(" ");
    c.OAuth2RedirectUrl("/swagger/oauth2-redirect.html");
    c.EnablePersistAuthorization();
});

// Configure the HTTP request pipeline.
//app.UseHttpsRedirection();

app.MapWeatherApi();

app.Run();

