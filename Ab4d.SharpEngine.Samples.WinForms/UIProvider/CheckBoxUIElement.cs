using Ab4d.SharpEngine.Samples.Common;
using System;

namespace Ab4d.SharpEngine.Samples.WinForms.UIProvider;

public class CheckBoxUIElement : WinFormsUIElement
{
    private CheckBox _checkBox;

    private Action<bool> _checkedChangedAction;

    public CheckBoxUIElement(WinFormsUIProvider winFormsUIProvider, string text, bool isInitiallyChecked, Action<bool> checkedChangedAction)
        : base(winFormsUIProvider)
    {
        _checkedChangedAction = checkedChangedAction;

        var (textToShow, toolTip) = winFormsUIProvider.ParseTextAndToolTip(text);

        _checkBox = new CheckBox()
        {
            Text = textToShow,
            Font = winFormsUIProvider.Font,
            AutoSize = true
        };

        if (isInitiallyChecked)
            _checkBox.Checked = true;

        if (toolTip != null)
            winFormsUIProvider.SetToolTip(_checkBox, toolTip);

        _checkBox.CheckedChanged += (sender, args) => _checkedChangedAction?.Invoke(_checkBox.Checked);

        WinFormsControl = _checkBox;
    }

    public override string? GetText() => _checkBox.Text as string;

    public override ICommonSampleUIElement SetText(string? text)
    {
        _checkBox.Text = text;
        return this;
    }

    protected override void OnSetColor(Color winFormsColor)
    {
        _checkBox.ForeColor = winFormsColor;
    }
    
    public override void SetValue(object newValue)
    {
        if (newValue is not bool isChecked)
            throw new ArgumentException($"SetValue for CheckBox expects bool value, but got {newValue?.GetType().Name}");

        _checkBox.Checked = isChecked;
    }    
}