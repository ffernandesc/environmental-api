using EnvironmentAlerts.Api.Data;
using EnvironmentAlerts.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EnvironmentAlerts.Api.Endpoints;

public static class LocationsEndpoints
{
    public static void MapLocationsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/locations", GetLocations)
            .WithName("GetLocations")
            .WithTags("Locations")
            .WithSummary("Lista todas as localizações")
            .WithDescription("Devolve todas as localizações registadas na base de dados.")
            .Produces<List<Location>>(StatusCodes.Status200OK);

        app.MapGet("/api/locations/{id}", GetLocationById)
            .WithName("GetLocationById")
            .WithTags("Locations")
            .WithSummary("Obtém uma localização por id")
            .WithDescription("Devolve os dados de uma localização específica através do seu identificador.")
            .Produces<Location>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/locations", CreateLocation)
            .WithName("CreateLocation")
            .WithTags("Locations")
            .WithSummary("Cria uma nova localização")
            .WithDescription("Regista uma nova localização. Requer autenticação por API Key.")
            .Produces<Location>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status401Unauthorized);

        app.MapPut("/api/locations/{id}", UpdateLocation)
            .WithName("UpdateLocation")
            .WithTags("Locations")
            .WithSummary("Atualiza uma localização")
            .WithDescription("Atualiza os dados de uma localização existente. Requer autenticação por API Key.")
            .Produces<Location>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        app.MapDelete("/api/locations/{id}", DeleteLocation)
            .WithName("DeleteLocation")
            .WithTags("Locations")
            .WithSummary("Remove uma localização")
            .WithDescription("Elimina uma localização existente. Requer autenticação por API Key.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
    }

    public static async Task<IResult> GetLocations(EnvironmentalAlertsDbContext db)
    {
        var locations = await db.Locations.ToListAsync();

        return Results.Ok(locations);
    }

    public static async Task<IResult> GetLocationById(int id, EnvironmentalAlertsDbContext db)
    {
        var location = await db.Locations.FindAsync(id);

        if (location is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(location);
    }

    public static async Task<IResult> CreateLocation(
        Location location,
        EnvironmentalAlertsDbContext db,
        HttpRequest request,
        IConfiguration configuration)
    {
        if (!ApiKeyAuthorization.IsAuthorized(request, configuration))
        {
            return Results.Unauthorized();
        }

        db.Locations.Add(location);
        await db.SaveChangesAsync();

        return Results.Created($"/api/locations/{location.Id}", location);
    }

    public static async Task<IResult> UpdateLocation(
        int id,
        Location updatedLocation,
        EnvironmentalAlertsDbContext db,
        HttpRequest request,
        IConfiguration configuration)
    {
        if (!ApiKeyAuthorization.IsAuthorized(request, configuration))
        {
            return Results.Unauthorized();
        }

        var location = await db.Locations.FindAsync(id);

        if (location is null)
        {
            return Results.NotFound();
        }

        location.City = updatedLocation.City;
        location.District = updatedLocation.District;
        location.Latitude = updatedLocation.Latitude;
        location.Longitude = updatedLocation.Longitude;

        await db.SaveChangesAsync();

        return Results.Ok(location);
    }

    public static async Task<IResult> DeleteLocation(
        int id,
        EnvironmentalAlertsDbContext db,
        HttpRequest request,
        IConfiguration configuration)
    {
        if (!ApiKeyAuthorization.IsAuthorized(request, configuration))
        {
            return Results.Unauthorized();
        }

        var location = await db.Locations.FindAsync(id);

        if (location is null)
        {
            return Results.NotFound();
        }

        var hasRelatedData = await db.Readings.AnyAsync(r => r.LocationId == id)
        || await db.Alerts.AnyAsync(a => a.LocationId == id);

        if (hasRelatedData)
        {
            return Results.Conflict("Cannot delete a location that has readings/alerts attached to it");
        }

        db.Locations.Remove(location);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }
}
