   M S B A M L   `   `   `  ����G  CDeskminderAI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null+clr-namespace:DeskminderAIDeskminderAI  5   	   %    DeskminderAI.TimerOverlayWindow   5   	   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35H 9http://schemas.microsoft.com/winfx/2006/xaml/presentation      5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   ,localclr-namespace:DeskminderAI     5   	   -   5          � Title$  Timer Selection��5   	   #��Z��5	   	   #��Z��5
   	    � WindowStyle$
 None=�5   	    � 
ResizeMode$ NoResize=�5   	    � AllowsTransparency$
 True��5   	   ���	#80000000 � WindowStartupLocation$ Manual=�5   	    � Left$ 0��5   	   
 � Top$ 0��5   	    � Topmost$
 True��5   	    � ShowInTaskbar$ False��5   	   	 � 	Resources	 5         TimerContainerStyle
 �� 
TargetType �� Property �� Value3  -System.Windows.Media.Effects.DropShadowEffect   ShadowDepth   	Direction   Color   Opacity   
BlurRadius �� Triggers P� Property P� Value  TimerTextBrush �� Color  TimerTextStyle  SecondsTextStyle  ConfirmButtonStyle �� 
TargetType J� Effect2  ,System.Windows.Media.Animation.QuadraticEase  EaseOutQuad7  1System.Windows.Media.Animation.EasingFunctionBase   
EasingMode��%D  &	        &	 �    &	 �    &	 7    &	 |    &	 �    �� 5   
   "
 ��6,   .�5      ��  � �� �	#CC0D47A16+   �� 5       �@��CornerRadius$ 12��6-   �� 5       � ��$ 34��6'   �� 5       �@J�Effect 5        5      $ 2Q�6'   $	 270Q�5   %   $ Black��5    %   $	 0.4Q�5!   %   $ 6Q�5"   %   5$       5%      P� 5&       �@J�IsMouseOver$
 True��61   . �5'      ��  � �� �	#CC1565C063   5(      5*      �� 5-   
   $ White��61   �� 5/   
   "
 ��6'   .�50      ��  � ��
 �����6+   �� 51       � ��$ Bahnschrift)�6+   �� 52       � ��$ SemiBold"�6+   �� 53       � ��$ 14(�6)   �� 54       � ��$ Center=�62    55      P� 56       �@J�IsMouseOver$
 True��61   . �57      ��  � �� �#E0E0E063   58      5:      �� 5=   
   "
 ��6)   .�5>      ��  � �� �#BBDEFB6+   �� 5?       � ��$ Bahnschrift)�6+   �� 5@       � ��$ SemiBold"�6+   �� 5A       � ��$ 12(�6)   �� 5B       � ��$ 4,0,0,0q�6'   �� 5C       � ��$ Center=�62   �� 5D       �@J�Opacity$ 0Q�6(   5E      �� 5G   
   "
 ��6+   .�5H      ��  � ��$ 24��6&   �� 5I       � ��$ 24��6'   �� 5J       � �� �	#CC0D47A16+   �� 5K       � ��
 ���� 6,   �� 5L       � ��
 �����6+   �� 5M       � �� 5N      �� 5O      " ��6&   .R�5P      � .+�5Q      A� #��z��6&    5R   "     5S   &   $ 2Q�67   $	 270Q�6G   $ Black��6W   $	 0.4Q�6e   $ 4Q�6s   5U      �� 5V      $	��✓��6(   $��14(�5W   (   $
��Bold"�5X   (   #��z��5Y   (   $��Center=�5Z   (   $��Center=�5[   (   $��0,1,0,0q�5\   (   5]      5^      5`      5a        5d   
   $ EaseOut=�6,   5e      .��5h      � .+�5j   
   �� -   5        ���	#800000005j   %   �� 5m   
   -   5         ��Name$ TimerSelectionDisplay��5m      #��[  5n      $��Center=�5o      $��Center=�5p      $	��180��5q      z�5u      V� 5v      .~�5w      �� T� 5x      5y      ._�5|      � $
��12,0q�6    � ColumnDefinitions 5}      �� 5~      $��* �6'   �� 5      $
��Auto �6'   �� 5�      $
��Auto �6'   .+�5�      �� -   5         ��Name$ TimerDisplayPanel��5�      $��0��5�       �� Orientation$ 
Horizontal=�5�      $
��Left=�5�      $y�Visible=�5�      .+�5�      �� -   5         ��Name$ TimerValueDisplay��5�       #��[ 5�      $y�Visible=�5�      ��5�      �� *	Minutes+ �� StringFormat$	 {0}��5�      �� 5�      $
�� min��6    #��[ 5�      �� 5�      -   5        $ SecondsDisplay��5�       #��[ 5�      $y�Visible=�5�      ��5�      �� *	Seconds+$  {0} sec��5�      �� 5�      $�� (��6    #��[ 5�      $��5,0,0,0q�5�      $��14(�5�      �� 5�      -   5        $ EndTimeDisplay��5�       #��[ 5�      $��14(�5�      $y�Visible=�5�      ��5�      �� *EndTimeString+5�      �� 5�      $��)��6    #��[ 5�      $��14(�5�      5�      �� 5�      -   5         ��Name$ ReminderTextInput��5�      $��0��5�      $��2��5�      
������ 
�������
������ $��0q�5�      $	��110��5�      $��14(�5�      $��Bahnschrift)�5�      $��SemiBold"�5�      $��Center=�5�      $��Center=�5�      $��Center=�5�      $��Center=�5�        �� TextAlignment$  Center=�5�      ! �� 
CaretBrush
! �����" �� SelectionBrush" �#1565C0$y�	Collapsed=�5�      �� 5�      -	   5        # ��Name$# ConfirmTimeButton��5�      $��2��5�      #��[ 5�      $��24��5�      $��24��5�      $��8,0,0,0q�5�      $y�Visible=�5�      5�      5�      5�      5�      5�      