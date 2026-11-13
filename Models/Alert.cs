using System.ComponentModel.DataAnnotations;

namespace EnvironmentAlerts.Api.Models;

public class Alert
{
    public int Id { get; set; }

    public int LocationId { get; set; }

    public int ReadingId { get; set; }

    public required string AlertType { get; set; }

    public required string WarningLevel { get; set; }

    public required string Message { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Location? Location { get; set; }

    public Reading? Reading { get; set; }
}