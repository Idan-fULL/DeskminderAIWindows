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
                            try
                            {
                                _instance.Load();
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"שגיאה בטעינת הגדרות: {ex.Message}\nנטענות הגדרות ברירת מחדל.", 
                                    "שגיאה", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
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
            // Default constructor with default values
            WindowPositionX = 100;
            WindowPositionY = 100;
            StartWithWindows = false;
            StartMinimized = false;
            AlwaysOnTop = true;
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
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var settings = JsonConvert.DeserializeObject<Settings>(json);
                        
                        if (settings != null)
                        {
                            WindowPositionX = settings.WindowPositionX;
                            WindowPositionY = settings.WindowPositionY;
                            StartWithWindows = settings.StartWithWindows;
                            StartMinimized = settings.StartMinimized;
                            AlwaysOnTop = settings.AlwaysOnTop;
                            return;
                        }
                    }
                }
                
                // If we got here, either file doesn't exist or couldn't be loaded
                // Try to load from Properties.Settings for compatibility
                try
                {
                    WindowPositionX = Properties.Settings.Default.WidgetPositionX;
                    WindowPositionY = Properties.Settings.Default.WidgetPositionY;
                    StartWithWindows = Properties.Settings.Default.StartWithWindows;
                    StartMinimized = Properties.Settings.Default.StartMinimized;
                    AlwaysOnTop = Properties.Settings.Default.AlwaysOnTop;
                }
                catch (Exception ex)
                {
                    // Log the error but continue with defaults
                    Console.WriteLine($"Failed to load from Properties.Settings: {ex.Message}");
                    
                    // If window position isn't set, set some sensible defaults
                    if (WindowPositionX <= 0 || WindowPositionY <= 0)
                    {
                        // Get screen dimensions if possible
                        try
                        {
                            WindowPositionX = SystemParameters.WorkArea.Width - 300;
                            WindowPositionY = 100;
                        }
                        catch
                        {
                            // Use hardcoded defaults if even that fails
                            WindowPositionX = 100;
                            WindowPositionY = 100;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load settings: {ex.Message}", ex);
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
                
                // Serialize settings to JSON
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                
                // Write to file
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
                catch (Exception ex)
                {
                    // Log the error but don't fail the whole save operation
                    Console.WriteLine($"Failed to save to Properties.Settings: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"שגיאה בשמירת הגדרות: {ex.Message}", "שגיאה", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 