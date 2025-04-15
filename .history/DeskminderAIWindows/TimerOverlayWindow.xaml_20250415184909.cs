using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Forms;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using System.Windows.Media.Animation;

namespace DeskminderAI
{
    public partial class TimerOverlayWindow : Window, INotifyPropertyChanged
    {
        // Variables for drag duration interface
        private bool _isDraggingDuration = false;
        private double _initialDragPosition;
        private int _initialDragMinutes;
        private const int MIN_DURATION = 1;  // Minimum 1 minute
        private const int MAX_DURATION = 120; // Maximum 120 minutes (2 hours)
        private const double DRAG_SCALE_FACTOR = 0.8; // Even slower dragging
        private const double SECONDS_ACTIVATION_DELAY = 800; // ms to wait before showing seconds
        private System.Windows.Threading.DispatcherTimer? _secondsTimer;
        private DateTime _lastDragTime;
        
        // Scaling function for display width based on minutes and seconds
        private double ScaleWidthByMinutes(int minutes, int seconds = 0)
        {
            // Base width
            double baseWidth = 180;
            
            // Linear component grows very slowly
            double linearComponent = minutes * 1.2;
            
            // Logarithmic component for initial quick growth that tapers off
            double logComponent = Math.Log(minutes + 1) * 25;
            
            // Add small increment for seconds
            double secondsComponent = seconds > 0 ? (seconds / 60.0) * 20 : 0;
            
            return baseWidth + linearComponent + logComponent + secondsComponent;
        }

        private int _minutes = 1;
        public int Minutes
        {
            get => _minutes;
            set
            {
                if (_minutes != value)
                {
                    _minutes = value;
                    OnPropertyChanged();
                }
            }
        }
        
