using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.AvaloniaUI.Common;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.Vulkan;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.UIProvider;

public class AvaloniaUIProvider : ICommonSampleUIProvider
{
    public const string StandardMarginSettingsKey = "StandardMargin";
    public const string HeaderTopMarginSettingsKey = "HeaderTopMargin";
    public const string HeaderBottomMarinSettingsKey = "HeaderBottomMarin";
    public const string FontSizeSettingsKey = "FontSize";

    public const string ToolTipTextKey = " (?):";

    public string GetProviderType() => "Avalonia";

    private Dictionary<string, string?> _settings;

    public AvaloniaUIPanel? CurrentPanel { get; private set; }

    public Panel BaseAvaloniaPanel { get; }

    private Control? PointerEventsSource { get; }
    
    private Action<Vector2>? _pointerMovedAction;
    private Action<string>? _fileDroppedAction;
    private DragAndDropHelper? _dragAndDropHelper;

    private double _lastSeparator;
    
    private List<AvaloniaUIElement> _rootUIElements;

    public double StandardMargin { get; private set; }
    public double HeaderTopMargin { get; private set; }
    public double HeaderBottomMarin { get; private set; }
    public double FontSize { get; private set; }

    private double GetDefaultHeaderTopMargin() => StandardMargin * 2;
    private double GetDefaultHeaderBottomMarin() => StandardMargin * 1.3;

    public AvaloniaUIProvider(Panel basePanel, Control? pointerEventsSource)
    {
        BaseAvaloniaPanel = basePanel;
        PointerEventsSource = pointerEventsSource;

        _rootUIElements = new List<AvaloniaUIElement>();

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
    }

    private void AddToCurrentPanel(AvaloniaUIElement avaloniaUiElement)
    {
        if (_lastSeparator > 0 || StandardMargin > 0)
        {
            var frameworkElement = avaloniaUiElement.AvaloniaControl;

            double margin = frameworkElement.Margin.Top;

            if (margin == 0)
                margin = StandardMargin;

            margin += _lastSeparator;

            bool isCurrentPanelVertical = CurrentPanel != null ? CurrentPanel.IsVertical : true;

            if (isCurrentPanelVertical)
                frameworkElement.Margin = new Thickness(frameworkElement.Margin.Left, margin, frameworkElement.Margin.Right, frameworkElement.Margin.Bottom);
            else
                frameworkElement.Margin = new Thickness(margin, frameworkElement.Margin.Top, frameworkElement.Margin.Right, frameworkElement.Margin.Bottom);

            _lastSeparator = 0;
        }

        if (CurrentPanel == null)
        {
            BaseAvaloniaPanel.Children.Add(avaloniaUiElement.AvaloniaControl);
            _rootUIElements.Add(avaloniaUiElement);
        }
        else
        {
            CurrentPanel.AddChild(avaloniaUiElement);
        }
    }

    public void ClearAll()
    {
        foreach (var rootUiElement in _rootUIElements)
            BaseAvaloniaPanel.Children.Remove(rootUiElement.AvaloniaControl);

        _rootUIElements.Clear();

        CurrentPanel = null;
        _lastSeparator = 0;

        UnRegisterKeyDown();
        
        if (_pointerMovedAction != null && PointerEventsSource != null)
        {
            PointerEventsSource.PointerMoved -= PointerEventsSourceOnPointerMoved;
            _pointerMovedAction = null;
        }

        if (_dragAndDropHelper != null)
        {
            _dragAndDropHelper.FileDropped -= DragAndDropHelperOnFileDropped;
            _dragAndDropHelper.Dispose(); // Unsubscribe
            _dragAndDropHelper = null;
            _fileDroppedAction = null;
        }

        ResetSettings();
    }

    public ICommonSampleUIPanel CreateStackPanel(PositionTypes alignment, bool isVertical = true, bool addBorder = true, bool isSemiTransparent = true, ICommonSampleUIPanel? parent = null)
    {
        var stackPanelUiElement = new StackPanelUIElement(this, alignment, isVertical, addBorder, isSemiTransparent);

        if (parent != null)
        {
            parent.AddChild(stackPanelUiElement);
        }
        else
        {
            BaseAvaloniaPanel.Children.Add(stackPanelUiElement.AvaloniaControl);
            _rootUIElements.Add(stackPanelUiElement);
        }

        CurrentPanel = stackPanelUiElement;

        return stackPanelUiElement;
    }

