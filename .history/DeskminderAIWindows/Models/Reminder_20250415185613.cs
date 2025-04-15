using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace DeskminderAI.Models
{
    public class Reminder : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private int _minutes;
        private DateTime _createdAt;
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
                }
            }
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            private set
            {
                if (_createdAt != value)
                {
                    _createdAt = value;
                    OnPropertyChanged();
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
                    OnPropertyChanged(nameof(TimeLeftDisplay));
                    OnPropertyChanged(nameof(IsExpired));
                }
            }
        }

        public bool IsExpired => TimeLeft.TotalSeconds <= 0;

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
            _name = name;
            _minutes = minutes;
            _createdAt = DateTime.Now;
            _timeLeft = TimeSpan.FromMinutes(minutes);
            UpdateTimeLeft();
        }

        public void UpdateTimeLeft()
        {
            var endTime = CreatedAt.AddMinutes(Minutes);
            TimeLeft = endTime - DateTime.Now;
            
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