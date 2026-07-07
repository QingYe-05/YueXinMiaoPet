using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Forms = System.Windows.Forms;
using YueXinMiaoPet.Utils;

namespace YueXinMiaoPet.Services
{
    public class TrayService : IDisposable
    {
        private readonly MainPetWindow _petWindow;
        private readonly ConfigService _configService;
        private readonly CodexStatusService _codexStatusService;
        private readonly MoodService _moodService;
        private readonly Action _showSettings;
        private readonly Action _showMood;
        private readonly Action _rescan;
        private readonly Action _exit;
        private readonly Forms.NotifyIcon _notifyIcon;
        private readonly Forms.ToolStripMenuItem _showHideItem;
        private readonly Forms.ToolStripMenuItem _codexEnabledItem;
        private readonly Dictionary<string, Forms.ToolStripMenuItem> _moodItems = new Dictionary<string, Forms.ToolStripMenuItem>(StringComparer.OrdinalIgnoreCase);

        public TrayService(
            MainPetWindow petWindow,
            ConfigService configService,
            CodexStatusService codexStatusService,
            MoodService moodService,
            Action showSettings,
            Action showMood,
            Action rescan,
            Action exit)
        {
            _petWindow = petWindow;
            _configService = configService;
            _codexStatusService = codexStatusService;
            _moodService = moodService;
            _showSettings = showSettings;
            _showMood = showMood;
            _rescan = rescan;
            _exit = exit;

            _notifyIcon = new Forms.NotifyIcon();
            _notifyIcon.Text = "月薪喵桌宠";
            _notifyIcon.Icon = LoadTrayIcon();
            _notifyIcon.Visible = true;
            _notifyIcon.DoubleClick += delegate { _petWindow.ShowPet(); };

            Forms.ContextMenuStrip menu = new Forms.ContextMenuStrip();
            _showHideItem = new Forms.ToolStripMenuItem("隐藏桌宠");
            _showHideItem.Click += delegate { TogglePetVisibility(); };
            menu.Items.Add(_showHideItem);

            Forms.ToolStripMenuItem showPetItem = new Forms.ToolStripMenuItem("显示月薪喵");
            showPetItem.Click += delegate { _petWindow.ShowPet(); };
            menu.Items.Add(showPetItem);

            Forms.ToolStripMenuItem resetPositionItem = new Forms.ToolStripMenuItem("重置位置到屏幕中央");
            resetPositionItem.Click += delegate { _petWindow.ResetPositionToScreenCenter(); };
            menu.Items.Add(resetPositionItem);

            menu.Items.Add(new Forms.ToolStripSeparator());

            Forms.ToolStripMenuItem codexMenu = new Forms.ToolStripMenuItem("Codex 状态");
            _codexEnabledItem = new Forms.ToolStripMenuItem("启用 Codex 状态显示");
            _codexEnabledItem.CheckOnClick = true;
            _codexEnabledItem.Checked = _configService != null && _configService.Current != null && _configService.Current.CodexStatusEnabled;
            _codexEnabledItem.Click += delegate { ToggleCodexStatus(); };
            codexMenu.DropDownItems.Add(_codexEnabledItem);

            Forms.ToolStripMenuItem showCodexItem = new Forms.ToolStripMenuItem("显示 Codex 状态");
            showCodexItem.Click += delegate { _petWindow.ShowCodexStatusNow(); };
            codexMenu.DropDownItems.Add(showCodexItem);

            Forms.ToolStripMenuItem openCodexFileItem = new Forms.ToolStripMenuItem("打开 Codex 状态文件");
            openCodexFileItem.Click += delegate { OpenCodexStatusFile(); };
            codexMenu.DropDownItems.Add(openCodexFileItem);

            Forms.ToolStripMenuItem resetCodexItem = new Forms.ToolStripMenuItem("重置 Codex 状态为空闲");
            resetCodexItem.Click += delegate { ResetCodexStatusToIdle(); };
            codexMenu.DropDownItems.Add(resetCodexItem);

            menu.Items.Add(codexMenu);

            menu.Items.Add(new Forms.ToolStripSeparator());

            Forms.ToolStripMenuItem moodMenu = new Forms.ToolStripMenuItem("今日心情");
            foreach (KeyValuePair<string, string> item in _moodService.GetMoodOptions())
            {
                string moodKey = item.Key;
                Forms.ToolStripMenuItem moodItem = new Forms.ToolStripMenuItem(item.Value);
                moodItem.Click += delegate
                {
                    _moodService.SetMood(moodKey, "today");
                    RefreshMoodChecks();
                    _petWindow.RefreshMoodNow();
                };
                _moodItems[moodKey] = moodItem;
                moodMenu.DropDownItems.Add(moodItem);
            }

            moodMenu.DropDownItems.Add(new Forms.ToolStripSeparator());
            Forms.ToolStripMenuItem customMood = new Forms.ToolStripMenuItem("打开心情面板...");
            customMood.Click += delegate { _showMood(); };
            moodMenu.DropDownItems.Add(customMood);
            menu.Items.Add(moodMenu);

            Forms.ToolStripMenuItem settingsItem = new Forms.ToolStripMenuItem("设置");
            settingsItem.Click += delegate { _showSettings(); };
            menu.Items.Add(settingsItem);

            Forms.ToolStripMenuItem rescanItem = new Forms.ToolStripMenuItem("重新扫描 GIF");
            rescanItem.Click += delegate { _rescan(); };
            menu.Items.Add(rescanItem);

            menu.Items.Add(new Forms.ToolStripSeparator());
            Forms.ToolStripMenuItem exitItem = new Forms.ToolStripMenuItem("退出");
            exitItem.Click += delegate { _exit(); };
            menu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = menu;
            RefreshMoodChecks();
        }

