using Ab4d.SharpEngine.Samples.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Ab4d.SharpEngine.Samples.WinUI.UIProvider;

public class RadioButtonsUIElement : WinUIElement
{
    private static int _groupsCount;

    private string[] _items;

    private Action<int, string> _checkedItemChangedAction;

    private StackPanel _radioBoxesStackPanel;
    private StackPanel? _keyStackPanel;
    private TextBlock? _keyTextBlock;

    public RadioButtonsUIElement(WinUIProvider winUIProvider,
                                 string[] items, 
                                 Action<int, string> checkedItemChangedAction, 
                                 int selectedItemIndex,
                                 double width = 0,
                                 string? keyText = null,
                                 double keyTextWidth = 0)
        : base(winUIProvider)
    {
        _items = items;
        _checkedItemChangedAction = checkedItemChangedAction;

        _radioBoxesStackPanel = new StackPanel()
        {
            Orientation = Orientation.Vertical
        };


        _groupsCount++;
        string groupName = "RadioButtonsGroup_" + _groupsCount.ToString();

        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];

            var (text, toolTip) = winUIProvider.ParseTextAndToolTip(item);

            var radioButton = new RadioButton()
            {
                Content = text,
                FontSize = winUIProvider.FontSize,
                Margin = new Thickness(0, 1, 0, 1),
                GroupName = groupName
            };

            if (i == selectedItemIndex)
                radioButton.IsChecked = true;

            if (width > 0)
                radioButton.Width = width;

            if (toolTip != null)
                ToolTipService.SetToolTip(radioButton, toolTip);

            radioButton.Checked += RadioButtonOnChecked;

            _radioBoxesStackPanel.Children.Add(radioButton);
        }

        if (keyText != null)
        {
            _keyStackPanel = new StackPanel() { Orientation = Orientation.Horizontal };

            var (text, toolTip) = winUIProvider.ParseTextAndToolTip(keyText);

            _keyTextBlock = new TextBlock()
            {
                Text = text,
                Margin = new Thickness(0, 1, 5, 0),
                VerticalAlignment = VerticalAlignment.Top,
                FontSize = winUIProvider.FontSize,
            };

            if (keyTextWidth > 0)
                _keyTextBlock.Width = keyTextWidth;

            if (toolTip != null)
                ToolTipService.SetToolTip(_keyTextBlock, toolTip);

            _keyStackPanel.Children.Add(_keyTextBlock);
            _keyStackPanel.Children.Add(_radioBoxesStackPanel);

            Element = _keyStackPanel;
        }
        else
        {
            Element = _radioBoxesStackPanel;
        }
    }

    private void RadioButtonOnChecked(object? sender, RoutedEventArgs e)
    {
        var radioButton = sender as RadioButton;

        if (radioButton == null)
            return;

        var selectedIndex = _radioBoxesStackPanel.Children.IndexOf(radioButton);
        string? selectedText = radioButton.Content as string;

        if (selectedIndex == -1 || selectedText == null)
            return;

        _checkedItemChangedAction?.Invoke(selectedIndex, selectedText);
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
        var solidColorBrush = new SolidColorBrush(avaloniaColor);

        if (_keyTextBlock != null)
            _keyTextBlock.Foreground = solidColorBrush;

        foreach (RadioButton radioButton in _radioBoxesStackPanel.Children.OfType<RadioButton>())
            radioButton.Foreground = solidColorBrush;
    }
    
    public override void SetValue(object newValue)
    {
        if (newValue is int newIndex)
        {
            if (newIndex < 0 || newIndex > _items.Length - 1)
                throw new ArgumentOutOfRangeException(nameof(newIndex), $"Expected index between 0 and {_items.Length - 1}, but got {newIndex}");

            ((RadioButton)_radioBoxesStackPanel.Children[newIndex]).IsChecked = true;
        }
        else if (newValue is string newSelectedItem)
        {
            int index = Array.IndexOf(_items, newSelectedItem);
            
            if (index == -1)
                throw new ArgumentException($"Selected item '{newSelectedItem}' not found in the items list.");
            
            ((RadioButton)_radioBoxesStackPanel.Children[index]).IsChecked = true;
        }
        else
        {
            throw new ArgumentException($"SetValue for RadioButton expects int or string value, but got {newValue?.GetType().Name}");
        }
    }     
}