using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection.PortableExecutable;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.WinForms.Common;
using Ab4d.SharpEngine.WinForms;

namespace Ab4d.SharpEngine.Samples.WinForms.UIProvider;

public class WinFormsUIProvider : ICommonSampleUIProvider
{
    public const string StandardMarginSettingsKey = "StandardMargin";
    public const string HeaderTopMarginSettingsKey = "HeaderTopMargin";
    public const string HeaderBottomMarinSettingsKey = "HeaderBottomMarin";
    public const string FontSizeSettingsKey = "FontSize";

    public const string ToolTipTextKey = " (?):";

    public string GetProviderType() => "WinForms";

    public const int DefaultFontSize = 10;

    // Many common samples use fixed width and height values.
    // But when they are used in WinForms with font size 10, then the size is slightly too small and some text may be clipped.
    // Therefore we need to scale the UI by small amount. This is applied to UIScale that is used instead of DpiScale.
    // If we would use font size 9, then there is too much space when applying Dpi scale and too little space when no Dpi scale is applied.
    public const float UIScaleFactor = 1.05f; 

    private Dictionary<string, string?> _settings;

    public WinFormsUIPanel? CurrentPanel { get; private set; }

    public UserControl BaseWinFormsPanel { get; }

    private SharpEngineSceneView _mainSceneView;

    private Action<Vector2>? _pointerMovedAction;
    private Action<string>? _fileDroppedAction;
    private DragAndDropHelper? _dragAndDropHelper;
    private ToolTip _toolTipProvider;

    private int _lastSeparator;

    private List<WinFormsUIElement> _rootUIElements;

    public float DpiScale { get; private set; }
    
    public float UIScale { get; private set; }

    public double StandardMargin { get; private set; }
    public double HeaderTopMargin { get; private set; }
    public double HeaderBottomMarin { get; private set; }

    private float _fontSize;
    public Font? Font { get; private set; }
    public Font? BoldFont { get; private set; }
    public Font? ItalicFont { get; private set; }

    private double GetDefaultHeaderTopMargin() => StandardMargin * 2;
    private double GetDefaultHeaderBottomMarin() => StandardMargin * 1.3;

    public WinFormsUIProvider(UserControl basePanel, SharpEngineSceneView mainSceneView)
    {
        BaseWinFormsPanel = basePanel;
        _mainSceneView = mainSceneView;
        
        _rootUIElements = new List<WinFormsUIElement>();

        _settings = new Dictionary<string, string?>();
        ResetSettings();

        DpiScale = (float)basePanel.DeviceDpi / 96.0f;
        UIScale = DpiScale * UIScaleFactor; // See comment for UIScaleFactor for more info

        _toolTipProvider = new ToolTip()
        {
            ShowAlways = true,
        };

        basePanel.SizeChanged += BasePanelOnSizeChanged;
    }

    public void SetToolTip(Control control, string? toolTip)
    {
        _toolTipProvider.SetToolTip(control, toolTip);
    }
    
    public void ShowToolTip(Control control, string toolTip)
    {
        _toolTipProvider.Show(toolTip, control);
    }

    private void BasePanelOnSizeChanged(object? sender, EventArgs e)
    {
        foreach (var rootUiElement in _rootUIElements)
        {
            if (rootUiElement is StackPanelUIElement stackPanelUiElement)
                stackPanelUiElement.UpdateLocation();
        }
    }

    private void ResetSettings()
    {
        _settings.Clear();

        _fontSize = DefaultFontSize;// BaseWinFormsPanel.Font.Size;
        UpdateFonts();

        SetSettingFloat(StandardMarginSettingsKey, 2);
        SetSettingFloat(HeaderTopMarginSettingsKey, (float)GetDefaultHeaderTopMargin());
        SetSettingFloat(HeaderBottomMarinSettingsKey, (float)GetDefaultHeaderBottomMarin());
        SetSettingFloat(FontSizeSettingsKey, _fontSize);
    }

    private void UpdateFonts()
    {
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (_fontSize == BaseWinFormsPanel.Font.Size)
        {
            Font = BaseWinFormsPanel.Font;
            BoldFont = new Font(BaseWinFormsPanel.Font, FontStyle.Bold);
            ItalicFont = new Font(BaseWinFormsPanel.Font, FontStyle.Italic);
        }
        else
        {
            var fontSize = _fontSize == 0 ? DefaultFontSize : _fontSize;

            Font = new Font(BaseWinFormsPanel.Font.FontFamily, fontSize, FontStyle.Regular);
            BoldFont = new Font(BaseWinFormsPanel.Font.FontFamily, fontSize, FontStyle.Bold);
            ItalicFont = new Font(BaseWinFormsPanel.Font.FontFamily, fontSize, FontStyle.Italic);
        }
    }

