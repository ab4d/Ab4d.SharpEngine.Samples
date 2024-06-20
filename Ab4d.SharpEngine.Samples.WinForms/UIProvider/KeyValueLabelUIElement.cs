using Ab4d.SharpEngine.Samples.Common;
using System;
using System.Windows;

namespace Ab4d.SharpEngine.Samples.WinForms.UIProvider;

public class KeyValueLabelUIElement : WinFormsUIElement
{
    private string? _keyText;
    private Func<string> _getValueTextFunc;

    private FlowLayoutPanel? _flowLayoutPanel;
    private Label? _keyLabel;
    private Label? _valueLabel;

    private Label? _keyValueLabel;

    public override bool IsUpdateSupported => true;

    public KeyValueLabelUIElement(WinFormsUIProvider winFormsUIProvider, string? keyText, Func<string> getValueTextFunc, double keyTextWidth)
        : base(winFormsUIProvider)
    {
        _keyText = keyText;
        _getValueTextFunc = getValueTextFunc;

        var (keyTextToShow, toolTip) = winFormsUIProvider.ParseTextAndToolTip(keyText);


        if (keyTextWidth > 0)
        {
            _flowLayoutPanel = new FlowLayoutPanel()
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true
            };

            _keyLabel = new Label()
            {
                Text = keyTextToShow,
                Margin = new Padding(0, 0, 2, 0),
                Font = winFormsUIProvider.Font,
            };

            if (keyTextWidth > 0)
            {
                _keyLabel.AutoSize = false;
                _keyLabel.Width = (int)(keyTextWidth * winFormsUIProvider.UIScale);
            }
            else
            {
                _keyLabel.AutoSize = true;
            }

            _valueLabel = new Label()
            {
                Font = winFormsUIProvider.Font,
                AutoSize = true
            };

            if (toolTip != null)
            {
                winFormsUIProvider.SetToolTip(_keyLabel, toolTip);
                winFormsUIProvider.SetToolTip(_valueLabel, toolTip);
            }

            _flowLayoutPanel.Controls.Add(_keyLabel);
            _flowLayoutPanel.Controls.Add(_valueLabel);

            WinFormsControl = _flowLayoutPanel;
        }
        else
        {
            // When there is no keyTextWidth, then we can use a single TextBlock to show key and value
            _keyValueLabel = new Label()
            {
                Font = winFormsUIProvider.Font,
                AutoSize = true
            };

            _keyText = keyTextToShow; // Update _keyText so that show text without toot tip

            //if (toolTip != null)
            //    ToolTip.SetTip(_keyValueLabel, toolTip);

            WinFormsControl = _keyValueLabel;
        }

        UpdateValue();
    }

    public sealed override void UpdateValue()
    {
        string newValueText = _getValueTextFunc();

        if (_valueLabel != null)
        {
            _valueLabel.Text = newValueText;
        }
        else if (_keyValueLabel != null)
        {
            if (string.IsNullOrEmpty(_keyText))
                _keyValueLabel.Text = newValueText;
            else
                _keyValueLabel.Text = _keyText + " " + newValueText;
        }
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

    protected override void OnSetColor(Color winFormsColor)
    {
        if (_keyLabel != null)
            _keyLabel.ForeColor = winFormsColor;

        if (_valueLabel != null)
            _valueLabel.ForeColor = winFormsColor;
    }
}