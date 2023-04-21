using System;
using System.Collections.Generic;
using Ab4d.SharpEngine.Samples.Common;
using Microsoft.UI.Xaml.Controls;

namespace Ab4d.SharpEngine.Samples.WinUI.UIProvider;

public abstract class WinUIPanel : WinUIElement, ICommonSampleUIPanel
{
    public virtual Panel? Panel => Element as Panel;

    protected List<WinUIElement> childUIElements;

    public abstract bool IsVertical { get; }


    public WinUIPanel(WinUIProvider winUIProvider)
        : base(winUIProvider)
    {
        childUIElements = new List<WinUIElement>();
    }

    public virtual int ChildrenCount => childUIElements.Count;

    public virtual ICommonSampleUIElement GetChild(int index) => childUIElements[index];
    
    public virtual WinUIElement GetWinUIChild(int index) => (WinUIElement)childUIElements[index];

    public virtual void RemoveChildAt(int index)
    {
        childUIElements.RemoveAt(index);
        Panel?.Children.RemoveAt(index);
    }

    public virtual void RemoveChild(ICommonSampleUIElement child)
    {
        if (child is WinUIElement winUiElement)
        {
            childUIElements.Remove(winUiElement);
            Panel?.Children.Remove(winUiElement.Element);
        }
    }

    public virtual void AddChild(ICommonSampleUIElement child)
    {
        if (child is WinUIElement winUiElement)
        {
            childUIElements.Add(winUiElement);
            Panel?.Children.Add(winUiElement.Element);
        }
    }

    public virtual void InsertChild(int index, ICommonSampleUIElement child)
    {
        if (child is WinUIElement winUiElement)
        {
            childUIElements.Insert(index, winUiElement);
            Panel?.Children.Insert(index, winUiElement.Element);
        }
    }

    public virtual void ClearChildren()
    {
        childUIElements.Clear();
        Panel?.Children.Clear();
    }
}