using Ab4d.SharpEngine.Samples.Common;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ab4d.SharpEngine.Samples.Wpf.UIProvider;

public class SliderUIElement : WpfUIElement
{
    private Func<float> _getValueFunc;
    private Action<float> _setValueAction;
    private Func<float, string>? _formatShownValueFunc;

    private StackPanel? _stackPanel;
    private TextBlock? _keyTextBlock;
    private TextBlock? _valueTextBlock;

    private Slider _slider;

    public override bool IsUpdateSupported => true;
    
    public SliderUIElement(WpfUIProvider wpfUIProvider,
                           float minValue,
                           float maxValue, 
                           Func<float> getValueFunc, 
                           Action<float> setValueAction, 
                           double width, 
                           bool showTicks, 
                           string? keyText, 
                           double keyTextWidth,
                           Func<float, string>? formatShownValueFunc,
                           double shownValueWidth)
        : base(wpfUIProvider)
    {
        _getValueFunc = getValueFunc;
        _setValueAction = setValueAction;
        _formatShownValueFunc = formatShownValueFunc;

        var (keyTextToShow, toolTip) = wpfUIProvider.ParseTextAndToolTip(keyText);

        _slider = new Slider()
        {
            Minimum = minValue,
            Maximum = maxValue,
        };

        if (width > 0)
            _slider.Width = width;

        if (toolTip != null)
            _slider.ToolTip = toolTip;

        UpdateValue();

        _slider.ValueChanged += SliderOnValueChanged;


        if (keyTextToShow != null || formatShownValueFunc != null)
        {
            _stackPanel = new StackPanel() { Orientation = Orientation.Horizontal };

            if (keyTextToShow != null)
            {
                _keyTextBlock = new TextBlock()
                {
                    Text = keyTextToShow,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = wpfUIProvider.FontSize,
                };

                if (keyTextWidth > 0)
                    _keyTextBlock.Width = keyTextWidth;

                if (toolTip != null)
                    _keyTextBlock.ToolTip = toolTip;

                _stackPanel.Children.Add(_keyTextBlock);
            }

            _stackPanel.Children.Add(_slider);

            if (formatShownValueFunc != null)
            {
                _valueTextBlock = new TextBlock()
                {
                    Text = formatShownValueFunc((float)_slider.Value),
                    Margin = new Thickness(5, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    FontSize = wpfUIProvider.FontSize,
                };

                if (toolTip != null)
                    _valueTextBlock.ToolTip = toolTip;

                // To prevent changing the size of _valueTextBlock we get the max possible size
                if (shownValueWidth > 0)
                    _valueTextBlock.Width = shownValueWidth;
                else if (shownValueWidth == 0)
                    _valueTextBlock.Width = MeasureShownValueTextBlock(minValue, maxValue);
                // else: when less then zero, then do not set the width

                _stackPanel.Children.Add(_valueTextBlock);
            }

            WpfElement = _stackPanel;
        }
        else
        {
            WpfElement = _slider;
        }
    }

    private void SliderOnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _setValueAction?.Invoke((float)e.NewValue);
        UpdateShownValue();
    }

    public sealed override void UpdateValue()
    {
        float newValue = _getValueFunc();

        _slider.Value = newValue;

        UpdateShownValue();
    }


    public override string? GetText()
    {
        if (_keyTextBlock != null)
            return _keyTextBlock.Text;

        return null;
    }

    public override ICommonSampleUIElement SetText(string? text)
    {
        if (_keyTextBlock != null)
            _keyTextBlock.Text = text;

        return this;
    }


    private void UpdateShownValue()
    {
        if (_valueTextBlock != null && _formatShownValueFunc != null)
            _valueTextBlock.Text = _formatShownValueFunc((float)_slider.Value);
    }

    private double MeasureShownValueTextBlock(float minValue, float maxValue)
    {
        if (_valueTextBlock == null || _formatShownValueFunc == null)
            return 0;

        string savedText = _valueTextBlock.Text;

        string minValueText = _formatShownValueFunc(minValue);
        _valueTextBlock.Text = minValueText;
        _valueTextBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        double width = _valueTextBlock.DesiredSize.Width;


        string maxValueText = _formatShownValueFunc(maxValue);
        _valueTextBlock.Text = maxValueText;
        _valueTextBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        if (_valueTextBlock.DesiredSize.Width > width)
            width = _valueTextBlock.DesiredSize.Width;

        _valueTextBlock.Text = savedText;

        return width;
    }

    protected override void OnSetColor(System.Windows.Media.Color wpfColor)
    {
        if (_keyTextBlock != null)
            _keyTextBlock.Foreground = new SolidColorBrush(wpfColor);
        
        if (_valueTextBlock != null)
            _valueTextBlock.Foreground = new SolidColorBrush(wpfColor);
    }
    
    public override void SetValue(object newValue)
    {
        if (newValue is int newIntValue)
        {
            _slider.Value = newIntValue;
        }
        else if (newValue is float newFloatValue)
        {
            _slider.Value = newFloatValue;
        }
        else if (newValue is double newDoubleValue)
        {
            _slider.Value = newDoubleValue;
        }
        else
        {
            throw new ArgumentException($"SetValue for Slider expects int, float or double value, but got {newValue?.GetType().Name}");
        }
    }   
    
    
    public override void SetProperty(string propertyName, string propertyValue)
    {
        if (propertyName.Equals("Maximum", StringComparison.OrdinalIgnoreCase))
            _slider.Maximum = double.Parse(propertyValue, NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
        else if (propertyName.Equals("Minimum", StringComparison.OrdinalIgnoreCase))
            _slider.Minimum = double.Parse(propertyValue, NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
    }
    
    public override string? GetPropertyValue(string propertyName)
    {
        if (propertyName.Equals("Maximum", StringComparison.OrdinalIgnoreCase))
            return _slider.Maximum.ToString(System.Globalization.CultureInfo.InvariantCulture);
        
        if (propertyName.Equals("Minimum", StringComparison.OrdinalIgnoreCase))
            return _slider.Minimum.ToString(System.Globalization.CultureInfo.InvariantCulture);

        return null;
    }
}