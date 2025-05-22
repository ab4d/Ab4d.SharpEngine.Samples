using Ab4d.SharpEngine.Samples.Common;
using System;
using System.Windows;

namespace Ab4d.SharpEngine.Samples.WinForms.UIProvider;

public class ComboBoxUIElement : WinFormsUIElement
{
    private string[] _items;

    private Action<int, string?> _itemChangedAction;

    private ComboBox _comboBox;

    private FlowLayoutPanel? _flowLayoutPanel;
    private Label? _keyLabel;

    public ComboBoxUIElement(WinFormsUIProvider winFormsUIProvider,
                             string[] items,
                             Action<int, string?> itemChangedAction,
                             int selectedItemIndex,
                             double width = 0,
                             string? keyText = null,
                             double keyTextWidth = 0)
        : base(winFormsUIProvider)
    {
        _items = items;
        _itemChangedAction = itemChangedAction;

        var (keyTextToShow, toolTip) = winFormsUIProvider.ParseTextAndToolTip(keyText);

        _comboBox = new ComboBox()
        {
            Font = winFormsUIProvider.Font,
            Height = 27
        };

        foreach (var item in items)
            _comboBox.Items.Add(item);

        _comboBox.SelectedIndex = selectedItemIndex;
        
        if (width > 0)
        {
            _comboBox.AutoSize = false;
            _comboBox.Width = (int)(width * winFormsUIProvider.UIScale);
        }
        else
        {
            int longestItemLength = 0;
            foreach (var item in items)
            {
                var textSize = TextRenderer.MeasureText(item, winFormsUIProvider.Font);
                if (textSize.Width > longestItemLength)
                    longestItemLength = textSize.Width;
            }

            _comboBox.Width = longestItemLength + 20;
        }

        if (toolTip != null)
            winFormsUIProvider.SetToolTip(_comboBox, toolTip);
        
        _comboBox.SelectedIndexChanged += ComboBoxOnSelectedIndexChanged;


        if (keyTextToShow != null)
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
                Margin = new Padding(0, 7, 0, 0),
                //VerticalAlignment = VerticalAlignment.Center,
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

            _flowLayoutPanel.Controls.Add(_keyLabel);

            _flowLayoutPanel.Controls.Add(_comboBox);

            WinFormsControl = _flowLayoutPanel;
        }
        else
        {
            WinFormsControl = _comboBox;
        }
    }

    private void ComboBoxOnSelectedIndexChanged(object? sender, EventArgs e)
    {
        var selectedIndex = _comboBox.SelectedIndex;
        string? selectedText = selectedIndex < 0 || selectedIndex > _items.Length - 1 ? null : _items[selectedIndex];

        _itemChangedAction?.Invoke(selectedIndex, selectedText);
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
    }
    
    public override void SetValue(object newValue)
    {
        if (newValue is int newIndex)
        {
            _comboBox.SelectedIndex = newIndex;
        }
        else if (newValue is string newSelectedItem)
        {
            _comboBox.SelectedItem = newSelectedItem;
        }
        else
        {
            throw new ArgumentException($"SetValue for ComboBox expects int or string value, but got {newValue?.GetType().Name}");
        }
    }      
}