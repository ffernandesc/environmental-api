using System.ComponentModel.DataAnnotations;

namespace EnvironmentAlerts.Api.Models;

public class Reading
{
    public int Id { get; set; }

    public int LocationId { get; set; }

    public DateTime DateTime { get; set; }

    public double Temperature { get; set; }

    public double Humidity { get; set; }

    public double WindSpeed { get; set; }

    public double Precipitation { get; set; }

    public double Pm10 { get; set; }

    public double Pm25 { get; set; }

    public int EuropeanAqi { get; set; }

    public Location? Location { get; set; }

    public List<Alert> Alerts { get; set; } = new();
}