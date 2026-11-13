using System.Text.Json.Serialization;

namespace EnvironmentAlerts.Api.Models.Seed;
public sealed class LocationsSeed
{
    [JsonPropertyName("city")]
    public required string City { get; init; }

    [JsonPropertyName("district")]
    public required string District { get; init; }

    [JsonPropertyName("latitude")]
    public double Latitude { get; init; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; init; }
}