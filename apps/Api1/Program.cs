using BlazorKCOidcBff.ApiService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication()
    .AddJwtBearer("Bearer", jwtOptions =>
{
    // Your authority (keycloak realm)
    jwtOptions.Authority = "http://localhost:8080/realms/ata/";

    // TODO: Maybe change in production
    // Your audience (scope in keycloak)
    jwtOptions.Audience = "api1";

    // Allow HTTP for development
    jwtOptions.RequireHttpsMetadata = false;
});
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.MapWeatherApi();

app.Run();

