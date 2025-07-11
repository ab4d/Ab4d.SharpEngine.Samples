﻿using Ab4d.SharpEngine.Samples.Common;
using System.Windows.Controls;
using System;

namespace Ab4d.SharpEngine.Samples.Wpf.UIProvider;

public class TextBoxUIElement : WpfUIElement
{
    private TextBox _textBox;
    private Action<string>? _textChangedAction;

    public TextBoxUIElement(WpfUIProvider wpfUIProvider, float width, float height, string? initialText, Action<string>? textChangedAction)
        : base(wpfUIProvider)
    {
        _textBox = new TextBox()
        {
            Text = initialText ?? "",
            FontSize = wpfUIProvider.FontSize,
        };

        if (width > 0)
        {
            _textBox.Width = width;
            _textBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        }

        if (height > 0)
            _textBox.Height = height;

        if (height > 0 || (initialText != null && initialText.Contains('\n'))) // is multiline?
        { 
            _textBox.AcceptsReturn = true;
            _textBox.AcceptsTab = true;
            _textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        }

        if (textChangedAction != null)
        {
            _textChangedAction = textChangedAction;
            _textBox.TextChanged += TextBoxOnTextChanged;
        }

        WpfElement = _textBox;
    }

    private void TextBoxOnTextChanged(object? sender, TextChangedEventArgs e)
    {
        _textChangedAction?.Invoke(_textBox.Text ?? "");
    }


    public override string? GetText() => _textBox.Text;

    public override ICommonSampleUIElement SetText(string? text)
    {
        _textBox.Text = text;
        return this;
    }
    
    public override void SetValue(object newValue)
    {
        if (newValue is not string newText)
            throw new ArgumentException($"SetValue for TextBox expects string value, but got {newValue?.GetType().Name}");

        _textBox.Text = newText;
    }     
}