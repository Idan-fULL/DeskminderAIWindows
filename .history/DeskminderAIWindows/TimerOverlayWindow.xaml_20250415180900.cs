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
        private const double DRAG_SCALE_FACTOR = 0.15; // More sensitive dragging for better control
        
        // Scaling function for display width based on minutes
        private double ScaleWidthByMinutes(int minutes)
        {
            // Exponential growth formula to make higher values appear wider
            // Base width of 180 pixels for 1 minute (matches the MinWidth in XAML)
            // Will grow more dramatically as minutes increase
            return 180 + Math.Log10(minutes + 1) * 80; // Reduced growth factor for more granular control
        }

        public int Minutes { get; set; } = 5;
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
                
                // Initialize with default value
                Minutes = 5;
                
                // Set the initial width based on the minutes
                TimerSelectionDisplay.MinWidth = ScaleWidthByMinutes(Minutes);
                
                // Show the confirm button from the start
                ConfirmTimeButton.Visibility = Visibility.Visible;
                
                // Ensure window is visible and on top
                Show();
                Activate();
                Topmost = true;
                
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
                // Only respond to mouse down if we're in timer selection mode (not text input mode)
                if (ReminderTextInput.Visibility != Visibility.Visible)
                {
                    Console.WriteLine("TimerDisplay_MouseDown event triggered");
                    
                    if (e.ChangedButton == MouseButton.Left)
                    {
                        _isDraggingDuration = true;
                        _initialDragPosition = e.GetPosition(this).X;
                        _initialDragMinutes = Minutes;
                        
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
                    
                    // Update the minutes and trigger property change
                    if (Minutes != newMinutes)
                    {
                        Minutes = newMinutes;
                        OnPropertyChanged(nameof(Minutes));
                        
                        // Update the display text directly
                        if (TimerValueDisplay != null)
                        {
                            TimerValueDisplay.Text = $"{Minutes} min";
                        }
                        
                        // Adjust the width of the timer display based on the minutes value
                        if (TimerSelectionDisplay != null)
                        {
                            double newWidth = ScaleWidthByMinutes(newMinutes);
                            TimerSelectionDisplay.MinWidth = newWidth;
                            
                            Console.WriteLine($"Adjusted width to {newWidth}px for {newMinutes} minutes");
                        }
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
                    
                    Console.WriteLine($"Duration drag ended, final minutes: {Minutes}");
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