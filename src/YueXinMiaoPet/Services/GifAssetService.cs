using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YueXinMiaoPet.Models;
using YueXinMiaoPet.Utils;

namespace YueXinMiaoPet.Services
{
    public class GifAssetService
    {
        private readonly GifTagInferenceService _tagInferenceService;
        private Dictionary<string, List<string>> _tagOverrides = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        public List<GifAsset> Assets { get; private set; }
        public string LastSource { get; private set; }
        public string LastTagSource { get; private set; }
        public string CurrentGifDirectory { get; private set; }
        public string CurrentSourceMode { get; private set; }
        public int CategoryCount { get; private set; }
        public Dictionary<string, int> CategoryGifCounts { get; private set; }

        public GifAssetService(GifTagInferenceService tagInferenceService)
        {
            _tagInferenceService = tagInferenceService;
            Assets = new List<GifAsset>();
            LastSource = string.Empty;
            LastTagSource = string.Empty;
            CurrentGifDirectory = FilePathHelper.GetPreferredBuiltInGifDirectory();
            CurrentSourceMode = "BuiltInClassified";
            CategoryCount = 0;
            CategoryGifCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        public List<GifAsset> LoadAssets(AppConfig config)
        {
            string gifDirectory = GetEffectiveGifDirectory(config);
            LoadAssetsFromDirectory(gifDirectory, GetSourceMode(config, gifDirectory));

            if (Assets.Count == 0 && IsUsingCustomDirectory(config, gifDirectory))
            {
                LogService.Warn("自定义 GIF 目录没有可用 GIF，已回退到内置月薪喵分类资源：" + gifDirectory);
                LoadAssetsFromDirectory(FilePathHelper.GetPreferredBuiltInGifDirectory(), "BuiltInClassified");
            }

            if (Assets.Count == 0 && !string.Equals(gifDirectory, FilePathHelper.DefaultGifDirectory, StringComparison.OrdinalIgnoreCase))
            {
                LogService.Warn("首选内置分类资源为空，继续回退到原始 PetAssets/Gifs。");
                LoadAssetsFromDirectory(FilePathHelper.DefaultGifDirectory, "BuiltIn");
            }

            return Assets;
        }

        private void LoadAssetsFromDirectory(string gifDirectory, string sourceMode)
        {
            CurrentGifDirectory = gifDirectory;
            CurrentSourceMode = sourceMode;
            CategoryCount = 0;
            CategoryGifCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            string assetRoot = GetAssetRoot(gifDirectory);
            string manualJson = Path.Combine(assetRoot, "assets.json");
            string generatedJson = Path.Combine(assetRoot, "assets.generated.json");
            string overrideJson = Path.Combine(assetRoot, "assets.tags.override.json");

            LoadTagOverrides(overrideJson);

            List<GifAsset> manualAssets = TryLoadManualAssets(manualJson, gifDirectory, sourceMode);
            if (manualAssets != null && manualAssets.Count > 0)
            {
                Assets = manualAssets;
                RebuildCategoryCounts(Assets);
                LastSource = manualJson;
                LastTagSource = _tagOverrides.Count > 0 ? "assets.json + assets.tags.override.json" : "assets.json";
                LogService.Info("已从 assets.json 加载 GIF 资源：" + Assets.Count);
                return;
            }

            Assets = ScanGifDirectory(gifDirectory, sourceMode);
            RebuildCategoryCounts(Assets);
            LastSource = generatedJson;
            LastTagSource = _tagOverrides.Count > 0 ? "assets.generated.json + assets.tags.override.json" : "assets.generated.json";

            if (!SafeJson.Write(generatedJson, Assets))
            {
                string fallback = Path.Combine(FilePathHelper.AppDataDir, "assets.generated.json");
                SafeJson.Write(fallback, Assets);
                LastSource = fallback;
            }

            LogService.Info("已扫描 GIF 资源：" + Assets.Count + "，分类：" + CategoryCount + "，目录：" + gifDirectory);
        }

        public string ResolveAssetPath(GifAsset asset, AppConfig config)
        {
            if (asset == null)
            {
                return string.Empty;
            }

            return FilePathHelper.ResolvePath(asset.File, GetEffectiveGifDirectory(config));
        }

        public List<GifAsset> GetEnabledExistingAssets(AppConfig config)
        {
            List<GifAsset> result = new List<GifAsset>();
            for (int i = 0; i < Assets.Count; i++)
            {
                GifAsset asset = Assets[i];
                if (asset != null && asset.Enabled && File.Exists(ResolveAssetPath(asset, config)))
                {
                    result.Add(asset);
                }
            }

            return result;
        }

        public string GetEffectiveGifDirectory(AppConfig config)
        {
            if (config == null)
            {
                return FilePathHelper.GetPreferredBuiltInGifDirectory();
            }

            if (string.Equals(config.GifSourceMode, "Custom", StringComparison.OrdinalIgnoreCase))
            {
                string custom = config.CustomGifDirectory;
                if (!string.IsNullOrWhiteSpace(custom) && Directory.Exists(custom))
                {
                    return custom;
                }

                LogService.Warn("自定义 GIF 目录无效，已回退到内置资源：" + custom);
                return FilePathHelper.GetPreferredBuiltInGifDirectory();
            }

            if (string.Equals(config.GifSourceMode, "BuiltIn", StringComparison.OrdinalIgnoreCase))
            {
                return FilePathHelper.DefaultGifDirectory;
            }

            string classified = config.BuiltInClassifiedGifDirectory;
            if (!string.IsNullOrWhiteSpace(classified) &&
                Directory.Exists(classified) &&
                Directory.GetFiles(classified, "*.gif", SearchOption.AllDirectories).Length > 0)
            {
                return classified;
            }

            return FilePathHelper.GetPreferredBuiltInGifDirectory();
        }

        private string GetSourceMode(AppConfig config, string effectiveDirectory)
        {
            if (config != null && string.Equals(config.GifSourceMode, "Custom", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(NormalizePath(effectiveDirectory), NormalizePath(FilePathHelper.GetPreferredBuiltInGifDirectory()), StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(NormalizePath(effectiveDirectory), NormalizePath(FilePathHelper.DefaultGifDirectory), StringComparison.OrdinalIgnoreCase))
            {
                return "Custom";
            }

            if (IsClassifiedDirectory(effectiveDirectory))
            {
                return "BuiltInClassified";
            }

            return "BuiltIn";
        }

        private bool IsUsingCustomDirectory(AppConfig config, string effectiveDirectory)
        {
            return config != null &&
                string.Equals(config.GifSourceMode, "Custom", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(NormalizePath(effectiveDirectory), NormalizePath(FilePathHelper.GetPreferredBuiltInGifDirectory()), StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(NormalizePath(effectiveDirectory), NormalizePath(FilePathHelper.DefaultGifDirectory), StringComparison.OrdinalIgnoreCase);
        }

        private string GetAssetRoot(string gifDirectory)
        {
            if (IsClassifiedDirectory(gifDirectory))
            {
                DirectoryInfo info = new DirectoryInfo(gifDirectory);
                if (info.Parent != null)
                {
                    return info.Parent.FullName;
                }
            }

            return FilePathHelper.GetAssetRootFromGifDirectory(gifDirectory);
        }

        private bool IsClassifiedDirectory(string gifDirectory)
        {
            if (string.IsNullOrWhiteSpace(gifDirectory) || !Directory.Exists(gifDirectory))
            {
                return false;
            }

            return Directory.GetDirectories(gifDirectory)
                .Select(Path.GetFileName)
                .Any(MoodCategoryService.IsValidCategoryFolderName);
        }

        private void LoadTagOverrides(string overrideJson)
        {
            _tagOverrides = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!File.Exists(overrideJson))
                {
                    return;
                }

                Dictionary<string, List<string>> overrides = SafeJson.Read<Dictionary<string, List<string>>>(overrideJson, null);
                if (overrides == null)
                {
                    return;
                }

                foreach (KeyValuePair<string, List<string>> pair in overrides)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null || pair.Value.Count == 0)
                    {
                        continue;
                    }

                    _tagOverrides[pair.Key.Trim()] = CleanTags(pair.Value);
                }
            }
            catch (Exception ex)
            {
                LogService.Error("读取 assets.tags.override.json 失败，继续使用自动推断。", ex);
            }
        }

        private IList<string> GetOverrideTags(string fileOrName)
        {
            if (string.IsNullOrWhiteSpace(fileOrName))
            {
                return null;
            }

            string fileName = Path.GetFileName(fileOrName);
            List<string> tags;
            if (_tagOverrides.TryGetValue(fileName, out tags))
            {
                return tags;
            }

            string noExt = Path.GetFileNameWithoutExtension(fileName);
            if (_tagOverrides.TryGetValue(noExt, out tags))
            {
                return tags;
            }

            return null;
        }

        private List<GifAsset> TryLoadManualAssets(string manualJson, string gifDirectory, string sourceMode)
        {
            try
            {
                if (!File.Exists(manualJson))
                {
                    return null;
                }

                List<GifAsset> assets = SafeJson.Read<List<GifAsset>>(manualJson, null);
                if (assets == null || assets.Count == 0)
                {
                    return null;
                }

                return NormalizeAssets(assets, gifDirectory, sourceMode);
            }
            catch (Exception ex)
            {
                LogService.Error("读取 assets.json 失败，回退到自动扫描。", ex);
                return null;
            }
        }

        private List<GifAsset> ScanGifDirectory(string gifDirectory, string sourceMode)
        {
            List<GifAsset> result = new List<GifAsset>();
            try
            {
                if (!Directory.Exists(gifDirectory))
                {
                    LogService.Warn("GIF 目录不存在：" + gifDirectory);
                    return result;
                }

                bool classified = IsClassifiedDirectory(gifDirectory);
                List<string> files = new List<string>();
                Dictionary<string, int> folderCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                if (classified)
                {
                    string[] categoryFolders = Directory.GetDirectories(gifDirectory)
                        .Where(d => MoodCategoryService.IsValidCategoryFolderName(Path.GetFileName(d)))
                        .OrderBy(d => Path.GetFileName(d), StringComparer.CurrentCultureIgnoreCase)
                        .ToArray();

                    for (int c = 0; c < categoryFolders.Length; c++)
                    {
                        string folder = categoryFolders[c];
                        string folderName = Path.GetFileName(folder);
                        string[] categoryFiles = Directory.GetFiles(folder, "*.gif", SearchOption.TopDirectoryOnly)
                            .OrderBy(p => Path.GetFileName(p), StringComparer.CurrentCultureIgnoreCase)
                            .ToArray();
                        folderCounts[folderName] = categoryFiles.Length;
                        files.AddRange(categoryFiles);
                    }

                    LogService.Info("扫描 GIF：目录=" + gifDirectory + "，use classified=true，实际分类数=" + categoryFolders.Length);
                    foreach (KeyValuePair<string, int> pair in folderCounts)
                    {
                        LogService.Info("扫描 GIF 分类：" + pair.Key + "，GIF 数量=" + pair.Value);
                    }
                }
                else
                {
                    files.AddRange(Directory.GetFiles(gifDirectory, "*.gif", SearchOption.TopDirectoryOnly)
                        .OrderBy(p => Path.GetFileName(p), StringComparer.CurrentCultureIgnoreCase));
                    LogService.Info("扫描 GIF：目录=" + gifDirectory + "，use classified=false，实际分类数=0");
                }

                HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < files.Count; i++)
                {
                    string file = files[i];
                    if (!seen.Add(Path.GetFullPath(file)))
                    {
                        continue;
                    }

                    string fileName = Path.GetFileName(file);
                    string name = Path.GetFileNameWithoutExtension(file);
                    string categoryName = classified ? MoodCategoryService.GetCanonicalCategory(GetFirstLevelCategoryName(gifDirectory, file)) : string.Empty;
                    string categoryTag = MoodCategoryService.CategoryTagFromFolder(categoryName);
                    List<string> tags = BuildTags(fileName, categoryName);

                    result.Add(new GifAsset
                    {
                        Id = "gif_" + (result.Count + 1).ToString("000"),
                        File = FilePathHelper.ToAppRelativePath(file),
                        Name = name,
                        Tags = tags,
                        Weight = GetDefaultWeight(categoryTag, tags),
                        Enabled = true,
                        CategoryName = categoryName,
                        CategoryTag = categoryTag,
                        CategoryPath = string.IsNullOrWhiteSpace(categoryName) ? string.Empty : categoryName,
                        SourceMode = sourceMode
                    });
                }
            }
            catch (Exception ex)
            {
                LogService.Error("扫描 GIF 目录失败：" + gifDirectory, ex);
            }

            return result;
        }

