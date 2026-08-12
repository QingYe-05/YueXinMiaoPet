using System;
using System.Runtime.Serialization;

namespace YueXinMiaoPet.Models
{
    [DataContract]
    public class WeatherInfo
    {
        [DataMember(Name = "weatherTag")]
        public string WeatherTag { get; set; }

        [DataMember(Name = "weatherText")]
        public string WeatherText { get; set; }

        [DataMember(Name = "temperature")]
        public double Temperature { get; set; }

        [DataMember(Name = "weatherCode")]
        public int WeatherCode { get; set; }

        [DataMember(Name = "windDirection")]
        public string WindDirection { get; set; }

        [DataMember(Name = "windLevel")]
        public string WindLevel { get; set; }

        [DataMember(Name = "windSpeed")]
        public double? WindSpeed { get; set; }

        [DataMember(Name = "updatedAtUtc")]
        public string UpdatedAtUtc { get; set; }

        [DataMember(Name = "source")]
        public string Source { get; set; }

        [DataMember(Name = "administrativeCode")]
        public string AdministrativeCode { get; set; }

        public WeatherInfo()
        {
            WeatherTag = "unknown";
            WeatherText = string.Empty;
            Temperature = 0;
            WeatherCode = -1;
            WindDirection = string.Empty;
            WindLevel = string.Empty;
            WindSpeed = null;
            UpdatedAtUtc = DateTime.UtcNow.ToString("o");
            Source = "none";
            AdministrativeCode = string.Empty;
        }

        public static WeatherInfo Unknown()
        {
            return new WeatherInfo
            {
                WeatherTag = "unknown",
                WeatherText = string.Empty,
                Temperature = 0,
                WeatherCode = -1,
                WindDirection = string.Empty,
                WindLevel = string.Empty,
                WindSpeed = null,
                UpdatedAtUtc = DateTime.UtcNow.ToString("o"),
                Source = "fallback",
                AdministrativeCode = string.Empty
            };
        }
    }
}
