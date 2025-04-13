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
        private bool _isAddingReminder;
        private bool _settingsVisible;
        private bool _startWithWindows;
        private bool _startMinimized;
        private bool _alwaysOnTop;
        
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
        
        public bool IsAddingReminder
        {
            get => _isAddingReminder;
            set
            {
                _isAddingReminder = value;
                OnPropertyChanged();
            }
        }
        
        public bool SettingsVisible
        {
            get => _settingsVisible;
            set
            {
                _settingsVisible = value;
                OnPropertyChanged();
            }
        }
        
        public bool StartWithWindows
        {
            get => _startWithWindows;
            set
            {
                _startWithWindows = value;
                OnPropertyChanged();
                
                try
                {
                    // Update the settings
                    Settings.Instance.StartWithWindows = value;
                    Settings.Instance.Save();
                    
                    // Update startup registry
                    StartupManager.SetStartupWithWindows(value);
                }
                catch (Exception ex)
                {
                    WPFMessageBox.Show($"שגיאה בעדכון הגדרות הפעלה: {ex.Message}", "שגיאה", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        public bool StartMinimized
        {
            get => _startMinimized;
            set
            {
                _startMinimized = value;
                OnPropertyChanged();
                
                try
                {
                    // Update the settings
                    Settings.Instance.StartMinimized = value;
                    Settings.Instance.Save();
                }
                catch (Exception ex)
                {
                    WPFMessageBox.Show($"שגיאה בעדכון הגדרות הפעלה: {ex.Message}", "שגיאה", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        public bool AlwaysOnTop
        {
            get => _alwaysOnTop;
            set
            {
                _alwaysOnTop = value;
                OnPropertyChanged();
                
                try
                {
                    // Update the settings
                    Settings.Instance.AlwaysOnTop = value;
                    Settings.Instance.Save();
                    
                    // Update the window topmost property
                    if (WPFApplication.Current.MainWindow != null)
                    {
                        WPFApplication.Current.MainWindow.Topmost = value;
                    }
                }
                catch (Exception ex)
                {
                    WPFMessageBox.Show($"שגיאה בעדכון הגדרות תמיד בחזית: {ex.Message}", "שגיאה", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        // Commands
        public ICommand AddReminderCommand { get; }
        public ICommand ShowAddReminderCommand { get; }
        public ICommand CancelAddReminderCommand { get; }
        public ICommand RemoveReminderCommand { get; }
        public ICommand ShowSettingsCommand { get; }
        public ICommand HideSettingsCommand { get; }
        
        public MainViewModel()
        {
            try
            {
                _reminderService = new ReminderService();
                _reminders = new ObservableCollection<Reminder>();
                
                // Load reminders safely
                try
                {
                    var loadedReminders = _reminderService.LoadReminders();
                    if (loadedReminders != null)
                    {
                        _reminders = loadedReminders;
                    }
                }
                catch (Exception ex)
                {
                    WPFMessageBox.Show($"שגיאה בטעינת תזכורות: {ex.Message}", "שגיאה", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    _reminders = new ObservableCollection<Reminder>();
                }
                
                // Load values from settings
                try
                {
                    _startWithWindows = Settings.Instance.StartWithWindows;
                    _startMinimized = Settings.Instance.StartMinimized;
                    _alwaysOnTop = Settings.Instance.AlwaysOnTop;
                }
                catch (Exception)
                {
                    // Use defaults if settings fail to load
                    _startWithWindows = false;
                    _startMinimized = false;
                    _alwaysOnTop = true;
                }
                
                // Initialize commands
                AddReminderCommand = new RelayCommand(AddReminder);
                ShowAddReminderCommand = new RelayCommand(ShowAddReminder);
                CancelAddReminderCommand = new RelayCommand(CancelAddReminder);
                RemoveReminderCommand = new RelayCommand<Reminder>(RemoveReminder);
                ShowSettingsCommand = new RelayCommand(() => SettingsVisible = true);
                HideSettingsCommand = new RelayCommand(() => SettingsVisible = false);
                
                // Set up collection change notification to save on changes
                Reminders.CollectionChanged += (s, e) => SaveReminders();
            }
            catch (Exception ex)
            {
                WPFMessageBox.Show($"שגיאה באתחול המודל: {ex.Message}", "שגיאה באתחול", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Ensure we at least have an empty collection
                _reminderService = _reminderService ?? new ReminderService();
                _reminders = _reminders ?? new ObservableCollection<Reminder>();
            }
        }
        
        private void AddReminder()
        {
            try
            {
                if (NewReminderMinutes <= 0)
                {
                    WPFMessageBox.Show("משך התזכורת חייב להיות לפחות דקה אחת.", "משך לא תקין", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                var name = string.IsNullOrWhiteSpace(NewReminderName) ? "תזכורת" : NewReminderName;
                var reminder = new Reminder(name, NewReminderMinutes);
                
                Reminders.Add(reminder);
                SaveReminders();
                
                // Reset form
                NewReminderName = "";
                NewReminderMinutes = 1;
                IsAddingReminder = false;
            }
            catch (Exception ex)
            {
                WPFMessageBox.Show($"שגיאה בהוספת תזכורת: {ex.Message}", "שגיאה", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void ShowAddReminder()
        {
            IsAddingReminder = true;
        }
        
        private void CancelAddReminder()
        {
            NewReminderName = "";
            NewReminderMinutes = 1;
            IsAddingReminder = false;
        }
        
        private void RemoveReminder(Reminder? reminder)
        {
            if (reminder == null) return;
            
            try
            {
                reminder.StopTimer();
                Reminders.Remove(reminder);
                SaveReminders();
            }
            catch (Exception ex)
            {
                WPFMessageBox.Show($"שגיאה בהסרת תזכורת: {ex.Message}", "שגיאה", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        public void SaveReminders()
        {
            try
            {
                if (_reminderService != null)
                {
                    _reminderService.SaveReminders(Reminders);
                }
            }
            catch (Exception ex)
            {
                WPFMessageBox.Show($"שגיאה בשמירת תזכורות: {ex.Message}", "שגיאה", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    // Helper command classes
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;
        
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }
        
        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();
        
        public void Execute(object? parameter) => _execute();
        
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
    
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Predicate<T?>? _canExecute;
        
        public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }
        
        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute(parameter is T t ? t : default);
        }
        
        public void Execute(object? parameter)
        {
            _execute(parameter is T t ? t : default);
        }
        
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
} 