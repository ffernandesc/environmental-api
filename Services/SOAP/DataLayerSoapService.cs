using EnvironmentAlerts.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EnvironmentAlerts.Api.Services.Soap;

public class DataLayerSoapService : IDataLayerSoapService
{
    private readonly EnvironmentalAlertsDbContext _db;

    public DataLayerSoapService(EnvironmentalAlertsDbContext db)
    {
        _db = db;
    }

    public async Task<List<SoapLocationDto>> GetLocations()
    {
        return await _db.Locations
            .Select(location => new SoapLocationDto
            {
                Id = location.Id,
                City = location.City,
                District = location.District,
                Latitude = location.Latitude,
                Longitude = location.Longitude
            })
            .ToListAsync();
    }

    public async Task<SoapLocationDto?> GetLocationById(int id)
    {
        return await _db.Locations
            .Where(location => location.Id == id)
            .Select(location => new SoapLocationDto
            {
                Id = location.Id,
                City = location.City,
                District = location.District,
                Latitude = location.Latitude,
                Longitude = location.Longitude
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<SoapAlertDto>> GetAlerts()
    {
        return await _db.Alerts
            .Include(alert => alert.Location)
            .Select(alert => new SoapAlertDto
            {
                Id = alert.Id,
                LocationId = alert.LocationId,
                ReadingId = alert.ReadingId,
                City = alert.Location == null ? string.Empty : alert.Location.City,
                AlertType = alert.AlertType,
                WarningLevel = alert.WarningLevel,
                Message = alert.Message,
                CreatedAt = alert.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<SoapAlertDto>> GetAlertsByLocation(int locationId)
    {
        return await _db.Alerts
            .Include(alert => alert.Location)
            .Where(alert => alert.LocationId == locationId)
            .Select(alert => new SoapAlertDto
            {
                Id = alert.Id,
                LocationId = alert.LocationId,
                ReadingId = alert.ReadingId,
                City = alert.Location == null ? string.Empty : alert.Location.City,
                AlertType = alert.AlertType,
                WarningLevel = alert.WarningLevel,
                Message = alert.Message,
                CreatedAt = alert.CreatedAt
            })
            .ToListAsync();
    }
}