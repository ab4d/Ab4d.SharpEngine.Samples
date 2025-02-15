using Ab4d.SharpEngine.Samples.Common;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ab4d.SharpEngine.Samples.Wpf.UIProvider;

public class KeyValueLabelUIElement : WpfUIElement
{
    private string? _keyText;
    private Func<string> _getValueTextFunc;

    private StackPanel? _stackPanel;
    private TextBlock? _keyTextBlock;
    private TextBlock? _valueTextBlock;

    private TextBlock? _keyValueTextBlock;

    private string? _styleString;

    public override bool IsUpdateSupported => true;

    public KeyValueLabelUIElement(WpfUIProvider wpfUIProvider, string? keyText, Func<string> getValueTextFunc, double keyTextWidth)
        : base(wpfUIProvider)
    {
        _keyText = keyText;
        _getValueTextFunc = getValueTextFunc;

        var (keyTextToShow, toolTip) = wpfUIProvider.ParseTextAndToolTip(keyText);

        if (keyTextWidth > 0)
        {
            _stackPanel = new StackPanel() { Orientation = Orientation.Horizontal };
            
            _keyTextBlock = new TextBlock()
            {
                Text = keyTextToShow,
                Margin = new Thickness(0, 0, 3, 0),
                Width = keyTextWidth,
                FontSize = wpfUIProvider.FontSize,
            };

            _valueTextBlock = new TextBlock()
            {
                FontSize = wpfUIProvider.FontSize,
            };

            if (toolTip != null)
            {
                _keyTextBlock.ToolTip = toolTip;
                _valueTextBlock.ToolTip = toolTip;
            }

            _stackPanel.Children.Add(_keyTextBlock);
            _stackPanel.Children.Add(_valueTextBlock);

            WpfElement = _stackPanel;
        }
        else
        {
            // When there is no keyTextWidth, then we can use a single TextBlock to show key and value
            _keyValueTextBlock = new TextBlock()
            {
                FontSize = wpfUIProvider.FontSize,
            };

            _keyText = keyTextToShow; // Update _keyText so that show text without toot tip

            if (toolTip != null)
                _keyValueTextBlock.ToolTip = toolTip;

            WpfElement = _keyValueTextBlock;
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

    protected override void OnSetColor(System.Windows.Media.Color wpfColor)
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
                _keyTextBlock.FontStyle = FontStyles.Italic;

            if (_valueTextBlock != null)
                _valueTextBlock.FontStyle = FontStyles.Italic;

            if (_keyValueTextBlock != null)
                _keyValueTextBlock.FontStyle = FontStyles.Italic;
        }

        return this;
    }
}