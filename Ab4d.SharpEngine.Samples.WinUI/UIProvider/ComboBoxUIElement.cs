using Ab4d.SharpEngine.Samples.Common;
using System;
using System.Windows;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Ab4d.SharpEngine.Samples.WinUI.UIProvider;

public class ComboBoxUIElement : WinUIElement
{
    private string[] _items;

    private Action<int, string?> _itemChangedAction;

    private ComboBox _comboBox;

    private StackPanel? _stackPanel;
    private TextBlock? _keyTextBlock;

    public ComboBoxUIElement(WinUIProvider winUIProvider,
                             string[] items, 
                             Action<int, string?> itemChangedAction, 
                             int selectedItemIndex, 
                             double width = 0, 
                             string? keyText = null, 
                             double keyTextWidth = 0)
        : base(winUIProvider)
    {
        _items = items;
        _itemChangedAction = itemChangedAction;

        var (keyTextToShow, toolTip) = winUIProvider.ParseTextAndToolTip(keyText);

        _comboBox = new ComboBox()
        {
            ItemsSource = items,
            SelectedIndex = selectedItemIndex,
            FontSize = winUIProvider.FontSize,
        };

        if (width > 0)
            _comboBox.Width = width;

        if (toolTip != null)
            ToolTipService.SetToolTip(_comboBox, toolTip);

        _comboBox.SelectionChanged += ComboBoxOnSelectionChanged;


        if (keyTextToShow != null)
        {
            _stackPanel = new StackPanel() { Orientation = Orientation.Horizontal };

            _keyTextBlock = new TextBlock()
            {
                Text = keyTextToShow,
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = winUIProvider.FontSize,
            };

            if (keyTextWidth > 0)
                _keyTextBlock.Width = keyTextWidth;

            if (toolTip != null)
                ToolTipService.SetToolTip(_keyTextBlock, toolTip);

            _stackPanel.Children.Add(_keyTextBlock);

            _stackPanel.Children.Add(_comboBox);

            Element = _stackPanel;
        }
        else
        {
            Element = _comboBox;
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

    protected override void OnSetColor(Color wpfColor)
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