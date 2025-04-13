using DeskminderAI.Models;
using DeskminderAI.Services;
using DeskminderAI.Utilities;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using WPFApplication = System.Windows.Application;
using WPFMessageBox = System.Windows.MessageBox;

namespace DeskminderAI.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ReminderService _reminderService;
        private ObservableCollection<Reminder> _reminders;
        private Reminder? _selectedReminder;
        private string _newReminderName = "";
        private int _newReminderMinutes = 1;
        private bool _startWithWindows;
        private bool _startMinimized;
        private bool _alwaysOnTop;
        
        // Properties marked as 'required' to ensure they are initialized
        public ObservableCollection<Reminder> Reminders
        {
            get => _reminders;
            set
            {
                _reminders = value;
                OnPropertyChanged();
            }
        }
        
        public Reminder? SelectedReminder
        {
            get => _selectedReminder;
            set
            {
                _selectedReminder = value;
                OnPropertyChanged();
            }
        }
        
        public string NewReminderName
        {
            get => _newReminderName;
            set
            {
                _newReminderName = value;
                OnPropertyChanged();
            }
        }
        
        public int NewReminderMinutes
        {
            get => _newReminderMinutes;
            set
            {
                _newReminderMinutes = value;
                OnPropertyChanged();
            }
        }
        
        public bool StartWithWindows
        {
            get => _startWithWindows;
            set
            {
                if (_startWithWindows != value)
                {
                    _startWithWindows = value;
                    OnPropertyChanged();
                    
                    // Update settings
                    Settings.Instance.StartWithWindows = value;
                    Settings.Instance.Save();
                    
                    // Update startup registry
                    StartupManager.SetStartupWithWindows(value);
                }
            }
        }
        
        public bool StartMinimized
        {
            get => _startMinimized;
            set
            {
                if (_startMinimized != value)
                {
                    _startMinimized = value;
                    OnPropertyChanged();
                    
                    // Update settings
                    Settings.Instance.StartMinimized = value;
                    Settings.Instance.Save();
                }
            }
        }
        
        public bool AlwaysOnTop
        {
            get => _alwaysOnTop;
            set
            {
                if (_alwaysOnTop != value)
                {
                    _alwaysOnTop = value;
                    OnPropertyChanged();
                    
                    // Update settings
                    Settings.Instance.AlwaysOnTop = value;
                    Settings.Instance.Save();
                    
                    // Update all open windows
                    foreach (Window window in WPFApplication.Current.Windows)
                    {
                        window.Topmost = value;
                    }
                }
            }
        }
        
        public MainViewModel()
        {
            _reminderService = new ReminderService();
            _reminders = new ObservableCollection<Reminder>();
            
            InitializeFromSettings();
            LoadReminders();
        }
        
        public void SaveReminders()
        {
            try
            {
                _reminderService.SaveReminders(Reminders);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving reminders: {ex.Message}");
            }
        }
        
        private void LoadReminders()
        {
            try
            {
                var loadedReminders = _reminderService.LoadReminders();
                if (loadedReminders != null)
                {
                    Reminders = loadedReminders;
                    
                    // Select the first reminder if available
                    if (Reminders.Count > 0)
                    {
                        SelectedReminder = Reminders[0];
                    }
                }
            }
            catch (Exception ex)
            {
                WPFMessageBox.Show($"שגיאה בטעינת תזכורות: {ex.Message}", "שגיאה", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Reminders = new ObservableCollection<Reminder>();
            }
        }
        
        private void InitializeFromSettings()
        {
            try
            {
                _startWithWindows = Settings.Instance.StartWithWindows;
                _startMinimized = Settings.Instance.StartMinimized;
                _alwaysOnTop = Settings.Instance.AlwaysOnTop;
                
                // Set up collection change notification to save on changes
                Reminders.CollectionChanged += (s, e) => 
                {
                    try 
                    {
                        SaveReminders();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error saving reminders on collection change: {ex.Message}");
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing from settings: {ex.Message}");
                // Use default values
                _startWithWindows = false;
                _startMinimized = false;
                _alwaysOnTop = true;
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 