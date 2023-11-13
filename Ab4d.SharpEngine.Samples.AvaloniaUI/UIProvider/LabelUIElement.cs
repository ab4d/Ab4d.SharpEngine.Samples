using Ab4d.SharpEngine.Samples.Common;
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.UIProvider;

public class LabelUIElement : AvaloniaUIElement
{
    private TextBlock _textBlock;

    private string? _styleString;

    public LabelUIElement(AvaloniaUIProvider avaloniaUIProvider, string text, bool isHeader, float width = 0, float height = 0)
        : base(avaloniaUIProvider)
    {
        var (textToShow, toolTip) = avaloniaUIProvider.ParseTextAndToolTip(text);

        _textBlock = new TextBlock
        {
            Text = textToShow,
            FontSize = avaloniaUIProvider.FontSize,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };

        if (width > 0)
            _textBlock.Width = width;
        
        if (height > 0)
            _textBlock.Height = height;

        if (toolTip != null)
            ToolTip.SetTip(_textBlock, toolTip);

        if (isHeader)
        {
            _textBlock.FontWeight = FontWeight.Bold;

            double topMargin = avaloniaUIProvider.HeaderTopMargin;
            if (avaloniaUIProvider.CurrentPanel != null && avaloniaUIProvider.CurrentPanel.ChildrenCount > 0)
                topMargin += 8;

            _textBlock.Margin = new Thickness(0, topMargin, 0, avaloniaUIProvider.HeaderBottomMarin);

            _styleString = "bold";
        }

        AvaloniaControl = _textBlock;
    }


    public override string? GetText() => _textBlock.Text;

    public override ICommonSampleUIElement SetText(string? text)
    {
        _textBlock.Text = text;
        return this;
    }


    protected override void OnSetColor(Color avaloniaColor)
    {
        _textBlock.Foreground = new SolidColorBrush(avaloniaColor);
    }


    public override string? GetStyle() => _styleString;

    public override ICommonSampleUIElement SetStyle(string style)
    {
        _styleString = style;

        _textBlock.FontWeight = style.Contains("bold", StringComparison.OrdinalIgnoreCase) ? FontWeight.Bold : FontWeight.Normal;

        if (style.Contains("italic"))
            _textBlock.FontStyle = FontStyle.Italic;

        return this;
    }
}