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
using Hardcodet.Wpf.TaskbarNotification;
// Use explicit types to avoid ambiguity
using WinSize = System.Windows.Size;
using WinRect = System.Windows.Rect;
using WinColor = System.Windows.Media.Color;
using WinColors = System.Windows.Media.Colors;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Media.Animation;

namespace DeskminderAI
{
    public partial class MainWindow : Window
    {
        private MainViewModel? _viewModel;
        private double _windowWidth;
        private double _windowHeight;
        private TaskbarIcon? _taskbarIcon;
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
        private static readonly double DEFAULT_POSITION_X = 16;
        private static readonly double DEFAULT_POSITION_Y = 20;
        
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
        private const double DRAG_SCALE_FACTOR = 0.15; // More sensitive dragging for better control
        
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
        
        // Variables for keeping track of reminder dragging
        private bool _isDraggingReminder = false;
        private WinPoint _reminderDragStartPoint;
        private UIElement? _currentDraggedReminder;
        private Dictionary<Guid, WinPoint> _reminderPositions = new Dictionary<Guid, WinPoint>();
        
        // Variables for app icon dragging
        private bool _isDraggingAppIcon = false;
        private WinPoint _appIconDragStartPoint;
        private WinPoint _appIconStartPosition = new WinPoint(10, 10); // Default margin
        
        // Track the current timer overlay to prevent multiple instances
        private TimerOverlayWindow? _currentTimerOverlay;
        
        // Track if reminders are collapsed
        private bool _areRemindersCollapsed = false;
        
        // Track original positions of reminders for animation
        private Dictionary<UIElement, System.Windows.Point> _originalReminderPositions = new Dictionary<UIElement, System.Windows.Point>();
        
        // Scaling function for display width based on minutes
        private double ScaleWidthByMinutes(int minutes)
        {
            // Exponential growth formula to make higher values appear wider
            // Base width of 120 pixels for 1 minute
            // Will grow more dramatically as minutes increase
            return 120 + Math.Log10(minutes + 1) * 60; // Reduced growth factor for more granular control
        }
        
        public MainWindow()
        {
            try
            {
                Console.WriteLine("Initializing MainWindow");
                InitializeComponent();
                
                // Create a simple icon programmatically
                CreateAndSetTaskbarIcon();
                
                // Set the window position at the top center of the screen
                Left = (SystemParameters.PrimaryScreenWidth / 2) - 50;
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
                Console.WriteLine("Creating taskbar icon");
                
                // Create a simple blue circle with white stars icon
                var iconBitmap = new System.Drawing.Bitmap(32, 32);
                using (var g = System.Drawing.Graphics.FromImage(iconBitmap))
                {
                    g.Clear(System.Drawing.Color.FromArgb(25, 118, 210)); // Blue background (#1976D2)
                    
                    // Draw white stars (simplified)
                    using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                    {
                        g.FillEllipse(brush, 8, 8, 4, 4);
                        g.FillEllipse(brush, 18, 14, 4, 4);
                    }
                }
                
                // Convert to icon
                var iconHandle = iconBitmap.GetHicon();
                var icon = System.Drawing.Icon.FromHandle(iconHandle);
                
                // Set the TaskbarIcon's Icon
                if (TaskbarIcon != null)
                {
                    TaskbarIcon.Icon = icon;
                    TaskbarIcon.Visibility = Visibility.Visible;
                    Console.WriteLine("Taskbar icon set successfully");
                }
                
                // Clean up
                iconBitmap.Dispose();
                DestroyIcon(iconHandle);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating taskbar icon: {ex.Message}");
            }
        }
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);
        
