using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DeskminderAI.Models
{
    public class Reminder : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private int _minutes;
        private DateTime _createdAt;
        private DateTime _endTime;
        private TimeSpan _timeLeft;
        private bool _isExpired;

        public Guid Id { get; } = Guid.NewGuid();

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Minutes
        {
            get => _minutes;
            set
            {
                if (_minutes != value)
                {
                    _minutes = value;
                    OnPropertyChanged();
                    UpdateEndTime();
                }
            }
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set
            {
                if (_createdAt != value)
                {
                    _createdAt = value;
                    OnPropertyChanged();
                    UpdateEndTime();
                }
            }
        }

        public DateTime EndTime
        {
            get => _endTime;
            set
            {
                if (_endTime != value)
                {
                    _endTime = value;
                    OnPropertyChanged();
                    UpdateTimeLeft();
                }
            }
        }

        public TimeSpan TimeLeft
        {
            get => _timeLeft;
            private set
            {
                if (_timeLeft != value)
                {
                    _timeLeft = value;
                    OnPropertyChanged();
                    IsExpired = _timeLeft.TotalSeconds <= 0;
                }
            }
        }

        public bool IsExpired
        {
            get => _isExpired;
            private set
            {
                if (_isExpired != value)
                {
                    _isExpired = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TimeLeftDisplay
        {
            get
            {
                if (TimeLeft.TotalHours >= 1)
                {
                    return $"{(int)TimeLeft.TotalHours}h";
                }
                if (TimeLeft.TotalMinutes >= 1)
                {
                    return $"{(int)TimeLeft.TotalMinutes}m";
                }
                return $"{(int)TimeLeft.TotalSeconds}s";
            }
        }

        public string DisplayTime => $"{Minutes}";

        public Reminder(string name, int minutes)
        {
            Name = name;
            Minutes = minutes;
            CreatedAt = DateTime.Now;
            UpdateEndTime();
            UpdateTimeLeft();
        }

        private void UpdateEndTime()
        {
            EndTime = CreatedAt.AddMinutes(Minutes);
        }

        public void UpdateTimeLeft()
        {
            TimeLeft = EndTime - DateTime.Now;
            
            if (TimeLeft.TotalSeconds <= 0)
            {
                TimeLeft = TimeSpan.Zero;
            }
        }

        public void StopTimer()
        {
            TimeLeft = TimeSpan.Zero;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 