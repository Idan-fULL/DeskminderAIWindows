using System.Windows;

namespace DeskminderAI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Ensure app starts with Windows if configured
            if (Properties.Settings.Default.StartWithWindows)
            {
                StartupManager.SetStartupWithWindows(true);
            }
        }
    }
} 