using System;
using System.Reflection;
using System.Text;
using Forms = System.Windows.Forms;
using Microsoft.Win32;
using YueXinMiaoPet.Models;
using YueXinMiaoPet.Utils;

namespace YueXinMiaoPet.Services
{
    public class StartupOptions
    {
        public bool SafeMode { get; set; }
        public bool ResetWindow { get; set; }
        public bool ForceSoftwareRender { get; set; }
        public bool IsWindows7 { get; set; }

        public bool UseSoftwareRender
        {
            get { return SafeMode || ForceSoftwareRender || IsWindows7; }
        }

        public static StartupOptions FromArgs(string[] args)
        {
            StartupOptions options = new StartupOptions();
            Version version = Environment.OSVersion.Version;
            options.IsWindows7 = version.Major == 6 && version.Minor == 1;
            options.SafeMode = HasArgument(args, "--safe-mode");
            options.ResetWindow = HasArgument(args, "--reset-window");
            options.ForceSoftwareRender = HasArgument(args, "--force-software-render");
            return options;
        }

        private static bool HasArgument(string[] args, string expected)
        {
            if (args == null)
            {
                return false;
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], expected, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class StartupDiagnosticsService
    {
        public void LogStartup(StartupOptions options, AppConfig config)
        {
            try
            {
                options = options ?? new StartupOptions();
                StringBuilder builder = new StringBuilder();
                Assembly assembly = Assembly.GetExecutingAssembly();
                Version appVersion = assembly.GetName().Version;

                builder.AppendLine("启动诊断");
                builder.AppendLine("AppVersion=" + (appVersion == null ? string.Empty : appVersion.ToString()));
                builder.AppendLine("OSVersion=" + Environment.OSVersion);
                builder.AppendLine("Is64BitOperatingSystem=" + Environment.Is64BitOperatingSystem);
                builder.AppendLine("Is64BitProcess=" + Environment.Is64BitProcess);
                builder.AppendLine("IsWindows7=" + options.IsWindows7);
                builder.AppendLine("DotNetReleaseKey=" + GetDotNet48ReleaseKey());
                builder.AppendLine("ForceSoftwareRender=" + options.ForceSoftwareRender);
                builder.AppendLine("SafeMode=" + options.SafeMode);
                builder.AppendLine("ResetWindow=" + options.ResetWindow);
                builder.AppendLine("UseSoftwareRender=" + options.UseSoftwareRender);
                builder.AppendLine("ConfigPath=" + FilePathHelper.ConfigPath);

                if (config != null)
                {
                    builder.AppendLine("CurrentMood=" + config.MoodTag);
                    builder.AppendLine("GifSourceMode=" + config.GifSourceMode);
                    builder.AppendLine("GifDirectory=" + config.GifDirectory);
                    builder.AppendLine("CustomGifDirectory=" + config.CustomGifDirectory);
                    builder.AppendLine("WeatherEnabled=" + config.WeatherEnabled);
                    builder.AppendLine("WeatherAffectsGif=" + config.WeatherAffectsGif);
                    builder.AppendLine("WindowLeft=" + NullableDoubleToString(config.WindowPositionX));
                    builder.AppendLine("WindowTop=" + NullableDoubleToString(config.WindowPositionY));
                    builder.AppendLine("ScalePercent=" + config.ScalePercent);
                    builder.AppendLine("OpacityPercent=" + config.OpacityPercent);
                }

                LogScreens(builder);
                LogService.Info(builder.ToString());
            }
            catch (Exception ex)
            {
                LogService.Error("写入启动诊断失败。", ex);
            }
        }

        public void LogAssets(AppConfig config, GifAssetService assetService, MoodService moodService)
        {
            try
            {
                int enabledCount = 0;
                if (assetService != null && config != null)
                {
                    enabledCount = assetService.GetEnabledExistingAssets(config).Count;
                }

                StringBuilder builder = new StringBuilder();
                builder.AppendLine("资源诊断");
                builder.AppendLine("GifAssetRoot=" + (assetService == null ? string.Empty : assetService.CurrentGifDirectory));
                builder.AppendLine("ClassifiedGifCount=" + (assetService == null ? 0 : assetService.CategoryCount));
                builder.AppendLine("GifTotalCount=" + (assetService == null || assetService.Assets == null ? 0 : assetService.Assets.Count));
                builder.AppendLine("EnabledGifCount=" + enabledCount);
                builder.AppendLine("CurrentMood=" + (moodService == null ? string.Empty : moodService.GetCurrentMood()));
                LogService.Info(builder.ToString());
            }
            catch (Exception ex)
            {
                LogService.Error("写入资源诊断失败。", ex);
            }
        }

        public static int GetDotNet48ReleaseKey()
        {
            int release = 0;
            release = Math.Max(release, ReadRelease(Registry.LocalMachine, @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"));
            release = Math.Max(release, ReadRelease(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\NET Framework Setup\NDP\v4\Full"));
            return release;
        }

        private static int ReadRelease(RegistryKey root, string path)
        {
            try
            {
                using (RegistryKey key = root.OpenSubKey(path))
                {
                    if (key == null)
                    {
                        return 0;
                    }

                    object value = key.GetValue("Release");
                    if (value is int)
                    {
                        return (int)value;
                    }
                }
            }
            catch
            {
            }

            return 0;
        }

        private void LogScreens(StringBuilder builder)
        {
            Forms.Screen[] screens = Forms.Screen.AllScreens;
            builder.AppendLine("ScreenCount=" + screens.Length);
            for (int i = 0; i < screens.Length; i++)
            {
                Forms.Screen screen = screens[i];
                builder.AppendLine("Screen[" + i + "]=" +
                    "Primary:" + screen.Primary +
                    ", Bounds:" + screen.Bounds +
                    ", WorkingArea:" + screen.WorkingArea);
            }

            builder.AppendLine("PrimaryScreenWorkArea=" + Forms.Screen.PrimaryScreen.WorkingArea);
        }

        private string NullableDoubleToString(double? value)
        {
            return value.HasValue ? value.Value.ToString("0.##") : "(null)";
        }
    }
}
