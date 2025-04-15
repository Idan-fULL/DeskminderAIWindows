using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Threading;
using DeskminderAI.Models;

namespace DeskminderAI
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private const string RemindersFileName = "reminders.json";
        private readonly DispatcherTimer _timer;
        private int _newReminderMinutes = 5;
        private string _newReminderName = "";
        
        public ObservableCollection<Reminder> Reminders { get; } = new ObservableCollection<Reminder>();

        public int NewReminderMinutes
        {
            get => _newReminderMinutes;
            set
            {
                if (value < 1) value = 1;
                if (value > 120) value = 120;
                
                _newReminderMinutes = value;
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
        
        public MainViewModel()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
            
            LoadReminders();
        }
        
        private void Timer_Tick(object? sender, EventArgs e)
        {
            foreach (var reminder in Reminders.ToList())
            {
                reminder.UpdateTimeLeft();
                if (reminder.IsExpired)
                {
                    Reminders.Remove(reminder);
                }
            }
            SaveReminders();
        }
        
        public void AddReminder()
        {
            if (string.IsNullOrWhiteSpace(NewReminderName))
            {
                return;
            }
            
            var reminder = new Reminder(NewReminderName, NewReminderMinutes);
            Reminders.Add(reminder);
            
            NewReminderName = "";
            NewReminderMinutes = 5;
            
            SaveReminders();
        }
        
        public void RemoveReminder(Guid id)
        {
            var reminder = Reminders.FirstOrDefault(r => r.Id == id);
            if (reminder != null)
            {
                Reminders.Remove(reminder);
                SaveReminders();
            }
        }
        
        public void SaveReminders()
        {
            try
            {
                var filePath = GetRemindersFilePath();
                var directory = Path.GetDirectoryName(filePath);
                
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var json = JsonSerializer.Serialize(Reminders.ToArray());
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving reminders: {ex.Message}");
            }
        }
        
        public void LoadReminders()
        {
            try
            {
                var filePath = GetRemindersFilePath();
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var savedReminders = JsonSerializer.Deserialize<Reminder[]>(json);
                    if (savedReminders != null)
                    {
                        Reminders.Clear();
                        foreach (var reminder in savedReminders.Where(r => !r.IsExpired))
                        {
                            Reminders.Add(reminder);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading reminders: {ex.Message}");
            }
        }
        
        private string GetRemindersFilePath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var deskminderPath = Path.Combine(appDataPath, "DeskminderAI");
            return Path.Combine(deskminderPath, RemindersFileName);
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 