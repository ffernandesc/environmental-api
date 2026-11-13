using EnvironmentAlerts.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EnvironmentAlerts.Api.Data;

public class EnvironmentalAlertsDbContext : DbContext
{
    public EnvironmentalAlertsDbContext(DbContextOptions<EnvironmentalAlertsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Location> Locations { get; set; }

    public DbSet<Reading> Readings { get; set; }

    public DbSet<Alert> Alerts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Location>()
            .HasMany(location => location.Readings)
            .WithOne(reading => reading.Location)
            .HasForeignKey(reading => reading.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Location>()
            .HasMany(location => location.Alerts)
            .WithOne(alert => alert.Location)
            .HasForeignKey(alert => alert.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Reading>()
            .HasMany(reading => reading.Alerts)
            .WithOne(alert => alert.Reading)
            .HasForeignKey(alert => alert.ReadingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}