using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using YueXinMiaoPet.Models;
using YueXinMiaoPet.Services;

namespace YueXinMiaoPet.Views
{
    public partial class PlaylistWindow : Window
    {
        private readonly ConfigService _configService;
        private readonly GifAssetService _assetService;
        private readonly GifPlaylistService _playlistService;
        private readonly Action _onPlaylistSaved;
        private bool _loading;

        public ObservableCollection<PlaylistGifItem> Items { get; private set; }

        public PlaylistWindow(
            ConfigService configService,
            GifAssetService assetService,
            GifPlaylistService playlistService,
            Action onPlaylistSaved)
        {
            InitializeComponent();
            _configService = configService;
            _assetService = assetService;
            _playlistService = playlistService;
            _onPlaylistSaved = onPlaylistSaved;
            Items = new ObservableCollection<PlaylistGifItem>();
            DataContext = this;

            LoadOptions();
            RebuildItems();
        }

        private void LoadOptions()
        {
            _loading = true;
            MoodCombo.ItemsSource = MoodCategoryService.GetMoodOptions();
            MoodCombo.SelectedValue = MoodCategoryService.NormalizeMood(_configService.Current.MoodTag);
            if (MoodCombo.SelectedItem == null)
            {
                MoodCombo.SelectedValue = "neutral";
            }

            SourceCombo.ItemsSource = new List<FilterOption>
            {
                new FilterOption { Key = "mood", Name = "当前心情 GIF" },
                new FilterOption { Key = "all", Name = "全部 GIF" }
            };
            SourceCombo.SelectedValue = "mood";
            UseGlobalPlaylistBox.IsChecked = _configService.Current.UseGlobalCustomPlaylist;
            _loading = false;
        }

