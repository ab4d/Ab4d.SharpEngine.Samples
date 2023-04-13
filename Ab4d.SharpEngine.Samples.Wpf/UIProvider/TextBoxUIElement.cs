using Ab4d.SharpEngine.Samples.Common;
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
            Text = initialText,
            FontSize = wpfUIProvider.FontSize,
        };

        if (width > 0)
            _textBox.Width = width;

        if (height > 0)
            _textBox.Height = height;

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
}