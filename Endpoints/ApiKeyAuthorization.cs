namespace EnvironmentAlerts.Api.Endpoints;

public static class ApiKeyAuthorization
{
    public static bool IsAuthorized(HttpRequest request, IConfiguration configuration)
    {
        var expectedApiKey = configuration["ApiKey"];

        if (string.IsNullOrWhiteSpace(expectedApiKey))
        {
            return false;
        }

        if (!request.Headers.TryGetValue("X-API-Key", out var providedApiKey))
        {
            return false;
        }

        return providedApiKey == expectedApiKey;
    }
}
