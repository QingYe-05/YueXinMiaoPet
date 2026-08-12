using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Collections.Generic;
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
                WeatherLocationCacheEntry location = await ResolveLocationAsync(config).ConfigureAwait(false);
                if (location == null)
                {
                    throw new InvalidOperationException("无法解析当前行政区天气位置，请重新选择天气地区。");
                }

                string url = string.Format(
                    CultureInfo.InvariantCulture,
                    "https://api.open-meteo.com/v1/forecast?latitude={0}&longitude={1}&current_weather=true&timezone=auto",
                    location.Latitude,
                    location.Longitude);

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
                    WeatherText = MapWeatherText(response.CurrentWeather.WeatherCode),
                    WindDirection = MapWindDirection(response.CurrentWeather.WindDirection),
                    WindLevel = MapWindLevel(response.CurrentWeather.WindSpeed),
                    WindSpeed = response.CurrentWeather.WindSpeed,
                    UpdatedAtUtc = DateTime.UtcNow.ToString("o"),
                    Source = "open-meteo",
                    AdministrativeCode = GetWeatherQueryCode(config)
                };

                string cacheKey = GetWeatherQueryCode(config);
                if (!string.IsNullOrWhiteSpace(cacheKey))
                {
                    config.WeatherCaches[cacheKey] = info;
                }

                LogService.Info(
                    "天气更新成功：WeatherCode=" + info.WeatherCode +
                    "，WeatherTag=" + info.WeatherTag +
                    "，WeatherText=" + info.WeatherText +
                    "，Temperature=" + info.Temperature.ToString("0.0", CultureInfo.InvariantCulture) + "℃" +
                    "，WindDirection=" + info.WindDirection +
                    "，WindLevel=" + info.WindLevel +
                    "，WindSpeed=" + (info.WindSpeed.HasValue ? info.WindSpeed.Value.ToString("0.#", CultureInfo.InvariantCulture) + "km/h" : "无"));
                return info;
            }
            catch (Exception ex)
            {
                LogService.Error("天气更新失败，使用缓存天气。", ex);
                string cacheKey = GetWeatherQueryCode(config);
                WeatherInfo regionalCache;
                if (!string.IsNullOrWhiteSpace(cacheKey) && config.WeatherCaches != null && config.WeatherCaches.TryGetValue(cacheKey, out regionalCache))
                {
                    LogService.Warn("天气网络请求失败，使用当前行政区缓存：" + cacheKey);
                    return regionalCache;
                }

                return WeatherInfo.Unknown();
            }
        }

        private async Task<WeatherLocationCacheEntry> ResolveLocationAsync(AppConfig config)
        {
            string[] codes = { config.CountyCode, config.CityCode, config.ProvinceCode };
            string[] levels = { "CountyCode", "CityCode", "ProvinceCode" };
            for (int i = 0; i < codes.Length; i++)
            {
                string code = codes[i];
                if (string.IsNullOrWhiteSpace(code)) continue;

                WeatherLocationCacheEntry cached;
                if (config.WeatherLocationCaches != null && config.WeatherLocationCaches.TryGetValue(code, out cached))
                {
                    LogService.Info("WeatherQueryLevel=" + levels[i] + "，WeatherQueryLocation=" + code + "，WeatherLocationId=" + cached.WeatherLocationId);
                    return cached;
                }

                WeatherLocationCacheEntry resolved = await QueryAdministrativeLocationAsync(code).ConfigureAwait(false);
                if (resolved != null)
                {
                    if (config.WeatherLocationCaches == null)
                    {
                        config.WeatherLocationCaches = new Dictionary<string, WeatherLocationCacheEntry>(StringComparer.OrdinalIgnoreCase);
                    }
                    config.WeatherLocationCaches[code] = resolved;
                    LogService.Info("WeatherQueryLevel=" + levels[i] + "，WeatherQueryLocation=" + code + "，WeatherLocationId=" + resolved.WeatherLocationId);
                    return resolved;
                }
            }

            return null;
        }

        private async Task<WeatherLocationCacheEntry> QueryAdministrativeLocationAsync(string code)
        {
            try
            {
                string url = "https://uapis.cn/api/v1/misc/district?adcode=" + Uri.EscapeDataString(code) + "&limit=20";
                string json = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
                DistrictResponse response = DeserializeDistrict(json);
                if (response == null || response.Results == null) return null;

                DistrictResult exact = response.Results.Find(item =>
                    item != null && string.Equals(item.AdministrativeCode, code, StringComparison.OrdinalIgnoreCase) && item.Center != null);
                if (exact == null) return null;
                return new WeatherLocationCacheEntry
                {
                    AdministrativeCode = code,
                    Latitude = exact.Center.Latitude,
                    Longitude = exact.Center.Longitude,
                    WeatherLocationId = "adcode:" + code,
                    UpdatedAtUtc = DateTime.UtcNow.ToString("o")
                };
            }
            catch (Exception ex)
            {
                LogService.Error("行政区代码定位失败：" + code, ex);
                return null;
            }
        }

        private string GetWeatherQueryCode(AppConfig config)
        {
            if (config == null) return string.Empty;
            if (!string.IsNullOrWhiteSpace(config.CountyCode)) return config.CountyCode;
            if (!string.IsNullOrWhiteSpace(config.CityCode)) return config.CityCode;
            return config.ProvinceCode ?? string.Empty;
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

        /// <summary>
        /// 将 Open-Meteo 的 WMO 天气码转换为中文显示文本。雨量按天气码细分，
        /// 但 WeatherTag 仍保持原有 rain/thunder 等值，避免影响 GIF 联动逻辑。
        /// </summary>
        public string MapWeatherText(int weatherCode)
        {
            if (weatherCode == 0 || weatherCode == 1) return "晴";
            if (weatherCode == 2) return "多云";
            if (weatherCode == 3) return "阴";
            if (weatherCode == 45 || weatherCode == 48) return "雾";

            // 雷阵雨优先于所有普通雨级别。
            if (weatherCode >= 95 && weatherCode <= 99) return "雷阵雨";
            if (weatherCode == 82) return "暴雨";
            if (weatherCode == 55 || weatherCode == 57 || weatherCode == 65 || weatherCode == 67) return "大雨";
            if (weatherCode == 53 || weatherCode == 63 || weatherCode == 81) return "中雨";
            if (weatherCode == 51 || weatherCode == 56 || weatherCode == 61 || weatherCode == 66 || weatherCode == 80) return "小雨";

            if ((weatherCode >= 71 && weatherCode <= 77) || weatherCode == 85 || weatherCode == 86) return "雪";
            return "未知";
        }

        /// <summary>
        /// 将气象风向角度转换为八方位中文名称。
        /// </summary>
        public string MapWindDirection(double? directionDegrees)
        {
            if (!directionDegrees.HasValue || double.IsNaN(directionDegrees.Value) || double.IsInfinity(directionDegrees.Value))
            {
                return string.Empty;
            }

            double normalized = directionDegrees.Value % 360;
            if (normalized < 0) normalized += 360;
            string[] directions = { "北风", "东北风", "东风", "东南风", "南风", "西南风", "西风", "西北风" };
            int index = (int)Math.Floor((normalized + 22.5) / 45.0) % directions.Length;
            return directions[index];
        }

        /// <summary>
        /// Open-Meteo 当前天气风速单位为 km/h，这里按蒲福风级阈值换算显示级别。
        /// </summary>
        public string MapWindLevel(double? speedKilometersPerHour)
        {
            if (!speedKilometersPerHour.HasValue || speedKilometersPerHour.Value < 0 ||
                double.IsNaN(speedKilometersPerHour.Value) || double.IsInfinity(speedKilometersPerHour.Value))
            {
                return string.Empty;
            }

            double speed = speedKilometersPerHour.Value;
            double[] upperBounds = { 1, 6, 12, 20, 29, 39, 50, 62, 75, 89, 103, 118 };
            for (int level = 0; level < upperBounds.Length; level++)
            {
                if (speed < upperBounds[level]) return level + "级";
            }

            return "12级";
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

        private DistrictResponse DeserializeDistrict(string json)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(DistrictResponse));
                return serializer.ReadObject(stream) as DistrictResponse;
            }
        }

        [DataContract]
        private class DistrictResponse
        {
            [DataMember(Name = "results")]
            public List<DistrictResult> Results { get; set; }
        }

        [DataContract]
        private class DistrictResult
        {
            [DataMember(Name = "adcode")]
            public string AdministrativeCode { get; set; }

            [DataMember(Name = "center")]
            public DistrictCenter Center { get; set; }
        }

        [DataContract]
        private class DistrictCenter
        {
            [DataMember(Name = "lat")]
            public double Latitude { get; set; }

            [DataMember(Name = "lng")]
            public double Longitude { get; set; }
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

            [DataMember(Name = "windspeed")]
            public double? WindSpeed { get; set; }

            [DataMember(Name = "winddirection")]
            public double? WindDirection { get; set; }
        }
    }
}
