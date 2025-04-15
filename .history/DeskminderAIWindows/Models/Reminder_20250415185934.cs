using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace DeskminderAI.Models
{
    public class Reminder : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private int _minutes;
        private DateTime _createdAt;
        private TimeSpan _timeLeft;

        [JsonPropertyName("id")]
        public Guid Id { get; private set; } = Guid.NewGuid();

        [JsonPropertyName("name")]
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

        [JsonPropertyName("minutes")]
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

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt
        {
            get => _createdAt;
            set
            {
                if (_createdAt != value)
                {
                    _createdAt = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonPropertyName("timeLeft")]
        public TimeSpan TimeLeft
        {
            get => _timeLeft;
            set
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

        [JsonIgnore]
        public bool IsExpired
        {
            get => TimeLeft.TotalSeconds <= 0;
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

        public Reminder(string name, int minutes)
        {
            Id = Guid.NewGuid();
            Name = name;
            Minutes = minutes;
            CreatedAt = DateTime.Now;
            TimeLeft = TimeSpan.FromMinutes(minutes);
            UpdateTimeLeft();
        }

        [JsonConstructor]
        public Reminder()
        {
            Id = Guid.NewGuid();
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