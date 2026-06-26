using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YueXinMiaoPet.Models;

namespace YueXinMiaoPet.Services
{
    public class GifPicker
    {
        private readonly Random _random = new Random();
        private string _lastAssetId = string.Empty;
        private int _repeatCount = 0;

        public GifPickResult Pick(PetState state, IList<GifAsset> assets)
        {
            GifPickResult result = new GifPickResult();
            if (assets == null || assets.Count == 0)
            {
                return result;
            }

            state = state ?? new PetState();

            List<GifPickCandidate> scored = new List<GifPickCandidate>();
            for (int i = 0; i < assets.Count; i++)
            {
                GifAsset asset = assets[i];
                if (asset == null || !asset.Enabled)
                {
                    continue;
                }

                scored.Add(ScoreAsset(state, asset));
            }

            if (scored.Count == 0)
            {
                return result;
            }

            List<GifPickCandidate> sorted = scored
                .OrderByDescending(c => c.Score)
                .ThenByDescending(c => c.Asset.Weight)
                .ThenBy(c => c.Asset.CategoryTag)
                .ThenBy(c => c.Asset.Name)
                .ToList();

            result.TopCandidates = sorted.Take(5).ToList();

            List<GifPickCandidate> candidatePool = BuildPriorityPool(state, sorted);
            if (_repeatCount >= 3 && candidatePool.Count > 1)
            {
                candidatePool = candidatePool.Where(c => c.Asset.Id != _lastAssetId).ToList();
            }

            if (candidatePool.Count == 0)
            {
                candidatePool = sorted.Take(10).ToList();
            }

            result.Selected = WeightedPick(candidatePool);
            Remember(result.Selected);
            return result;
        }

        public GifPickResult PickForCurrentMoodInteraction(PetState state, IList<GifAsset> assets)
        {
            GifPickResult result = new GifPickResult();
            if (assets == null || assets.Count == 0)
            {
                return result;
            }

            state = state ?? new PetState();
            state.ActionTag = "touch";
            string mood = MoodCategoryService.NormalizeMood(state.MoodTag);
            IList<string> categories = MoodCategoryService.GetPrimaryCategories(mood);

            List<GifAsset> pool = FilterEnabledByCategories(assets, categories);
            string fallbackReason = "current_mood";

            if (pool.Count == 0 && !string.Equals(mood, "neutral", StringComparison.OrdinalIgnoreCase))
            {
                categories = MoodCategoryService.GetPrimaryCategories("neutral");
                pool = FilterEnabledByCategories(assets, categories);
                fallbackReason = "fallback_neutral_category_empty";
            }

            if (pool.Count == 0)
            {
                pool = assets.Where(a => a != null && a.Enabled).ToList();
                fallbackReason = "fallback_all_enabled";
            }

            List<GifPickCandidate> candidates = pool
                .Select(asset => ScoreAsset(state, asset))
                .OrderByDescending(c => c.Score)
                .ThenByDescending(c => c.Asset.Weight)
                .ThenBy(c => c.Asset.CategoryName)
                .ThenBy(c => c.Asset.Name)
                .ToList();

            result.TopCandidates = candidates.Take(5).ToList();
            List<GifPickCandidate> candidatePool = TakePool(candidates, Math.Min(10, candidates.Count));
            if (_repeatCount >= 3 && candidatePool.Count > 1)
            {
                candidatePool = candidatePool.Where(c => c.Asset.Id != _lastAssetId).ToList();
            }

            result.Selected = WeightedPick(candidatePool);
            Remember(result.Selected);

            string selected = result.Selected == null ? "(null)" : result.Selected.Name + " / " + result.Selected.CategoryName;
            LogService.Info("点击选择 GIF：MoodTag=" + mood +
                "，categories=" + MoodCategoryService.FormatCategories(categories) +
                "，candidateCount=" + candidates.Count +
                "，selected=" + selected +
                "，fallback=" + fallbackReason);

            return result;
        }

