using System.ServiceModel;

namespace EnvironmentAlerts.Api.Services.Soap;

[ServiceContract(Namespace = "https://environmentalerts.api/soap")]
public interface IDataLayerSoapService
{
    [OperationContract]
    Task<List<SoapLocationDto>> GetLocations();

    [OperationContract]
    Task<SoapLocationDto?> GetLocationById(int id);

    [OperationContract]
    Task<List<SoapAlertDto>> GetAlerts();

    [OperationContract]
    Task<List<SoapAlertDto>> GetAlertsByLocation(int locationId);
}