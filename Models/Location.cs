using System.ComponentModel.DataAnnotations;

namespace EnvironmentAlerts.Api.Models;

public class Location
{
    public int Id { get; set; }

    public required string City { get; set; }

    public required string District { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public List<Reading> Readings { get; set; } = new();

    public List<Alert> Alerts { get; set; } = new();
}