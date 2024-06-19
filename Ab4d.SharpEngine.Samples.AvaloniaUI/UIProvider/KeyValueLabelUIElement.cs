using Ab4d.SharpEngine.Samples.Common;
using System;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.UIProvider;

public class KeyValueLabelUIElement : AvaloniaUIElement
{
    private string? _keyText;
    private Func<string> _getValueTextFunc;

    private StackPanel? _stackPanel;
    private TextBlock? _keyTextBlock;
    private TextBlock? _valueTextBlock;

    private TextBlock? _keyValueTextBlock;

    public override bool IsUpdateSupported => true;

    public KeyValueLabelUIElement(AvaloniaUIProvider avaloniaUIProvider, string? keyText, Func<string> getValueTextFunc, double keyTextWidth)
        : base(avaloniaUIProvider)
    {
        _keyText = keyText;
        _getValueTextFunc = getValueTextFunc;

        var (keyTextToShow, toolTip) = avaloniaUIProvider.ParseTextAndToolTip(keyText);


        if (keyTextWidth > 0)
        {
            _stackPanel = new StackPanel() { Orientation = Orientation.Horizontal };
            
            _keyTextBlock = new TextBlock()
            {
                Text = keyTextToShow,
                Margin = new Thickness(0, 0, 3, 0),
                Width = keyTextWidth,
                FontSize = avaloniaUIProvider.FontSize,
            };

            _valueTextBlock = new TextBlock()
            {
                FontSize = avaloniaUIProvider.FontSize,
            };

            if (toolTip != null)
            {
                ToolTip.SetTip(_keyTextBlock, toolTip);
                ToolTip.SetTip(_valueTextBlock, toolTip);
            }

            _stackPanel.Children.Add(_keyTextBlock);
            _stackPanel.Children.Add(_valueTextBlock);

            AvaloniaControl = _stackPanel;
        }
        else
        {
            // When there is no keyTextWidth, then we can use a single TextBlock to show key and value
            _keyValueTextBlock = new TextBlock()
            {
                FontSize = avaloniaUIProvider.FontSize,
            };

            _keyText = keyTextToShow; // Update _keyText so that show text without toot tip

            if (toolTip != null)
                ToolTip.SetTip(_keyValueTextBlock, toolTip);

            AvaloniaControl = _keyValueTextBlock;
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

    protected override void OnSetColor(Color avaloniaColor)
    {
        if (_keyTextBlock != null)
            _keyTextBlock.Foreground = new SolidColorBrush(avaloniaColor);

        if (_valueTextBlock != null)
            _valueTextBlock.Foreground = new SolidColorBrush(avaloniaColor);
    }
}