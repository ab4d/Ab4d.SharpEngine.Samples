using Ab4d.SharpEngine.Samples.Common;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Ab4d.SharpEngine.Samples.WinUI.UIProvider;

public class TextBoxUIElement : WinUIElement
{
    private TextBox _textBox;
    private Action<string>? _textChangedAction;

    private bool _fixNewLine = true;

    public TextBoxUIElement(WinUIProvider winUIProvider, float width, float height, string? initialText, Action<string>? textChangedAction)
        : base(winUIProvider)
    {
        _textBox = new TextBox()
        {
            Text = initialText,
            FontSize = winUIProvider.FontSize,
        };

        if (width > 0)
            _textBox.Width = width;

        if (height > 0)
            _textBox.Height = height;

        if (height > 0 || (initialText != null && initialText.Contains('\n'))) // is multiline?
        {
            _textBox.AcceptsReturn = true;
            _textBox.Text = initialText;

            // UH UH:
            // TextBox in WinUI just removes the '\n' from the text and uses '\r' instead.
            // Maybe this is fixed in the future - check here is '\n' is preserved and in this case do not replace '\r' by '\n' in the TextBoxOnTextChanged.
            // See also:
            // https://stackoverflow.com/questions/42867242/uwp-textbox-puts-r-only-how-to-set-linebreak
            // https://stackoverflow.com/questions/35138047/uwp-textbox-selectedtext-changes-r-n-to-r
            if (initialText != null && initialText.Contains('\n') && _textBox.Text != null && _textBox.Text.Contains('\n'))
                _fixNewLine = false;
        }


        if (textChangedAction != null)
        {
            _textChangedAction = textChangedAction;
            _textBox.TextChanged += TextBoxOnTextChanged;
        }

        Element = _textBox;
    }

    private void TextBoxOnTextChanged(object? sender, TextChangedEventArgs e)
    {
        var text = _textBox.Text ?? "";

        if (_fixNewLine)
            text = text.Replace('\r', '\n'); // see comment in constructor

        _textChangedAction?.Invoke(text);
    }


    public override string? GetText() => _textBox.Text;

    public override ICommonSampleUIElement SetText(string? text)
    {
        _textBox.Text = text;
        return this;
    }
}