using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Ab4d.SharpEngine.Samples.Common;

namespace Ab4d.SharpEngine.Samples.Blazor.UIProvider;

public class LabelUIElement : BlazorUIElement
{
    private string _text;
    private string? _styleString;
    private double _width;
    private double _height;
    private double _maxWidth;
    private bool _isHeader;
    private string? _color;

    public LabelUIElement(BlazorUIProvider blazorUIProvider, string text, bool isHeader, float width = 0, float height = 0, float maxWidth = 0)
        : base(blazorUIProvider)
    {
        _isHeader = isHeader;
        _width = width;
        _height = height;
        _maxWidth = maxWidth;

        var (textToShow, toolTip) = blazorUIProvider.ParseTextAndToolTip(text);
        _text = textToShow ?? string.Empty;

        if (toolTip != null)
            SetToolTip(toolTip);

        if (isHeader)
        {
            _styleString = "bold";

            double topMargin = blazorUIProvider.HeaderTopMargin;
            if (blazorUIProvider.CurrentPanel != null && blazorUIProvider.CurrentPanel.ChildrenCount > 0)
                topMargin += 8;

            SetMargin(0, topMargin, 0, blazorUIProvider.HeaderBottomMarin);
        }

        BuildRenderFragment();
    }

    private void BuildRenderFragment()
    {
        BlazorElement = builder =>
        {
            builder.OpenElement(0, "span");

            // Add "white-space: pre-wrap;" - preserves new lines ('\n') in text
            var style = "white-space: pre-wrap;" + GetMarginStyle() + GetVisibilityStyle();

            if (_width > 0)
                style += $"width: {_width}px;";

            if (_height > 0)
                style += $"height: {_height}px;";

            if (_maxWidth > 0)
                style += $"max-width: {_maxWidth}px;";

            if (blazorUIProvider.FontSize > 0)
                style += $"font-size: {blazorUIProvider.FontSize}px;";

            style += "vertical-align: middle; display: inline-block; word-wrap: break-word;";

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

            builder.AddContent(3, _text);
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