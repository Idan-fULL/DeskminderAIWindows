using Microsoft.Win32;
using System;
using System.Reflection;
using System.Windows;

namespace DeskminderAI
{
    public static class StartupManager
    {
        private const string RUN_REGISTRY_KEY = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string APP_NAME = "DeskminderAI";

        public static void SetStartupWithWindows(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_REGISTRY_KEY, true))
                {
                    if (key == null)
                    {
                        MessageBox.Show("Failed to access startup registry key.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (enable)
                    {
                        string appPath = Assembly.GetExecutingAssembly().Location;
                        key.SetValue(APP_NAME, appPath);
                    }
                    else
                    {
                        if (key.GetValue(APP_NAME) != null)
                        {
                            key.DeleteValue(APP_NAME);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to set startup preference: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_REGISTRY_KEY))
                {
                    return key?.GetValue(APP_NAME) != null;
                }
            }
            catch
            {
                return false;
            }
        }
    }
} 