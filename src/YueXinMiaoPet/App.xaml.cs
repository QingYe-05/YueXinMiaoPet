using System;
using System.Windows;
using YueXinMiaoPet.Services;
using YueXinMiaoPet.Views;

namespace YueXinMiaoPet
{
    public partial class App : Application
    {
        private SettingsWindow _settingsWindow;
        private MoodWindow _moodWindow;

        public static App Instance
        {
            get { return (App)Current; }
        }

        public ConfigService ConfigService { get; private set; }
        public GifTagInferenceService GifTagInferenceService { get; private set; }
        public GifAssetService GifAssetService { get; private set; }
        public GifPicker GifPicker { get; private set; }
        public WeatherService WeatherService { get; private set; }
        public TimeStateService TimeStateService { get; private set; }
        public MoodService MoodService { get; private set; }
        public StartupService StartupService { get; private set; }
        public DebugStateService DebugStateService { get; private set; }
        public CityCatalogService CityCatalogService { get; private set; }
        public TrayService TrayService { get; private set; }
        public MainPetWindow PetWindow { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
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

                GifAssetService.LoadAssets(ConfigService.Current);

                PetWindow = new MainPetWindow(
                    ConfigService,
                    GifAssetService,
                    GifPicker,
                    WeatherService,
                    TimeStateService,
                    MoodService,
                    DebugStateService);

                TrayService = new TrayService(
                    PetWindow,
                    MoodService,
                    ShowSettingsWindow,
                    ShowMoodWindow,
                    RescanAssets,
                    ExitApplication);

                PetWindow.AttachTrayService(TrayService);
                PetWindow.Show();

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
            GifPicker = new GifPicker();
            WeatherService = new WeatherService();
            TimeStateService = new TimeStateService();
            MoodService = new MoodService(ConfigService);
            StartupService = new StartupService();
            DebugStateService = new DebugStateService();
            CityCatalogService = new CityCatalogService();
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
                LogService.Info("UI smoke test 已打开设置窗口和心情窗口。");
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

            Models.PetState state = new Models.PetState();
            state.TimeTag = TimeStateService.GetTimeTag(ConfigService.Current, DateTime.Now);
            state.MoodTag = MoodService.GetCurrentMood();
            state.WeatherTag = ConfigService.Current.LastWeatherCache == null ? "unknown" : ConfigService.Current.LastWeatherCache.WeatherTag;
            state.IsWorkingTime = TimeStateService.IsWorkingTime(ConfigService.Current, DateTime.Now);
            state.IsAfterWork = TimeStateService.IsAfterWork(ConfigService.Current, DateTime.Now);

            Models.GifPickResult result = GifPicker.Pick(state, GifAssetService.GetEnabledExistingAssets(ConfigService.Current));
            if (result.Selected == null)
            {
                throw new InvalidOperationException("Smoke test 失败：规则引擎没有选出 GIF。");
            }

            LogService.Info("Smoke test 成功，资源数：" + GifAssetService.Assets.Count + "，分类数：" + GifAssetService.CategoryCount + "，选中：" + result.Selected.Name);
        }

        private void RunMoodClickTest()
        {
            LogService.Info("开始心情点击分类自测。");
            BuildServices();
            GifAssetService.LoadAssets(ConfigService.Current);

            string[][] cases = new[]
            {
                new[] { "angry", "05_生气" },
                new[] { "happy", "02_开心" },
                new[] { "hungry", "10_饿了" },
                new[] { "sleepy", "08_困了" },
                new[] { "neutral", "01_普通" }
            };

            System.Collections.Generic.List<Models.GifAsset> assets = GifAssetService.GetEnabledExistingAssets(ConfigService.Current);
            for (int i = 0; i < cases.Length; i++)
            {
                string mood = cases[i][0];
                string expectedCategory = cases[i][1];
                Models.PetState state = new Models.PetState
                {
                    MoodTag = mood,
                    ActionTag = "touch",
                    WeatherTag = "unknown",
                    TimeTag = "morning",
                    LastInteractionTime = DateTime.Now
                };

                for (int c = 0; c < 10; c++)
                {
                    Models.GifPickResult pick = GifPicker.PickForCurrentMoodInteraction(state, assets);
                    if (pick.Selected == null)
                    {
                        throw new InvalidOperationException("心情点击自测失败：" + mood + " 没有选出 GIF。");
                    }

                    if (!pick.Selected.HasCategory(expectedCategory))
                    {
                        throw new InvalidOperationException("心情点击自测失败：" + mood +
                            " 期望分类 " + expectedCategory +
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

            LogService.Info("心情点击分类自测成功：angry/happy/hungry/sleepy/neutral 各点击 10 次均命中目标分类，过期恢复 neutral 正常。");
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
                RefreshWeatherNow);
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
                PetWindow.RefreshNow(true);
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
