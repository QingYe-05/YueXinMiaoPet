using System;
using System.Windows;
using Forms = System.Windows.Forms;
using YueXinMiaoPet.Models;

namespace YueXinMiaoPet.Services
{
    public static class WindowPlacementService
    {
        public static bool NormalizeConfig(AppConfig config, double windowWidth, double windowHeight, bool forceCenter, string reason)
        {
            if (config == null)
            {
                return false;
            }

            bool changed = false;
            int originalScale = config.ScalePercent;
            int originalOpacity = config.OpacityPercent;

            config.ScalePercent = Clamp(config.ScalePercent <= 0 ? 100 : config.ScalePercent, 50, 200);
            config.Scale = config.ScalePercent / 100.0;
            config.OpacityPercent = Clamp(config.OpacityPercent <= 0 ? 100 : config.OpacityPercent, 30, 100);
            config.Opacity = config.OpacityPercent / 100.0;

            if (originalScale != config.ScalePercent || originalOpacity != config.OpacityPercent)
            {
                changed = true;
                LogService.Warn("窗口缩放或透明度异常，已修正。ScalePercent=" + config.ScalePercent + "，OpacityPercent=" + config.OpacityPercent);
            }

            double safeWidth = GetSafeSize(windowWidth, 220);
            double safeHeight = GetSafeSize(windowHeight, 220);
            double left = GetConfiguredLeft(config);
            double top = GetConfiguredTop(config);
            bool hasValidPosition = IsFinite(left) && IsFinite(top);
            bool visible = hasValidPosition && IsRectVisible(left, top, safeWidth, safeHeight);

            if (forceCenter || !hasValidPosition || !visible)
            {
                string normalizeReason = forceCenter ? "强制居中" : (!hasValidPosition ? "坐标异常" : "窗口在屏幕外");
                CenterConfigOnPrimary(config, safeWidth, safeHeight);
                changed = true;
                LogService.Warn("已重置窗口位置到主屏幕中央。原因=" + normalizeReason + "，触发=" + reason +
                    "，原始Left=" + left + "，原始Top=" + top +
                    "，新Left=" + config.WindowPositionX + "，新Top=" + config.WindowPositionY);
            }

            return changed;
        }

        public static void CenterConfigOnPrimary(AppConfig config, double windowWidth, double windowHeight)
        {
            if (config == null)
            {
                return;
            }

            System.Drawing.Rectangle area = Forms.Screen.PrimaryScreen.WorkingArea;
            double safeWidth = GetSafeSize(windowWidth, 220);
            double safeHeight = GetSafeSize(windowHeight, 220);
            double left = area.Left + Math.Max(0, (area.Width - safeWidth) / 2.0);
            double top = area.Top + Math.Max(0, (area.Height - safeHeight) / 2.0);
            config.WindowPositionX = left;
            config.WindowPositionY = top;
            config.WindowLeft = left;
            config.WindowTop = top;
        }

        public static bool EnsureWindowVisible(Window window, AppConfig config, string reason)
        {
            if (window == null)
            {
                return false;
            }

            double width = GetSafeSize(window.Width, 220);
            double height = GetSafeSize(window.Height, 220);
            double left = window.Left;
            double top = window.Top;

            if (IsFinite(left) && IsFinite(top) && IsRectVisible(left, top, width, height))
            {
                return false;
            }

            if (config != null)
            {
                CenterConfigOnPrimary(config, width, height);
                window.Left = config.WindowPositionX.HasValue ? config.WindowPositionX.Value : window.Left;
                window.Top = config.WindowPositionY.HasValue ? config.WindowPositionY.Value : window.Top;
            }
            else
            {
                System.Drawing.Rectangle area = Forms.Screen.PrimaryScreen.WorkingArea;
                window.Left = area.Left + Math.Max(0, (area.Width - width) / 2.0);
                window.Top = area.Top + Math.Max(0, (area.Height - height) / 2.0);
            }

            LogService.Warn("窗口不可见或在屏幕外，已移动到主屏幕中央。触发=" + reason +
                "，原始Left=" + left + "，原始Top=" + top +
                "，新Left=" + window.Left + "，新Top=" + window.Top);
            return true;
        }

        public static bool IsRectVisible(double left, double top, double width, double height)
        {
            if (!IsFinite(left) || !IsFinite(top))
            {
                return false;
            }

            double safeWidth = GetSafeSize(width, 220);
            double safeHeight = GetSafeSize(height, 220);
            Rect windowRect = new Rect(left, top, safeWidth, safeHeight);
            Forms.Screen[] screens = Forms.Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                System.Drawing.Rectangle working = screens[i].WorkingArea;
                Rect area = new Rect(working.Left, working.Top, working.Width, working.Height);
                if (windowRect.IntersectsWith(area))
                {
                    return true;
                }
            }

            return false;
        }

        private static double GetConfiguredLeft(AppConfig config)
        {
            if (config.WindowPositionX.HasValue)
            {
                return config.WindowPositionX.Value;
            }

            return config.WindowLeft.HasValue ? config.WindowLeft.Value : double.NaN;
        }

        private static double GetConfiguredTop(AppConfig config)
        {
            if (config.WindowPositionY.HasValue)
            {
                return config.WindowPositionY.Value;
            }

            return config.WindowTop.HasValue ? config.WindowTop.Value : double.NaN;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && Math.Abs(value) < 1000000;
        }

        private static double GetSafeSize(double value, double fallback)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 32 || value > 4000)
            {
                return fallback;
            }

            return value;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