        private List<GifAsset> NormalizeAssets(List<GifAsset> assets, string gifDirectory, string sourceMode)
        {
            List<GifAsset> result = new List<GifAsset>();
            for (int i = 0; i < assets.Count; i++)
            {
                GifAsset asset = assets[i];
                if (asset == null || string.IsNullOrWhiteSpace(asset.File))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(asset.Id))
                {
                    asset.Id = "gif_" + (i + 1).ToString("000");
                }

                if (string.IsNullOrWhiteSpace(asset.Name))
                {
                    asset.Name = Path.GetFileNameWithoutExtension(asset.File);
                }

                if (string.IsNullOrWhiteSpace(asset.CategoryName))
                {
                    asset.CategoryName = GuessCategoryNameFromAsset(asset, gifDirectory);
                }

                asset.CategoryName = MoodCategoryService.GetCanonicalCategory(asset.CategoryName);

                if (string.IsNullOrWhiteSpace(asset.CategoryTag))
                {
                    asset.CategoryTag = MoodCategoryService.CategoryTagFromFolder(asset.CategoryName);
                }

                if (string.IsNullOrWhiteSpace(asset.CategoryPath))
                {
                    asset.CategoryPath = asset.CategoryName ?? string.Empty;
                }

                if (string.IsNullOrWhiteSpace(asset.SourceMode))
                {
                    asset.SourceMode = sourceMode;
                }

                if (asset.Tags == null || asset.Tags.Count == 0)
                {
                    string fileName = Path.GetFileName(asset.File);
                    asset.Tags = BuildTags(fileName, asset.CategoryName);
                }
                else
                {
                    List<string> tags = CleanTags(asset.Tags);
                    MergeCategoryTags(tags, asset.CategoryName);
                    asset.Tags = tags.Count == 0 ? new List<string> { "idle" } : tags;
                }

                if (asset.Weight <= 0)
                {
                    asset.Weight = 1;
                }

                if (!File.Exists(FilePathHelper.ResolvePath(asset.File, gifDirectory)))
                {
                    LogService.Warn("assets.json 中的 GIF 文件不存在：" + asset.File);
                }

                result.Add(asset);
            }

