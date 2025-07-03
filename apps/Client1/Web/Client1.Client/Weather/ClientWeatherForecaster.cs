using System.Net.Http.Json;

namespace Client1.Client.Weather;

internal sealed class ClientWeatherForecaster(HttpClient httpClient) : IWeatherForecaster
{
    public async Task<IEnumerable<WeatherForecast>> GetWeatherForecastAsync() =>
        await httpClient.GetFromJsonAsync<WeatherForecast[]>("/weather-forecast") ??
            throw new IOException("No weather forecast!");

    public async Task<string> GetUserRoleStringFromServer()
    {
        var response = await httpClient.GetFromJsonAsync<UserInfoResponse>("/user-info");
        return response?.Message ?? string.Empty;
    }

    private record UserInfoResponse(string Message);
}
