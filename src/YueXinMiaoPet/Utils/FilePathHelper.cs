using System;
using System.IO;

namespace YueXinMiaoPet.Utils
{
    public static class FilePathHelper
    {
        public static string AppDataDir
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YueXinMiaoPet");
            }
        }

        public static string ConfigPath
        {
            get { return Path.Combine(AppDataDir, "config.json"); }
        }

        public static string LogsDir
        {
            get { return Path.Combine(AppDataDir, "logs"); }
        }

        public static string LogPath
        {
            get { return Path.Combine(LogsDir, "app.log"); }
        }

        public static string AppBaseDir
        {
            get { return AppDomain.CurrentDomain.BaseDirectory; }
        }

        public static string DefaultGifDirectory
        {
            get { return Path.Combine(AppBaseDir, "PetAssets", "Gifs"); }
        }

        public static string DefaultClassifiedGifDirectory
        {
            get { return Path.Combine(AppBaseDir, "PetAssets", "classified_gifs"); }
        }

        public static string SourceProjectClassifiedGifDirectory
        {
            get
            {
                return Path.GetFullPath(Path.Combine(AppBaseDir, "..", "..", "..", "..", "classified_gifs"));
            }
        }

        public static string GetPreferredBuiltInGifDirectory()
        {
            if (Directory.Exists(DefaultClassifiedGifDirectory) &&
                Directory.GetFiles(DefaultClassifiedGifDirectory, "*.gif", SearchOption.AllDirectories).Length > 0)
            {
                return DefaultClassifiedGifDirectory;
            }

            if (Directory.Exists(SourceProjectClassifiedGifDirectory) &&
                Directory.GetFiles(SourceProjectClassifiedGifDirectory, "*.gif", SearchOption.AllDirectories).Length > 0)
            {
                return SourceProjectClassifiedGifDirectory;
            }

            return DefaultGifDirectory;
        }

        public static string GetAssetRootFromGifDirectory(string gifDirectory)
        {
            if (string.IsNullOrWhiteSpace(gifDirectory))
            {
                return Path.Combine(AppBaseDir, "PetAssets");
            }

            DirectoryInfo info = new DirectoryInfo(gifDirectory);
            if (info.Parent == null)
            {
                return gifDirectory;
            }

            return info.Parent.FullName;
        }

        public static string ToAppRelativePath(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return string.Empty;
            }

            string baseDir = EnsureTrailingSlash(Path.GetFullPath(AppBaseDir));
            string absolute = Path.GetFullPath(fullPath);
            if (absolute.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
            {
                return absolute.Substring(baseDir.Length).Replace('\\', '/');
            }

            return absolute;
        }

        public static string ResolvePath(string path, string gifDirectory)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            if (Path.IsPathRooted(path))
            {
                return path;
            }

            string fromApp = Path.Combine(AppBaseDir, path.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fromApp))
            {
                return fromApp;
            }

            string assetRoot = GetAssetRootFromGifDirectory(gifDirectory);
            string fromAssetRoot = Path.Combine(assetRoot, path.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fromAssetRoot))
            {
                return fromAssetRoot;
            }

            return fromApp;
        }

        public static void EnsureDirectory(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        private static string EnsureTrailingSlash(string path)
        {
            if (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                return path;
            }

            return path + Path.DirectorySeparatorChar;
        }
    }
}
