using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace EnvironmentAlerts.Api.Tests.TestSupport;

public static class TestHttpFactory
{
    public const string ValidApiKey = "test-api-key";

    public static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiKey"] = ValidApiKey
            })
            .Build();
    }

    public static HttpRequest CreateRequest(string? apiKeyHeaderValue = ValidApiKey)
    {
        var context = new DefaultHttpContext();

        if (apiKeyHeaderValue is not null)
        {
            context.Request.Headers["X-API-Key"] = apiKeyHeaderValue;
        }

        return context.Request;
    }
}
