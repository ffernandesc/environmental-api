using EnvironmentAlerts.Api.Endpoints;
using EnvironmentAlerts.Api.Models;
using EnvironmentAlerts.Api.Services;
using EnvironmentAlerts.Api.Tests.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EnvironmentAlerts.Api.Tests.Endpoints;

public class ReadingsEndpointsTests : IDisposable
{
    private readonly SqliteDbContextFactory _dbContextFactory = new();
    private readonly AlertService _alertService = new();

    public void Dispose() => _dbContextFactory.Dispose();

    private async Task<int> SeedLocationAsync()
    {
        await using var db = _dbContextFactory.CreateContext();
        var location = new Location { City = "Braga", District = "Braga", Latitude = 41.55, Longitude = -8.42 };
        db.Locations.Add(location);
        await db.SaveChangesAsync();

        return location.Id;
    }

    [Fact]
    public async Task CreateReading_ReturnsBadRequest_WhenLocationDoesNotExist()
    {
        await using var db = _dbContextFactory.CreateContext();
        var reading = new Reading { LocationId = 999, Temperature = 20, Humidity = 50, WindSpeed = 10, Precipitation = 0, Pm10 = 10, Pm25 = 10, EuropeanAqi = 10 };

        var result = await ReadingsEndpoints.CreateReading(
            reading,
            db,
            _alertService,
            TestHttpFactory.CreateRequest(),
            TestHttpFactory.CreateConfiguration());

        Assert.IsType<BadRequest<string>>(result);
        Assert.Empty(db.Readings);
    }

    [Fact]
    public async Task CreateReading_ReturnsUnauthorized_WhenApiKeyIsMissing()
    {
        var locationId = await SeedLocationAsync();
        await using var db = _dbContextFactory.CreateContext();
        var reading = new Reading { LocationId = locationId, Temperature = 20, Humidity = 50, WindSpeed = 10, Precipitation = 0, Pm10 = 10, Pm25 = 10, EuropeanAqi = 10 };

        var result = await ReadingsEndpoints.CreateReading(
            reading,
            db,
            _alertService,
            TestHttpFactory.CreateRequest(apiKeyHeaderValue: null),
            TestHttpFactory.CreateConfiguration());

        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task CreateReading_DoesNotGenerateAlert_WhenValuesAreWithinSafeRanges()
    {
        var locationId = await SeedLocationAsync();
        await using var db = _dbContextFactory.CreateContext();
        var reading = new Reading { LocationId = locationId, Temperature = 20, Humidity = 50, WindSpeed = 10, Precipitation = 0, Pm10 = 10, Pm25 = 10, EuropeanAqi = 10 };

        var result = await ReadingsEndpoints.CreateReading(
            reading,
            db,
            _alertService,
            TestHttpFactory.CreateRequest(),
            TestHttpFactory.CreateConfiguration());

        AssertStatusCode(result, StatusCodes.Status201Created);
        Assert.Single(db.Readings);
        Assert.Empty(db.Alerts);
    }

    [Fact]
    public async Task CreateReading_GeneratesAlert_WhenTemperatureExceedsThreshold()
    {
        var locationId = await SeedLocationAsync();
        await using var db = _dbContextFactory.CreateContext();
        var reading = new Reading { LocationId = locationId, Temperature = 36, Humidity = 50, WindSpeed = 10, Precipitation = 0, Pm10 = 10, Pm25 = 10, EuropeanAqi = 10 };

        var result = await ReadingsEndpoints.CreateReading(
            reading,
            db,
            _alertService,
            TestHttpFactory.CreateRequest(),
            TestHttpFactory.CreateConfiguration());

        AssertStatusCode(result, StatusCodes.Status201Created);
        Assert.Single(db.Readings);

        var alert = Assert.Single(db.Alerts);
        Assert.Equal("Temperature", alert.AlertType);
        Assert.Equal("Very High", alert.WarningLevel);
    }

    [Fact]
    public async Task GetReadingById_ReturnsNotFound_WhenReadingDoesNotExist()
    {
        await using var db = _dbContextFactory.CreateContext();

        var result = await ReadingsEndpoints.GetReadingById(id: 999, db);

        Assert.IsType<NotFound>(result);
    }

    private static void AssertStatusCode(IResult result, int expectedStatusCode)
    {
        var statusCodeResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(expectedStatusCode, statusCodeResult.StatusCode);
    }
}
