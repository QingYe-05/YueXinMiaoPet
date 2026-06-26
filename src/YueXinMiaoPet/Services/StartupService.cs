using System;
using Microsoft.Win32;

namespace YueXinMiaoPet.Services
{
    public class StartupService
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "YueXinMiaoPet";

        public bool IsEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, false))
                {
                    object value = key == null ? null : key.GetValue(ValueName);
                    return value != null && !string.IsNullOrWhiteSpace(value.ToString());
                }
            }
            catch (Exception ex)
            {
                LogService.Error("读取开机启动状态失败。", ex);
                return false;
            }
        }

        public void SetEnabled(bool enabled)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (key == null)
                    {
                        return;
                    }

                    if (enabled)
                    {
                        string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        key.SetValue(ValueName, "\"" + exePath + "\"");
                    }
                    else
                    {
                        key.DeleteValue(ValueName, false);
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error("设置开机启动失败。", ex);
            }
        }
    }
}
