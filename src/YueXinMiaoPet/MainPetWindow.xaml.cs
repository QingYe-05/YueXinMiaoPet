using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using YueXinMiaoPet.Models;
using YueXinMiaoPet.Services;

namespace YueXinMiaoPet
{
    public partial class MainPetWindow : Window
    {
        private readonly ConfigService _configService;
        private readonly GifAssetService _assetService;
        private readonly GifPicker _gifPicker;
        private readonly WeatherService _weatherService;
        private readonly TimeStateService _timeStateService;
        private readonly MoodService _moodService;
        private readonly DebugStateService _debugStateService;
        private readonly DispatcherTimer _stateTimer;
        private readonly DispatcherTimer _weatherTimer;
        private readonly DispatcherTimer _singleClickTimer;
        private readonly DispatcherTimer _weatherBubbleTimer;

        private TrayService _trayService;
        private PetState _state;
        private DateTime _actionLockUntil;
        private DateTime _weatherReactionUntil;
        private DateTime _lastGifChange;
        private string _currentGifPath;
        private bool _allowClose;
        private bool _mouseDown;
        private bool _isDragging;
        private Point _mouseDownScreen;
        private double _windowStartLeft;
        private double _windowStartTop;

        public MainPetWindow(
            ConfigService configService,
            GifAssetService assetService,
            GifPicker gifPicker,
            WeatherService weatherService,
            TimeStateService timeStateService,
            MoodService moodService,
            DebugStateService debugStateService)
        {
            InitializeComponent();

            _configService = configService;
            _assetService = assetService;
            _gifPicker = gifPicker;
            _weatherService = weatherService;
            _timeStateService = timeStateService;
            _moodService = moodService;
            _debugStateService = debugStateService;
            _state = new PetState();
            _currentGifPath = string.Empty;
            _actionLockUntil = DateTime.MinValue;
            _weatherReactionUntil = DateTime.MinValue;
            _lastGifChange = DateTime.MinValue;

            _stateTimer = new DispatcherTimer();
            _stateTimer.Interval = TimeSpan.FromSeconds(5);
            _stateTimer.Tick += delegate { RefreshNow(false); };

            _weatherTimer = new DispatcherTimer();
            _weatherTimer.Interval = TimeSpan.FromMinutes(10);
            _weatherTimer.Tick += async delegate { await RefreshWeatherAsync(true); };

            _singleClickTimer = new DispatcherTimer();
            _singleClickTimer.Interval = TimeSpan.FromMilliseconds(260);
            _singleClickTimer.Tick += OnSingleClickTimerTick;

            _weatherBubbleTimer = new DispatcherTimer();
            _weatherBubbleTimer.Interval = TimeSpan.FromSeconds(6);
            _weatherBubbleTimer.Tick += delegate
            {
                _weatherBubbleTimer.Stop();
                WeatherBubble.Visibility = Visibility.Collapsed;
            };

            BuildPetContextMenu();
            ApplyConfig();
            Loaded += async delegate
            {
                RefreshNow(true);
                await RefreshWeatherAsync(true);
                _stateTimer.Start();
                _weatherTimer.Start();
            };
        }

        public void AttachTrayService(TrayService trayService)
        {
            _trayService = trayService;
        }

        public void ApplyConfig()
        {
            AppConfig config = _configService.Current;
            ApplyAppearance(config.Scale, config.Opacity);
            Topmost = config.AlwaysOnTop;

            int interval = Math.Max(1, Math.Min(120, config.WeatherUpdateIntervalMinutes));
            _weatherTimer.Interval = TimeSpan.FromMinutes(interval);

            double? left = config.WindowPositionX.HasValue ? config.WindowPositionX : config.WindowLeft;
            double? top = config.WindowPositionY.HasValue ? config.WindowPositionY : config.WindowTop;
            if (left.HasValue && top.HasValue && IsPositionVisible(left.Value, top.Value))
            {
                Left = left.Value;
                Top = top.Value;
            }
            else
            {
                Forms.Screen screen = Forms.Screen.PrimaryScreen;
                Left = screen.WorkingArea.Right - Width - 40;
                Top = screen.WorkingArea.Bottom - Height - 40;
            }
        }

        public void PreviewAppearance(double scale, double opacity)
        {
            ApplyAppearance(scale, opacity);
        }

        public void RefreshMoodNow()
        {
            _actionLockUntil = DateTime.MinValue;
            _state.ActionTag = "idle";
            _lastGifChange = DateTime.MinValue;
            RefreshNow(true);
            if (_trayService != null)
            {
                _trayService.RefreshMoodChecks();
            }
        }

