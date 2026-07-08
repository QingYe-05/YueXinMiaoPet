using System;
using YueXinMiaoPet.Models;
using YueXinMiaoPet.Utils;

namespace YueXinMiaoPet.Services
{
    public class ConfigService
    {
        public AppConfig Current { get; private set; }

        public ConfigService()
        {
            Current = Load();
        }

        public AppConfig Load()
        {
            FilePathHelper.EnsureDirectory(FilePathHelper.AppDataDir);

            AppConfig fallback = CreateDefault();
            AppConfig config = SafeJson.Read(FilePathHelper.ConfigPath, fallback);
            if (config == null)
            {
                config = fallback;
            }

            config.ApplyDefaults();
            NormalizeGifDirectory(config);
            Current = config;
            Save();
            return Current;
        }

        public bool Save()
        {
            if (Current == null)
            {
                Current = CreateDefault();
            }

            Current.ApplyDefaults();
            NormalizeGifDirectory(Current);
            return SafeJson.Write(FilePathHelper.ConfigPath, Current);
        }

        public void Update(Action<AppConfig> change)
        {
            if (change == null)
            {
                return;
            }

            change(Current);
            Current.ApplyDefaults();
            NormalizeGifDirectory(Current);
            Save();
        }

        public static AppConfig CreateDefault()
        {
            AppConfig config = new AppConfig();
            config.GifSourceMode = "BuiltInClassified";
            config.PreferClassifiedGifs = true;
            config.UseBuiltInGifLibrary = true;
            config.GifDirectory = FilePathHelper.GetPreferredBuiltInGifDirectory();
            config.BuiltInClassifiedGifDirectory = FilePathHelper.DefaultClassifiedGifDirectory;
            config.CustomGifDirectory = string.Empty;
            config.ScalePercent = 100;
            config.OpacityPercent = 100;
            config.Scale = 1.0;
            config.Opacity = 1.0;
            config.Province = "上海市";
            config.City = "上海市";
            config.LegacyCity = "上海市";
            config.Latitude = 31.2304;
            config.Longitude = 121.4737;
            config.WeatherEnabled = false;
            config.EnableWeather = false;
            config.WeatherUpdateIntervalMinutes = 30;
            config.WeatherBubbleEnabled = false;
            config.WeatherAffectsGif = false;
            config.UseGlobalCustomPlaylist = false;
            config.GlobalCustomPlaylist = new System.Collections.Generic.List<string>();
            config.MoodCustomPlaylists = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>(StringComparer.OrdinalIgnoreCase);
            config.ApplyDefaults();
            return config;
        }

        private void NormalizeGifDirectory(AppConfig config)
        {
            if (config == null)
            {
                return;
            }

            if (string.Equals(config.GifSourceMode, "Custom", StringComparison.OrdinalIgnoreCase))
            {
                config.UseBuiltInGifLibrary = false;
                config.GifDirectory = config.CustomGifDirectory ?? string.Empty;
            }
            else if (string.Equals(config.GifSourceMode, "BuiltIn", StringComparison.OrdinalIgnoreCase))
            {
                // 兼容旧版 BuiltIn/Gifs。如果用户没有主动选择旧 Gifs，优先升级到分类内置库。
                if (string.IsNullOrWhiteSpace(config.BuiltInClassifiedGifDirectory) &&
                    FilePathHelper.GetPreferredBuiltInGifDirectory().IndexOf("classified_gifs", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    config.GifSourceMode = "BuiltInClassified";
                    config.UseBuiltInGifLibrary = true;
                    config.PreferClassifiedGifs = true;
                    config.BuiltInClassifiedGifDirectory = FilePathHelper.DefaultClassifiedGifDirectory;
                    config.GifDirectory = FilePathHelper.GetPreferredBuiltInGifDirectory();
                    return;
                }

                config.UseBuiltInGifLibrary = true;
                config.PreferClassifiedGifs = false;
                config.GifDirectory = FilePathHelper.DefaultGifDirectory;
            }
            else
            {
                config.GifSourceMode = "BuiltInClassified";
                config.UseBuiltInGifLibrary = true;
                config.PreferClassifiedGifs = true;
                config.BuiltInClassifiedGifDirectory = FilePathHelper.DefaultClassifiedGifDirectory;
                config.GifDirectory = FilePathHelper.GetPreferredBuiltInGifDirectory();
            }
        }
    }
}
