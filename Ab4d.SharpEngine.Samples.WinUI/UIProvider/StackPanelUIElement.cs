using System.Collections.Generic;
using Windows.UI;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Colors = Microsoft.UI.Colors;

namespace Ab4d.SharpEngine.Samples.WinUI.UIProvider;

public class StackPanelUIElement : WinUIPanel
{
    private StackPanel _stackPanel;

    public override Panel? Panel => _stackPanel;

    public override bool IsVertical => _stackPanel.Orientation == Orientation.Vertical;

    public StackPanelUIElement(WinUIProvider winUIProvider, PositionTypes alignment, bool isVertical, bool addBorder, bool isSemiTransparent, float width, float xMargin)
        : base(winUIProvider)
    {
        _stackPanel = new StackPanel();

        if (width != 0)
            _stackPanel.Width = width;

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
                BorderBrush = new SolidColorBrush(Colors.Black),
                BorderThickness = new Thickness(2),
            };

            _stackPanel.Margin = new Thickness(10, 5, 10, 5);

            border.Margin = float.IsNaN(xMargin) ? new Thickness(5) 
                                                 : alignment.HasFlag(PositionTypes.Left) ? new Thickness(xMargin, 5, 5, 5) 
                                                                                         : new Thickness(5, 5, xMargin, 5);

            if (isSemiTransparent)
                border.Background = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255));
            else
                border.Background = new SolidColorBrush(Colors.White);


            border.HorizontalAlignment = horizontalAlignment;
            border.VerticalAlignment = verticalAlignment;

            border.Child = _stackPanel;

            Element = border;
        }
        else
        {
            _stackPanel.HorizontalAlignment = horizontalAlignment;
            _stackPanel.VerticalAlignment = verticalAlignment;

            _stackPanel.Margin = float.IsNaN(xMargin) ? new Thickness(10, 5, 10, 5) 
                                                      : alignment.HasFlag(PositionTypes.Left) ? new Thickness(xMargin, 5, 10, 5) 
                                                                                              : new Thickness(10, 5, xMargin, 5);

            Element = _stackPanel;
        }
    }
}