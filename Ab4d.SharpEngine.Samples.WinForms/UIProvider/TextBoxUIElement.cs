using Ab4d.SharpEngine.Samples.Common;
using System;

namespace Ab4d.SharpEngine.Samples.WinForms.UIProvider;

public class TextBoxUIElement : WinFormsUIElement
{
    private TextBox _textBox;
    private Action<string>? _textChangedAction;

    public TextBoxUIElement(WinFormsUIProvider winFormsUIProvider, float width, float height, string? initialText, Action<string>? textChangedAction)
        : base(winFormsUIProvider)
    {
        _textBox = new TextBox()
        {
            Text = initialText,
            Font = winFormsUIProvider.Font,
        };

        bool isMultiline = height > 0 || (initialText != null && initialText.Contains('\n'));
        _textBox.Multiline = isMultiline;

        if (isMultiline)
        {
            _textBox.AcceptsReturn = true;
            _textBox.AcceptsTab = true;
        }

        Size textSize;
        if (!string.IsNullOrEmpty(initialText))
            textSize = TextRenderer.MeasureText(initialText, winFormsUIProvider.Font);
        else
            textSize = Size.Empty;

        if (width == 0 && height == 0)
        {
            if (textSize.IsEmpty)
            {
                _textBox.AutoSize = true;
            }
            else
            {
                _textBox.AutoSize = false;
                _textBox.Width = textSize.Width;
                _textBox.Height = textSize.Height + 8;
            }
        }
        else
        {
            _textBox.AutoSize = false;
            
            if (width > 0)
                _textBox.Width = (int)(width * winFormsUIProvider.UIScale);
            else
                _textBox.Width = textSize.Width;

            if (height > 0)
                _textBox.Height = (int)(height * winFormsUIProvider.UIScale);
            else
                _textBox.Height = textSize.Height + 8;
        }


        if (textChangedAction != null)
        {
            _textChangedAction = textChangedAction;
            _textBox.TextChanged += (sender, args) => _textChangedAction?.Invoke(_textBox.Text ?? "");;
        }

        WinFormsControl = _textBox;
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