        private void RebuildItems()
        {
            if (_loading || Items == null)
            {
                return;
            }

            string mood = GetSelectedMood();
            string source = GetSelectedSource();
            string query = (SearchBox.Text ?? string.Empty).Trim();
            bool editingGlobal = UseGlobalPlaylistBox.IsChecked == true;
            HashSet<string> selectedKeys = LoadSelectedKeys(mood, editingGlobal);
            IList<string> moodCategories = MoodCategoryService.GetPrimaryCategories(mood);

            IEnumerable<GifAsset> assets = _assetService.Assets.Where(a => a != null);
            if (string.Equals(source, "mood", StringComparison.OrdinalIgnoreCase))
            {
                assets = assets.Where(a => moodCategories.Any(category => a.HasCategory(category)));
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                assets = assets.Where(a =>
                    ContainsText(a.Name, query) ||
                    ContainsText(a.File, query) ||
                    ContainsText(Path.GetFileName(a.File ?? string.Empty), query));
            }

            List<GifAsset> sorted = assets
                .OrderBy(a => MoodCategoryService.GetCanonicalCategory(a.CategoryName) ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(a => Path.GetFileName((a.File ?? string.Empty).Replace('/', Path.DirectorySeparatorChar)), StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(a => a.File ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Items.Clear();
            for (int i = 0; i < sorted.Count; i++)
            {
                GifAsset asset = sorted[i];
                string key = _playlistService.GetStableKey(asset);
                Items.Add(new PlaylistGifItem
                {
                    Asset = asset,
                    IsSelected = selectedKeys.Contains(_playlistService.NormalizePlaylistKey(key)),
                    FileName = Path.GetFileName((asset.File ?? string.Empty).Replace('/', Path.DirectorySeparatorChar)),
                    CategoryName = string.IsNullOrWhiteSpace(asset.CategoryName) ? "(未分类)" : asset.CategoryName,
                    RelativePath = key,
                    Enabled = asset.Enabled
                });
            }

            UpdateStatus();
        }

        private HashSet<string> LoadSelectedKeys(string mood, bool editingGlobal)
        {
            HashSet<string> keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            IList<string> sourceKeys = null;
            AppConfig config = _configService.Current;

            if (editingGlobal)
            {
                sourceKeys = config.GlobalCustomPlaylist;
            }
            else if (config.MoodCustomPlaylists != null)
            {
                List<string> moodKeys;
                if (config.MoodCustomPlaylists.TryGetValue(mood, out moodKeys))
                {
                    sourceKeys = moodKeys;
                }
            }

            if (sourceKeys == null)
            {
                return keys;
            }

            for (int i = 0; i < sourceKeys.Count; i++)
            {
                string normalized = _playlistService.NormalizePlaylistKey(sourceKeys[i]);
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    keys.Add(normalized);
                }
            }

            return keys;
        }

        private bool ContainsText(string source, string query)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return false;
            }

            return source.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        private string GetSelectedMood()
        {
            return MoodCategoryService.NormalizeMood(MoodCombo.SelectedValue as string);
        }

        private string GetSelectedSource()
        {
            return (SourceCombo.SelectedValue as string) ?? "mood";
        }

        private List<string> GetSelectedKeys()
        {
            return Items
                .Where(i => i.IsSelected && i.Enabled)
                .Select(i => _playlistService.NormalizePlaylistKey(i.RelativePath))
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private bool SaveCurrentSelection(bool showMessage)
        {
            string mood = GetSelectedMood();
            bool editingGlobal = UseGlobalPlaylistBox.IsChecked == true;
            List<string> selected = GetSelectedKeys();

            _configService.Update(config =>
            {
                if (config.MoodCustomPlaylists == null)
                {
                    config.MoodCustomPlaylists = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                }

                if (editingGlobal)
                {
                    config.UseGlobalCustomPlaylist = true;
                    config.GlobalCustomPlaylist = selected;
                }
                else
                {
                    if (selected.Count == 0)
                    {
                        config.MoodCustomPlaylists.Remove(mood);
                    }
                    else
                    {
                        config.MoodCustomPlaylists[mood] = selected;
                    }
                }
            });

            if (_onPlaylistSaved != null)
            {
                _onPlaylistSaved();
            }

            UpdateStatus();
            if (showMessage)
            {
                MessageBox.Show("GIF 轮播设置已保存并立即生效。", "月薪喵桌宠", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            return true;
        }

        private void UpdateStatus()
        {
            string mood = GetSelectedMood();
            int selected = Items.Count(i => i.IsSelected && i.Enabled);
            int moodCustom = _playlistService.GetMoodCustomPlaylistCount(_configService.Current, mood);
            int globalCustom = _playlistService.GetGlobalCustomPlaylistCount(_configService.Current);
            StatusText.Text = "当前筛选结果：" + Items.Count +
                "，已选：" + selected +
                "，当前心情自定义轮播：" + moodCustom +
                "，全局自定义轮播：" + globalCustom +
                "，当前编辑：" + (UseGlobalPlaylistBox.IsChecked == true ? "全局自定义轮播" : "当前心情轮播");
        }

        private void OnFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            RebuildItems();
        }

        private void OnSearchChanged(object sender, TextChangedEventArgs e)
        {
            RebuildItems();
        }

        private void OnGlobalPlaylistChanged(object sender, RoutedEventArgs e)
        {
            RebuildItems();
        }

        private void OnSelectAllFilteredClick(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].Enabled)
                {
                    Items[i].IsSelected = true;
                }
            }

            GifGrid.Items.Refresh();
            UpdateStatus();
        }

        private void OnClearMoodClick(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                Items[i].IsSelected = false;
            }

            GifGrid.Items.Refresh();
            UpdateStatus();
        }

        private void OnSaveCurrentClick(object sender, RoutedEventArgs e)
        {
            SaveCurrentSelection(true);
        }

        private void OnRestoreDefaultClick(object sender, RoutedEventArgs e)
        {
            string mood = GetSelectedMood();
            _configService.Update(config =>
            {
                if (config.MoodCustomPlaylists != null)
                {
                    config.MoodCustomPlaylists.Remove(mood);
                }
            });

            for (int i = 0; i < Items.Count; i++)
            {
                Items[i].IsSelected = false;
            }

            if (_onPlaylistSaved != null)
            {
                _onPlaylistSaved();
            }

            GifGrid.Items.Refresh();
            UpdateStatus();
            MessageBox.Show("已恢复为该心情分类下全部 GIF 顺序轮播。", "月薪喵桌宠", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnSaveAndCloseClick(object sender, RoutedEventArgs e)
        {
            SaveCurrentSelection(false);
            Close();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public class PlaylistGifItem
        {
            public GifAsset Asset { get; set; }
            public bool IsSelected { get; set; }
            public string FileName { get; set; }
            public string CategoryName { get; set; }
            public string RelativePath { get; set; }
            public bool Enabled { get; set; }
        }

        private class FilterOption
        {
            public string Key { get; set; }
            public string Name { get; set; }
        }
    }
}
