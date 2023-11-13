using Ab4d.SharpEngine.Samples.Common;
using System;
using System.Windows;
using Windows.UI;
using Windows.UI.Text;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Ab4d.SharpEngine.Samples.WinUI.UIProvider;

public class LabelUIElement : WinUIElement
{
    private TextBlock _textBlock;

    private string? _styleString;

    public LabelUIElement(WinUIProvider winUIProvider, string text, bool isHeader, float width = 0, float height = 0)
        : base(winUIProvider)
    {
        var (textToShow, toolTip) = winUIProvider.ParseTextAndToolTip(text);

        _textBlock = new TextBlock
        {
            Text = textToShow,
            FontSize = winUIProvider.FontSize,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };

        if (width > 0)
            _textBlock.Width = width;

        if (height > 0)
            _textBlock.Height = height;

        if (toolTip != null)
            ToolTipService.SetToolTip(_textBlock, toolTip);

        if (isHeader)
        {
            _textBlock.FontWeight = FontWeights.Bold;

            double topMargin = winUIProvider.HeaderTopMargin;
            if (winUIProvider.CurrentPanel != null && winUIProvider.CurrentPanel.ChildrenCount > 0)
                topMargin += 8;

            _textBlock.Margin = new Thickness(0, topMargin, 0, winUIProvider.HeaderBottomMarin);

            _styleString = "bold";
        }

        Element = _textBlock;
    }


    public override string? GetText() => _textBlock.Text;

    public override ICommonSampleUIElement SetText(string? text)
    {
        _textBlock.Text = text;
        return this;
    }


    protected override void OnSetColor(Color winUIColor)
    {
        _textBlock.Foreground = new SolidColorBrush(winUIColor);
    }


    public override string? GetStyle() => _styleString;

    public override ICommonSampleUIElement SetStyle(string style)
    {
        _styleString = style;

        _textBlock.FontWeight = style.Contains("bold", StringComparison.OrdinalIgnoreCase) ? FontWeights.Bold : FontWeights.Normal;

        if (style.Contains("italic"))
            _textBlock.FontStyle = FontStyle.Italic;

        return this;
    }
}