using Ab4d.SharpEngine.Samples.Common;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ab4d.SharpEngine.Samples.Wpf.UIProvider;

public class ComboBoxUIElement : WpfUIElement
{
    private string[] _items;

    private Action<int, string?> _itemChangedAction;

    private ComboBox _comboBox;

    private StackPanel? _stackPanel;
    private TextBlock? _keyTextBlock;

    public ComboBoxUIElement(WpfUIProvider wpfUIProvider,
                             string[] items, 
                             Action<int, string?> itemChangedAction, 
                             int selectedItemIndex, 
                             double width = 0, 
                             string? keyText = null, 
                             double keyTextWidth = 0)
        : base(wpfUIProvider)
    {
        _items = items;
        _itemChangedAction = itemChangedAction;

        var (keyTextToShow, toolTip) = wpfUIProvider.ParseTextAndToolTip(keyText);

        _comboBox = new ComboBox()
        {
            ItemsSource = items,
            SelectedIndex = selectedItemIndex,
            FontSize = wpfUIProvider.FontSize,
        };

        if (width > 0)
            _comboBox.Width = width;

        if (toolTip != null)
            _comboBox.ToolTip = toolTip;

        _comboBox.SelectionChanged += ComboBoxOnSelectionChanged;


        if (keyTextToShow != null)
        {
            _stackPanel = new StackPanel() { Orientation = Orientation.Horizontal };

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

            _stackPanel.Children.Add(_comboBox);

            WpfElement = _stackPanel;
        }
        else
        {
            WpfElement = _comboBox;
        }
    }

    private void ComboBoxOnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedIndex = _comboBox.SelectedIndex;
        string? selectedText = selectedIndex < 0 || selectedIndex > _items.Length - 1 ? null : _items[selectedIndex];
        
        _itemChangedAction?.Invoke(selectedIndex, selectedText);
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

    protected override void OnSetColor(System.Windows.Media.Color wpfColor)
    {
        if (_keyTextBlock != null)
            _keyTextBlock.Foreground = new SolidColorBrush(wpfColor);
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