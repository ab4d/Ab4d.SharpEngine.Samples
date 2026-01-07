using System;
using System.Windows;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;

namespace Ab4d.SharpEngine.Samples.Wpf.UIProvider;

public abstract class WpfUIElement : ICommonSampleUIElement
{
    public virtual bool IsUpdateSupported => false;

    protected WpfUIProvider wpfUIProvider;

    public FrameworkElement WpfElement { get; init; } = null!; // set to "null!" to prevent compiler warning


    protected WpfUIElement(WpfUIProvider wpfUIProvider)
    {
        this.wpfUIProvider = wpfUIProvider;
    }


    public virtual bool GetIsVisible() => WpfElement.IsVisible;

    public virtual ICommonSampleUIElement SetIsVisible(bool isVisible)
    {
        WpfElement.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
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
        var wpfColor = System.Windows.Media.Color.FromRgb((byte)(color.Red * 255f), (byte)(color.Green * 255f), (byte)(color.Blue * 255f));
        OnSetColor(wpfColor);

        return this;
    }

    protected virtual void OnSetColor(System.Windows.Media.Color wpfColor)
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


    public virtual string? GetToolTip() => WpfElement.ToolTip as string;

    public virtual ICommonSampleUIElement SetToolTip(string tooltip)
    {
        WpfElement.ToolTip = tooltip;
        return this;
    }


    /// <summary>
    /// Returns margin (left, top, right, bottom)
    /// </summary>
    /// <returns>margin (left, top, right, bottom)</returns>
    public virtual (double left, double top, double right, double bottom) GetMargin() => (WpfElement.Margin.Left, WpfElement.Margin.Top, WpfElement.Margin.Right, WpfElement.Margin.Bottom);

    public virtual ICommonSampleUIElement SetMargin(double left, double top, double right, double bottom)
    {
        WpfElement.Margin = new Thickness(left, top, right, bottom);
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