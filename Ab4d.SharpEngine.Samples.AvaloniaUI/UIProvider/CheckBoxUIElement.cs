using Ab4d.SharpEngine.Samples.Common;
using System;
using Avalonia.Controls;
using Avalonia.Media;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.UIProvider;

public class CheckBoxUIElement : AvaloniaUIElement
{
    private CheckBox _checkBox;

    private Action<bool> _checkedChangedAction;

    public CheckBoxUIElement(AvaloniaUIProvider avaloniaUIProvider, string text, bool isInitiallyChecked, Action<bool> checkedChangedAction)
        : base(avaloniaUIProvider)
    {
        _checkedChangedAction = checkedChangedAction;

        var (textToShow, toolTip) = avaloniaUIProvider.ParseTextAndToolTip(text);

        _checkBox = new CheckBox()
        {
            Content = textToShow,
            FontSize = avaloniaUIProvider.FontSize,
        };

        if (isInitiallyChecked)
            _checkBox.IsChecked = true;

        if (toolTip != null)
            ToolTip.SetTip(_checkBox, toolTip);

        _checkBox.IsCheckedChanged += (sender, args) => _checkedChangedAction?.Invoke(_checkBox.IsChecked ?? false);

        AvaloniaControl = _checkBox;
    }

    public override string? GetText() => _checkBox.Content as string;

    public override ICommonSampleUIElement SetText(string? text)
    {
        _checkBox.Content = text;
        return this;
    }

    protected override void OnSetColor(Color avaloniaColor)
    {
        _checkBox.Foreground = new SolidColorBrush(avaloniaColor);
    }
}