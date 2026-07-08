using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace YueXinMiaoPet.Models
{
    [DataContract]
    public class AppConfig
    {
        [DataMember(Name = "WindowPositionX")]
        public double? WindowPositionX { get; set; }

        [DataMember(Name = "WindowPositionY")]
        public double? WindowPositionY { get; set; }

        [DataMember(Name = "ScalePercent")]
        public int ScalePercent { get; set; }

        [DataMember(Name = "OpacityPercent")]
        public int OpacityPercent { get; set; }

        [DataMember(Name = "AlwaysOnTop")]
        public bool AlwaysOnTop { get; set; }

        [DataMember(Name = "AutoStart")]
        public bool AutoStart { get; set; }

        [DataMember(Name = "MoodTag")]
        public string MoodTag { get; set; }

        [DataMember(Name = "MoodExpireMode")]
        public string MoodExpireMode { get; set; }

        [DataMember(Name = "MoodExpireAt")]
        public string MoodExpireAt { get; set; }

        [DataMember(Name = "LastMoodChangedAt")]
        public string LastMoodChangedAt { get; set; }

        [DataMember(Name = "WeatherEnabled")]
        public bool WeatherEnabled { get; set; }

        [DataMember(Name = "WeatherUpdateIntervalMinutes")]
        public int WeatherUpdateIntervalMinutes { get; set; }

        [DataMember(Name = "WeatherBubbleEnabled")]
        public bool WeatherBubbleEnabled { get; set; }

        [DataMember(Name = "WeatherAffectsGif")]
        public bool WeatherAffectsGif { get; set; }

        [DataMember(Name = "LastWeatherTag")]
        public string LastWeatherTag { get; set; }

        [DataMember(Name = "LastTemperature")]
        public double LastTemperature { get; set; }

        [DataMember(Name = "LastWeatherUpdateAt")]
        public string LastWeatherUpdateAt { get; set; }

        [DataMember(Name = "Province")]
        public string Province { get; set; }

        [DataMember(Name = "City")]
        public string City { get; set; }

        [DataMember(Name = "Latitude")]
        public double Latitude { get; set; }

        [DataMember(Name = "Longitude")]
        public double Longitude { get; set; }

        [DataMember(Name = "GifSourceMode")]
        public string GifSourceMode { get; set; }

        [DataMember(Name = "BuiltInClassifiedGifDirectory")]
        public string BuiltInClassifiedGifDirectory { get; set; }

        [DataMember(Name = "CustomGifDirectory")]
        public string CustomGifDirectory { get; set; }

        [DataMember(Name = "PreferClassifiedGifs")]
        public bool PreferClassifiedGifs { get; set; }

        [DataMember(Name = "UseBuiltInGifLibrary")]
        public bool UseBuiltInGifLibrary { get; set; }

        [DataMember(Name = "LastWeatherCache")]
        public WeatherInfo LastWeatherCache { get; set; }

        [DataMember(Name = "UseGlobalCustomPlaylist")]
        public bool UseGlobalCustomPlaylist { get; set; }

        [DataMember(Name = "GlobalCustomPlaylist")]
        public List<string> GlobalCustomPlaylist { get; set; }

        [DataMember(Name = "MoodCustomPlaylists")]
        public Dictionary<string, List<string>> MoodCustomPlaylists { get; set; }

        // 兼容旧版配置字段。保留这些字段可以让旧 config.json 自动升级。
        [DataMember(Name = "windowLeft")]
        public double? WindowLeft { get; set; }

        [DataMember(Name = "windowTop")]
        public double? WindowTop { get; set; }

        [DataMember(Name = "scale")]
        public double Scale { get; set; }

        [DataMember(Name = "opacity")]
        public double Opacity { get; set; }

        [DataMember(Name = "startWithWindows")]
        public bool StartWithWindows { get; set; }

        [DataMember(Name = "currentMood")]
        public string CurrentMood { get; set; }

        [DataMember(Name = "moodExpiresAtUtc")]
        public string MoodExpiresAtUtc { get; set; }

        [DataMember(Name = "city")]
        public string LegacyCity { get; set; }

        [DataMember(Name = "enableWeather")]
        public bool EnableWeather { get; set; }

        [DataMember(Name = "gifDirectory")]
        public string GifDirectory { get; set; }

        [DataMember(Name = "workStartTime")]
        public string WorkStartTime { get; set; }

        [DataMember(Name = "workEndTime")]
        public string WorkEndTime { get; set; }

        [DataMember(Name = "lunchStartTime")]
        public string LunchStartTime { get; set; }

        [DataMember(Name = "lunchEndTime")]
        public string LunchEndTime { get; set; }

        [DataMember(Name = "eveningStartTime")]
        public string EveningStartTime { get; set; }

        [DataMember(Name = "sleepTime")]
        public string SleepTime { get; set; }

        [DataMember(Name = "lastWeather")]
        public WeatherInfo LastWeather { get; set; }

        public AppConfig()
        {
            AlwaysOnTop = true;
            WeatherEnabled = false;
            EnableWeather = false;
            WeatherUpdateIntervalMinutes = 30;
            WeatherBubbleEnabled = false;
            WeatherAffectsGif = false;
            UseBuiltInGifLibrary = true;
            PreferClassifiedGifs = true;
            UseGlobalCustomPlaylist = false;
            GlobalCustomPlaylist = new List<string>();
            MoodCustomPlaylists = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            GifSourceMode = "BuiltInClassified";
            ScalePercent = 100;
            OpacityPercent = 100;
            Scale = 1.0;
            Opacity = 1.0;
            MoodTag = "neutral";
            CurrentMood = "neutral";
            MoodExpireMode = "forever";
            Province = "上海市";
            City = "上海市";
            LegacyCity = "上海市";
            Latitude = 31.2304;
            Longitude = 121.4737;
            BuiltInClassifiedGifDirectory = string.Empty;
            CustomGifDirectory = string.Empty;
            LastWeatherTag = "unknown";
            LastMoodChangedAt = DateTime.UtcNow.ToString("o");
            ApplyDefaults();
        }

        public void ApplyDefaults()
        {
            SyncWindowPosition();
            SyncAppearance();
            SyncMood();
            SyncWeather();
            SyncLocation();
            SyncGifSource();
            SyncPlaylists();
            SyncTimes();
        }

        private void SyncWindowPosition()
        {
            if (!WindowPositionX.HasValue && WindowLeft.HasValue) WindowPositionX = WindowLeft;
            if (!WindowPositionY.HasValue && WindowTop.HasValue) WindowPositionY = WindowTop;
            if (!WindowLeft.HasValue && WindowPositionX.HasValue) WindowLeft = WindowPositionX;
            if (!WindowTop.HasValue && WindowPositionY.HasValue) WindowTop = WindowPositionY;
        }

        private void SyncAppearance()
        {
            if (ScalePercent <= 0 && Scale > 0) ScalePercent = (int)Math.Round(Scale * 100.0);
            if (ScalePercent <= 0) ScalePercent = 100;
            ScalePercent = Clamp(ScalePercent, 50, 200);
            Scale = ScalePercent / 100.0;

            if (OpacityPercent <= 0 && Opacity > 0) OpacityPercent = (int)Math.Round(Opacity * 100.0);
            if (OpacityPercent <= 0) OpacityPercent = 100;
            OpacityPercent = Clamp(OpacityPercent, 30, 100);
            Opacity = OpacityPercent / 100.0;

            if (StartWithWindows) AutoStart = true;
            else StartWithWindows = AutoStart;
        }

        private void SyncMood()
        {
            if (string.IsNullOrWhiteSpace(MoodTag))
            {
                MoodTag = string.IsNullOrWhiteSpace(CurrentMood) ? "neutral" : CurrentMood;
            }

            MoodTag = YueXinMiaoPet.Services.MoodCategoryService.NormalizeMood(MoodTag);
            if (string.IsNullOrWhiteSpace(CurrentMood)) CurrentMood = MoodTag;
            CurrentMood = YueXinMiaoPet.Services.MoodCategoryService.NormalizeMood(CurrentMood);
            if (string.IsNullOrWhiteSpace(MoodExpireMode)) MoodExpireMode = "forever";

            if (string.IsNullOrWhiteSpace(MoodExpireAt) && !string.IsNullOrWhiteSpace(MoodExpiresAtUtc))
            {
                MoodExpireAt = MoodExpiresAtUtc;
            }

            if (string.IsNullOrWhiteSpace(MoodExpiresAtUtc) && !string.IsNullOrWhiteSpace(MoodExpireAt))
            {
                MoodExpiresAtUtc = MoodExpireAt;
            }

            if (string.IsNullOrWhiteSpace(LastMoodChangedAt))
            {
                LastMoodChangedAt = DateTime.UtcNow.ToString("o");
            }

            CurrentMood = MoodTag;
        }

        private void SyncWeather()
        {
            EnableWeather = WeatherEnabled;
            if (!WeatherEnabled)
            {
                WeatherBubbleEnabled = false;
            }

            if (WeatherUpdateIntervalMinutes <= 0) WeatherUpdateIntervalMinutes = 30;
            WeatherUpdateIntervalMinutes = Clamp(WeatherUpdateIntervalMinutes, 1, 120);

            if (LastWeatherCache == null && LastWeather != null) LastWeatherCache = LastWeather;
            if (LastWeatherCache == null) LastWeatherCache = WeatherInfo.Unknown();
            LastWeather = LastWeatherCache;

            if (string.IsNullOrWhiteSpace(LastWeatherTag)) LastWeatherTag = LastWeatherCache.WeatherTag;
            if (Math.Abs(LastTemperature) < 0.000001) LastTemperature = LastWeatherCache.Temperature;
            if (string.IsNullOrWhiteSpace(LastWeatherUpdateAt)) LastWeatherUpdateAt = LastWeatherCache.UpdatedAtUtc;
        }

        private void SyncLocation()
        {
            if (string.IsNullOrWhiteSpace(Province)) Province = "上海市";
            if (string.IsNullOrWhiteSpace(City)) City = string.IsNullOrWhiteSpace(LegacyCity) ? "上海市" : LegacyCity;
            LegacyCity = City;

            if (Math.Abs(Latitude) < 0.000001 && Math.Abs(Longitude) < 0.000001)
            {
                Latitude = 31.2304;
                Longitude = 121.4737;
            }
        }

        private void SyncGifSource()
        {
            if (string.IsNullOrWhiteSpace(BuiltInClassifiedGifDirectory))
            {
                BuiltInClassifiedGifDirectory = string.Empty;
            }

            if (string.IsNullOrWhiteSpace(GifSourceMode))
            {
                GifSourceMode = InferGifSourceMode();
            }

            if (string.Equals(GifSourceMode, "Custom", StringComparison.OrdinalIgnoreCase))
            {
                UseBuiltInGifLibrary = false;
                if (string.IsNullOrWhiteSpace(CustomGifDirectory) && !LooksLikeBuiltInPath(GifDirectory))
                {
                    CustomGifDirectory = GifDirectory;
                }
            }
            else if (string.Equals(GifSourceMode, "BuiltIn", StringComparison.OrdinalIgnoreCase))
            {
                UseBuiltInGifLibrary = true;
                PreferClassifiedGifs = false;
            }
            else
            {
                GifSourceMode = "BuiltInClassified";
                UseBuiltInGifLibrary = true;
                PreferClassifiedGifs = true;
            }

            if (string.IsNullOrWhiteSpace(CustomGifDirectory)) CustomGifDirectory = string.Empty;
            if (GifDirectory == null) GifDirectory = string.Empty;
        }

        private void SyncPlaylists()
        {
            if (GlobalCustomPlaylist == null)
            {
                GlobalCustomPlaylist = new List<string>();
            }

            if (MoodCustomPlaylists == null)
            {
                MoodCustomPlaylists = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                return;
            }

            Dictionary<string, List<string>> normalized = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, List<string>> pair in MoodCustomPlaylists)
            {
                string mood = YueXinMiaoPet.Services.MoodCategoryService.NormalizeMood(pair.Key);
                List<string> list = pair.Value == null ? new List<string>() : pair.Value;
                normalized[mood] = list;
            }

            MoodCustomPlaylists = normalized;
        }

        private void SyncTimes()
        {
            if (string.IsNullOrWhiteSpace(WorkStartTime)) WorkStartTime = "09:00";
            if (string.IsNullOrWhiteSpace(WorkEndTime)) WorkEndTime = "18:00";
            if (string.IsNullOrWhiteSpace(LunchStartTime)) LunchStartTime = "12:00";
            if (string.IsNullOrWhiteSpace(LunchEndTime)) LunchEndTime = "13:30";
            if (string.IsNullOrWhiteSpace(EveningStartTime)) EveningStartTime = "20:00";
            if (string.IsNullOrWhiteSpace(SleepTime)) SleepTime = "23:30";
        }

        private string InferGifSourceMode()
        {
            if (!string.IsNullOrWhiteSpace(CustomGifDirectory)) return "Custom";
            if (!string.IsNullOrWhiteSpace(GifDirectory) && !LooksLikeBuiltInPath(GifDirectory))
            {
                CustomGifDirectory = GifDirectory;
                return "Custom";
            }

            return "BuiltInClassified";
        }

        private bool LooksLikeBuiltInPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return true;
            string normalized = path.Replace('/', '\\').TrimEnd('\\');
            return normalized.EndsWith("\\PetAssets\\Gifs", StringComparison.OrdinalIgnoreCase) ||
                normalized.EndsWith("\\PetAssets\\classified_gifs", StringComparison.OrdinalIgnoreCase);
        }

        private int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
