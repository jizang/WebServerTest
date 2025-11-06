namespace WebServer.Models
{
    public class WeatherModels
    {
        public class WeatherStation
        {
            public string County { get; set; }
            public string LowlandStationId { get; set; }
            public string MountainStationId { get; set; }
        }

        public class DetectStationsConfig
        {
            public List<WeatherStation> WeatherStations { get; set; }
        }
    }

}
