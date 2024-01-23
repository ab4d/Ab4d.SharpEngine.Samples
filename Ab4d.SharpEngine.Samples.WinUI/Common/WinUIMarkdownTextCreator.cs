using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Windows;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common.Utils;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.WinUI;
using Ab4d.Vulkan;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Colors = Ab4d.SharpEngine.Common.Colors;
using Image = Microsoft.UI.Xaml.Controls.Image;

namespace Ab4d.SharpEngine.Samples.WinUI.Common;

public class WinUIMarkdownTextCreator : MarkdownTextCreator<TextBlock>
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
        if (_textBrush == null || _textBrush.Color != TextColor.ToWinUIColor())
            _textBrush = new SolidColorBrush(TextColor.ToWinUIColor());

        _boldTextBrush = new SolidColorBrush(GetBoldTextColor().ToWinUIColor());

        if (_bulletBrush == null || _bulletBrush.Color != BulletColor.ToWinUIColor())
            _bulletBrush = new SolidColorBrush(BulletColor.ToWinUIColor());
        
        if (_linkColorBrush == null || _linkColorBrush.Color != LinkColor.ToWinUIColor())
            _linkColorBrush = new SolidColorBrush(LinkColor.ToWinUIColor());
        
        if (_codeBackgroundBrush == null || _codeBackgroundBrush.Color != CodeBackgroundColor.ToWinUIColor())
        {
            if (CodeBackgroundColor == Colors.Transparent)
                _codeBackgroundBrush = null;
            else
                _codeBackgroundBrush = new SolidColorBrush(CodeBackgroundColor.ToWinUIColor());
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

        var bulletRun = new Run() 
        {
            Text = BulletText, // (" ▪ ")
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

        var run = new Run()
        {
            Text = headingText,
            FontWeight = FontWeights.Bold,
            FontSize = GetHeadingFontSize(headingLevel),
            Foreground = new SolidColorBrush(GetHeadingTextColor(headingLevel).ToWinUIColor())
        };

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

        var hyperlink = new Hyperlink()
        {
            Foreground = _linkColorBrush
        };

        hyperlink.Inlines.Add(new Run() { Text = text });

        if (!string.IsNullOrEmpty(url))
        {
            hyperlink.NavigateUri = new Uri(url);

            hyperlink.Click += (sender, args) =>
            {
                OnLinkClicked(url);
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

        // UH: When adding InlineUIContainer with Image, an exception is thrown: Item does not fall withing a valid range
        
        //source = FileUtils.FixDirectorySeparator(source);

        //if (!System.IO.Path.IsPathRooted(source))
        //    source = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, source);

        //var bitmapImage = new BitmapImage(new Uri(source, UriKind.Absolute));
        
        //var image = new Image();
        //image.Source = bitmapImage;
        //image.Stretch = Stretch.None;

        //var inlineUiContainer = new InlineUIContainer();
        //inlineUiContainer.Child = image;

        //_textBlock.Inlines.Add(inlineUiContainer);
    }

    [MemberNotNull(nameof(_textBlock))]
    private void AddCurrentTextToInline()
    {
        if (_textBlock == null)
            throw new InvalidOperationException();

        if (_sb.Length == 0)
            return;

        var run = new Run()
        {
            Text = _sb.ToString()
        };

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
            run.FontFamily = CodeFontFamily;

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (_textBlock.FontSize != FontSize)
            run.FontSize = FontSize;

        _textBlock.Inlines.Add(run);

        _sb.Clear();
    }
}