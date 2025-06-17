using System;
using System.Windows;
using Ab4d.SharpEngine.Samples.Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ab4d.SharpEngine.Samples.WinUI.UIProvider;

public class ButtonUIElement : WinUIElement
{
    private Button _button;

    private Action _clickedAction;

    public ButtonUIElement(WinUIProvider winUiProvider, string text, Action clickedAction, double width = 0, bool alignLeft = false)
        : base(winUiProvider)
    {
        _clickedAction = clickedAction;

        var (textToShow, toolTip) = winUiProvider.ParseTextAndToolTip(text);

        _button = new Button()
        {
            Content = textToShow,
            FontSize = winUiProvider.FontSize,
        };

        if (width > 0 ) 
            _button.Width = width;

        if (alignLeft)
            _button.HorizontalAlignment = HorizontalAlignment.Left;

        if (toolTip != null)
            ToolTipService.SetToolTip(_button, toolTip);

        _button.Click += (sender, args) => _clickedAction?.Invoke();

        Element = _button;
    }


    public override string? GetText() => _button.Content as string;

    public override ICommonSampleUIElement SetText(string? text)
    {
        var (textToShow, toolTip) = winUIProvider.ParseTextAndToolTip(text);
        _button.Content = textToShow;
        ToolTipService.SetToolTip(_button, toolTip);
        return this;
    }        
}