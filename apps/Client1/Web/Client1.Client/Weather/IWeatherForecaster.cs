
namespace Client1.Client.Weather;

public interface IWeatherForecaster
{
    Task<IEnumerable<WeatherForecast>> GetWeatherForecastAsync();
    Task<string> GetUserRoleStringFromServer();
}
