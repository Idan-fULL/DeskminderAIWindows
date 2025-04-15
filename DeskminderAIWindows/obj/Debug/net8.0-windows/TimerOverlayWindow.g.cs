﻿#pragma checksum "..\..\..\TimerOverlayWindow.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "9729406D3721C2F0A4534F6E1FCD7563BCB10E77"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using DeskminderAI;
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
    /// TimerOverlayWindow
    /// </summary>
    public partial class TimerOverlayWindow : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 109 "..\..\..\TimerOverlayWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Border TimerSelectionDisplay;
        
        #line default
        #line hidden
        
        
        #line 135 "..\..\..\TimerOverlayWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.StackPanel TimerDisplayPanel;
        
        #line default
        #line hidden
        
        
        #line 137 "..\..\..\TimerOverlayWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock TimerValueDisplay;
        
        #line default
        #line hidden
        
        
        #line 143 "..\..\..\TimerOverlayWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock SecondsDisplay;
        
        #line default
        #line hidden
        
        
        #line 150 "..\..\..\TimerOverlayWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox ReminderTextInput;
        
        #line default
        #line hidden
        
        
        #line 170 "..\..\..\TimerOverlayWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button ConfirmTimeButton;
        
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
            System.Uri resourceLocater = new System.Uri("/DeskminderAI;component/timeroverlaywindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\TimerOverlayWindow.xaml"
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
            
            #line 20 "..\..\..\TimerOverlayWindow.xaml"
            ((DeskminderAI.TimerOverlayWindow)(target)).KeyDown += new System.Windows.Input.KeyEventHandler(this.Window_KeyDown);
            
            #line default
            #line hidden
            return;
            case 2:
            
            #line 106 "..\..\..\TimerOverlayWindow.xaml"
            ((System.Windows.Shapes.Rectangle)(target)).MouseDown += new System.Windows.Input.MouseButtonEventHandler(this.Rectangle_MouseDown);
            
            #line default
            #line hidden
            return;
            case 3:
            this.TimerSelectionDisplay = ((System.Windows.Controls.Border)(target));
            
            #line 114 "..\..\..\TimerOverlayWindow.xaml"
            this.TimerSelectionDisplay.MouseDown += new System.Windows.Input.MouseButtonEventHandler(this.TimerDisplay_MouseDown);
            
            #line default
            #line hidden
            
            #line 115 "..\..\..\TimerOverlayWindow.xaml"
            this.TimerSelectionDisplay.MouseMove += new System.Windows.Input.MouseEventHandler(this.TimerDisplay_MouseMove);
            
            #line default
            #line hidden
            
            #line 116 "..\..\..\TimerOverlayWindow.xaml"
            this.TimerSelectionDisplay.MouseUp += new System.Windows.Input.MouseButtonEventHandler(this.TimerDisplay_MouseUp);
            
            #line default
            #line hidden
            return;
            case 4:
            this.TimerDisplayPanel = ((System.Windows.Controls.StackPanel)(target));
            return;
            case 5:
            this.TimerValueDisplay = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 6:
            this.SecondsDisplay = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 7:
            this.ReminderTextInput = ((System.Windows.Controls.TextBox)(target));
            
            #line 166 "..\..\..\TimerOverlayWindow.xaml"
            this.ReminderTextInput.KeyDown += new System.Windows.Input.KeyEventHandler(this.ReminderTextInput_KeyDown);
            
            #line default
            #line hidden
            return;
            case 8:
            this.ConfirmTimeButton = ((System.Windows.Controls.Button)(target));
            
            #line 177 "..\..\..\TimerOverlayWindow.xaml"
            this.ConfirmTimeButton.Click += new System.Windows.RoutedEventHandler(this.ConfirmTimeButton_Click);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

