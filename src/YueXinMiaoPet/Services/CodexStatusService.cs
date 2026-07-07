using System;
using System.IO;
using System.Threading;
using YueXinMiaoPet.Models;
using YueXinMiaoPet.Utils;

namespace YueXinMiaoPet.Services
{
    public class CodexStatusChangedEventArgs : EventArgs
    {
        public CodexStatus Status { get; private set; }
        public string DisplayText { get; private set; }

        public CodexStatusChangedEventArgs(CodexStatus status, string displayText)
        {
            Status = status;
            DisplayText = displayText ?? string.Empty;
        }
    }

    public class CodexStatusService : IDisposable
    {
        private readonly object _syncRoot = new object();
        private FileSystemWatcher _watcher;
        private Timer _pollTimer;
        private AppConfig _config;
        private string _watchedPath = string.Empty;
        private string _lastStatusKey = string.Empty;
        private DateTime _lastParseErrorLogAt = DateTime.MinValue;

        public event EventHandler<CodexStatusChangedEventArgs> StatusChanged;

        public CodexStatus CurrentStatus { get; private set; }
        public string CurrentDisplayText { get; private set; }
        public bool IsRunning { get; private set; }

        public CodexStatusService()
        {
            CurrentStatus = CreateDefaultStatus("manual");
            CurrentDisplayText = GetDisplayText(CurrentStatus);
        }

        public void Start(AppConfig config)
        {
            lock (_syncRoot)
            {
                _config = config;
                if (config == null || !config.CodexStatusEnabled)
                {
                    StopLocked();
                    CurrentDisplayText = string.Empty;
                    RaiseChanged(CurrentStatus, CurrentDisplayText);
                    return;
                }

                string path = GetStatusFilePath(config);
                EnsureDefaultStatusFile(path);
                SetupWatcher(path);
                SetupPolling(config);
                IsRunning = true;
                LogService.Info("Codex 状态监听已启动。Path=" + path +
                    "，Bubble=" + config.CodexStatusBubbleEnabled +
                    "，AffectsGif=" + config.CodexStatusAffectsGif);
            }

            RefreshNow();
        }

        public void Stop()
        {
            lock (_syncRoot)
            {
                StopLocked();
            }
        }

        public void RefreshNow()
        {
            AppConfig config;
            lock (_syncRoot)
            {
                config = _config;
            }

            if (config == null || !config.CodexStatusEnabled)
            {
                return;
            }

            string path = GetStatusFilePath(config);
            CodexStatus status = ReadStatusSafe(path);
            string display = GetDisplayText(status);
            string key = BuildStatusKey(status, display);

            bool changed = false;
            lock (_syncRoot)
            {
                CurrentStatus = status;
                CurrentDisplayText = display;
                if (!string.Equals(_lastStatusKey, key, StringComparison.Ordinal))
                {
                    _lastStatusKey = key;
                    changed = true;
                }
            }

            if (changed)
            {
                LogService.Info("Codex 状态变化：" + (status == null ? "(null)" : status.Status) + "，" + display.Replace(Environment.NewLine, " / "));
                RaiseChanged(status, display);
            }
        }

        public void WriteIdleStatus(AppConfig config, string source)
        {
            WriteStatus(config, CreateDefaultStatus(string.IsNullOrWhiteSpace(source) ? "tray" : source));
        }

        public void WriteTestStatus(AppConfig config)
        {
            CodexStatus status = new CodexStatus
            {
                Enabled = true,
                Status = "coding",
                Title = "Codex 测试状态",
                Message = "如果你看到这条消息，说明月薪喵已成功读取 Codex 状态。",
                Task = "Codex 状态测试",
                Progress = 50,
                Source = "settings"
            };
            status.UpdatedAt = DateTime.Now;
            WriteStatus(config, status);
        }

        public void WriteStatus(AppConfig config, CodexStatus status)
        {
            if (config == null)
            {
                return;
            }

            WriteStatusFile(GetStatusFilePath(config), status);
            RefreshNow();
        }

        public void WriteStatusFile(string path, CodexStatus status)
        {
            try
            {
                if (status == null)
                {
                    status = CreateDefaultStatus("manual");
                }

                NormalizeStatus(status);
                string resolved = ResolveStatusFilePath(path);
                SafeJson.Write(resolved, status);
                LogService.Info("Codex 状态文件已写入：" + resolved + "，Status=" + status.Status);
            }
            catch (Exception ex)
            {
                LogService.Error("写入 Codex 状态文件失败：" + path, ex);
            }
        }

