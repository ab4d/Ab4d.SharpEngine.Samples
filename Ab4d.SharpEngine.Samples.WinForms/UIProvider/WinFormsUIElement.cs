using System;
using System.Windows;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;

namespace Ab4d.SharpEngine.Samples.WinForms.UIProvider;

public abstract class WinFormsUIElement : ICommonSampleUIElement
{
    public virtual bool IsUpdateSupported => false;

    protected WinFormsUIProvider winFormsUIProvider;

    public Control WinFormsControl { get; init; } = null!; // set to "null!" to prevent compiler warning


    protected WinFormsUIElement(WinFormsUIProvider winFormsUIProvider)
    {
        this.winFormsUIProvider = winFormsUIProvider;
    }


    public virtual bool GetIsVisible() => WinFormsControl.Visible;

    public virtual ICommonSampleUIElement SetIsVisible(bool isVisible)
    {
        WinFormsControl.Visible = isVisible;
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
        var winFormsColor = Color.FromArgb((byte)(color.Red * 255f), (byte)(color.Green * 255f), (byte)(color.Blue * 255f));
        OnSetColor(winFormsColor);

        return this;
    }

    protected virtual void OnSetColor(Color winFormsColor)
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


    public virtual string? GetToolTip() => null;// WinFormsControl.ToolTip as string;

    public virtual ICommonSampleUIElement SetToolTip(string tooltip)
    {
        //WinFormsControl.ToolTip = tooltip;
        return this;
    }


    /// <summary>
    /// Returns margin (left, top, right, bottom)
    /// </summary>
    /// <returns>margin (left, top, right, bottom)</returns>
    public virtual (double left, double top, double right, double bottom) GetMargin() => (WinFormsControl.Margin.Left, WinFormsControl.Margin.Top, WinFormsControl.Margin.Right, WinFormsControl.Margin.Bottom);

    public virtual ICommonSampleUIElement SetMargin(double left, double top, double right, double bottom)
    {
        WinFormsControl.Margin = new Padding((int)left, (int)top, (int)right, (int)bottom);
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