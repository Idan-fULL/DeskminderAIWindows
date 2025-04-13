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
        
        // Default values for widget position
        private const double DEFAULT_POSITION_X = 16;
        private const double DEFAULT_POSITION_Y = 300;
        
        // Flag to indicate if we're showing just the icon or the full UI
        private bool _isIconOnlyMode = true;
        
        public MainWindow()
        {
            InitializeComponent();
            
            // Set the window position with default values
            Left = DEFAULT_POSITION_X;
            Top = DEFAULT_POSITION_Y;
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set the window topmost property based on settings
            Topmost = true; // Default to always on top
            
            // Switch to icon-only mode on startup
            SetIconOnlyMode(true);
            
            // Make sure app icon is showing in taskbar
            TaskbarIcon.Visibility = Visibility.Visible;
        }
        
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Allow dragging the window when clicking on it
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }
        
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide the window when clicking the minimize button
            SetIconOnlyMode(true);
        }
        
        private void TaskbarIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            // Toggle between icon-only mode and full UI
            SetIconOnlyMode(false);
            
            // Make sure we're visible (in case the window was moved off-screen)
            EnsureWindowVisibility();
        }
        
        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Show full UI when clicking the Open menu item
            SetIconOnlyMode(false);
            
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
            // Save reminders before closing
            ViewModel.SaveReminders();
            
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
            }
        }
        
        // Toggle between showing just the app icon or the full UI
        private void SetIconOnlyMode(bool iconOnly)
        {
            _isIconOnlyMode = iconOnly;
            
            if (iconOnly)
            {
                // Show only the add button, hide everything else
                MainBorder.Visibility = Visibility.Collapsed;
                AddButtonOnly.Visibility = Visibility.Visible;
                Width = 50;
                Height = 50;
            }
            else
            {
                // Show the full UI
                MainBorder.Visibility = Visibility.Visible;
                AddButtonOnly.Visibility = Visibility.Collapsed;
                Width = 280;
                Height = 400;
                
                // Make sure we're active and on top
                Activate();
            }
        }
        
        // Handler for the floating add button click
        private void FloatingAddButton_Click(object sender, RoutedEventArgs e)
        {
            // When clicking the floating add button, show the time selector directly
            ViewModel.ShowAddReminder();
            
            // And show the full UI
            SetIconOnlyMode(false);
        }
    }
} 