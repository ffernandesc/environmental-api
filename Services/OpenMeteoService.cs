using System.Globalization;
using System.Text.Json;
using EnvironmentAlerts.Api.Models;

namespace EnvironmentAlerts.Api.Services;

public class OpenMeteoService
{
    private readonly HttpClient _httpClient;

    public OpenMeteoService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Reading> GetCurrentReadingAsync(Location location)
    {
        var latitude = location.Latitude.ToString(CultureInfo.InvariantCulture);
        var longitude = location.Longitude.ToString(CultureInfo.InvariantCulture);

        var weatherUrl =
            $"https://api.open-meteo.com/v1/forecast" +
            $"?latitude={latitude}" +
            $"&longitude={longitude}" +
            $"&current=temperature_2m,relative_humidity_2m,precipitation,wind_speed_10m" +
            $"&timezone=Europe/Lisbon";

        var airQualityUrl =
            $"https://air-quality-api.open-meteo.com/v1/air-quality" +
            $"?latitude={latitude}" +
            $"&longitude={longitude}" +
            $"&current=pm10,pm2_5,european_aqi" +
            $"&timezone=Europe/Lisbon";

        using var weatherResponse = await _httpClient.GetAsync(weatherUrl);
        using var airQualityResponse = await _httpClient.GetAsync(airQualityUrl);

        weatherResponse.EnsureSuccessStatusCode();
        airQualityResponse.EnsureSuccessStatusCode();

        using var weatherJson = await JsonDocument.ParseAsync(
            await weatherResponse.Content.ReadAsStreamAsync()
        );

        using var airQualityJson = await JsonDocument.ParseAsync(
            await airQualityResponse.Content.ReadAsStreamAsync()
        );

        var weather = weatherJson.RootElement.GetProperty("current");
        var airQuality = airQualityJson.RootElement.GetProperty("current");

        return new Reading
        {
            LocationId = location.Id,
            DateTime = DateTime.Parse(weather.GetProperty("time").GetString()!),
            Temperature = weather.GetProperty("temperature_2m").GetDouble(),
            Humidity = weather.GetProperty("relative_humidity_2m").GetDouble(),
            Precipitation = weather.GetProperty("precipitation").GetDouble(),
            WindSpeed = weather.GetProperty("wind_speed_10m").GetDouble(),
            Pm10 = airQuality.GetProperty("pm10").GetDouble(),
            Pm25 = airQuality.GetProperty("pm2_5").GetDouble(),
            EuropeanAqi = airQuality.GetProperty("european_aqi").GetInt32()
        };
    }
}