using System;
using System.Collections.Generic;
using YueXinMiaoPet.Models;

namespace YueXinMiaoPet.Services
{
    public class MoodService
    {
        private readonly ConfigService _configService;

        public MoodService(ConfigService configService)
        {
            _configService = configService;
        }

        public Dictionary<string, string> GetMoodOptions()
        {
            return MoodCategoryService.GetMoodDisplayMap();
        }

        public IList<MoodOption> GetMoodCards()
        {
            return MoodCategoryService.GetMoodOptions();
        }

        public string GetCurrentMood()
        {
            AppConfig config = _configService.Current;
            string expiresText = !string.IsNullOrWhiteSpace(config.MoodExpireAt) ? config.MoodExpireAt : config.MoodExpiresAtUtc;

            if (!string.IsNullOrWhiteSpace(expiresText))
            {
                DateTime expiresAt;
                if (DateTime.TryParse(expiresText, null, System.Globalization.DateTimeStyles.RoundtripKind, out expiresAt))
                {
                    if (DateTime.UtcNow > expiresAt.ToUniversalTime())
                    {
                        SetMood("neutral", "forever");
                        LogService.Info("心情已过期，自动恢复普通。");
                        return "neutral";
                    }
                }
            }

            string mood = !string.IsNullOrWhiteSpace(config.MoodTag) ? config.MoodTag : config.CurrentMood;
            return MoodCategoryService.NormalizeMood(mood);
        }

        public void SetMood(string mood, string duration)
        {
            mood = MoodCategoryService.NormalizeMood(mood);

            string mode = string.IsNullOrWhiteSpace(duration) ? "forever" : duration;
            string expires = BuildExpireAt(mode);
            string now = DateTime.UtcNow.ToString("o");

            _configService.Update(config =>
            {
                config.MoodTag = mood;
                config.CurrentMood = mood;
                config.MoodExpireMode = mode;
                config.MoodExpireAt = expires;
                config.MoodExpiresAtUtc = expires;
                config.LastMoodChangedAt = now;
            });

            LogService.Info("心情已设置为：" + mood +
                "，categories=" + MoodCategoryService.FormatCategories(MoodCategoryService.GetPrimaryCategories(mood)) +
                "，有效期：" + mode);
        }

        public bool IsMoodExpired()
        {
            AppConfig config = _configService.Current;
            string expiresText = !string.IsNullOrWhiteSpace(config.MoodExpireAt) ? config.MoodExpireAt : config.MoodExpiresAtUtc;
            if (string.IsNullOrWhiteSpace(expiresText))
            {
                return false;
            }

            DateTime expiresAt;
            if (!DateTime.TryParse(expiresText, null, System.Globalization.DateTimeStyles.RoundtripKind, out expiresAt))
            {
                return false;
            }

            return DateTime.UtcNow > expiresAt.ToUniversalTime();
        }

        public string GetMoodDisplayName(string mood)
        {
            return MoodCategoryService.GetMoodOption(mood).Name;
        }

        public string GetMoodExpireDisplayName(string duration)
        {
            if (string.Equals(duration, "thirty_minutes", StringComparison.OrdinalIgnoreCase))
            {
                return "30分钟";
            }

            if (string.Equals(duration, "one_hour", StringComparison.OrdinalIgnoreCase))
            {
                return "1小时";
            }

            if (string.Equals(duration, "two_hours", StringComparison.OrdinalIgnoreCase))
            {
                return "2小时";
            }

            if (string.Equals(duration, "today", StringComparison.OrdinalIgnoreCase))
            {
                return "今天有效";
            }

            return "一直有效";
        }

        private string BuildExpireAt(string duration)
        {
            if (string.Equals(duration, "thirty_minutes", StringComparison.OrdinalIgnoreCase))
            {
                return DateTime.UtcNow.AddMinutes(30).ToString("o");
            }

            if (string.Equals(duration, "one_hour", StringComparison.OrdinalIgnoreCase))
            {
                return DateTime.UtcNow.AddHours(1).ToString("o");
            }

            if (string.Equals(duration, "two_hours", StringComparison.OrdinalIgnoreCase))
            {
                return DateTime.UtcNow.AddHours(2).ToString("o");
            }

            if (string.Equals(duration, "today", StringComparison.OrdinalIgnoreCase))
            {
                DateTime tomorrow = DateTime.Now.Date.AddDays(1);
                return tomorrow.ToUniversalTime().ToString("o");
            }

            return null;
        }
    }
}
