using EnvironmentAlerts.Api.Models;

namespace EnvironmentAlerts.Api.Services;

public class AlertService
{
    public Alert? GenerateAlert(Reading reading)
    {
        var temperatureWarning = EvaluateTemperature(reading.Temperature);
        var windWarning = EvaluateWind(reading.WindSpeed);
        var rainWarning = EvaluateRain(reading.Precipitation);
        var humidityWarning = EvaluateHumidity(reading.Humidity);
        var airQualityWarning = EvaluateAirQuality(reading.Pm25, reading.Pm10, reading.EuropeanAqi);

        var warnings = new[]
        {
            temperatureWarning,
            windWarning,
            rainWarning,
            humidityWarning,
            airQualityWarning
        };

        var worstWarning = warnings
            .OrderByDescending(warning => GetSeverityScore(warning.WarningLevel))
            .First();
        
        if (worstWarning.WarningLevel == "No Warning")
        {
            return null;
        }

        return new Alert
        {
            LocationId = reading.LocationId,
            ReadingId = reading.Id,
            AlertType = worstWarning.AlertType,
            WarningLevel = worstWarning.WarningLevel,
            Message = worstWarning.Message,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static AlertWarning EvaluateTemperature(double temperature)
    {
        if (temperature >= 35)
        {
            return new AlertWarning(
                "Temperature",
                "Very High",
                "Temperatura muito elevada registada."
            );
        }

        if (temperature >= 30)
        {
            return new AlertWarning(
                "Temperature",
                "High",
                "Temperatura elevada registada."
            );
        }

        if (temperature >= 25)
        {
            return new AlertWarning(
                "Temperature",
                "Moderate",
                "Temperatura moderadamente elevada registada."
            );
        }

        return AlertWarning.None;
    }

    private static AlertWarning EvaluateWind(double windSpeed)
    {
        if (windSpeed >= 50)
        {
            return new AlertWarning(
                "Wind",
                "Very High",
                "Vento muito forte registado."
            );
        }

        if (windSpeed >= 35)
        {
            return new AlertWarning(
                "Wind",
                "High",
                "Vento forte registado."
            );
        }

        if (windSpeed >= 25)
        {
            return new AlertWarning(
                "Wind",
                "Moderate",
                "Vento moderado registado."
            );
        }

        return AlertWarning.None;
    }

    private static AlertWarning EvaluateRain(double precipitation)
    {
        if (precipitation >= 10)
        {
            return new AlertWarning(
                "Rain",
                "Very High",
                "Precipitação muito intensa registada."
            );
        }

        if (precipitation >= 3)
        {
            return new AlertWarning(
                "Rain",
                "High",
                "Precipitação intensa registada."
            );
        }

        if (precipitation >= 1)
        {
            return new AlertWarning(
                "Rain",
                "Moderate",
                "Precipitação moderada registada."
            );
        }

        return AlertWarning.None;
    }

    private static AlertWarning EvaluateHumidity(double humidity)
    {
        if (humidity >= 90)
        {
            return new AlertWarning(
                "Humidity",
                "Very High",
                "Humidade muito elevada registada."
            );
        }

        if (humidity >= 80)
        {
            return new AlertWarning(
                "Humidity",
                "High",
                "Humidade elevada registada."
            );
        }

        if (humidity >= 70)
        {
            return new AlertWarning(
                "Humidity",
                "Moderate",
                "Humidade moderadamente elevada registada."
            );
        }

        return AlertWarning.None;
    }

    private static AlertWarning EvaluateAirQuality(double pm25, double pm10, int europeanAqi)
    {
        if (pm25 >= 50 || pm10 >= 100 || europeanAqi >= 80)
        {
            return new AlertWarning(
                "Air Quality",
                "Very High",
                "Qualidade do ar muito fraca registada."
            );
        }

        if (pm25 >= 25 || pm10 >= 50 || europeanAqi >= 60)
        {
            return new AlertWarning(
                "Air Quality",
                "High",
                "Qualidade do ar fraca registada."
            );
        }

        if (pm25 >= 20 || pm10 >= 40 || europeanAqi >= 40)
        {
            return new AlertWarning(
                "Air Quality",
                "Moderate",
                "Qualidade do ar moderada registada."
            );
        }

        return AlertWarning.None;
    }

    private static int GetSeverityScore(string warningLevel)
    {
        return warningLevel switch
        {
            "Very High" => 3,
            "High" => 2,
            "Moderate" => 1,
            _ => 0
        };
    }

    private record AlertWarning(
        string AlertType,
        string WarningLevel,
        string Message
    )
    {
        public static AlertWarning None =>
            new("None", "No Warning", "Sem aviso ambiental.");
    }
}