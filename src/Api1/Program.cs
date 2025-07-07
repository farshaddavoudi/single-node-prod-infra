using BlazorKCOidcBff.ApiService;
using Microsoft.IdentityModel.Tokens;

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

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.MapWeatherApi();

app.Run();

