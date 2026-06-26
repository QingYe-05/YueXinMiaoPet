using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using YueXinMiaoPet.Models;

namespace YueXinMiaoPet.Services
{
    public class WeatherService
    {
        private readonly HttpClient _httpClient;

        public WeatherService()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public async Task<WeatherInfo> UpdateWeatherAsync(AppConfig config)
        {
            if (config == null || !config.WeatherEnabled)
            {
                return config != null && config.LastWeatherCache != null ? config.LastWeatherCache : WeatherInfo.Unknown();
            }

            try
            {
                string url = string.Format(
                    CultureInfo.InvariantCulture,
                    "https://api.open-meteo.com/v1/forecast?latitude={0}&longitude={1}&current_weather=true&timezone=auto",
                    config.Latitude,
                    config.Longitude);

                string json = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
                OpenMeteoResponse response = Deserialize(json);
                if (response == null || response.CurrentWeather == null)
                {
                    throw new InvalidOperationException("天气接口返回为空。");
                }

                WeatherInfo info = new WeatherInfo
                {
                    Temperature = response.CurrentWeather.Temperature,
                    WeatherCode = response.CurrentWeather.WeatherCode,
                    WeatherTag = MapWeatherTag(response.CurrentWeather.WeatherCode, response.CurrentWeather.Temperature),
                    UpdatedAtUtc = DateTime.UtcNow.ToString("o"),
                    Source = "open-meteo"
                };

                LogService.Info("天气更新成功：" + info.WeatherTag + " " + info.Temperature.ToString("0.0") + "℃");
                return info;
            }
            catch (Exception ex)
            {
                LogService.Error("天气更新失败，使用缓存天气。", ex);
                if (config.LastWeatherCache != null)
                {
                    return config.LastWeatherCache;
                }

                if (config.LastWeather != null)
                {
                    return config.LastWeather;
                }

                return WeatherInfo.Unknown();
            }
        }

        public string MapWeatherTag(int weatherCode, double temperature)
        {
            string tag;
            if (weatherCode == 0)
            {
                tag = "sunny";
            }
            else if ((weatherCode >= 1 && weatherCode <= 3) || weatherCode == 45 || weatherCode == 48)
            {
                tag = "cloudy";
            }
            else if ((weatherCode >= 51 && weatherCode <= 67) || (weatherCode >= 80 && weatherCode <= 82))
            {
                tag = "rain";
            }
            else if ((weatherCode >= 71 && weatherCode <= 77) || weatherCode == 85 || weatherCode == 86)
            {
                tag = "snow";
            }
            else if (weatherCode >= 95 && weatherCode <= 99)
            {
                tag = "thunder";
            }
            else
            {
                tag = "unknown";
            }

            if (temperature >= 32)
            {
                return "hot";
            }

            if (temperature <= 10)
            {
                return "cold";
            }

            return tag;
        }

        private OpenMeteoResponse Deserialize(string json)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(OpenMeteoResponse));
                return serializer.ReadObject(stream) as OpenMeteoResponse;
            }
        }

        [DataContract]
        private class OpenMeteoResponse
        {
            [DataMember(Name = "current_weather")]
            public OpenMeteoCurrentWeather CurrentWeather { get; set; }
        }

        [DataContract]
        private class OpenMeteoCurrentWeather
        {
            [DataMember(Name = "temperature")]
            public double Temperature { get; set; }

            [DataMember(Name = "weathercode")]
            public int WeatherCode { get; set; }
        }
    }
}
