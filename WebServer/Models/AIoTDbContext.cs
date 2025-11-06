using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace WebServer.Models;

public partial class AIoTDbContext : DbContext
{
    public AIoTDbContext()
    {
    }

    public AIoTDbContext(DbContextOptions<AIoTDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<WeatherObservation> WeatherObservations { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlite("Data Source=C:\\Users\\C25\\Desktop\\AIoT2025\\AIoT_DB");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WeatherObservation>(entity =>
        {
            entity.ToTable("weather_observation");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AirPressure).HasColumnName("air_pressure");
            entity.Property(e => e.AirTemperature).HasColumnName("air_temperature");
            entity.Property(e => e.Altitude).HasColumnName("altitude");
            entity.Property(e => e.CountyName).HasColumnName("county_name");
            entity.Property(e => e.DailyHighTemp).HasColumnName("daily_high_temp");
            entity.Property(e => e.DailyHighTime)
                .HasColumnType("DATETIME")
                .HasColumnName("daily_high_time");
            entity.Property(e => e.DailyLowTemp).HasColumnName("daily_low_temp");
            entity.Property(e => e.DailyLowTime)
                .HasColumnType("DATETIME")
                .HasColumnName("daily_low_time");
            entity.Property(e => e.DataReceivedTime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("DATETIME")
                .HasColumnName("data_received_time");
            entity.Property(e => e.GustDirection).HasColumnName("gust_direction");
            entity.Property(e => e.GustSpeed).HasColumnName("gust_speed");
            entity.Property(e => e.GustTime)
                .HasColumnType("DATETIME")
                .HasColumnName("gust_time");
            entity.Property(e => e.Latitude).HasColumnName("latitude");
            entity.Property(e => e.Longitude).HasColumnName("longitude");
            entity.Property(e => e.ObservationTime)
                .HasColumnType("DATETIME")
                .HasColumnName("observation_time");
            entity.Property(e => e.Precipitation).HasColumnName("precipitation");
            entity.Property(e => e.RelativeHumidity).HasColumnName("relative_humidity");
            entity.Property(e => e.StationId).HasColumnName("station_id");
            entity.Property(e => e.StationName).HasColumnName("station_name");
            entity.Property(e => e.TownName).HasColumnName("town_name");
            entity.Property(e => e.Weather).HasColumnName("weather");
            entity.Property(e => e.WindDirection).HasColumnName("wind_direction");
            entity.Property(e => e.WindSpeed).HasColumnName("wind_speed");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
