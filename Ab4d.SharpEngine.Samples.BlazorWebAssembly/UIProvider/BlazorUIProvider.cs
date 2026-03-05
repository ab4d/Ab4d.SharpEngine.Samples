using Ab4d.SharpEngine.Browser;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;
using System.Globalization;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Blazor.UIProvider;

public class BlazorUIProvider : ICommonSampleUIProvider
{
    public const string StandardMarginSettingsKey = "StandardMargin";
    public const string HeaderTopMarginSettingsKey = "HeaderTopMargin";
    public const string HeaderBottomMarinSettingsKey = "HeaderBottomMarin";
    public const string FontSizeSettingsKey = "FontSize";

    public const string ToolTipTextKey = " (?):";

    public string GetProviderType() => "Blazor";

    private Dictionary<string, string?> _settings;

    public BlazorUIPanel? CurrentPanel { get; private set; }

    // For Blazor, we'll store a reference to a container element or component that can be updated
    private Action? _stateChangedCallback;

    private double _lastSeparator;

    private List<BlazorUIElement> _rootUIElements;

    // private Func<string, bool>? _keyDownFunc;
    // private DragAndDropHelper? _dragAndDropHelper;

    public double StandardMargin { get; private set; }
    public double HeaderTopMargin { get; private set; }
    public double HeaderBottomMarin { get; private set; }
    public double FontSize { get; private set; }

    private double GetDefaultHeaderTopMargin() => StandardMargin * 2;
    private double GetDefaultHeaderBottomMarin() => StandardMargin * 1.3;
    
    public ICanvasInterop? CanvasInterop { get; set; }
    private ICanvasInterop? _subscribedCanvasInterop;
    private Action<Vector2>? _pointerMovedAction;

    public BlazorUIProvider(Action? stateChangedCallback = null)
    {
        _stateChangedCallback = stateChangedCallback;

        _rootUIElements = new List<BlazorUIElement>();

        _settings = new Dictionary<string, string?>();
        ResetSettings();
    }

    private void ResetSettings()
    {
        _settings.Clear();

        SetSettingFloat(StandardMarginSettingsKey, 3);
        SetSettingFloat(HeaderTopMarginSettingsKey, (float)GetDefaultHeaderTopMargin());
        SetSettingFloat(HeaderBottomMarinSettingsKey, (float)GetDefaultHeaderBottomMarin());
        SetSettingFloat(FontSizeSettingsKey, 14);

        if (_subscribedCanvasInterop != null)
        {
            _subscribedCanvasInterop.PointerMoved -= OnPointerMoved;
            _subscribedCanvasInterop = null;
        }
    }

    private void AddToCurrentPanel(BlazorUIElement blazorUiElement)
    {
        if (_lastSeparator > 0 || StandardMargin > 0)
        {
            var margin = blazorUiElement.GetMargin();

            double marginValue = margin.top;

            if (marginValue == 0)
                marginValue = StandardMargin;

            marginValue += _lastSeparator;

            bool isCurrentPanelVertical = CurrentPanel != null ? CurrentPanel.IsVertical : true;

            if (isCurrentPanelVertical)
                blazorUiElement.SetMargin(margin.left, marginValue, margin.right, margin.bottom);
            else
                blazorUiElement.SetMargin(marginValue, margin.top, margin.right, margin.bottom);

            _lastSeparator = 0;
        }

        if (CurrentPanel == null)
        {
            _rootUIElements.Add(blazorUiElement);
        }
        else
        {
            CurrentPanel.AddChild(blazorUiElement);
        }

        _stateChangedCallback?.Invoke();
    }

    public void ClearAll()
    {
        _rootUIElements.Clear();

        CurrentPanel = null;
        _lastSeparator = 0;

        ResetSettings();

        _stateChangedCallback?.Invoke();
    }

