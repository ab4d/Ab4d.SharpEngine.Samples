using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;

namespace Ab4d.SharpEngine.Samples.Wpf.UIProvider;

public class StackPanelUIElement : WpfUIPanel
{
    private StackPanel _stackPanel;

    public override Panel? WpfPanel => _stackPanel;

    public StackPanelUIElement(WpfUIProvider wpfUIProvider, PositionTypes alignment, bool isVertical, bool addBorder, bool isSemiTransparent)
        : base(wpfUIProvider)
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
                Margin = new Thickness(10)
            };

            if (isSemiTransparent)
                border.Background = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255));
            else
                border.Background = Brushes.White;


            border.HorizontalAlignment = horizontalAlignment;
            border.VerticalAlignment = verticalAlignment;

            border.Child = _stackPanel;

            WpfElement = border;
        }
        else
        {
            _stackPanel.HorizontalAlignment = horizontalAlignment;
            _stackPanel.VerticalAlignment = verticalAlignment;

            WpfElement = _stackPanel;
        }
    }
}