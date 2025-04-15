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
using System.Collections.Specialized;
using System.Windows.Data;
using System.Windows.Threading;
using System.IO;
using System.Text.Json;

namespace DeskminderAI.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ReminderService _reminderService;
        private readonly DispatcherTimer _timer;
        private readonly string _remindersFilePath;
        private Reminder? _selectedReminder;
        private string _newReminderName = "";
        private int _newReminderMinutes = 1;
        private bool _startWithWindows;
        private bool _startMinimized;
        private bool _alwaysOnTop;
        
        public ObservableCollection<Reminder> Reminders { get; } = new ObservableCollection<Reminder>();
        
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
                if (_newReminderMinutes != value)
                {
                    _newReminderMinutes = value;
                    OnPropertyChanged();
                }
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
            _remindersFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DeskminderAI",
                "reminders.json"
            );

            // Initialize reminders collection
            Reminders.CollectionChanged += Reminders_CollectionChanged;

            // Load saved reminders
            LoadReminders();

            // Initialize timer for updating reminders
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // Ensure the view updates on the UI thread
            BindingOperations.EnableCollectionSynchronization(Reminders, new object());

            InitializeFromSettings();
        }
        
        private void Timer_Tick(object? sender, EventArgs e)
        {
            foreach (var reminder in Reminders.ToList())
            {
                reminder.UpdateTimeLeft();
                var timeLeft = reminder.TimeLeft;
                var isExpired = timeLeft.TotalSeconds <= 0;
                if (isExpired)
                {
                    Reminders.Remove(reminder);
                }
            }
            SaveReminders();
        }
        
        private void Reminders_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            SaveReminders();
            OnPropertyChanged(nameof(Reminders));
        }
        
        public void SaveReminders()
        {
            try
            {
                var directory = Path.GetDirectoryName(_remindersFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(Reminders.ToArray());
                File.WriteAllText(_remindersFilePath, json);
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
                if (File.Exists(_remindersFilePath))
                {
                    var json = File.ReadAllText(_remindersFilePath);
                    var savedReminders = JsonSerializer.Deserialize<Reminder[]>(json);
                    if (savedReminders != null)
                    {
                        Reminders.Clear();
                        foreach (var reminder in savedReminders.Where(r => r.TimeLeft.TotalSeconds > 0))
                        {
                            Reminders.Add(reminder);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WPFMessageBox.Show($"שגיאה בטעינת תזכורות: {ex.Message}", "שגיאה", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void InitializeFromSettings()
        {
            // Load settings
            StartWithWindows = Settings.Instance.StartWithWindows;
            StartMinimized = Settings.Instance.StartMinimized;
            AlwaysOnTop = Settings.Instance.AlwaysOnTop;
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 