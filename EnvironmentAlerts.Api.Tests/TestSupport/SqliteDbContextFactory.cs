using EnvironmentAlerts.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EnvironmentAlerts.Api.Tests.TestSupport;

public sealed class SqliteDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<EnvironmentalAlertsDbContext> _options;

    public SqliteDbContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<EnvironmentalAlertsDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public EnvironmentalAlertsDbContext CreateContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}
