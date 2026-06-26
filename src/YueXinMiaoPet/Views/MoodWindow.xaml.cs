using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YueXinMiaoPet.Models;
using YueXinMiaoPet.Services;

namespace YueXinMiaoPet.Views
{
    public partial class MoodWindow : Window
    {
        private readonly MoodService _moodService;
        private readonly GifAssetService _assetService;
        private readonly ConfigService _configService;
        private readonly Action _onMoodChanged;
        private readonly Dictionary<string, Button> _cardButtons = new Dictionary<string, Button>(StringComparer.OrdinalIgnoreCase);
        private string _selectedMood;

        public MoodWindow(MoodService moodService, GifAssetService assetService, ConfigService configService, Action onMoodChanged)
        {
            InitializeComponent();
            _moodService = moodService;
            _assetService = assetService;
            _configService = configService;
            _onMoodChanged = onMoodChanged;
            _selectedMood = _moodService.GetCurrentMood();

            BuildDurationOptions();
            BuildMoodCards();
            SelectMood(_selectedMood);
        }

        private void BuildDurationOptions()
        {
            DurationCombo.ItemsSource = new List<DurationOption>
            {
                new DurationOption { Key = "thirty_minutes", Name = "30分钟" },
                new DurationOption { Key = "one_hour", Name = "1小时" },
                new DurationOption { Key = "two_hours", Name = "2小时" },
                new DurationOption { Key = "today", Name = "今天有效" },
                new DurationOption { Key = "forever", Name = "一直有效" }
            };

            string mode = string.IsNullOrWhiteSpace(_configService.Current.MoodExpireMode)
                ? "today"
                : _configService.Current.MoodExpireMode;
            DurationCombo.SelectedValue = mode;
            if (DurationCombo.SelectedItem == null)
            {
                DurationCombo.SelectedValue = "today";
            }
        }

        private void BuildMoodCards()
        {
            MoodCardsPanel.Children.Clear();
            _cardButtons.Clear();

            foreach (MoodOption option in _moodService.GetMoodCards())
            {
                Button button = new Button
                {
                    Width = 200,
                    MinHeight = 96,
                    Margin = new Thickness(0, 0, 10, 10),
                    Padding = new Thickness(10),
                    Tag = option.Key,
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(226, 198, 173)),
                    BorderThickness = new Thickness(1),
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    VerticalContentAlignment = VerticalAlignment.Stretch
                };

                StackPanel panel = new StackPanel();
                TextBlock title = new TextBlock
                {
                    Text = option.Emoji + "  " + option.Name,
                    FontSize = 15,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(74, 47, 36)),
                    TextWrapping = TextWrapping.Wrap
                };
                TextBlock description = new TextBlock
                {
                    Text = option.Description,
                    Margin = new Thickness(0, 6, 0, 0),
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 85, 78)),
                    TextWrapping = TextWrapping.Wrap
                };
                TextBlock category = new TextBlock
                {
                    Text = option.CategorySummary,
                    Margin = new Thickness(0, 6, 0, 0),
                    Foreground = new SolidColorBrush(Color.FromRgb(143, 105, 80)),
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap
                };

                panel.Children.Add(title);
                panel.Children.Add(description);
                panel.Children.Add(category);
                button.Content = panel;
                button.Click += OnMoodCardClick;

                _cardButtons[option.Key] = button;
                MoodCardsPanel.Children.Add(button);
            }
        }

        private void OnMoodCardClick(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null)
            {
                return;
            }

            SelectMood(button.Tag as string);
        }

        private void SelectMood(string mood)
        {
            mood = MoodCategoryService.NormalizeMood(mood);

            _selectedMood = mood;
            MoodOption option = MoodCategoryService.GetMoodOption(mood);
            SelectedMoodText.Text = option.Emoji + " " + option.Name;
            UpdateCardHighlights();
            UpdatePreview(option);
        }

        private void UpdateCardHighlights()
        {
            foreach (KeyValuePair<string, Button> pair in _cardButtons)
            {
                bool selected = string.Equals(pair.Key, _selectedMood, StringComparison.OrdinalIgnoreCase);
                pair.Value.Background = selected
                    ? new SolidColorBrush(Color.FromRgb(255, 237, 217))
                    : Brushes.White;
                pair.Value.BorderBrush = selected
                    ? new SolidColorBrush(Color.FromRgb(184, 113, 70))
                    : new SolidColorBrush(Color.FromRgb(226, 198, 173));
                pair.Value.BorderThickness = selected ? new Thickness(2) : new Thickness(1);
            }
        }

        private void UpdatePreview(MoodOption option)
        {
            GifAsset asset = FindPreviewAsset(option.Key);
            if (asset == null)
            {
                PreviewImage.GifPath = string.Empty;
                PreviewInfoText.Text = "还没有找到可预览的 GIF。请确认内置或自定义目录里存在 GIF。";
                return;
            }

            string path = _assetService.ResolveAssetPath(asset, _configService.Current);
            if (File.Exists(path))
            {
                PreviewImage.GifPath = path;
            }

            PreviewInfoText.Text = "分类：" + (string.IsNullOrWhiteSpace(asset.CategoryName) ? "未分类" : asset.CategoryName) +
                "\n文件：" + Path.GetFileName(asset.File);
        }

        private GifAsset FindPreviewAsset(string mood)
        {
            List<GifAsset> assets = _assetService.GetEnabledExistingAssets(_configService.Current);
            if (assets.Count == 0)
            {
                return null;
            }

            IList<string> primary = MoodCategoryService.GetPrimaryCategories(mood);
            GifAsset asset = assets.FirstOrDefault(a => primary.Any(category => a.HasCategory(category)));
            if (asset != null)
            {
                return asset;
            }

            IList<string> fallback = MoodCategoryService.GetFallbackCategories(mood);
            asset = assets.FirstOrDefault(a => fallback.Any(category => a.HasCategory(category)));
            if (asset != null)
            {
                return asset;
            }

            asset = assets.FirstOrDefault(a => a.HasTag(mood));
            return asset ?? assets[0];
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            string duration = DurationCombo.SelectedValue as string;
            _moodService.SetMood(_selectedMood, duration);

            if (_onMoodChanged != null)
            {
                _onMoodChanged();
            }

            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private class DurationOption
        {
            public string Key { get; set; }
            public string Name { get; set; }
        }
    }
}
