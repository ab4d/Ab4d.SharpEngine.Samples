using Ab4d.SharpEngine.Samples.Common;
using System;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.UIProvider;

public class SliderUIElement : AvaloniaUIElement
{
    private Func<float> _getValueFunc;
    private Action<float> _setValueAction;
    private Func<float, string>? _formatShownValueFunc;

    private StackPanel? _stackPanel;
    private TextBlock? _keyTextBlock;
    private TextBlock? _valueTextBlock;

    private Slider _slider;

    public override bool IsUpdateSupported => true;

    public SliderUIElement(AvaloniaUIProvider avaloniaUIProvider,
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
        : base(avaloniaUIProvider)
    {
        _getValueFunc = getValueFunc;
        _setValueAction = setValueAction;
        _formatShownValueFunc = formatShownValueFunc;

        var (keyTextToShow, toolTip) = avaloniaUIProvider.ParseTextAndToolTip(keyText);

        _slider = new Slider()
        {
            Minimum = minValue,
            Maximum = maxValue,
        };

        if (width > 0)
            _slider.Width = width;

        if (toolTip != null)
            ToolTip.SetTip(_slider, toolTip);

        UpdateValue();

        _slider.PropertyChanged += (sender, args) =>
        {
            if (args.Property == RangeBase.ValueProperty && args.NewValue != null)
            {
                _setValueAction?.Invoke((float)(double)args.NewValue);
                UpdateShownValue();
            }
        };

        if (keyTextToShow != null || formatShownValueFunc != null)
        {
            _stackPanel = new StackPanel() { Orientation = Orientation.Horizontal };

            if (keyText != null)
            {
                _keyTextBlock = new TextBlock()
                {
                    Text = keyTextToShow,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = avaloniaUIProvider.FontSize,
                };

                if (toolTip != null)
                    ToolTip.SetTip(_keyTextBlock, toolTip);

                if (keyTextWidth > 0)
                    _keyTextBlock.Width = keyTextWidth;

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
                    FontSize = avaloniaUIProvider.FontSize,
                };

                // To prevent changing the size of _valueTextBlock we get the max possible size
                if (shownValueWidth > 0)
                    _valueTextBlock.Width = shownValueWidth;
                else if (shownValueWidth == 0)
                    _valueTextBlock.Width = MeasureShownValueTextBlock(minValue, maxValue);
                // else: when less then zero, then do not set the width

                _stackPanel.Children.Add(_valueTextBlock);
            }

            AvaloniaControl = _stackPanel;
        }
        else
        {
            AvaloniaControl = _slider;
        }
    }

    //private void SliderOnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    //{
    //    _setValueAction?.Invoke((float)e.NewValue);
    //    UpdateShownValue();
    //}

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

        string? savedText = _valueTextBlock.Text;

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

    protected override void OnSetColor(Color avaloniaColor)
    {
        if (_keyTextBlock != null)
            _keyTextBlock.Foreground = new SolidColorBrush(avaloniaColor);
        
        if (_valueTextBlock != null)
            _valueTextBlock.Foreground = new SolidColorBrush(avaloniaColor);
    }
}