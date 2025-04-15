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

        public static bool ExportReminders(List<Reminder> reminders, string filePath)
        {
            try
            {
                if (reminders == null || !reminders.Any())
                {
                    return false;
                }

                // Create header row
                string header = "Name,Minutes,Created At,End Time,Status";
                
                // Create content with each reminder on a new line
                var lines = new List<string> { header };
                foreach (var reminder in reminders)
                {
                    lines.Add(reminder.ToExportString());
                }
                
                // Write all lines to the file
                File.WriteAllLines(filePath, lines);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting reminders: {ex.Message}");
                return false;
            }
        }
    }
} 