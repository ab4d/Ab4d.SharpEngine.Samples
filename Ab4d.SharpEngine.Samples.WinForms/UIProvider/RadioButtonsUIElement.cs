using Ab4d.SharpEngine.Samples.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Ab4d.SharpEngine.Samples.WinForms.UIProvider;

public class RadioButtonsUIElement : WinFormsUIElement
{
    private static int _groupsCount;

    private string[] _items;

    private Action<int, string> _checkedItemChangedAction;

    private FlowLayoutPanel _radioBoxesFlowLayoutPanel;
    private FlowLayoutPanel? _keyFlowLayoutPanel;
    private Label? _keyLabel;

    public RadioButtonsUIElement(WinFormsUIProvider winFormsUIProvider,
                                 string[] items,
                                 Action<int, string> checkedItemChangedAction,
                                 int selectedItemIndex,
                                 double width = 0,
                                 string? keyText = null,
                                 double keyTextWidth = 0)
        : base(winFormsUIProvider)
    {
        _items = items;
        _checkedItemChangedAction = checkedItemChangedAction;

        _radioBoxesFlowLayoutPanel = new FlowLayoutPanel()
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true
        };


        _groupsCount++;

        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];

            var (text, toolTip) = winFormsUIProvider.ParseTextAndToolTip(item);

            var radioButton = new RadioButton()
            {
                Text = text,
                Font = winFormsUIProvider.Font,
                Margin = new Padding(0, 1, 0, 1),
            };

            if (i == selectedItemIndex)
                radioButton.Checked = true;
            
            if (width > 0)
            {
                radioButton.AutoSize = false;
                radioButton.Width = (int)(width * winFormsUIProvider.UIScale);
            }
            else
            {
                radioButton.AutoSize = true;
            }

            if (toolTip != null)
                winFormsUIProvider.SetToolTip(radioButton, toolTip);

            radioButton.CheckedChanged += RadioButtonOnCheckedChanged;

            _radioBoxesFlowLayoutPanel.Controls.Add(radioButton);
        }

        if (keyText != null)
        {
            _keyFlowLayoutPanel = new FlowLayoutPanel()
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true
            };

            var (text, toolTip) = winFormsUIProvider.ParseTextAndToolTip(keyText);

            _keyLabel = new Label()
            {
                Text = text,
                Margin = new Padding(0, 1, 3, 0),
                //VerticalAlignment = VerticalAlignment.Top,
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

            if (toolTip != null)
                winFormsUIProvider.SetToolTip(_keyLabel, toolTip);

            _keyFlowLayoutPanel.Controls.Add(_keyLabel);
            _keyFlowLayoutPanel.Controls.Add(_radioBoxesFlowLayoutPanel);

            WinFormsControl = _keyFlowLayoutPanel;
        }
        else
        {
            WinFormsControl = _radioBoxesFlowLayoutPanel;
        }
    }

    private void RadioButtonOnCheckedChanged(object? sender, EventArgs e)
    {
        if (sender is not RadioButton radioButton)
            return;

        var isChecked = radioButton.Checked;
        if (!isChecked)
            return;

        var selectedIndex = _radioBoxesFlowLayoutPanel.Controls.IndexOf(radioButton);

        if (selectedIndex == -1)
            return;

        _checkedItemChangedAction.Invoke(selectedIndex, radioButton.Text);
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

        foreach (RadioButton radioButton in _radioBoxesFlowLayoutPanel.Controls.OfType<RadioButton>())
            radioButton.ForeColor = winFormsColor;
    }
}