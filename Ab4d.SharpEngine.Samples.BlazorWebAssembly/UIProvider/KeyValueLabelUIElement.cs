using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Ab4d.SharpEngine.Samples.Common;

namespace Ab4d.SharpEngine.Samples.Blazor.UIProvider;

public class KeyValueLabelUIElement : BlazorUIElement
{
    private string? _keyText;
    private Func<string> _getValueTextFunc;
    private string _currentValueText = "";
    private double _keyTextWidth;
    private string? _styleString;
    private string? _color;

    public override bool IsUpdateSupported => true;

    public KeyValueLabelUIElement(BlazorUIProvider blazorUIProvider, string? keyText, Func<string> getValueTextFunc, double keyTextWidth)
        : base(blazorUIProvider)
    {
        _getValueTextFunc = getValueTextFunc;
        _keyTextWidth = keyTextWidth;

        var (keyTextToShow, toolTip) = blazorUIProvider.ParseTextAndToolTip(keyText);
        _keyText = keyTextToShow;

        if (toolTip != null)
            SetToolTip(toolTip);

        UpdateValue();
    }

    public sealed override void UpdateValue()
    {
        _currentValueText = _getValueTextFunc();
        BuildRenderFragment();

        // Trigger a re-render of the parent component
        blazorUIProvider.NotifyStateChanged();
    }

    private void BuildRenderFragment()
    {
        BlazorElement = builder =>
        {
            if (_keyTextWidth > 0)
            {
                builder.OpenElement(0, "div");

                // Add "white-space: pre-wrap;" - preserves new lines ('\n') in text
                builder.AddAttribute(1, "style", "display: flex; white-space: pre-wrap;" + GetMarginStyle() + GetVisibilityStyle());

                builder.OpenElement(2, "span");
                var keyStyle = $"width: {_keyTextWidth}px; margin-right: 3px;";

                if (blazorUIProvider.FontSize > 0)
                    keyStyle += $"font-size: {blazorUIProvider.FontSize}px;";

                if (_styleString != null && _styleString.Contains("bold", StringComparison.OrdinalIgnoreCase))
                    keyStyle += "font-weight: bold;";

                if (_styleString != null && _styleString.Contains("italic", StringComparison.OrdinalIgnoreCase))
                    keyStyle += "font-style: italic;";

                if (_color != null)
                    keyStyle += $"color: {_color};";

                builder.AddAttribute(3, "style", keyStyle);

                if (!string.IsNullOrEmpty(_tooltip))
                    builder.AddAttribute(4, "title", _tooltip);

                builder.AddContent(5, _keyText);
                builder.CloseElement();

                builder.OpenElement(6, "span");
                var valueStyle = "white-space: pre-wrap;"; // Add "white-space: pre-wrap;" - preserves new lines ('\n') in text

                if (blazorUIProvider.FontSize > 0)
                    valueStyle += $"font-size: {blazorUIProvider.FontSize}px;";

                if (_styleString != null && _styleString.Contains("bold", StringComparison.OrdinalIgnoreCase))
                    valueStyle += "font-weight: bold;";

                if (_styleString != null && _styleString.Contains("italic", StringComparison.OrdinalIgnoreCase))
                    valueStyle += "font-style: italic;";

                if (_color != null)
                    valueStyle += $"color: {_color};";

                if (!string.IsNullOrEmpty(valueStyle))
                    builder.AddAttribute(7, "style", valueStyle);

                if (!string.IsNullOrEmpty(_tooltip))
                    builder.AddAttribute(8, "title", _tooltip);

                builder.AddContent(9, _currentValueText);
                builder.CloseElement();

                builder.CloseElement();
            }
            else
            {
                builder.OpenElement(0, "span");

                // Add "white-space: pre-wrap;" - preserves new lines ('\n') in text
                var style = "white-space: pre-wrap;" + GetMarginStyle() + GetVisibilityStyle();

                if (blazorUIProvider.FontSize > 0)
                    style += $"font-size: {blazorUIProvider.FontSize}px;";

                if (_styleString != null && _styleString.Contains("bold", StringComparison.OrdinalIgnoreCase))
                    style += "font-weight: bold;";

                if (_styleString != null && _styleString.Contains("italic", StringComparison.OrdinalIgnoreCase))
                    style += "font-style: italic;";

                if (_color != null)
                    style += $"color: {_color};";

                if (!string.IsNullOrEmpty(style))
                    builder.AddAttribute(1, "style", style);

                if (!string.IsNullOrEmpty(_tooltip))
                    builder.AddAttribute(2, "title", _tooltip);

                var displayText = string.IsNullOrEmpty(_keyText) ? _currentValueText : _keyText + " " + _currentValueText;
                builder.AddContent(3, displayText);
                builder.CloseElement();
            }
        };
    }


    public override string? GetText() => _keyText;

    public override ICommonSampleUIElement SetText(string? text)
    {
        _keyText = text;
        BuildRenderFragment();
        return this;
    }

    protected override void OnSetColor(string htmlColor)
    {
        _color = htmlColor;
        BuildRenderFragment();
    }

    public override string? GetStyle() => _styleString;

    public override ICommonSampleUIElement SetStyle(string style)
    {
        _styleString = style;
        BuildRenderFragment();
        return this;
    }
}