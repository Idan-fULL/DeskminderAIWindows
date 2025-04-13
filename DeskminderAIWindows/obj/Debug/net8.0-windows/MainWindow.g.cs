﻿#pragma checksum "..\..\..\MainWindow.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "1AD1EE0548C271282D70756E867E343791F1DF65"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using DeskminderAI;
using DeskminderAI.Converters;
using DeskminderAI.ViewModels;
using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace DeskminderAI {
    
    
    /// <summary>
    /// MainWindow
    /// </summary>
    public partial class MainWindow : System.Windows.Window, System.Windows.Markup.IComponentConnector, System.Windows.Markup.IStyleConnector {
        
        
        #line 192 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button AddButtonOnly;
        
        #line default
        #line hidden
        
        
        #line 200 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Border TimerSelectionDisplay;
        
        #line default
        #line hidden
        
        
        #line 219 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock TimerValueDisplay;
        
        #line default
        #line hidden
        
        
        #line 229 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox ReminderTextInput;
        
        #line default
        #line hidden
        
        
        #line 244 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button ConfirmTimeButton;
        
        #line default
        #line hidden
        
        
        #line 259 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ItemsControl ActiveRemindersPanel;
        
        #line default
        #line hidden
        
        
        #line 272 "..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal Hardcodet.Wpf.TaskbarNotification.TaskbarIcon TaskbarIcon;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "10.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/DeskminderAI;component/mainwindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\MainWindow.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "10.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            
            #line 20 "..\..\..\MainWindow.xaml"
            ((DeskminderAI.MainWindow)(target)).Loaded += new System.Windows.RoutedEventHandler(this.Window_Loaded);
            
            #line default
            #line hidden
            
            #line 21 "..\..\..\MainWindow.xaml"
            ((DeskminderAI.MainWindow)(target)).MouseDown += new System.Windows.Input.MouseButtonEventHandler(this.Window_MouseDown);
            
            #line default
            #line hidden
            
            #line 22 "..\..\..\MainWindow.xaml"
            ((DeskminderAI.MainWindow)(target)).Closing += new System.ComponentModel.CancelEventHandler(this.Window_Closing);
            
            #line default
            #line hidden
            return;
            case 3:
            this.AddButtonOnly = ((System.Windows.Controls.Button)(target));
            
            #line 195 "..\..\..\MainWindow.xaml"
            this.AddButtonOnly.Click += new System.Windows.RoutedEventHandler(this.AddButtonOnly_Click);
            
            #line default
            #line hidden
            return;
            case 4:
            this.TimerSelectionDisplay = ((System.Windows.Controls.Border)(target));
            
            #line 207 "..\..\..\MainWindow.xaml"
            this.TimerSelectionDisplay.MouseDown += new System.Windows.Input.MouseButtonEventHandler(this.TimerDisplay_MouseDown);
            
            #line default
            #line hidden
            
            #line 208 "..\..\..\MainWindow.xaml"
            this.TimerSelectionDisplay.MouseMove += new System.Windows.Input.MouseEventHandler(this.TimerDisplay_MouseMove);
            
            #line default
            #line hidden
            
            #line 209 "..\..\..\MainWindow.xaml"
            this.TimerSelectionDisplay.MouseUp += new System.Windows.Input.MouseButtonEventHandler(this.TimerDisplay_MouseUp);
            
            #line default
            #line hidden
            return;
            case 5:
            this.TimerValueDisplay = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 6:
            this.ReminderTextInput = ((System.Windows.Controls.TextBox)(target));
            
            #line 238 "..\..\..\MainWindow.xaml"
            this.ReminderTextInput.KeyDown += new System.Windows.Input.KeyEventHandler(this.ReminderTextInput_KeyDown);
            
            #line default
            #line hidden
            return;
            case 7:
            this.ConfirmTimeButton = ((System.Windows.Controls.Button)(target));
            
            #line 249 "..\..\..\MainWindow.xaml"
            this.ConfirmTimeButton.Click += new System.Windows.RoutedEventHandler(this.ConfirmTimeButton_Click);
            
            #line default
            #line hidden
            return;
            case 8:
            this.ActiveRemindersPanel = ((System.Windows.Controls.ItemsControl)(target));
            return;
            case 9:
            this.TaskbarIcon = ((Hardcodet.Wpf.TaskbarNotification.TaskbarIcon)(target));
            
            #line 274 "..\..\..\MainWindow.xaml"
            this.TaskbarIcon.TrayLeftMouseDown += new System.Windows.RoutedEventHandler(this.TaskbarIcon_TrayLeftMouseDown);
            
            #line default
            #line hidden
            return;
            case 10:
            
            #line 280 "..\..\..\MainWindow.xaml"
            ((System.Windows.Controls.MenuItem)(target)).Click += new System.Windows.RoutedEventHandler(this.ExitMenuItem_Click);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "10.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        void System.Windows.Markup.IStyleConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 2:
            
            #line 146 "..\..\..\MainWindow.xaml"
            ((System.Windows.Controls.Grid)(target)).MouseEnter += new System.Windows.Input.MouseEventHandler(this.ReminderContainer_MouseEnter);
            
            #line default
            #line hidden
            
            #line 146 "..\..\..\MainWindow.xaml"
            ((System.Windows.Controls.Grid)(target)).MouseLeave += new System.Windows.Input.MouseEventHandler(this.ReminderContainer_MouseLeave);
            
            #line default
            #line hidden
            break;
            }
        }
    }
}

