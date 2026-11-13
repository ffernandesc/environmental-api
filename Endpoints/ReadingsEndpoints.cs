using EnvironmentAlerts.Api.Data;
using EnvironmentAlerts.Api.Models;
using EnvironmentAlerts.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace EnvironmentAlerts.Api.Endpoints;

public static class ReadingsEndpoints
{
    public static void MapReadingsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/readings", GetReadings)
            .WithName("GetReadings")
            .WithTags("Readings")
            .WithSummary("Lista todas as leituras ambientais")
            .WithDescription("Devolve todas as leituras ambientais, incluindo a localização associada.")
            .Produces(StatusCodes.Status200OK);

        app.MapGet("/api/readings/{id}", GetReadingById)
            .WithName("GetReadingById")
            .WithTags("Readings")
            .WithSummary("Obtém uma leitura por id")
            .WithDescription("Devolve os dados de uma leitura ambiental específica, incluindo a localização associada.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/readings", CreateReading)
            .WithName("CreateReading")
            .WithTags("Readings")
            .WithSummary("Regista uma nova leitura ambiental")
            .WithDescription("Cria uma leitura ambiental e gera automaticamente um alerta caso os valores excedam os limites definidos. Requer autenticação por API Key.")
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        app.MapPut("/api/readings/{id}", UpdateReading)
            .WithName("UpdateReading")
            .WithTags("Readings")
            .WithSummary("Atualiza uma leitura ambiental")
            .WithDescription("Atualiza os dados de uma leitura existente. Requer autenticação por API Key.")
            .Produces<Reading>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        app.MapDelete("/api/readings/{id}", DeleteReading)
            .WithName("DeleteReading")
            .WithTags("Readings")
            .WithSummary("Remove uma leitura ambiental")
            .WithDescription("Elimina uma leitura existente. Requer autenticação por API Key.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/readings/import-current/{locationId}", ImportCurrentReading)
            .WithName("ImportCurrentReading")
            .WithTags("Readings")
            .WithSummary("Importa a leitura meteorológica atual do Open-Meteo")
            .WithDescription("Obtém dados meteorológicos e de qualidade do ar atuais para a localização indicada através da API Open-Meteo, guarda-os como uma leitura e gera um alerta se necessário. Requer autenticação por API Key.")
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static object ToReadingResponse(Reading reading) => new
    {
        reading.Id,
        reading.LocationId,
        Location = reading.Location == null ? null : new
        {
            reading.Location.Id,
            reading.Location.City,
            reading.Location.District,
            reading.Location.Latitude,
            reading.Location.Longitude
        },
        reading.DateTime,
        reading.Temperature,
        reading.Humidity,
        reading.WindSpeed,
        reading.Precipitation,
        reading.Pm10,
        reading.Pm25,
        reading.EuropeanAqi
    };

    private static object? ToAlertResponse(Alert? alert) => alert is null ? null : new
    {
        alert.Id,
        alert.LocationId,
        alert.ReadingId,
        alert.AlertType,
        alert.WarningLevel,
        alert.Message,
        alert.CreatedAt
    };

    public static async Task<IResult> GetReadings(EnvironmentalAlertsDbContext db)
    {
        var readings = await db.Readings
            .Include(reading => reading.Location)
            .ToListAsync();

        return Results.Ok(readings.Select(ToReadingResponse));
    }

    public static async Task<IResult> GetReadingById(int id, EnvironmentalAlertsDbContext db)
    {
        var reading = await db.Readings
            .Include(reading => reading.Location)
            .FirstOrDefaultAsync(reading => reading.Id == id);

        if (reading is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(ToReadingResponse(reading));
    }

    public static async Task<IResult> CreateReading(
        Reading reading,
        EnvironmentalAlertsDbContext db,
        AlertService alertService,
        HttpRequest request,
        IConfiguration configuration)
    {
        if (!ApiKeyAuthorization.IsAuthorized(request, configuration))
        {
            return Results.Unauthorized();
        }

        var locationExists = await db.Locations.AnyAsync(location => location.Id == reading.LocationId);

        if (!locationExists)
        {
            return Results.BadRequest("The provided LocationId does not exist.");
        }

        db.Readings.Add(reading);
        await db.SaveChangesAsync();

        var alert = alertService.GenerateAlert(reading);

        if (alert is not null)
        {
            db.Alerts.Add(alert);
            await db.SaveChangesAsync();
        }

        return Results.Created($"/api/readings/{reading.Id}", new
        {
            Reading = ToReadingResponse(reading),
            Alert = ToAlertResponse(alert)
        });
    }

    public static async Task<IResult> UpdateReading(
        int id,
        Reading updatedReading,
        EnvironmentalAlertsDbContext db,
        HttpRequest request,
        IConfiguration configuration)
    {
        if (!ApiKeyAuthorization.IsAuthorized(request, configuration))
        {
            return Results.Unauthorized();
        }

        var reading = await db.Readings.FindAsync(id);

        if (reading is null)
        {
            return Results.NotFound();
        }

        var locationExists = await db.Locations.AnyAsync(location => location.Id == updatedReading.LocationId);

        if (!locationExists)
        {
            return Results.BadRequest("The provided LocationId does not exist.");
        }

        reading.LocationId = updatedReading.LocationId;
        reading.DateTime = updatedReading.DateTime;
        reading.Temperature = updatedReading.Temperature;
        reading.Humidity = updatedReading.Humidity;
        reading.WindSpeed = updatedReading.WindSpeed;
        reading.Precipitation = updatedReading.Precipitation;
        reading.Pm10 = updatedReading.Pm10;
        reading.Pm25 = updatedReading.Pm25;
        reading.EuropeanAqi = updatedReading.EuropeanAqi;

        await db.SaveChangesAsync();

        return Results.Ok(reading);
    }

    public static async Task<IResult> DeleteReading(
        int id,
        EnvironmentalAlertsDbContext db,
        HttpRequest request,
        IConfiguration configuration)
    {
        if (!ApiKeyAuthorization.IsAuthorized(request, configuration))
        {
            return Results.Unauthorized();
        }

        var reading = await db.Readings.FindAsync(id);

        if (reading is null)
        {
            return Results.NotFound();
        }

        var hasRelatedData = await db.Alerts.AnyAsync(a => a.ReadingId == id);

        if (hasRelatedData)
        {
            return Results.Conflict("Cannot delete a reading that has alerts attached to it");
        }

        db.Readings.Remove(reading);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    public static async Task<IResult> ImportCurrentReading(
        int locationId,
        EnvironmentalAlertsDbContext db,
        OpenMeteoService openMeteoService,
        AlertService alertService,
        HttpRequest request,
        IConfiguration configuration)
    {
        if (!ApiKeyAuthorization.IsAuthorized(request, configuration))
        {
            return Results.Unauthorized();
        }

        var location = await db.Locations.FindAsync(locationId);

        if (location is null)
        {
            return Results.NotFound("Location not found.");
        }

        var reading = await openMeteoService.GetCurrentReadingAsync(location);

        db.Readings.Add(reading);
        await db.SaveChangesAsync();

        var alert = alertService.GenerateAlert(reading);

        if (alert is not null)
        {
            db.Alerts.Add(alert);
            await db.SaveChangesAsync();
        }

        reading.Location = location;

        return Results.Created($"/api/readings/{reading.Id}", new
        {
            Reading = ToReadingResponse(reading),
            Alert = ToAlertResponse(alert)
        });
    }
}