    public ICommonSampleUIPanel CreateStackPanel(PositionTypes alignment, bool isVertical = true, bool addBorder = true, bool isSemiTransparent = true, ICommonSampleUIPanel? parent = null)
    {
        var stackPanelUiElement = new StackPanelUIElement(this, alignment, isVertical, addBorder, isSemiTransparent, isChildPanel: parent != null);

        if (parent != null)
        {
            parent.AddChild(stackPanelUiElement);
        }
        else
        {
            _rootUIElements.Add(stackPanelUiElement);
        }

        CurrentPanel = stackPanelUiElement;

        _stateChangedCallback?.Invoke();

        return stackPanelUiElement;
    }

    public void SetCurrentPanel(ICommonSampleUIPanel? panel)
    {
        CurrentPanel = panel as BlazorUIPanel;
    }

    public ICommonSampleUIElement CreateLabel(string text, bool isHeader = false, float width = 0, float height = 0, float maxWidth = 0)
    {
        var newElement = new LabelUIElement(this, text, isHeader, width, height, maxWidth);
        AddToCurrentPanel(newElement);

        return newElement;
    }

    public ICommonSampleUIElement CreateKeyValueLabel(string? keyText, Func<string> getValueTextFunc, double keyTextWidth = 0)
    {
        var newElement = new KeyValueLabelUIElement(this, keyText, getValueTextFunc, keyTextWidth);
        AddToCurrentPanel(newElement);

        return newElement;
    }

    public ICommonSampleUIElement CreateButton(string text, Action clickedAction, double width = 0, bool alignLeft = false)
    {
        var newElement = new ButtonUIElement(this, text, clickedAction, width, alignLeft);
        AddToCurrentPanel(newElement);

        return newElement;
    }

    public ICommonSampleUIElement CreateSlider(float minValue, float maxValue, Func<float> getValueFunc, Action<float> setValueAction, double width = 0D, bool showTicks = false, string? keyText = null, double keyTextWidth = 0D, Func<float, string>? formatShownValueFunc = null, double shownValueWidth = 0)
    {
        var newElement = new SliderUIElement(this, minValue, maxValue, getValueFunc, setValueAction, width, showTicks, keyText, keyTextWidth, formatShownValueFunc, shownValueWidth);
        AddToCurrentPanel(newElement);
    
        return newElement;
    }

    public ICommonSampleUIElement CreateComboBox(string[] items, Action<int, string?> itemChangedAction, int selectedItemIndex, double width = 0, string? keyText = null, double keyTextWidth = 0)
    {
        var newElement = new ComboBoxUIElement(this, items, itemChangedAction, selectedItemIndex, width, keyText, keyTextWidth);
        AddToCurrentPanel(newElement);

        return newElement;
    }

    public ICommonSampleUIElement CreateRadioButtons(string[] items, Action<int, string> checkedItemChangedAction, int selectedItemIndex)
    {
        var newElement = new RadioButtonsUIElement(this, items, checkedItemChangedAction, selectedItemIndex);
        AddToCurrentPanel(newElement);

        return newElement;
    }

    public ICommonSampleUIElement CreateCheckBox(string text, bool isInitiallyChecked, Action<bool> checkedChangedAction)
    {
        var newElement = new CheckBoxUIElement(this, text, isInitiallyChecked, checkedChangedAction);
        AddToCurrentPanel(newElement);

        return newElement;
    }

    public ICommonSampleUIElement CreateTextBox(float width, float height = 0, string? initialText = null, Action<string>? textChangedAction = null)
    {
        var newElement = new TextBoxUIElement(this, width, height, initialText, textChangedAction);
        AddToCurrentPanel(newElement);

        return newElement;
    }

    // This will be implemented later
    // public ICommonSampleUIElement CreateMarkdownText(string text, float fontSize = 0)
    // {
    //     var newElement = new MarkdownUIElement(this, text, fontSize);
    //     AddToCurrentPanel(newElement);
    //
    //     return newElement;
    // }

    public void AddSeparator(double height = 8)
    {
        _lastSeparator += height;
    }

    public void UpdateAllValues()
    {
        foreach (var uiElement in _rootUIElements)
        {
            if (uiElement is BlazorUIPanel blazorUiPanel)
                UpdateAllValues(blazorUiPanel);
            else if (uiElement.IsUpdateSupported)
                uiElement.UpdateValue();
        }

        _stateChangedCallback?.Invoke();
    }
    
