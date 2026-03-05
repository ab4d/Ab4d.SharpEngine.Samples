using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Ab4d.SharpEngine.Samples.Common;

namespace Ab4d.SharpEngine.Samples.Blazor.UIProvider;

public class ButtonUIElement : BlazorUIElement
{
    private string _text;
    private Action _clickedAction;
    private double _width;
    private bool _alignLeft;

    public ButtonUIElement(BlazorUIProvider blazorUIProvider, string text, Action clickedAction, double width = 0, bool alignLeft = false)
        : base(blazorUIProvider)
    {
        _clickedAction = clickedAction;
        _width = width;
        _alignLeft = alignLeft;

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
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "class", "btn");

            var style = GetMarginStyle() + GetVisibilityStyle();

            if (_width > 0)
                style += $"width: {_width}px;";

            style += "text-align: " + (_alignLeft ? "left;" : "center;");

            if (blazorUIProvider.FontSize > 0)
                style += $"font-size: {blazorUIProvider.FontSize}px;";

            if (!string.IsNullOrEmpty(style))
                builder.AddAttribute(2, "style", style);

            if (!string.IsNullOrEmpty(_tooltip))
                builder.AddAttribute(3, "title", _tooltip);

            builder.AddAttribute(4, "onclick", EventCallback.Factory.Create(this, () => _clickedAction?.Invoke()));

            builder.AddContent(5, _text);
            builder.CloseElement();
        };
    }

    public override string? GetText() => _text;

    public override ICommonSampleUIElement SetText(string? text)
    {
        var (textToShow, toolTip) = blazorUIProvider.ParseTextAndToolTip(text);
        _text = textToShow ?? string.Empty;

        if (toolTip != null)
            SetToolTip(toolTip);

        BuildRenderFragment();

        return this;
    }
}