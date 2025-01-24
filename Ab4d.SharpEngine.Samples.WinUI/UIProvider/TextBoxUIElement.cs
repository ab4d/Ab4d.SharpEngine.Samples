using Ab4d.SharpEngine.Samples.Common;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Ab4d.SharpEngine.Samples.WinUI.UIProvider;

public class TextBoxUIElement : WinUIElement
{
    private TextBox _textBox;
    private Action<string>? _textChangedAction;

    public TextBoxUIElement(WinUIProvider winUIProvider, float width, float height, string? initialText, Action<string>? textChangedAction)
        : base(winUIProvider)
    {
        _textBox = new TextBox()
        {
            Text = initialText,
            FontSize = winUIProvider.FontSize,
            AcceptsReturn = true,
        };

        if (width > 0)
            _textBox.Width = width;

        if (height > 0)
            _textBox.Height = height;

        if (height > 0 || (initialText != null && initialText.Contains('\n'))) // is multiline?
            _textBox.AcceptsReturn = true;


        if (textChangedAction != null)
        {
            _textChangedAction = textChangedAction;
            _textBox.TextChanged += TextBoxOnTextChanged;
        }

        Element = _textBox;
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
}