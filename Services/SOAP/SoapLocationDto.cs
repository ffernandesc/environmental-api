namespace EnvironmentAlerts.Api.Services.Soap;

public class SoapLocationDto
{
    public int Id { get; set; }

    public string City { get; set; } = string.Empty;

    public string District { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }
}