    public void SetCurrentPanel(ICommonSampleUIPanel? panel)
    {
        CurrentPanel = panel as AvaloniaUIPanel;
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

    public ICommonSampleUIElement CreateMarkdownText(string text, float fontSize = 0)
    {
        throw new NotSupportedException();
        //var newElement = new MarkdownUIElement(this, text, fontSize);
        //AddToCurrentPanel(newElement);

        //return newElement;
    }

    public void AddSeparator(double height = 8)
    {
        _lastSeparator += height;
    }

    public void UpdateAllValues()
    {
        foreach (var uiElement in _rootUIElements)
        {
            if (uiElement is AvaloniaUIPanel avaloniaUiPanel)
                UpdateAllValues(avaloniaUiPanel);
            else if (uiElement.IsUpdateSupported)
                uiElement.UpdateValue();
        }
    }
    
    private void UpdateAllValues(AvaloniaUIPanel avaloniaUiPanel)
    {
        var childrenCount = avaloniaUiPanel.ChildrenCount;

        for (int i = 0; i < childrenCount; i++)
        {
            var child = avaloniaUiPanel.GetChild(i) as AvaloniaUIElement;

            if (child is AvaloniaUIPanel childAvaloniaUiPanel)
                UpdateAllValues(childAvaloniaUiPanel);
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

    public bool RegisterKeyDown(Func<string, bool>? keyDownFunc)
    {
        return false; // not supported for now

        //UnRegisterKeyDown();

        //_keyDownFunc = keyDownFunc;

        //if (_keyDownFunc == null)
        //    return;

        //var parentWindow = Window.GetWindow(_baseAvaloniaPanel);

        //if (parentWindow != null)
        //    parentWindow.PreviewKeyDown += BaseWpfPanelOnPreviewKeyDown;
    }

    private void BaseWpfPanelOnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        //if (_keyDownFunc == null)
        //{
        //    e.Handled = false;
        //    return;
        //}

        //e.Handled = _keyDownFunc.Invoke(e.Key.ToString());
    }

    private void UnRegisterKeyDown()
    {
        //if (_keyDownFunc != null)
        //{
        //    var parentWindow = Window.GetWindow(_baseAvaloniaPanel);

        //    if (parentWindow != null)
        //        parentWindow.PreviewKeyDown -= BaseWpfPanelOnPreviewKeyDown;

        //    _keyDownFunc = null;
        //}
    }

    public bool RegisterPointerMoved(Action<Vector2> pointerMovedAction)
    {
        if (PointerEventsSource == null || _pointerMovedAction != null)
            return false;

        PointerEventsSource.PointerMoved += PointerEventsSourceOnPointerMoved;
        _pointerMovedAction = pointerMovedAction;

        return true;
    }

    private void PointerEventsSourceOnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (PointerEventsSource == null || _pointerMovedAction == null)
            return;

        var currentPoint = e.GetPosition(PointerEventsSource);
        var pointerPosition = new Vector2((float)currentPoint.X, (float)currentPoint.Y);

        _pointerMovedAction(pointerPosition);
    }


    public bool RegisterFileDropped(string? filePattern, Action<string>? fileDroppedAction)
    {
        if (PointerEventsSource == null)
            return false;

        _fileDroppedAction = fileDroppedAction;

        if (_dragAndDropHelper != null)
        {
            _dragAndDropHelper.FileDropped -= DragAndDropHelperOnFileDropped;
            _dragAndDropHelper = null;
        }

        if (fileDroppedAction == null)
            return false;

        _dragAndDropHelper = new DragAndDropHelper(PointerEventsSource, filePattern ?? ".*");
        _dragAndDropHelper.FileDropped += DragAndDropHelperOnFileDropped;

        return true;
    }

    private void DragAndDropHelperOnFileDropped(object? sender, FileDroppedEventArgs e)
    {
        _fileDroppedAction?.Invoke(e.FileName);
    }



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
}