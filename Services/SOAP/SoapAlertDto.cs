namespace EnvironmentAlerts.Api.Services.Soap;

public class SoapAlertDto
{
    public int Id { get; set; }

    public int LocationId { get; set; }

    public int ReadingId { get; set; }

    public string City { get; set; } = string.Empty;

    public string AlertType { get; set; } = string.Empty;

    public string WarningLevel { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}