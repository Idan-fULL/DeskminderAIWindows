using DeskminderAI.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace DeskminderAI.Services
{
    public class ReminderService
    {
        private static readonly string DataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DeskminderAI");
            
        private static readonly string RemindersFile = Path.Combine(DataFolder, "reminders.json");
        
        public ObservableCollection<Reminder> LoadReminders()
        {
            try
            {
                // Ensure the data directory exists
                if (!Directory.Exists(DataFolder))
                {
                    Directory.CreateDirectory(DataFolder);
                }
                
                // Create new collection if file doesn't exist
                if (!File.Exists(RemindersFile))
                {
                    return new ObservableCollection<Reminder>();
                }
                
                // Read and deserialize the file
                string json = File.ReadAllText(RemindersFile);
                var savedReminders = JsonConvert.DeserializeObject<List<ReminderData>>(json);
                
                if (savedReminders == null)
                {
                    return new ObservableCollection<Reminder>();
                }
                
                // Filter out expired reminders
                var activeReminders = savedReminders
                    .Where(r => DateTime.Parse(r.EndTime) > DateTime.Now)
                    .Select(r => new Reminder
                    {
                        Id = r.Id,
                        Name = r.Name,
                        EndTime = DateTime.Parse(r.EndTime),
                        Minutes = r.Minutes
                    })
                    .ToList();
                
                return new ObservableCollection<Reminder>(activeReminders);
            }
            catch (Exception)
            {
                return new ObservableCollection<Reminder>();
            }
        }
        
        public void SaveReminders(IEnumerable<Reminder> reminders)
        {
            try
            {
                // Ensure the data directory exists
                if (!Directory.Exists(DataFolder))
                {
                    Directory.CreateDirectory(DataFolder);
                }
                
                var reminderData = reminders.Select(r => new ReminderData
                {
                    Id = r.Id,
                    Name = r.Name,
                    EndTime = r.EndTime.ToString("o"),
                    Minutes = r.Minutes
                }).ToList();
                
                string json = JsonConvert.SerializeObject(reminderData, Formatting.Indented);
                File.WriteAllText(RemindersFile, json);
            }
            catch (Exception)
            {
                // Handle or log the error as needed
            }
        }
        
        private class ReminderData
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public string EndTime { get; set; } = "";
            public int Minutes { get; set; }
        }
    }
} 