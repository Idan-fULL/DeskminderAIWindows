using DeskminderAI.Utilities;
using System.Windows;
using WPFApplication = System.Windows.Application;
using System.Diagnostics;
using System;

namespace DeskminderAI
{
    public partial class App : WPFApplication
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try 
            {
                base.OnStartup(e);

                // Ensure app starts with Windows if configured
                if (Settings.Instance.StartWithWindows)
                {
                    StartupManager.SetStartupWithWindows(true);
                }

                // Create the main window
                MainWindow mainWindow = new MainWindow();
            
                // Show the window directly in a visible state
                mainWindow.Show();
                mainWindow.Activate();
                mainWindow.Focus();

                // Make sure the window is properly displayed
                mainWindow.EnsureWindowVisibility();
                
                // Initialize with the timer panel visible
                mainWindow.ShowTimerPanel();
                
                // Set as main window
                Current.MainWindow = mainWindow;
                
                Console.WriteLine("Application started with main window visible");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting application: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 