using System;
using Microsoft.AspNetCore.Components;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;

namespace Ab4d.SharpEngine.Samples.Blazor.UIProvider;

public abstract class BlazorUIElement : ICommonSampleUIElement
{
    public virtual bool IsUpdateSupported => false;

    protected BlazorUIProvider blazorUIProvider;

    public RenderFragment BlazorElement { get; set; } = null!; // set to "null!" to prevent compiler warning

    // Store element state
    protected bool _isVisible = true;
    protected string? _tooltip;
    protected (double left, double top, double right, double bottom) _margin = (0, 0, 0, 0);


    protected BlazorUIElement(BlazorUIProvider blazorUIProvider)
    {
        this.blazorUIProvider = blazorUIProvider;
    }


    public virtual bool GetIsVisible() => _isVisible;

    public virtual ICommonSampleUIElement SetIsVisible(bool isVisible)
    {
        _isVisible = isVisible;
        return this;
    }


    public virtual string? GetText()
    {
        throw new InvalidOperationException(); // This must be provided by a derived element (if supported)
    }

    public virtual ICommonSampleUIElement SetText(string? text)
    {
        throw new InvalidOperationException(); // This must be provided by a derived element (if supported)
    }

    public ICommonSampleUIElement SetColor(Color3 color)
    {
        var htmlColor = $"rgb({(int)(color.Red * 255)}, {(int)(color.Green * 255)}, {(int)(color.Blue * 255)})";
        OnSetColor(htmlColor);

        return this;
    }

    protected virtual void OnSetColor(string htmlColor)
    {

    }


    public virtual string? GetStyle()
    {
        throw new InvalidOperationException(); // This must be provided by a derived element (if supported)
    }

    public virtual ICommonSampleUIElement SetStyle(string style)
    {
        throw new InvalidOperationException(); // This must be provided by a derived element (if supported)
    }


    public virtual string? GetToolTip() => _tooltip;

    public virtual ICommonSampleUIElement SetToolTip(string tooltip)
    {
        _tooltip = tooltip;
        return this;
    }


    /// <summary>
    /// Returns margin (left, top, right, bottom)
    /// </summary>
    /// <returns>margin (left, top, right, bottom)</returns>
    public (double left, double top, double right, double bottom) GetMargin() => _margin;

    public ICommonSampleUIElement SetMargin(double left, double top, double right, double bottom)
    {
        _margin = (left, top, right, bottom);
        return this;
    }


    public virtual void SetProperty(string propertyName, string propertyValue)
    {
        throw new System.NotImplementedException();
    }

    public virtual string? GetPropertyValue(string propertyName)
    {
        throw new System.NotImplementedException();
    }
    
    
    public virtual void UpdateValue()
    {
        throw new InvalidOperationException(); // This must be provided by a derived element (if supported)
    }


    public virtual void SetValue(object newValue)
    {
        throw new NotImplementedException($"SetValue for {this.GetType().Name} not implemented"); // This must be provided by a derived element (if supported)
    }


    // Helper method to generate margin CSS style
    protected string GetMarginStyle()
    {
        if (_margin == (0, 0, 0, 0))
            return string.Empty;

        return $"margin: {_margin.top}px {_margin.right}px {_margin.bottom}px {_margin.left}px; ";
    }

    // Helper method to generate visibility CSS style
    protected string GetVisibilityStyle()
    {
        return _isVisible ? string.Empty : "display: none; ";
    }
}