using System;
using System.Collections.ObjectModel;
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
        private string _name;
        private int _minutes;
        private DateTime _endTime;
        private string _timeLeftDisplay;
        private DispatcherTimer _timer;

        public Guid Id { get; } = Guid.NewGuid();
        
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

        public Reminder()
        {
            Name = "New Reminder";
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
                return;
            }
            
            if (timeLeft.TotalHours >= 1)
            {
                TimeLeftDisplay = $"{Math.Floor(timeLeft.TotalHours)}:{timeLeft.Minutes:00}:{timeLeft.Seconds:00}";
            }
            else
            {
                TimeLeftDisplay = $"{timeLeft.Minutes}:{timeLeft.Seconds:00}";
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

        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
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
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
            
            LoadReminders();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            foreach (var reminder in Reminders.ToList())
            {
                reminder.UpdateTimeLeft();
                
                // Check if reminder has expired and needs notification
                if (DateTime.Now >= reminder.EndTime)
                {
                    // Notification logic will be added elsewhere
                }
            }
        }

        public void AddReminder()
        {
            if (string.IsNullOrWhiteSpace(NewReminderName))
            {
                NewReminderName = $"תזכורת {DateTime.Now.ToString("HH:mm")}";
            }
            
            var reminder = new Reminder
            {
                Name = NewReminderName,
                Minutes = NewReminderMinutes,
                EndTime = DateTime.Now.AddMinutes(NewReminderMinutes)
            };
            
            reminder.UpdateTimeLeft();
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
                Reminders.Remove(reminder);
                SaveReminders();
            }
        }
        
        public void SaveReminders()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                var json = JsonSerializer.Serialize(Reminders, options);
                File.WriteAllText(GetRemindersFilePath(), json);
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
                    var reminders = JsonSerializer.Deserialize<ObservableCollection<Reminder>>(json);
                    
                    if (reminders != null)
                    {
                        Reminders.Clear();
                        foreach (var reminder in reminders)
                        {
                            // Only add reminders that have not expired
                            if (reminder.EndTime > DateTime.Now)
                            {
                                reminder.UpdateTimeLeft();
                                Reminders.Add(reminder);
                            }
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
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appData, "DeskminderAI");
            
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            
            return Path.Combine(appFolder, RemindersFileName);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 