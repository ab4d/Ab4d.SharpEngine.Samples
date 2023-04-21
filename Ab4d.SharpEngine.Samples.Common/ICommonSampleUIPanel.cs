namespace Ab4d.SharpEngine.Samples.Common;

public interface ICommonSampleUIPanel : ICommonSampleUIElement
{
    int ChildrenCount { get; }

    bool IsVertical { get; }

    ICommonSampleUIElement GetChild(int index);

    void RemoveChildAt(int index);

    void RemoveChild(ICommonSampleUIElement child);

    void AddChild(ICommonSampleUIElement child);
    
    void InsertChild(int index, ICommonSampleUIElement child);

    void ClearChildren();
}