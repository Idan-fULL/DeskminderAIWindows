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
        private MainViewModel ViewModel => DataContext as MainViewModel ?? new MainViewModel();
        
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
            try
            {
                InitializeComponent();
                
                // Set the window position with default values
                Left = DEFAULT_POSITION_X;
                Top = DEFAULT_POSITION_Y;
                
                try
                {
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
                catch (Exception ex)
                {
                    MessageBox.Show($"שגיאה בטעינת הגדרות: {ex.Message}", "שגיאה", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"שגיאה באתחול חלון ראשי: {ex.Message}", "שגיאה", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Set the window topmost property based on settings
                Topmost = true; // Default to always on top
                
                // If set to start minimized, start in icon-only mode
                SetIconOnlyMode(Settings.Instance.StartMinimized || _isIconOnlyMode);
                
                // Make sure app icon is showing in taskbar
                if (TaskbarIcon != null)
                {
                    TaskbarIcon.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"שגיאה בטעינת חלון: {ex.Message}", "שגיאה", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    _isDragging = true;
                    _lastPosition = e.GetPosition(this);
                    CaptureMouse();
                }
            }
            catch (Exception ex)
            {
                // Quietly log the error but don't bother the user
                Console.WriteLine($"Error in mouse down: {ex.Message}");
                _isDragging = false;
            }
        }
        
        protected override void OnMouseMove(MouseEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                // Quietly log the error but don't bother the user
                Console.WriteLine($"Error in mouse move: {ex.Message}");
                _isDragging = false;
            }
        }
        
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            try
            {
                base.OnMouseUp(e);
                
                if (e.ChangedButton == MouseButton.Left)
                {
                    _isDragging = false;
                    ReleaseMouseCapture();
                }
            }
            catch (Exception ex)
            {
                // Quietly log the error but don't bother the user
                Console.WriteLine($"Error in mouse up: {ex.Message}");
                _isDragging = false;
            }
        }
        
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Hide the window when clicking the minimize button
                SetIconOnlyMode(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"שגיאה במזעור חלון: {ex.Message}", "שגיאה", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void TaskbarIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            try
            {
                // Show window when clicking on tray icon
                Show();
                WindowState = WindowState.Normal;
                Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"שגיאה בפתיחת חלון: {ex.Message}", "שגיאה", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Show full UI when selecting Open from context menu
                SetIconOnlyMode(false);
                Show();
                WindowState = WindowState.Normal;
                Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"שגיאה בפתיחת חלון: {ex.Message}", "שגיאה", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Close the application when clicking the Exit menu item
                WPFApplication.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"שגיאה בסגירת אפליקציה: {ex.Message}", "שגיאה", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                // Force exit if we can't exit gracefully
                Environment.Exit(1);
            }
        }
        
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // Save reminders before closing
                if (ViewModel != null)
                {
                    ViewModel.SaveReminders();
                }
                
                // Clean up the taskbar icon to prevent ghost icons
                if (TaskbarIcon != null)
                {
                    TaskbarIcon.Dispose();
                }
                
                // Save window position on closing
                try
                {
                    Settings.Instance.WindowPositionX = Left;
                    Settings.Instance.WindowPositionY = Top;
                    Settings.Instance.Save();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving window position: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"שגיאה בסגירת חלון: {ex.Message}", "שגיאה", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void EnsureWindowVisibility()
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error ensuring window visibility: {ex.Message}");
                // Use fallback values for center of screen
                Left = SystemParameters.WorkArea.Width / 2 - Width / 2;
                Top = SystemParameters.WorkArea.Height / 2 - Height / 2;
            }
        }
        
        // Toggle between showing just the app icon or the full UI
        private void SetIconOnlyMode(bool iconOnly)
        {
            try
            {
                _isIconOnlyMode = iconOnly;
                
                if (iconOnly)
                {
                    // Show only the icon
                    if (AddButtonOnly != null) AddButtonOnly.Visibility = Visibility.Visible;
                    if (MainBorder != null) MainBorder.Visibility = Visibility.Collapsed;
                    
                    // Resize window to be just big enough for the icon
                    Width = 50;
                    Height = 50;
                }
                else
                {
                    // Show the full UI
                    if (AddButtonOnly != null) AddButtonOnly.Visibility = Visibility.Collapsed;
                    if (MainBorder != null) MainBorder.Visibility = Visibility.Visible;
                    
                    // Resize window to full size
                    Width = 280;
                    Height = 400;
                    
                    // Make sure we're active and on top
                    Activate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"שגיאה בשינוי מצב חלון: {ex.Message}", "שגיאה", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // Handler for the floating add button click
        private void FloatingAddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Show time setting interface or switch to full UI
                OpenTimeSettingMenu();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"שגיאה בהוספת תזכורת: {ex.Message}", "שגיאה", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void OpenTimeSettingMenu()
        {
            try
            {
                // Check if we recently opened to prevent double-click issues
                if ((DateTime.Now - _lastTimeSettingOpened).TotalMilliseconds < 500)
                    return;
                    
                _lastTimeSettingOpened = DateTime.Now;
                
                // Toggle to full UI mode to show the add reminder interface
                SetIconOnlyMode(false);
                
                // Show the add reminder panel
                var viewModel = DataContext as ViewModels.MainViewModel;
                if (viewModel != null)
                {
                    viewModel.ShowAddReminderCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"שגיאה בפתיחת תפריט תזכורות: {ex.Message}", "שגיאה", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 