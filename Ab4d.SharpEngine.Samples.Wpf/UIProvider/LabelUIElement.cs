using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Wpf.Common;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ab4d.SharpEngine.Samples.Wpf.UIProvider;

public class LabelUIElement : WpfUIElement
{
    private TextBlock _textBlock;

    private string? _styleString;

    public LabelUIElement(WpfUIProvider wpfUIProvider, string text, bool isHeader, float width = 0, float height = 0, float maxWidth = 0)
        : base(wpfUIProvider)
    {
        var (textToShow, toolTip) = wpfUIProvider.ParseTextAndToolTip(text);

        _textBlock = new TextBlock
        {
            Text = textToShow,
            FontSize = wpfUIProvider.FontSize,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };

        if (width > 0)
            _textBlock.Width = width;
        
        if (maxWidth > 0)
            _textBlock.MaxWidth = maxWidth;
        
        if (height > 0)
            _textBlock.Height = height;

        if (toolTip != null)
            _textBlock.ToolTip = toolTip;

        if (isHeader)
        {
            _textBlock.FontWeight = FontWeights.Bold;

            double topMargin = wpfUIProvider.HeaderTopMargin;
            if (wpfUIProvider.CurrentPanel != null && wpfUIProvider.CurrentPanel.ChildrenCount > 0)
                topMargin += 8;

            _textBlock.Margin = new Thickness(0, topMargin, 0, wpfUIProvider.HeaderBottomMarin);

            _styleString = "bold";
        }

        WpfElement = _textBlock;
    }


    public override string? GetText() => _textBlock.Text;

    public override ICommonSampleUIElement SetText(string? text)
    {
        var (textToShow, toolTip) = wpfUIProvider.ParseTextAndToolTip(text);
        _textBlock.Text = textToShow;
        _textBlock.ToolTip = toolTip;        
        return this;
    }


    protected override void OnSetColor(System.Windows.Media.Color wpfColor)
    {
        _textBlock.Foreground = new SolidColorBrush(wpfColor);
    }


    public override string? GetStyle() => _styleString;

    public override ICommonSampleUIElement SetStyle(string style)
    {
        _styleString = style;

        _textBlock.FontWeight = style.Contains("bold", StringComparison.OrdinalIgnoreCase) ? FontWeights.Bold : FontWeights.Normal;

        if (style.Contains("italic"))
            _textBlock.FontStyle = FontStyles.Italic;

        return this;
    }
}