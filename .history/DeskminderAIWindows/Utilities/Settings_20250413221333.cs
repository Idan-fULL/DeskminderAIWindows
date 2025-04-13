using System;
using System.Configuration;
using System.IO;
using System.Windows;
using Newtonsoft.Json;

namespace DeskminderAI.Utilities
{
    public class Settings
    {
        private static Settings? _instance;
        private static readonly object _lock = new object();
        
        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new Settings();
                            _instance.Load();
                        }
                    }
                }
                return _instance;
            }
        }
        
        // App settings 
        public double WindowPositionX { get; set; }
        public double WindowPositionY { get; set; }
        public bool StartWithWindows { get; set; }
        public bool StartMinimized { get; set; }
        public bool AlwaysOnTop { get; set; } = true;
        
        // File path for settings
        private static readonly string SettingsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DeskminderAI");
            
        private static readonly string SettingsFile = Path.Combine(SettingsFolder, "settings.json");
        
        private Settings()
        {
            // Default constructor
        }
        
        public void Load()
        {
            try
            {
                // Ensure the settings directory exists
                if (!Directory.Exists(SettingsFolder))
                {
                    Directory.CreateDirectory(SettingsFolder);
                }
                
                // Load settings if file exists
                if (File.Exists(SettingsFile))
                {
                    string json = File.ReadAllText(SettingsFile);
                    var settings = JsonConvert.DeserializeObject<Settings>(json);
                    
                    if (settings != null)
                    {
                        WindowPositionX = settings.WindowPositionX;
                        WindowPositionY = settings.WindowPositionY;
                        StartWithWindows = settings.StartWithWindows;
                        StartMinimized = settings.StartMinimized;
                        AlwaysOnTop = settings.AlwaysOnTop;
                    }
                }
                
                // Apply settings from Properties.Settings for compatibility
                if (WindowPositionX == 0 && WindowPositionY == 0)
                {
                    try
                    {
                        WindowPositionX = Properties.Settings.Default.WidgetPositionX;
                        WindowPositionY = Properties.Settings.Default.WidgetPositionY;
                        StartWithWindows = Properties.Settings.Default.StartWithWindows;
                        StartMinimized = Properties.Settings.Default.StartMinimized;
                        AlwaysOnTop = Properties.Settings.Default.AlwaysOnTop;
                    }
                    catch (Exception)
                    {
                        // Use defaults if Properties.Settings fail
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load settings: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        public void Save()
        {
            try
            {
                // Ensure the settings directory exists
                if (!Directory.Exists(SettingsFolder))
                {
                    Directory.CreateDirectory(SettingsFolder);
                }
                
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsFile, json);
                
                // Also update Properties.Settings for compatibility
                try
                {
                    Properties.Settings.Default.WidgetPositionX = WindowPositionX;
                    Properties.Settings.Default.WidgetPositionY = WindowPositionY;
                    Properties.Settings.Default.StartWithWindows = StartWithWindows;
                    Properties.Settings.Default.StartMinimized = StartMinimized;
                    Properties.Settings.Default.AlwaysOnTop = AlwaysOnTop;
                    Properties.Settings.Default.Save();
                }
                catch (Exception)
                {
                    // Continue even if Properties.Settings fail
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 