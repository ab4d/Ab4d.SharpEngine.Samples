using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Xml.Linq;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.AvaloniaUI.UIProvider;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Wpf.Common;
using static System.Net.Mime.MediaTypeNames;

namespace Ab4d.SharpEngine.Samples.Wpf.UIProvider;

public class WpfUIProvider : ICommonSampleUIProvider
{
    public const string StandardMarginSettingsKey = "StandardMargin";
    public const string HeaderTopMarginSettingsKey = "HeaderTopMargin";
    public const string HeaderBottomMarinSettingsKey = "HeaderBottomMarin";
    public const string FontSizeSettingsKey = "FontSize";

    public const string ToolTipTextKey = " (?):";

    public string GetProviderType() => "WPF";

    private Dictionary<string, string?> _settings;

    public WpfUIPanel? CurrentPanel { get; private set; }

    public Panel BaseWpfPanel { get; }

    private double _lastSeparator;

    private List<WpfUIElement> _rootUIElements;

    private Func<string, bool>? _keyDownFunc;

    public double StandardMargin { get; private set; }
    public double HeaderTopMargin { get; private set; }
    public double HeaderBottomMarin { get; private set; }
    public double FontSize { get; private set; }

    private double GetDefaultHeaderTopMargin() => StandardMargin * 2;
    private double GetDefaultHeaderBottomMarin() => StandardMargin * 1.3;

    public WpfUIProvider(Panel basePanel)
    {
        BaseWpfPanel = basePanel;
        _rootUIElements = new List<WpfUIElement>();

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

    private void AddToCurrentPanel(WpfUIElement wpfUiElement)
    {
        if (_lastSeparator > 0 || StandardMargin > 0)
        {
            var frameworkElement = wpfUiElement.WpfElement;

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
            BaseWpfPanel.Children.Add(wpfUiElement.WpfElement);
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
            BaseWpfPanel.Children.Remove(rootUiElement.WpfElement);

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
            BaseWpfPanel.Children.Add(stackPanelUiElement.WpfElement);
            _rootUIElements.Add(stackPanelUiElement);
        }

        CurrentPanel = stackPanelUiElement;

        return stackPanelUiElement;
    }

    public void SetCurrentPanel(ICommonSampleUIPanel? panel)
    {
        CurrentPanel = panel as WpfUIPanel;
    }

    public ICommonSampleUIElement CreateLabel(string text, bool isHeader = false, float width = 0, float height = 0)
    {
        var newElement = new LabelUIElement(this, text, isHeader, width, height);
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
            if (uiElement is WpfUIPanel wpfUiPanel)
                UpdateAllValues(wpfUiPanel);
            else if (uiElement.IsUpdateSupported)
                uiElement.UpdateValue();
        }
    }
    
    private void UpdateAllValues(WpfUIPanel wpfUiPanel)
    {
        var childrenCount = wpfUiPanel.ChildrenCount;

        for (int i = 0; i < childrenCount; i++)
        {
            var child = wpfUiPanel.GetChild(i) as WpfUIElement;

            if (child is WpfUIPanel childWpfUiPanel)
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
        UnRegisterKeyDown();

        _keyDownFunc = keyDownFunc;

        if (_keyDownFunc == null)
            return false; // report that no event is subscribed

        var parentWindow = Window.GetWindow(BaseWpfPanel);

        if (parentWindow != null)
            parentWindow.PreviewKeyDown += BaseWpfPanelOnPreviewKeyDown;

        return true; // report that events are subscribed
    }

    private void BaseWpfPanelOnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_keyDownFunc == null)
        {
            e.Handled = false;
            return;
        }

        e.Handled = _keyDownFunc.Invoke(e.Key.ToString());
    }

    private void UnRegisterKeyDown()
    {
        if (_keyDownFunc != null)
        {
            var parentWindow = Window.GetWindow(BaseWpfPanel);

            if (parentWindow != null)
                parentWindow.PreviewKeyDown -= BaseWpfPanelOnPreviewKeyDown;

            _keyDownFunc = null;
        }
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