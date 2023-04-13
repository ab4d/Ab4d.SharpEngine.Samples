using Ab4d.SharpEngine.Samples.Common;
using System;
using Windows.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Ab4d.SharpEngine.Samples.WinUI.UIProvider;

public class CheckBoxUIElement : WinUIElement
{
    private CheckBox _checkBox;

    private Action<bool> _checkedChangedAction;

    public CheckBoxUIElement(WinUIProvider winUIProvider, string text, bool isInitiallyChecked, Action<bool> checkedChangedAction)
        : base(winUIProvider)
    {
        _checkedChangedAction = checkedChangedAction;

        var (textToShow, toolTip) = winUIProvider.ParseTextAndToolTip(text);

        _checkBox = new CheckBox()
        {
            Content = textToShow,
            FontSize = winUIProvider.FontSize,
        };

        if (isInitiallyChecked)
            _checkBox.IsChecked = true;

        if (toolTip != null)
            ToolTipService.SetToolTip(_checkBox, toolTip);

        _checkBox.Checked   += (sender, args) => _checkedChangedAction?.Invoke(true);
        _checkBox.Unchecked += (sender, args) => _checkedChangedAction?.Invoke(false);

        Element = _checkBox;
    }

    public override string? GetText() => _checkBox.Content as string;

    public override ICommonSampleUIElement SetText(string? text)
    {
        _checkBox.Content = text;
        return this;
    }

    protected override void OnSetColor(Color wpfColor)
    {
        _checkBox.Foreground = new SolidColorBrush(wpfColor);
    }
}