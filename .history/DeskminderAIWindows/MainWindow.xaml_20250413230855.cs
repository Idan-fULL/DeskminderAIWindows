using DeskminderAI.ViewModels;
using DeskminderAI.Models;
using DeskminderAI.Utilities;
using System;
using System.Windows;
using System.Windows.Input;
using WPFApplication = System.Windows.Application;
using System.Windows.Media;
// מגדיר את המשתנים באופן מפורש כדי למנוע התנגשויות
using WinPoint = System.Windows.Point;
using WinMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WinMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using WinForms = System.Windows.Forms;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows.Controls;

namespace DeskminderAI
{
    public partial class MainWindow : Window
    {
        private MainViewModel? _viewModel;
        private MainViewModel ViewModel 
        { 
            get
            {
                if (_viewModel == null)
                {
                    try
                    {
                        _viewModel = DataContext as MainViewModel;
                        if (_viewModel == null)
                        {
                            _viewModel = new MainViewModel();
                            DataContext = _viewModel;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"שגיאה באתחול המודל: {ex.Message}", "שגיאה", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        _viewModel = new MainViewModel();
                    }
                }
                return _viewModel;
            }
        }
        
        // Default values for widget position
        private const double DEFAULT_POSITION_X = 16;
        private const double DEFAULT_POSITION_Y = 300;
        
        // Variables for window state
        private bool _isDragging = false;
        private WinPoint _lastPosition;
        private DateTime _lastTimeSettingOpened = DateTime.MinValue;
        
        // Variables for drag duration interface
        private bool _isDraggingDuration = false;
        private double _initialDragPosition;
        private int _initialDragMinutes;
        private const int MIN_DURATION = 1;  // Minimum 1 minute
        private const int MAX_DURATION = 120; // Maximum 120 minutes (2 hours)
        private const double DRAG_SCALE_FACTOR = 4.0; // How many pixels per minute
        
        // VisualState names
        private const string STATE_ICON_ONLY = "IconOnlyState";
        private const string STATE_TIMER = "TimerState";
        private const string STATE_REMINDER = "ReminderState";
        private const string STATE_ACTIVE_REMINDERS = "ActiveRemindersState";
        
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                
                // Set the window position with default values
                Left = DEFAULT_POSITION_X;
                Top = DEFAULT_POSITION_Y;
                
                // Ensure the DataContext is set
                DataContext = new MainViewModel();
                
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
        
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Force the DataContext to be initialized
                var vm = ViewModel;
                
                // Reset to initial minutes value
                vm.NewReminderMinutes = 1;
                
                // Delay the initialization to allow the window to fully load
                await Task.Delay(300);
                
                // Make sure app icon is showing in taskbar
                if (TaskbarIcon != null)
                {
                    TaskbarIcon.Visibility = Visibility.Visible;
                }
                
                // Start with icon-only state
                ChangeVisualState(STATE_ICON_ONLY);
                ResizeWindowForState(STATE_ICON_ONLY);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"שגיאה בטעינת חלון: {ex.Message}", "שגיאה", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void Window_MouseDown(object sender, WinMouseButtonEventArgs e)
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
        
        protected override void OnMouseMove(WinMouseEventArgs e)
        {
            try
            {
                base.OnMouseMove(e);
                
                if (_isDragging)
                {
                    WinPoint currentPosition = e.GetPosition(this);
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
        
        protected override void OnMouseUp(WinMouseButtonEventArgs e)
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
        
        private void TaskbarIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            try
            {
                // Show window when clicking on tray icon
                Show();
                WindowState = WindowState.Normal;
                Activate();
                
                if (ViewModel.Reminders.Count > 0)
                {
                    // Show active reminders if there are any
                    ChangeVisualState(STATE_ACTIVE_REMINDERS);
                    ResizeWindowForState(STATE_ACTIVE_REMINDERS);
                }
                else
                {
                    // Otherwise show icon-only state
                    ChangeVisualState(STATE_ICON_ONLY);
                    ResizeWindowForState(STATE_ICON_ONLY);
                }
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
                var screen = WinForms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
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
        
        // Handler for the add button click
        private void AddButtonOnly_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check if we recently opened to prevent double-click issues
                if ((DateTime.Now - _lastTimeSettingOpened).TotalMilliseconds < 500)
                    return;
                
                // Set the last opened time
                _lastTimeSettingOpened = DateTime.Now;
                
                // Reset minutes to default
                ViewModel.NewReminderMinutes = 1;
                
                // Show timer panel
                ChangeVisualState(STATE_TIMER);
                ResizeWindowForState(STATE_TIMER);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"שגיאה בפתיחת תפריט תזכורת: {ex.Message}", "שגיאה", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // Timer drag interface methods
        private void TimerDragCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton == MouseButton.Left && TimerDragCanvas != null)
                {
                    _isDraggingDuration = true;
                    _initialDragPosition = e.GetPosition(TimerDragCanvas).X;
                    _initialDragMinutes = ViewModel.NewReminderMinutes;
                    TimerDragCanvas.CaptureMouse();
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in duration drag start: {ex.Message}");
                _isDraggingDuration = false;
                if (TimerDragCanvas != null && TimerDragCanvas.IsMouseCaptured)
                {
                    TimerDragCanvas.ReleaseMouseCapture();
                }
            }
        }
        
        private void TimerDragCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (_isDraggingDuration && TimerDragCanvas != null)
                {
                    double currentX = e.GetPosition(TimerDragCanvas).X;
                    double deltaX = currentX - _initialDragPosition;
                    
                    // Calculate new minutes based on drag distance
                    int newMinutes = _initialDragMinutes + (int)(deltaX / DRAG_SCALE_FACTOR);
                    
                    // Ensure the value is within bounds
                    newMinutes = Math.Max(MIN_DURATION, Math.Min(MAX_DURATION, newMinutes));
                    
                    // Update the view model
                    ViewModel.NewReminderMinutes = newMinutes;
                    
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in duration drag move: {ex.Message}");
                _isDraggingDuration = false;
                if (TimerDragCanvas != null && TimerDragCanvas.IsMouseCaptured)
                {
                    TimerDragCanvas.ReleaseMouseCapture();
                }
            }
        }
        
        private void TimerDragCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_isDraggingDuration && TimerDragCanvas != null)
                {
                    _isDraggingDuration = false;
                    TimerDragCanvas.ReleaseMouseCapture();
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in duration drag end: {ex.Message}");
                _isDraggingDuration = false;
                if (TimerDragCanvas != null && TimerDragCanvas.IsMouseCaptured)
                {
                    TimerDragCanvas.ReleaseMouseCapture();
                }
            }
        }
        
