using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Threading;

namespace DeskminderAI
{
    public class Reminder : INotifyPropertyChanged
    {
        private string _name = "";
        private int _minutes;
        private DateTime _endTime;
        private string _timeLeftDisplay = "";
        private string _remainingTimeText = "";
        private DispatcherTimer? _timer;
        private Guid _id = Guid.NewGuid();

        public Guid Id 
        { 
            get => _id;
            set => _id = value;
        }
        
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }
        
        public int Minutes
        {
            get => _minutes;
            set
            {
                _minutes = value;
                OnPropertyChanged();
            }
        }
        
        public DateTime EndTime
        {
            get => _endTime;
            set
            {
                _endTime = value;
                OnPropertyChanged();
            }
        }

        public string TimeLeftDisplay
        {
            get => _timeLeftDisplay;
            set
            {
                _timeLeftDisplay = value;
                OnPropertyChanged();
            }
        }

        public string RemainingTimeText
        {
            get => _remainingTimeText;
            set
            {
                _remainingTimeText = value;
                OnPropertyChanged();
            }
        }

        public Reminder()
        {
            Name = "תזכורת";
            Minutes = 5;
            EndTime = DateTime.Now.AddMinutes(Minutes);
            InitializeTimer();
        }

        public Reminder(string name, int minutes)
        {
            Name = name;
            Minutes = minutes;
            EndTime = DateTime.Now.AddMinutes(minutes);
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) => UpdateTimeLeft();
            _timer.Start();
            UpdateTimeLeft();
        }

        public void UpdateTimeLeft()
        {
            var timeLeft = EndTime - DateTime.Now;
            
            if (timeLeft.TotalSeconds <= 0)
            {
                TimeLeftDisplay = "הסתיים!";
                RemainingTimeText = "הסתיים!";
                return;
            }
            
            // Format as minutes and seconds
            int minutesLeft = (int)Math.Floor(timeLeft.TotalMinutes);
            int secondsLeft = (int)Math.Floor(timeLeft.TotalSeconds % 60);
            
            if (minutesLeft == 0)
            {
                TimeLeftDisplay = $"{secondsLeft} שניות";
                RemainingTimeText = $"{secondsLeft} שניות";
            }
            else if (minutesLeft == 1)
            {
                TimeLeftDisplay = $"דקה אחת";
                RemainingTimeText = $"דקה ו-{secondsLeft} שניות";
            }
            else
            {
                TimeLeftDisplay = $"{minutesLeft} דקות";
                RemainingTimeText = $"{minutesLeft} דקות ו-{secondsLeft} שניות";
            }
        }

        public void StopTimer()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

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
            // Create a main timer for general cleanup and maintenance
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(10); // Less frequent checks
            _timer.Tick += Timer_Tick;
            _timer.Start();
            
            LoadReminders();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            // Check for and clean up expired reminders
            var expiredReminders = Reminders.Where(r => DateTime.Now >= r.EndTime.AddMinutes(5)).ToList();
            
            foreach (var reminder in expiredReminders)
            {
                // Stop the timer to avoid memory leaks
                reminder.StopTimer();
                
                // Remove from collection
                Reminders.Remove(reminder);
            }
            
            // Save if any reminders were removed
            if (expiredReminders.Any())
            {
                SaveReminders();
            }
        }

        public void AddReminder()
        {
            // Always use a generic name for reminders
            var reminderName = "תזכורת";
            
            var reminder = new Reminder(reminderName, NewReminderMinutes);
            Reminders.Add(reminder);
            
            // Reset fields for next reminder
            NewReminderName = "";
            
            SaveReminders();
        }
        
        public void RemoveReminder(Guid id)
        {
            var reminder = Reminders.FirstOrDefault(r => r.Id == id);
            if (reminder != null)
            {
                reminder.StopTimer();
                Reminders.Remove(reminder);
                SaveReminders();
            }
        }
        
        private void SaveReminders()
        {
            try
            {
                var remindersToSave = Reminders.Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Minutes,
                    EndTimeUnixTimeSeconds = new DateTimeOffset(r.EndTime).ToUnixTimeSeconds()
                }).ToList();

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(remindersToSave, options);
                
                string folderPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "DeskminderAI");
                
                Directory.CreateDirectory(folderPath);
                File.WriteAllText(Path.Combine(folderPath, RemindersFileName), json);
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
                string folderPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "DeskminderAI");
                
                string filePath = Path.Combine(folderPath, RemindersFileName);
                
                if (!File.Exists(filePath))
                {
                    return;
                }
                
                string json = File.ReadAllText(filePath);
                var loadedReminders = JsonSerializer.Deserialize<List<ReminderData>>(json);
                
                if (loadedReminders == null)
                {
                    return;
                }
                
                Reminders.Clear();
                
                foreach (var reminderData in loadedReminders)
                {
                    // Skip reminders that already ended
                    var endTime = DateTimeOffset.FromUnixTimeSeconds(reminderData.EndTimeUnixTimeSeconds).DateTime;
                    if (endTime < DateTime.Now)
                    {
                        continue;
                    }
                    
                    var reminder = new Reminder
                    {
                        Id = reminderData.Id,
                        Name = reminderData.Name,
                        Minutes = reminderData.Minutes,
                        EndTime = endTime
                    };
                    
                    reminder.InitializeTimer();
                    Reminders.Add(reminder);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading reminders: {ex.Message}");
            }
        }
        
        private class ReminderData
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = "";
            public int Minutes { get; set; }
            public long EndTimeUnixTimeSeconds { get; set; }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 