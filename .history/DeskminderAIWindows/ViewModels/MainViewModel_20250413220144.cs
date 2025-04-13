using DeskminderAI.Models;
using DeskminderAI.Services;
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
                
                // Removed Properties.Settings calls
                // Will implement registry startup manually
                StartupManager.SetStartupWithWindows(value);
            }
        }
        
        public bool StartMinimized
        {
            get => _startMinimized;
            set
            {
                _startMinimized = value;
                OnPropertyChanged();
                
                // Removed Properties.Settings calls
            }
        }
        
        public bool AlwaysOnTop
        {
            get => _alwaysOnTop;
            set
            {
                _alwaysOnTop = value;
                OnPropertyChanged();
                
                // Removed Properties.Settings calls
                
                // Update the window topmost property
                if (WPFApplication.Current.MainWindow != null)
                {
                    WPFApplication.Current.MainWindow.Topmost = value;
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
            _reminderService = new ReminderService();
            _reminders = _reminderService.LoadReminders();
            
            // Set default values for settings
            StartWithWindows = false;
            StartMinimized = false;
            AlwaysOnTop = true;
            
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
        
        private void AddReminder()
        {
            if (NewReminderMinutes <= 0)
            {
                WPFMessageBox.Show("Reminder duration must be at least 1 minute.", "Invalid Duration", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var name = string.IsNullOrWhiteSpace(NewReminderName) ? "Reminder" : NewReminderName;
            var reminder = new Reminder(name, NewReminderMinutes);
            
            Reminders.Add(reminder);
            SaveReminders();
            
            // Reset form
            NewReminderName = "";
            NewReminderMinutes = 1;
            IsAddingReminder = false;
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
            
            reminder.StopTimer();
            Reminders.Remove(reminder);
            SaveReminders();
        }
        
        public void SaveReminders()
        {
            _reminderService.SaveReminders(Reminders);
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