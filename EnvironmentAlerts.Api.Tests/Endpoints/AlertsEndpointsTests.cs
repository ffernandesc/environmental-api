using EnvironmentAlerts.Api.Endpoints;
using EnvironmentAlerts.Api.Models;
using EnvironmentAlerts.Api.Tests.TestSupport;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EnvironmentAlerts.Api.Tests.Endpoints;

public class AlertsEndpointsTests : IDisposable
{
    private readonly SqliteDbContextFactory _dbContextFactory = new();

    public void Dispose() => _dbContextFactory.Dispose();

    private async Task<(int LocationId, int ReadingId)> SeedLocationAndReadingAsync()
    {
        await using var db = _dbContextFactory.CreateContext();
        var location = new Location { City = "Braga", District = "Braga", Latitude = 41.55, Longitude = -8.42 };
        db.Locations.Add(location);
        await db.SaveChangesAsync();

        var reading = new Reading { LocationId = location.Id, Temperature = 20, Humidity = 50, WindSpeed = 10, Precipitation = 0, Pm10 = 10, Pm25 = 10, EuropeanAqi = 10 };
        db.Readings.Add(reading);
        await db.SaveChangesAsync();

        return (location.Id, reading.Id);
    }

    [Fact]
    public async Task CreateAlert_ReturnsBadRequest_WhenReadingIdDoesNotExist()
    {
        var (locationId, _) = await SeedLocationAndReadingAsync();
        await using var db = _dbContextFactory.CreateContext();
        var alert = new Alert { LocationId = locationId, ReadingId = 999, AlertType = "Temperature", WarningLevel = "High", Message = "Test" };

        var result = await AlertsEndpoints.CreateAlert(
            alert,
            db,
            TestHttpFactory.CreateRequest(),
            TestHttpFactory.CreateConfiguration());

        Assert.IsType<BadRequest<string>>(result);
        Assert.Empty(db.Alerts);
    }

    [Fact]
    public async Task CreateAlert_ReturnsUnauthorized_WhenApiKeyIsMissing()
    {
        var (locationId, readingId) = await SeedLocationAndReadingAsync();
        await using var db = _dbContextFactory.CreateContext();
        var alert = new Alert { LocationId = locationId, ReadingId = readingId, AlertType = "Temperature", WarningLevel = "High", Message = "Test" };

        var result = await AlertsEndpoints.CreateAlert(
            alert,
            db,
            TestHttpFactory.CreateRequest(apiKeyHeaderValue: null),
            TestHttpFactory.CreateConfiguration());

        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task CreateAlert_PersistsAlert_WhenLocationAndReadingExist()
    {
        var (locationId, readingId) = await SeedLocationAndReadingAsync();
        await using var db = _dbContextFactory.CreateContext();
        var alert = new Alert { LocationId = locationId, ReadingId = readingId, AlertType = "Temperature", WarningLevel = "High", Message = "Test" };

        var result = await AlertsEndpoints.CreateAlert(
            alert,
            db,
            TestHttpFactory.CreateRequest(),
            TestHttpFactory.CreateConfiguration());

        Assert.IsType<Created<Alert>>(result);
        Assert.Single(db.Alerts);
    }

    [Fact]
    public async Task GetAlertById_ReturnsNotFound_WhenAlertDoesNotExist()
    {
        await using var db = _dbContextFactory.CreateContext();

        var result = await AlertsEndpoints.GetAlertById(id: 999, db);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task DeleteAlert_ReturnsUnauthorized_WhenApiKeyIsMissing()
    {
        await using var db = _dbContextFactory.CreateContext();

        var result = await AlertsEndpoints.DeleteAlert(
            id: 1,
            db,
            TestHttpFactory.CreateRequest(apiKeyHeaderValue: null),
            TestHttpFactory.CreateConfiguration());

        Assert.IsType<UnauthorizedHttpResult>(result);
    }
}
