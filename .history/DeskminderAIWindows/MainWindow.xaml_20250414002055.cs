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
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Windows.Media.Imaging;
// Use explicit types to avoid ambiguity
using WinSize = System.Windows.Size;
using WinRect = System.Windows.Rect;
using WinColor = System.Windows.Media.Color;
using WinColors = System.Windows.Media.Colors;

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
                        Console.WriteLine("Initializing ViewModel");
                        _viewModel = DataContext as MainViewModel;
                        if (_viewModel == null)
                        {
                            Console.WriteLine("Creating new ViewModel");
                            _viewModel = new MainViewModel();
                            DataContext = _viewModel;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error initializing ViewModel: {ex.Message}");
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
                Console.WriteLine("Initializing MainWindow");
                InitializeComponent();
                
                // Create a simple icon programmatically
                CreateAndSetTaskbarIcon();
                
                // Set the window position with default values
                Left = DEFAULT_POSITION_X;
                Top = DEFAULT_POSITION_Y;
                
                // Ensure the DataContext is set
                DataContext = new MainViewModel();
                Console.WriteLine("DataContext set to new MainViewModel");
                
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
                    Console.WriteLine($"Window position set to {Left}, {Top}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading settings: {ex.Message}");
                    MessageBox.Show($"שגיאה בטעינת הגדרות: {ex.Message}", "שגיאה", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MainWindow constructor: {ex.Message}");
                MessageBox.Show($"שגיאה באתחול חלון ראשי: {ex.Message}", "שגיאה", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void CreateAndSetTaskbarIcon()
        {
            try
            {
                // Create a simple icon using a Canvas and shapes that TaskbarIcon can use
                var canvas = new Canvas { Width = 32, Height = 32 };
                
                // Create a green sparkle-like icon
                System.Windows.Shapes.Path sparkleCenter = new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse("M12,3 L13.5,8.5 L19,10 L13.5,11.5 L12,17 L10.5,11.5 L5,10 L10.5,8.5 Z"),
                    Fill = new SolidColorBrush(WinColors.White),
                    Width = 24,
                    Height = 24
                };
                
                canvas.Children.Add(new System.Windows.Shapes.Ellipse
                {
                    Width = 32,
                    Height = 32,
                    Fill = new LinearGradientBrush(WinColors.Green, WinColors.DarkGreen, new Point(0, 0), new Point(1, 1))
                });
                
                Canvas.SetLeft(sparkleCenter, 4);
                Canvas.SetTop(sparkleCenter, 4);
                canvas.Children.Add(sparkleCenter);
                
                // Measure and arrange the canvas
                canvas.Measure(new WinSize(32, 32));
                canvas.Arrange(new WinRect(0, 0, 32, 32));
                
                // Create a RenderTargetBitmap from the canvas
                var renderBitmap = new RenderTargetBitmap(32, 32, 96, 96, PixelFormats.Pbgra32);
                renderBitmap.Render(canvas);
                
                // Set the TaskbarIcon's IconSource
                if (TaskbarIcon != null)
                {
                    TaskbarIcon.IconSource = renderBitmap;
                    Console.WriteLine("Taskbar icon set programmatically");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating taskbar icon: {ex.Message}");
            }
        }
        
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("Window_Loaded event triggered");
                
                // Force the DataContext to be initialized
                var vm = ViewModel;
                
                // Set initial minutes value
                vm.NewReminderMinutes = 1;
                
                // Delay the initialization to allow the window to fully load
                await Task.Delay(300);
                
                // Make sure app icon is showing in taskbar
                if (TaskbarIcon != null)
                {
                    TaskbarIcon.Visibility = Visibility.Visible;
                    Console.WriteLine("TaskbarIcon visibility set to Visible");
                }
                
                // Start with icon-only state
                ChangeVisualState(STATE_ICON_ONLY);
                
                Console.WriteLine($"Window initialized with size {Width}x{Height}");
                
                // Load saved reminders
                LoadExistingReminders();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Window_Loaded: {ex.Message}");
                MessageBox.Show($"שגיאה בטעינת חלון: {ex.Message}", "שגיאה", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void LoadExistingReminders()
        {
            // Reminders are already loaded through the ViewModel
            // Just make sure the UI is updated
            if (ViewModel.Reminders.Count > 0)
            {
                Console.WriteLine($"Loaded {ViewModel.Reminders.Count} existing reminders");
            }
        }
        
        private void Window_MouseDown(object sender, WinMouseButtonEventArgs e)
        {
            try
            {
                // Only start dragging if not clicking on a button
                if (e.ChangedButton == MouseButton.Left && 
                    !(e.OriginalSource is Button) && 
                    !(e.OriginalSource is TextBlock && ((TextBlock)e.OriginalSource).IsDescendantOf(AddButtonOnly)))
                {
                    _isDragging = true;
                    _lastPosition = e.GetPosition(this);
                    CaptureMouse();
                    Console.WriteLine("Window dragging started");
                }
            }
            catch (Exception ex)
            {
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
                Console.WriteLine($"Error in mouse move: {ex.Message}");
                _isDragging = false;
            }
        }
        
        protected override void OnMouseUp(WinMouseButtonEventArgs e)
        {
            try
            {
                base.OnMouseUp(e);
                
                if (e.ChangedButton == MouseButton.Left && _isDragging)
                {
                    _isDragging = false;
                    ReleaseMouseCapture();
                    Console.WriteLine("Window dragging ended");
                    
                    // Save the widget position
                    Settings.Instance.WindowPositionX = Left;
                    Settings.Instance.WindowPositionY = Top;
                    Settings.Instance.Save();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in mouse up: {ex.Message}");
                _isDragging = false;
            }
        }
        
        private void TaskbarIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("TaskbarIcon clicked");
                
                // Ensure the window is visible and properly positioned
                EnsureWindowVisibility();
                
                Console.WriteLine($"Window state: Visibility={Visibility}, State={WindowState}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling tray icon click: {ex.Message}");
                MessageBox.Show($"שגיאה בטיפול בלחיצה על סמל המגש: {ex.Message}", "שגיאה", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("ExitMenuItem_Click event triggered");
                // Close the application when clicking the Exit menu item
                WPFApplication.Current.Shutdown();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExitMenuItem_Click: {ex.Message}");
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
                Console.WriteLine("Window_Closing event triggered");
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
                Console.WriteLine($"Error in Window_Closing: {ex.Message}");
                MessageBox.Show($"שגיאה בסגירת חלון: {ex.Message}", "שגיאה", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        public void EnsureWindowVisibility()
        {
            try
            {
                Console.WriteLine("Ensuring window visibility");
                
                // Make window visible regardless of settings
                this.Visibility = Visibility.Visible;
                this.WindowState = WindowState.Normal;
                this.ShowInTaskbar = false; // Keep it out of the taskbar
                
                // Check if window is off-screen
                bool isOffScreen = 
                    Left <= -Width || 
                    Left >= SystemParameters.VirtualScreenWidth || 
                    Top <= -Height || 
                    Top >= SystemParameters.VirtualScreenHeight;
                
                Console.WriteLine($"Window position: Left={Left}, Top={Top}, isOffScreen={isOffScreen}");
                
                if (isOffScreen)
                {
                    // Reset to default position if window is off-screen
                    Left = DEFAULT_POSITION_X;
                    Top = DEFAULT_POSITION_Y;
                    Console.WriteLine($"Window repositioned to {Left}, {Top}");
                    
                    // Save the new position
                    Settings.Instance.WindowPositionX = Left;
                    Settings.Instance.WindowPositionY = Top;
                    Settings.Instance.Save();
                }
                
                // Always bring to front
                Activate();
                Topmost = true;
                Topmost = Settings.Instance.AlwaysOnTop;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ensuring window visibility: {ex.Message}");
            }
        }
        
        // Handler for the add button click
        private void AddButtonOnly_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("AddButtonOnly_Click event triggered");
                
                // Check if we recently opened to prevent double-click issues
                if ((DateTime.Now - _lastTimeSettingOpened).TotalMilliseconds < 500)
                {
                    Console.WriteLine("Ignoring click - too soon after previous");
                    return;
                }
                
                // Set the last opened time
                _lastTimeSettingOpened = DateTime.Now;
                
                // Show timer panel
                ChangeVisualState(STATE_TIMER);
                
                Console.WriteLine("Changed to timer state");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddButtonOnly_Click: {ex.Message}");
                MessageBox.Show($"שגיאה בפתיחת תפריט תזכורת: {ex.Message}", "שגיאה", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // Timer drag interface methods
        private void TimerDragCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Console.WriteLine("TimerDragCanvas_MouseDown event triggered");
                
                if (e.ChangedButton == MouseButton.Left && TimerDragCanvas != null)
                {
                    _isDraggingDuration = true;
                    _initialDragPosition = e.GetPosition(TimerDragCanvas).X;
                    _initialDragMinutes = ViewModel.NewReminderMinutes;
                    TimerDragCanvas.CaptureMouse();
                    e.Handled = true;
                    
                    Console.WriteLine($"Duration drag started at position {_initialDragPosition}, minutes: {_initialDragMinutes}");
                    
                    // Add visual feedback
                    TimerDragCanvas.Background = new SolidColorBrush(WinColor.FromRgb(100, 175, 80));
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
                Console.WriteLine("TimerDragCanvas_MouseUp event triggered");
                
                if (_isDraggingDuration && TimerDragCanvas != null)
                {
                    _isDraggingDuration = false;
                    TimerDragCanvas.ReleaseMouseCapture();
                    e.Handled = true;
                    
                    // Reset visual feedback
                    TimerDragCanvas.Background = new SolidColorBrush(WinColor.FromRgb(51, 51, 51));
                    
                    Console.WriteLine($"Duration drag ended, final minutes: {ViewModel.NewReminderMinutes}");
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
        
        // Handler for clicking on the compact timer display
        private void TimerDisplay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Console.WriteLine("TimerDisplay_MouseDown event triggered");
                
                if (e.ChangedButton == MouseButton.Left)
                {
                    // Show timer panel
                    ChangeVisualState(STATE_TIMER);
                    
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in TimerDisplay_MouseDown: {ex.Message}");
            }
        }
        
        // Visual state management
        private void ChangeVisualState(string stateName)
        {
            try
            {
                Console.WriteLine($"Changing visual state to: {stateName}");
                
                // Try the standard VisualStateManager approach first
                bool result = VisualStateManager.GoToState(this, stateName, true);
                Console.WriteLine($"VisualStateManager.GoToState result: {result}");
                
                // Set visibility directly based on the state
                // Reset all panels
                if (TimerDisplay != null) TimerDisplay.Visibility = Visibility.Collapsed;
                if (AddButtonOnly != null) AddButtonOnly.Visibility = Visibility.Collapsed;
                if (TimerPanel != null) TimerPanel.Visibility = Visibility.Collapsed;
                if (ActiveRemindersPanel != null) ActiveRemindersPanel.Visibility = Visibility.Visible;
                
                // Set the appropriate panel to visible
                switch (stateName)
                {
                    case STATE_ICON_ONLY:
                        if (TimerDisplay != null) TimerDisplay.Visibility = Visibility.Visible;
                        if (AddButtonOnly != null) AddButtonOnly.Visibility = Visibility.Visible;
                        Width = 180;  // Wider to accommodate reminders
                        Height = 300; // Taller to accommodate reminders
                        break;
                    case STATE_TIMER:
                        if (TimerPanel != null) TimerPanel.Visibility = Visibility.Visible;
                        Width = 190;  // Wider for the timer panel
                        Height = 300; // Taller to accommodate reminders
                        break;
                }
                
                // Set background color based on state
                Background = new SolidColorBrush(WinColor.FromArgb(1, 0, 0, 0)); // Transparent
                
                // Debug UI elements visibility
                if (TimerDisplay != null) Console.WriteLine($"TimerDisplay visibility: {TimerDisplay.Visibility}");
                if (AddButtonOnly != null) Console.WriteLine($"AddButtonOnly visibility: {AddButtonOnly.Visibility}");
                if (TimerPanel != null) Console.WriteLine($"TimerPanel visibility: {TimerPanel.Visibility}");
                
                // Force layout update
                UpdateLayout();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error changing visual state: {ex.Message}");
            }
        }
        
        // Timer panel buttons
        private void CloseTimer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("CloseTimer_Click event triggered");
                ChangeVisualState(STATE_ICON_ONLY);
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
                Console.WriteLine("NextToReminder_Click event triggered");
                // Reset reminder name to empty
                ViewModel.NewReminderName = string.Empty;
                
                // Show reminder name panel
                ChangeVisualState(STATE_REMINDER);
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
                Console.WriteLine("BackToTimer_Click event triggered");
                ChangeVisualState(STATE_TIMER);
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
                Console.WriteLine("AddReminder_Click event triggered");
                
                // Use the current minutes with a generic reminder name
                string reminderName = $"Reminder ({ViewModel.NewReminderMinutes} min)";
                
                Console.WriteLine($"Creating reminder: {reminderName}, minutes: {ViewModel.NewReminderMinutes}");
                
                // Create and add the reminder
                var reminder = new Reminder(reminderName, ViewModel.NewReminderMinutes);
                ViewModel.Reminders.Add(reminder);
                
                // Save reminders
                ViewModel.SaveReminders();
                
                // Go back to icon-only state
                ChangeVisualState(STATE_ICON_ONLY);
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
                Console.WriteLine("RemoveReminderButton_Click event triggered");
                
                // Get the reminder ID from the button's Tag
                if (sender is Button button && button.Tag is string reminderIdStr)
                {
                    // Parse the string ID to a Guid
                    if (Guid.TryParse(reminderIdStr, out Guid reminderId))
                    {
                        // Find and remove the reminder
                        var reminderToRemove = ViewModel.Reminders.FirstOrDefault(r => r.Id == reminderId);
                        if (reminderToRemove != null)
                        {
                            Console.WriteLine($"Removing reminder: {reminderToRemove.Name}");
                            
                            // Stop the timer to avoid memory leaks
                            reminderToRemove.StopTimer();
                            
                            // Remove from collection
                            ViewModel.Reminders.Remove(reminderToRemove);
                            
                            // Save reminders
                            ViewModel.SaveReminders();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing reminder: {ex.Message}");
            }
        }
        
        // Method to show the timer panel directly
        public void ShowTimerPanel()
        {
            try
            {
                Console.WriteLine("ShowTimerPanel method called");
                
                // Ensure the window is visible
                EnsureWindowVisibility();
                
                // Switch to timer state
                ChangeVisualState(STATE_TIMER);
                
                // Force re-render
                UpdateLayout();
                
                Console.WriteLine("Timer panel should now be visible");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing timer panel: {ex.Message}");
            }
        }
    }
    
    // Extension method to check if an element is a descendant of another
    public static class UIElementExtensions
    {
        public static bool IsDescendantOf(this FrameworkElement element, DependencyObject parent)
        {
            DependencyObject current = element;
            
            while (current != null)
            {
                if (current == parent)
                    return true;
                
                // Traverse up the visual tree
                current = VisualTreeHelper.GetParent(current);
            }
            
            return false;
        }
    }
} 