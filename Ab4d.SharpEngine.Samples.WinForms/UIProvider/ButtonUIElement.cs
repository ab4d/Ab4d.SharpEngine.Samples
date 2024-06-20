using System;
using System.Windows;
using Ab4d.SharpEngine.Samples.Common;

namespace Ab4d.SharpEngine.Samples.WinForms.UIProvider;

public class ButtonUIElement : WinFormsUIElement
{
    private Button _button;

    private Action _clickedAction;

    public ButtonUIElement(WinFormsUIProvider winFormsUIProvider, string text, Action clickedAction, double width = 0, bool alignLeft = false)
        : base(winFormsUIProvider)
    {
        _clickedAction = clickedAction;

        var (textToShow, toolTip) = winFormsUIProvider.ParseTextAndToolTip(text);

        _button = new Button()
        {
            Text = textToShow,
            Font = winFormsUIProvider.Font,
            AutoSize = true,
            Margin = new Padding(2, 2, 2, 2)
        };

        if (width > 0)
            _button.Width = (int)(width * winFormsUIProvider.UIScale);

        //if (alignLeft)
        //    _button.HorizontalAlignment = HorizontalAlignment.Left;

        if (toolTip != null)
            winFormsUIProvider.SetToolTip(_button, toolTip);

        _button.Click += (sender, args) => _clickedAction?.Invoke();

        WinFormsControl = _button;
    }


    public override string? GetText() => _button.Text as string;

    public override ICommonSampleUIElement SetText(string? text)
    {
        _button.Text = text;
        return this;
    }
}