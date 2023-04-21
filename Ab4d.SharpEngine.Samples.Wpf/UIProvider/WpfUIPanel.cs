using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Ab4d.SharpEngine.Samples.Common;

namespace Ab4d.SharpEngine.Samples.Wpf.UIProvider;

public abstract class WpfUIPanel : WpfUIElement, ICommonSampleUIPanel
{
    public virtual Panel? WpfPanel => WpfElement as Panel;

    protected List<WpfUIElement> childUIElements;

    public abstract bool IsVertical { get; }


    public WpfUIPanel(WpfUIProvider wpfUIProvider)
        : base(wpfUIProvider)
    {
        childUIElements = new List<WpfUIElement>();
    }

    public virtual int ChildrenCount => childUIElements.Count;

    public virtual ICommonSampleUIElement GetChild(int index) => childUIElements[index];
    
    public virtual WpfUIElement GetWpfChild(int index) => (WpfUIElement)childUIElements[index];

    public virtual void RemoveChildAt(int index)
    {
        childUIElements.RemoveAt(index);
        WpfPanel?.Children.RemoveAt(index);
    }

    public virtual void RemoveChild(ICommonSampleUIElement child)
    {
        if (child is WpfUIElement wpfUiElement)
        {
            childUIElements.Remove(wpfUiElement);
            WpfPanel?.Children.Remove(wpfUiElement.WpfElement);
        }
    }

    public virtual void AddChild(ICommonSampleUIElement child)
    {
        if (child is WpfUIElement wpfUiElement)
        {
            childUIElements.Add(wpfUiElement);
            WpfPanel?.Children.Add(wpfUiElement.WpfElement);
        }
    }

    public virtual void InsertChild(int index, ICommonSampleUIElement child)
    {
        if (child is WpfUIElement wpfUiElement)
        {
            childUIElements.Insert(index, wpfUiElement);
            WpfPanel?.Children.Insert(index, wpfUiElement.WpfElement);
        }
    }

    public virtual void ClearChildren()
    {
        childUIElements.Clear();
        WpfPanel?.Children.Clear();
    }
}