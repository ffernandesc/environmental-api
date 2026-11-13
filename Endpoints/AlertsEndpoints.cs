using EnvironmentAlerts.Api.Data;
using EnvironmentAlerts.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EnvironmentAlerts.Api.Endpoints;

public static class AlertsEndpoints
{
    public static void MapAlertsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/alerts", GetAlerts)
            .WithName("GetAlerts")
            .WithTags("Alerts")
            .WithSummary("Lista todos os alertas")
            .WithDescription("Devolve todos os alertas gerados, incluindo a localização e a leitura associadas.")
            .Produces(StatusCodes.Status200OK);

        app.MapGet("/api/alerts/{id}", GetAlertById)
            .WithName("GetAlertById")
            .WithTags("Alerts")
            .WithSummary("Obtém um alerta por id")
            .WithDescription("Devolve os dados de um alerta específico, incluindo a localização e a leitura associadas.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/alerts", CreateAlert)
            .WithName("CreateAlert")
            .WithTags("Alerts")
            .WithSummary("Cria um novo alerta")
            .WithDescription("Regista manualmente um novo alerta associado a uma localização e a uma leitura. Requer autenticação por API Key.")
            .Produces<Alert>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        app.MapPut("/api/alerts/{id}", UpdateAlert)
            .WithName("UpdateAlert")
            .WithTags("Alerts")
            .WithSummary("Atualiza um alerta")
            .WithDescription("Atualiza os dados de um alerta existente. Requer autenticação por API Key.")
            .Produces<Alert>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        app.MapDelete("/api/alerts/{id}", DeleteAlert)
            .WithName("DeleteAlert")
            .WithTags("Alerts")
            .WithSummary("Remove um alerta")
            .WithDescription("Elimina um alerta existente. Requer autenticação por API Key.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static object ToAlertResponse(Alert alert) => new
    {
        alert.Id,
        alert.LocationId,
        Location = alert.Location == null ? null : new
        {
            alert.Location.Id,
            alert.Location.City,
            alert.Location.District,
            alert.Location.Latitude,
            alert.Location.Longitude
        },
        alert.ReadingId,
        Reading = alert.Reading == null ? null : new
        {
            alert.Reading.Id,
            alert.Reading.DateTime,
            alert.Reading.Temperature,
            alert.Reading.Humidity,
            alert.Reading.WindSpeed,
            alert.Reading.Precipitation,
            alert.Reading.Pm10,
            alert.Reading.Pm25,
            alert.Reading.EuropeanAqi
        },
        alert.AlertType,
        alert.WarningLevel,
        alert.Message,
        alert.CreatedAt
    };

    public static async Task<IResult> GetAlerts(EnvironmentalAlertsDbContext db)
    {
        var alerts = await db.Alerts
            .Include(alert => alert.Location)
            .Include(alert => alert.Reading)
            .ToListAsync();

        return Results.Ok(alerts.Select(ToAlertResponse));
    }

    public static async Task<IResult> GetAlertById(int id, EnvironmentalAlertsDbContext db)
    {
        var alert = await db.Alerts
            .Include(alert => alert.Location)
            .Include(alert => alert.Reading)
            .FirstOrDefaultAsync(alert => alert.Id == id);

        if (alert is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(ToAlertResponse(alert));
    }

    public static async Task<IResult> CreateAlert(
        Alert alert,
        EnvironmentalAlertsDbContext db,
        HttpRequest request,
        IConfiguration configuration)
    {
        if (!ApiKeyAuthorization.IsAuthorized(request, configuration))
        {
            return Results.Unauthorized();
        }

        var locationExists = await db.Locations.AnyAsync(location => location.Id == alert.LocationId);
        var readingExists = await db.Readings.AnyAsync(reading => reading.Id == alert.ReadingId);

        var reading = await db.Readings.FindAsync(alert.ReadingId);

        if (reading is null)
        {
            return Results.BadRequest("The provided ReadingId does not exist.");
        }

        if (reading.LocationId != alert.LocationId)
        {
            return Results.BadRequest(
                "The provided reading does not belong to the selected location.");
        }

        alert.CreatedAt = DateTime.UtcNow;

        db.Alerts.Add(alert);
        await db.SaveChangesAsync();

        return Results.Created($"/api/alerts/{alert.Id}", alert);
    }

    public static async Task<IResult> UpdateAlert(
        int id,
        Alert updatedAlert,
        EnvironmentalAlertsDbContext db,
        HttpRequest request,
        IConfiguration configuration)
    {
        if (!ApiKeyAuthorization.IsAuthorized(request, configuration))
        {
            return Results.Unauthorized();
        }

        var alert = await db.Alerts.FindAsync(id);

        if (alert is null)
        {
            return Results.NotFound();
        }

        var locationExists = await db.Locations.AnyAsync(location => location.Id == alert.LocationId);

        if (!locationExists)
        {
            return Results.BadRequest("The provided LocationId does not exist.");
        }

        var reading = await db.Readings.FindAsync(alert.ReadingId);

        if (reading is null)
        {
            return Results.BadRequest("The provided ReadingId does not exist.");
        }

        if (reading.LocationId != alert.LocationId)
        {
            return Results.BadRequest(
                "The provided reading does not belong to the selected location.");
        }


        alert.LocationId = updatedAlert.LocationId;
        alert.ReadingId = updatedAlert.ReadingId;
        alert.AlertType = updatedAlert.AlertType;
        alert.WarningLevel = updatedAlert.WarningLevel;
        alert.Message = updatedAlert.Message;

        await db.SaveChangesAsync();

        return Results.Ok(alert);
    }

    public static async Task<IResult> DeleteAlert(
        int id,
        EnvironmentalAlertsDbContext db,
        HttpRequest request,
        IConfiguration configuration)
    {
        if (!ApiKeyAuthorization.IsAuthorized(request, configuration))
        {
            return Results.Unauthorized();
        }

        var alert = await db.Alerts.FindAsync(id);

        if (alert is null)
        {
            return Results.NotFound();
        }

        db.Alerts.Remove(alert);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }
}
