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
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 YueXinMiaoPet/2.1");
        }

        public async Task<WeatherInfo> UpdateWeatherAsync(AppConfig config)
        {
            if (config == null || !config.WeatherEnabled)
            {
                return config != null && config.LastWeatherCache != null ? config.LastWeatherCache : WeatherInfo.Unknown();
            }

            try
            {
                WeatherInfo amap = await UpdateAmapWeatherAsync(config).ConfigureAwait(false);
                if (amap != null)
                {
                    SaveRegionalCache(config, amap);
                    LogWeatherResult("高德地图天气实况", amap);
                    return amap;
                }

                throw new InvalidOperationException("高德地图天气接口未返回有效数据。");
            }
            catch (Exception ex)
            {
                LogService.Error("高德地图天气不可用，使用当前行政区缓存。", ex);
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

        private async Task<WeatherInfo> UpdateAmapWeatherAsync(AppConfig config)
        {
            string apiKey = ResolveAmapApiKey(config);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("未配置高德地图 Web 服务 API Key。");
            }
            string administrativeCode = GetWeatherQueryCode(config);
            if (string.IsNullOrWhiteSpace(administrativeCode))
            {
                throw new InvalidOperationException("当前行政区代码为空。");
            }

            string url = "https://restapi.amap.com/v3/weather/weatherInfo?city=" +
                Uri.EscapeDataString(administrativeCode) + "&extensions=base&output=JSON&key=" +
                Uri.EscapeDataString(apiKey);
            AmapWeatherResponse response = DeserializeAmapWeather(await _httpClient.GetStringAsync(url).ConfigureAwait(false));
            if (response == null || !string.Equals(response.Status, "1", StringComparison.Ordinal) ||
                !string.Equals(response.InfoCode, "10000", StringComparison.Ordinal))
            {
                string infoCode = response == null ? "无响应" : CleanText(response.InfoCode);
                string info = response == null ? string.Empty : CleanText(response.Info);
                throw new InvalidOperationException("高德天气接口返回失败：" + infoCode +
                    (string.IsNullOrWhiteSpace(info) ? string.Empty : " / " + info));
            }
            if (response.Lives == null || response.Lives.Count == 0)
            {
                throw new InvalidOperationException("高德天气接口未返回实况数据。");
            }
            AmapWeatherLive live = response.Lives.Find(item => item != null &&
                string.Equals(item.AdministrativeCode, administrativeCode, StringComparison.OrdinalIgnoreCase)) ?? response.Lives[0];
            double temperature;
            if (live == null || string.IsNullOrWhiteSpace(live.Weather) ||
                !double.TryParse(live.Temperature, NumberStyles.Float, CultureInfo.InvariantCulture, out temperature))
            {
                throw new InvalidOperationException("高德天气实况缺少天气状况或温度。");
            }
            DateTime updatedAtUtc = ParseAmapReportTimeUtc(live.ReportTime);
            if (updatedAtUtc != DateTime.MinValue && DateTime.UtcNow - updatedAtUtc > TimeSpan.FromHours(3))
            {
                throw new InvalidOperationException("高德天气实况已超过 3 小时：" + updatedAtUtc.ToString("o"));
            }
            string weatherText = NormalizeAmapWeatherText(live.Weather);
            return new WeatherInfo
            {
                Temperature = temperature,
                WeatherCode = MapAmapWeatherCode(weatherText),
                WeatherTag = MapAmapWeatherTag(weatherText, temperature),
                WeatherText = weatherText,
                WindDirection = NormalizeAmapWindDirection(live.WindDirection),
                WindLevel = NormalizeAmapWindPower(live.WindPower),
                WindSpeed = null,
                UpdatedAtUtc = (updatedAtUtc == DateTime.MinValue ? DateTime.UtcNow : updatedAtUtc).ToString("o"),
                Source = "amap",
                AdministrativeCode = administrativeCode
            };
        }

        private void SaveRegionalCache(AppConfig config, WeatherInfo info)
        {
            string cacheKey = GetWeatherQueryCode(config);
            if (!string.IsNullOrWhiteSpace(cacheKey) && config.WeatherCaches != null && info != null)
            {
                config.WeatherCaches[cacheKey] = info;
            }
        }

        private void LogWeatherResult(string provider, WeatherInfo info)
        {
            LogService.Info(provider + "更新成功：Source=" + info.Source +
                "，UpdatedAtUtc=" + info.UpdatedAtUtc +
                "，WeatherCode=" + info.WeatherCode + "，WeatherTag=" + info.WeatherTag +
                "，WeatherText=" + info.WeatherText +
                "，Temperature=" + info.Temperature.ToString("0.0", CultureInfo.InvariantCulture) + "℃" +
                "，WindDirection=" + info.WindDirection + "，WindLevel=" + info.WindLevel +
                "，WindSpeed=" + (info.WindSpeed.HasValue ? info.WindSpeed.Value.ToString("0.#", CultureInfo.InvariantCulture) + "km/h" : "无"));
        }

        private string CleanText(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "暂无实况", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "null", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "undefined", StringComparison.OrdinalIgnoreCase)) return string.Empty;
            return value.Trim();
        }

        private string ResolveAmapApiKey(AppConfig config)
        {
            if (config != null && !string.IsNullOrWhiteSpace(config.AmapWeatherApiKey)) return config.AmapWeatherApiKey.Trim();
            string environmentKey = Environment.GetEnvironmentVariable("YUEXINMIAO_AMAP_KEY");
            if (!string.IsNullOrWhiteSpace(environmentKey)) return environmentKey.Trim();

            string[] paths =
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PetAssets", "amap.key"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YueXinMiaoPet", "amap.key")
            };
            foreach (string path in paths)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        string key = File.ReadAllText(path, Encoding.UTF8).Trim();
                        if (!string.IsNullOrWhiteSpace(key)) return key;
                    }
                }
                catch (Exception ex)
                {
                    LogService.Warn("读取高德天气 Key 文件失败：" + Path.GetFileName(path) + "，" + ex.Message);
                }
            }
            return string.Empty;
        }

        private DateTime ParseAmapReportTimeUtc(string value)
        {
            DateTime local;
            if (!DateTime.TryParseExact(CleanText(value), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out local)) return DateTime.MinValue;
            return DateTime.SpecifyKind(local.AddHours(-8), DateTimeKind.Utc);
        }

        private string NormalizeAmapWeatherText(string value)
        {
            string text = CleanText(value);
            if (text.Contains("雷阵雨") || text.Contains("雷雨")) return "雷阵雨";
            if (text.Contains("暴雨")) return "暴雨";
            if (text.Contains("大雨")) return "大雨";
            if (text.Contains("中雨")) return "中雨";
            if (text.Contains("小雨") || text.Contains("阵雨") || text.Contains("毛毛雨") || text.Contains("细雨") || text == "雨") return "小雨";
            // 其余高德天气现象（如晴间多云、浓雾、中度霾）保留原文显示。
            return text;
        }

        private int MapAmapWeatherCode(string normalized)
        {
            if (normalized.Contains("晴") || normalized.Contains("少云")) return 0;
            if (normalized.Contains("多云")) return 2;
            if (normalized.Contains("阴")) return 3;
            if (normalized.Contains("雾") || normalized.Contains("霾")) return 45;
            if (normalized == "雷阵雨") return 95;
            if (normalized == "暴雨") return 82;
            if (normalized == "大雨") return 65;
            if (normalized == "中雨") return 63;
            if (normalized == "小雨") return 61;
            if (normalized.Contains("雪")) return 71;
            return -1;
        }

        private string MapAmapWeatherTag(string normalized, double temperature)
        {
            if (temperature >= 32) return "hot";
            if (temperature <= 10) return "cold";
            if (normalized.Contains("晴") || normalized.Contains("少云")) return "sunny";
            if (normalized.Contains("多云") || normalized.Contains("阴") || normalized.Contains("雾") ||
                normalized.Contains("霾") || normalized.Contains("沙") || normalized.Contains("尘")) return "cloudy";
            if (normalized.Contains("雨")) return normalized == "雷阵雨" ? "thunder" : "rain";
            if (normalized.Contains("雪")) return "snow";
            return "unknown";
        }

        private string NormalizeAmapWindDirection(string value)
        {
            string text = CleanText(value);
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            if (text == "无风向" || text == "旋转不定") return text;
            return text.EndsWith("风", StringComparison.Ordinal) ? text : text + "风";
        }

        private string NormalizeAmapWindPower(string value)
        {
            string text = CleanText(value);
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            return text.EndsWith("级", StringComparison.Ordinal) ? text : text + "级";
        }

        private string GetWeatherQueryCode(AppConfig config)
        {
            if (config == null) return string.Empty;
            if (!string.IsNullOrWhiteSpace(config.CountyCode)) return config.CountyCode;
            if (!string.IsNullOrWhiteSpace(config.CityCode)) return config.CityCode;
            return config.ProvinceCode ?? string.Empty;
        }

        private AmapWeatherResponse DeserializeAmapWeather(string json)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AmapWeatherResponse));
                return serializer.ReadObject(stream) as AmapWeatherResponse;
            }
        }

        [DataContract]
        private class AmapWeatherResponse
        {
            [DataMember(Name = "status")]
            public string Status { get; set; }

            [DataMember(Name = "info")]
            public string Info { get; set; }

            [DataMember(Name = "infocode")]
            public string InfoCode { get; set; }

            [DataMember(Name = "lives")]
            public List<AmapWeatherLive> Lives { get; set; }
        }

        [DataContract]
        private class AmapWeatherLive
        {
            [DataMember(Name = "province")]
            public string Province { get; set; }

            [DataMember(Name = "city")]
            public string City { get; set; }

            [DataMember(Name = "adcode")]
            public string AdministrativeCode { get; set; }

            [DataMember(Name = "weather")]
            public string Weather { get; set; }

            [DataMember(Name = "temperature")]
            public string Temperature { get; set; }

            [DataMember(Name = "winddirection")]
            public string WindDirection { get; set; }

            [DataMember(Name = "windpower")]
            public string WindPower { get; set; }

            [DataMember(Name = "reporttime")]
            public string ReportTime { get; set; }
        }

    }
}
