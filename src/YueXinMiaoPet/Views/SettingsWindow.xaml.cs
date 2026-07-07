using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using Forms = System.Windows.Forms;
using YueXinMiaoPet.Models;
using YueXinMiaoPet.Services;
using YueXinMiaoPet.Utils;

namespace YueXinMiaoPet.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly ConfigService _configService;
        private readonly GifAssetService _assetService;
        private readonly StartupService _startupService;
        private readonly DebugStateService _debugStateService;
        private readonly CityCatalogService _cityCatalogService;
        private readonly CodexStatusService _codexStatusService;
        private readonly Action<double, double> _onPreviewAppearance;
        private readonly Action _onCancelPreview;
        private readonly Action _onConfigChanged;
        private readonly Action _onRescan;
        private readonly Action _onRefreshWeather;
        private readonly Action _onOpenPlaylistSettings;

        private bool _loading;
        private int _originalScalePercent;
        private int _originalOpacityPercent;

        public SettingsWindow(
            ConfigService configService,
            GifAssetService assetService,
            StartupService startupService,
            DebugStateService debugStateService,
            CityCatalogService cityCatalogService,
            CodexStatusService codexStatusService,
            Action<double, double> onPreviewAppearance,
            Action onCancelPreview,
            Action onConfigChanged,
            Action onRescan,
            Action onRefreshWeather,
            Action onOpenPlaylistSettings)
        {
            InitializeComponent();
            _configService = configService;
            _assetService = assetService;
            _startupService = startupService;
            _debugStateService = debugStateService;
            _cityCatalogService = cityCatalogService;
            _codexStatusService = codexStatusService;
            _onPreviewAppearance = onPreviewAppearance;
            _onCancelPreview = onCancelPreview;
            _onConfigChanged = onConfigChanged;
            _onRescan = onRescan;
            _onRefreshWeather = onRefreshWeather;
            _onOpenPlaylistSettings = onOpenPlaylistSettings;

            LoadFromConfig();
            RefreshDebug();
        }

        private void LoadFromConfig()
        {
            _loading = true;
            AppConfig config = _configService.Current;
            _originalScalePercent = config.ScalePercent;
            _originalOpacityPercent = config.OpacityPercent;

            ProvinceCombo.ItemsSource = _cityCatalogService.GetProvinces();
            SelectProvinceAndCity(config.Province, config.City);

            EnableWeatherBox.IsChecked = config.WeatherEnabled;
            WeatherBubbleBox.IsChecked = config.WeatherEnabled;
            WeatherAffectsGifBox.IsChecked = config.WeatherAffectsGif;
            WeatherIntervalBox.Text = Math.Max(1, config.WeatherUpdateIntervalMinutes).ToString(CultureInfo.InvariantCulture);
            LatitudeBox.Text = config.Latitude.ToString(CultureInfo.InvariantCulture);
            LongitudeBox.Text = config.Longitude.ToString(CultureInfo.InvariantCulture);
            CodexStatusEnabledBox.IsChecked = config.CodexStatusEnabled;
            CodexStatusBubbleBox.IsChecked = config.CodexStatusBubbleEnabled;
            CodexStatusAffectsGifBox.IsChecked = config.CodexStatusAffectsGif;
            CodexStatusFilePathBox.Text = string.IsNullOrWhiteSpace(config.CodexStatusFilePath) ? FilePathHelper.CodexStatusPath : config.CodexStatusFilePath;
            CodexStatusIntervalBox.Text = Math.Max(1, config.CodexStatusRefreshIntervalSeconds).ToString(CultureInfo.InvariantCulture);
            AlwaysOnTopBox.IsChecked = config.AlwaysOnTop;
            StartupBox.IsChecked = _startupService.IsEnabled();
            ScaleSlider.Value = config.ScalePercent <= 0 ? 100 : config.ScalePercent;
            OpacitySlider.Value = config.OpacityPercent <= 0 ? 100 : config.OpacityPercent;
            WorkStartBox.Text = config.WorkStartTime;
            WorkEndBox.Text = config.WorkEndTime;
            LunchStartBox.Text = config.LunchStartTime;
            LunchEndBox.Text = config.LunchEndTime;
            EveningStartBox.Text = config.EveningStartTime;
            SleepTimeBox.Text = config.SleepTime;

            string mode = string.IsNullOrWhiteSpace(config.GifSourceMode) ? "BuiltInClassified" : config.GifSourceMode;
            ClassifiedRadio.IsChecked = string.Equals(mode, "BuiltInClassified", StringComparison.OrdinalIgnoreCase);
            BuiltInRadio.IsChecked = string.Equals(mode, "BuiltIn", StringComparison.OrdinalIgnoreCase);
            CustomRadio.IsChecked = string.Equals(mode, "Custom", StringComparison.OrdinalIgnoreCase);
            if (ClassifiedRadio.IsChecked != true && BuiltInRadio.IsChecked != true && CustomRadio.IsChecked != true)
            {
                ClassifiedRadio.IsChecked = true;
            }

            CustomGifDirectoryBox.Text = config.CustomGifDirectory;

            _loading = false;
            UpdateAppearanceText();
            UpdateResourceFields();
            UpdateWeatherFields();
            UpdateWeatherInfoText();
            UpdateCodexStatusInfoText();
        }

        private void SelectProvinceAndCity(string province, string city)
        {
            ProvinceCombo.SelectedItem = province;
            if (ProvinceCombo.SelectedItem == null && ProvinceCombo.Items.Count > 0)
            {
                ProvinceCombo.SelectedIndex = 0;
            }

            PopulateCities(ProvinceCombo.SelectedItem as string, city);
        }

        private void PopulateCities(string province, string selectedCity)
        {
            if (string.IsNullOrWhiteSpace(province))
            {
                return;
            }

            CityCombo.ItemsSource = _cityCatalogService.GetCities(province);
            CityCombo.SelectedValue = selectedCity;
            if (CityCombo.SelectedItem == null && CityCombo.Items.Count > 0)
            {
                CityCombo.SelectedIndex = 0;
            }
        }

        private bool SaveSettings(bool showMessage, bool forceRescan)
        {
            double latitude;
            double longitude;
            int weatherInterval;
            int codexInterval;

            if (!double.TryParse(LatitudeBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out latitude) ||
                !double.TryParse(LongitudeBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out longitude))
            {
                MessageBox.Show("经纬度必须是数字，例如 31.2304 / 121.4737。", "月薪喵设置", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!int.TryParse(WeatherIntervalBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out weatherInterval))
            {
                MessageBox.Show("天气刷新间隔必须是整数分钟。", "月薪喵设置", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            weatherInterval = Math.Max(1, Math.Min(120, weatherInterval));

            if (!int.TryParse(CodexStatusIntervalBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out codexInterval))
            {
                MessageBox.Show("Codex 状态刷新间隔必须是整数秒。", "月薪喵设置", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            codexInterval = Math.Max(1, Math.Min(60, codexInterval));

            if (!IsTime(WorkStartBox.Text) || !IsTime(WorkEndBox.Text) || !IsTime(LunchStartBox.Text) ||
                !IsTime(LunchEndBox.Text) || !IsTime(EveningStartBox.Text) || !IsTime(SleepTimeBox.Text))
            {
                MessageBox.Show("时间格式必须是 HH:mm，例如 09:00。", "月薪喵设置", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            string mode = GetSelectedMode();
            string customDirectory = (CustomGifDirectoryBox.Text ?? string.Empty).Trim();
            if (string.Equals(mode, "Custom", StringComparison.OrdinalIgnoreCase) && !Directory.Exists(customDirectory))
            {
                MessageBox.Show("自定义 GIF 目录无效，已切回内置月薪喵分类 GIF。", "月薪喵设置", MessageBoxButton.OK, MessageBoxImage.Information);
                mode = "BuiltInClassified";
                ClassifiedRadio.IsChecked = true;
                BuiltInRadio.IsChecked = false;
                CustomRadio.IsChecked = false;
            }

            AppConfig oldConfig = _configService.Current;
            string oldMode = oldConfig.GifSourceMode;
            string oldCustomDirectory = oldConfig.CustomGifDirectory;
            string oldProvince = oldConfig.Province;
            string oldCity = oldConfig.City;
            double oldLatitude = oldConfig.Latitude;
            double oldLongitude = oldConfig.Longitude;
            bool oldWeatherEnabled = oldConfig.WeatherEnabled;
            bool oldWeatherAffectsGif = oldConfig.WeatherAffectsGif;
            int oldWeatherInterval = oldConfig.WeatherUpdateIntervalMinutes;
            bool oldCodexEnabled = oldConfig.CodexStatusEnabled;
            bool oldCodexBubble = oldConfig.CodexStatusBubbleEnabled;
            bool oldCodexAffectsGif = oldConfig.CodexStatusAffectsGif;
            string oldCodexPath = oldConfig.CodexStatusFilePath;
            int oldCodexInterval = oldConfig.CodexStatusRefreshIntervalSeconds;

            CityInfo city = CityCombo.SelectedItem as CityInfo;
            string provinceName = ProvinceCombo.SelectedItem as string;
            string cityName = city == null ? oldConfig.City : city.City;

            int scalePercent = (int)Math.Round(ScaleSlider.Value);
            int opacityPercent = (int)Math.Round(OpacitySlider.Value);
            string selectedMode = mode;
            bool weatherEnabled = EnableWeatherBox.IsChecked == true;
            bool weatherAffectsGif = WeatherAffectsGifBox.IsChecked == true;
            bool codexEnabled = CodexStatusEnabledBox.IsChecked == true;
            bool codexBubble = CodexStatusBubbleBox.IsChecked == true;
            bool codexAffectsGif = CodexStatusAffectsGifBox.IsChecked == true;
            string codexStatusPath = (CodexStatusFilePathBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(codexStatusPath))
            {
                codexStatusPath = FilePathHelper.CodexStatusPath;
            }

            _configService.Update(config =>
            {
                config.GifSourceMode = selectedMode;
                config.UseBuiltInGifLibrary = !string.Equals(selectedMode, "Custom", StringComparison.OrdinalIgnoreCase);
                config.PreferClassifiedGifs = string.Equals(selectedMode, "BuiltInClassified", StringComparison.OrdinalIgnoreCase);
                config.BuiltInClassifiedGifDirectory = FilePathHelper.DefaultClassifiedGifDirectory;
                config.CustomGifDirectory = string.Equals(selectedMode, "Custom", StringComparison.OrdinalIgnoreCase) ? customDirectory : string.Empty;
                config.GifDirectory = GetDirectoryForMode(selectedMode, config.CustomGifDirectory);

                config.WeatherEnabled = weatherEnabled;
                config.EnableWeather = weatherEnabled;
                config.WeatherBubbleEnabled = weatherEnabled;
                config.WeatherAffectsGif = weatherAffectsGif;
                config.WeatherUpdateIntervalMinutes = weatherInterval;
                config.CodexStatusEnabled = codexEnabled;
                config.CodexStatusBubbleEnabled = codexBubble;
                config.CodexStatusAffectsGif = codexAffectsGif;
                config.CodexStatusFilePath = codexStatusPath;
                config.CodexStatusRefreshIntervalSeconds = codexInterval;
                config.Province = string.IsNullOrWhiteSpace(provinceName) ? "上海市" : provinceName;
                config.City = string.IsNullOrWhiteSpace(cityName) ? "上海市" : cityName;
                config.LegacyCity = config.City;
                config.Latitude = latitude;
                config.Longitude = longitude;
                config.AlwaysOnTop = AlwaysOnTopBox.IsChecked == true;
                config.AutoStart = StartupBox.IsChecked == true;
                config.StartWithWindows = config.AutoStart;
                config.ScalePercent = scalePercent;
                config.Scale = scalePercent / 100.0;
                config.OpacityPercent = opacityPercent;
                config.Opacity = opacityPercent / 100.0;
                config.WorkStartTime = WorkStartBox.Text.Trim();
                config.WorkEndTime = WorkEndBox.Text.Trim();
                config.LunchStartTime = LunchStartBox.Text.Trim();
                config.LunchEndTime = LunchEndBox.Text.Trim();
                config.EveningStartTime = EveningStartBox.Text.Trim();
                config.SleepTime = SleepTimeBox.Text.Trim();
            });

            _startupService.SetEnabled(StartupBox.IsChecked == true);

            bool gifChanged = forceRescan ||
                !string.Equals(oldMode, _configService.Current.GifSourceMode, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(oldCustomDirectory ?? string.Empty, _configService.Current.CustomGifDirectory ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            bool weatherChanged =
                oldWeatherEnabled != _configService.Current.WeatherEnabled ||
                oldWeatherAffectsGif != _configService.Current.WeatherAffectsGif ||
                oldWeatherInterval != _configService.Current.WeatherUpdateIntervalMinutes ||
                !string.Equals(oldProvince, _configService.Current.Province, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(oldCity, _configService.Current.City, StringComparison.OrdinalIgnoreCase) ||
                Math.Abs(oldLatitude - _configService.Current.Latitude) > 0.000001 ||
                Math.Abs(oldLongitude - _configService.Current.Longitude) > 0.000001;

            bool codexChanged =
                oldCodexEnabled != _configService.Current.CodexStatusEnabled ||
                oldCodexBubble != _configService.Current.CodexStatusBubbleEnabled ||
                oldCodexAffectsGif != _configService.Current.CodexStatusAffectsGif ||
                oldCodexInterval != _configService.Current.CodexStatusRefreshIntervalSeconds ||
                !string.Equals(oldCodexPath ?? string.Empty, _configService.Current.CodexStatusFilePath ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            if (_configService.Current.CodexStatusEnabled && _codexStatusService != null)
            {
                _codexStatusService.EnsureDefaultStatusFile(_configService.Current);
                if (codexChanged)
                {
                    _codexStatusService.RefreshNow();
                }
            }

            if (_onConfigChanged != null)
            {
                _onConfigChanged();
            }

            if (gifChanged && _onRescan != null)
            {
                _onRescan();
            }

            if (_configService.Current.WeatherEnabled && weatherChanged && _onRefreshWeather != null)
            {
                _onRefreshWeather();
            }

            RefreshDebug();
            UpdateResourceFields();
            UpdateWeatherFields();
            UpdateWeatherInfoText();
            UpdateCodexStatusInfoText();

            if (showMessage)
            {
                MessageBox.Show("设置已保存。", "月薪喵设置", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            return true;
        }

        private string GetSelectedMode()
        {
            if (CustomRadio.IsChecked == true)
            {
                return "Custom";
            }

            if (BuiltInRadio.IsChecked == true)
            {
                return "BuiltIn";
            }

            return "BuiltInClassified";
        }

        private string GetDirectoryForMode(string mode, string customDirectory)
        {
            if (string.Equals(mode, "Custom", StringComparison.OrdinalIgnoreCase))
            {
                return customDirectory;
            }

            if (string.Equals(mode, "BuiltIn", StringComparison.OrdinalIgnoreCase))
            {
                return FilePathHelper.DefaultGifDirectory;
            }

            return FilePathHelper.GetPreferredBuiltInGifDirectory();
        }

        private bool IsTime(string value)
        {
            TimeSpan parsed;
            return TimeSpan.TryParse(value, out parsed);
        }

        private void RefreshDebug()
        {
            AppConfig config = _configService.Current;
            DebugSnapshot snapshot = _debugStateService.GetSnapshot();
            PetState debugState = snapshot == null ? null : snapshot.State;
            string currentMood = MoodCategoryService.NormalizeMood(debugState == null ? config.MoodTag : debugState.MoodTag);
            System.Collections.Generic.IList<string> clickCategories = MoodCategoryService.GetPrimaryCategories(currentMood);
            string text = _debugStateService.FormatSnapshot();
            text += Environment.NewLine;
            text += "当前 GIF 资源模式: " + config.GifSourceMode + Environment.NewLine;
            text += "当前 GIF 目录: " + _assetService.CurrentGifDirectory + Environment.NewLine;
            text += "当前分类数量: " + _assetService.CategoryCount + Environment.NewLine;
            text += "当前 MoodTag 对应分类: " + MoodCategoryService.FormatCategories(clickCategories) + Environment.NewLine;
            text += "当前心情分类 GIF 数量: " + CountAssetsInCategories(clickCategories) + Environment.NewLine;
            text += "当前使用的资源索引来源: " + _assetService.LastSource + Environment.NewLine;
            text += "当前使用的标签来源: " + _assetService.LastTagSource + Environment.NewLine;
            text += "GIF 总数量: " + _assetService.Assets.Count + Environment.NewLine;
            text += "enabled GIF 数量: " + CountEnabledAssets() + Environment.NewLine;
            text += "当前城市: " + config.Province + " / " + config.City + Environment.NewLine;
            text += "当前经纬度: " + config.Latitude.ToString(CultureInfo.InvariantCulture) + ", " + config.Longitude.ToString(CultureInfo.InvariantCulture) + Environment.NewLine;
            text += "WeatherEnabled: " + config.WeatherEnabled + Environment.NewLine;
            text += "WeatherAffectsGif: " + config.WeatherAffectsGif + Environment.NewLine;
            text += "WeatherBadgeText: " + (snapshot == null ? string.Empty : snapshot.WeatherBadgeText) + Environment.NewLine;
            text += "CodexStatusEnabled: " + config.CodexStatusEnabled + Environment.NewLine;
            text += "CodexStatusBubbleEnabled: " + config.CodexStatusBubbleEnabled + Environment.NewLine;
            text += "CodexStatusAffectsGif: " + config.CodexStatusAffectsGif + Environment.NewLine;
            text += "CodexStatusFilePath: " + config.CodexStatusFilePath + Environment.NewLine;
            text += "CodexStatusTag: " + (snapshot == null ? string.Empty : snapshot.CodexStatusTag) + Environment.NewLine;
            text += "CodexStatusBadgeText: " + (snapshot == null ? string.Empty : snapshot.CodexStatusBadgeText) + Environment.NewLine;
            text += "当前播放模式: " + (snapshot == null ? string.Empty : snapshot.CurrentPlaybackMode) + Environment.NewLine;
            text += "当前播放列表来源: " + (snapshot == null ? string.Empty : snapshot.CurrentPlaylistSource) + Environment.NewLine;
            text += "当前播放列表数量: " + (snapshot == null ? 0 : snapshot.CurrentPlaylistCount) + Environment.NewLine;
            text += "当前播放索引: " + (snapshot == null ? 0 : snapshot.CurrentPlaylistIndex) + Environment.NewLine;
            text += "当前心情自定义轮播数量: " + (snapshot == null ? 0 : snapshot.CurrentMoodCustomPlaylistCount) + Environment.NewLine;
            text += "全局自定义轮播数量: " + (snapshot == null ? 0 : snapshot.GlobalCustomPlaylistCount) + Environment.NewLine;
            text += "天气刷新间隔: " + config.WeatherUpdateIntervalMinutes + " 分钟" + Environment.NewLine;
            text += "当前缩放比例: " + config.ScalePercent + "%" + Environment.NewLine;
            text += "当前透明度: " + config.OpacityPercent + "%" + Environment.NewLine;
            text += "当前配置文件: " + FilePathHelper.ConfigPath + Environment.NewLine;
            text += Environment.NewLine;
            text += "13 类扫描计数：" + Environment.NewLine;
            text += FormatCategoryCounts();
            text += Environment.NewLine;
            text += "心情分类映射：" + Environment.NewLine;
            foreach (MoodOption option in MoodCategoryService.GetMoodOptions())
            {
                text += "  " + option.Key + " / " + option.Name + " => " +
                    MoodCategoryService.FormatCategories(MoodCategoryService.GetPrimaryCategories(option.Key)) +
                    Environment.NewLine;
            }

            DebugTextBox.Text = text;
        }

        private int CountAssetsInCategories(System.Collections.Generic.IList<string> categories)
        {
            if (categories == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < _assetService.Assets.Count; i++)
            {
                GifAsset asset = _assetService.Assets[i];
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

        private string FormatCategoryCounts()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            foreach (MoodOption option in MoodCategoryService.GetMoodOptions())
            {
                System.Collections.Generic.IList<string> categories = MoodCategoryService.GetPrimaryCategories(option.Key);
                for (int i = 0; i < categories.Count; i++)
                {
                    string category = categories[i];
                    int count = 0;
                    if (_assetService.CategoryGifCounts != null && _assetService.CategoryGifCounts.ContainsKey(category))
                    {
                        count = _assetService.CategoryGifCounts[category];
                    }

                    builder.Append("  ");
                    builder.Append(category);
                    builder.Append(": ");
                    builder.Append(count);
                    builder.AppendLine(" 张");
                }
            }

            return builder.ToString();
        }

        private int CountEnabledAssets()
        {
            int count = 0;
            for (int i = 0; i < _assetService.Assets.Count; i++)
            {
                if (_assetService.Assets[i] != null && _assetService.Assets[i].Enabled)
                {
                    count++;
                }
            }

            return count;
        }

        private void UpdateResourceFields()
        {
            string mode = GetSelectedMode();
            bool useCustom = string.Equals(mode, "Custom", StringComparison.OrdinalIgnoreCase);
            BrowseGifButton.IsEnabled = useCustom;
            CustomGifDirectoryBox.IsEnabled = useCustom;

            string configuredPath = GetDirectoryForMode(mode, (CustomGifDirectoryBox.Text ?? string.Empty).Trim());
            string label = "BuiltInClassified / 使用内置月薪喵分类 GIF";
            if (string.Equals(mode, "BuiltIn", StringComparison.OrdinalIgnoreCase))
            {
                label = "BuiltIn / 使用原始内置 Gifs";
            }
            else if (useCustom)
            {
                label = "Custom / 使用自定义 GIF 目录";
            }

            CurrentResourceModeText.Text = "当前资源模式：" + label;
            CurrentGifPathText.Text = "当前 GIF 路径：" + configuredPath;
            GifCountText.Text = "已加载 GIF 数量：" + _assetService.Assets.Count;
            EnabledGifCountText.Text = "enabled GIF 数量：" + CountEnabledAssets();
            CategoryCountText.Text = "分类数量：" + _assetService.CategoryCount;
        }

        private void UpdateWeatherInfoText()
        {
            AppConfig config = _configService.Current;
            WeatherInfo info = config.LastWeatherCache ?? config.LastWeather ?? WeatherInfo.Unknown();
            CurrentWeatherInfoText.Text = "当前天气标签：" + info.WeatherTag +
                "，温度：" + info.Temperature.ToString("0.#", CultureInfo.InvariantCulture) + "℃" +
                "，天气码：" + info.WeatherCode +
                "，更新时间：" + FormatTime(info.UpdatedAtUtc);
        }

        private void UpdateCodexStatusInfoText()
        {
            if (CurrentCodexStatusInfoText == null)
            {
                return;
            }

            AppConfig config = _configService.Current;
            string path = _codexStatusService == null ? (config.CodexStatusFilePath ?? string.Empty) : _codexStatusService.GetStatusFilePath(config);
            CodexStatus status = _codexStatusService == null ? null : _codexStatusService.CurrentStatus;
            string statusText = status == null ? "unknown" : status.Status;
            string display = _codexStatusService == null ? string.Empty : _codexStatusService.CurrentDisplayText;

            CurrentCodexStatusInfoText.Text =
                "当前状态文件：" + path + Environment.NewLine +
                "当前 Codex status：" + statusText + Environment.NewLine +
                "当前气泡：" + (string.IsNullOrWhiteSpace(display) ? "未显示" : display.Replace(Environment.NewLine, " / "));
        }

        private void UpdateWeatherFields()
        {
            bool enabled = EnableWeatherBox.IsChecked == true;
            WeatherBubbleBox.IsChecked = enabled;
            WeatherAffectsGifBox.IsEnabled = enabled;
            WeatherIntervalBox.IsEnabled = enabled;
            RefreshWeatherButton.IsEnabled = enabled;
            ProvinceCombo.IsEnabled = enabled;
            CityCombo.IsEnabled = enabled;
            LatitudeBox.IsEnabled = enabled;
            LongitudeBox.IsEnabled = enabled;
        }

        private string FormatTime(string utcText)
        {
            DateTime time;
            if (DateTime.TryParse(utcText, null, DateTimeStyles.RoundtripKind, out time))
            {
                return time.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            }

            return "未知";
        }

        private void UpdateAppearanceText()
        {
            ScaleValueText.Text = ((int)Math.Round(ScaleSlider.Value)).ToString(CultureInfo.InvariantCulture) + "%";
            OpacityValueText.Text = ((int)Math.Round(OpacitySlider.Value)).ToString(CultureInfo.InvariantCulture) + "%";
        }

        private void OnProvinceChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_loading)
            {
                return;
            }

            PopulateCities(ProvinceCombo.SelectedItem as string, null);
            OnCityChanged(sender, e);
        }

        private void OnCityChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_loading)
            {
                return;
            }

            CityInfo city = CityCombo.SelectedItem as CityInfo;
            if (city == null)
            {
                return;
            }

            LatitudeBox.Text = city.Latitude.ToString(CultureInfo.InvariantCulture);
            LongitudeBox.Text = city.Longitude.ToString(CultureInfo.InvariantCulture);
        }

        private void OnAppearanceSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ScaleValueText == null || OpacityValueText == null)
            {
                return;
            }

            UpdateAppearanceText();
            if (!_loading && _onPreviewAppearance != null)
            {
                _onPreviewAppearance(ScaleSlider.Value / 100.0, OpacitySlider.Value / 100.0);
            }
        }

        private void OnGifSourceModeChanged(object sender, RoutedEventArgs e)
        {
            if (ClassifiedRadio == null || BuiltInRadio == null || CustomRadio == null)
            {
                return;
            }

            UpdateResourceFields();
        }

        private void OnWeatherEnabledChanged(object sender, RoutedEventArgs e)
        {
            if (_loading)
            {
                return;
            }

            UpdateWeatherFields();
        }

        private void OnBrowseGifDirectoryClick(object sender, RoutedEventArgs e)
        {
            using (Forms.FolderBrowserDialog dialog = new Forms.FolderBrowserDialog())
            {
                dialog.Description = "选择 GIF 目录";
                dialog.SelectedPath = Directory.Exists(CustomGifDirectoryBox.Text) ? CustomGifDirectoryBox.Text : FilePathHelper.GetPreferredBuiltInGifDirectory();
                if (dialog.ShowDialog() == Forms.DialogResult.OK)
                {
                    CustomRadio.IsChecked = true;
                    CustomGifDirectoryBox.Text = dialog.SelectedPath;
                    UpdateResourceFields();
                }
            }
        }

        private void OnUseBuiltInClick(object sender, RoutedEventArgs e)
        {
            ClassifiedRadio.IsChecked = true;
            BuiltInRadio.IsChecked = false;
            CustomRadio.IsChecked = false;
            UpdateResourceFields();
        }

        private void OnOpenCurrentResourceDirectoryClick(object sender, RoutedEventArgs e)
        {
            string directory = GetDirectoryForMode(GetSelectedMode(), (CustomGifDirectoryBox.Text ?? string.Empty).Trim());
            if (!Directory.Exists(directory))
            {
                MessageBox.Show("目录不存在：" + directory, "月薪喵设置", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Process.Start(new ProcessStartInfo("explorer.exe", "\"" + directory + "\""));
        }

        private void OnOpenPlaylistSettingsClick(object sender, RoutedEventArgs e)
        {
            if (_onOpenPlaylistSettings != null)
            {
                _onOpenPlaylistSettings();
            }
        }

        private void OnBrowseCodexStatusFileClick(object sender, RoutedEventArgs e)
        {
            using (Forms.OpenFileDialog dialog = new Forms.OpenFileDialog())
            {
                dialog.Title = "选择 Codex 状态文件";
                dialog.Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*";
                dialog.CheckFileExists = false;
                dialog.FileName = Path.GetFileName(string.IsNullOrWhiteSpace(CodexStatusFilePathBox.Text) ? FilePathHelper.CodexStatusPath : CodexStatusFilePathBox.Text);
                string currentPath = _codexStatusService == null ? CodexStatusFilePathBox.Text : _codexStatusService.ResolveStatusFilePath(CodexStatusFilePathBox.Text);
                string dir = Path.GetDirectoryName(currentPath);
                if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                {
                    dialog.InitialDirectory = dir;
                }

                if (dialog.ShowDialog() == Forms.DialogResult.OK)
                {
                    CodexStatusFilePathBox.Text = dialog.FileName;
                    UpdateCodexStatusInfoText();
                }
            }
        }

        private void OnOpenCodexStatusDirectoryClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = _codexStatusService == null ? CodexStatusFilePathBox.Text : _codexStatusService.ResolveStatusFilePath(CodexStatusFilePathBox.Text);
                if (string.IsNullOrWhiteSpace(path))
                {
                    path = FilePathHelper.CodexStatusPath;
                }

                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir))
                {
                    FilePathHelper.EnsureDirectory(dir);
                    Process.Start(new ProcessStartInfo("explorer.exe", "\"" + dir + "\""));
                }
            }
            catch (Exception ex)
            {
                LogService.Error("打开 Codex 状态文件目录失败。", ex);
                MessageBox.Show("打开目录失败，请查看日志。", "月薪喵设置", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnCreateDefaultCodexStatusClick(object sender, RoutedEventArgs e)
        {
            if (_codexStatusService == null)
            {
                return;
            }

            string path = string.IsNullOrWhiteSpace(CodexStatusFilePathBox.Text) ? FilePathHelper.CodexStatusPath : CodexStatusFilePathBox.Text;
            _codexStatusService.WriteStatusFile(path, CodexStatusService.CreateDefaultStatus("settings"));
            UpdateCodexStatusInfoText();
            MessageBox.Show("默认 Codex 状态文件已创建。", "月薪喵设置", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnTestCodexStatusClick(object sender, RoutedEventArgs e)
        {
            CodexStatusEnabledBox.IsChecked = true;
            CodexStatusBubbleBox.IsChecked = true;
            if (!SaveSettings(false, false))
            {
                return;
            }

            if (_codexStatusService != null)
            {
                _codexStatusService.WriteTestStatus(_configService.Current);
            }

            if (_onConfigChanged != null)
            {
                _onConfigChanged();
            }

            UpdateCodexStatusInfoText();
            RefreshDebug();
        }

        private void OnRestoreDefaultsClick(object sender, RoutedEventArgs e)
        {
            _loading = true;
            ClassifiedRadio.IsChecked = true;
            BuiltInRadio.IsChecked = false;
            CustomRadio.IsChecked = false;
            CustomGifDirectoryBox.Text = string.Empty;
            EnableWeatherBox.IsChecked = false;
            WeatherBubbleBox.IsChecked = false;
            WeatherAffectsGifBox.IsChecked = false;
            WeatherIntervalBox.Text = "30";
            CodexStatusEnabledBox.IsChecked = false;
            CodexStatusBubbleBox.IsChecked = true;
            CodexStatusAffectsGifBox.IsChecked = false;
            CodexStatusFilePathBox.Text = FilePathHelper.CodexStatusPath;
            CodexStatusIntervalBox.Text = "2";
            SelectProvinceAndCity("上海市", "上海市");
            LatitudeBox.Text = "31.2304";
            LongitudeBox.Text = "121.4737";
            AlwaysOnTopBox.IsChecked = true;
            StartupBox.IsChecked = false;
            ScaleSlider.Value = 100;
            OpacitySlider.Value = 100;
            WorkStartBox.Text = "09:00";
            WorkEndBox.Text = "18:00";
            LunchStartBox.Text = "12:00";
            LunchEndBox.Text = "13:30";
            EveningStartBox.Text = "20:00";
            SleepTimeBox.Text = "23:30";
            _loading = false;
            UpdateAppearanceText();
            UpdateResourceFields();
            UpdateWeatherFields();
            UpdateCodexStatusInfoText();
            if (_onPreviewAppearance != null)
            {
                _onPreviewAppearance(1.0, 1.0);
            }
        }

        private void OnRefreshDebugClick(object sender, RoutedEventArgs e)
        {
            UpdateWeatherInfoText();
            UpdateCodexStatusInfoText();
            RefreshDebug();
        }

        private void OnRefreshWeatherClick(object sender, RoutedEventArgs e)
        {
            if (EnableWeatherBox.IsChecked != true)
            {
                MessageBox.Show("请先开启“显示天气挂件”。", "月薪喵设置", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_onRefreshWeather != null)
            {
                _onRefreshWeather();
            }

            UpdateWeatherInfoText();
        }

        private void OnRescanClick(object sender, RoutedEventArgs e)
        {
            if (!SaveSettings(false, true))
            {
                return;
            }

            RefreshDebug();
            MessageBox.Show("GIF 已重新扫描。", "月薪喵设置", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            SaveSettings(true, false);
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            if (_onCancelPreview != null)
            {
                _onCancelPreview();
            }

            Close();
        }
    }
}
