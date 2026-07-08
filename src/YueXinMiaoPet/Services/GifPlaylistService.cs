using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YueXinMiaoPet.Models;

namespace YueXinMiaoPet.Services
{
    public class GifPlaylistResult
    {
        public GifAsset Selected { get; set; }
        public List<GifAsset> Playlist { get; set; }
        public string Source { get; set; }
        public string MoodTag { get; set; }
        public int PlaylistCount { get; set; }
        public int PlaylistIndex { get; set; }
        public string MoodCategorySummary { get; set; }
        public List<GifPickCandidate> TopCandidates { get; set; }

        public GifPlaylistResult()
        {
            Playlist = new List<GifAsset>();
            Source = "AllFallback";
            MoodTag = "neutral";
            PlaylistIndex = 0;
            MoodCategorySummary = string.Empty;
            TopCandidates = new List<GifPickCandidate>();
        }
    }

    /// <summary>
    /// GIF 顺序轮播服务。
    /// 它替代旧随机/权重选择逻辑，保证当前心情下的 GIF 按固定顺序循环播放。
    /// </summary>
    public class GifPlaylistService
    {
        public const string SourceMoodCustomPlaylist = "MoodCustomPlaylist";
        public const string SourceGlobalCustomPlaylist = "GlobalCustomPlaylist";
        public const string SourceMoodDefaultSequential = "MoodDefaultSequential";
        public const string SourceNeutralFallback = "NeutralFallback";
        public const string SourceAllFallback = "AllFallback";

        private readonly Dictionary<string, int> _indexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private string _lastPlaylistKey = string.Empty;

        public GifPlaylistResult LastResult { get; private set; }

        public GifPlaylistService()
        {
            LastResult = new GifPlaylistResult();
        }

        public GifPlaylistResult NextGif(PetState state, IList<GifAsset> allAssets, AppConfig config, bool resetIndex)
        {
            string mood = NormalizeMood(state == null ? null : state.MoodTag);
            GifPlaylistResult result = GetPlaylistForMood(mood, allAssets, config);
            if (result.Playlist == null || result.Playlist.Count == 0)
            {
                LastResult = result;
                return result;
            }

            string key = GetIndexKey(result.Source, mood);
            if (resetIndex || !string.Equals(_lastPlaylistKey, key, StringComparison.OrdinalIgnoreCase))
            {
                _indexes[key] = 0;
            }

            int index;
            if (!_indexes.TryGetValue(key, out index))
            {
                index = 0;
            }

            if (index < 0 || index >= result.Playlist.Count)
            {
                index = 0;
            }

            result.Selected = result.Playlist[index];
            result.PlaylistIndex = index + 1;
            result.PlaylistCount = result.Playlist.Count;
            result.TopCandidates = BuildSequentialCandidates(result.Playlist, index);

            _indexes[key] = result.Playlist.Count == 0 ? 0 : (index + 1) % result.Playlist.Count;
            _lastPlaylistKey = key;
            LastResult = result;
            return result;
        }

