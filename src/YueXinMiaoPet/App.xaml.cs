using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using YueXinMiaoPet.Models;
using YueXinMiaoPet.Services;
using YueXinMiaoPet.Views;

namespace YueXinMiaoPet
{
    public partial class App : Application
    {
        private SettingsWindow _settingsWindow;
        private MoodWindow _moodWindow;
        private PlaylistWindow _playlistWindow;

        public static App Instance
        {
            get { return (App)Current; }
        }

        public ConfigService ConfigService { get; private set; }
        public GifTagInferenceService GifTagInferenceService { get; private set; }
        public GifAssetService GifAssetService { get; private set; }
        public GifPlaylistService GifPlaylistService { get; private set; }
        public WeatherService WeatherService { get; private set; }
        public TimeStateService TimeStateService { get; private set; }
        public MoodService MoodService { get; private set; }
        public StartupService StartupService { get; private set; }
        public DebugStateService DebugStateService { get; private set; }
        public CityCatalogService CityCatalogService { get; private set; }
        public StartupDiagnosticsService StartupDiagnosticsService { get; private set; }
        public StartupOptions StartupOptions { get; private set; }
        public TrayService TrayService { get; private set; }
        public MainPetWindow PetWindow { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            StartupOptions = StartupOptions.FromArgs(e.Args);
            ApplyRenderMode(StartupOptions);
            base.OnStartup(e);

            try
            {
                if (HasArgument(e.Args, "--mood-click-test"))
                {
                    RunMoodClickTest();
                    Shutdown(0);
                    return;
                }

                if (IsSmokeTest(e.Args))
                {
                    RunSmokeTest();
                    Shutdown(0);
                    return;
                }

                LogService.Info("月薪喵桌宠启动。");
                BuildServices();
                StartupDiagnosticsService.LogStartup(StartupOptions, ConfigService.Current);

                if (StartupOptions.ResetWindow || StartupOptions.SafeMode)
                {
                    WindowPlacementService.CenterConfigOnPrimary(ConfigService.Current, 220, 220);
                    ConfigService.Save();
                    LogService.Warn("启动参数要求重置窗口位置。SafeMode=" + StartupOptions.SafeMode + "，ResetWindow=" + StartupOptions.ResetWindow);
                }

                if (StartupOptions.SafeMode)
                {
                    GifAssetService.LoadSafeBuiltInAssets();
                }
                else
                {
                    GifAssetService.LoadAssets(ConfigService.Current);
                }

                StartupDiagnosticsService.LogAssets(ConfigService.Current, GifAssetService, MoodService);

                PetWindow = new MainPetWindow(
                    ConfigService,
                    GifAssetService,
                    GifPlaylistService,
                    WeatherService,
                    TimeStateService,
                    MoodService,
                    DebugStateService,
                    StartupOptions);

                TrayService = new TrayService(
                    PetWindow,
                    MoodService,
                    ShowSettingsWindow,
                    ShowMoodWindow,
                    RescanAssets,
                    ExitApplication);

                PetWindow.AttachTrayService(TrayService);
                PetWindow.ShowPet();

                if (HasArgument(e.Args, "--ui-smoke-test"))
                {
                    StartUiSmokeTest();
                }
            }
            catch (Exception ex)
            {
                LogService.Error("应用启动失败。", ex);
                MessageBox.Show("月薪喵启动失败，请查看日志：\n" + Utils.FilePathHelper.LogPath, "月薪喵桌宠", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        private void BuildServices()
        {
            ConfigService = new ConfigService();
            GifTagInferenceService = new GifTagInferenceService();
            GifAssetService = new GifAssetService(GifTagInferenceService);
            GifPlaylistService = new GifPlaylistService();
            WeatherService = new WeatherService();
            TimeStateService = new TimeStateService();
            MoodService = new MoodService(ConfigService);
            StartupService = new StartupService();
            DebugStateService = new DebugStateService();
            CityCatalogService = new CityCatalogService();
            StartupDiagnosticsService = new StartupDiagnosticsService();
        }

        private void ApplyRenderMode(StartupOptions options)
        {
            try
            {
                if (options != null && options.UseSoftwareRender)
                {
                    RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
                    LogService.Warn("已启用 WPF 软件渲染。IsWindows7=" + options.IsWindows7 +
                        "，SafeMode=" + options.SafeMode +
                        "，ForceSoftwareRender=" + options.ForceSoftwareRender);
                }
                else
                {
                    LogService.Info("使用默认 WPF 渲染模式。");
                }
            }
            catch (Exception ex)
            {
                LogService.Error("设置 WPF 渲染模式失败。", ex);
            }
        }

        private bool IsSmokeTest(string[] args)
        {
            return HasArgument(args, "--smoke-test");
        }

        private bool HasArgument(string[] args, string expected)
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

        private void StartUiSmokeTest()
        {
            LogService.Info("开始 UI smoke test。");

            Dispatcher.BeginInvoke(new Action(delegate
            {
                ShowSettingsWindow();
                ShowMoodWindow();
                ShowPlaylistWindow();
                LogService.Info("UI smoke test 已打开设置、心情和轮播窗口。");
            }));

            System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(20);
            timer.Tick += delegate
            {
                timer.Stop();
                LogService.Info("UI smoke test 完成。");
                ExitApplication();
            };
            timer.Start();
        }

        private void RunSmokeTest()
        {
            LogService.Info("开始 smoke test。");
            BuildServices();
            GifAssetService.LoadAssets(ConfigService.Current);

            if (GifAssetService.Assets.Count == 0)
            {
                throw new InvalidOperationException("Smoke test 失败：没有扫描到 GIF 资源。");
            }

            PetState state = new PetState();
            state.TimeTag = TimeStateService.GetTimeTag(ConfigService.Current, DateTime.Now);
            state.MoodTag = MoodService.GetCurrentMood();
            state.WeatherTag = ConfigService.Current.WeatherEnabled && ConfigService.Current.LastWeatherCache != null
                ? ConfigService.Current.LastWeatherCache.WeatherTag
                : "unknown";
            state.IsWorkingTime = TimeStateService.IsWorkingTime(ConfigService.Current, DateTime.Now);
            state.IsAfterWork = TimeStateService.IsAfterWork(ConfigService.Current, DateTime.Now);

            GifPlaylistResult result = GifPlaylistService.NextGif(state, GifAssetService.GetEnabledExistingAssets(ConfigService.Current), ConfigService.Current, true);
            if (result.Selected == null)
            {
                throw new InvalidOperationException("Smoke test 失败：顺序轮播服务没有选出 GIF。");
            }

            LogService.Info("Smoke test 成功，资源数：" + GifAssetService.Assets.Count +
                "，分类数：" + GifAssetService.CategoryCount +
                "，播放来源：" + result.Source +
                "，选中：" + result.Selected.Name);
        }

        private void RunMoodClickTest()
        {
            LogService.Info("开始心情顺序轮播分类自测。");
            BuildServices();
            GifAssetService.LoadAssets(ConfigService.Current);

            string[] moods = new[] { "angry", "happy", "hungry", "sleepy", "neutral", "love", "shy", "tired", "lazy", "excited", "thinking", "collapse" };
            System.Collections.Generic.List<GifAsset> assets = GifAssetService.GetEnabledExistingAssets(ConfigService.Current);
            for (int i = 0; i < moods.Length; i++)
            {
                string mood = moods[i];
                System.Collections.Generic.IList<string> categories = MoodCategoryService.GetPrimaryCategories(mood);
                if (categories.Count == 0)
                {
                    continue;
                }

                GifPlaylistService.ResetMoodIndex(mood);
                PetState state = new PetState
                {
                    MoodTag = mood,
                    ActionTag = "touch",
                    WeatherTag = "unknown",
                    TimeTag = "morning",
                    LastInteractionTime = DateTime.Now
                };

                for (int c = 0; c < 10; c++)
                {
                    GifPlaylistResult pick = GifPlaylistService.NextGif(state, assets, ConfigService.Current, c == 0);
                    if (pick.Selected == null)
                    {
                        throw new InvalidOperationException("心情轮播自测失败：" + mood + " 没有选出 GIF。");
                    }

                    if (!pick.Selected.HasCategory(categories[0]))
                    {
                        throw new InvalidOperationException("心情轮播自测失败：" + mood +
                            " 期望分类 " + categories[0] +
                            "，实际 " + pick.Selected.CategoryName +
                            "，文件 " + pick.Selected.Name);
                    }
                }
            }

            string oldMood = ConfigService.Current.MoodTag;
            string oldCurrentMood = ConfigService.Current.CurrentMood;
            string oldMode = ConfigService.Current.MoodExpireMode;
            string oldExpireAt = ConfigService.Current.MoodExpireAt;
            string oldExpireUtc = ConfigService.Current.MoodExpiresAtUtc;
            string oldChanged = ConfigService.Current.LastMoodChangedAt;
            try
            {
                ConfigService.Update(config =>
                {
                    config.MoodTag = "happy";
                    config.CurrentMood = "happy";
                    config.MoodExpireMode = "thirty_minutes";
                    config.MoodExpireAt = DateTime.UtcNow.AddMinutes(-1).ToString("o");
                    config.MoodExpiresAtUtc = config.MoodExpireAt;
                    config.LastMoodChangedAt = DateTime.UtcNow.AddHours(-1).ToString("o");
                });

                string expiredMood = MoodService.GetCurrentMood();
                if (!string.Equals(expiredMood, "neutral", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("心情过期自测失败：期望 neutral，实际 " + expiredMood);
                }
            }
            finally
            {
                ConfigService.Update(config =>
                {
                    config.MoodTag = oldMood;
                    config.CurrentMood = oldCurrentMood;
                    config.MoodExpireMode = oldMode;
                    config.MoodExpireAt = oldExpireAt;
                    config.MoodExpiresAtUtc = oldExpireUtc;
                    config.LastMoodChangedAt = oldChanged;
                });
            }

            LogService.Info("心情顺序轮播分类自测成功，过期恢复 neutral 正常。");
        }

        public void ShowSettingsWindow()
        {
            if (_settingsWindow != null && _settingsWindow.IsVisible)
            {
                _settingsWindow.Activate();
                return;
            }

            _settingsWindow = new SettingsWindow(
                ConfigService,
                GifAssetService,
                StartupService,
                DebugStateService,
                CityCatalogService,
                delegate(double scale, double opacity)
                {
                    if (PetWindow != null)
                    {
                        PetWindow.PreviewAppearance(scale, opacity);
                    }
                },
                delegate
                {
                    if (PetWindow != null)
                    {
                        PetWindow.ApplyConfig();
                    }
                },
                ApplyConfigChanged,
                RescanAssets,
                RefreshWeatherNow,
                ShowPlaylistWindow);
            _settingsWindow.Owner = PetWindow;
            _settingsWindow.Show();
        }

        public void ShowMoodWindow()
        {
            if (_moodWindow != null && _moodWindow.IsVisible)
            {
                _moodWindow.Activate();
                return;
            }

            _moodWindow = new MoodWindow(MoodService, GifAssetService, ConfigService, delegate
            {
                if (PetWindow != null)
                {
                    PetWindow.RefreshMoodNow();
                }
            });
            _moodWindow.Owner = PetWindow;
            _moodWindow.Show();
        }

        public void ShowPlaylistWindow()
        {
            if (_playlistWindow != null && _playlistWindow.IsVisible)
            {
                _playlistWindow.Activate();
                return;
            }

            _playlistWindow = new PlaylistWindow(ConfigService, GifAssetService, GifPlaylistService, delegate
            {
                if (PetWindow != null)
                {
                    PetWindow.RefreshPlaylistNow();
                }
            });
            _playlistWindow.Owner = PetWindow;
            _playlistWindow.Show();
        }

        public void ApplyConfigChanged()
        {
            if (PetWindow != null)
            {
                PetWindow.ApplyConfig();
                PetWindow.RefreshNow(true);
            }

            if (TrayService != null)
            {
                TrayService.RefreshMoodChecks();
            }
        }

        public void RescanAssets()
        {
            GifAssetService.LoadAssets(ConfigService.Current);
            if (PetWindow != null)
            {
                PetWindow.RefreshPlaylistNow();
            }
        }

        public void RefreshWeatherNow()
        {
            if (PetWindow != null)
            {
                PetWindow.RefreshWeatherNow();
            }
        }

        public void ExitApplication()
        {
            try
            {
                if (PetWindow != null)
                {
                    PetWindow.AllowClose();
                    PetWindow.SaveWindowPosition();
                }

                if (TrayService != null)
                {
                    TrayService.Dispose();
                }

                ConfigService.Save();
            }
            catch (Exception ex)
            {
                LogService.Error("退出时保存状态失败。", ex);
            }

            Shutdown(0);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (TrayService != null)
            {
                TrayService.Dispose();
            }

            LogService.Info("月薪喵桌宠退出。");
            base.OnExit(e);
        }
    }
}