        public void UpdateVisibilityText(bool visible)
        {
            if (_showHideItem != null)
            {
                _showHideItem.Text = visible ? "隐藏桌宠" : "显示桌宠";
            }
        }

        public void RefreshMoodChecks()
        {
            string current = _moodService.GetCurrentMood();
            foreach (KeyValuePair<string, Forms.ToolStripMenuItem> item in _moodItems)
            {
                item.Value.Checked = string.Equals(item.Key, current, StringComparison.OrdinalIgnoreCase);
            }
        }

        private Icon LoadTrayIcon()
        {
            try
            {
                string path = Path.Combine(FilePathHelper.AppBaseDir, "Assets", "Icons", "tray.ico");
                if (File.Exists(path))
                {
                    return new Icon(path);
                }
            }
            catch (Exception ex)
            {
                LogService.Error("加载托盘图标失败，使用系统兜底图标。", ex);
            }

            return SystemIcons.Application;
        }

        private void TogglePetVisibility()
        {
            if (_petWindow.IsVisible)
            {
                _petWindow.HidePet();
            }
            else
            {
                _petWindow.ShowPet();
            }
        }

        private void ToggleCodexStatus()
        {
            if (_configService == null || _configService.Current == null)
            {
                return;
            }

            bool enabled = _codexEnabledItem != null && _codexEnabledItem.Checked;
            _configService.Update(config =>
            {
                config.CodexStatusEnabled = enabled;
                if (enabled)
                {
                    config.CodexStatusBubbleEnabled = true;
                }
            });

            if (_codexStatusService != null && enabled)
            {
                _codexStatusService.EnsureDefaultStatusFile(_configService.Current);
            }

            _petWindow.ApplyConfig();
            _petWindow.RefreshCodexStatusNow();
        }

        private void OpenCodexStatusFile()
        {
            try
            {
                if (_codexStatusService == null || _configService == null)
                {
                    return;
                }

                _codexStatusService.EnsureDefaultStatusFile(_configService.Current);
                string path = _codexStatusService.GetStatusFilePath(_configService.Current);
                if (File.Exists(path))
                {
                    Process.Start("explorer.exe", "/select,\"" + path + "\"");
                }
                else
                {
                    string dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrWhiteSpace(dir))
                    {
                        Process.Start("explorer.exe", "\"" + dir + "\"");
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error("打开 Codex 状态文件失败。", ex);
            }
        }

        private void ResetCodexStatusToIdle()
        {
            if (_codexStatusService == null || _configService == null)
            {
                return;
            }

            _codexStatusService.WriteIdleStatus(_configService.Current, "tray");
            _petWindow.RefreshCodexStatusNow();
        }

        public void Dispose()
        {
            try
            {
                _notifyIcon.Visible = false;
                if (_notifyIcon.Icon != null && !object.ReferenceEquals(_notifyIcon.Icon, SystemIcons.Application))
                {
                    _notifyIcon.Icon.Dispose();
                }
                _notifyIcon.Dispose();
            }
            catch
            {
            }
        }
    }
}
