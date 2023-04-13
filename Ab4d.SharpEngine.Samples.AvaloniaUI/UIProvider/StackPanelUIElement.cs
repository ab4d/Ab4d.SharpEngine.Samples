using System.Collections.Generic;
using System.Windows;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.UIProvider;

public class StackPanelUIElement : AvaloniaUIPanel
{
    private StackPanel _stackPanel;

    public override Panel? AvaloniaPanel => _stackPanel;

    public StackPanelUIElement(AvaloniaUIProvider avaloniaUIProvider, PositionTypes alignment, bool isVertical, bool addBorder, bool isSemiTransparent)
        : base(avaloniaUIProvider)
    {
        _stackPanel = new StackPanel();

        _stackPanel.Margin = new Thickness(10, 5, 10, 5);
        _stackPanel.Orientation = isVertical ? Orientation.Vertical : Orientation.Horizontal;

        var horizontalAlignment = alignment.HasFlag(PositionTypes.Left) ? HorizontalAlignment.Left :
                                  alignment.HasFlag(PositionTypes.Right) ? HorizontalAlignment.Right :
                                  HorizontalAlignment.Center;

        var verticalAlignment = alignment.HasFlag(PositionTypes.Top) ? VerticalAlignment.Top :
                                alignment.HasFlag(PositionTypes.Bottom) ? VerticalAlignment.Bottom :
                                VerticalAlignment.Center;

        if (addBorder)
        {
            var border = new Border()
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(2),
                Margin = new Thickness(5)
            };

            if (isSemiTransparent)
                border.Background = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255));
            else
                border.Background = Brushes.White;


            border.HorizontalAlignment = horizontalAlignment;
            border.VerticalAlignment = verticalAlignment;

            border.Child = _stackPanel;

            AvaloniaControl = border;
        }
        else
        {
            _stackPanel.HorizontalAlignment = horizontalAlignment;
            _stackPanel.VerticalAlignment = verticalAlignment;

            AvaloniaControl = _stackPanel;
        }
    }
}