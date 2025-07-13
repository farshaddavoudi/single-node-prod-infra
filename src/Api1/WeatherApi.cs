using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;

namespace BlazorKCOidcBff.ApiService;

internal static class WeatherApi
{
    internal static RouteGroupBuilder MapWeatherApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("").WithTags("Weather");

        api.MapGet("weather-forecast", GetWeather)
            .WithName("GetWeatherForecast")
            .WithSummary("Get weather forecast")
            .WithOpenApi()
            .RequireAuthorization();

        api.MapGet("user-info", GetUserInfoResponse)
            .WithName("GetUserInfo")
            .WithSummary("Get user info")
            .WithOpenApi()
            .RequireAuthorization();

        api.MapGet("ping", GetPing)
            .WithOpenApi()
            .WithSummary("Ping app");

        return api;
    }

    private static Results<Ok<UserInfoResponse>, BadRequest<string>> GetUserInfoResponse(ClaimsPrincipal claimsPrincipal)
    {
        if (claimsPrincipal is null)
        {
            return TypedResults.BadRequest("No user info found.");
        }

        return TypedResults.Ok(new UserInfoResponse(GetUserRoleInfo(claimsPrincipal)));
    }

    private static string GetUserRoleInfo(ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal switch
        {
            _ when claimsPrincipal.IsInRole("admin") => "You are an admin.",
            _ when claimsPrincipal.IsInRole("general") => "You are a general user.",
            _ => "You are a user."
        };
    }

    private static WeatherForecast[] GetWeather()
    {
        var summaries = new[]
{
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
                .ToArray();
        return forecast;
    }

    private static string[] GetPing(HttpContext context)
    {
        var ip = $"ip={context.Connection.RemoteIpAddress}";
        var machine = $"machine={Environment.MachineName}";
        var sso = $"sso: https://sso.farshaddavoudi.ir";

        var headers = context.Request.Headers
            .Select(h => $"header:{h.Key}={string.Join(",", h.Value.ToArray())}")
            .ToArray();

        return [ip, machine, sso, .. headers];
    }
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
record UserInfoResponse
{
    public string UserInfo { get; }
    public UserInfoResponse(string userInfo)
    {
        UserInfo = userInfo;
    }

}