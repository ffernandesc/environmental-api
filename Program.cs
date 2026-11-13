using EnvironmentAlerts.Api.Data;
using EnvironmentAlerts.Api.Endpoints;
using EnvironmentAlerts.Api.Services;
using EnvironmentAlerts.Api.Services.Soap;
using EnvironmentAlerts.Api.Models.Seed;
using SoapCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Environmental Alerts API",
        Version = "v1",
        Description = "API REST para gestão de localizações, leituras ambientais e alertas. Inclui integração com Open-Meteo, persistência em Azure SQL Database e segurança por API Key."    
    });

    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key necessária para operações protegidas. Usar o header: X-API-Key",
        Name = "X-API-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("ApiKey", document),
            new List<string>()
        }
    });
});

builder.Services.AddDbContext<EnvironmentalAlertsDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddScoped<AlertService>();

builder.Services.AddSoapCore();

builder.Services.AddScoped<IDataLayerSoapService, DataLayerSoapService>();

builder.Services.AddHttpClient<OpenMeteoService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientDemo", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateAsyncScope())
{
    var services = scope.ServiceProvider;
    
    try
    {
        var db = services.GetRequiredService<EnvironmentalAlertsDbContext>();
        await db.Database.MigrateAsync();
        await LocationSeeder.SeedAsync(db);
    }
    catch (Exception)
    {
        throw;
    }
}


app.MapOpenApi();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Environmental Alerts API v1");
    options.RoutePrefix = "swagger";
});


app.UseHttpsRedirection();

app.UseCors("ClientDemo");

app.MapGet("/", () => "API is running!")
    .WithName("HealthCheck")
    .WithTags("Health")
    .WithSummary("Verifica se a API está a funcionar")
    .WithDescription("Endpoint simples para confirmar que a API está online.")
    .Produces<string>(StatusCodes.Status200OK);

app.MapLocationsEndpoints();
app.MapReadingsEndpoints();
app.MapAlertsEndpoints();

((IApplicationBuilder)app).UseSoapEndpoint<IDataLayerSoapService>(
    "/soap/datalayer.asmx",
    new SoapEncoderOptions(),
    SoapSerializer.XmlSerializer
);

app.Run();
