using Ab4d.SharpEngine.Common;

namespace Ab4d.SharpEngine.Samples.Common;

public interface ICommonSampleUIElement
{
    bool GetIsVisible();
    ICommonSampleUIElement SetIsVisible(bool isVisible);

    string? GetText();
    ICommonSampleUIElement SetText(string? text);

    ICommonSampleUIElement SetColor(Color3 color);

    string? GetStyle();
    ICommonSampleUIElement SetStyle(string style);

    string? GetToolTip();
    ICommonSampleUIElement SetToolTip(string tooltip);

    (double left, double top, double right, double bottom) GetMargin();
    ICommonSampleUIElement SetMargin(double left, double top, double right, double bottom);

    void SetProperty(string propertyName, string propertyValue);
    string? GetPropertyValue(string propertyName);

    void UpdateValue();

    void SetValue(object newValue);
}