using System;
using System.Collections.Generic;
using Ab4d.SharpEngine.Samples.Common;
using Avalonia.Controls;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.UIProvider;

public abstract class AvaloniaUIPanel : AvaloniaUIElement, ICommonSampleUIPanel
{
    public virtual Panel? AvaloniaPanel => AvaloniaControl as Panel;

    protected List<AvaloniaUIElement> childUIElements;


    public AvaloniaUIPanel(AvaloniaUIProvider avaloniaUIProvider)
        : base(avaloniaUIProvider)
    {
        childUIElements = new List<AvaloniaUIElement>();
    }

    public virtual int ChildrenCount => childUIElements.Count;

    public virtual ICommonSampleUIElement GetChild(int index) => childUIElements[index];
    
    public virtual AvaloniaUIElement GetAvaloniaChild(int index) => (AvaloniaUIElement)childUIElements[index];

    public virtual void RemoveChildAt(int index)
    {
        childUIElements.RemoveAt(index);
        AvaloniaPanel?.Children.RemoveAt(index);
    }

    public virtual void RemoveChild(ICommonSampleUIElement child)
    {
        if (child is AvaloniaUIElement avaloniaUiElement)
        {
            childUIElements.Remove(avaloniaUiElement);
            AvaloniaPanel?.Children.Remove(avaloniaUiElement.AvaloniaControl);
        }
    }

    public virtual void AddChild(ICommonSampleUIElement child)
    {
        if (child is AvaloniaUIElement avaloniaUiElement)
        {
            childUIElements.Add(avaloniaUiElement);
            AvaloniaPanel?.Children.Add(avaloniaUiElement.AvaloniaControl);
        }
    }

    public virtual void InsertChild(int index, ICommonSampleUIElement child)
    {
        if (child is AvaloniaUIElement avaloniaUiElement)
        {
            childUIElements.Insert(index, avaloniaUiElement);
            AvaloniaPanel?.Children.Insert(index, avaloniaUiElement.AvaloniaControl);
        }
    }

    public virtual void ClearChildren()
    {
        childUIElements.Clear();
        AvaloniaPanel?.Children.Clear();
    }
}