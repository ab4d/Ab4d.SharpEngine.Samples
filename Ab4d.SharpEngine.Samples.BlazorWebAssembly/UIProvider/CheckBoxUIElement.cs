using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Ab4d.SharpEngine.Samples.Common;

namespace Ab4d.SharpEngine.Samples.Blazor.UIProvider;

public class CheckBoxUIElement : BlazorUIElement
{
    private string _text;
    private bool _isChecked;
    private Action<bool> _checkedChangedAction;
    private string? _color;

    public CheckBoxUIElement(BlazorUIProvider blazorUIProvider, string text, bool isInitiallyChecked, Action<bool> checkedChangedAction)
        : base(blazorUIProvider)
    {
        _checkedChangedAction = checkedChangedAction;
        _isChecked = isInitiallyChecked;

        var (textToShow, toolTip) = blazorUIProvider.ParseTextAndToolTip(text);
        _text = textToShow ?? string.Empty;

        if (toolTip != null)
            SetToolTip(toolTip);

        BuildRenderFragment();
    }

    private void BuildRenderFragment()
    {
        BlazorElement = builder =>
        {
            builder.OpenElement(0, "label");

            var style = GetMarginStyle() + GetVisibilityStyle();

            if (blazorUIProvider.FontSize > 0)
                style += $"font-size: {blazorUIProvider.FontSize}px;";

            if (_color != null)
                style += $"color: {_color};";

            if (!string.IsNullOrEmpty(style))
                builder.AddAttribute(1, "style", style);

            if (!string.IsNullOrEmpty(_tooltip))
                builder.AddAttribute(2, "title", _tooltip);

            builder.OpenElement(3, "input");
            builder.AddAttribute(4, "type", "checkbox");
            builder.AddAttribute(5, "checked", _isChecked);
            builder.AddAttribute(6, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, e =>
            {
                _isChecked = e.Value as bool? ?? false;
                _checkedChangedAction?.Invoke(_isChecked);
            }));
            builder.CloseElement();

            builder.AddContent(7, " " + _text);
            builder.CloseElement();
        };
    }

    public override string? GetText() => _text;

    public override ICommonSampleUIElement SetText(string? text)
    {
        _text = text ?? string.Empty;
        BuildRenderFragment();
        return this;
    }

    protected override void OnSetColor(string htmlColor)
    {
        _color = htmlColor;
        BuildRenderFragment();
    }

    public override void SetValue(object newValue)
    {
        if (newValue is not bool isChecked)
            throw new ArgumentException($"SetValue for CheckBox expects bool value, but got {newValue?.GetType().Name}");

        _isChecked = isChecked;
        BuildRenderFragment();

        _checkedChangedAction?.Invoke(isChecked);

        // Trigger a re-render of the parent component
        blazorUIProvider.NotifyStateChanged();
    }
}