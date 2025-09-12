using Ab4d.SharpEngine.Samples.Common;
using System;

namespace Ab4d.SharpEngine.Samples.WinForms.UIProvider;

public class TextBoxUIElement : WinFormsUIElement
{
    private readonly float _width;
    private readonly float _height;
    private TextBox _textBox;
    private Action<string>? _textChangedAction;

    public TextBoxUIElement(WinFormsUIProvider winFormsUIProvider, float width, float height, string? initialText, Action<string>? textChangedAction)
        : base(winFormsUIProvider)
    {
        _width = width;
        _height = height;
        
        _textBox = new TextBox()
        {
            Text = initialText,
            Font = winFormsUIProvider.Font,
        };

        UpdateTextSize(initialText);
        
        if (textChangedAction != null)
        {
            _textChangedAction = textChangedAction;
            _textBox.TextChanged += (sender, args) => _textChangedAction?.Invoke(_textBox.Text ?? "");;
        }

        WinFormsControl = _textBox;
    }
    
    private void UpdateTextSize(string? text)
    {
        bool isMultiline = _height > 0 || (text != null && text.Contains('\n'));
        _textBox.Multiline = isMultiline;

        if (isMultiline)
        {
            _textBox.AcceptsReturn = true;
            _textBox.AcceptsTab = true;
        }

        Size textSize;
        if (!string.IsNullOrEmpty(text))
            textSize = TextRenderer.MeasureText(text, winFormsUIProvider.Font);
        else
            textSize = Size.Empty;

        if (_width == 0 && _height == 0)
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
            
            if (_width > 0)
                _textBox.Width = (int)(_width * winFormsUIProvider.UIScale);
            else
                _textBox.Width = textSize.Width;

            if (_height > 0)
                _textBox.Height = (int)(_height * winFormsUIProvider.UIScale);
            else
                _textBox.Height = textSize.Height + 8;
        }
    }

    public override string? GetText() => _textBox.Text;

    public override ICommonSampleUIElement SetText(string? text)
    {
        _textBox.Text = text;
        UpdateTextSize(text);
        return this;
    }
    
    public override void SetValue(object newValue)
    {
        if (newValue is not string newText)
            throw new ArgumentException($"SetValue for TextBox expects string value, but got {newValue?.GetType().Name}");

        _textBox.Text = newText;
    }      
}