using System;
using System.Runtime.Serialization;

namespace YueXinMiaoPet.Models
{
    [DataContract]
    public class WeatherInfo
    {
        [DataMember(Name = "weatherTag")]
        public string WeatherTag { get; set; }

        [DataMember(Name = "temperature")]
        public double Temperature { get; set; }

        [DataMember(Name = "weatherCode")]
        public int WeatherCode { get; set; }

        [DataMember(Name = "updatedAtUtc")]
        public string UpdatedAtUtc { get; set; }

        [DataMember(Name = "source")]
        public string Source { get; set; }

        public WeatherInfo()
        {
            WeatherTag = "unknown";
            Temperature = 0;
            WeatherCode = -1;
            UpdatedAtUtc = DateTime.UtcNow.ToString("o");
            Source = "none";
        }

        public static WeatherInfo Unknown()
        {
            return new WeatherInfo
            {
                WeatherTag = "unknown",
                Temperature = 0,
                WeatherCode = -1,
                UpdatedAtUtc = DateTime.UtcNow.ToString("o"),
                Source = "fallback"
            };
        }
    }
}