        public void EnsureWindowVisibility()
        {
            try
            {
                Console.WriteLine("Ensuring window visibility");
                
                // Force window to be visible
                this.Visibility = Visibility.Visible;
                this.WindowState = WindowState.Normal;
                this.ShowInTaskbar = false;
                
                // Check if window is off-screen
                bool isOffScreen = 
                    Left <= -Width || 
                    Left >= SystemParameters.VirtualScreenWidth || 
                    Top <= -Height || 
                    Top >= SystemParameters.VirtualScreenHeight;
                
                if (isOffScreen)
                {
                    // Reset to center of screen
                    Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
                    Top = (SystemParameters.PrimaryScreenHeight - Height) / 2;
                    Console.WriteLine($"Window repositioned to center: {Left}, {Top}");
                }
                
                // Ensure proper z-order
                Topmost = true;
                Activate();
                
                // Make sure the main content is visible
                MainContentGrid.Visibility = Visibility.Visible;
                AddButtonOnly.Visibility = Visibility.Visible;
                
                // Refresh the view
                UpdateLayout();
                
                Console.WriteLine("Window visibility ensured");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ensuring window visibility: {ex.Message}");
            }
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Initialize window size values
                _windowWidth = Width;
                _windowHeight = Height;

                // Create taskbar icon dynamically
                _taskbarIcon = new TaskbarIcon
                {
                    ToolTipText = "DeskminderAI",
                    Visibility = Visibility.Visible
                };
                
                // Set icon using the dynamic icon creation we already have
                CreateAndSetTaskbarIcon();
                
                // Add event handler for double-click
                _taskbarIcon.TrayMouseDoubleClick += TaskbarIcon_TrayMouseDoubleClick;

                // Ensure UI elements are visible
                MainContentGrid.Visibility = Visibility.Visible;
                MainContentGrid.Opacity = 1;
                AddButtonOnly.Visibility = Visibility.Visible;

                // Load existing reminders
                LoadReminders();

                // Force layout update
                UpdateLayout();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"Error in Window_Loaded: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
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
                // Show the timer for selection
                ShowTimerPicker();
                
                Console.WriteLine("Toggled timer display");
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
                
                // Make sure we don't show the timer picker again if it's already visible
                if (_currentTimerOverlay != null)
                {
                    Console.WriteLine("Timer picker is already showing");
                    return;
                }
                
                // Show the background overlay and apply blur
                BackgroundOverlay.Visibility = Visibility.Visible;
                MainContentGrid.Effect = Resources["BackgroundBlurEffect"] as BlurEffect;
                
                // Create and show the fullscreen overlay window for timer selection
                var timerOverlay = new TimerOverlayWindow(this);
                _currentTimerOverlay = timerOverlay;
                
                // Wire up the events to handle reminder creation or cancellation
                timerOverlay.ReminderCreated += TimerOverlay_ReminderCreated;
                timerOverlay.SelectionCancelled += TimerOverlay_SelectionCancelled;
                
                // Show the window as a dialog
                timerOverlay.Show();
                
                Console.WriteLine("Timer overlay window shown with blur effect");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing timer picker: {ex.Message}");
                // Ensure the add button is visible in case of error
                AddButtonOnly.Visibility = Visibility.Visible;
                _currentTimerOverlay = null;
                
                // Reset blur and overlay in case of error
                BackgroundOverlay.Visibility = Visibility.Collapsed;
                MainContentGrid.Effect = null;
                
                MessageBox.Show($"שגיאה בהצגת בחירת זמן: {ex.Message}", "שגיאה", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void TimerOverlay_ReminderCreated(object sender, ReminderCreatedEventArgs e)
        {
            try
            {
                Console.WriteLine($"Timer overlay returned reminder: {e.Name}, minutes: {e.Minutes}, seconds: {e.Seconds}");
                
                // Create and add the reminder with both minutes and seconds
                var reminder = new Reminder(e.Name, e.Minutes);
                
                // If there are seconds, adjust the EndTime to include them
                if (e.Seconds > 0)
                {
                    reminder.EndTime = reminder.EndTime.AddSeconds(e.Seconds);
                    // Force update time left to reflect new end time with seconds
                    reminder.UpdateTimeLeft();
                }
                
                ViewModel.Reminders.Add(reminder);
                
                // Save reminders
                ViewModel.SaveReminders();
                
                // Make sure the add button is visible
                AddButtonOnly.Visibility = Visibility.Visible;
                
                // Remove blur effect and hide overlay
                BackgroundOverlay.Visibility = Visibility.Collapsed;
                MainContentGrid.Effect = null;
                
                // Clear the current overlay reference
                _currentTimerOverlay = null;
                
                Console.WriteLine("Reminder created from overlay, blur effect removed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating reminder from overlay: {ex.Message}");
                // Ensure the add button is visible in case of error
                AddButtonOnly.Visibility = Visibility.Visible;
                // Remove blur effect and hide overlay even in case of error
                BackgroundOverlay.Visibility = Visibility.Collapsed;
                MainContentGrid.Effect = null;
                _currentTimerOverlay = null;
            }
        }
        
        private void TimerOverlay_SelectionCancelled(object sender, EventArgs e)
        {
            // Make sure the add button is visible when timer selection is cancelled
            AddButtonOnly.Visibility = Visibility.Visible;
            
            // Remove blur effect and hide overlay
            BackgroundOverlay.Visibility = Visibility.Collapsed;
            MainContentGrid.Effect = null;
            
            // Clear the current overlay reference
            _currentTimerOverlay = null;
            
            Console.WriteLine("Timer selection cancelled, restored normal UI and removed blur effect");
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
                    // More sensitive scale factor for better response
                    int newMinutes = _initialDragMinutes + (int)(deltaX / DRAG_SCALE_FACTOR);
                    
                    // Ensure the value is within bounds
                    newMinutes = Math.Max(MIN_DURATION, Math.Min(MAX_DURATION, newMinutes));
                    
                    // Update the view model
                    ViewModel.NewReminderMinutes = newMinutes;
                    
                    // Adjust the width of the timer display based on the minutes value
                    if (TimerSelectionDisplay != null)
                    {
                        // Apply a non-linear scaling to make the width increase more dramatically
                        double newWidth = ScaleWidthByMinutes(newMinutes);
                        TimerSelectionDisplay.MinWidth = newWidth;
                        
                        Console.WriteLine($"Adjusted width to {newWidth}px for {newMinutes} minutes");
                    }
                    
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
            
            // Restore window to original size
            RestoreWindowSize();
            
            Console.WriteLine("Reminder created, UI reset");
        }
        
        private void RestoreWindowSize()
        {
            // Only restore if we're in fullscreen mode
            if (_isFullScreenMode)
            {
                Console.WriteLine($"Restoring window to original size: {_originalWidth}x{_originalHeight} at {_originalLeft},{_originalTop}");
                
                // Restore the original window position and size
                Left = _originalLeft;
                Top = _originalTop;
                Width = _originalWidth;
                Height = _originalHeight;
                WindowState = _originalWindowState;
                
                _isFullScreenMode = false;
            }
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
                    
                    // Restore window to original size
                    RestoreWindowSize();
                    
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
                    // Get the parent canvas that contains all elements
                    var parentCanvas = VisualTreeHelper.GetParent(grid) as Canvas;
                    if (parentCanvas == null) return;

                    // Find elements within the canvas
                    var deleteButton = parentCanvas.Children.OfType<Button>().FirstOrDefault(b => b.Name == "DeleteButton");
                    var reminderText = parentCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Name == "ReminderText");
                    var minutesBubble = parentCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Name == "MinutesBubble");
                    
                    if (deleteButton != null)
                    {
                        // Show the delete button
                        deleteButton.Visibility = Visibility.Visible;
                    }
                    
                    // Show the reminder text with animation
                    if (reminderText != null)
                    {
                        // Reset transform values to prepare for animation
                        var transformGroup = reminderText.RenderTransform as TransformGroup;
                        if (transformGroup != null)
                        {
                            var scaleTransform = transformGroup.Children[0] as ScaleTransform;
                            var translateTransform = transformGroup.Children[1] as TranslateTransform;
                            
                            if (scaleTransform != null)
                            {
                                scaleTransform.ScaleX = 0.8;
                                scaleTransform.ScaleY = 0.8;
                            }
                            
                            if (translateTransform != null)
                            {
                                translateTransform.Y = -10;
                            }
                        }
                        
                        reminderText.Opacity = 0;
                        reminderText.Visibility = Visibility.Visible;
                        
                        // Apply animation on mouse enter
                        var storyboard = new Storyboard();
                        
                        // Scale animation
                        var scaleXAnim = new DoubleAnimation
                        {
                            From = 0.8,
                            To = 1.0,
                            Duration = TimeSpan.FromSeconds(0.3)
                        };
                        var easing = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 };
                        scaleXAnim.EasingFunction = easing;
                        Storyboard.SetTarget(scaleXAnim, reminderText);
                        Storyboard.SetTargetProperty(scaleXAnim, new PropertyPath("RenderTransform.Children[0].ScaleX"));
                        storyboard.Children.Add(scaleXAnim);
                        
                        var scaleYAnim = new DoubleAnimation
                        {
                            From = 0.8,
                            To = 1.0,
                            Duration = TimeSpan.FromSeconds(0.3)
                        };
                        scaleYAnim.EasingFunction = easing;
                        Storyboard.SetTarget(scaleYAnim, reminderText);
                        Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath("RenderTransform.Children[0].ScaleY"));
                        storyboard.Children.Add(scaleYAnim);
                        
