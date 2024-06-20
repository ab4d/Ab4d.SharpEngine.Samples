using System;
using System.Collections.Generic;
using Ab4d.SharpEngine.Samples.Common;

namespace Ab4d.SharpEngine.Samples.WinForms.UIProvider;

public abstract class WinFormsUIPanel : WinFormsUIElement, ICommonSampleUIPanel
{
    public virtual Panel? WinFormsPanel => WinFormsControl as Panel;

    protected List<WinFormsUIElement> childUIElements;

    public abstract bool IsVertical { get; }

    public WinFormsUIPanel(WinFormsUIProvider winFormsUIProvider)
        : base(winFormsUIProvider)
    {
        childUIElements = new List<WinFormsUIElement>();
    }

    public virtual int ChildrenCount => childUIElements.Count;

    public virtual ICommonSampleUIElement GetChild(int index) => childUIElements[index];

    public virtual WinFormsUIElement GetWinFormsChild(int index) => (WinFormsUIElement)childUIElements[index];

    public virtual void RemoveChildAt(int index)
    {
        childUIElements.RemoveAt(index);
        WinFormsPanel?.Controls.RemoveAt(index);
    }

    public virtual void RemoveChild(ICommonSampleUIElement child)
    {
        if (child is WinFormsUIElement winFormsUiElement)
        {
            childUIElements.Remove(winFormsUiElement);
            WinFormsPanel?.Controls.Remove(winFormsUiElement.WinFormsControl);
        }
    }

    public virtual void AddChild(ICommonSampleUIElement child)
    {
        if (child is WinFormsUIElement winFormsUiElement)
        {
            childUIElements.Add(winFormsUiElement);
            WinFormsPanel?.Controls.Add(winFormsUiElement.WinFormsControl);
        }
    }

    public virtual void InsertChild(int index, ICommonSampleUIElement child)
    {
        if (child is WinFormsUIElement winFormsUiElement)
        {
            childUIElements.Insert(index, winFormsUiElement);
            WinFormsPanel?.Controls.Add(winFormsUiElement.WinFormsControl);
            WinFormsPanel?.Controls.SetChildIndex(winFormsUiElement.WinFormsControl, index);
        }
    }

    public virtual void ClearChildren()
    {
        childUIElements.Clear();
        WinFormsPanel?.Controls.Clear();
    }
}