        public void RefreshWeatherNow()
        {
            Dispatcher.BeginInvoke(new Action(async delegate
            {
                await RefreshWeatherAsync(true);
            }));
        }

        public void RefreshNow(bool force)
        {
            try
            {
                string previousMood = _state == null ? string.Empty : _state.MoodTag;
                UpdateState();
                if (!string.Equals(previousMood, _state.MoodTag, StringComparison.OrdinalIgnoreCase))
                {
                    force = true;
                    if (_trayService != null)
                    {
                        _trayService.RefreshMoodChecks();
                    }
                }

                bool locked = DateTime.Now < _actionLockUntil;
                if (!force && locked)
                {
                    return;
                }

                if (!force && DateTime.Now - _lastGifChange < TimeSpan.FromSeconds(8))
                {
                    return;
                }

                List<GifAsset> assets = _assetService.GetEnabledExistingAssets(_configService.Current);
                GifPickResult pick = _gifPicker.Pick(_state, assets);
                if (pick.Selected == null)
                {
                    _debugStateService.Update(_state, "(没有可用 GIF)", pick);
                    return;
                }

                string path = _assetService.ResolveAssetPath(pick.Selected, _configService.Current);
                if (!File.Exists(path))
                {
                    LogService.Warn("选择到的 GIF 不存在：" + path);
                    _debugStateService.Update(_state, path, pick);
                    return;
                }

                if (force || !string.Equals(path, _currentGifPath, StringComparison.OrdinalIgnoreCase))
                {
                    _currentGifPath = path;
                    PetImage.GifPath = path;
                    _lastGifChange = DateTime.Now;
                }

                _debugStateService.Update(_state, Path.GetFileName(path), pick);
            }
            catch (Exception ex)
            {
                LogService.Error("刷新桌宠状态失败。", ex);
            }
        }

        public void PlayInteraction(string action)
        {
            if (string.Equals(action, "touch", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(action))
            {
                PlayMoodClickInteraction();
                return;
            }

            _state.LastInteractionTime = DateTime.Now;
            _state.ActionTag = string.IsNullOrWhiteSpace(action) ? "touch" : action;
            RefreshNow(true);

            double milliseconds = Math.Max(1800, Math.Min(8000, PetImage.CurrentCycleDuration.TotalMilliseconds));
            _actionLockUntil = DateTime.Now.AddMilliseconds(milliseconds);
        }

        private void PlayMoodClickInteraction()
        {
            try
            {
                bool expiredBeforeClick = _moodService.IsMoodExpired();
                _state.LastInteractionTime = DateTime.Now;
                UpdateState();
                _state.ActionTag = "touch";

                List<GifAsset> assets = _assetService.GetEnabledExistingAssets(_configService.Current);
                string mood = MoodCategoryService.NormalizeMood(_state.MoodTag);
                IList<string> categories = MoodCategoryService.GetPrimaryCategories(mood);
                int candidateCount = CountAssetsInCategories(assets, categories);

                GifPickResult pick = _gifPicker.PickForCurrentMoodInteraction(_state, assets);
                if (pick.Selected == null)
                {
                    _debugStateService.Update(_state, "(没有可用 GIF)", pick);
                    LogService.Warn("点击桌宠未选出 GIF：MoodTag=" + mood +
                        "，expired=" + expiredBeforeClick +
                        "，categories=" + MoodCategoryService.FormatCategories(categories) +
                        "，candidateCount=" + candidateCount);
                    return;
                }

                string path = _assetService.ResolveAssetPath(pick.Selected, _configService.Current);
                if (!File.Exists(path))
                {
                    LogService.Warn("点击桌宠选中的 GIF 不存在：" + path);
                    _debugStateService.Update(_state, path, pick);
                    return;
                }

                _currentGifPath = path;
                PetImage.GifPath = path;
                _lastGifChange = DateTime.Now;
                _debugStateService.Update(_state, Path.GetFileName(path), pick);

                double milliseconds = Math.Max(1800, Math.Min(8000, PetImage.CurrentCycleDuration.TotalMilliseconds));
                _actionLockUntil = DateTime.Now.AddMilliseconds(milliseconds);

                LogService.Info("点击桌宠：MoodTag=" + mood +
                    "，expired=" + expiredBeforeClick +
                    "，categories=" + MoodCategoryService.FormatCategories(categories) +
                    "，candidateCount=" + candidateCount +
                    "，selectedGif=" + Path.GetFileName(path) +
                    "，selectedCategory=" + pick.Selected.CategoryName);
            }
            catch (Exception ex)
            {
                LogService.Error("点击桌宠切换 GIF 失败。", ex);
            }
        }

        private int CountAssetsInCategories(IList<GifAsset> assets, IList<string> categories)
        {
            if (assets == null || categories == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < assets.Count; i++)
            {
                GifAsset asset = assets[i];
                if (asset == null || !asset.Enabled)
                {
                    continue;
                }

                for (int c = 0; c < categories.Count; c++)
                {
                    if (asset.HasCategory(categories[c]))
                    {
                        count++;
                        break;
                    }
                }
            }

            return count;
        }

        public void SaveWindowPosition()
        {
            _configService.Update(config =>
            {
                config.WindowPositionX = Left;
                config.WindowPositionY = Top;
                config.WindowLeft = Left;
                config.WindowTop = Top;
            });
        }

        public void AllowClose()
        {
            _allowClose = true;
        }

        public void ShowPet()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            if (_trayService != null)
            {
                _trayService.UpdateVisibilityText(true);
            }
        }

