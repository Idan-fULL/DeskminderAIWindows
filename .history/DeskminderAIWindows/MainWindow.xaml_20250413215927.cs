using DeskminderAI.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;
using WPFApplication = System.Windows.Application;

namespace DeskminderAI
{
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel => (MainViewModel)DataContext;
        
        public MainWindow()
        {
            InitializeComponent();
            
            // Set the window position from saved settings
            Left = Properties.Settings.Default.WidgetPositionX;
            Top = Properties.Settings.Default.WidgetPositionY;
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set the window topmost property based on settings
            Topmost = ViewModel.AlwaysOnTop;
            
            // Check if the window should start minimized
            if (Properties.Settings.Default.StartMinimized)
            {
                Hide();
            }
            
            // Make sure app icon is showing in taskbar
            TaskbarIcon.Visibility = Visibility.Visible;
        }
        
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Allow dragging the window when clicking on it
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
                
                // Save the new position
                Properties.Settings.Default.WidgetPositionX = Left;
                Properties.Settings.Default.WidgetPositionY = Top;
                Properties.Settings.Default.Save();
            }
        }
        
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide the window when clicking the minimize button
            Hide();
        }
        
        private void TaskbarIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            // Show and activate the window when clicking on the taskbar icon
            Show();
            Activate();
            
            // Make sure we're visible (in case the window was moved off-screen)
            EnsureWindowVisibility();
        }
        
        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Show and activate the window when clicking the Open menu item
            Show();
            Activate();
            
            // Make sure we're visible (in case the window was moved off-screen)
            EnsureWindowVisibility();
        }
        
        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Close the application when clicking the Exit menu item
            WPFApplication.Current.Shutdown();
        }
        
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save reminders before closing - make this public in the ViewModel
            // ViewModel.SaveReminders();
            
            // Clean up the taskbar icon to prevent ghost icons
            TaskbarIcon.Dispose();
        }
        
        private void EnsureWindowVisibility()
        {
            // Make sure the window is within the screen bounds
            var screen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            var workingArea = screen.WorkingArea;
            
            // If window is off-screen, center it on the current screen
            if (Left < workingArea.Left || Left + Width > workingArea.Right ||
                Top < workingArea.Top || Top + Height > workingArea.Bottom)
            {
                Left = workingArea.Left + (workingArea.Width - Width) / 2;
                Top = workingArea.Top + (workingArea.Height - Height) / 2;
                
                // Save the corrected position
                Properties.Settings.Default.WidgetPositionX = Left;
                Properties.Settings.Default.WidgetPositionY = Top;
                Properties.Settings.Default.Save();
            }
        }
    }
} 