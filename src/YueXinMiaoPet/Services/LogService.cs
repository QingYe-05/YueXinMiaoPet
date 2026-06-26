using System;
using System.IO;
using System.Text;
using YueXinMiaoPet.Utils;

namespace YueXinMiaoPet.Services
{
    public static class LogService
    {
        private static readonly object SyncRoot = new object();

        public static void Info(string message)
        {
            Write("INFO", message, null);
        }

        public static void Warn(string message)
        {
            Write("WARN", message, null);
        }

        public static void Error(string message, Exception ex)
        {
            Write("ERROR", message, ex);
        }

        private static void Write(string level, string message, Exception ex)
        {
            try
            {
                lock (SyncRoot)
                {
                    FilePathHelper.EnsureDirectory(FilePathHelper.LogsDir);
                    RotateIfNeeded();

                    StringBuilder builder = new StringBuilder();
                    builder.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    builder.Append(" [");
                    builder.Append(level);
                    builder.Append("] ");
                    builder.AppendLine(message ?? string.Empty);

                    if (ex != null)
                    {
                        builder.AppendLine(ex.ToString());
                    }

                    File.AppendAllText(FilePathHelper.LogPath, builder.ToString(), new UTF8Encoding(false));
                }
            }
            catch
            {
                // 日志不能影响桌宠主流程。
            }
        }

        private static void RotateIfNeeded()
        {
            try
            {
                if (!File.Exists(FilePathHelper.LogPath))
                {
                    return;
                }

                FileInfo info = new FileInfo(FilePathHelper.LogPath);
                if (info.Length < 2 * 1024 * 1024)
                {
                    return;
                }

                string backup = Path.Combine(FilePathHelper.LogsDir, "app.old.log");
                if (File.Exists(backup))
                {
                    File.Delete(backup);
                }

                File.Move(FilePathHelper.LogPath, backup);
            }
            catch
            {
            }
        }
    }
}
