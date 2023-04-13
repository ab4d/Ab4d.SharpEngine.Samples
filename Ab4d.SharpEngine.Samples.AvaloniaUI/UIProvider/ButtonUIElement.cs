using System;
using System.Windows;
using Ab4d.SharpEngine.Samples.Common;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.UIProvider;

public class ButtonUIElement : AvaloniaUIElement
{
    private Button _button;

    private Action _clickedAction;

    public ButtonUIElement(AvaloniaUIProvider avaloniaUIProvider, string text, Action clickedAction, double width = 0, bool alignLeft = false)
        : base(avaloniaUIProvider)
    {
        _clickedAction = clickedAction;

        var (textToShow, toolTip) = avaloniaUIProvider.ParseTextAndToolTip(text);

        _button = new Button()
        {
            Content = textToShow,
            FontSize = avaloniaUIProvider.FontSize,
        };

        if (width > 0 ) 
            _button.Width = width;

        if (alignLeft)
            _button.HorizontalAlignment = HorizontalAlignment.Left;

        if (toolTip != null)
            ToolTip.SetTip(_button, toolTip);

        _button.Click += (sender, args) => _clickedAction?.Invoke();

        AvaloniaControl = _button;
    }


    public override string? GetText() => _button.Content as string;

    public override ICommonSampleUIElement SetText(string? text)
    {
        _button.Content = text;
        return this;
    }
}