        private int _seconds = 0;
        public int Seconds
        {
            get => _seconds;
            set
            {
                if (_seconds != value)
                {
                    _seconds = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ReminderText { get; set; } = "";

        // Event to notify main window when a reminder is created
        public event EventHandler<ReminderCreatedEventArgs>? ReminderCreated;
        
        // Event to notify main window when timer selection is cancelled
        public event EventHandler? SelectionCancelled;

        public TimerOverlayWindow()
        {
            try
            {
                Console.WriteLine("Initializing TimerOverlayWindow");
                InitializeComponent();
                
                // Apply blur effect to the entire window
                DataContext = this;
                
                // Ensure we are at the proper position
                Left = 0;
                Top = 0;
                
                // Get screen dimensions - use fallback if needed
                var primaryScreen = Screen.PrimaryScreen;
                if (primaryScreen != null)
                {
                    Width = primaryScreen.Bounds.Width;
                    Height = primaryScreen.Bounds.Height;
                }
                else
                {
                    // Fallback to WPF system parameters
                    Width = SystemParameters.PrimaryScreenWidth;
                    Height = SystemParameters.PrimaryScreenHeight;
                }
                
                Console.WriteLine($"TimerOverlayWindow initialized at size: {Width}x{Height}");
                
                // Initialize with 1 minute
                Minutes = 1;
                Seconds = 0;
                
                // Set the initial width based on the minutes
                TimerSelectionDisplay.MinWidth = ScaleWidthByMinutes(Minutes);
                
                // Initialize seconds timer
                _secondsTimer = new System.Windows.Threading.DispatcherTimer();
                _secondsTimer.Interval = TimeSpan.FromMilliseconds(50);
                _secondsTimer.Tick += CheckForSecondsActivation;
                
                // Show the confirm button from the start
                ConfirmTimeButton.Visibility = Visibility.Visible;
                
                // Ensure window is visible and on top
                Show();
                Activate();
                Topmost = true;
                
                // Add entrance animation
                BeginEntranceAnimation();
                
                Console.WriteLine("TimerOverlayWindow shown and activated");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing TimerOverlayWindow: {ex.Message}");
                System.Windows.MessageBox.Show($"שגיאה באתחול חלון בחירת זמן: {ex.Message}", "שגיאה", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                // Cancel when Escape is pressed
                CancelSelection();
                e.Handled = true;
            }
        }

        private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Cancel if clicking outside the timer selection area
            if (e.ChangedButton == MouseButton.Left)
            {
                CancelSelection();
                e.Handled = true;
            }
        }

        private void TimerDisplay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (ReminderTextInput.Visibility != Visibility.Visible)
                {
                    _isDraggingDuration = true;
                    _initialDragPosition = e.GetPosition(this).X;
                    _initialDragMinutes = Minutes;
                    
                    if (TimerSelectionDisplay != null)
                    {
                        TimerSelectionDisplay.CaptureMouse();
                    }
                    
                    // Reset seconds when starting new drag
                    Seconds = 0;
                    HideSecondsDisplay();
                    
                    e.Handled = true;
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
        
        private void TimerDisplay_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            try
            {
                if (_isDraggingDuration)
                {
                    double currentX = e.GetPosition(this).X;
                    double deltaX = currentX - _initialDragPosition;
                    
                    // Calculate new minutes based on drag distance
                    int newMinutes = _initialDragMinutes + (int)(deltaX / DRAG_SCALE_FACTOR);
                    
                    // Ensure the value is within bounds
                    newMinutes = Math.Max(MIN_DURATION, Math.Min(MAX_DURATION, newMinutes));
                    
                    if (Minutes != newMinutes)
                    {
                        Minutes = newMinutes;
                        Seconds = 0; // Reset seconds when minutes change
                        
                        // Update width with animation
                        double newWidth = ScaleWidthByMinutes(newMinutes);
                        var animation = new DoubleAnimation
                        {
                            To = newWidth,
                            Duration = TimeSpan.FromSeconds(0.2),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };
                        TimerSelectionDisplay.BeginAnimation(FrameworkElement.MinWidthProperty, animation);
                    }
                    
                    _lastDragTime = DateTime.Now;
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
                    
                    // Start checking for seconds activation
                    _lastDragTime = DateTime.Now;
                    _secondsTimer?.Start();
                    
                    e.Handled = true;
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
            
            Console.WriteLine($"Creating reminder: {reminderName}, minutes: {Minutes}");
            
            // Raise the event to create the reminder in main window
            ReminderCreated?.Invoke(this, new ReminderCreatedEventArgs
            {
                Name = reminderName,
                Minutes = Minutes
            });
            
            // Close this window
            Close();
        }

        private void ReminderTextInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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
                    CancelSelection();
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ReminderTextInput_KeyDown: {ex.Message}");
            }
        }

        private void CancelSelection()
        {
            Console.WriteLine("Timer selection cancelled");
            SelectionCancelled?.Invoke(this, EventArgs.Empty);
            Close();
        }

        private void BeginEntranceAnimation()
        {
            var scaleTransform = (TransformGroup)TimerSelectionDisplay.RenderTransform;
            var scale = (ScaleTransform)scaleTransform.Children[0];
            
            scale.ScaleX = 0.9;
            scale.ScaleY = 0.9;
            
            var animation = new DoubleAnimation
            {
                From = 0.9,
                To = 1.0,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
        }
        
        private void CheckForSecondsActivation(object? sender, EventArgs e)
        {
            if (!_isDraggingDuration && (DateTime.Now - _lastDragTime).TotalMilliseconds > SECONDS_ACTIVATION_DELAY)
            {
                _secondsTimer?.Stop();
                ShowSecondsDisplay();
            }
        }
        
        private void ShowSecondsDisplay()
        {
            if (SecondsDisplay.Opacity < 1)
            {
                var animation = new DoubleAnimation
                {
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.2),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                SecondsDisplay.BeginAnimation(UIElement.OpacityProperty, animation);
            }
        }
        
        private void HideSecondsDisplay()
        {
            if (SecondsDisplay.Opacity > 0)
            {
                var animation = new DoubleAnimation
                {
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.2),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                SecondsDisplay.BeginAnimation(UIElement.OpacityProperty, animation);
            }
        }

        // Add INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    public class ReminderCreatedEventArgs : EventArgs
    {
        public string Name { get; set; } = "";
        public int Minutes { get; set; }
    }
} 