        public void EnsureDefaultStatusFile(AppConfig config)
        {
            if (config == null)
            {
                return;
            }

            EnsureDefaultStatusFile(GetStatusFilePath(config));
        }

        public void EnsureDefaultStatusFile(string path)
        {
            try
            {
                string resolved = ResolveStatusFilePath(path);
                string dir = Path.GetDirectoryName(resolved);
                if (!string.IsNullOrWhiteSpace(dir))
                {
                    FilePathHelper.EnsureDirectory(dir);
                }

                if (!File.Exists(resolved))
                {
                    SafeJson.Write(resolved, CreateDefaultStatus("default"));
                    LogService.Info("已创建默认 Codex 状态文件：" + resolved);
                }
            }
            catch (Exception ex)
            {
                LogService.Error("创建默认 Codex 状态文件失败：" + path, ex);
            }
        }

        public string GetStatusFilePath(AppConfig config)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.CodexStatusFilePath))
            {
                return FilePathHelper.CodexStatusPath;
            }

            return ResolveStatusFilePath(config.CodexStatusFilePath);
        }

        public string ResolveStatusFilePath(string path)
        {
            string resolved = FilePathHelper.ExpandEnvironmentPath(path);
            if (string.IsNullOrWhiteSpace(resolved))
            {
                return FilePathHelper.CodexStatusPath;
            }

            return resolved;
        }

        public static CodexStatus CreateDefaultStatus(string source)
        {
            CodexStatus status = new CodexStatus
            {
                Enabled = true,
                Status = "idle",
                Title = "Codex 空闲中",
                Message = "没有正在执行的任务",
                Task = string.Empty,
                Progress = 0,
                Source = string.IsNullOrWhiteSpace(source) ? "manual" : source
            };
            status.UpdatedAt = DateTime.Now;
            return status;
        }

        public static string GetStatusDisplayName(string status)
        {
            string normalized = NormalizeStatusTag(status);
            if (normalized == "idle") return "空闲";
            if (normalized == "planning") return "正在分析任务";
            if (normalized == "reading") return "正在阅读项目";
            if (normalized == "coding") return "正在写代码";
            if (normalized == "building") return "正在构建";
            if (normalized == "testing") return "正在测试";
            if (normalized == "reviewing") return "正在检查";
            if (normalized == "waiting") return "等待用户确认";
            if (normalized == "done") return "已完成";
            if (normalized == "error") return "出错了";
            return "未知状态";
        }

        public static string NormalizeStatusTag(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return "unknown";
            }

            string value = status.Trim().ToLowerInvariant();
            if (value == "idle" || value == "planning" || value == "reading" || value == "coding" ||
                value == "building" || value == "testing" || value == "reviewing" || value == "waiting" ||
                value == "done" || value == "error")
            {
                return value;
            }

            return "unknown";
        }

        public static string MapStatusToMoodSuggestion(string status)
        {
            string normalized = NormalizeStatusTag(status);
            if (normalized == "done") return "happy";
            if (normalized == "error") return "collapse";
            if (normalized == "waiting" || normalized == "building" || normalized == "testing") return "thinking";
            if (normalized == "planning" || normalized == "reading" || normalized == "coding" || normalized == "reviewing") return "thinking";
            return string.Empty;
        }

        public string GetDisplayText(CodexStatus status)
        {
            if (status == null || !status.Enabled)
            {
                return string.Empty;
            }

            NormalizeStatus(status);
            string displayName = GetStatusDisplayName(status.Status);
            bool stale = IsStale(status);

            string firstLine = stale ? "Codex：状态可能已过期" : "Codex：" + displayName;
            string secondLine = string.Empty;
            if (!string.IsNullOrWhiteSpace(status.Message))
            {
                secondLine = status.Message.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(status.Title))
            {
                secondLine = status.Title.Trim();
            }

            if (status.Progress > 0 && status.Progress < 100)
            {
                if (secondLine.Length > 0)
                {
                    secondLine += " " + status.Progress + "%";
                }
                else
                {
                    secondLine = status.Progress + "%";
                }
            }

            if (string.IsNullOrWhiteSpace(secondLine))
            {
                return firstLine;
            }

            return firstLine + Environment.NewLine + secondLine;
        }

        public bool IsStale(CodexStatus status)
        {
            if (status == null)
            {
                return false;
            }

            string normalized = NormalizeStatusTag(status.Status);
            if (normalized == "idle")
            {
                return false;
            }

            DateTime updatedAt = status.UpdatedAt;
            if (updatedAt == DateTime.MinValue)
            {
                return true;
            }

            return DateTime.Now - updatedAt.ToLocalTime() > TimeSpan.FromMinutes(10);
        }

        private CodexStatus ReadStatusSafe(string path)
        {
            try
            {
                EnsureDefaultStatusFile(path);
                CodexStatus fallback = CreateDefaultStatus("fallback");
                CodexStatus status = SafeJson.Read(ResolveStatusFilePath(path), fallback) ?? fallback;
                NormalizeStatus(status);
                return status;
            }
            catch (Exception ex)
            {
                if (DateTime.Now - _lastParseErrorLogAt > TimeSpan.FromSeconds(30))
                {
                    _lastParseErrorLogAt = DateTime.Now;
                    LogService.Error("读取 Codex 状态文件失败，已回退 idle：" + path, ex);
                }

                return CreateDefaultStatus("fallback");
            }
        }

        private void NormalizeStatus(CodexStatus status)
        {
            if (status == null)
            {
                return;
            }

            status.Status = NormalizeStatusTag(status.Status);
            if (string.IsNullOrWhiteSpace(status.Title))
            {
                status.Title = "Codex " + GetStatusDisplayName(status.Status);
            }

            if (status.Message == null) status.Message = string.Empty;
            if (status.Task == null) status.Task = string.Empty;
            if (status.Source == null) status.Source = string.Empty;
            if (status.Progress < 0) status.Progress = 0;
            if (status.Progress > 100) status.Progress = 100;
            if (status.UpdatedAt == DateTime.MinValue)
            {
                status.UpdatedAt = DateTime.Now;
            }
        }

        private void SetupWatcher(string path)
        {
            string resolved = ResolveStatusFilePath(path);
            if (string.Equals(_watchedPath, resolved, StringComparison.OrdinalIgnoreCase) && _watcher != null)
            {
                return;
            }

            if (_watcher != null)
            {
                _watcher.Dispose();
                _watcher = null;
            }

            _watchedPath = resolved;
            string dir = Path.GetDirectoryName(resolved);
            string file = Path.GetFileName(resolved);
            if (string.IsNullOrWhiteSpace(dir) || string.IsNullOrWhiteSpace(file))
            {
                return;
            }

            FilePathHelper.EnsureDirectory(dir);
            try
            {
                _watcher = new FileSystemWatcher(dir, file);
                _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime;
                _watcher.Changed += OnStatusFileChanged;
                _watcher.Created += OnStatusFileChanged;
                _watcher.Renamed += OnStatusFileChanged;
                _watcher.Deleted += OnStatusFileChanged;
                _watcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                LogService.Error("启动 Codex 状态 FileSystemWatcher 失败，将依赖轮询：" + resolved, ex);
            }
        }

        private void SetupPolling(AppConfig config)
        {
            int seconds = config == null ? 2 : config.CodexStatusRefreshIntervalSeconds;
            seconds = Math.Max(1, Math.Min(60, seconds));

            if (_pollTimer != null)
            {
                _pollTimer.Dispose();
            }

            _pollTimer = new Timer(delegate { RefreshNow(); }, null, TimeSpan.FromSeconds(seconds), TimeSpan.FromSeconds(seconds));
        }

        private void OnStatusFileChanged(object sender, FileSystemEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    Thread.Sleep(120);
                    RefreshNow();
                }
                catch (Exception ex)
                {
                    LogService.Error("处理 Codex 状态文件变化失败。", ex);
                }
            });
        }

        private string BuildStatusKey(CodexStatus status, string display)
        {
            if (status == null)
            {
                return string.Empty;
            }

            return (status.Enabled ? "1" : "0") + "|" +
                (status.Status ?? string.Empty) + "|" +
                (status.Title ?? string.Empty) + "|" +
                (status.Message ?? string.Empty) + "|" +
                status.Progress + "|" +
                (status.UpdatedAtText ?? string.Empty) + "|" +
                (display ?? string.Empty);
        }

        private void StopLocked()
        {
            IsRunning = false;
            if (_watcher != null)
            {
                _watcher.Dispose();
                _watcher = null;
            }

            if (_pollTimer != null)
            {
                _pollTimer.Dispose();
                _pollTimer = null;
            }

            _watchedPath = string.Empty;
        }

        private void RaiseChanged(CodexStatus status, string display)
        {
            EventHandler<CodexStatusChangedEventArgs> handler = StatusChanged;
            if (handler != null)
            {
                handler(this, new CodexStatusChangedEventArgs(status == null ? null : status.Clone(), display));
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
