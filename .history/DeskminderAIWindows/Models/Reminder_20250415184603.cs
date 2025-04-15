using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace DeskminderAI.Models
{
    public class Reminder : INotifyPropertyChanged
    {
        private string _id = "";
        private string _name = "";
        private DateTime _endTime;
        private int _minutes;
        private string _timeLeft = "";
        private bool _isCompleted;
        private DispatcherTimer? _timer;
        private DateTime _createdAt;
        private TimeSpan _timeLeftSpan;
        private bool _isExpired;
        
        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
            }
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
        
        public DateTime EndTime
        {
            get => _endTime;
            set
            {
                _endTime = value;
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
        
        public string TimeLeft
        {
            get => _timeLeft;
            set
            {
                _timeLeft = value;
                OnPropertyChanged();
            }
        }
        
        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                _isCompleted = value;
                OnPropertyChanged();
            }
        }
        
        public bool IsExpired
        {
            get => _isExpired;
            private set
            {
                _isExpired = value;
                OnPropertyChanged();
            }
        }
        
        public string TimeLeftDisplay
        {
            get
            {
                if (_timeLeftSpan.TotalHours >= 1)
                {
                    return $"{(int)_timeLeftSpan.TotalHours}h";
                }
                if (_timeLeftSpan.TotalMinutes >= 1)
                {
                    return $"{(int)_timeLeftSpan.TotalMinutes}m";
                }
                return $"{(int)_timeLeftSpan.TotalSeconds}s";
            }
        }
        
        public string DisplayTime => $"{Minutes}";
        
        public Reminder()
        {
            Id = "reminder-" + DateTime.Now.Ticks;
            Name = "New Reminder";
            Minutes = 5;
            EndTime = DateTime.Now.AddMinutes(Minutes);
            TimeLeft = "5:00";
            StartTimer();
        }
        
        public Reminder(string name, int minutes)
        {
            Id = "reminder-" + DateTime.Now.Ticks;
            Name = name;
            Minutes = minutes;
            EndTime = DateTime.Now.AddMinutes(minutes);
            StartTimer();
        }
        
        private void StartTimer()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            
            _timer.Tick += (s, e) => UpdateTimer();
            _timer.Start();
            
            // Initial update
            UpdateTimer();
        }
        
        private void UpdateTimer()
        {
            var now = DateTime.Now;
            var timeLeft = EndTime - now;
            
            if (timeLeft.TotalSeconds <= 0)
            {
                TimeLeft = "Done!";
                IsCompleted = true;
                _timer?.Stop();
                return;
            }
            
            int minutesLeft = (int)timeLeft.TotalMinutes;
            int secondsLeft = timeLeft.Seconds;
            
            TimeLeft = $"{minutesLeft}:{secondsLeft:D2}";
        }
        
        public void StopTimer()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }
        }
        
        public void UpdateTimeLeft()
        {
            var endTime = DateTime.Now.AddMinutes(Minutes);
            _timeLeftSpan = endTime - DateTime.Now;
            
            if (_timeLeftSpan.TotalSeconds <= 0)
            {
                TimeLeft = "Done!";
                IsCompleted = true;
                IsExpired = true;
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 