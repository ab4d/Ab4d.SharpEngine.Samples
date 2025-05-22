using System;
using System.Windows;
using Windows.UI;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;
using Microsoft.UI.Xaml;

namespace Ab4d.SharpEngine.Samples.WinUI.UIProvider;

public abstract class WinUIElement : ICommonSampleUIElement
{
    public virtual bool IsUpdateSupported => false;

    protected WinUIProvider winUIProvider;

    public FrameworkElement Element { get; init; } = null!; // set to "null!" to prevent compiler warning


    protected WinUIElement(WinUIProvider winUIProvider)
    {
        this.winUIProvider = winUIProvider;
    }


    public virtual bool GetIsVisible() => Element.Visibility == Visibility.Visible;

    public virtual ICommonSampleUIElement SetIsVisible(bool isVisible)
    {
        Element.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
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
        var winUIColor = Color.FromArgb(255, (byte)(color.Red * 255f), (byte)(color.Green * 255f), (byte)(color.Blue * 255f));
        OnSetColor(winUIColor);

        return this;
    }

    protected virtual void OnSetColor(Color winUIColor)
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


    public virtual string? GetToolTip() => null; //WpfElement.ToolTip as string;

    public virtual ICommonSampleUIElement SetToolTip(string tooltip)
    {
        //WpfElement.ToolTip = tooltip;
        return this;
    }


    /// <summary>
    /// Returns margin (left, top, right, bottom)
    /// </summary>
    /// <returns>margin (left, top, right, bottom)</returns>
    public virtual (double, double, double, double) GetMargin() => (Element.Margin.Left, Element.Margin.Top, Element.Margin.Right, Element.Margin.Bottom);

    public virtual ICommonSampleUIElement SetMargin(double left, double top, double right, double bottom)
    {
        Element.Margin = new Thickness(left, top, right, bottom);
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
}