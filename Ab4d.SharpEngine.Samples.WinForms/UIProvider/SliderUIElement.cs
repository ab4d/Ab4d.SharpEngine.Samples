using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;
using System;
using System.Globalization;
using System.Windows;

namespace Ab4d.SharpEngine.Samples.WinForms.UIProvider;

public class SliderUIElement : WinFormsUIElement
{
    private Func<float> _getValueFunc;
    private Action<float> _setValueAction;
    private Func<float, string>? _formatShownValueFunc;

    private FlowLayoutPanel? _flowLayoutPanel;
    private Label? _keyLabel;
    private Label? _valueLabel;

    private TrackBar _trackBar;

    private float _valueScale;

    public override bool IsUpdateSupported => true;

    public SliderUIElement(WinFormsUIProvider winFormsUIProvider,
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
        : base(winFormsUIProvider)
    {
        _getValueFunc = getValueFunc;
        _setValueAction = setValueAction;
        _formatShownValueFunc = formatShownValueFunc;

        var (keyTextToShow, toolTip) = winFormsUIProvider.ParseTextAndToolTip(keyText);

        if (maxValue - minValue < 5)
        {
            // WinForms slider supports only integer values so for small values, we need to multiply that by 100
            // Then in the _trackBar.ValueChanged we will divide the slider value by _valueScale
            _valueScale = 100;
            minValue *= 100;
            maxValue *= 100;
        }
        else
        {
            _valueScale = 1; // No scale
        }

        _trackBar = new TrackBar()
        {
            Minimum = (int)minValue,
            Maximum = (int)maxValue,
            TickStyle = TickStyle.None,
            BackColor = Color.White,
            AutoSize = false,
            Height = 30,
            Width = (int)(width * winFormsUIProvider.UIScale),
            Margin = new Padding(0, 0, 0, 0),
        };

        if (width == 0)
            _trackBar.AutoSize = true;

        if (toolTip != null)
            winFormsUIProvider.SetToolTip(_trackBar, toolTip);

        UpdateValue();

        _trackBar.ValueChanged += (sender, args) =>
        {
            _setValueAction?.Invoke((float)(_trackBar.Value / _valueScale));
            UpdateShownValue();
        };


        if (keyTextToShow != null || formatShownValueFunc != null)
        {
            _flowLayoutPanel = new FlowLayoutPanel()
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true
            };

            if (keyText != null)
            {
                _keyLabel = new Label()
                {
                    Text = keyTextToShow,
                    Margin = new Padding(0, 3, 2, 0),
                    Font = winFormsUIProvider.Font,
                };

                if (toolTip != null)
                    winFormsUIProvider.SetToolTip(_keyLabel, toolTip);

                if (keyTextWidth > 0)
                {
                    _keyLabel.AutoSize = false;
                    _keyLabel.Width = (int)(keyTextWidth * winFormsUIProvider.UIScale);
                }
                else
                {
                    _keyLabel.AutoSize = true;
                }

                _flowLayoutPanel.Controls.Add(_keyLabel);
            }

            _flowLayoutPanel.Controls.Add(_trackBar);

            if (formatShownValueFunc != null)
            {
                _valueLabel = new Label()
                {
                    Text = formatShownValueFunc((float)_trackBar.Value / _valueScale),
                    Margin = new Padding(2, 3, 0, 0),
                    //VerticalAlignment = VerticalAlignment.Center,
                    //HorizontalAlignment = HorizontalAlignment.Left,
                    Font = winFormsUIProvider.Font,
                };

                // To prevent changing the size of _valueTextBlock we get the max possible size
                if (shownValueWidth > 0)
                    _valueLabel.Width = (int)shownValueWidth;
                else if (shownValueWidth == 0)
                    _valueLabel.Width = MeasureShownValueTextBlock(minValue / _valueScale, maxValue / _valueScale);
                // else: when less then zero, then do not set the width

                _flowLayoutPanel.Controls.Add(_valueLabel);
            }

            WinFormsControl = _flowLayoutPanel;
        }
        else
        {
            WinFormsControl = _trackBar;
        }
    }

    public sealed override void UpdateValue()
    {
        float newValue = _getValueFunc() * _valueScale;

        _trackBar.Value = Math.Min(_trackBar.Maximum, Math.Max(_trackBar.Minimum, (int)newValue)); // It is not allowed to set Value outsize of Min - Max

        UpdateShownValue();
    }


    public override string? GetText()
    {
        if (_keyLabel != null)
            return _keyLabel.Text;

        return null;
    }

    public override ICommonSampleUIElement SetText(string? text)
    {
        if (_keyLabel != null)
            _keyLabel.Text = text;

        return this;
    }


    private void UpdateShownValue()
    {
        if (_valueLabel != null && _formatShownValueFunc != null)
            _valueLabel.Text = _formatShownValueFunc((float)_trackBar.Value / _valueScale);
    }

    private int MeasureShownValueTextBlock(float minValue, float maxValue)
    {
        if (_valueLabel == null || _formatShownValueFunc == null)
            return 0;

        string? savedText = _valueLabel.Text;

        string minValueText = _formatShownValueFunc(minValue);
        _valueLabel.Text = minValueText;
        
        int width = _valueLabel.PreferredSize.Width;


        string maxValueText = _formatShownValueFunc(maxValue);
        _valueLabel.Text = maxValueText;

        if (_valueLabel.PreferredSize.Width > width)
            width = _valueLabel.PreferredSize.Width;

        _valueLabel.Text = savedText;

        return width;
    }

    protected override void OnSetColor(Color winFormsColor)
    {
        if (_keyLabel != null)
            _keyLabel.ForeColor = winFormsColor;

        if (_valueLabel != null)
            _valueLabel.ForeColor = winFormsColor;
    }
    
    public override void SetValue(object newValue)
    {
        if (newValue is int newIntValue)
        {
            _trackBar.Value = newIntValue;
        }
        else if (newValue is float newFloatValue)
        {
            _trackBar.Value = (int)newFloatValue;
        }
        else if (newValue is double newDoubleValue)
        {
            _trackBar.Value = (int)newDoubleValue;
        }
        else
        {
            throw new ArgumentException($"SetValue for Slider expects int, float or double value, but got {newValue?.GetType().Name}");
        }
    }       
    
    public override void SetProperty(string propertyName, string propertyValue)
    {
        if (propertyName.Equals("Maximum", StringComparison.OrdinalIgnoreCase))
            _trackBar.Maximum = Int32.Parse(propertyValue);
        else if (propertyName.Equals("Minimum", StringComparison.OrdinalIgnoreCase))
            _trackBar.Minimum = Int32.Parse(propertyValue);
    }
    
    public override string? GetPropertyValue(string propertyName)
    {
        if (propertyName.Equals("Maximum", StringComparison.OrdinalIgnoreCase))
            return _trackBar.Maximum.ToString();
        
        if (propertyName.Equals("Minimum", StringComparison.OrdinalIgnoreCase))
            return _trackBar.Minimum.ToString();

        return null;
    }    
}