        private List<GifPickCandidate> BuildPriorityPool(PetState state, List<GifPickCandidate> sorted)
        {
            string action = state.ActionTag ?? string.Empty;
            if (!string.Equals(action, "idle", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(action))
            {
                string actionMood = MoodCategoryService.NormalizeMood(state.MoodTag);
                List<GifPickCandidate> moodInteractionPool = TakePoolByCategories(sorted, MoodCategoryService.GetPrimaryCategories(actionMood), 10);
                if (moodInteractionPool.Count > 0)
                {
                    return moodInteractionPool;
                }

                List<GifPickCandidate> actionPool = TakePool(sorted.Where(c => c.Asset.HasTag(action)), 10);
                if (actionPool.Count > 0)
                {
                    return actionPool;
                }
            }

            string mood = MoodCategoryService.NormalizeMood(state.MoodTag);
            List<GifPickCandidate> primaryMood = TakePoolByCategories(sorted, MoodCategoryService.GetPrimaryCategories(mood), 10);
            if (!string.Equals(mood, "neutral", StringComparison.OrdinalIgnoreCase) && primaryMood.Count > 0)
            {
                return primaryMood;
            }

            // 天气刷新后的短暂反应期只在普通心情或没有可用心情池时生效，避免覆盖当前心情。
            if (state.IsWeatherReactionActive && !state.IsMoodLocked)
            {
                List<GifPickCandidate> weatherReaction = TakePoolByCategories(sorted, MoodCategoryService.GetWeatherCategories(state.WeatherTag), 10);
                if (weatherReaction.Count > 0)
                {
                    return weatherReaction;
                }
            }

            if (primaryMood.Count > 0)
            {
                return primaryMood;
            }

            List<GifPickCandidate> fallbackMood = TakePoolByCategories(sorted, MoodCategoryService.GetFallbackCategories(mood), 10);
            if (fallbackMood.Count > 0)
            {
                return fallbackMood;
            }

            List<GifPickCandidate> workOrTime = TakePoolByCategories(sorted, MoodCategoryService.GetTimeCategories(state), 10);
            if (workOrTime.Count > 0)
            {
                return workOrTime;
            }

            List<GifPickCandidate> moodTags = TakePool(sorted.Where(c => c.Asset.HasTag(mood)), 10);
            if (moodTags.Count > 0)
            {
                return moodTags;
            }

            List<GifPickCandidate> weatherPool = TakePoolByCategories(sorted, MoodCategoryService.GetWeatherCategories(state.WeatherTag), 10);
            if (weatherPool.Count > 0)
            {
                return weatherPool;
            }

            List<GifPickCandidate> weatherTags = TakePool(sorted.Where(c => c.Asset.HasTag(state.WeatherTag)), 10);
            if (weatherTags.Count > 0)
            {
                return weatherTags;
            }

            List<GifPickCandidate> idle = TakePool(sorted.Where(c => c.Asset.HasTag("idle") || c.Asset.HasCategory("01_普通")), 10);
            return idle.Count > 0 ? idle : sorted.Take(10).ToList();
        }

        private List<GifAsset> FilterEnabledByCategories(IList<GifAsset> assets, IList<string> categories)
        {
            if (assets == null || categories == null || categories.Count == 0)
            {
                return new List<GifAsset>();
            }

            return assets
                .Where(a => a != null && a.Enabled && categories.Any(category => a.HasCategory(category)))
                .ToList();
        }

        private List<GifPickCandidate> TakePoolByCategories(IEnumerable<GifPickCandidate> candidates, IList<string> categories, int count)
        {
            if (categories == null || categories.Count == 0)
            {
                return new List<GifPickCandidate>();
            }

            return TakePool(candidates.Where(c => c.Asset != null && categories.Any(category => c.Asset.HasCategory(category))), count);
        }

        private List<GifPickCandidate> TakePool(IEnumerable<GifPickCandidate> candidates, int count)
        {
            return candidates
                .OrderByDescending(c => c.Score)
                .ThenByDescending(c => c.Asset.Weight)
                .Take(count)
                .ToList();
        }

        private GifPickCandidate ScoreAsset(PetState state, GifAsset asset)
        {
            int score = 0;
            StringBuilder reason = new StringBuilder();

            string action = state.ActionTag ?? string.Empty;
            if (!string.Equals(action, "idle", StringComparison.OrdinalIgnoreCase))
            {
                AddIfTag(asset, action, 120, "action", ref score, reason);
            }

            string mood = MoodCategoryService.NormalizeMood(state.MoodTag);
            AddCategoryMatches(asset, MoodCategoryService.GetPrimaryCategories(mood), 100, "mood-category", ref score, reason);
            AddCategoryMatches(asset, MoodCategoryService.GetFallbackCategories(mood), 60, "mood-fallback", ref score, reason);

            if (state.IsWorkingTime && asset.HasTag("work_start"))
            {
                score += 35;
                reason.Append("work_start+35 ");
            }

            if (state.IsAfterWork && asset.HasTag("work_end"))
            {
                score += 35;
                reason.Append("work_end+35 ");
            }

            AddCategoryMatches(asset, MoodCategoryService.GetTimeCategories(state), 25, "time-category", ref score, reason);

            int weatherCategoryScore = state.IsWeatherReactionActive && !state.IsMoodLocked ? 55 : 22;
            AddCategoryMatches(asset, MoodCategoryService.GetWeatherCategories(state.WeatherTag), weatherCategoryScore, "weather-category", ref score, reason);

            AddIfTag(asset, mood, 20, "mood-tag", ref score, reason);
            AddIfTag(asset, state.WeatherTag, state.IsWeatherReactionActive ? 18 : 10, "weather-tag", ref score, reason);
            AddIfTag(asset, state.TimeTag, 8, "time-tag", ref score, reason);

            if (asset.HasTag("idle"))
            {
                score += 1;
                reason.Append("idle+1 ");
            }

            return new GifPickCandidate
            {
                Asset = asset,
                Score = score,
                CategoryName = asset.CategoryName,
                Reason = reason.ToString().Trim()
            };
        }

        private void AddCategoryMatches(GifAsset asset, IList<string> categories, int points, string label, ref int score, StringBuilder reason)
        {
            if (asset == null || categories == null)
            {
                return;
            }

            for (int i = 0; i < categories.Count; i++)
            {
                string category = categories[i];
                if (asset.HasCategory(category))
                {
                    score += points;
                    reason.Append(label);
                    reason.Append(":");
                    reason.Append(category);
                    reason.Append("+");
                    reason.Append(points);
                    reason.Append(" ");
                    return;
                }
            }
        }

        private void AddIfTag(GifAsset asset, string tag, int points, string label, ref int score, StringBuilder reason)
        {
            if (asset.HasTag(tag))
            {
                score += points;
                reason.Append(label);
                reason.Append(":");
                reason.Append(tag);
                reason.Append("+");
                reason.Append(points);
                reason.Append(" ");
            }
        }

        private GifAsset WeightedPick(List<GifPickCandidate> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            int total = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                total += Math.Max(1, candidates[i].Asset.Weight) + Math.Max(0, candidates[i].Score);
            }

            int roll = _random.Next(1, total + 1);
            int current = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                current += Math.Max(1, candidates[i].Asset.Weight) + Math.Max(0, candidates[i].Score);
                if (roll <= current)
                {
                    return candidates[i].Asset;
                }
            }

            return candidates[0].Asset;
        }

        private void Remember(GifAsset selected)
        {
            if (selected == null)
            {
                return;
            }

            if (string.Equals(selected.Id, _lastAssetId, StringComparison.OrdinalIgnoreCase))
            {
                _repeatCount++;
            }
            else
            {
                _lastAssetId = selected.Id;
                _repeatCount = 1;
            }
        }
    }
}
