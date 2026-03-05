using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Ab4d.SharpEngine.Samples.Common;

namespace Ab4d.SharpEngine.Samples.Blazor.UIProvider;

public class ComboBoxUIElement : BlazorUIElement
{
    private string[] _items;
    private int _selectedIndex;
    private Action<int, string?> _itemChangedAction;
    private double _width;
    private string? _keyText;
    private double _keyTextWidth;
    private string? _color;

    public ComboBoxUIElement(BlazorUIProvider blazorUIProvider,
                             string[] items,
                             Action<int, string?> itemChangedAction,
                             int selectedItemIndex,
                             double width = 0,
                             string? keyText = null,
                             double keyTextWidth = 0)
        : base(blazorUIProvider)
    {
        _items = items;
        _selectedIndex = selectedItemIndex;
        _itemChangedAction = itemChangedAction;
        _width = width;
        _keyTextWidth = keyTextWidth;

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
                builder.AddAttribute(1, "style", "display: flex; align-items: center;" + GetMarginStyle() + GetVisibilityStyle());

                builder.OpenElement(2, "span");
                var labelStyle = string.Empty;

                if (_keyTextWidth > 0)
                    labelStyle += $"width: {_keyTextWidth}px;";

                labelStyle += "margin-right: 5px;";

                if (blazorUIProvider.FontSize > 0)
                    labelStyle += $"font-size: {blazorUIProvider.FontSize}px;";

                builder.AddAttribute(3, "style", labelStyle);

                if (!string.IsNullOrEmpty(_tooltip))
                    builder.AddAttribute(4, "title", _tooltip);

                builder.AddContent(5, _keyText);
                builder.CloseElement();

                BuildSelectElement(builder, 6);

                builder.CloseElement();
            }
            else
            {
                BuildSelectElement(builder, 0);
            }
        };
    }

    private void BuildSelectElement(RenderTreeBuilder builder, int sequence)
    {
        builder.OpenElement(sequence, "select");

        var style = string.Empty;

        if (_keyText == null)
            style += GetMarginStyle() + GetVisibilityStyle();

        if (_width > 0)
            style += $"width: {_width}px;";

        if (blazorUIProvider.FontSize > 0)
            style += $"font-size: {blazorUIProvider.FontSize}px;";

        if (_color != null)
            style += $"color: {_color};";

        if (!string.IsNullOrEmpty(style))
            builder.AddAttribute(sequence + 1, "style", style);

        if (_keyText == null && !string.IsNullOrEmpty(_tooltip))
            builder.AddAttribute(sequence + 2, "title", _tooltip);

        builder.AddAttribute(sequence + 3, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, e =>
        {
            if (int.TryParse(e.Value?.ToString(), out var newIndex))
            {
                _selectedIndex = newIndex;
                string? selectedText = newIndex < 0 || newIndex > _items.Length - 1 ? null : _items[newIndex];
                _itemChangedAction?.Invoke(newIndex, selectedText);
            }
        }));

        for (int i = 0; i < _items.Length; i++)
        {
            builder.OpenElement(sequence + 4 + i * 3, "option");
            builder.AddAttribute(sequence + 5 + i * 3, "value", i.ToString());

            if (i == _selectedIndex)
                builder.AddAttribute(sequence + 6 + i * 3, "selected", "selected");

            builder.AddContent(sequence + 7 + i * 3, _items[i]);
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
            _selectedIndex = newIndex;
        }
        else if (newValue is string newSelectedItem)
        {
            _selectedIndex = Array.IndexOf(_items, newSelectedItem);
        }
        else
        {
            throw new ArgumentException($"SetValue for ComboBox expects int or string value, but got {newValue?.GetType().Name}");
        }

        BuildRenderFragment();

        _itemChangedAction?.Invoke(_selectedIndex, _items[_selectedIndex]);

        // Trigger a re-render of the parent component
        blazorUIProvider.NotifyStateChanged();
    }
}