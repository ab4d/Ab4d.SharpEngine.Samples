using Ab4d.SharpEngine.Samples.Common;
using System;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.UIProvider;

public class ComboBoxUIElement : AvaloniaUIElement
{
    private string[] _items;

    private Action<int, string?> _itemChangedAction;

    private ComboBox _comboBox;

    private StackPanel? _stackPanel;
    private TextBlock? _keyTextBlock;

    public ComboBoxUIElement(AvaloniaUIProvider avaloniaUIProvider,
                             string[] items, 
                             Action<int, string?> itemChangedAction, 
                             int selectedItemIndex, 
                             double width = 0, 
                             string? keyText = null, 
                             double keyTextWidth = 0)
        : base(avaloniaUIProvider)
    {
        _items = items;
        _itemChangedAction = itemChangedAction;

        var (keyTextToShow, toolTip) = avaloniaUIProvider.ParseTextAndToolTip(keyText);

        _comboBox = new ComboBox()
        {
            ItemsSource = items,
            SelectedIndex = selectedItemIndex,
            FontSize = avaloniaUIProvider.FontSize,
        };

        if (width > 0)
            _comboBox.Width = width;

        if (toolTip != null)
            ToolTip.SetTip(_comboBox, toolTip);

        _comboBox.SelectionChanged += ComboBoxOnSelectionChanged;


        if (keyTextToShow != null)
        {
            _stackPanel = new StackPanel() { Orientation = Orientation.Horizontal };

            _keyTextBlock = new TextBlock()
            {
                Text = keyTextToShow,
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = avaloniaUIProvider.FontSize,
            };

            if (keyTextWidth > 0)
                _keyTextBlock.Width = keyTextWidth;

            if (toolTip != null)
                ToolTip.SetTip(_keyTextBlock, toolTip);

            _stackPanel.Children.Add(_keyTextBlock);

            _stackPanel.Children.Add(_comboBox);

            AvaloniaControl = _stackPanel;
        }
        else
        {
            AvaloniaControl = _comboBox;
        }
    }

    private void ComboBoxOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
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

    protected override void OnSetColor(Color avaloniaColor)
    {
        if (_keyTextBlock != null)
            _keyTextBlock.Foreground = new SolidColorBrush(avaloniaColor);
    }
}