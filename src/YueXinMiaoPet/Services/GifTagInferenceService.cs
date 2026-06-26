using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace YueXinMiaoPet.Services
{
    public class GifTagInferenceService
    {
        private readonly List<Rule> _exactPhraseRules = new List<Rule>();
        private readonly List<Rule> _wordRules = new List<Rule>();
        private readonly List<Rule> _singleCharRules = new List<Rule>();

        public GifTagInferenceService()
        {
            BuildRules();
        }

        public List<string> InferTags(string fileName)
        {
            return InferTags(fileName, null);
        }

        public List<string> InferTags(string fileName, IList<string> overrideTags)
        {
            List<string> tags = new List<string>();

            // 手工标签优先：assets.json / assets.tags.override.json 命中后不再自动推断。
            AddTags(tags, overrideTags);
            if (tags.Count > 0)
            {
                return tags;
            }

            string normalized = NormalizeName(fileName);
            ApplyRules(_exactPhraseRules, normalized, tags);
            ApplyRules(_wordRules, normalized, tags);
            ApplyRules(_singleCharRules, normalized, tags);

            if (tags.Count == 0)
            {
                tags.Add("idle");
            }

            return tags;
        }

        public string NormalizeName(string fileName)
        {
            string name = fileName ?? string.Empty;
            try
            {
                string justName = Path.GetFileName(name);
                if (!string.IsNullOrWhiteSpace(justName))
                {
                    name = justName;
                }

                string noExt = Path.GetFileNameWithoutExtension(name);
                if (!string.IsNullOrWhiteSpace(noExt))
                {
                    name = noExt;
                }
            }
            catch
            {
                // 路径异常时仍使用原字符串做兜底推断。
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                char c = ToHalfWidth(name[i]);
                if (char.IsWhiteSpace(c) || char.IsPunctuation(c) || char.IsSymbol(c))
                {
                    continue;
                }

                if (IsChinese(c) || char.IsLetterOrDigit(c))
                {
                    builder.Append(char.ToLowerInvariant(c));
                }
            }

            return builder.ToString();
        }

        private void BuildRules()
        {
            AddExact(new[] { "work_start", "sleepy", "tired" }, "上班困", "上班困困", "早八困", "打工困", "工作困");
            AddExact(new[] { "work_end", "happy", "excited" }, "下班开心", "终于下班", "下班啦", "下班冲", "跑路");
            AddExact(new[] { "rain", "idle", "lazy" }, "下雨天发呆", "雨天发呆", "撑伞发呆");
            AddExact(new[] { "night", "sleep", "sleepy" }, "晚安睡觉", "睡前晚安", "晚安入睡");
            AddExact(new[] { "touch", "happy" }, "摸头开心", "撸猫开心", "rua开心");
            AddExact(new[] { "angry" }, "生气炸毛", "炸毛生气", "火大炸毛");
            AddExact(new[] { "hungry" }, "饿了干饭", "干饭", "饭呢", "想吃零食", "想喝奶茶");
            AddExact(new[] { "work_start", "tired" }, "打工累", "打工累趴", "被迫加班", "上班累");
            AddExact(new[] { "snow", "cold" }, "下雪发抖", "雪天发抖", "冻得发抖");
            AddExact(new[] { "sunny", "happy", "idle" }, "晴天晒太阳", "晒太阳", "好天气");

            AddWords(new[] { "sunny" }, "晴", "晴天", "太阳", "阳光", "晒太阳", "好天气", "蓝天");
            AddWords(new[] { "cloudy" }, "阴", "阴天", "多云", "云", "乌云", "灰蒙蒙");
            AddWords(new[] { "rain" }, "雨", "下雨", "雨天", "淋雨", "撑伞", "伞", "暴雨", "小雨", "大雨", "雨伞");
            AddWords(new[] { "thunder" }, "雷", "雷雨", "打雷", "闪电", "轰隆");
            AddWords(new[] { "snow" }, "雪", "下雪", "雪天", "雪花", "堆雪人");
            AddWords(new[] { "hot" }, "热", "好热", "太热", "流汗", "汗", "中暑", "冒汗", "烤化");
            AddWords(new[] { "cold" }, "冷", "好冷", "太冷", "发抖", "冻", "冻死", "瑟瑟发抖");

            AddWords(new[] { "morning" }, "早", "早上", "早安", "起床", "醒了", "晨", "上午");
            AddWords(new[] { "noon" }, "中午", "午饭", "午餐", "吃午饭", "饭点");
            AddWords(new[] { "afternoon" }, "下午", "午后", "下午茶");
            AddWords(new[] { "evening" }, "晚上", "晚", "傍晚", "夜晚", "夜", "天黑");
            AddWords(new[] { "night" }, "半夜", "深夜", "晚安", "睡前", "熬夜", "凌晨");
            AddWords(new[] { "work_start" }, "上班", "打工", "搬砖", "上工", "通勤", "早八", "开工", "上班中", "工作中", "社畜", "上班困", "打卡");
            AddWords(new[] { "work_end" }, "下班", "收工", "回家", "下班啦", "下班开心", "放工", "终于下班", "下班冲", "跑路");

            AddWords(new[] { "happy" }, "开心", "高兴", "快乐", "笑", "哈哈", "嘿嘿", "嘻嘻", "开心喵", "开心猫", "乐", "愉快", "满足");
            AddWords(new[] { "tired" }, "累", "好累", "疲惫", "疲劳", "没精神", "瘫", "虚弱", "打工累", "累趴");
            AddWords(new[] { "sleepy" }, "困", "困困", "好困", "想睡", "打哈欠", "哈欠", "瞌睡", "睡眼", "迷糊");
            AddWords(new[] { "sad" }, "哭", "难过", "伤心", "emo", "流泪", "泪", "可怜", "委屈", "不开心", "低落", "失落");
            AddWords(new[] { "angry" }, "生气", "气", "怒", "愤怒", "炸毛", "暴躁", "抓狂", "火大", "哼", "无语");
            AddWords(new[] { "lazy" }, "懒", "懒得动", "摸鱼", "摆烂", "躺", "躺平", "发呆", "开摆", "偷懒", "不想动");
            AddWords(new[] { "excited" }, "兴奋", "激动", "期待", "冲", "开心到飞起", "蹦", "跳", "庆祝", "欢呼");
            AddWords(new[] { "hungry" }, "饿", "饿了", "吃", "吃饭", "饭", "零食", "干饭", "馋", "奶茶", "点心");
            AddWords(new[] { "shy" }, "害羞", "脸红", "不好意思", "羞", "捂脸");
            AddWords(new[] { "lonely" }, "孤单", "孤独", "没人理", "陪陪", "寂寞", "可怜兮兮");
            AddWords(new[] { "neutral" }, "普通", "正常", "默认", "日常");

            AddWords(new[] { "idle" }, "待机", "发呆", "坐着", "站着", "趴着", "闲着", "看着", "普通", "默认", "日常");
            AddWords(new[] { "touch" }, "摸", "摸头", "戳", "戳一戳", "点击", "点点", "rua", "撸猫", "贴贴");
            AddWords(new[] { "drag" }, "拖", "拖动", "拽", "拉", "被拖走");
            AddWords(new[] { "greet" }, "打招呼", "你好", "嗨", "hi", "hello", "挥手", "早安", "晚上好");
            AddWords(new[] { "sleep" }, "睡", "睡觉", "睡着", "zzz", "晚安", "入睡", "打瞌睡");
            AddWords(new[] { "meow" }, "喵", "猫叫", "叫", "喵喵", "喵呜");

            AddSingle(new[] { "sunny" }, "晴", "晒");
            AddSingle(new[] { "cloudy" }, "云", "阴");
            AddSingle(new[] { "rain" }, "雨", "伞");
            AddSingle(new[] { "thunder" }, "雷");
            AddSingle(new[] { "snow" }, "雪");
            AddSingle(new[] { "hot" }, "热", "汗");
            AddSingle(new[] { "cold" }, "冷", "冻");
            AddSingle(new[] { "happy" }, "笑", "乐");
            AddSingle(new[] { "sad" }, "哭", "泪");
            AddSingle(new[] { "angry" }, "怒");
            AddSingle(new[] { "hungry" }, "饿", "饭");
            AddSingle(new[] { "sleepy" }, "困");
            AddSingle(new[] { "sleep" }, "睡");
            AddSingle(new[] { "touch" }, "摸", "戳");
            AddSingle(new[] { "meow" }, "喵");
        }

        private void AddExact(string[] tags, params string[] words)
        {
            AddRules(_exactPhraseRules, tags, words);
        }

        private void AddWords(string[] tags, params string[] words)
        {
            AddRules(_wordRules, tags, words);
        }

        private void AddSingle(string[] tags, params string[] words)
        {
            AddRules(_singleCharRules, tags, words);
        }

        private void AddRules(List<Rule> rules, string[] tags, params string[] words)
        {
            for (int i = 0; i < words.Length; i++)
            {
                string normalized = NormalizeName(words[i]);
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    rules.Add(new Rule { Keyword = normalized, Tags = tags.ToList() });
                }
            }
        }

        private void ApplyRules(List<Rule> rules, string normalizedName, List<string> tags)
        {
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return;
            }

            for (int i = 0; i < rules.Count; i++)
            {
                Rule rule = rules[i];
                if (!string.IsNullOrWhiteSpace(rule.Keyword) &&
                    normalizedName.IndexOf(rule.Keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    AddTags(tags, rule.Tags);
                }
            }
        }

        private void AddTags(List<string> tags, IList<string> newTags)
        {
            if (newTags == null)
            {
                return;
            }

            for (int i = 0; i < newTags.Count; i++)
            {
                string tag = (newTags[i] ?? string.Empty).Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(tag))
                {
                    continue;
                }

                if (!tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                {
                    tags.Add(tag);
                }
            }
        }

        private char ToHalfWidth(char c)
        {
            if (c == 12288)
            {
                return ' ';
            }

            if (c >= 65281 && c <= 65374)
            {
                return (char)(c - 65248);
            }

            return c;
        }

        private bool IsChinese(char c)
        {
            return c >= 0x4e00 && c <= 0x9fff;
        }

        private class Rule
        {
            public string Keyword { get; set; }
            public List<string> Tags { get; set; }
        }
    }
}
