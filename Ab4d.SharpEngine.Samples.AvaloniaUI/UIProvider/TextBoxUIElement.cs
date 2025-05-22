using Ab4d.SharpEngine.Samples.Common;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia;
using System;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.UIProvider;

public class TextBoxUIElement : AvaloniaUIElement
{
    private TextBox _textBox;
    private Action<string>? _textChangedAction;

    public TextBoxUIElement(AvaloniaUIProvider avaloniaUIProvider, float width, float height, string? initialText, Action<string>? textChangedAction)
        : base(avaloniaUIProvider)
    {
        _textBox = new TextBox()
        {
            Text = initialText,
            FontSize = avaloniaUIProvider.FontSize,
        };

        if (width > 0)
            _textBox.Width = width;

        if (height > 0)
            _textBox.Height = height;

        if (height > 0 || (initialText != null && initialText.Contains('\n'))) // is multiline?
        { 
            _textBox.AcceptsReturn = true;
            _textBox.AcceptsTab = true;
        }

        if (textChangedAction != null)
        {
            _textChangedAction = textChangedAction;
            _textBox.TextChanged += TextBoxOnTextChanged;
        }

        AvaloniaControl = _textBox;
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