        public void HidePet()
        {
            Hide();
            if (_trayService != null)
            {
                _trayService.UpdateVisibilityText(false);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!_allowClose)
            {
                e.Cancel = true;
                HidePet();
                return;
            }

            base.OnClosing(e);
        }

        private void ApplyAppearance(double scale, double opacity)
        {
            double safeScale = Math.Max(0.5, Math.Min(2.0, scale));
            double size = Math.Max(80, 220 * safeScale);
            Width = size;
            Height = size;
            Opacity = Math.Max(0.3, Math.Min(1.0, opacity));
        }

        private async System.Threading.Tasks.Task RefreshWeatherAsync(bool showBubble)
        {
            WeatherInfo info = await _weatherService.UpdateWeatherAsync(_configService.Current);
            bool success = info != null && string.Equals(info.Source, "open-meteo", StringComparison.OrdinalIgnoreCase);
            info = info ?? WeatherInfo.Unknown();

            _configService.Update(config =>
            {
                config.LastWeatherCache = info;
                config.LastWeather = info;
                config.LastWeatherTag = info.WeatherTag;
                config.LastTemperature = info.Temperature;
                config.LastWeatherUpdateAt = info.UpdatedAtUtc;
            });

            if (!string.Equals(info.WeatherTag, "unknown", StringComparison.OrdinalIgnoreCase))
            {
                _weatherReactionUntil = DateTime.Now.AddSeconds(12);
            }

            if (showBubble)
            {
                ShowWeatherBubble(info, success);
            }

            RefreshNow(success);
        }

        private void ShowWeatherBubble(WeatherInfo info, bool success)
        {
            if (!_configService.Current.WeatherBubbleEnabled)
            {
                return;
            }

            string weatherName = GetWeatherDisplayName(info.WeatherTag);
            string prefix = success ? "天气更新" : "天气缓存";
            WeatherBubbleText.Text = prefix + "：" + weatherName + "  " + info.Temperature.ToString("0.#", CultureInfo.InvariantCulture) + "℃";
            WeatherBubble.Visibility = Visibility.Visible;
            _weatherBubbleTimer.Stop();
            _weatherBubbleTimer.Start();
        }

        private string GetWeatherDisplayName(string weatherTag)
        {
            if (string.Equals(weatherTag, "sunny", StringComparison.OrdinalIgnoreCase)) return "晴天";
            if (string.Equals(weatherTag, "cloudy", StringComparison.OrdinalIgnoreCase)) return "多云";
            if (string.Equals(weatherTag, "rain", StringComparison.OrdinalIgnoreCase)) return "下雨";
            if (string.Equals(weatherTag, "thunder", StringComparison.OrdinalIgnoreCase)) return "雷雨";
            if (string.Equals(weatherTag, "snow", StringComparison.OrdinalIgnoreCase)) return "下雪";
            if (string.Equals(weatherTag, "hot", StringComparison.OrdinalIgnoreCase)) return "高温";
            if (string.Equals(weatherTag, "cold", StringComparison.OrdinalIgnoreCase)) return "低温";
            return "未知";
        }