                        // Translation animation
                        var translateYAnim = new DoubleAnimation
                        {
                            From = -10,
                            To = 0,
                            Duration = TimeSpan.FromSeconds(0.3)
                        };
                        translateYAnim.EasingFunction = easing;
                        Storyboard.SetTarget(translateYAnim, reminderText);
                        Storyboard.SetTargetProperty(translateYAnim, new PropertyPath("RenderTransform.Children[1].Y"));
                        storyboard.Children.Add(translateYAnim);
                        
                        // Opacity animation
                        var opacityAnim = new DoubleAnimation
                        {
                            From = 0,
                            To = 1,
                            Duration = TimeSpan.FromSeconds(0.3)
                        };
                        Storyboard.SetTarget(opacityAnim, reminderText);
                        Storyboard.SetTargetProperty(opacityAnim, new PropertyPath("Opacity"));
                        storyboard.Children.Add(opacityAnim);
                        
                        // Start the animation
                        storyboard.Begin();
                    }
                    
                    // Keep minutes bubble hidden when text is shown
                    if (minutesBubble != null)
                    {
                        minutesBubble.Visibility = Visibility.Collapsed;
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
                    // Get the parent canvas that contains all elements
                    var parentCanvas = VisualTreeHelper.GetParent(grid) as Canvas;
                    if (parentCanvas == null) return;

                    // Find elements within the canvas
                    var deleteButton = parentCanvas.Children.OfType<Button>().FirstOrDefault(b => b.Name == "DeleteButton");
                    var reminderText = parentCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Name == "ReminderText");
                    var minutesBubble = parentCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Name == "MinutesBubble");
                    
                    if (deleteButton != null)
                    {
                        // Hide the delete button
                        deleteButton.Visibility = Visibility.Collapsed;
                    }
                    
                    if (reminderText != null && reminderText.Visibility == Visibility.Visible)
                    {
                        // Animate hiding the reminder text
                        var storyboard = new Storyboard();
                        
                        // Scale animation
                        var scaleXAnim = new DoubleAnimation
                        {
                            From = 1.0,
                            To = 0.8,
                            Duration = TimeSpan.FromSeconds(0.2)
                        };
                        var easing = new QuadraticEase { EasingMode = EasingMode.EaseIn };
                        scaleXAnim.EasingFunction = easing;
                        Storyboard.SetTarget(scaleXAnim, reminderText);
                        Storyboard.SetTargetProperty(scaleXAnim, new PropertyPath("RenderTransform.Children[0].ScaleX"));
                        storyboard.Children.Add(scaleXAnim);
                        
                        var scaleYAnim = new DoubleAnimation
                        {
                            From = 1.0,
                            To = 0.8,
                            Duration = TimeSpan.FromSeconds(0.2)
                        };
                        scaleYAnim.EasingFunction = easing;
                        Storyboard.SetTarget(scaleYAnim, reminderText);
                        Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath("RenderTransform.Children[0].ScaleY"));
                        storyboard.Children.Add(scaleYAnim);
                        
                        // Translation animation
                        var translateYAnim = new DoubleAnimation
                        {
                            From = 0,
                            To = -10,
                            Duration = TimeSpan.FromSeconds(0.2)
                        };
                        translateYAnim.EasingFunction = easing;
                        Storyboard.SetTarget(translateYAnim, reminderText);
                        Storyboard.SetTargetProperty(translateYAnim, new PropertyPath("RenderTransform.Children[1].Y"));
                        storyboard.Children.Add(translateYAnim);
                        
                        // Opacity animation
                        var opacityAnim = new DoubleAnimation
                        {
                            From = 1,
                            To = 0,
                            Duration = TimeSpan.FromSeconds(0.2)
                        };
                        Storyboard.SetTarget(opacityAnim, reminderText);
                        Storyboard.SetTargetProperty(opacityAnim, new PropertyPath("Opacity"));
                        storyboard.Children.Add(opacityAnim);
                        
                        // When the animation completes, hide the element
                        opacityAnim.Completed += (s, args) => 
                        {
                            reminderText.Visibility = Visibility.Collapsed;
                        };
                        
                        // Start the animation
                        storyboard.Begin();
                    }
                    
                    if (minutesBubble != null)
                    {
                        // Hide the minutes bubble too
                        minutesBubble.Visibility = Visibility.Collapsed;
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
        
        // Replace individual reminder dragging with whole window dragging
        private void ReminderContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Instead of dragging individual reminders, trigger window dragging
            Window_MouseDown(this, e);
        }
        
        private void ReminderContainer_MouseMove(object sender, MouseEventArgs e)
        {
            // No special handling needed, window dragging is handled by OnMouseMove
        }
        
        private void ReminderContainer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // No special handling needed, window dragging is handled by OnMouseUp
        }
        
        private void AddButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Only handle left mouse button for dragging
                if (e.ChangedButton == MouseButton.Left)
                {
                    // Start dragging the app icon
                    _isDraggingAppIcon = true;
                    _appIconDragStartPoint = e.GetPosition(this);
                    
                    // Get current margin as the starting position
                    Thickness currentMargin = AddButtonOnly.Margin;
                    _appIconStartPosition = new WinPoint(currentMargin.Left, currentMargin.Top);
                    
                    // Capture mouse
                    AddButtonOnly.CaptureMouse();
                    e.Handled = true;
                    
                    Console.WriteLine("Started dragging app icon");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting app icon drag: {ex.Message}");
                _isDraggingAppIcon = false;
                if (AddButtonOnly.IsMouseCaptured)
                {
                    AddButtonOnly.ReleaseMouseCapture();
                }
            }
        }
        
        private void AddButton_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (_isDraggingAppIcon && e.LeftButton == MouseButtonState.Pressed)
                {
                    // Calculate movement delta
                    WinPoint currentPoint = e.GetPosition(this);
                    double deltaX = currentPoint.X - _appIconDragStartPoint.X;
                    double deltaY = currentPoint.Y - _appIconDragStartPoint.Y;
                    
                    // Update position
                    double newLeft = Math.Max(0, _appIconStartPosition.X + deltaX);
                    double newTop = Math.Max(0, _appIconStartPosition.Y + deltaY);
                    
                    // Apply new margin to move the button
                    AddButtonOnly.Margin = new Thickness(newLeft, newTop, 0, 10);
                    
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during app icon drag: {ex.Message}");
            }
        }
        
        private void AddButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_isDraggingAppIcon)
                {
                    // End dragging
                    _isDraggingAppIcon = false;
                    AddButtonOnly.ReleaseMouseCapture();
                    e.Handled = true;
                    
                    // Save position
                    Thickness finalMargin = AddButtonOnly.Margin;
                    _appIconStartPosition = new WinPoint(finalMargin.Left, finalMargin.Top);
                    
                    // Could save to settings here
                    Console.WriteLine($"App icon moved to {_appIconStartPosition.X}, {_appIconStartPosition.Y}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ending app icon drag: {ex.Message}");
                _isDraggingAppIcon = false;
                if (AddButtonOnly.IsMouseCaptured)
                {
                    AddButtonOnly.ReleaseMouseCapture();
                }
            }
        }

        private void TaskbarIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureWindowVisibility();
                ShowTimerPanel();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling taskbar double click: {ex.Message}");
            }
        }

        private void LoadReminders()
        {
            try
            {
                if (ViewModel != null)
                {
                    LoadExistingReminders();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading reminders: {ex.Message}");
            }
        }

        private void AddButton_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Toggle the reminders collapsed state
                _areRemindersCollapsed = !_areRemindersCollapsed;
                
                if (_areRemindersCollapsed)
                {
                    // Collapse reminders with animation (magnet effect)
                    CollapseRemindersWithAnimation();
                }
                else
                {
                    // Expand reminders with animation
                    ExpandRemindersWithAnimation();
                }
                
                e.Handled = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddButton_MouseDoubleClick: {ex.Message}");
            }
        }
        
        private void CollapseRemindersWithAnimation()
        {
            try
            {
                // Store the collection of reminder elements
                var reminderElements = new List<UIElement>();
                
                // Get all reminder containers
                for (int i = 0; i < ListReminders.Items.Count; i++)
                {
                    var item = ListReminders.Items[i];
                    var container = ListReminders.ItemContainerGenerator.ContainerFromItem(item) as ContentPresenter;
                    
                    if (container == null)
                    {
                        // Force update layout to ensure containers are generated
                        ListReminders.UpdateLayout();
                        container = ListReminders.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
                    }
                    
                    if (container != null)
                    {
                        // Find the reminder container in the visual tree
                        var reminderContainer = FindVisualChild<Grid>(container);
                        if (reminderContainer != null)
                        {
                            reminderElements.Add(reminderContainer);
                            
                            // Store original position for later restoration
                            System.Windows.Point originalPos = reminderContainer.TranslatePoint(new System.Windows.Point(0, 0), this);
                            _originalReminderPositions[reminderContainer] = originalPos;
                        }
                    }
                }
                
                // Get the position of the add button (target position)
                System.Windows.Point targetPoint = AddButtonOnly.TranslatePoint(new System.Windows.Point(24, 24), this);
                
                // Create and start animations for each reminder
                foreach (UIElement reminder in reminderElements)
                {
                    System.Windows.Point startPoint = reminder.TranslatePoint(new System.Windows.Point(0, 0), this);
                    
                    // Calculate the translation to move to the target
                    TranslateTransform transform = new TranslateTransform();
                    reminder.RenderTransform = transform;
                    
                    // Create animation for X movement
                    DoubleAnimation animX = new DoubleAnimation
                    {
                        From = 0,
                        To = targetPoint.X - startPoint.X,
                        Duration = TimeSpan.FromSeconds(0.5),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    
                    // Create animation for Y movement
                    DoubleAnimation animY = new DoubleAnimation
                    {
                        From = 0,
                        To = targetPoint.Y - startPoint.Y,
                        Duration = TimeSpan.FromSeconds(0.5),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    
                    // Create animation for opacity
                    DoubleAnimation opacityAnim = new DoubleAnimation
                    {
                        From = 1.0,
                        To = 0.0,
                        Duration = TimeSpan.FromSeconds(0.4),
                        BeginTime = TimeSpan.FromSeconds(0.1)
                    };
                    
                    // Create animation for scale
                    ScaleTransform scaleTransform = new ScaleTransform(1, 1);
                    TransformGroup transformGroup = new TransformGroup();
                    transformGroup.Children.Add(scaleTransform);
                    transformGroup.Children.Add(transform);
                    reminder.RenderTransform = transformGroup;
                    
                    DoubleAnimation scaleAnim = new DoubleAnimation
                    {
                        From = 1.0,
                        To = 0.3,
                        Duration = TimeSpan.FromSeconds(0.5),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    
                    // Start animations
                    transform.BeginAnimation(TranslateTransform.XProperty, animX);
                    transform.BeginAnimation(TranslateTransform.YProperty, animY);
                    reminder.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
                }
                
                // Hide the scrollviewer after animation completes
                DispatcherTimer timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(0.5)
                };
                
                timer.Tick += (s, e) =>
                {
                    RemindersScrollViewer.Visibility = Visibility.Collapsed;
                    timer.Stop();
                };
                
                timer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CollapseRemindersWithAnimation: {ex.Message}");
                
                // Fallback in case of error - just hide the elements
                RemindersScrollViewer.Visibility = Visibility.Collapsed;
            }
        }
        
        private void ExpandRemindersWithAnimation()
        {
            try
            {
                // Make the reminders visible again
                RemindersScrollViewer.Visibility = Visibility.Visible;
                
                // Get the collection of reminder elements
                var reminderElements = new List<UIElement>();
                
                // Get all reminder containers
                for (int i = 0; i < ListReminders.Items.Count; i++)
                {
                    var item = ListReminders.Items[i];
                    var container = ListReminders.ItemContainerGenerator.ContainerFromItem(item) as ContentPresenter;
                    
                    if (container == null)
                    {
                        // Force update layout to ensure containers are generated
                        ListReminders.UpdateLayout();
                        container = ListReminders.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
                    }
                    
                    if (container != null)
                    {
                        // Find the reminder container in the visual tree
                        var reminderContainer = FindVisualChild<Grid>(container);
                        if (reminderContainer != null)
                        {
                            reminderElements.Add(reminderContainer);
                            reminderContainer.Opacity = 0; // Start invisible
                        }
                    }
                }
                
                // Get the add button position (starting position)
                System.Windows.Point startPoint = AddButtonOnly.TranslatePoint(new System.Windows.Point(24, 24), this);
                
                // Create and start animations for each reminder
                for (int i = 0; i < reminderElements.Count; i++)
                {
                    UIElement reminder = reminderElements[i];
                    
                    // Reset transforms
                    TranslateTransform transform = new TranslateTransform();
                    ScaleTransform scaleTransform = new ScaleTransform(0.3, 0.3);
                    TransformGroup transformGroup = new TransformGroup();
                    transformGroup.Children.Add(scaleTransform);
                    transformGroup.Children.Add(transform);
                    reminder.RenderTransform = transformGroup;
                    
                    // Get target position
                    System.Windows.Point targetPoint = new System.Windows.Point(0, 0);
                    if (_originalReminderPositions.ContainsKey(reminder))
                    {
                        targetPoint = _originalReminderPositions[reminder];
                    }
                    
                    // Add delay between each reminder
                    double delay = i * 0.05;
                    
                    // Create animation for X movement
                    DoubleAnimation animX = new DoubleAnimation
                    {
                        From = startPoint.X - targetPoint.X,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.5),
                        BeginTime = TimeSpan.FromSeconds(delay),
                        EasingFunction = new ElasticEase 
                        { 
                            EasingMode = EasingMode.EaseOut,
                            Oscillations = 2,
                            Springiness = 5
                        }
                    };
                    
                    // Create animation for Y movement
                    DoubleAnimation animY = new DoubleAnimation
                    {
                        From = startPoint.Y - targetPoint.Y,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.5),
                        BeginTime = TimeSpan.FromSeconds(delay),
                        EasingFunction = new ElasticEase 
                        { 
                            EasingMode = EasingMode.EaseOut,
                            Oscillations = 2,
                            Springiness = 5
                        }
                    };
                    
                    // Create animation for opacity
                    DoubleAnimation opacityAnim = new DoubleAnimation
                    {
                        From = 0.0,
                        To = 1.0,
                        Duration = TimeSpan.FromSeconds(0.3),
                        BeginTime = TimeSpan.FromSeconds(delay)
                    };
                    
                    // Create animation for scale
                    DoubleAnimation scaleAnim = new DoubleAnimation
                    {
                        From = 0.3,
                        To = 1.0,
                        Duration = TimeSpan.FromSeconds(0.5),
                        BeginTime = TimeSpan.FromSeconds(delay),
                        EasingFunction = new ElasticEase 
                        { 
                            EasingMode = EasingMode.EaseOut,
                            Oscillations = 2,
                            Springiness = 5
                        }
                    };
                    
                    // Start animations
                    transform.BeginAnimation(TranslateTransform.XProperty, animX);
                    transform.BeginAnimation(TranslateTransform.YProperty, animY);
                    reminder.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExpandRemindersWithAnimation: {ex.Message}");
                
                // Fallback in case of error - just show the elements without animation
                RemindersScrollViewer.Visibility = Visibility.Visible;
            }
        }
        
        /// <summary>
        /// Finds a visual child element of the specified type in a parent element.
        /// </summary>
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                
                if (child != null && child is T result)
                {
                    return result;
                }
                else
                {
                    T childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                    {
                        return childOfChild;
                    }
                }
            }
            
            return null;
        }

        // Helper method to find a specific named element in a container
        private T FindElementByName<T>(FrameworkElement container, string name) where T : FrameworkElement
        {
            // First, try to find directly in the container's children if it's a Panel
            if (container is Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    if (child is T foundItem && (foundItem as FrameworkElement)?.Name == name)
                        return foundItem;
                }
            }
            
            // Try to find in the named scope
            var namedElement = container.FindName(name) as T;
            if (namedElement != null)
                return namedElement;
            
            // Search deeper in the visual tree
            int childCount = VisualTreeHelper.GetChildrenCount(container);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(container, i) as FrameworkElement;
                if (child != null)
                {
                    // Check if this child is the one we're looking for
                    if (child is T foundElement && child.Name == name)
                        return foundElement;
                    
                    // Recursively search in this child's subtree
                    var result = FindElementByName<T>(child, name);
                    if (result != null)
                        return result;
                }
            }
            
            return null;
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