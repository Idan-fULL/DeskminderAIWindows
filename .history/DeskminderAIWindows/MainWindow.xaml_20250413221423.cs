using DeskminderAI.ViewModels;
using DeskminderAI.Models;
using DeskminderAI.Utilities;
using System;
using System.Windows;
using System.Windows.Input;
using WPFApplication = System.Windows.Application;
using System.Windows.Media;

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
        private Point _lastPosition;
        private bool _isDragging = false;
        private DateTime _lastTimeSettingOpened = DateTime.MinValue;
        
        public MainWindow()
        {
            InitializeComponent();
            
            // Set the window position with default values
            Left = DEFAULT_POSITION_X;
            Top = DEFAULT_POSITION_Y;
            
            Topmost = Settings.Instance.AlwaysOnTop;
            
            // Set default window position if not already set
            if (Settings.Instance.WindowPositionX == 0 && Settings.Instance.WindowPositionY == 0)
            {
                Settings.Instance.WindowPositionX = SystemParameters.WorkArea.Width - 300;
                Settings.Instance.WindowPositionY = 100;
                Settings.Instance.Save();
            }
            
            Left = Settings.Instance.WindowPositionX;
            Top = Settings.Instance.WindowPositionY;
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set the window topmost property based on settings
            Topmost = true; // Default to always on top
            
            // If set to start minimized, start in icon-only mode
            SetIconOnlyMode(Settings.Instance.StartMinimized || _isIconOnlyMode);
            
            // Make sure app icon is showing in taskbar
            TaskbarIcon.Visibility = Visibility.Visible;
        }
        
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _isDragging = true;
                _lastPosition = e.GetPosition(this);
                CaptureMouse();
            }
        }
        
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            
            if (_isDragging)
            {
                Point currentPosition = e.GetPosition(this);
                Vector diff = currentPosition - _lastPosition;
                
                Left += diff.X;
                Top += diff.Y;
                
                // Keep the window within screen bounds
                if (Left < 0) Left = 0;
                if (Top < 0) Top = 0;
                if (Left + Width > SystemParameters.WorkArea.Width)
                    Left = SystemParameters.WorkArea.Width - Width;
                if (Top + Height > SystemParameters.WorkArea.Height)
                    Top = SystemParameters.WorkArea.Height - Height;
            }
        }
        
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            
            if (e.ChangedButton == MouseButton.Left)
            {
                _isDragging = false;
                ReleaseMouseCapture();
            }
        }
        
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide the window when clicking the minimize button
            SetIconOnlyMode(true);
        }
        
        private void TaskbarIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            // Show window when clicking on tray icon
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }
        
        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Show full UI when selecting Open from context menu
            SetIconOnlyMode(false);
            Show();
            WindowState = WindowState.Normal;
            Activate();
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
            
            // Save window position on closing
            Settings.Instance.WindowPositionX = Left;
            Settings.Instance.WindowPositionY = Top;
            Settings.Instance.Save();
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
                // Show only the icon
                AddButtonOnly.Visibility = Visibility.Visible;
                MainBorder.Visibility = Visibility.Collapsed;
                
                // Resize window to be just big enough for the icon
                Width = 50;
                Height = 50;
            }
            else
            {
                // Show the full UI
                AddButtonOnly.Visibility = Visibility.Collapsed;
                MainBorder.Visibility = Visibility.Visible;
                
                // Resize window to full size
                Width = 280;
                Height = 400;
                
                // Make sure we're active and on top
                Activate();
            }
        }
        
        // Handler for the floating add button click
        private void FloatingAddButton_Click(object sender, RoutedEventArgs e)
        {
            // Show time setting interface or switch to full UI
            OpenTimeSettingMenu();
        }
        
        private void OpenTimeSettingMenu()
        {
            // Check if we recently opened to prevent double-click issues
            if ((DateTime.Now - _lastTimeSettingOpened).TotalMilliseconds < 500)
                return;
                
            _lastTimeSettingOpened = DateTime.Now;
            
            // Toggle to full UI mode to show the add reminder interface
            SetIconOnlyMode(false);
            
            // Show the add reminder panel
            var viewModel = (ViewModels.MainViewModel)DataContext;
            viewModel.ShowAddReminderCommand.Execute(null);
        }
    }
} 