        private void UpdateState()
        {
            AppConfig config = _configService.Current;
            WeatherInfo weather = config.LastWeatherCache ?? config.LastWeather ?? WeatherInfo.Unknown();
            DateTime now = DateTime.Now;

            _state.WeatherTag = string.IsNullOrWhiteSpace(weather.WeatherTag) ? "unknown" : weather.WeatherTag;
            _state.Temperature = weather.Temperature;
            _state.WeatherCode = weather.WeatherCode;
            _state.TimeTag = _timeStateService.GetTimeTag(config, now);
            _state.MoodTag = _moodService.GetCurrentMood();
            _state.IsWorkingTime = _timeStateService.IsWorkingTime(config, now);
            _state.IsAfterWork = _timeStateService.IsAfterWork(config, now);
            _state.LastMoodChangedAt = ParseUtcDate(config.LastMoodChangedAt);
            _state.IsMoodLocked = !string.Equals(_state.MoodTag, "neutral", StringComparison.OrdinalIgnoreCase) &&
                _state.LastMoodChangedAt != DateTime.MinValue &&
                DateTime.UtcNow - _state.LastMoodChangedAt.ToUniversalTime() < TimeSpan.FromSeconds(60);
            _state.IsWeatherReactionActive = DateTime.Now < _weatherReactionUntil;

            if (DateTime.Now >= _actionLockUntil)
            {
                TimeSpan idleTime = DateTime.Now - _state.LastInteractionTime;
                if (idleTime > TimeSpan.FromMinutes(20) || string.Equals(_state.TimeTag, "night", StringComparison.OrdinalIgnoreCase))
                {
                    _state.ActionTag = "sleep";
                }
                else
                {
                    _state.ActionTag = "idle";
                }
            }
        }

        private DateTime ParseUtcDate(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return DateTime.MinValue;
            }

            DateTime value;
            if (DateTime.TryParse(text, null, DateTimeStyles.RoundtripKind, out value))
            {
                return value.ToUniversalTime();
            }

            return DateTime.MinValue;
        }

        private bool IsPositionVisible(double left, double top)
        {
            Rect windowRect = new Rect(left, top, Width, Height);
            foreach (Forms.Screen screen in Forms.Screen.AllScreens)
            {
                Rect area = new Rect(screen.WorkingArea.Left, screen.WorkingArea.Top, screen.WorkingArea.Width, screen.WorkingArea.Height);
                if (windowRect.IntersectsWith(area))
                {
                    return true;
                }
            }

            return false;
        }

        private void BuildPetContextMenu()
        {
            ContextMenu menu = new ContextMenu();
            MenuItem mood = new MenuItem { Header = "今日心情" };
            mood.Click += delegate { App.Instance.ShowMoodWindow(); };
            MenuItem settings = new MenuItem { Header = "设置" };
            settings.Click += delegate { App.Instance.ShowSettingsWindow(); };
            MenuItem rescan = new MenuItem { Header = "重新扫描 GIF" };
            rescan.Click += delegate { App.Instance.RescanAssets(); };
            MenuItem exit = new MenuItem { Header = "退出" };
            exit.Click += delegate { App.Instance.ExitApplication(); };
            menu.Items.Add(mood);
            menu.Items.Add(settings);
            menu.Items.Add(rescan);
            menu.Items.Add(new Separator());
            menu.Items.Add(exit);
            ContextMenu = menu;
        }

        private Point GetMouseScreenPoint(MouseEventArgs e)
        {
            return PointToScreen(e.GetPosition(this));
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _mouseDown = true;
            _isDragging = false;
            _mouseDownScreen = GetMouseScreenPoint(e);
            _windowStartLeft = Left;
            _windowStartTop = Top;
            CaptureMouse();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_mouseDown || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Point current = GetMouseScreenPoint(e);
            double dx = current.X - _mouseDownScreen.X;
            double dy = current.Y - _mouseDownScreen.Y;

            if (!_isDragging && (Math.Abs(dx) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(dy) > SystemParameters.MinimumVerticalDragDistance))
            {
                _isDragging = true;
                PlayInteraction("drag");
            }

            if (_isDragging)
            {
                Left = _windowStartLeft + dx;
                Top = _windowStartTop + dy;
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ReleaseMouseCapture();
            _mouseDown = false;

            if (_isDragging)
            {
                SaveWindowPosition();
                _state.LastInteractionTime = DateTime.Now;
                _actionLockUntil = DateTime.Now.AddSeconds(2);
                return;
            }

            _singleClickTimer.Stop();
            _singleClickTimer.Start();
        }

        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _singleClickTimer.Stop();
            PlayInteraction("greet");
            App.Instance.ShowMoodWindow();
            e.Handled = true;
        }

        private void OnSingleClickTimerTick(object sender, EventArgs e)
        {
            _singleClickTimer.Stop();
            PlayInteraction("touch");
        }

        private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ContextMenu != null)
            {
                ContextMenu.IsOpen = true;
            }
        }

        private void OnStateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                HidePet();
            }
        }
    }
}
