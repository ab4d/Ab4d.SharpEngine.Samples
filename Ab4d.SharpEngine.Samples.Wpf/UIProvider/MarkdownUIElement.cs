using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Common.Utils;
using Ab4d.SharpEngine.Samples.Wpf.Common;
using System.Windows.Controls;
using System.Windows.Media;
using Ab4d.SharpEngine.Wpf;

namespace Ab4d.SharpEngine.Samples.Wpf.UIProvider;

public class MarkdownUIElement : WpfUIElement
{
    private string _markdownText;
    private TextBlock _createdTextBlock;
    private WpfMarkdownTextCreator _markdownTextCreator;

    public MarkdownUIElement(WpfUIProvider wpfUIProvider, string markdownText, float fontSize = 0)
        : base(wpfUIProvider)
    {
        _markdownText = markdownText;

        _markdownTextCreator = new WpfMarkdownTextCreator();

        if (fontSize > 0)
            _markdownTextCreator.FontSize = fontSize;

        _createdTextBlock = _markdownTextCreator.Create(markdownText) as TextBlock;

        WpfElement = _createdTextBlock;
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


    protected override void OnSetColor(System.Windows.Media.Color wpfColor)
    {
        _markdownTextCreator.TextColor = wpfColor.ToColor4();
        _markdownTextCreator.Update(_markdownText);
    }
}