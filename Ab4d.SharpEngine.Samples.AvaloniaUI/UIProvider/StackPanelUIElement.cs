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

    public override bool IsVertical => _stackPanel.Orientation == Orientation.Vertical;

    public StackPanelUIElement(AvaloniaUIProvider avaloniaUIProvider, PositionTypes alignment, bool isVertical, bool addBorder, bool isSemiTransparent, float width, float xMargin)
        : base(avaloniaUIProvider)
    {
        _stackPanel = new StackPanel();

        _stackPanel.Orientation = isVertical ? Orientation.Vertical : Orientation.Horizontal;

        if (width != 0)
            _stackPanel.Width = width;

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
            };

            _stackPanel.Margin = new Thickness(10, 5, 10, 5);

            border.Margin = float.IsNaN(xMargin) ? new Thickness(5) 
                                                 : alignment.HasFlag(PositionTypes.Left) ? new Thickness(xMargin, 5, 5, 5) 
                                                                                         : new Thickness(5, 5, xMargin, 5);

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

            _stackPanel.Margin = float.IsNaN(xMargin) ? new Thickness(10, 5, 10, 5) 
                                                      : alignment.HasFlag(PositionTypes.Left) ? new Thickness(xMargin, 5, 10, 5) 
                                                                                              : new Thickness(10, 5, xMargin, 5);

            AvaloniaControl = _stackPanel;
        }
    }
}