    private void AddToCurrentPanel(WinFormsUIElement winFormsUiElement)
    {
        if (_lastSeparator > 0 || StandardMargin > 0)
        {
            var control = winFormsUiElement.WinFormsControl;

            int margin = control.Margin.Top;

            if (margin == 0)
                margin = (int)StandardMargin;

            margin += _lastSeparator;

            bool isCurrentPanelVertical = CurrentPanel != null ? CurrentPanel.IsVertical : true;

            if (isCurrentPanelVertical)
                control.Margin = new Padding(control.Margin.Left, margin, control.Margin.Right, control.Margin.Bottom);
            else
                control.Margin = new Padding(margin, control.Margin.Top, control.Margin.Right, control.Margin.Bottom);

            _lastSeparator = 0;
        }

        if (CurrentPanel == null)
        {
            BaseWinFormsPanel.Controls.Add(winFormsUiElement.WinFormsControl);

            // Make sure that SharpEngineSceneView is always last so other controls are on top of it
            BaseWinFormsPanel.Controls.SetChildIndex(_mainSceneView, BaseWinFormsPanel.Controls.Count - 1); 

            _rootUIElements.Add(winFormsUiElement);
        }
        else
        {
            CurrentPanel.AddChild(winFormsUiElement);
        }
    }

    public void ClearAll()
    {
        foreach (var rootUiElement in _rootUIElements)
            BaseWinFormsPanel.Controls.Remove(rootUiElement.WinFormsControl);

        _rootUIElements.Clear();

        CurrentPanel = null;
        _lastSeparator = 0;

        UnRegisterKeyDown();

        if (_pointerMovedAction != null)
        {
            _mainSceneView.MouseMove -= MainSceneViewOnMouseMove;
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
        object parentObject = parent != null ? parent : BaseWinFormsPanel;
        var stackPanelUiElement = new StackPanelUIElement(this, parentObject, alignment, isVertical, addBorder, isSemiTransparent);

        if (parent != null)
        {
            parent.AddChild(stackPanelUiElement);
        }
        else
        {
            BaseWinFormsPanel.Controls.Add(stackPanelUiElement.WinFormsControl);

            // Make sure that SharpEngineSceneView is always last so other controls are on top of it
            BaseWinFormsPanel.Controls.SetChildIndex(_mainSceneView, BaseWinFormsPanel.Controls.Count - 1);

            _rootUIElements.Add(stackPanelUiElement);
        }

        CurrentPanel = stackPanelUiElement;

        return stackPanelUiElement;
    }

    public void SetCurrentPanel(ICommonSampleUIPanel? panel)
    {
        CurrentPanel = panel as WinFormsUIPanel;
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
        _lastSeparator += (int)height;
    }

    public void UpdateAllValues()
    {
        foreach (var uiElement in _rootUIElements)
        {
            if (uiElement is WinFormsUIPanel WinFormsUiPanel)
                UpdateAllValues(WinFormsUiPanel);
            else if (uiElement.IsUpdateSupported)
                uiElement.UpdateValue();
        }
    }
    
    private void UpdateAllValues(WinFormsUIPanel winFormsUiPanel)
    {
        var childrenCount = winFormsUiPanel.ChildrenCount;

        for (int i = 0; i < childrenCount; i++)
        {
            var child = winFormsUiPanel.GetChild(i) as WinFormsUIElement;

            if (child is WinFormsUIPanel childWinFormsUiPanel)
                UpdateAllValues(childWinFormsUiPanel);
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

        //var parentWindow = Window.GetWindow(_baseWinFormsPanel);

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
        //    var parentWindow = Window.GetWindow(_baseWinFormsPanel);

        //    if (parentWindow != null)
        //        parentWindow.PreviewKeyDown -= BaseWpfPanelOnPreviewKeyDown;

        //    _keyDownFunc = null;
        //}
    }

    public bool RegisterPointerMoved(Action<Vector2> pointerMovedAction)
    {
        _mainSceneView.MouseMove += MainSceneViewOnMouseMove;
        _pointerMovedAction = pointerMovedAction;

        return true;
    }

    private void MainSceneViewOnMouseMove(object? sender, MouseEventArgs e)
    {
        if (_pointerMovedAction == null)
            return;

        var pointerPosition = new Vector2((float)e.Location.X / DpiScale, (float)e.Location.Y / DpiScale);

        _pointerMovedAction(pointerPosition);
    }

    public bool RegisterFileDropped(string? filePattern, Action<string>? fileDroppedAction)
    {
        _fileDroppedAction = fileDroppedAction;

        if (_dragAndDropHelper != null)
        {
            _dragAndDropHelper.FileDropped -= DragAndDropHelperOnFileDropped;
            _dragAndDropHelper.Dispose(); // Unsubscribe
            _dragAndDropHelper = null;
        }

        if (fileDroppedAction == null)
            return false;

        _dragAndDropHelper = new DragAndDropHelper(_mainSceneView, filePattern ?? ".*");
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
            _fontSize = 0;
        else
            _fontSize = fontSizeValue;

        UpdateFonts();
    }
}