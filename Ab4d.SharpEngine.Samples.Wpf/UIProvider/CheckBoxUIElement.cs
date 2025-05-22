using Ab4d.SharpEngine.Samples.Common;
using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ab4d.SharpEngine.Samples.Wpf.UIProvider;

public class CheckBoxUIElement : WpfUIElement
{
    private CheckBox _checkBox;

    private Action<bool> _checkedChangedAction;

    public CheckBoxUIElement(WpfUIProvider wpfUIProvider, string text, bool isInitiallyChecked, Action<bool> checkedChangedAction)
        : base(wpfUIProvider)
    {
        _checkedChangedAction = checkedChangedAction;

        var (textToShow, toolTip) = wpfUIProvider.ParseTextAndToolTip(text);

        _checkBox = new CheckBox()
        {
            Content = textToShow,
            FontSize = wpfUIProvider.FontSize,
        };

        if (isInitiallyChecked)
            _checkBox.IsChecked = true;

        if (toolTip != null)
            _checkBox.ToolTip = toolTip;

        _checkBox.Checked   += (sender, args) => _checkedChangedAction?.Invoke(true);
        _checkBox.Unchecked += (sender, args) => _checkedChangedAction?.Invoke(false);

        WpfElement = _checkBox;
    }

    public override string? GetText() => _checkBox.Content as string;

    public override ICommonSampleUIElement SetText(string? text)
    {
        _checkBox.Content = text;
        return this;
    }

    protected override void OnSetColor(System.Windows.Media.Color wpfColor)
    {
        _checkBox.Foreground = new SolidColorBrush(wpfColor);
    }
    
    public override void SetValue(object newValue)
    {
        if (newValue is not bool isChecked)
            throw new ArgumentException($"SetValue for CheckBox expects bool value, but got {newValue?.GetType().Name}");

        _checkBox.IsChecked = isChecked;
    }
}