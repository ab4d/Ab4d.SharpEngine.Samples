using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Windows;
using Ab4d.SharpEngine.AvaloniaUI;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common.Utils;
using Ab4d.SharpEngine.Utilities;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Colors = Ab4d.SharpEngine.Common.Colors;
using Image = Ab4d.Vulkan.Image;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.Common;

public class AvaloniaMarkdownTextCreator : MarkdownTextCreator<TextBlock>
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
        {
            _textBlock = new TextBlock();
            _textBlock.Inlines = new InlineCollection();
            _textBlock.TextWrapping = TextWrapping.Wrap;
        }
        else if (_textBlock == null)
        {
            throw new ArgumentException("createNew is false, but the UI element was not created yet");
        }
        else
        {
            if (_textBlock.Inlines != null)
                _textBlock.Inlines.Clear();
        }

        _textBlock.Foreground = _textBrush;
        _textBlock.FontSize = FontSize;
    }

    private void UpdateBrushes()
    {
        if (_textBrush == null || _textBrush.Color != TextColor.ToAvaloniaColor())
            _textBrush = new SolidColorBrush(TextColor.ToAvaloniaColor());

        _boldTextBrush = new SolidColorBrush(GetBoldTextColor().ToAvaloniaColor());

        if (_bulletBrush == null || _bulletBrush.Color != BulletColor.ToAvaloniaColor())
            _bulletBrush = new SolidColorBrush(BulletColor.ToAvaloniaColor());
        
        if (_linkColorBrush == null || _linkColorBrush.Color != LinkColor.ToAvaloniaColor())
            _linkColorBrush = new SolidColorBrush(LinkColor.ToAvaloniaColor());
        
        if (_codeBackgroundBrush == null || _codeBackgroundBrush.Color != CodeBackgroundColor.ToAvaloniaColor())
        {
            if (CodeBackgroundColor == Colors.Transparent)
                _codeBackgroundBrush = null;
            else
                _codeBackgroundBrush = new SolidColorBrush(CodeBackgroundColor.ToAvaloniaColor());
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
        AddInline(new LineBreak());
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
            FontWeight = FontWeight.Bold
        };

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (_textBlock.FontSize != FontSize)
            bulletRun.FontSize = FontSize;

        AddInline(bulletRun);
    }

    protected override void AddHeading(string? headingText, int headingLevel)
    {
        if (string.IsNullOrEmpty(headingText) || _textBlock == null)
            return;

        var run = new Run(headingText);
        run.FontWeight = FontWeight.Bold;
        run.FontSize = GetHeadingFontSize(headingLevel);
        run.Foreground = new SolidColorBrush(GetHeadingTextColor(headingLevel).ToAvaloniaColor());

        AddInline(run);
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

        // Hyperlink is not supported by Avalonia
        // Use TextBlock instead (Run does not support pointer events)

        var linkTextBlock = new TextBlock()
        {
            Text = text,
            Tag = url,
            Foreground = _linkColorBrush,
            TextDecorations = TextDecorations.Underline
        };
        
        if (isBold)
            linkTextBlock.FontWeight = FontWeight.Bold;

        if (url != null)
        {
            if (url != text)
                ToolTip.SetTip(linkTextBlock, url);

            linkTextBlock.PointerPressed += delegate (object? sender, PointerPressedEventArgs args)
            {
                OnLinkClicked(url);
                args.Handled = true;
            };
        }

        var inlineUiContainer = new InlineUIContainer(linkTextBlock);

        AddInline(inlineUiContainer);
    }
    
    protected override void AddImage(string source, string? altText)
    {
        if (_textBlock == null)
            return;

        AddCurrentTextToInline();

        source = FileUtils.FixDirectorySeparator(source);

        if (!System.IO.Path.IsPathRooted(source))
            source = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, source);

        var bitmapImage = new Bitmap(source);
        var image = new Avalonia.Controls.Image();
        image.Source = bitmapImage;
        image.Stretch = Stretch.None;

        if (altText != null)
            ToolTip.SetTip(image, altText);

        var inlineUiContainer = new InlineUIContainer(image);

        AddInline(inlineUiContainer);
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
            run.FontWeight = FontWeight.Bold;
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

        AddInline(run);

        _sb.Clear();
    }

    private void AddInline(Inline inline)
    {
        if (_textBlock == null)
            return;

        if (_textBlock.Inlines == null)
            _textBlock.Inlines = new InlineCollection();

        _textBlock.Inlines.Add(inline);
    }
}