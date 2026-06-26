using System;
using System.Collections.Generic;
using System.Linq;
using YueXinMiaoPet.Models;

namespace YueXinMiaoPet.Services
{
    public class MoodOption
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CategorySummary { get; set; }
        public string Emoji { get; set; }
    }

    /// <summary>
    /// 月薪喵心情与 13 个一层分类文件夹的唯一映射中心。
    /// 注意：用户点击桌宠时必须严格停留在当前 MoodTag 对应的分类池里。
    /// </summary>
    public static class MoodCategoryService
    {
        private static readonly Dictionary<string, MoodOption> MoodOptions = new Dictionary<string, MoodOption>(StringComparer.OrdinalIgnoreCase)
        {
            { "neutral", new MoodOption { Key = "neutral", Name = "普通", Emoji = "🐾", Description = "日常陪伴、待机、小互动。", CategorySummary = "01_普通" } },
            { "happy", new MoodOption { Key = "happy", Name = "开心", Emoji = "😸", Description = "开心、搞笑、轻松愉快。", CategorySummary = "02_开心" } },
            { "love", new MoodOption { Key = "love", Name = "喜欢", Emoji = "💖", Description = "喜欢、贴贴、亲昵、爱心攻击。", CategorySummary = "03_喜欢" } },
            { "shy", new MoodOption { Key = "shy", Name = "害羞", Emoji = "🙈", Description = "脸红、腼腆、卖萌。", CategorySummary = "04_害羞" } },
            { "angry", new MoodOption { Key = "angry", Name = "生气", Emoji = "💢", Description = "炸毛、生气、不满、拒绝营业。", CategorySummary = "05_生气" } },
            { "sad", new MoodOption { Key = "sad", Name = "难过", Emoji = "🥺", Description = "难过、委屈、流泪，需要安慰。", CategorySummary = "06_难过" } },
            { "tired", new MoodOption { Key = "tired", Name = "累了", Emoji = "😵", Description = "打工累趴，精神电量不足。", CategorySummary = "07_累了" } },
            { "sleepy", new MoodOption { Key = "sleepy", Name = "困了", Emoji = "💤", Description = "困倦、睡觉、晚安、起床失败。", CategorySummary = "08_困了" } },
            { "lazy", new MoodOption { Key = "lazy", Name = "想摸鱼", Emoji = "🎣", Description = "摸鱼、摆烂、躺平、追剧。", CategorySummary = "09_想摸鱼" } },
            { "hungry", new MoodOption { Key = "hungry", Name = "饿了", Emoji = "🍪", Description = "干饭、零食、奶茶、外卖。", CategorySummary = "10_饿了" } },
            { "excited", new MoodOption { Key = "excited", Name = "兴奋", Emoji = "✨", Description = "期待、庆祝、蹦蹦跳跳。", CategorySummary = "11_兴奋" } },
            { "thinking", new MoodOption { Key = "thinking", Name = "思考", Emoji = "🤔", Description = "思考、疑惑、反应中。", CategorySummary = "12_思考" } },
            { "collapse", new MoodOption { Key = "collapse", Name = "崩溃", Emoji = "🫠", Description = "麻了、完了、社畜崩溃。", CategorySummary = "13_崩溃" } }
        };

        private static readonly Dictionary<string, string[]> PrimaryMoodCategories = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "neutral", new[] { "01_普通" } },
            { "happy", new[] { "02_开心" } },
            { "love", new[] { "03_喜欢" } },
            { "shy", new[] { "04_害羞" } },
            { "angry", new[] { "05_生气" } },
            { "sad", new[] { "06_难过" } },
            { "tired", new[] { "07_累了" } },
            { "sleepy", new[] { "08_困了" } },
            { "lazy", new[] { "09_想摸鱼" } },
            { "hungry", new[] { "10_饿了" } },
            { "excited", new[] { "11_兴奋" } },
            { "thinking", new[] { "12_思考" } },
            { "collapse", new[] { "13_崩溃" } }
        };

        private static readonly Dictionary<string, string[]> CategoryTags = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "01_普通", new[] { "idle", "neutral", "greet", "touch" } },
            { "02_开心", new[] { "happy" } },
            { "03_喜欢", new[] { "love", "happy", "touch" } },
            { "04_害羞", new[] { "shy" } },
            { "05_生气", new[] { "angry" } },
            { "06_难过", new[] { "sad", "lonely" } },
            { "07_累了", new[] { "tired", "work_start", "hot", "cold" } },
            { "08_困了", new[] { "sleepy", "sleep", "night" } },
            { "09_想摸鱼", new[] { "lazy", "idle" } },
            { "10_饿了", new[] { "hungry", "noon" } },
            { "11_兴奋", new[] { "excited", "work_end" } },
            { "12_思考", new[] { "thinking", "idle" } },
            { "13_崩溃", new[] { "collapse", "tired", "sad", "angry" } }
        };

        private static readonly Dictionary<string, string> TagToCanonicalCategory = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "cat_01", "01_普通" },
            { "cat_02", "02_开心" },
            { "cat_03", "03_喜欢" },
            { "cat_04", "04_害羞" },
            { "cat_05", "05_生气" },
            { "cat_06", "06_难过" },
            { "cat_07", "07_累了" },
            { "cat_08", "08_困了" },
            { "cat_09", "09_想摸鱼" },
            { "cat_10", "10_饿了" },
            { "cat_11", "11_兴奋" },
            { "cat_12", "12_思考" },
            { "cat_13", "13_崩溃" }
        };

        private static readonly Dictionary<string, string> MoodAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "普通", "neutral" },
            { "开心", "happy" },
            { "高兴", "happy" },
            { "喜欢", "love" },
            { "害羞", "shy" },
            { "生气", "angry" },
            { "难过", "sad" },
            { "累", "tired" },
            { "累了", "tired" },
            { "困", "sleepy" },
            { "困了", "sleepy" },
            { "摸鱼", "lazy" },
            { "想摸鱼", "lazy" },
            { "饿", "hungry" },
            { "饿了", "hungry" },
            { "兴奋", "excited" },
            { "思考", "thinking" },
            { "崩溃", "collapse" },
            { "lonely", "sad" }
        };

        public static IList<MoodOption> GetMoodOptions()
        {
            return MoodOptions.Values.ToList();
        }

        public static Dictionary<string, string> GetMoodDisplayMap()
        {
            return MoodOptions.Values.ToDictionary(o => o.Key, o => o.Name, StringComparer.OrdinalIgnoreCase);
        }

        public static Dictionary<string, string[]> GetMoodCategoryMap()
        {
            return PrimaryMoodCategories.ToDictionary(
                p => p.Key,
                p => p.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsKnownMood(string mood)
        {
            return MoodOptions.ContainsKey(NormalizeMood(mood));
        }

        public static string NormalizeMood(string mood)
        {
            if (string.IsNullOrWhiteSpace(mood))
            {
                return "neutral";
            }

            string trimmed = mood.Trim();
            string alias;
            if (MoodAliases.TryGetValue(trimmed, out alias))
            {
                return alias;
            }

            return MoodOptions.ContainsKey(trimmed) ? trimmed : "neutral";
        }

        public static MoodOption GetMoodOption(string mood)
        {
            MoodOption option;
            if (!MoodOptions.TryGetValue(NormalizeMood(mood), out option))
            {
                option = MoodOptions["neutral"];
            }

            return option;
        }

        public static IList<string> GetPrimaryCategories(string mood)
        {
            string[] result;
            return PrimaryMoodCategories.TryGetValue(NormalizeMood(mood), out result)
                ? result.ToList()
                : PrimaryMoodCategories["neutral"].ToList();
        }

        public static IList<string> GetFallbackCategories(string mood)
        {
            return new List<string>();
        }

        public static IList<string> GetMoodCategories(string mood)
        {
            return GetPrimaryCategories(mood).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static string FormatCategories(IEnumerable<string> categories)
        {
            if (categories == null)
            {
                return string.Empty;
            }

            return string.Join(", ", categories.Where(c => !string.IsNullOrWhiteSpace(c)).ToArray());
        }

        public static bool IsValidCategoryFolderName(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName) || folderName.Length < 4)
            {
                return false;
            }

            return char.IsDigit(folderName[0]) &&
                char.IsDigit(folderName[1]) &&
                folderName[2] == '_' &&
                TagToCanonicalCategory.ContainsKey(CategoryTagFromFolder(folderName));
        }

        public static string GetCanonicalCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return string.Empty;
            }

            string trimmed = category.Trim();
            string canonical;
            if (TagToCanonicalCategory.TryGetValue(trimmed, out canonical))
            {
                return canonical;
            }

            string tag = CategoryTagFromFolder(trimmed);
            if (TagToCanonicalCategory.TryGetValue(tag, out canonical))
            {
                return canonical;
            }

            return trimmed;
        }

        public static IList<string> GetTagsForCategory(string category)
        {
            string canonical = GetCanonicalCategory(category);
            string[] tags;
            return CategoryTags.TryGetValue(canonical, out tags) ? tags : new string[0];
        }

        public static IList<string> GetWeatherCategories(string weatherTag)
        {
            if (string.IsNullOrWhiteSpace(weatherTag) || string.Equals(weatherTag, "unknown", StringComparison.OrdinalIgnoreCase))
            {
                return new List<string>();
            }

            if (string.Equals(weatherTag, "cold", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(weatherTag, "snow", StringComparison.OrdinalIgnoreCase))
            {
                return new List<string> { "08_困了" };
            }

            if (string.Equals(weatherTag, "rain", StringComparison.OrdinalIgnoreCase))
            {
                return new List<string> { "06_难过", "09_想摸鱼" };
            }

            if (string.Equals(weatherTag, "thunder", StringComparison.OrdinalIgnoreCase))
            {
                return new List<string> { "12_思考", "13_崩溃" };
            }

            if (string.Equals(weatherTag, "sunny", StringComparison.OrdinalIgnoreCase))
            {
                return new List<string> { "02_开心", "01_普通" };
            }

            if (string.Equals(weatherTag, "hot", StringComparison.OrdinalIgnoreCase))
            {
                return new List<string> { "07_累了", "13_崩溃" };
            }

            return new List<string> { "01_普通" };
        }

        public static IList<string> GetTimeCategories(PetState state)
        {
            List<string> result = new List<string>();
            if (state == null)
            {
                return result;
            }

            if (state.IsWorkingTime || string.Equals(state.TimeTag, "work_start", StringComparison.OrdinalIgnoreCase))
            {
                result.Add("07_累了");
            }

            if (state.IsAfterWork || string.Equals(state.TimeTag, "work_end", StringComparison.OrdinalIgnoreCase))
            {
                result.Add("02_开心");
                result.Add("11_兴奋");
            }

            if (string.Equals(state.TimeTag, "noon", StringComparison.OrdinalIgnoreCase))
            {
                result.Add("10_饿了");
            }

            if (string.Equals(state.TimeTag, "night", StringComparison.OrdinalIgnoreCase))
            {
                result.Add("08_困了");
            }

            return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static string CategoryTagFromFolder(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName) || folderName.Length < 2)
            {
                return string.Empty;
            }

            string prefix = new string(folderName.Trim().TakeWhile(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(prefix))
            {
                return string.Empty;
            }

            int number;
            if (!int.TryParse(prefix, out number))
            {
                return string.Empty;
            }

            return "cat_" + number.ToString("00");
        }
    }
}
