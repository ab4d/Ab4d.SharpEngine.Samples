using System;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Ab4d.SharpEngine.Samples.Common;

namespace Ab4d.SharpEngine.Samples.Blazor.UIProvider;

public class SliderUIElement : BlazorUIElement
{
    private Func<float> _getValueFunc;
    private Action<float> _setValueAction;
    private Func<float, string>? _formatShownValueFunc;

    private float _minValue;
    private float _maxValue;
    private float _currentValue;
    private double _width;
    private bool _showTicks;
    private string? _keyText;
    private double _keyTextWidth;
    private double _shownValueWidth;
    private string? _color;

    public override bool IsUpdateSupported => true;

    public SliderUIElement(BlazorUIProvider blazorUIProvider,
                           float minValue,
                           float maxValue,
                           Func<float> getValueFunc,
                           Action<float> setValueAction,
                           double width,
                           bool showTicks,
                           string? keyText,
                           double keyTextWidth,
                           Func<float, string>? formatShownValueFunc,
                           double shownValueWidth)
        : base(blazorUIProvider)
    {
        _getValueFunc = getValueFunc;
        _setValueAction = setValueAction;
        _formatShownValueFunc = formatShownValueFunc;
        _minValue = minValue;
        _maxValue = maxValue;
        _width = width;
        _showTicks = showTicks;
        _keyTextWidth = keyTextWidth;
        _shownValueWidth = shownValueWidth;

        var (keyTextToShow, toolTip) = blazorUIProvider.ParseTextAndToolTip(keyText);
        _keyText = keyTextToShow;

        if (toolTip != null)
            SetToolTip(toolTip);

        // Calculate approximate width for value display if needed
        if (_formatShownValueFunc != null && shownValueWidth == 0)
        {
            // Estimate width based on formatted min/max values
            string minText = _formatShownValueFunc(minValue);
            string maxText = _formatShownValueFunc(maxValue);
            int maxLength = Math.Max(minText.Length, maxText.Length);
            _shownValueWidth = maxLength * 8 + 10; // Approximate character width + padding
        }

        UpdateValue();
    }

    private void BuildRenderFragment()
    {
        BlazorElement = builder =>
        {
            if (_keyText != null || _formatShownValueFunc != null)
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "style", "display: flex; align-items: center;" + GetMarginStyle() + GetVisibilityStyle());

                if (_keyText != null)
                {
                    builder.OpenElement(2, "span");
                    var labelStyle = "margin-right: 5px;";

                    if (_keyTextWidth > 0)
                        labelStyle += $"width: {_keyTextWidth}px;";

                    if (blazorUIProvider.FontSize > 0)
                        labelStyle += $"font-size: {blazorUIProvider.FontSize}px;";

                    if (_color != null)
                        labelStyle += $"color: {_color};";

                    builder.AddAttribute(3, "style", labelStyle);

                    if (!string.IsNullOrEmpty(_tooltip))
                        builder.AddAttribute(4, "title", _tooltip);

                    builder.AddContent(5, _keyText);
                    builder.CloseElement();
                }

                BuildSliderInput(builder, 6);

                if (_formatShownValueFunc != null)
                {
                    builder.OpenElement(7, "span");
                    var valueStyle = "margin-left: 5px;";

                    if (_shownValueWidth > 0)
                        valueStyle += $"width: {_shownValueWidth}px; display: inline-block;";

                    if (blazorUIProvider.FontSize > 0)
                        valueStyle += $"font-size: {blazorUIProvider.FontSize}px;";

                    if (_color != null)
                        valueStyle += $"color: {_color};";

                    builder.AddAttribute(8, "style", valueStyle);
                    builder.AddContent(9, _formatShownValueFunc(_currentValue));
                    builder.CloseElement();
                }

                builder.CloseElement();
            }
            else
            {
                BuildSliderInput(builder, 0);
            }
        };
    }

    private void BuildSliderInput(RenderTreeBuilder builder, int sequence)
    {
        builder.OpenElement(sequence, "input");
        builder.AddAttribute(sequence + 1, "type", "range");
        builder.AddAttribute(sequence + 2, "min", _minValue.ToString(CultureInfo.InvariantCulture));
        builder.AddAttribute(sequence + 3, "max", _maxValue.ToString(CultureInfo.InvariantCulture));
        builder.AddAttribute(sequence + 4, "step", "any"); // Allow any float value
        builder.AddAttribute(sequence + 5, "value", _currentValue.ToString(CultureInfo.InvariantCulture));

        var style = string.Empty;

        if (_keyText == null && _formatShownValueFunc == null)
            style += GetMarginStyle() + GetVisibilityStyle();

        if (_width > 0)
            style += $"width: {_width}px;";

        if (!string.IsNullOrEmpty(style))
            builder.AddAttribute(sequence + 6, "style", style);

        if (_keyText == null && !string.IsNullOrEmpty(_tooltip))
            builder.AddAttribute(sequence + 7, "title", _tooltip);

        builder.AddAttribute(sequence + 8, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, e =>
        {
            if (float.TryParse(e.Value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var newValue))
            {
                _currentValue = newValue;
                _setValueAction?.Invoke(newValue);

                // Rebuild the render fragment to update the displayed value
                BuildRenderFragment();

                // Trigger a re-render of the parent component
                blazorUIProvider.NotifyStateChanged();
            }
        }));

        builder.CloseElement();
    }

    public sealed override void UpdateValue()
    {
        float newValue = _getValueFunc();
        _currentValue = newValue;
        BuildRenderFragment();
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


    public override void SetValue(object newValue)
    {
        if (newValue is int newIntValue)
            _currentValue = newIntValue;
        else if (newValue is float newFloatValue)
            _currentValue = newFloatValue;
        else if (newValue is double newDoubleValue)
            _currentValue = (float)newDoubleValue;
        else
            throw new ArgumentException($"SetValue for Slider expects int, float or double value, but got {newValue?.GetType().Name}");

        BuildRenderFragment();
    }

    public override void SetProperty(string propertyName, string propertyValue)
    {
        if (propertyName.Equals("Maximum", StringComparison.OrdinalIgnoreCase))
        {
            _maxValue = float.Parse(propertyValue, NumberStyles.Float, CultureInfo.InvariantCulture);
            BuildRenderFragment();
        }
        else if (propertyName.Equals("Minimum", StringComparison.OrdinalIgnoreCase))
        {
            _minValue = float.Parse(propertyValue, NumberStyles.Float, CultureInfo.InvariantCulture);
            BuildRenderFragment();
        }
    }

    public override string? GetPropertyValue(string propertyName)
    {
        if (propertyName.Equals("Maximum", StringComparison.OrdinalIgnoreCase))
            return _maxValue.ToString(CultureInfo.InvariantCulture);

        if (propertyName.Equals("Minimum", StringComparison.OrdinalIgnoreCase))
            return _minValue.ToString(CultureInfo.InvariantCulture);

        return null;
    }
}