            return result;
        }

        private List<string> BuildTags(string fileName, string category)
        {
            List<string> tags = CleanTags(_tagInferenceService.InferTags(fileName, GetOverrideTags(fileName)));
            MergeCategoryTags(tags, category);
            if (tags.Count == 0)
            {
                tags.Add("idle");
            }

            return tags;
        }

        private void MergeCategoryTags(List<string> tags, string category)
        {
            if (tags == null || string.IsNullOrWhiteSpace(category))
            {
                return;
            }

            string canonical = MoodCategoryService.GetCanonicalCategory(category);
            string categoryTag = MoodCategoryService.CategoryTagFromFolder(canonical);
            AddTag(tags, categoryTag);
            foreach (string tag in MoodCategoryService.GetTagsForCategory(canonical))
            {
                AddTag(tags, tag);
            }
        }

        private void AddTag(List<string> tags, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return;
            }

            string normalized = tag.Trim().ToLowerInvariant();
            if (!tags.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                tags.Add(normalized);
            }
        }

        private List<string> CleanTags(IEnumerable<string> tags)
        {
            if (tags == null)
            {
                return new List<string>();
            }

            return tags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim().ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private int GetDefaultWeight(string categoryTag, List<string> tags)
        {
            if (!string.IsNullOrWhiteSpace(categoryTag))
            {
                return 8;
            }

            if (tags != null && (tags.Contains("idle") || tags.Contains("neutral")))
            {
                return 4;
            }

            return 5;
        }

        private string GuessCategoryNameFromAsset(GifAsset asset, string gifDirectory)
        {
            string resolved = FilePathHelper.ResolvePath(asset.File, gifDirectory);
            if (File.Exists(resolved) && IsClassifiedDirectory(gifDirectory))
            {
                return GetFirstLevelCategoryName(gifDirectory, resolved);
            }

            return string.Empty;
        }

        private string GetFirstLevelCategoryName(string rootDirectory, string fullPath)
        {
            try
            {
                string relative = GetRelativeToRoot(rootDirectory, fullPath);
                int slash = relative.IndexOfAny(new[] { '\\', '/' });
                if (slash > 0)
                {
                    return relative.Substring(0, slash);
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private string GetRelativeToRoot(string rootDirectory, string fullPath)
        {
            string root = Path.GetFullPath(rootDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string path = Path.GetFullPath(fullPath);
            if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                return path.Substring(root.Length);
            }

            return Path.GetFileName(fullPath);
        }

        private int CountCategories(IEnumerable<GifAsset> assets)
        {
            if (assets == null)
            {
                return 0;
            }

            return assets
                .Where(a => a != null && !string.IsNullOrWhiteSpace(a.CategoryName))
                .Select(a => MoodCategoryService.GetCanonicalCategory(a.CategoryName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
        }

        private void RebuildCategoryCounts(IEnumerable<GifAsset> assets)
        {
            CategoryGifCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (assets != null)
            {
                foreach (GifAsset asset in assets)
                {
                    if (asset == null || string.IsNullOrWhiteSpace(asset.CategoryName))
                    {
                        continue;
                    }

                    string category = MoodCategoryService.GetCanonicalCategory(asset.CategoryName);
                    if (!CategoryGifCounts.ContainsKey(category))
                    {
                        CategoryGifCounts[category] = 0;
                    }

                    CategoryGifCounts[category]++;
                }
            }

            CategoryCount = CategoryGifCounts.Count;
        }

        private string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
