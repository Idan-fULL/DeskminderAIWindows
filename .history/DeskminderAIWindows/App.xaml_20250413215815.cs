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
            if (Properties.Settings.Default.StartWithWindows)
            {
                StartupManager.SetStartupWithWindows(true);
            }
        }
    }
} 