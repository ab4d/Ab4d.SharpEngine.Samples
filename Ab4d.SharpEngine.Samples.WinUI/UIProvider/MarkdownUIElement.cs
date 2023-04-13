using Windows.UI;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Common.Utils;
using Ab4d.SharpEngine.Samples.WinUI.Common;
using Ab4d.SharpEngine.WinUI;
using Microsoft.UI.Xaml.Controls;
using static System.Net.Mime.MediaTypeNames;

namespace Ab4d.SharpEngine.Samples.WinUI.UIProvider;

public class MarkdownUIElement : WinUIElement
{
    private string _markdownText;
    private TextBlock _createdTextBlock;
    private WinUIMarkdownTextCreator _markdownTextCreator;

    public MarkdownUIElement(WinUIProvider winUIProvider, string markdownText, float fontSize = 0)
        : base(winUIProvider)
    {
        _markdownText = markdownText;

        _markdownTextCreator = new WinUIMarkdownTextCreator();

        if (fontSize > 0)
            _markdownTextCreator.FontSize = fontSize;

        _createdTextBlock = _markdownTextCreator.Create(markdownText) as TextBlock;

        Element = _createdTextBlock;
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


    protected override void OnSetColor(Color winUiColor)
    {
        _markdownTextCreator.TextColor = winUiColor.ToColor3();
        _markdownTextCreator.Update(_markdownText);
    }
}