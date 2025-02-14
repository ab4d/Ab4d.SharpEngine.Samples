using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common.Utils;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Wpf;
using Ab4d.Vulkan;
using Colors = Ab4d.SharpEngine.Common.Colors;
using Image = System.Windows.Controls.Image;

namespace Ab4d.SharpEngine.Samples.Wpf.Common;

public class WpfMarkdownTextCreator : MarkdownTextCreator<TextBlock>
{
    private TextBlock? _textBlock;

    private SolidColorBrush? _textBrush;
    private SolidColorBrush? _boldTextBrush;
    private SolidColorBrush? _bulletBrush;
    private SolidColorBrush? _linkColorBrush;
    private SolidColorBrush? _codeBackgroundBrush;

    private StringBuilder _sb = new StringBuilder();

    public FontFamily CodeFontFamily = new FontFamily("Consolas");

    protected override void BeginMarkdown(string markdownText, bool createNew)
    {
        UpdateBrushes();

        if (createNew)
            _textBlock = new TextBlock();
        else if (_textBlock == null)
            throw new ArgumentException("createNew is false, but the UI element was not created yet");
        else
            _textBlock.Inlines.Clear();

        _textBlock.Foreground = _textBrush;
        _textBlock.FontSize = FontSize;
        _textBlock.TextWrapping = TextWrapping.Wrap;
    }

    private void UpdateBrushes()
    {
        if (_textBrush == null || _textBrush.Color != TextColor.ToWpfColor())
            _textBrush = new SolidColorBrush(TextColor.ToWpfColor());

        _boldTextBrush = new SolidColorBrush(GetBoldTextColor().ToWpfColor());

        if (_bulletBrush == null || _bulletBrush.Color != BulletColor.ToWpfColor())
            _bulletBrush = new SolidColorBrush(BulletColor.ToWpfColor());
        
        if (_linkColorBrush == null || _linkColorBrush.Color != LinkColor.ToWpfColor())
            _linkColorBrush = new SolidColorBrush(LinkColor.ToWpfColor());
        
        if (_codeBackgroundBrush == null || _codeBackgroundBrush.Color != CodeBackgroundColor.ToWpfColor())
        {
            if (CodeBackgroundColor == Colors.Transparent)
                _codeBackgroundBrush = null;
            else
                _codeBackgroundBrush = new SolidColorBrush(CodeBackgroundColor.ToWpfColor());
        }
    }

    protected override TextBlock CompleteMarkdown()
    {
        AddCurrentTextToInline();

        return _textBlock;
    }

    protected override void AddLineBreak()
    {
        AddCurrentTextToInline();
        _textBlock.Inlines.Add(new LineBreak());
    }

    protected override void AddChar(char ch)
    {
        _sb.Append(ch);
    }

    protected override void AddBullet()
    {
        if (_textBlock == null)
            throw new InvalidOperationException();

        var bulletRun = new Run(BulletText) // (" ▪ ")
        {
            Foreground = _bulletBrush,
            FontWeight = FontWeights.Bold
        };

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (_textBlock.FontSize != FontSize)
            bulletRun.FontSize = FontSize;

        _textBlock.Inlines.Add(bulletRun);
    }

    protected override void AddHeading(string? headingText, int headingLevel)
    {
        if (string.IsNullOrEmpty(headingText) || _textBlock == null)
            return;

        var run = new Run(headingText);
        run.FontWeight = FontWeights.Bold;
        run.FontSize = GetHeadingFontSize(headingLevel);
        run.Foreground = new SolidColorBrush(GetHeadingTextColor(headingLevel).ToWpfColor());

        _textBlock.Inlines.Add(run);
    }

    protected override void WriteCurrentText()
    {
        UpdateBrushes();
        AddCurrentTextToInline();
    }

    protected override void AddLink(string? url, string? text)
    {
        if ((url == null && text == null) || _textBlock == null)
            return;

        AddCurrentTextToInline();

        if (text == null)
            text = url;

        var hyperlink = new Hyperlink(new Run(text));
        hyperlink.Foreground = _linkColorBrush;

        if (!string.IsNullOrEmpty(url))
        {
            hyperlink.NavigateUri = new Uri(url);

            if (url != text)
                hyperlink.ToolTip = url;

            hyperlink.RequestNavigate += (sender, args) =>
            {
                OnLinkClicked(url);
                args.Handled = true; // do not handle the click by WPF
            };
        }

        if (isBold)
            hyperlink.FontWeight = FontWeights.Bold;

        _textBlock.Inlines.Add(hyperlink);
    }
    
    protected override void AddImage(string source, string? altText)
    {
        if (_textBlock == null)
            return;

        AddCurrentTextToInline();

        string fileSource = FileUtils.FixDirectorySeparator(source);

        if (!System.IO.Path.IsPathRooted(fileSource))
            fileSource = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileSource);

        if (System.IO.File.Exists(fileSource))
            source = fileSource;
        else
            source = "pack://application:,,,/" + source;

        var bitmapImage = new BitmapImage(new Uri(source, UriKind.Absolute));
        var image = new Image();
        image.Source = bitmapImage;
        image.Stretch = Stretch.None;

        if (altText != null)
            image.ToolTip = altText;

        var inlineUiContainer = new InlineUIContainer(image);

        _textBlock.Inlines.Add(inlineUiContainer);
    }

    [MemberNotNull(nameof(_textBlock))]
    private void AddCurrentTextToInline()
    {
        if (_textBlock == null)
            throw new InvalidOperationException();

        if (_sb.Length == 0)
            return;

        var run = new Run(_sb.ToString());

        if (isBold)
        {
            run.FontWeight = FontWeights.Bold;
            run.Foreground = _boldTextBrush;
        }
        else if(_textBlock.Foreground != _textBrush)
        {
            run.Foreground = _textBrush;
        }

        if (isCode)
        {
            run.FontFamily = CodeFontFamily;
            run.Background = _codeBackgroundBrush;
        }

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (_textBlock.FontSize != FontSize)
            run.FontSize = FontSize;

        _textBlock.Inlines.Add(run);

        _sb.Clear();
    }
}