        // Visual state management
        private void ChangeVisualState(string stateName)
        {
            try
            {
                VisualStateManager.GoToState(this, stateName, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error changing visual state: {ex.Message}");
            }
        }
        
        private void ResizeWindowForState(string stateName)
        {
            try
            {
                switch (stateName)
                {
                    case STATE_ICON_ONLY:
                        Width = 50;
                        Height = 50;
                        break;
                    case STATE_TIMER:
                        Width = 170;
                        Height = 40;
                        break;
                    case STATE_REMINDER:
                        Width = 240;
                        Height = 40;
                        break;
                    case STATE_ACTIVE_REMINDERS:
                        Width = 170;
                        Height = 150;
                        break;
                }
                
                // Ensure window is visible on screen
                EnsureWindowVisibility();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resizing window: {ex.Message}");
            }
        }
        
        // Timer panel buttons
        private void CloseTimer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ChangeVisualState(STATE_ICON_ONLY);
                ResizeWindowForState(STATE_ICON_ONLY);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing timer panel: {ex.Message}");
            }
        }
        
        private void NextToReminder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Reset reminder name to empty
                ViewModel.NewReminderName = string.Empty;
                
                // Show reminder name panel
                ChangeVisualState(STATE_REMINDER);
                ResizeWindowForState(STATE_REMINDER);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error transitioning to reminder panel: {ex.Message}");
            }
        }
        
        // Reminder panel buttons
        private void BackToTimer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ChangeVisualState(STATE_TIMER);
                ResizeWindowForState(STATE_TIMER);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error returning to timer panel: {ex.Message}");
            }
        }
        
        private void AddReminder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var reminderName = ViewModel.NewReminderName;
                if (string.IsNullOrWhiteSpace(reminderName))
                {
                    reminderName = "Reminder";
                }
                
                // Create and add the reminder
                var reminder = new Reminder(reminderName, ViewModel.NewReminderMinutes);
                ViewModel.Reminders.Add(reminder);
                ViewModel.SelectedReminder = reminder;
                
                // Save reminders
                ViewModel.SaveReminders();
                
                // Show active reminders
                ChangeVisualState(STATE_ACTIVE_REMINDERS);
                ResizeWindowForState(STATE_ACTIVE_REMINDERS);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding reminder: {ex.Message}");
            }
        }
        
        // Active reminders panel
        private void RemoveReminderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ViewModel.SelectedReminder != null)
                {
                    ViewModel.Reminders.Remove(ViewModel.SelectedReminder);
                    
                    // If there are more reminders, select the first one
                    if (ViewModel.Reminders.Count > 0)
                    {
                        ViewModel.SelectedReminder = ViewModel.Reminders[0];
                    }
                    else
                    {
                        // No more reminders, return to icon-only mode
                        ChangeVisualState(STATE_ICON_ONLY);
                        ResizeWindowForState(STATE_ICON_ONLY);
                    }
                    
                    // Save reminders
                    ViewModel.SaveReminders();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing reminder: {ex.Message}");
            }
        }
    }
} 