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
using System.Collections.Generic;

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
        private const double DRAG_SCALE_FACTOR = 0.5; // More sensitive dragging (lower value = more sensitive)
        
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
        private Point _reminderDragStartPoint;
        private UIElement? _currentDraggedReminder;
        private Dictionary<Guid, Point> _reminderPositions = new Dictionary<Guid, Point>();
        
        // Variables for app icon dragging
        private bool _isDraggingAppIcon = false;
        private Point _appIconDragStartPoint;
        private Point _appIconStartPosition = new Point(10, 10); // Default margin
        
        // Scaling function for display width based on minutes
        private double ScaleWidthByMinutes(int minutes)
        {
            // Exponential growth formula to make higher values appear wider
            // Base width of 120 pixels for 1 minute
            // Will grow more dramatically as minutes increase
            return 120 + Math.Log10(minutes + 1) * 80;
        }
        
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
                
                // Initialize window size values to avoid null references
                _originalWidth = double.IsNaN(Width) ? 300 : Width;
                _originalHeight = double.IsNaN(Height) ? 400 : Height;
                _originalLeft = Left;
                _originalTop = Top;
                _originalWindowState = WindowState;
                
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
                
                // Create and show the fullscreen overlay window for timer selection
                var timerOverlay = new TimerOverlayWindow();
                
                // Wire up the events to handle reminder creation or cancellation
                timerOverlay.ReminderCreated += TimerOverlay_ReminderCreated;
                timerOverlay.SelectionCancelled += TimerOverlay_SelectionCancelled;
                
                // Show the window as a dialog
                timerOverlay.Show();
                
                Console.WriteLine("Timer overlay window shown");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing timer picker: {ex.Message}");
                // Ensure the add button is visible in case of error
                AddButtonOnly.Visibility = Visibility.Visible;
            }
        }
        
        private void TimerOverlay_ReminderCreated(object sender, ReminderCreatedEventArgs e)
        {
            try
            {
                Console.WriteLine($"Timer overlay returned reminder: {e.Name}, minutes: {e.Minutes}");
                
                // Create and add the reminder
                var reminder = new Reminder(e.Name, e.Minutes);
                ViewModel.Reminders.Add(reminder);
                
                // Save reminders
                ViewModel.SaveReminders();
                
                // Make sure the add button is visible
                AddButtonOnly.Visibility = Visibility.Visible;
                
                Console.WriteLine("Reminder created from overlay");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating reminder from overlay: {ex.Message}");
                // Ensure the add button is visible in case of error
                AddButtonOnly.Visibility = Visibility.Visible;
            }
        }
        
        private void TimerOverlay_SelectionCancelled(object sender, EventArgs e)
        {
            // Make sure the add button is visible when timer selection is cancelled
            AddButtonOnly.Visibility = Visibility.Visible;
            Console.WriteLine("Timer selection cancelled, restoring normal UI");
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
        
        // Used for storing custom positions of dragged reminders
        private TranslateTransform GetReminderTransform(UIElement element)
        {
            // Check if the element already has a transform
            if (element.RenderTransform is TranslateTransform transform)
            {
                return transform;
            }
            
            // Create a new transform if one doesn't exist
            var newTransform = new TranslateTransform();
            element.RenderTransform = newTransform;
            return newTransform;
        }
        
        private void ReminderContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Grid reminderGrid)
                {
                    // Start dragging this reminder
                    _isDraggingReminder = true;
                    _reminderDragStartPoint = e.GetPosition(this);
                    _currentDraggedReminder = reminderGrid;
                    
                    // Capture mouse to receive events even when outside the element
                    reminderGrid.CaptureMouse();
                    e.Handled = true;
                    
                    Console.WriteLine("Started dragging reminder");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting reminder drag: {ex.Message}");
                _isDraggingReminder = false;
                if (_currentDraggedReminder != null && _currentDraggedReminder.IsMouseCaptured)
                {
                    _currentDraggedReminder.ReleaseMouseCapture();
                }
            }
        }
        
        private void ReminderContainer_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (_isDraggingReminder && _currentDraggedReminder != null && sender is Grid reminderGrid)
                {
                    // Get the current mouse position
                    Point currentPosition = e.GetPosition(this);
                    
                    // Calculate the offset from the start position
                    double offsetX = currentPosition.X - _reminderDragStartPoint.X;
                    double offsetY = currentPosition.Y - _reminderDragStartPoint.Y;
                    
                    // Apply the translation to move the reminder
                    var transform = GetReminderTransform(reminderGrid);
                    transform.X = offsetX;
                    transform.Y = offsetY;
                    
                    // Store the position if the reminder has an ID
                    if (reminderGrid.Tag is Guid reminderId)
                    {
                        _reminderPositions[reminderId] = new Point(offsetX, offsetY);
                    }
                    
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during reminder drag: {ex.Message}");
            }
        }
        
        private void ReminderContainer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_isDraggingReminder && _currentDraggedReminder != null)
                {
                    // End dragging
                    _isDraggingReminder = false;
                    _currentDraggedReminder.ReleaseMouseCapture();
                    _currentDraggedReminder = null;
                    e.Handled = true;
                    
                    Console.WriteLine("Stopped dragging reminder");
                    
                    // Save positions to settings if needed
                    SaveReminderPositions();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ending reminder drag: {ex.Message}");
                _isDraggingReminder = false;
                if (_currentDraggedReminder != null && _currentDraggedReminder.IsMouseCaptured)
                {
                    _currentDraggedReminder.ReleaseMouseCapture();
                }
            }
        }
        
        private void SaveReminderPositions()
        {
            // Could be implemented to save positions to settings
            Console.WriteLine($"Saved positions for {_reminderPositions.Count} reminders");
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
                    _appIconStartPosition = new Point(currentMargin.Left, currentMargin.Top);
                    
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
                    Point currentPoint = e.GetPosition(this);
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
                    _appIconStartPosition = new Point(finalMargin.Left, finalMargin.Top);
                    
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