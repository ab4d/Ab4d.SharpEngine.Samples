using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Ab4d.SharpEngine.Samples.Common;

namespace Ab4d.SharpEngine.Samples.Blazor.UIProvider;

public class TextBoxUIElement : BlazorUIElement
{
    private string _text;
    private Action<string>? _textChangedAction;
    private double _width;
    private double _height;
    private bool _isMultiline;

    public TextBoxUIElement(BlazorUIProvider blazorUIProvider, float width, float height, string? initialText, Action<string>? textChangedAction)
        : base(blazorUIProvider)
    {
        _text = initialText ?? "";
        _textChangedAction = textChangedAction;
        _width = width;
        _height = height;
        _isMultiline = height > 0 || (initialText != null && initialText.Contains('\n'));

        BuildRenderFragment();
    }

    private void BuildRenderFragment()
    {
        BlazorElement = builder =>
        {
            if (_isMultiline)
            {
                builder.OpenElement(0, "textarea");
            }
            else
            {
                builder.OpenElement(0, "input");
                builder.AddAttribute(1, "type", "text");
            }

            var style = GetMarginStyle() + GetVisibilityStyle();

            if (_width > 0)
                style += $"width: {_width}px;";

            if (_height > 0)
                style += $"height: {_height}px;";

            if (blazorUIProvider.FontSize > 0)
                style += $"font-size: {blazorUIProvider.FontSize}px;";

            if (_isMultiline)
                style += "overflow: auto;";

            if (!string.IsNullOrEmpty(style))
                builder.AddAttribute(2, "style", style);

            builder.AddAttribute(3, "value", _text);
            builder.AddAttribute(4, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, e =>
            {
                _text = e.Value?.ToString() ?? "";
                _textChangedAction?.Invoke(_text);
            }));

            builder.CloseElement();
        };
    }

    public override string? GetText() => _text;

    public override ICommonSampleUIElement SetText(string? text)
    {
        _text = text ?? "";
        BuildRenderFragment();

        // Trigger a re-render of the parent component
        blazorUIProvider.NotifyStateChanged();

        return this;
    }

    public override void SetValue(object newValue)
    {
        if (newValue is not string newText)
            throw new ArgumentException($"SetValue for TextBox expects string value, but got {newValue?.GetType().Name}");

        _text = newText;
        BuildRenderFragment();
    }
}