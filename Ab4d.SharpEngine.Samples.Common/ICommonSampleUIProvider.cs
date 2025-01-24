using System.Numerics;
using Ab4d.SharpEngine.Common;

namespace Ab4d.SharpEngine.Samples.Common;

public interface ICommonSampleUIProvider
{
    /// <summary>
    /// Returns "WPF", "Avalonia", ...
    /// </summary>
    /// <returns></returns>
    string GetProviderType();

    void ClearAll();

    ICommonSampleUIPanel CreateStackPanel(PositionTypes alignment, bool isVertical = true, bool addBorder = true, bool isSemiTransparent = true, ICommonSampleUIPanel? parent = null);

    void SetCurrentPanel(ICommonSampleUIPanel? panel);

    ICommonSampleUIElement CreateLabel(string text, bool isHeader = false, float width = 0, float height = 0, float maxWidth = 0);

    ICommonSampleUIElement CreateKeyValueLabel(string? keyText, Func<string> getValueTextFunc, double keyTextWidth = 0);

    ICommonSampleUIElement CreateButton(string text, Action clickedAction, double width = 0, bool alignLeft = false);

    ICommonSampleUIElement CreateSlider(float minValue, float maxValue, Func<float> getValueFunc, Action<float> setValueAction, double width = 0, bool showTicks = false, string? keyText = null, double keyTextWidth = 0, Func<float, string>? formatShownValueFunc = null, double shownValueWidth = 0);

    ICommonSampleUIElement CreateComboBox(string[] items, Action<int, string?> itemChangedAction, int selectedItemIndex, double width = 0, string? keyText = null, double keyTextWidth = 0);
    
    ICommonSampleUIElement CreateRadioButtons(string[] items, Action<int, string> checkedItemChangedAction, int selectedItemIndex);

    ICommonSampleUIElement CreateCheckBox(string text, bool isInitiallyChecked, Action<bool> checkedChangedAction);
    
    ICommonSampleUIElement CreateTextBox(float width, float height = 0, string? initialText = null, Action<string>? textChangedAction = null);
    
    ICommonSampleUIElement CreateMarkdownText(string markdownText, float fontSize = 0);

    void AddSeparator(double height = 8);

    void UpdateAllValues();

    // returns false is UI does not provide keyboard support
    bool RegisterKeyDown(Func<string, bool>? keyDownFunc);

    // Returns false, if not supported
    bool RegisterPointerMoved(Action<Vector2> pointerMovedAction);

    // returns false is UI does not provide file drag and drop
    bool RegisterFileDropped(string? filePattern, Action<string> fileDroppedAction);


    string[] GetAllSettingKeys();
    
    string? GetSettingText(string settingKey);

    float GetSettingFloat(string settingKey);

    void SetSettingText(string settingKey, string? newValue);
    
    void SetSettingFloat(string settingKey, float newValue);
}