using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Ab4d.SharpEngine.Samples.Common;

namespace Ab4d.SharpEngine.Samples.Blazor.UIProvider;

public class RadioButtonsUIElement : BlazorUIElement
{
    private static int _groupsCount;

    private string[] _items;
    private int _selectedIndex;
    private Action<int, string> _checkedItemChangedAction;
    private double _width;
    private string? _keyText;
    private double _keyTextWidth;
    private string _groupName;
    private string? _color;

    public RadioButtonsUIElement(BlazorUIProvider blazorUIProvider,
                                 string[] items,
                                 Action<int, string> checkedItemChangedAction,
                                 int selectedItemIndex,
                                 double width = 0,
                                 string? keyText = null,
                                 double keyTextWidth = 0)
        : base(blazorUIProvider)
    {
        _items = items;
        _selectedIndex = selectedItemIndex;
        _checkedItemChangedAction = checkedItemChangedAction;
        _width = width;
        _keyTextWidth = keyTextWidth;

        _groupsCount++;
        _groupName = "RadioButtonsGroup_" + _groupsCount.ToString();

        var (keyTextToShow, toolTip) = blazorUIProvider.ParseTextAndToolTip(keyText);
        _keyText = keyTextToShow;

        if (toolTip != null)
            SetToolTip(toolTip);

        BuildRenderFragment();
    }

    private void BuildRenderFragment()
    {
        BlazorElement = builder =>
        {
            if (_keyText != null)
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "style", "display: flex;" + GetMarginStyle() + GetVisibilityStyle());

                builder.OpenElement(2, "span");
                var labelStyle = "margin-right: 5px; margin-top: 1px; vertical-align: top;";

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

                BuildRadioButtons(builder, 6);

                builder.CloseElement();
            }
            else
            {
                BuildRadioButtons(builder, 0);
            }
        };
    }

    private void BuildRadioButtons(RenderTreeBuilder builder, int sequence)
    {
        builder.OpenElement(sequence, "div");
        var style = "display: flex; flex-direction: column;";

        if (_keyText == null)
            style += GetMarginStyle() + GetVisibilityStyle();

        builder.AddAttribute(sequence + 1, "style", style);

        for (int i = 0; i < _items.Length; i++)
        {
            int index = i; // Capture for lambda
            var (text, toolTip) = blazorUIProvider.ParseTextAndToolTip(_items[i]);

            builder.OpenElement(sequence + 2 + i * 10, "label");
            var labelStyle = "margin: 1px 0;";
            
            if (blazorUIProvider.FontSize > 0)
                labelStyle += $"font-size: {blazorUIProvider.FontSize}px;";
            
            if (_color != null)
                labelStyle += $"color: {_color};";

            if (toolTip != null)
                builder.AddAttribute(sequence + 9 + i * 10, "title", toolTip);

            builder.AddAttribute(sequence + 3 + i * 10, "style", labelStyle);

            builder.OpenElement(sequence + 4 + i * 10, "input");
            builder.AddAttribute(sequence + 5 + i * 10, "type", "radio");
            builder.AddAttribute(sequence + 6 + i * 10, "name", _groupName);

            if (i == _selectedIndex)
                builder.AddAttribute(sequence + 7 + i * 10, "checked", "checked");
            
            if (_width > 0)
                builder.AddAttribute(sequence + 8 + i * 10, "style", $"width: {_width}px;");
            
            builder.AddAttribute(sequence + 10 + i * 10, "onchange", EventCallback.Factory.Create(this, () =>
            {
                _selectedIndex = index;
                _checkedItemChangedAction?.Invoke(index, _items[index]);
            }));

            builder.CloseElement();

            builder.AddContent(sequence + 11 + i * 10, " " + text);
            builder.CloseElement();
        }

        builder.CloseElement();
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
        if (newValue is int newIndex)
        {
            if (newIndex < 0 || newIndex > _items.Length - 1)
                throw new ArgumentOutOfRangeException(nameof(newIndex), $"Expected index between 0 and {_items.Length - 1}, but got {newIndex}");

            _selectedIndex = newIndex;
        }
        else if (newValue is string newSelectedItem)
        {
            int index = Array.IndexOf(_items, newSelectedItem);

            if (index == -1)
                throw new ArgumentException($"Selected item '{newSelectedItem}' not found in the items list.");

            _selectedIndex = index;
        }
        else
        {
            throw new ArgumentException($"SetValue for RadioButton expects int or string value, but got {newValue?.GetType().Name}");
        }

        BuildRenderFragment();
    }
}