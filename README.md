# Environmental Alerts API

Practical assignment for the Internet and Distributed Systems course — 2025/2026.

This application is an API for managing locations, environmental readings and alerts.

It was built with .NET 10 and exposes both REST endpoints and a SOAP service.

## Technologies

* .NET 10
* Entity Framework Core
* SQL Server / Azure SQL Database
* SoapCore
* xUnit
* Open-Meteo API

## Features

The application allows you to:

* create, retrieve, update and delete locations;
* create and retrieve environmental readings;
* import current weather data from Open-Meteo;
* automatically generate alerts based on reading values;
* query locations and alerts through SOAP;
* protect write endpoints with an API key.

## Project structure

```text
EnvironmentAlerts.Api/
├── Data/
├── Models/
├── Endpoints/
├── Services/
├── Migrations/
├── ClientDemo/
└── EnvironmentAlerts.Api.Tests/
```

## Configuration

The application requires the following settings:

```text
ConnectionStrings:DefaultConnection
ApiKey
```

Do not commit real values. Provide them through [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)
during development, or through environment variables in other environments
(for example `ConnectionStrings__DefaultConnection` and `ApiKey`).

## Database

The application uses Entity Framework Core with SQL Server (originally hosted on Azure SQL Database).

On startup, pending migrations are applied:

```csharp
await db.Database.MigrateAsync();
```

A seeder also runs, reading the file:

```text
Data/Seed/locations.json
```

and adding only the locations that do not yet exist in the database.

## Authentication

`GET` endpoints are public.

`POST`, `PUT` and `DELETE` endpoints require the header:

```text
X-API-Key: <key>
```

## Main endpoints

### Locations

```text
GET    /api/locations
GET    /api/locations/{id}
POST   /api/locations
PUT    /api/locations/{id}
DELETE /api/locations/{id}
```

### Readings

```text
GET    /api/readings
GET    /api/readings/{id}
POST   /api/readings
PUT    /api/readings/{id}
DELETE /api/readings/{id}
POST   /api/readings/import-current/{locationId}
```

### Alerts

```text
GET    /api/alerts
GET    /api/alerts/{id}
POST   /api/alerts
PUT    /api/alerts/{id}
DELETE /api/alerts/{id}
```

### SOAP

```text
/soap/datalayer.asmx
```

WSDL:

```text
/soap/datalayer.asmx?wsdl
```

Available operations:

* `GetLocations`
* `GetLocationById`
* `GetAlerts`
* `GetAlertsByLocation`

## Location example

```json
{
  "city": "Braga",
  "district": "Braga",
  "latitude": 41.5454,
  "longitude": -8.4265
}
```

## Reading example

```json
{
  "locationId": 1,
  "dateTime": "2026-07-09T12:00:00Z",
  "temperature": 28.5,
  "humidity": 55,
  "windSpeed": 18,
  "precipitation": 0,
  "pm10": 20,
  "pm25": 12,
  "europeanAqi": 35
}
```

## Tests

To run the tests:

```bash
dotnet test
```

The tests use xUnit and an in-memory SQLite database.

## Deployment

The API was originally deployed to Azure App Service; that deployment is no longer online.

To build a production version:

```bash
dotnet publish EnvironmentAlerts.Api.csproj -c Release -o ./publish_output
```
