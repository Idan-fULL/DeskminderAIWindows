using DeskminderAI.Utilities;
using System.Windows;
using WPFApplication = System.Windows.Application;

namespace DeskminderAI
{
    public partial class App : WPFApplication
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Ensure app starts with Windows if configured
            if (Settings.Instance.StartWithWindows)
            {
                StartupManager.SetStartupWithWindows(true);
            }
            
            // Create and configure the main window
            MainWindow mainWindow = new MainWindow();
            
            // Show the window properly based on settings
            if (Settings.Instance.StartMinimized)
            {
                // Don't show the main window if we're starting minimized
                // The window will be accessible via the system tray icon
                mainWindow.WindowState = WindowState.Minimized;
                mainWindow.ShowInTaskbar = false;
            }
            else
            {
                // Show the window normally
                mainWindow.Show();
            }
            
            // Set this as the main window
            Current.MainWindow = mainWindow;
        }
    }
} 