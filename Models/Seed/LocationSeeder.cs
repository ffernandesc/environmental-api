using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using EnvironmentAlerts.Api.Data;

namespace EnvironmentAlerts.Api.Models.Seed;

public static class LocationSeeder
{
    public static async Task SeedAsync(
        EnvironmentalAlertsDbContext db,
        CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(
            AppContext.BaseDirectory,
            "Data",
            "Seed",
            "locations.json");

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "Locations seed file was not found.",
                filePath);
        }

        await using var stream = File.OpenRead(filePath);

        var locations =
            await JsonSerializer.DeserializeAsync<List<LocationsSeed>>(
                stream,
                cancellationToken: cancellationToken)
            ?? [];

        // Case-insensitive key
        static string CreateKey(string city, string district) =>
            $"{city.Trim()}|{district.Trim()}".ToUpperInvariant();

        var existingLocations = await db.Locations
            .AsNoTracking()
            .Select(location => new
            {
                location.City,
                location.District
            })
            .ToListAsync(cancellationToken);

        var existingKeys = existingLocations
            .Select(location => CreateKey(location.City, location.District))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingLocations = locations
            .Where(capital =>
                !existingKeys.Contains(CreateKey(capital.City, capital.District)))
            .Select(capital => new Location
            {
                City = capital.City.Trim(),
                District = capital.District.Trim(),
                Latitude = capital.Latitude,
                Longitude = capital.Longitude
            })
            .ToList();

        if (missingLocations.Count == 0)
        {
            return;
        }

        db.Locations.AddRange(missingLocations);
        await db.SaveChangesAsync(cancellationToken);
    }
}