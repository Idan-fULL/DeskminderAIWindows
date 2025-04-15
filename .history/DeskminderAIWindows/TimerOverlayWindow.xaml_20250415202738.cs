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
using WPFMessageBox = System.Windows.MessageBox;
using DeskminderAI.Models;

namespace DeskminderAI
{
    public partial class TimerOverlayWindow : Window, INotifyPropertyChanged
    {
        private readonly MainWindow _mainWindow;

        // Variables for drag duration interface
        private bool _isDraggingDuration = false;
        private double _initialDragPosition;
        private int _initialDragMinutes;
        private int _initialDragSeconds = 0;
        private const int MIN_DURATION = 1;  // Minimum 1 minute
        private const int MAX_DURATION = 120; // Maximum 120 minutes (2 hours)
        private const double DRAG_SCALE_FACTOR = 0.8; // Even slower dragging
        private const double SECONDS_SCALE_FACTOR = 0.2; // Slower dragging for seconds
        private const double SECONDS_ACTIVATION_DELAY = 800; // ms to wait before showing seconds
        private System.Windows.Threading.DispatcherTimer? _secondsTimer;
        private DateTime _lastDragTime;
        private bool _secondsMode = false; // Flag for seconds editing mode
        
        // Scaling function for display width based on minutes and seconds
        private double ScaleWidthByMinutes(int minutes, int seconds = 0)
        {
            // רוחב בסיס קטן יותר
            double baseWidth = 120;
            
            // גידול לינארי איטי יותר
            double linearComponent = minutes * 0.8;
            
            // גידול לוגריתמי שמתמתן במספרים גדולים
            double logComponent = Math.Log(minutes + 1) * 20;
            
            // תוספת לשניות
            double secondsComponent = seconds > 0 ? (seconds / 60.0) * 15 : 0;
            
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

        public TimerOverlayWindow(MainWindow mainWindow)
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

                _mainWindow = mainWindow;
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
                    _initialDragSeconds = Seconds;
                    
                    if (TimerSelectionDisplay != null)
                    {
                        TimerSelectionDisplay.CaptureMouse();
                    }
                    
                    // במצב שניות, לא מאפסים את השניות בהתחלת גרירה חדשה
                    if (!_secondsMode)
                    {
                        Seconds = 0;
                        HideSecondsDisplay();
                    }
                    
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
                    
                    // בדיקה אם אנחנו במצב עריכת שניות
                    if (_secondsMode)
                    {
                        // במצב שניות, הגרירה משפיעה רק על השניות
                        // השתמש בפקטור סקיילינג ייעודי לשניות
                        double secondsDelta = deltaX / SECONDS_SCALE_FACTOR;
                        int newSeconds = _initialDragSeconds + (int)secondsDelta;
                        
                        // וידוא ששניות בטווח תקין
                        newSeconds = Math.Max(0, Math.Min(59, newSeconds));
                        
                        // עדכון מודל
                        if (Seconds != newSeconds)
                        {
                            Seconds = newSeconds;
                            
                            // עדכון רוחב עם אנימציה
                            double newWidth = ScaleWidthByMinutes(Minutes, newSeconds);
                            var animation = new DoubleAnimation
                            {
                                To = newWidth,
                                Duration = TimeSpan.FromSeconds(0.1),
                                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                            };
                            TimerSelectionDisplay.BeginAnimation(FrameworkElement.MinWidthProperty, animation);
                        }
                    }
                    else
                    {
                        // התנהגות מקורית - דקות שלמות בלבד
                        int newMinutes = _initialDragMinutes + (int)(deltaX / DRAG_SCALE_FACTOR);
                        
                        // וידוא שהערך בטווח
                        newMinutes = Math.Max(MIN_DURATION, Math.Min(MAX_DURATION, newMinutes));
                        
                        if (Minutes != newMinutes)
                        {
                            Minutes = newMinutes;
                            Seconds = 0; // איפוס שניות כשדקות משתנות
                            
                            // עדכון רוחב עם אנימציה
                            double newWidth = ScaleWidthByMinutes(newMinutes);
                            var animation = new DoubleAnimation
                            {
                                To = newWidth,
                                Duration = TimeSpan.FromSeconds(0.2),
                                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                            };
                            TimerSelectionDisplay.BeginAnimation(FrameworkElement.MinWidthProperty, animation);
                        }
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
                    
                    // אם עדיין לא במצב שניות, עובר למצב שניות
                    if (!_secondsMode)
                    {
                        _secondsMode = true;
                        
                        // מציג את השניות באופן מידי
                        ShowSecondsDisplay();
                        
                        // הוספת הנחיה חזותית (אופציונלי)
                        Console.WriteLine("Switched to seconds mode");
                    }
                    
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
                    // אם כבר במצב הזנת טקסט, יוצרים את התזכורת
                    CreateReminderFromInput();
                }
                else
                {
                    // יוצאים ממצב שניות ועוברים למצב הזנת טקסט
                    _secondsMode = false;
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
            // Hide timer elements and show text input
            TimerDisplayPanel.Visibility = Visibility.Collapsed;
            ReminderTextInput.Visibility = Visibility.Visible;
            
            // Set default reminder name (in Hebrew: "תזכורת")
            ReminderTextInput.Text = "תזכורת";
            ReminderTextInput.SelectAll();
            ReminderTextInput.Focus();
            
            // Display the selected time at the bottom of the window
            string timeFormat = Seconds > 0 ? 
                $"{Minutes} דקות ו-{Seconds} שניות" : 
                $"{Minutes} דקות";
            
            if (Minutes == 1 && Seconds == 0)
            {
                timeFormat = "דקה אחת";
            }
            else if (Minutes == 1)
            {
                timeFormat = $"דקה אחת ו-{Seconds} שניות";
            }
            
            SelectedTimeDisplay.Text = timeFormat;
            SelectedTimeDisplay.Visibility = Visibility.Visible;
            
            // Update button to show check mark for confirming text
            ConfirmTimeButton.Style = (Style)FindResource("ConfirmTextButtonStyle");
            
            // Adjust window width to accommodate text entry
            int newWidth = 200;
            Width = newWidth;
            
            // Set focus to the text input
            ReminderTextInput.Focus();
        }
        
        private void CreateReminderFromInput()
        {
            try
            {
                // Get reminder text from input, or use a default if empty
                string reminderName = "תזכורת";
                if (!string.IsNullOrWhiteSpace(ReminderTextInput.Text) && 
                    ReminderTextInput.Text != "תזכורת")
                {
                    reminderName = ReminderTextInput.Text.Trim();
                }
                
                // יצירת תזכורת עם דקות ושניות
                var reminder = new Reminder
                {
                    Name = reminderName,
                    Minutes = Minutes,
                    CreatedAt = DateTime.Now,
                    EndTime = DateTime.Now.AddMinutes(Minutes).AddSeconds(Seconds)
                };
                
                // חישוב הזמן הנותר
                reminder.UpdateTimeLeft();
                
                // שליחת אירוע ליצירת התזכורת בחלון הראשי
                ReminderCreated?.Invoke(this, new ReminderCreatedEventArgs
                {
                    Name = reminder.Name,
                    Minutes = Minutes,
                    Seconds = Seconds
                });
                
                // איפוס מצב החלון הראשי
                if (_mainWindow != null)
                {
                    _mainWindow.BackgroundOverlay.Visibility = Visibility.Collapsed;
                    _mainWindow.MainContentGrid.Effect = null;
                }
                
                // סגירת החלון הנוכחי
                Close();
            }
            catch (Exception ex)
            {
                WPFMessageBox.Show($"Error creating reminder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            try
            {
                // Reset main window state
                if (_mainWindow != null)
                {
                    _mainWindow.BackgroundOverlay.Visibility = Visibility.Collapsed;
                    _mainWindow.MainContentGrid.Effect = null;
                }
                
                // Close this window
                Close();
            }
            catch (Exception ex)
            {
                WPFMessageBox.Show($"Error cancelling timer: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            // תמיד מציג את השניות לאחר שחרור העכבר
            if (!_isDraggingDuration)
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
            
            // וידוא שהדגל של מצב שניות מעודכן
            _secondsMode = true;
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
            
            // וידוא שהדגל של מצב שניות מעודכן
            _secondsMode = false;
        }

        private void ResetTimerToDefault()
        {
            // Reset to time selection mode
            TimerDisplayPanel.Visibility = Visibility.Visible;
            ReminderTextInput.Visibility = Visibility.Collapsed;
            SelectedTimeDisplay.Visibility = Visibility.Collapsed;
            
            // Reset time values
            Minutes = DefaultMinutes;
            Seconds = 0;
            
            // Reset button style to default
            ConfirmTimeButton.Style = (Style)FindResource("ConfirmButtonStyle");
            
            // Reset window width to default
            Width = 180;
            
            IsDragging = false;
            IsTextMode = false;
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
        public int Seconds { get; set; }
    }
} 