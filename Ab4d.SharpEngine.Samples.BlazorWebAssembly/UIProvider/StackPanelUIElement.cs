using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;

namespace Ab4d.SharpEngine.Samples.Blazor.UIProvider;

public class StackPanelUIElement : BlazorUIPanel
{
    private bool _isVertical;
    private PositionTypes _alignment;
    private bool _addBorder;
    private bool _isSemiTransparent;
    private bool _isChildPanel;

    public override bool IsVertical => _isVertical;

    public StackPanelUIElement(BlazorUIProvider blazorUIProvider, PositionTypes alignment, bool isVertical, bool addBorder, bool isSemiTransparent, bool isChildPanel)
        : base(blazorUIProvider)
    {
        _isVertical = isVertical;
        _alignment = alignment;
        _addBorder = addBorder;
        _isSemiTransparent = isSemiTransparent;
        _isChildPanel = isChildPanel;

        SetMargin(5, 5, 5, 5);

        BuildRenderFragment();
    }

    protected override void RegeneratePanelRenderFragment()
    {
        BuildRenderFragment();
    }

    private void BuildRenderFragment()
    {
        BlazorElement = builder =>
        {
            builder.OpenElement(0, "div");
            var style = _isChildPanel ? "" : "position: absolute; padding: 5px 10px; ";

            style += GetMarginStyle() + GetVisibilityStyle();

            if (_addBorder)
            {
                style += "border: 2px solid black; ";

                if (_isSemiTransparent)
                    style += "background-color: rgba(255, 255, 255, 0.86); ";
                else
                    style += "background-color: white; ";
            }


            var horizontalAlign = _alignment.HasFlag(PositionTypes.Left) ? $"left: 0; " :
                                  _alignment.HasFlag(PositionTypes.Right) ? $"right: 0; " :
                                  "left: 0; right: 0; width: fit-content; margin-inline: auto; "; // center

            var verticalAlign = _alignment.HasFlag(PositionTypes.Top) ? $"top: 0; " :
                                _alignment.HasFlag(PositionTypes.Bottom) ? $"bottom: 0; " :
                                "top: 50%; bottom: 50%; height: fit-content; margin-inline: auto; "; // center

            style += horizontalAlign + verticalAlign;

            builder.AddAttribute(1, "style", style);

            BuildStackPanelContent(builder, 2);

            builder.CloseElement();
        };
    }

    private void BuildStackPanelContent(RenderTreeBuilder builder, int sequence)
    {
        builder.OpenElement(sequence, "div");

        var style = $"display: flex; flex-direction: {(_isVertical ? "column" : "row")};";

        builder.AddAttribute(sequence + 1, "style", style);

        // Render all child elements
        int childSequence = sequence + 2;
        foreach (var child in childUIElements)
        {
            builder.AddContent(childSequence++, child.BlazorElement);
        }

        builder.CloseElement();
    }
}