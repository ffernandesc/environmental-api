using EnvironmentAlerts.Api.Endpoints;
using EnvironmentAlerts.Api.Models;
using EnvironmentAlerts.Api.Tests.TestSupport;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EnvironmentAlerts.Api.Tests.Endpoints;

public class LocationsEndpointsTests : IDisposable
{
    private readonly SqliteDbContextFactory _dbContextFactory = new();

    public void Dispose() => _dbContextFactory.Dispose();

    [Fact]
    public async Task GetLocationById_ReturnsNotFound_WhenLocationDoesNotExist()
    {
        await using var db = _dbContextFactory.CreateContext();

        var result = await LocationsEndpoints.GetLocationById(id: 999, db);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task GetLocationById_ReturnsLocation_WhenLocationExists()
    {
        await using (var seedDb = _dbContextFactory.CreateContext())
        {
            seedDb.Locations.Add(new Location { City = "Braga", District = "Braga", Latitude = 41.55, Longitude = -8.42 });
            await seedDb.SaveChangesAsync();
        }

        await using var db = _dbContextFactory.CreateContext();

        var result = await LocationsEndpoints.GetLocationById(id: 1, db);

        var okResult = Assert.IsType<Ok<Location>>(result);
        Assert.Equal("Braga", okResult.Value!.City);
    }

    [Fact]
    public async Task CreateLocation_ReturnsUnauthorized_WhenApiKeyIsMissing()
    {
        await using var db = _dbContextFactory.CreateContext();
        var location = new Location { City = "Porto", District = "Porto", Latitude = 41.15, Longitude = -8.61 };

        var result = await LocationsEndpoints.CreateLocation(
            location,
            db,
            TestHttpFactory.CreateRequest(apiKeyHeaderValue: null),
            TestHttpFactory.CreateConfiguration());

        Assert.IsType<UnauthorizedHttpResult>(result);
        Assert.Empty(db.Locations);
    }

    [Fact]
    public async Task CreateLocation_PersistsLocation_WhenApiKeyIsValid()
    {
        await using var db = _dbContextFactory.CreateContext();
        var location = new Location { City = "Porto", District = "Porto", Latitude = 41.15, Longitude = -8.61 };

        var result = await LocationsEndpoints.CreateLocation(
            location,
            db,
            TestHttpFactory.CreateRequest(),
            TestHttpFactory.CreateConfiguration());

        Assert.IsType<Created<Location>>(result);
        Assert.Single(db.Locations);
    }

    [Fact]
    public async Task DeleteLocation_ReturnsNotFound_WhenLocationDoesNotExist()
    {
        await using var db = _dbContextFactory.CreateContext();

        var result = await LocationsEndpoints.DeleteLocation(
            id: 999,
            db,
            TestHttpFactory.CreateRequest(),
            TestHttpFactory.CreateConfiguration());

        Assert.IsType<NotFound>(result);
    }
}
