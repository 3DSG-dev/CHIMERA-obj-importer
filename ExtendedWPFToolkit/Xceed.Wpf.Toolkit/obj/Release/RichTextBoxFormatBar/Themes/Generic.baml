   M S B A M L   `   `   `  ���� X  TXceed.Wpf.Toolkit, Version=2.1.0.0, Culture=neutral, PublicKeyToken=3e4669d2f30244f45clr-namespace:Xceed.Wpf.ToolkitXceed.Wpf.Toolkit  U/clr-namespace:Xceed.Wpf.Toolkit.Core.Converters!Xceed.Wpf.Toolkit.Core.Converters  \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089G    AXceed.Wpf.Toolkit.Core.Converters.ColorToSolidColorBrushConverter #  ColorToSolidColorBrushConverter  ThumbControlTemplate  �� 
TargetType � Cursor � ToolTip J� SnapsToDevicePixels �� Stretch �� StrokeDashArray	 �� X1	 �� X2 �� Color  MouseOverBorderBrush  MouseOverBackgroundBrush	 �� 
StartPoint
 �� EndPoint � Offset � Color  CheckedBorderBrush  CheckedBackgroundBrush  PressedBorderBrush  PressedBackgroundBrush  FormatBarToggleButtonStyle �� 
TargetType �� Property �� Value ��Name �� CornerRadius �� Padding ��Name ��Name �� BitmapScalingMode �� Triggers P� Property P� Value �� 
TargetName ]� 
Conditions �� Property �� Value 	 FontColorButtonStyle � ColumnDefinitions �� RelativeSource �� Mode  �� AncestorType! �� 	Converter !
 FontBackgrounColorButtonStyle   richTextBoxFormatBarTemplate,   &Xceed.Wpf.Toolkit.RichTextBoxFormatBar" � 	Resources# �� BasedOn$ � RowDefinitions% n�Name& �� Orientation' ��Name( �� 
IsEditable) d�Name* �� Path.  (System.Windows.Documents.EditingCommands+   ToggleItalic,   ToggleUnderline- 	�Name.   	AlignLeft/   AlignCenter0   
AlignRight#   Xceed.Wpf.Toolkit.ColorPicker1  Name2   ButtonStyle3  -System.Windows.Media.Effects.DropShadowEffect3   
BlurRadius4   Opacity�� D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    :x,http://schemas.microsoft.com/winfx/2006/xaml     )localclr-namespace:Xceed.Wpf.Toolkit  8conv/clr-namespace:Xceed.Wpf.Toolkit.Core.Converters  %N$  &	        &	      &	 d    &	 y    &	 O    &	 d    &	 �    &	     &	 �    7  7  7  7  7  7  &		 ^
    7   7  7  7  7  7  7  &	
 &    7   7  7  7  7  7  7  &	 �    7�7  7  7  7  7  7  7  7  7  7 
 7 	 '  �"    7     �� "  n�.R��� 
������ $
 Hand��$ Click to Drag��._��� $��Center=�$��75��.+���  . $
 Fill=�$	 1,2R�$��1��$ 0��$ 1��$��.5q����� $
 Gray����  . $
 Fill=�$	 1,2R�$��1��$ 0��$ 1��$��.5q����� $
 Gray����  . $
 Fill=�$	 1,2R�$��1��$ 0��$ 1��$��.5q����� $
 Gray���� $ 	#FFFFB700���� $		 0,0&�$	
 0,1&�.��� $ 0Q�$ 	#FFFEFBF4��� $
 0.19Q�$ 	#FFFDE7CE��� $
 0.39Q�$ 	#FFFDDEB8��� $
 0.39Q�$ 	#FFFFCE6B��� $
 0.79Q�$ 	#FFFFDE9A��� $ 1Q�$ 	#FFFFEBAA���� $ 	#FFC29B29���� $		 0,0&�$	
 0,1&�.��� $ 0Q�$ 	#FFFFDCA0��� $
 0.18Q�$ 	#FFFFD692��� $
 0.39Q�$ 	#FFFFC45D��� $ 1Q�$ 	#FFFFD178���� $ 	#FFC29B29���� $		 0,0&�$	
 0,1&�.��� $ 0Q�$ 	#FFE3C085��� $
 0.19Q�$ 	#FFF4CC89��� $
 0.36Q�$ 	#FFF5C777��� $
 0.36Q�$ 	#FFF5BB56��� $
 0.79Q�$ 	#FFF4CE9A��� $ 1Q�$ 	#FFF3E28D���� " d�.���  � ��
 ���� ��  � ��
 ���� ��  � ��$ 1q���  �@�FocusVisualStyle Z� ��  � ��$ 22����  � ��$ Center=���  �@^�InitialShowDelay$	 900����  �@^�ShowDuration$ 20000����  �@^�BetweenShowDelay$ 0����  � �� �� "  d�.R��  . .+��� $ OuterBorder��#��z��#��z��#��z��$ 2���� $ MiddleBorder��
������ #��z��
������ $ 2��._��� $ InnerBorder��
������ #��z��
������ $ 2��# z��._��� $ 
StackPanel��#��z��.+��� $ Content��#��z��$��1q�$ NearestNeighbor=�$��Center=�#��z�� P�  �@J�IsMouseOver . . ��� $ OuterBorder��8    � ���� $ OuterBorder��8   � ���� $ InnerBorder�� � �� �	#80FFFFFFP�  � {� .  . ��� $ Content�� �@J�Opacity$	 0.5Q��� $ OuterBorder�� � �� �	#FF9E9E9EP�  �@d�	IsChecked$
 TrueY�. ��� $ OuterBorder��8   � ���� $ OuterBorder��8   � ���� $ InnerBorder�� � �� �� $	
 0,1&�$		 0,0&�.��� $ 	#FFE7CBAD��$ 0Q�� $ 	#FFF7D7B5��$	 0.1Q�� $ 	#FFFFD38C��$
 0.36Q�� $ 	#FFFFC75A��$
 0.36Q�� $ 	#FFFFEFA5��$ 1Q�P�  � �� . . ��� $ OuterBorder��8   � ���� $ OuterBorder��8   � ���� $ InnerBorder�� � ��
 ���� ]�  ��  �@d�	IsChecked$
 TrueY���  �@J�IsMouseOver . ./��� $ MiddleBorder�� � �� �� $	
 0,1&�$		 0,0&�.��� $ 	#40FFFEFE��$ 0Q�� $ 	#40FFFEFE��$
 0.39Q�� $ 	#20FFCE68��$
 0.39Q�� $ 	#20FFCE68��$
 0.69Q�� $ 	#10FFFFFF��$ 1Q��� " d�.���  � ��
 ���� ��  � ��
 ���� ��  � ��$ 1q���  �@�FocusVisualStyle Z� ��  � ��$ 22����  � ��$ Center=���  �@^�InitialShowDelay$	 900����  �@^�ShowDuration$ 20000����  �@^�BetweenShowDelay$ 0����  � �� �� "  d�.R��  . .+��� $ OuterBorder��#��z��#��z��$ 2���� $ MiddleBorder��
������ #��z��
������ $ 2��._��� $ InnerBorder��
������ #��z��
������ $ 2��# z��._��  �� $��* ��� .+�� .+��� $%��../Images/FontColorPicker16.png��$��16��$��16���� $��1��$��4��$��0,12,0,0q����� *SelectedColor+ �� $ FindAncestor=�$  local:ColorPickerL�8!   F� $��1��$��7��$��4�������    @B �    @B �        ��- ��- ���-     ��- ��- ���- @B  	=  	= � 	= @B  	=  	= � 	=     ��j ��j ���j     ��j ��j ���j @B ��[ ��[ Ӏ�[ @B ��[ �� ��[ �� Ӏ�[ �� @KL �� @KL �� �@KL �� @KL ��- @KL ��- �@KL ��-  	= ��-  	= ��- � 	= ��-  	=  	=  	=  	= � 	=  	= ��-  	= ��-  	= ���-  	= ��- ��- ��- ��- ���- ��- �� ��- �� ��- Ӏ� ��- �� �� �� �� Ӏ� �� �� �� �@B �� �@B @B 	���	#FF000000 P�  �@J�IsMouseOver . . ��� $ OuterBorder��8   � ���� $ OuterBorder��8   � ���� $ InnerBorder�� � �� �	#80FFFFFFP�  � {� .  . ��� $ OuterBorder�� � �� �	#FF9E9E9EP�  �@d�	IsChecked$
 TrueY�. ��� $ OuterBorder��8   � ���� $ OuterBorder��8   � ���� $ InnerBorder�� � �� �� $	
 0,1&�$		 0,0&�.��� $ 	#FFE7CBAD��$ 0Q�� $ 	#FFF7D7B5��$	 0.1Q�� $ 	#FFFFD38C��$
 0.36Q�� $ 	#FFFFC75A��$
 0.36Q�� $ 	#FFFFEFA5��$ 1Q�P�  � �� . . ��� $ OuterBorder��8   � ���� $ OuterBorder��8   � ���� $ InnerBorder�� � ��
 ���� ]�  ��  �@d�	IsChecked$
 TrueY���  �@J�IsMouseOver . ./��� $ MiddleBorder�� � �� �� $	
 0,1&�$		 0,0&�.��� $ 	#40FFFEFE��$ 0Q�� $ 	#40FFFEFE��$
 0.39Q�� $ 	#20FFCE68��$
 0.39Q�� $ 	#20FFCE68��$
 0.69Q�� $ 	#10FFFFFF��$ 1Q��� " d�.���  � ��
 �������  � ��
 ���� ��  � ��$ 1q���  �@�FocusVisualStyle Z� ��  � ��$ 22����  � ��$ Center=���  �@^�InitialShowDelay$	 900����  �@^�ShowDuration$ 20000����  �@^�BetweenShowDelay$ 0����  � �� �� "  d�.R��  . .+��� $ OuterBorder��#��z��#��z��$ 2���� $ MiddleBorder��
������ #��z��
������ $ 2��._��� $ InnerBorder��
������ #��z��
������ $ 2��# z��._��  �� $��* ��� .+�� .+��� $.��(../Images/TextHighlightColorPicker16.png��$��16��$��16���� $��1��$��4��$��0,12,0,0q����� *SelectedColor+ �� $ FindAncestor=�$  local:ColorPickerL�8!   F� $��1��$��7��$��4�������    @B �    @B �        ��- ��- ���-     ��- ��- ���- @B  	=  	= � 	= @B  	=  	= � 	=     ��j ��j ���j     ��j ��j ���j @B ��[ ��[ Ӏ�[ @B ��[ �� ��[ �� Ӏ�[ �� @KL �� @KL �� �@KL �� @KL ��- @KL ��- �@KL ��-  	= ��-  	= ��- � 	= ��-  	=  	=  	=  	= � 	=  	= ��-  	= ��-  	= ���-  	= ��- ��- ��- ��- ���- ��- �� ��- �� ��- Ӏ� ��- �� �� �� �� Ӏ� �� �� �� �@B �� �@B @B 	���	#FF000000 P�  �@J�IsMouseOver . . ��� $ OuterBorder��8   � ���� $ OuterBorder��8   � ���� $ InnerBorder�� � �� �	#80FFFFFFP�  � {� .  . ��� $ OuterBorder�� � �� �	#FF9E9E9EP�  �@d�	IsChecked$
 TrueY�. ��� $ OuterBorder��8   � ���� $ OuterBorder��8   � ���� $ InnerBorder�� � �� �� $	
 0,1&�$		 0,0&�.��� $ 	#FFE7CBAD��$ 0Q�� $ 	#FFF7D7B5��$	 0.1Q�� $ 	#FFFFD38C��$
 0.36Q�� $ 	#FFFFC75A��$
 0.36Q�� $ 	#FFFFEFA5��$ 1Q�P�  � �� . . ��� $ OuterBorder��8   � ���� $ OuterBorder��8   � ���� $ InnerBorder�� � ��
 ���� ]�  ��  �@d�	IsChecked$
 TrueY���  �@J�IsMouseOver . ./��� $ MiddleBorder�� � �� �� $	
 0,1&�$		 0,0&�.��� $ 	#40FFFEFE��$ 0Q�� $ 	#40FFFEFE��$
 0.39Q�� $ 	#20FFCE68��$
 0.39Q�� $ 	#20FFCE68��$
 0.69Q�� $ 	#10FFFFFF��$ 1Q��� "   " ���� '��       " ��8#   .R��� $ 3��$��1q�
�������
�������._�� $��5,0,5,5q�$ �� $
��Auto ��� $
��Auto �.+�n� $% _dragWidget��$��10��8�� �� $��1��.+��� $& 
Horizontal=�.+��� $' _cmbFontFamilies��( . $	��100��$ Font Family���� $' _cmbFontSizes��( . $��43��$ 	Font Size���� d� $) _btnBullets��8�� $#��EditingCommands.ToggleBullets��$ Bullets������  �� *TemplatedParent+$* Target�.���� $��../Images/Bullets16.png��d� $) _btnNumbers��8�� $%��EditingCommands.ToggleNumbering��$ 	Numbering������  �� *TemplatedParent+$* Target�.���� $��../Images/Numbering16.png���� $& 
Horizontal=�$��0,3,0,0q�.+�d� $) _btnBold��8�� $ ��EditingCommands.ToggleBold��$
 Bold������  �� *TemplatedParent+$* Target�.���� $��../Images/Bold16.png��d� $) 
_btnItalic��8�� #��Z+ $ Italic������  �� *TemplatedParent+$* Target�.���� $��../Images/Italic16.png��d� $) _btnUnderline��8�� #��Z, $ 	Underline������  �� *TemplatedParent+$* Target�.���� $��../Images/Underline16.png���� 	� $- _btnAlignLeft��8�� #��Z. $ 
Align Left������  �� *TemplatedParent+$* Target�.���� $��../Images/LeftAlign16.png��	� $- _btnAlignCenter��8�� #��Z/ $ Align Center������  �� *TemplatedParent+$* Target�.���� $!��../Images/CenterAlign16.png��	� $- _btnAlignRight��8��	 #��Z0 $ Align Right������  �� *TemplatedParent+$* Target�.���� $ ��../Images/RightAlign16.png����   $1 _cmbFontBackgroundColor��$��0q�82 
 $ Text Highlight Color��  $1 _cmbFontColor��$��0q�82  $ 
Font Color���� "  .���  � ��8   ��  �@J�Effect   $3 5Q�$	4 .25Q���  � ��
 ���� ��  � �� .  