        public GifPlaylistResult GetPlaylistForMood(string moodTag, IList<GifAsset> allAssets, AppConfig config)
        {
            string mood = NormalizeMood(moodTag);
            List<GifAsset> enabled = SortAssets(FilterEnabled(allAssets));
            GifPlaylistResult result = new GifPlaylistResult
            {
                MoodTag = mood,
                MoodCategorySummary = MoodCategoryService.FormatCategories(MoodCategoryService.GetPrimaryCategories(mood))
            };

            if (enabled.Count == 0)
            {
                result.Source = SourceAllFallback;
                result.Playlist = new List<GifAsset>();
                result.PlaylistCount = 0;
                result.TopCandidates = new List<GifPickCandidate>();
                return result;
            }

            List<GifAsset> moodCustom = ResolveCustomPlaylist(GetMoodCustomPlaylist(config, mood), enabled, SourceMoodCustomPlaylist);
            if (moodCustom.Count > 0)
            {
                result.Source = SourceMoodCustomPlaylist;
                result.Playlist = moodCustom;
                result.PlaylistCount = moodCustom.Count;
                result.TopCandidates = BuildSequentialCandidates(moodCustom, 0);
                return result;
            }

            if (config != null && config.UseGlobalCustomPlaylist)
            {
                List<GifAsset> globalCustom = ResolveCustomPlaylist(config.GlobalCustomPlaylist, enabled, SourceGlobalCustomPlaylist);
                if (globalCustom.Count > 0)
                {
                    result.Source = SourceGlobalCustomPlaylist;
                    result.Playlist = globalCustom;
                    result.PlaylistCount = globalCustom.Count;
                    result.TopCandidates = BuildSequentialCandidates(globalCustom, 0);
                    return result;
                }
            }

            List<GifAsset> moodDefault = FilterByCategories(enabled, MoodCategoryService.GetPrimaryCategories(mood));
            if (moodDefault.Count > 0)
            {
                result.Source = SourceMoodDefaultSequential;
                result.Playlist = moodDefault;
                result.PlaylistCount = moodDefault.Count;
                result.TopCandidates = BuildSequentialCandidates(moodDefault, 0);
                return result;
            }

            List<GifAsset> neutral = FilterByCategories(enabled, MoodCategoryService.GetPrimaryCategories("neutral"));
            if (neutral.Count > 0)
            {
                result.Source = SourceNeutralFallback;
                result.Playlist = neutral;
                result.PlaylistCount = neutral.Count;
                result.TopCandidates = BuildSequentialCandidates(neutral, 0);
                return result;
            }

            result.Source = SourceAllFallback;
            result.Playlist = enabled;
            result.PlaylistCount = enabled.Count;
            result.TopCandidates = BuildSequentialCandidates(enabled, 0);
            return result;
        }

        public void ResetMoodIndex(string moodTag)
        {
            string mood = NormalizeMood(moodTag);
            List<string> keys = _indexes.Keys.Where(k => k.EndsWith("|" + mood, StringComparison.OrdinalIgnoreCase)).ToList();
            for (int i = 0; i < keys.Count; i++)
            {
                _indexes[keys[i]] = 0;
            }
        }

        public void ResetAll()
        {
            _indexes.Clear();
            _lastPlaylistKey = string.Empty;
        }

        public string NormalizeMood(string moodTag)
        {
            return MoodCategoryService.NormalizeMood(moodTag);
        }

        public IList<string> GetMoodCategories(string moodTag)
        {
            return MoodCategoryService.GetPrimaryCategories(NormalizeMood(moodTag));
        }

        public string GetStableKey(GifAsset asset)
        {
            if (asset == null)
            {
                return string.Empty;
            }

            return NormalizePlaylistKey(asset.File);
        }

        public string NormalizePlaylistKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            string normalized = key.Trim().Replace('\\', '/');
            while (normalized.StartsWith("./", StringComparison.Ordinal))
            {
                normalized = normalized.Substring(2);
            }

            if (normalized.StartsWith("PetAssets/", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring("PetAssets/".Length);
            }

            return normalized.TrimStart('/');
        }

        public int GetMoodCustomPlaylistCount(AppConfig config, string moodTag)
        {
            List<string> list = GetMoodCustomPlaylist(config, NormalizeMood(moodTag));
            return list == null ? 0 : list.Count;
        }

        public int GetGlobalCustomPlaylistCount(AppConfig config)
        {
            return config == null || config.GlobalCustomPlaylist == null ? 0 : config.GlobalCustomPlaylist.Count;
        }

        private List<GifAsset> FilterEnabled(IList<GifAsset> assets)
        {
            if (assets == null)
            {
                return new List<GifAsset>();
            }

            return assets.Where(a => a != null && a.Enabled).ToList();
        }

