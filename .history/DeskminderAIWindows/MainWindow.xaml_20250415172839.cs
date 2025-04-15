using DeskminderAI.ViewModels;
using DeskminderAI.Models;
using DeskminderAI.Utilities;
using System;
using System.Windows;
using System.Windows.Input;
using WPFApplication = System.Windows.Application;
using System.Windows.Media;
using System.Windows.Media.Effects;
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
        private const double DRAG_SCALE_FACTOR = 0.8; // How many pixels per minute
        
        // VisualState names - still used for logging but not for actual state management anymore
        private const string STATE_ICON_ONLY = "IconOnlyState";
        private const string STATE_TIMER = "TimerState";
        private const string STATE_ACTIVE_REMINDERS = "ActiveRemindersState";
        private const string STATE_REMINDER_TEXT = "ReminderTextState";
        
        // Store original window state for restoration
        private double _originalWidth;
        private double _originalHeight;
        private double _originalLeft;
        private double _originalTop;
        private WindowState _originalWindowState;
        private bool _isFullScreenMode = false;
        
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
                // Create a canvas for the icon
                var canvas = new Canvas { Width = 32, Height = 32 };
                
                // Create a green circular background
                canvas.Children.Add(new System.Windows.Shapes.Ellipse
                {
                    Width = 32,
                    Height = 32,
                    Fill = new SolidColorBrush(WinColor.FromRgb(76, 175, 80))  // Green color #4CAF50
                });
                
                // Create two small stars
                var star1 = new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse("M8,8 L9,11 L12,12 L9,13 L8,16 L7,13 L4,12 L7,11 Z"), // Small star
                    Fill = new SolidColorBrush(Colors.White),
                    StrokeThickness = 0
                };
                
                var star2 = new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse("M18,14 L19,17 L22,18 L19,19 L18,22 L17,19 L14,18 L17,17 Z"), // Small star
                    Fill = new SolidColorBrush(Colors.White),
                    StrokeThickness = 0
                };
                
                canvas.Children.Add(star1);
                canvas.Children.Add(star2);
                
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
                
                // Start with add button visible, timer hidden
                AddButtonOnly.Visibility = Visibility.Visible;
                TimerSelectionDisplay.Visibility = Visibility.Collapsed;
                
                // Ensure no blur is applied on startup
                BackgroundOverlay.Visibility = Visibility.Collapsed;
                MainContentGrid.Effect = null;
                Console.WriteLine("Ensuring no blur effect is applied at startup");
                
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
                
                // Give the UI time to load the items
                Dispatcher.InvokeAsync(() =>
                {
                    // Ensure all MinutesBubble elements are initially hidden
                    ListReminders.UpdateLayout();
                    
                    foreach (var item in ListReminders.Items)
                    {
                        var container = ListReminders.ItemContainerGenerator.ContainerFromItem(item);
                        if (container != null)
                        {
                            // Get the visual tree for this container
                            var presenter = VisualTreeHelper.GetChild(container, 0) as FrameworkElement;
                            if (presenter != null)
                            {
                                var grid = FindChildByName<Grid>(presenter, "ReminderContainer");
                                if (grid != null)
                                {
                                    var minutesBubble = FindChildByName<Border>(grid, "MinutesBubble");
                                    if (minutesBubble != null)
                                    {
                                        minutesBubble.Visibility = Visibility.Collapsed;
                                        Console.WriteLine("MinutesBubble set to Collapsed in LoadExistingReminders");
                                    }
                                    
                                    var reminderText = FindChildByName<Border>(grid, "ReminderText");
                                    if (reminderText != null)
                                    {
                                        reminderText.Visibility = Visibility.Collapsed;
                                        Console.WriteLine("ReminderText set to Collapsed in LoadExistingReminders");
                                    }
                                }
                            }
                        }
                    }
                }, DispatcherPriority.Loaded);
            }
        }
        
        // Helper method to find a child element by name
        private T? FindChildByName<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;
            
            // Try to find the child with the given name
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                // Check if this is the child we're looking for
                if (child is FrameworkElement element && element.Name == childName && child is T typedChild)
                {
                    return typedChild;
                }
                
                // Recursively search through child elements
                var result = FindChildByName<T>(child, childName);
                if (result != null)
                {
                    return result;
                }
            }
            
            return null;
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
                
                // Toggle timer selection display when clicking on the taskbar icon
                ToggleTimerSelectionDisplay();
                
                Console.WriteLine($"Window state: Visibility={Visibility}, State={WindowState}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling tray icon click: {ex.Message}");
                MessageBox.Show($"שגיאה בטיפול בלחיצה על סמל המגש: {ex.Message}", "שגיאה", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void ToggleTimerSelectionDisplay()
        {
            try
            {
                // If timer is visible, hide it; otherwise show it
                if (TimerSelectionDisplay.Visibility == Visibility.Visible)
                {
                    // Reset and hide the timer
                    ResetTimerDisplay();
                    TimerSelectionDisplay.Visibility = Visibility.Collapsed;
                    AddButtonOnly.Visibility = Visibility.Visible;
                }
                else
                {
                    // Show the timer for selection
                    ShowTimerPicker();
                }
                
                Console.WriteLine($"Toggled timer display. Visibility: {TimerSelectionDisplay.Visibility}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error toggling timer display: {ex.Message}");
            }
        }
        
        private void ShowTimerPicker()
        {
            try
            {
                Console.WriteLine("ShowTimerPicker called - preparing to show timer");
                
                // Set default minutes
                ViewModel.NewReminderMinutes = 5;
                
                // Store the current window state before going fullscreen
                if (!_isFullScreenMode)
                {
                    // Remember original window position and size
                    _originalWidth = Width;
                    _originalHeight = Height;
                    _originalLeft = Left;
                    _originalTop = Top;
                    _originalWindowState = WindowState;
                    _isFullScreenMode = true;
                    
                    Console.WriteLine($"Storing original window state: {_originalWidth}x{_originalHeight} at {_originalLeft},{_originalTop}");
                    
                    // Make window cover full screen
                    WindowState = WindowState.Normal; // Reset to normal before setting position
                    
                    // Set to cover primary screen
                    var screen = System.Windows.Forms.Screen.PrimaryScreen;
                    if (screen != null)
                    {
                        // Set window to cover the entire screen
                        Left = screen.Bounds.Left;
                        Top = screen.Bounds.Top;
                        Width = screen.Bounds.Width;
                        Height = screen.Bounds.Height;
                        Console.WriteLine($"Expanded window to cover screen: {Width}x{Height} at {Left},{Top}");
                    }
                    else
                    {
                        // Fallback if screen info isn't available
                        WindowState = WindowState.Maximized;
                        Console.WriteLine("Fallback to maximized window state");
                    }
                }
                
                // Reset UI state
                ResetTimerDisplay();
                
                // Hide the add button first
                AddButtonOnly.Visibility = Visibility.Collapsed;
                
                // Show the timer but don't position it yet
                TimerSelectionDisplay.Visibility = Visibility.Visible;
                
                // Show the background overlay with blur effect
                BackgroundOverlay.Visibility = Visibility.Visible;
                MainContentGrid.Effect = Resources["BackgroundBlurEffect"] as BlurEffect;
                Console.WriteLine("Applied blur effect to MainContentGrid");
                
                // Position the TimerSelectionDisplay at the absolute center of the screen
                TimerSelectionDisplay.HorizontalAlignment = HorizontalAlignment.Left;
                TimerSelectionDisplay.VerticalAlignment = VerticalAlignment.Top;
                
                // We need to force layout to get the actual size
                TimerSelectionDisplay.UpdateLayout();
                
                // Get the primary screen dimensions using Windows Forms (more reliable)
                var screen = System.Windows.Forms.Screen.PrimaryScreen;
                if (screen == null)
                {
                    // Use SystemParameters as fallback
                    double screenWidth = SystemParameters.PrimaryScreenWidth;
                    double screenHeight = SystemParameters.PrimaryScreenHeight;
                    
                    double timerWidth = TimerSelectionDisplay.ActualWidth;
                    double timerHeight = TimerSelectionDisplay.ActualHeight;
                    
                    // Calculate center of screen
                    double screenLeft = (screenWidth - timerWidth) / 2;
                    double screenTop = (screenHeight - timerHeight) / 2;
                    
                    TimerSelectionDisplay.Margin = new Thickness(screenLeft, screenTop, 0, 0);
                    
                    Console.WriteLine($"Using SystemParameters for screen center: {screenWidth}x{screenHeight}");
                }
                else
                {
                    var screenBounds = screen.Bounds;
                    
                    // Calculate the center point of the screen in screen coordinates
                    double screenCenterX = screenBounds.Width / 2;
                    double screenCenterY = screenBounds.Height / 2;
                    
                    // Get the actual size of the timer display after layout
                    double timerWidth = TimerSelectionDisplay.ActualWidth;
                    double timerHeight = TimerSelectionDisplay.ActualHeight;
                    
                    // Calculate the top-left position to center the timer on screen
                    double screenLeft = screenCenterX - (timerWidth / 2);
                    double screenTop = screenCenterY - (timerHeight / 2);
                    
                    // Convert screen position to window coordinates
                    Point windowPoint = this.PointFromScreen(new Point(screenLeft, screenTop));
                    
                    // Set the margin to position the timer at the calculated position
                    TimerSelectionDisplay.Margin = new Thickness(windowPoint.X, windowPoint.Y, 0, 0);
                    
                    Console.WriteLine($"Timer picker shown at screen center. Screen: {screenCenterX}x{screenCenterY}, Timer size: {timerWidth}x{timerHeight}");
                }
                
                // Focus on the timer display
                TimerSelectionDisplay.Focus();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing timer picker: {ex.Message}");
                // Fallback to default positioning in case of error
                TimerSelectionDisplay.HorizontalAlignment = HorizontalAlignment.Center;
                TimerSelectionDisplay.VerticalAlignment = VerticalAlignment.Center;
                TimerSelectionDisplay.Visibility = Visibility.Visible;
            }
        }
        
        private void ResetTimerDisplay()
        {
            Console.WriteLine("ResetTimerDisplay called - clearing timer UI state");
            
            // Reset timer display to initial state for duration selection
            TimerValueDisplay.Visibility = Visibility.Visible;
            ReminderTextInput.Visibility = Visibility.Collapsed;
            ConfirmTimeButton.Visibility = Visibility.Collapsed;
            ReminderTextInput.Text = "";
            
            // Hide the background overlay
            BackgroundOverlay.Visibility = Visibility.Collapsed;
            MainContentGrid.Effect = null;
            Console.WriteLine("Removed blur effect from MainContentGrid");
        }
        
        // Handler for clicking on the compact timer display
        private void TimerDisplay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Only respond to mouse down if we're in timer selection mode (not text input mode)
                if (ReminderTextInput.Visibility != Visibility.Visible)
                {
                    Console.WriteLine("TimerDisplay_MouseDown event triggered");
                    
                    if (e.ChangedButton == MouseButton.Left)
                    {
                        _isDraggingDuration = true;
                        _initialDragPosition = e.GetPosition(this).X;
                        _initialDragMinutes = ViewModel.NewReminderMinutes;
                        
                        if (TimerSelectionDisplay != null)
                        {
                            TimerSelectionDisplay.CaptureMouse();
                        }
                        
                        e.Handled = true;
                        
                        Console.WriteLine($"Duration drag started at position {_initialDragPosition}, minutes: {_initialDragMinutes}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in TimerDisplay_MouseDown: {ex.Message}");
                _isDraggingDuration = false;
                
                if (TimerSelectionDisplay != null && TimerSelectionDisplay.IsMouseCaptured)
                {
                    TimerSelectionDisplay.ReleaseMouseCapture();
                }
            }
        }
        
        private void TimerDisplay_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (_isDraggingDuration)
                {
                    double currentX = e.GetPosition(this).X;
                    double deltaX = currentX - _initialDragPosition;
                    
                    // Calculate new minutes based on drag distance
                    // Use a symmetric scale factor to ensure growth in both directions
                    int newMinutes = _initialDragMinutes + (int)(deltaX / DRAG_SCALE_FACTOR);
                    
                    // Ensure the value is within bounds
                    newMinutes = Math.Max(MIN_DURATION, Math.Min(MAX_DURATION, newMinutes));
                    
                    // Update the view model
                    ViewModel.NewReminderMinutes = newMinutes;
                    
                    // Show the confirm button while dragging
                    if (ConfirmTimeButton != null)
                    {
                        ConfirmTimeButton.Visibility = Visibility.Visible;
                    }
                    
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in TimerDisplay_MouseMove: {ex.Message}");
            }
        }
        
        private void TimerDisplay_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_isDraggingDuration)
                {
                    _isDraggingDuration = false;
                    
                    if (TimerSelectionDisplay != null && TimerSelectionDisplay.IsMouseCaptured)
                    {
                        TimerSelectionDisplay.ReleaseMouseCapture();
                    }
                    
                    // Keep the confirm button visible
                    if (ConfirmTimeButton != null)
                    {
                        ConfirmTimeButton.Visibility = Visibility.Visible;
                    }
                    
                    e.Handled = true;
                    
                    Console.WriteLine($"Duration drag ended, final minutes: {ViewModel.NewReminderMinutes}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in TimerDisplay_MouseUp: {ex.Message}");
                _isDraggingDuration = false;
                
                if (TimerSelectionDisplay != null && TimerSelectionDisplay.IsMouseCaptured)
                {
                    TimerSelectionDisplay.ReleaseMouseCapture();
                }
            }
        }
        
        private void ConfirmTimeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("ConfirmTimeButton_Click event triggered");
                
                if (ReminderTextInput.Visibility == Visibility.Visible)
                {
                    // We're in text entry mode, so create the reminder
                    CreateReminderFromInput();
                }
                else
                {
                    // We're in time selection mode, so switch to text entry mode
                    SwitchToTextEntryMode();
                }
                
                e.Handled = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ConfirmTimeButton_Click: {ex.Message}");
            }
        }
        
        private void SwitchToTextEntryMode()
        {
            // Switch from time selection to text entry mode
            TimerValueDisplay.Visibility = Visibility.Collapsed;
            ReminderTextInput.Visibility = Visibility.Visible;
            ReminderTextInput.Text = "Reminder...";
            ReminderTextInput.Focus();
            ReminderTextInput.SelectAll();
            
            Console.WriteLine("Switched to text entry mode");
        }
        
        private void CreateReminderFromInput()
        {
            // Get the reminder text
            string reminderName = "תזכורת"; // Default
            
            if (!string.IsNullOrWhiteSpace(ReminderTextInput.Text) && 
                ReminderTextInput.Text != "Reminder...")
            {
                reminderName = ReminderTextInput.Text.Trim();
            }
            
            Console.WriteLine($"Creating reminder: {reminderName}, minutes: {ViewModel.NewReminderMinutes}");
            
            // Create and add the reminder
            var reminder = new Reminder(reminderName, ViewModel.NewReminderMinutes);
            ViewModel.Reminders.Add(reminder);
            
            // Save reminders
            ViewModel.SaveReminders();
            
            // Reset and hide timer display
            ResetTimerDisplay();
            TimerSelectionDisplay.Visibility = Visibility.Collapsed;
            AddButtonOnly.Visibility = Visibility.Visible;
            
            Console.WriteLine("Reminder created, UI reset");
        }
        
        private void ReminderTextInput_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    // Create reminder when Enter is pressed
                    CreateReminderFromInput();
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    // Cancel and reset the display when Escape is pressed
                    ResetTimerDisplay();
                    TimerSelectionDisplay.Visibility = Visibility.Collapsed;
                    AddButtonOnly.Visibility = Visibility.Visible;
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ReminderTextInput_KeyDown: {ex.Message}");
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
                
                // Show the timer picker
                ShowTimerPicker();
                
                Console.WriteLine("Started new reminder creation flow");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddButtonOnly_Click: {ex.Message}");
                MessageBox.Show($"שגיאה בפתיחת תפריט תזכורת: {ex.Message}", "שגיאה", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // Handler for ReminderContainer_MouseEnter
        private void ReminderContainer_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                if (sender is Grid grid)
                {
                    // Find the delete button and reminder text elements
                    var deleteButton = grid.FindName("DeleteButton") as Button;
                    var reminderText = grid.FindName("ReminderText") as Border;
                    var minutesBubble = grid.FindName("MinutesBubble") as Border;
                    
                    if (deleteButton != null)
                    {
                        // Show the delete button
                        deleteButton.Visibility = Visibility.Visible;
                    }
                    
                    if (reminderText != null)
                    {
                        // Show the reminder text (full information) when hovering
                        reminderText.Visibility = Visibility.Visible;
                        Console.WriteLine($"MouseEnter: Set ReminderText to Visible for {grid.Name}");
                    }
                    
                    if (minutesBubble != null)
                    {
                        // Keep minutes bubble hidden
                        minutesBubble.Visibility = Visibility.Collapsed;
                        Console.WriteLine($"MouseEnter: Keeping MinutesBubble as Collapsed for {grid.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ReminderContainer_MouseEnter: {ex.Message}");
            }
        }
        
        // Handler for ReminderContainer_MouseLeave
        private void ReminderContainer_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                if (sender is Grid grid)
                {
                    // Find the delete button and reminder text elements
                    var deleteButton = grid.FindName("DeleteButton") as Button;
                    var reminderText = grid.FindName("ReminderText") as Border;
                    var minutesBubble = grid.FindName("MinutesBubble") as Border;
                    
                    if (deleteButton != null)
                    {
                        // Hide the delete button
                        deleteButton.Visibility = Visibility.Collapsed;
                    }
                    
                    if (reminderText != null)
                    {
                        // Hide the reminder text
                        reminderText.Visibility = Visibility.Collapsed;
                        Console.WriteLine($"MouseLeave: Set ReminderText to Collapsed for {grid.Name}");
                    }
                    
                    if (minutesBubble != null)
                    {
                        // Hide the minutes bubble too (changed from previous behavior)
                        minutesBubble.Visibility = Visibility.Collapsed;
                        Console.WriteLine($"MouseLeave: Set MinutesBubble to Collapsed for {grid.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ReminderContainer_MouseLeave: {ex.Message}");
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
        
        // Active reminders panel
        private void RemoveReminderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("RemoveReminderButton_Click event triggered");
                
                // Get the reminder ID from the button's Tag
                if (sender is Button button)
                {
                    Guid reminderId;
                    
                    // Handle both string and Guid types in tag
                    if (button.Tag is Guid guidId)
                    {
                        reminderId = guidId;
                    }
                    else if (button.Tag is string reminderIdStr && Guid.TryParse(reminderIdStr, out Guid parsedId))
                    {
                        reminderId = parsedId;
                    }
                    else
                    {
                        Console.WriteLine("Invalid reminder ID format");
                        return;
                    }
                    
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
                
                // Show timer picker
                ShowTimerPicker();
                
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