   M S B A M L   `   `   `  ���� O  Kmscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e0891&clr-namespace:System;assembly=mscorlibSystem  X TXceed.Wpf.Toolkit, Version=2.1.0.0, Culture=neutral, PublicKeyToken=3e4669d2f30244f45clr-namespace:Xceed.Wpf.ToolkitXceed.Wpf.Toolkit \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089%   Xceed.Wpf.Toolkit.BusyIndicator  �� 
TargetType �� Property �� Value �� 
TargetType'  !System.Windows.VisualStateManager   VisualStateGroups%  System.Windows.VisualStateGroup   States   System.Windows.VisualState   
Storyboard g� 	BeginTime g� Duration	 �� 
TargetName
 �� TargetProperty R� KeyTime R� Value  System.Windows.Visibility ��Name ��Name    OverlayStyle ��Name �� CornerRadius �� 
StartPoint �� EndPoint � Color � Offset � RowDefinitions    BusyContent    BusyContentTemplate    ProgressBarStyle�� D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    :x,http://schemas.microsoft.com/winfx/2006/xaml     .sys&clr-namespace:System;assembly=mscorlib  )localclr-namespace:Xceed.Wpf.Toolkit  %�  '         �� "    .���  �@  BusyContent$ Please wait...����  � �� .  ��  � |� .  ��  �@  OverlayStyle �� "  ��.���  � ��
 �������  �@J�Opacity$	 0.5Q���  �@  ProgressBarStyle �� "  �.���  �@�IsIndeterminate . ��  � ��$ 15����  � ��$ 8,0,8,8q���  �@  DisplayAfter$ 
00:00:00.1h���  � ��$ Stretch=���  � ��$ Stretch=���  � ��$ Stretch=���  � ��$ Stretch=���  � �� �� "   .R��    VisibilityStates��.   
Hidden��. �� .��T� $ 00:00:00X�$ 00:00:00.001D�$	 busycontent��$
 (UIElement.Visibility)�..�i� $ 00:00:00��  	Collapsed=�T� $ 00:00:00X�$ 00:00:00.001D�$	 overlay��$
 (UIElement.Visibility)�..�i� $ 00:00:00��  	Collapsed=�  Visible��. �� .��T� $ 00:00:00X�$ 00:00:00.001D�$	 busycontent��$
 (UIElement.Visibility)�..�i� $ 00:00:00��  Visible=�T� $ 00:00:00X�$ 00:00:00.001D�$	 overlay��$
 (UIElement.Visibility)�..�i� $ 00:00:00��  Visible=�  BusyStatusStates��.   Idle��. �� .��T� $ 00:00:00X�$ 00:00:00.001D�$	 content��$
 (Control.IsEnabled)�..�i� $ 00:00:00�� ��True��  Busy��. �� .��T� $ 00:00:00X�$ 00:00:00.001D�$	 content��$
 (Control.IsEnabled)�..�i� $ 00:00:00�� ��	False��.+��� $ content��#��z��#��z��#��z��#��z����.  |�.  �� $ overlay��#��z �� $ busycontent����� $��Center=�$��Center=�.+��� 
�������$��1q�$ 2������ $ 0.5,0&�$ 0.5,1&�.��� $ 	#FFA3AEB9��$ 0Q�� $ 	#FF8399A9��$ 0.375Q�� $ 	#FF718597��$ 0.375Q�� $ 	#FF617584��$ 1Q�._��� $	 1.5��$��1q����� $ 0.5,0&�$ 0.5,1&�.��� $ 	#FFF6F8F9��$
 0.02Q�� $ 	#FFB8B8B8��$ 0.996Q�._�� $	��150�� �� �� $
��Auto �.+��� #��z #��z $��8q�� $��1��#��z 