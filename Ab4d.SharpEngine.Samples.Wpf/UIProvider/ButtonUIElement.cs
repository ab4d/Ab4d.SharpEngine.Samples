using System.Windows.Controls;
using System;
using System.Windows;
using Ab4d.SharpEngine.Samples.Common;

namespace Ab4d.SharpEngine.Samples.Wpf.UIProvider;

public class ButtonUIElement : WpfUIElement
{
    private Button _button;

    private Action _clickedAction;

    public ButtonUIElement(WpfUIProvider wpfUIProvider, string text, Action clickedAction, double width = 0, bool alignLeft = false)
        : base(wpfUIProvider)
    {
        _clickedAction = clickedAction;

        var (textToShow, toolTip) = wpfUIProvider.ParseTextAndToolTip(text);

        _button = new Button()
        {
            Content = textToShow,
            FontSize = wpfUIProvider.FontSize,
        };

        if (width > 0 ) 
            _button.Width = width;

        if (alignLeft)
            _button.HorizontalAlignment = HorizontalAlignment.Left;

        if (toolTip != null)
            _button.ToolTip = toolTip;

        _button.Click += (sender, args) => _clickedAction?.Invoke();

        WpfElement = _button;
    }


    public override string? GetText() => _button.Content as string;

    public override ICommonSampleUIElement SetText(string? text)
    {
        var (textToShow, toolTip) = wpfUIProvider.ParseTextAndToolTip(text);
        _button.Content = textToShow;
        _button.ToolTip = toolTip;
        return this;
    }
}