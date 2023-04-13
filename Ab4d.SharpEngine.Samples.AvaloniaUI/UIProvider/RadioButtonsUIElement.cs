using Ab4d.SharpEngine.Samples.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.UIProvider;

public class RadioButtonsUIElement : AvaloniaUIElement
{
    private static int _groupsCount;

    private string[] _items;

    private Action<int, string> _checkedItemChangedAction;

    private StackPanel _radioBoxesStackPanel;
    private StackPanel? _keyStackPanel;
    private TextBlock? _keyTextBlock;

    public RadioButtonsUIElement(AvaloniaUIProvider avaloniaUIProvider,
                                 string[] items, 
                                 Action<int, string> checkedItemChangedAction, 
                                 int selectedItemIndex,
                                 double width = 0,
                                 string? keyText = null,
                                 double keyTextWidth = 0)
        : base(avaloniaUIProvider)
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

            var (text, toolTip) = avaloniaUIProvider.ParseTextAndToolTip(item);

            var radioButton = new RadioButton()
            {
                Content = text,
                FontSize = avaloniaUIProvider.FontSize,
                Margin = new Thickness(0, 1, 0, 1),
                GroupName = groupName
            };

            if (i == selectedItemIndex)
                radioButton.IsChecked = true;

            if (width > 0)
                radioButton.Width = width;

            if (toolTip != null)
                ToolTip.SetTip(radioButton, toolTip);

            radioButton.IsCheckedChanged += RadioButtonOnIsCheckedChanged;

            _radioBoxesStackPanel.Children.Add(radioButton);
        }

        if (keyText != null)
        {
            _keyStackPanel = new StackPanel() { Orientation = Orientation.Horizontal };

            var (text, toolTip) = avaloniaUIProvider.ParseTextAndToolTip(keyText);

            _keyTextBlock = new TextBlock()
            {
                Text = text,
                Margin = new Thickness(0, 1, 5, 0),
                VerticalAlignment = VerticalAlignment.Top,
                FontSize = avaloniaUIProvider.FontSize,
            };

            if (keyTextWidth > 0)
                _keyTextBlock.Width = keyTextWidth;

            if (toolTip != null)
                ToolTip.SetTip(_keyTextBlock, toolTip);

            _keyStackPanel.Children.Add(_keyTextBlock);
            _keyStackPanel.Children.Add(_radioBoxesStackPanel);

            AvaloniaControl = _keyStackPanel;
        }
        else
        {
            AvaloniaControl = _radioBoxesStackPanel;
        }
    }

    private void RadioButtonOnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton radioButton)
            return;

        var isChecked = radioButton.IsChecked ?? false;
        if (!isChecked)
            return;

        var selectedIndex = _radioBoxesStackPanel.Children.IndexOf(radioButton);

        if (selectedIndex == -1 || radioButton.Content is not string selectedText)
            return;

        _checkedItemChangedAction.Invoke(selectedIndex, selectedText);
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
}