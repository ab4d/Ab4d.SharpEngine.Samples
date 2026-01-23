using Ab4d.SharpEngine.AvaloniaUI;
using Ab4d.SharpEngine.Samples.AvaloniaUI.Common;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Common.Utils;
using Avalonia.Controls;
using Avalonia.Media;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.UIProvider;

public class MarkdownUIElement : AvaloniaUIElement
{
    private string _markdownText;
    private TextBlock _createdTextBlock;
    private AvaloniaMarkdownTextCreator _markdownTextCreator;

    public MarkdownUIElement(AvaloniaUIProvider avaloniaUIProvider, string markdownText, float fontSize = 0)
        : base(avaloniaUIProvider)
    {
        _markdownText = markdownText;

        _markdownTextCreator = new AvaloniaMarkdownTextCreator();

        if (fontSize > 0)
            _markdownTextCreator.FontSize = fontSize;

        _createdTextBlock = _markdownTextCreator.Create(markdownText) as TextBlock;

        AvaloniaControl = _createdTextBlock;
    }

    public override string? GetText() => _markdownText;

    public override ICommonSampleUIElement SetText(string? text)
    {
        if (text == null)
        {
            _createdTextBlock.Text = "";
        }
        else
        {
            _markdownTextCreator.Update(text);
        }

        return this;
    }


    protected override void OnSetColor(Color avaloniaColor)
    {
        _markdownTextCreator.TextColor = avaloniaColor.ToColor4();
        _markdownTextCreator.Update(_markdownText);
    }
}