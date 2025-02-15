using Ab4d.SharpEngine.Samples.Common;
using System;
using System.Windows;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Text;
using Windows.UI.Text;

namespace Ab4d.SharpEngine.Samples.WinUI.UIProvider;

public class KeyValueLabelUIElement : WinUIElement
{
    private string? _keyText;
    private Func<string> _getValueTextFunc;

    private StackPanel? _stackPanel;
    private TextBlock? _keyTextBlock;
    private TextBlock? _valueTextBlock;

    private TextBlock? _keyValueTextBlock;

    private string? _styleString;

    public override bool IsUpdateSupported => true;

    public KeyValueLabelUIElement(WinUIProvider winUIProvider, string? keyText, Func<string> getValueTextFunc, double keyTextWidth)
        : base(winUIProvider)
    {
        _keyText = keyText;
        _getValueTextFunc = getValueTextFunc;

        var (keyTextToShow, toolTip) = winUIProvider.ParseTextAndToolTip(keyText);

        if (keyTextWidth > 0)
        {
            _stackPanel = new StackPanel() { Orientation = Orientation.Horizontal };
            
            _keyTextBlock = new TextBlock()
            {
                Text = keyTextToShow,
                Margin = new Thickness(0, 0, 3, 0),
                Width = keyTextWidth,
                FontSize = winUIProvider.FontSize,
            };

            _valueTextBlock = new TextBlock()
            {
                FontSize = winUIProvider.FontSize,
            };

            if (toolTip != null)
            {
                ToolTipService.SetToolTip(_keyTextBlock, toolTip);
                ToolTipService.SetToolTip(_valueTextBlock, toolTip);
            }

            _stackPanel.Children.Add(_keyTextBlock);
            _stackPanel.Children.Add(_valueTextBlock);

            Element = _stackPanel;
        }
        else
        {
            // When there is no keyTextWidth, then we can use a single TextBlock to show key and value
            _keyValueTextBlock = new TextBlock()
            {
                FontSize = winUIProvider.FontSize,
            };

            _keyText = keyTextToShow; // Update _keyText so that show text without toot tip

            if (toolTip != null)
                ToolTipService.SetToolTip(_keyValueTextBlock, toolTip);

            Element = _keyValueTextBlock;
        }

        UpdateValue();
    }

    public sealed override void UpdateValue()
    {
        string newValueText = _getValueTextFunc();

        if (_valueTextBlock != null)
        {
            _valueTextBlock.Text = newValueText;
        }
        else if (_keyValueTextBlock != null)
        {
            if (string.IsNullOrEmpty(_keyText))
                _keyValueTextBlock.Text = newValueText;
            else
                _keyValueTextBlock.Text = _keyText + " " + newValueText;
        }
    }


    public override string? GetText()
    {
        if (_keyTextBlock != null)
            return _keyTextBlock.Text;

        return null;
    }

    public override ICommonSampleUIElement SetText(string? text)
    {
        if (_keyTextBlock != null)
            _keyTextBlock.Text = text;

        return this;
    }

    protected override void OnSetColor(Color wpfColor)
    {
        if (_keyTextBlock != null)
            _keyTextBlock.Foreground = new SolidColorBrush(wpfColor);

        if (_valueTextBlock != null)
            _valueTextBlock.Foreground = new SolidColorBrush(wpfColor);

        if (_keyValueTextBlock != null)
            _keyValueTextBlock.Foreground = new SolidColorBrush(wpfColor);
    }

    public override string? GetStyle() => _styleString;

    public override ICommonSampleUIElement SetStyle(string style)
    {
        _styleString = style;

        var fontWeight = style.Contains("bold", StringComparison.OrdinalIgnoreCase) ? FontWeights.Bold : FontWeights.Normal;

        if (_keyTextBlock != null)
            _keyTextBlock.FontWeight = fontWeight;

        if (_valueTextBlock != null)
            _valueTextBlock.FontWeight = fontWeight;
        
        if (_keyValueTextBlock != null)
            _keyValueTextBlock.FontWeight = fontWeight;

        if (style.Contains("italic"))
        {
            if (_keyTextBlock != null)
                _keyTextBlock.FontStyle = FontStyle.Italic;

            if (_valueTextBlock != null)
                _valueTextBlock.FontStyle = FontStyle.Italic;

            if (_keyValueTextBlock != null)
                _keyValueTextBlock.FontStyle = FontStyle.Italic;
        }

        return this;
    }
}