        private List<GifAsset> SortAssets(IEnumerable<GifAsset> assets)
        {
            return assets
                .OrderBy(a => MoodCategoryService.GetCanonicalCategory(a.CategoryName) ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(a => GetFileNameForSort(a), StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(a => a.File ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private string GetFileNameForSort(GifAsset asset)
        {
            if (asset == null || string.IsNullOrWhiteSpace(asset.File))
            {
                return string.Empty;
            }

            string normalized = asset.File.Replace('/', Path.DirectorySeparatorChar);
            return Path.GetFileName(normalized) ?? asset.Name ?? string.Empty;
        }

        private List<string> GetMoodCustomPlaylist(AppConfig config, string mood)
        {
            if (config == null || config.MoodCustomPlaylists == null)
            {
                return new List<string>();
            }

            List<string> list;
            if (config.MoodCustomPlaylists.TryGetValue(NormalizeMood(mood), out list) && list != null)
            {
                return list;
            }

            return new List<string>();
        }

        private List<GifAsset> ResolveCustomPlaylist(IList<string> keys, IList<GifAsset> enabledAssets, string sourceName)
        {
            List<GifAsset> result = new List<GifAsset>();
            if (keys == null || keys.Count == 0 || enabledAssets == null || enabledAssets.Count == 0)
            {
                return result;
            }

            Dictionary<string, GifAsset> map = new Dictionary<string, GifAsset>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < enabledAssets.Count; i++)
            {
                GifAsset asset = enabledAssets[i];
                AddAssetKey(map, GetStableKey(asset), asset);
                AddAssetKey(map, asset.File, asset);
                AddAssetKey(map, asset.Id, asset);
            }

            for (int i = 0; i < keys.Count; i++)
            {
                string normalizedKey = NormalizePlaylistKey(keys[i]);
                GifAsset asset;
                if (map.TryGetValue(normalizedKey, out asset))
                {
                    result.Add(asset);
                    continue;
                }

                if (map.TryGetValue(keys[i] ?? string.Empty, out asset))
                {
                    result.Add(asset);
                    continue;
                }

                LogService.Warn(sourceName + " 中的 GIF 已不存在或已禁用，已跳过：" + keys[i]);
            }

            return result;
        }

        private void AddAssetKey(Dictionary<string, GifAsset> map, string key, GifAsset asset)
        {
            string normalized = NormalizePlaylistKey(key);
            if (!string.IsNullOrWhiteSpace(normalized) && !map.ContainsKey(normalized))
            {
                map[normalized] = asset;
            }

            if (!string.IsNullOrWhiteSpace(key) && !map.ContainsKey(key))
            {
                map[key] = asset;
            }
        }

        private List<GifAsset> FilterByCategories(IList<GifAsset> assets, IList<string> categories)
        {
            if (assets == null || categories == null || categories.Count == 0)
            {
                return new List<GifAsset>();
            }

            return SortAssets(assets.Where(asset =>
                categories.Any(category => asset.HasCategory(category))));
        }

        private List<GifPickCandidate> BuildSequentialCandidates(IList<GifAsset> playlist, int startIndex)
        {
            List<GifPickCandidate> result = new List<GifPickCandidate>();
            if (playlist == null || playlist.Count == 0)
            {
                return result;
            }

            int count = Math.Min(5, playlist.Count);
            for (int i = 0; i < count; i++)
            {
                int index = (startIndex + i) % playlist.Count;
                GifAsset asset = playlist[index];
                result.Add(new GifPickCandidate
                {
                    Asset = asset,
                    CategoryName = asset == null ? string.Empty : asset.CategoryName,
                    Score = playlist.Count - i,
                    Reason = "sequential#" + (index + 1).ToString()
                });
            }

            return result;
        }

        private string GetIndexKey(string source, string mood)
        {
            if (string.Equals(source, SourceGlobalCustomPlaylist, StringComparison.OrdinalIgnoreCase))
            {
                return SourceGlobalCustomPlaylist + "|global";
            }

            return (source ?? SourceAllFallback) + "|" + NormalizeMood(mood);
        }
    }
}
