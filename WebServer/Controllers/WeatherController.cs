using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WebServer.Models;
using static WebServer.Models.WeatherModels;

namespace WebServer.Controllers
{
    [ApiController]
    [Route("[controller]")] //Setting Route
    public class WeatherController : ControllerBase
    {
        private readonly DetectStationsConfig _stations;
        private readonly string _authCode;
        private readonly IHttpClientFactory _httpClientFactory;

        public WeatherController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _stations = configuration.GetSection("DetectStations").Get<DetectStationsConfig>();
            _authCode = configuration["CwaApi:Authorization"];
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> GetWeatherData()
        {
            var allData = new List<object>();
            foreach (var county in _stations.WeatherStations)
            {
                var json = await GetWeatherDataAsync(county.LowlandStationId, county.MountainStationId, _authCode);
                // 將 JSON 字串轉成 JsonElement
                var dataObj = JsonSerializer.Deserialize<JsonElement>(json);

                // 呼叫寫入資料庫
                await SaveWeatherToDbAsync(county.County, dataObj);

                allData.Add(new
                {
                    County = county.County,
                    WeatherData = dataObj
                });
            }
            return Ok(new { message = "資料已寫入資料庫", data = allData });
        }

        public async Task<string> GetWeatherDataAsync(string lowlandId, string mountainId, string authCode)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("CWA");
                var stationIds = $"{lowlandId},{mountainId}";
                var url = $"api/v1/rest/datastore/O-A0001-001?Authorization={authCode}&StationId={stationIds}";

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"HTTP 請求失敗: {ex.Message}", ex);
            }
        }
        
        private async Task SaveWeatherToDbAsync(string county, JsonElement dataObj)
        {
            try
            {
                // 檢查 success 是否為 true
                if (!dataObj.TryGetProperty("success", out var successElement) ||
                    successElement.GetString() != "true")
                {
                    return;
                }

                // 取得 records.Station 陣列
                if (!dataObj.TryGetProperty("records", out var records) ||
                    !records.TryGetProperty("Station", out var stations))
                {
                    return;
                }

                var weatherObservations = new List<WeatherObservation>();

                // 遍歷每個測站資料
                foreach (var station in stations.EnumerateArray())
                {
                    var observation = new WeatherObservation
                    {
                        // 基本資訊
                        StationId = GetJsonString(station, "StationId"),
                        StationName = GetJsonString(station, "StationName"),

                        // 地理資訊 - 使用 WGS84 座標（第二組）
                        CountyName = GetNestedJsonString(station, "GeoInfo", "CountyName"),
                        TownName = GetNestedJsonString(station, "GeoInfo", "TownName"),
                        Altitude = GetNestedJsonDouble(station, "GeoInfo", "StationAltitude"),

                        // 觀測時間
                        ObservationTime = GetNestedDateTime(station, "ObsTime", "DateTime"),

                        // 氣象資料
                        Weather = GetNestedJsonString(station, "WeatherElement", "Weather"),
                        WindDirection = GetNestedJsonDouble(station, "WeatherElement", "WindDirection"),
                        WindSpeed = GetNestedJsonDouble(station, "WeatherElement", "WindSpeed"),
                        AirTemperature = GetNestedJsonDouble(station, "WeatherElement", "AirTemperature"),
                        RelativeHumidity = GetNestedJsonDouble(station, "WeatherElement", "RelativeHumidity"),
                        AirPressure = GetNestedJsonDouble(station, "WeatherElement", "AirPressure"),

                        // 資料接收時間
                        DataReceivedTime = DateTime.Now
                    };

                    // 處理座標（使用 WGS84 - 第二組座標）
                    if (station.TryGetProperty("GeoInfo", out var geoInfo) &&
                        geoInfo.TryGetProperty("Coordinates", out var coordinates))
                    {
                        var coordinatesArray = coordinates.EnumerateArray().ToList();
                        if (coordinatesArray.Count > 1)
                        {
                            var wgs84 = coordinatesArray[1]; // 取得 WGS84 座標
                            observation.Latitude = GetJsonDouble(wgs84, "StationLatitude");
                            observation.Longitude = GetJsonDouble(wgs84, "StationLongitude");
                        }
                    }

                    // 處理降雨量（在 Now 物件內）
                    if (station.TryGetProperty("WeatherElement", out var weatherElement) &&
                        weatherElement.TryGetProperty("Now", out var now))
                    {
                        observation.Precipitation = GetJsonDouble(now, "Precipitation");
                    }

                    // 處理陣風資訊
                    if (station.TryGetProperty("WeatherElement", out weatherElement) &&
                        weatherElement.TryGetProperty("GustInfo", out var gustInfo))
                    {
                        var peakGustSpeed = GetJsonDouble(gustInfo, "PeakGustSpeed");
                        observation.GustSpeed = (peakGustSpeed == -99) ? null : peakGustSpeed;

                        if (gustInfo.TryGetProperty("Occurred_at", out var occurredAt))
                        {
                            var gustDirection = GetJsonDouble(occurredAt, "WindDirection");
                            observation.GustDirection = (gustDirection == -99) ? null : gustDirection;

                            var gustTimeStr = GetJsonString(occurredAt, "DateTime");
                            if (gustTimeStr != "-99" && DateTime.TryParse(gustTimeStr, out var gustTime))
                            {
                                observation.GustTime = gustTime;
                            }
                        }
                    }

                    // 處理每日極值溫度
                    if (station.TryGetProperty("WeatherElement", out weatherElement) &&
                        weatherElement.TryGetProperty("DailyExtreme", out var dailyExtreme))
                    {
                        // 最高溫
                        if (dailyExtreme.TryGetProperty("DailyHigh", out var dailyHigh) &&
                            dailyHigh.TryGetProperty("TemperatureInfo", out var highTempInfo))
                        {
                            observation.DailyHighTemp = GetJsonDouble(highTempInfo, "AirTemperature");

                            if (highTempInfo.TryGetProperty("Occurred_at", out var highOccurred))
                            {
                                observation.DailyHighTime = GetDateTime(highOccurred, "DateTime");
                            }
                        }

                        // 最低溫
                        if (dailyExtreme.TryGetProperty("DailyLow", out var dailyLow) &&
                            dailyLow.TryGetProperty("TemperatureInfo", out var lowTempInfo))
                        {
                            observation.DailyLowTemp = GetJsonDouble(lowTempInfo, "AirTemperature");

                            if (lowTempInfo.TryGetProperty("Occurred_at", out var lowOccurred))
                            {
                                observation.DailyLowTime = GetDateTime(lowOccurred, "DateTime");
                            }
                        }
                    }

                    weatherObservations.Add(observation);
                }

                // 寫入資料庫
                if (weatherObservations.Any())
                {
                    var _context = new AIoTDbContext();
                    _context.WeatherObservations.AddRange(weatherObservations);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
            }
        }

        // 輔助方法
        private string? GetJsonString(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property))
            {
                return property.GetString();
            }
            return null;
        }

        private double? GetJsonDouble(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property))
            {
                var stringValue = property.GetString();
                if (!string.IsNullOrEmpty(stringValue) && double.TryParse(stringValue, out var result))
                {
                    return result;
                }
            }
            return null;
        }

        private string? GetNestedJsonString(JsonElement element, string parentProperty, string childProperty)
        {
            if (element.TryGetProperty(parentProperty, out var parent))
            {
                return GetJsonString(parent, childProperty);
            }
            return null;
        }

        private double? GetNestedJsonDouble(JsonElement element, string parentProperty, string childProperty)
        {
            if (element.TryGetProperty(parentProperty, out var parent))
            {
                return GetJsonDouble(parent, childProperty);
            }
            return null;
        }

        private DateTime? GetDateTime(JsonElement element, string propertyName)
        {
            var dateString = GetJsonString(element, propertyName);
            if (!string.IsNullOrEmpty(dateString) && DateTime.TryParse(dateString, out var result))
            {
                return result;
            }
            return null;
        }

        private DateTime? GetNestedDateTime(JsonElement element, string parentProperty, string childProperty)
        {
            if (element.TryGetProperty(parentProperty, out var parent))
            {
                return GetDateTime(parent, childProperty);
            }
            return null;
        }

    }
}
