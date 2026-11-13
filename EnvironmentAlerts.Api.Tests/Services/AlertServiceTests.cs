using EnvironmentAlerts.Api.Models;
using EnvironmentAlerts.Api.Services;

namespace EnvironmentAlerts.Api.Tests.Services;

public class AlertServiceTests
{
    private readonly AlertService _alertService = new();

    private static Reading BaselineReading() => new()
    {
        Id = 1,
        LocationId = 1,
        Temperature = 20,
        Humidity = 50,
        WindSpeed = 10,
        Precipitation = 0,
        Pm10 = 10,
        Pm25 = 10,
        EuropeanAqi = 10
    };

    [Theory]
    [InlineData(24.9, "No Warning")]
    [InlineData(25, "Moderate")]
    [InlineData(29.9, "Moderate")]
    [InlineData(30, "High")]
    [InlineData(34.9, "High")]
    [InlineData(35, "Very High")]
    public void GenerateAlert_EvaluatesTemperatureThresholds(double temperature, string expectedWarningLevel)
    {
        var reading = BaselineReading();
        reading.Temperature = temperature;

        var alert = _alertService.GenerateAlert(reading);

        if (expectedWarningLevel == "No Warning")
        {
            Assert.Null(alert);
        }
        else
        {
            Assert.NotNull(alert);
            Assert.Equal("Temperature", alert!.AlertType);
            Assert.Equal(expectedWarningLevel, alert.WarningLevel);
        }
    }

    [Fact]
    public void GenerateAlert_ReturnsNull_WhenAllValuesAreWithinSafeRanges()
    {
        var reading = BaselineReading();

        var alert = _alertService.GenerateAlert(reading);

        Assert.Null(alert);
    }

    [Fact]
    public void GenerateAlert_ReturnsMostSevereWarning_WhenMultipleThresholdsAreExceeded()
    {
        var reading = BaselineReading();
        reading.Temperature = 30;
        reading.WindSpeed = 50;

        var alert = _alertService.GenerateAlert(reading);

        Assert.NotNull(alert);
        Assert.Equal("Wind", alert!.AlertType);
        Assert.Equal("Very High", alert.WarningLevel);
    }
}