    private void UpdateAllValues(BlazorUIPanel blazorUiPanel)
    {
        var childrenCount = blazorUiPanel.ChildrenCount;

        for (int i = 0; i < childrenCount; i++)
        {
            var child = blazorUiPanel.GetChild(i) as BlazorUIElement;

            if (child is BlazorUIPanel childBlazorUiPanel)
                UpdateAllValues(childBlazorUiPanel);
            else if (child != null && child.IsUpdateSupported)
                child.UpdateValue();
        }
    }

    public (string? text, string? toolTip) ParseTextAndToolTip(string? textWithToolTip)
    {
        if (textWithToolTip == null)
            return (null, null);

        var index = textWithToolTip.IndexOf(ToolTipTextKey, StringComparison.InvariantCulture);

        if (index == -1)
            return (textWithToolTip, null); // no tool tip

        string? text;
        if (index == 0)
        {
            text = null;
        }
        else
        {
            text = textWithToolTip.Substring(0, index); // strip off the " (?):"
            if (text.EndsWith(':'))
                text = text.Substring(0, text.Length - 1) + " 🛈:"; // " (?):";
            else
                text += " 🛈"; // " (?)";
        }

        string? toolTip;
        if (index == textWithToolTip.Length - ToolTipTextKey.Length)
            toolTip = null;
        else
            toolTip = textWithToolTip.Substring(index + ToolTipTextKey.Length);

        return (text, toolTip);
    }

    // public bool RegisterKeyDown(Func<string, bool>? keyDownFunc)
    // {
    //     UnRegisterKeyDown();
    //
    //     _keyDownFunc = keyDownFunc;
    //
    //     if (_keyDownFunc == null)
    //         return false; // report that no event is subscribed
    //
    //     var parentWindow = Window.GetWindow(BaseWpfPanel);
    //
    //     if (parentWindow != null)
    //         parentWindow.PreviewKeyDown += BaseWpfPanelOnPreviewKeyDown;
    //
    //     return true; // report that events are subscribed
    // }
    //
    // private void BaseWpfPanelOnPreviewKeyDown(object sender, KeyEventArgs e)
    // {
    //     if (_keyDownFunc == null)
    //     {
    //         e.Handled = false;
    //         return;
    //     }
    //
    //     e.Handled = _keyDownFunc.Invoke(e.Key.ToString());
    // }
    //
    // private void UnRegisterKeyDown()
    // {
    //     if (_keyDownFunc != null)
    //     {
    //         var parentWindow = Window.GetWindow(BaseWpfPanel);
    //
    //         if (parentWindow != null)
    //             parentWindow.PreviewKeyDown -= BaseWpfPanelOnPreviewKeyDown;
    //
    //         _keyDownFunc = null;
    //     }
    // }
    //
    //
    // public bool RegisterPointerMoved(Action<Vector2> pointerMovedAction)
    // {
    //     if (MouseEventsSource == null || _pointerMovedAction != null)
    //         return false;
    //
    //     MouseEventsSource.MouseMove += PointerEventsSourceOnMouseMove;
    //     _pointerMovedAction = pointerMovedAction;
    //
    //     return true;
    // }
    //
    // private void PointerEventsSourceOnMouseMove(object sender, MouseEventArgs e)
    // {
    //     if (MouseEventsSource == null || _pointerMovedAction == null)
    //         return;
    //
    //     var currentPoint = e.GetPosition(MouseEventsSource);
    //     var pointerPosition = new Vector2((float)currentPoint.X, (float)currentPoint.Y);
    //
    //     _pointerMovedAction(pointerPosition);
    // }
    //
    //
    // public bool RegisterFileDropped(string? filePattern, Action<string>? fileDroppedAction)
    // {
    //     if (MouseEventsSource == null)
    //         return false;
    //
    //     _fileDroppedAction = fileDroppedAction;
    //
    //     if (_dragAndDropHelper != null)
    //     {
    //         _dragAndDropHelper.FileDropped -= DragAndDropHelperOnFileDropped;
    //         _dragAndDropHelper.Dispose(); // Unsubscribe
    //         _dragAndDropHelper = null;
    //     }
    //
    //     if (fileDroppedAction == null)
    //         return false;
    //
    //     _dragAndDropHelper = new DragAndDropHelper(MouseEventsSource, filePattern ?? ".*");
    //     _dragAndDropHelper.FileDropped += DragAndDropHelperOnFileDropped;
    //
    //     return true;
    // }
    //
    // private void DragAndDropHelperOnFileDropped(object? sender, FileDroppedEventArgs e)
    // {
    //     _fileDroppedAction?.Invoke(e.FileName);
    // }


