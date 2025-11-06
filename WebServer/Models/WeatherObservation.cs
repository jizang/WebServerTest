using System;
using System.Collections.Generic;

namespace WebServer.Models;

public partial class WeatherObservation
{
    public int Id { get; set; }

    public string? StationId { get; set; }

    public string? StationName { get; set; }

    public string? CountyName { get; set; }

    public string? TownName { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public double? Altitude { get; set; }

    public DateTime? ObservationTime { get; set; }

    public string? Weather { get; set; }

    public double? Precipitation { get; set; }

    public double? WindDirection { get; set; }

    public double? WindSpeed { get; set; }

    public double? AirTemperature { get; set; }

    public double? RelativeHumidity { get; set; }

    public double? AirPressure { get; set; }

    public double? GustSpeed { get; set; }

    public double? GustDirection { get; set; }

    public DateTime? GustTime { get; set; }

    public double? DailyHighTemp { get; set; }

    public DateTime? DailyHighTime { get; set; }

    public double? DailyLowTemp { get; set; }

    public DateTime? DailyLowTime { get; set; }

    public DateTime? DataReceivedTime { get; set; }
}
