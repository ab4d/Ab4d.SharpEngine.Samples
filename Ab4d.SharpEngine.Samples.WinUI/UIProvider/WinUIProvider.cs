using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using static System.Net.Mime.MediaTypeNames;

namespace Ab4d.SharpEngine.Samples.WinUI.UIProvider;

public class WinUIProvider : ICommonSampleUIProvider
{
    public const string StandardMarginSettingsKey = "StandardMargin";
    public const string HeaderTopMarginSettingsKey = "HeaderTopMargin";
    public const string HeaderBottomMarinSettingsKey = "HeaderBottomMarin";
    public const string FontSizeSettingsKey = "FontSize";

    public const string ToolTipTextKey = " (?):";

    public string GetProviderType() => "WinUI";

    private Dictionary<string, string?> _settings;

    public WinUIPanel? CurrentPanel { get; private set; }

    public Panel BaseWinUIPanel { get; }

    private double _lastSeparator;

    private List<WinUIElement> _rootUIElements;

    //private Func<string, bool>? _keyDownFunc;

    public double StandardMargin { get; private set; }
    public double HeaderTopMargin { get; private set; }
    public double HeaderBottomMarin { get; private set; }
    public double FontSize { get; private set; }

    private double GetDefaultHeaderTopMargin() => StandardMargin * 2;
    private double GetDefaultHeaderBottomMarin() => StandardMargin * 1.3;

    public WinUIProvider(Panel basePanel)
    {
        BaseWinUIPanel = basePanel;
        _rootUIElements = new List<WinUIElement>();

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

    private void AddToCurrentPanel(WinUIElement wpfUiElement)
    {
        if (_lastSeparator > 0 || StandardMargin > 0)
        {
            var frameworkElement = wpfUiElement.Element;

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
            BaseWinUIPanel.Children.Add(wpfUiElement.Element);
            _rootUIElements.Add(wpfUiElement);
        }
        else
        {
            CurrentPanel.AddChild(wpfUiElement);
        }
    }

    public void ClearAll()
    {
        foreach (var rootUiElement in _rootUIElements)
            BaseWinUIPanel.Children.Remove(rootUiElement.Element);

        _rootUIElements.Clear();

        CurrentPanel = null;
        _lastSeparator = 0;

        UnRegisterKeyDown();

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
            BaseWinUIPanel.Children.Add(stackPanelUiElement.Element);
            _rootUIElements.Add(stackPanelUiElement);
        }

        CurrentPanel = stackPanelUiElement;

        return stackPanelUiElement;
    }

    public void SetCurrentPanel(ICommonSampleUIPanel? panel)
    {
        CurrentPanel = panel as WinUIPanel;
    }

    public ICommonSampleUIElement CreateLabel(string text, bool isHeader = false)
    {
        var newElement = new LabelUIElement(this, text, isHeader);
        AddToCurrentPanel(newElement);

        return newElement;
    }

    public ICommonSampleUIElement CreateKeyValueLabel(string keyText, Func<string> getValueTextFunc, double keyTextWidth = 0)
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

    public ICommonSampleUIElement CreateSlider(float minValue, float maxValue, Func<float> getValueFunc, Action<float> setValueAction, double width = 0, bool showTicks = false, string? keyText = null, double keyTextWidth = 0, Func<float, string>? formatShownValueFunc = null)
    {
        var newElement = new SliderUIElement(this, minValue, maxValue, getValueFunc, setValueAction, width, showTicks, keyText, keyTextWidth, formatShownValueFunc);
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
        var newElement = new MarkdownUIElement(this, text, fontSize);
        AddToCurrentPanel(newElement);

        return newElement;
    }

    public void AddSeparator(double height = 8)
    {
        _lastSeparator = height;
    }

    public void UpdateAllValues()
    {
        foreach (var uiElement in _rootUIElements)
        {
            if (uiElement is WinUIPanel wpfUiPanel)
                UpdateAllValues(wpfUiPanel);
            else if (uiElement.IsUpdateSupported)
                uiElement.UpdateValue();
        }
    }
    
    private void UpdateAllValues(WinUIPanel wpfUiPanel)
    {
        var childrenCount = wpfUiPanel.ChildrenCount;

        for (int i = 0; i < childrenCount; i++)
        {
            var child = wpfUiPanel.GetChild(i) as WinUIElement;

            if (child is WinUIPanel childWpfUiPanel)
                UpdateAllValues(childWpfUiPanel);
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
                text = text.Substring(0, text.Length - 1) + " (?):";
            else
                text += " (?)";
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
        // It seems that it is not possible to reliable get key down events in WinUI (except by using WinAPI) !!!
        // See: https://github.com/microsoft/microsoft-ui-xaml/issues/7330

        return false; // report that this is not supported
    }

    private void UnRegisterKeyDown()
    {
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