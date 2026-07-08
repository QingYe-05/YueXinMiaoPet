using System;
using System.Text;
using YueXinMiaoPet.Models;

namespace YueXinMiaoPet.Services
{
    public class DebugStateService
    {
        private readonly object _syncRoot = new object();
        private DebugSnapshot _snapshot = new DebugSnapshot();

        public void Update(PetState state, string currentGifFile, GifPickResult pickResult)
        {
            lock (_syncRoot)
            {
                _snapshot = new DebugSnapshot
                {
                    State = state == null ? new PetState() : state.Clone(),
                    CurrentGifFile = currentGifFile ?? string.Empty,
                    CurrentGifCategory = pickResult == null || pickResult.Selected == null ? string.Empty : pickResult.Selected.CategoryName,
                    TopCandidates = pickResult == null ? new System.Collections.Generic.List<GifPickCandidate>() : pickResult.TopCandidates
                };
            }
        }

        public void UpdatePlaylist(
            PetState state,
            string currentGifFile,
            GifPlaylistResult playlistResult,
            AppConfig config,
            string weatherBadgeText,
            int moodCustomPlaylistCount,
            int globalCustomPlaylistCount)
        {
            lock (_syncRoot)
            {
                _snapshot = new DebugSnapshot
                {
                    State = state == null ? new PetState() : state.Clone(),
                    CurrentGifFile = currentGifFile ?? string.Empty,
                    CurrentGifCategory = playlistResult == null || playlistResult.Selected == null ? string.Empty : playlistResult.Selected.CategoryName,
                    CurrentPlaybackMode = "Sequential",
                    CurrentPlaylistSource = playlistResult == null ? string.Empty : playlistResult.Source,
                    CurrentPlaylistCount = playlistResult == null ? 0 : playlistResult.PlaylistCount,
                    CurrentPlaylistIndex = playlistResult == null ? 0 : playlistResult.PlaylistIndex,
                    CurrentMoodCategory = playlistResult == null ? string.Empty : playlistResult.MoodCategorySummary,
                    CurrentMoodCustomPlaylistCount = moodCustomPlaylistCount,
                    GlobalCustomPlaylistCount = globalCustomPlaylistCount,
                    WeatherEnabled = config != null && config.WeatherEnabled,
                    WeatherAffectsGif = config != null && config.WeatherAffectsGif,
                    WeatherBadgeText = weatherBadgeText ?? string.Empty,
                    TopCandidates = playlistResult == null ? new System.Collections.Generic.List<GifPickCandidate>() : playlistResult.TopCandidates
                };
            }
        }

        public DebugSnapshot GetSnapshot()
        {
            lock (_syncRoot)
            {
                return _snapshot;
            }
        }

        public string FormatSnapshot()
        {
            DebugSnapshot snapshot = GetSnapshot();
            PetState state = snapshot.State ?? new PetState();
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("当前状态");
            builder.AppendLine("当前 WeatherTag: " + state.WeatherTag);
            builder.AppendLine("当前 TimeTag: " + state.TimeTag);
            builder.AppendLine("当前 MoodTag: " + state.MoodTag);
            builder.AppendLine("当前 MoodTag 对应分类: " + MoodCategoryService.FormatCategories(MoodCategoryService.GetPrimaryCategories(state.MoodTag)));
            builder.AppendLine("当前 ActionTag: " + state.ActionTag);
            builder.AppendLine("当前温度: " + state.Temperature.ToString("0.0") + "℃");
            builder.AppendLine("当前天气码: " + state.WeatherCode);
            builder.AppendLine("心情锁定中: " + (state.IsMoodLocked ? "是" : "否"));
            builder.AppendLine("天气反应中: " + (state.IsWeatherReactionActive ? "是" : "否"));
            builder.AppendLine("当前播放 GIF 文件名: " + snapshot.CurrentGifFile);
            builder.AppendLine("当前播放 GIF 分类: " + snapshot.CurrentGifCategory);
            builder.AppendLine("当前播放模式: " + snapshot.CurrentPlaybackMode);
            builder.AppendLine("当前播放列表来源: " + snapshot.CurrentPlaylistSource);
            builder.AppendLine("当前播放列表数量: " + snapshot.CurrentPlaylistCount);
            builder.AppendLine("当前播放索引: " + snapshot.CurrentPlaylistIndex);
            builder.AppendLine("当前心情自定义轮播数量: " + snapshot.CurrentMoodCustomPlaylistCount);
            builder.AppendLine("全局自定义轮播数量: " + snapshot.GlobalCustomPlaylistCount);
            builder.AppendLine("WeatherEnabled: " + (snapshot.WeatherEnabled ? "true" : "false"));
            builder.AppendLine("WeatherAffectsGif: " + (snapshot.WeatherAffectsGif ? "true" : "false"));
            builder.AppendLine("WeatherBadgeText: " + snapshot.WeatherBadgeText);
            builder.AppendLine();
            builder.AppendLine("当前播放列表前 5 项");

            if (snapshot.TopCandidates == null || snapshot.TopCandidates.Count == 0)
            {
                builder.AppendLine("暂无候选。");
                builder.AppendLine();
                builder.AppendLine("刷新时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                return builder.ToString();
            }

            for (int i = 0; i < snapshot.TopCandidates.Count; i++)
            {
                GifPickCandidate candidate = snapshot.TopCandidates[i];
                string name = candidate.Asset == null ? "(null)" : candidate.Asset.Name;
                string category = candidate.Asset == null ? string.Empty : candidate.Asset.CategoryName;
                builder.Append(i + 1);
                builder.Append(". ");
                builder.Append(name);
                if (!string.IsNullOrWhiteSpace(category))
                {
                    builder.Append(" | category=");
                    builder.Append(category);
                }
                builder.Append(" | order=");
                builder.Append(candidate.Score);
                if (!string.IsNullOrWhiteSpace(candidate.Reason))
                {
                    builder.Append(" | ");
                    builder.Append(candidate.Reason);
                }
                builder.AppendLine();
            }

            builder.AppendLine();
            builder.AppendLine("刷新时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            return builder.ToString();
        }
    }
}
