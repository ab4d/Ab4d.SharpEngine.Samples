using System;
using System.Windows;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.UIProvider;

public abstract class AvaloniaUIElement : ICommonSampleUIElement
{
    public virtual bool IsUpdateSupported => false;

    protected AvaloniaUIProvider avaloniaUIProvider;

    public Control AvaloniaControl { get; init; } = null!; // set to "null!" to prevent compiler warning


    protected AvaloniaUIElement(AvaloniaUIProvider avaloniaUIProvider)
    {
        this.avaloniaUIProvider = avaloniaUIProvider;
    }


    public virtual bool GetIsVisible() => AvaloniaControl.IsVisible;

    public virtual ICommonSampleUIElement SetIsVisible(bool isVisible)
    {
        AvaloniaControl.IsVisible = isVisible;
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
        var avaloniaColor = Color.FromRgb((byte)(color.Red * 255f), (byte)(color.Green * 255f), (byte)(color.Blue * 255f));
        OnSetColor(avaloniaColor);

        return this;
    }

    protected virtual void OnSetColor(Color avaloniaColor)
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


    public virtual string? GetToolTip() => null;// AvaloniaControl.ToolTip as string;

    public virtual ICommonSampleUIElement SetToolTip(string tooltip)
    {
        //AvaloniaControl.ToolTip = tooltip;
        return this;
    }


    /// <summary>
    /// Returns margin (left, top, right, bottom)
    /// </summary>
    /// <returns>margin (left, top, right, bottom)</returns>
    public virtual (double, double, double, double) GetMargin() => (AvaloniaControl.Margin.Left, AvaloniaControl.Margin.Top, AvaloniaControl.Margin.Right, AvaloniaControl.Margin.Bottom);

    public virtual ICommonSampleUIElement SetMargin(double left, double top, double right, double bottom)
    {
        AvaloniaControl.Margin = new Thickness(left, top, right, bottom);
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
}