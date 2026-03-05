using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Ab4d.SharpEngine.Samples.Common;

namespace Ab4d.SharpEngine.Samples.Blazor.UIProvider;

public abstract class BlazorUIPanel : BlazorUIElement, ICommonSampleUIPanel
{
    protected List<BlazorUIElement> childUIElements;

    public abstract bool IsVertical { get; }


    public BlazorUIPanel(BlazorUIProvider blazorUIProvider)
        : base(blazorUIProvider)
    {
        childUIElements = new List<BlazorUIElement>();
    }

    public virtual int ChildrenCount => childUIElements.Count;

    public virtual ICommonSampleUIElement GetChild(int index) => childUIElements[index];

    public virtual BlazorUIElement GetBlazorChild(int index) => (BlazorUIElement)childUIElements[index];

    public virtual void RemoveChildAt(int index)
    {
        childUIElements.RemoveAt(index);
        // In Blazor, the panel's RenderFragment will be regenerated
        RegeneratePanelRenderFragment();
    }

    public virtual void RemoveChild(ICommonSampleUIElement child)
    {
        if (child is BlazorUIElement blazorUiElement)
        {
            childUIElements.Remove(blazorUiElement);
            // In Blazor, the panel's RenderFragment will be regenerated
            RegeneratePanelRenderFragment();
        }
    }

    public virtual void AddChild(ICommonSampleUIElement child)
    {
        if (child is BlazorUIElement blazorUiElement)
        {
            childUIElements.Add(blazorUiElement);
            // In Blazor, the panel's RenderFragment will be regenerated
            RegeneratePanelRenderFragment();
        }
    }

    public virtual void InsertChild(int index, ICommonSampleUIElement child)
    {
        if (child is BlazorUIElement blazorUiElement)
        {
            childUIElements.Insert(index, blazorUiElement);
            // In Blazor, the panel's RenderFragment will be regenerated
            RegeneratePanelRenderFragment();
        }
    }

    public virtual void ClearChildren()
    {
        childUIElements.Clear();
        // In Blazor, the panel's RenderFragment will be regenerated
        RegeneratePanelRenderFragment();
    }

    protected abstract void RegeneratePanelRenderFragment();
}