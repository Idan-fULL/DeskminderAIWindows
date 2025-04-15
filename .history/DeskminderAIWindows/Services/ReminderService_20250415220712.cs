using DeskminderAI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

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
                var reminders = JsonConvert.DeserializeObject<List<Reminder>>(json);
                
                if (reminders == null)
                {
                    return new ObservableCollection<Reminder>();
                }
                
                // Filter out expired reminders and update their time left
                var activeReminders = reminders
                    .Where(r => !r.IsExpired)
                    .ToList();
                
                foreach (var reminder in activeReminders)
                {
                    reminder.UpdateTimeLeft();
                }
                
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
                
                string json = JsonConvert.SerializeObject(reminders, Formatting.Indented);
                File.WriteAllText(RemindersFile, json);
            }
            catch (Exception)
            {
                // Handle or log the error as needed
            }
        }
    }
} 