    public string[] GetAllSettingKeys() => _settings.Keys.ToArray();

    public string? GetSettingText(string settingKey)
    {
        if (_settings.TryGetValue(settingKey, out var settingValueText))
            return settingValueText;

        return null;
    }

    public float GetSettingFloat(string settingKey)
    {
        if (_settings.TryGetValue(settingKey, out var settingValueText))
        {
            if (float.TryParse(settingValueText, NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var floatValue))
                return floatValue;
        }

        return float.NaN;
    }

    public void SetSettingText(string settingKey, string? newValue)
    {
        _settings[settingKey] = newValue;
        OnSettingsChanged();
    }

    public void SetSettingFloat(string settingKey, float newValue)
    {
        _settings[settingKey] = newValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
        OnSettingsChanged();
    }

    private void OnSettingsChanged()
    {
        var standardMarginValue = GetSettingFloat(StandardMarginSettingsKey);
        if (float.IsNaN(standardMarginValue))
            StandardMargin = 0;
        else
            StandardMargin = (double)standardMarginValue;

        var headerTopMarginValue = GetSettingFloat(HeaderTopMarginSettingsKey);
        if (float.IsNaN(headerTopMarginValue))
            HeaderTopMargin = GetDefaultHeaderTopMargin();
        else
            HeaderTopMargin = (double)headerTopMarginValue;

        var headerBottomMarginValue = GetSettingFloat(HeaderBottomMarinSettingsKey);
        if (float.IsNaN(headerBottomMarginValue))
            HeaderBottomMarin = GetDefaultHeaderBottomMarin();
        else
            HeaderBottomMarin = (double)headerBottomMarginValue;

        var fontSizeValue = GetSettingFloat(FontSizeSettingsKey);
        if (float.IsNaN(fontSizeValue))
            FontSize = 0;
        else
            FontSize = (double)fontSizeValue;
    }

    // Helper method for Blazor to get all root UI elements for rendering
    public IReadOnlyList<BlazorUIElement> GetRootUIElements() => _rootUIElements.AsReadOnly();

    // Public method to trigger state change notification (for UI elements to call when they need to re-render)
    public void NotifyStateChanged()
    {
        _stateChangedCallback?.Invoke();
    }


    // The following methods are not supported in the browser
    public ICommonSampleUIElement CreateMarkdownText(string markdownText, float fontSize = 0)
    {
        throw new NotImplementedException();
    }

    public bool RegisterKeyDown(Func<string, bool>? keyDownFunc)
    {
        return false; // notify caller that this is not supported
    }

    public bool RegisterPointerMoved(Action<Vector2> pointerMovedAction)
    {
        if (CanvasInterop == null)
            return false; // notify caller that this is not supported

        CanvasInterop.PointerMoved += OnPointerMoved;
        _subscribedCanvasInterop = CanvasInterop;
        _pointerMovedAction = pointerMovedAction;

        return true;
    }

    private void OnPointerMoved(object? sender, MouseMoveEventArgs e)
    {
        var pointerPosition = new Vector2(e.MouseX, e.MouseY);
        _pointerMovedAction?.Invoke(pointerPosition);
    }

    public bool RegisterFileDropped(string? filePattern, Action<string> fileDroppedAction)
    {
        return false; // notify caller that this is not supported
    }
}