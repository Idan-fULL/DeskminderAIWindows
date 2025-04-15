using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

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

        [JsonProperty("id")]
        public Guid Id { get; } = Guid.NewGuid();

        [JsonProperty("name")]
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

        [JsonProperty("minutes")]
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

        [JsonProperty("createdAt")]
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

        [JsonProperty("endTime")]
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

        [JsonProperty("timeLeft")]
        public TimeSpan TimeLeft
        {
            get => _timeLeft;
            set
            {
                if (_timeLeft != value)
                {
                    _timeLeft = value;
                    OnPropertyChanged();
                    IsExpired = _timeLeft.TotalSeconds <= 0;
                }
            }
        }

        [JsonProperty("isExpired")]
        public bool IsExpired
        {
            get => _isExpired;
            set
            {
                if (_isExpired != value)
                {
                    _isExpired = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
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

        [JsonIgnore]
        public string DisplayTime => $"{Minutes}";

        [JsonConstructor]
        public Reminder()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.Now;
            EndTime = CreatedAt;
            TimeLeft = TimeSpan.Zero;
            IsExpired = true;
        }

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