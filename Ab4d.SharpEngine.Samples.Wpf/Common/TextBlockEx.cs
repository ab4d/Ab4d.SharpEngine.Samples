#nullable disable

// ----------------------------------------------------------------
// <copyright file="TextBlockEx.cs" company="AB4D d.o.o.">
//     Copyright (c) AB4D d.o.o.  All Rights Reserved
// </copyright>
// -----------------------------------------------------------------

// License note:
// You may use this control free of charge and for any project you wish. Just do not blame me for any problems with the control.

using Ab4d;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Navigation;

namespace Ab4d.SharpEngine.Samples.Wpf.Common
{
    /// <summary>
    /// TextBlockEx is an extended TextBlock that adds simple support for bold text and adding new lines.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>TextBlockEx</strong> is an extended TextBlock that adds simple support for bold text and adding new lines.
    /// </para>
    /// <para>
    /// To add <strong>bold text</strong> insert "\b" or "\!" (without quotes) into your text.
    /// The first occurrence start bold text and the next occurrence stops bold text.
    /// </para>
    /// <para>
    /// To add <strong>new line</strong> insert "\n" into the text.
    /// </para>
    /// <para>
    /// To add <strong>orange square bullet sign</strong> insert "\*" into the text.
    /// </para>
    /// <para>
    /// To add <strong>spaces</strong> insert "\_" with as many underscored as many spaces is needed - for example "\___" - for 3 spaces
    /// </para>
    /// <para>
    /// To change <strong>text color</strong> (set Run.Foreground property) insert "\#RRGGBB" where RRGGBB is color is hex value. To reset the Foreground property to null, use "\#_".
    /// </para>
    /// <para>
    /// To add <strong>hyperlink</strong> insert "\@" that is followed by anchor text, then add ':' character and then url address. Complete with '|' character into the text - for example: "click here \@Ab4d.SharpEngine:https://www.ab4d.com/SharpEngine.aspx| to learn more"
    /// </para>
    /// </remarks>
    [ContentProperty("ContentText")]
    public class TextBlockEx : TextBlock
    {
        #region CreateCustomInlineEventArgs
        /// <summary>
        /// CreateCustomInlineEventArgs is used for CreateCustomInlineCallback event.
        /// </summary>
        public class CreateCustomInlineEventArgs : EventArgs
        {
            /// <summary>
            /// customActionIndex - from 0 to 9
            /// </summary>
            public int CustomActionIndex { get; private set; }

            /// <summary>
            /// text between custom actions markers (for example "abc" in the following text "...\1abc\1..."
            /// </summary>
            public string ContentText { get; private set; }

            /// <summary>
            /// CreatedInline property must be set in the event handler.
            /// </summary>
            public Inline CreatedInline { get; set; }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="customActionIndex">customActionIndex</param>
            /// <param name="contentText">contentText</param>
            public CreateCustomInlineEventArgs(int customActionIndex, string contentText)
            {
                CustomActionIndex = customActionIndex;
                ContentText = contentText;
            }
        }
        #endregion

        #region LinkClickedEventArgs
        /// <summary>
        /// LinkClickedEventArgs is used for LinkClicked event.
        /// </summary>
        public class LinkClickedEventArgs : EventArgs
        {
            /// <summary>
            /// url of the clicked link
            /// </summary>
            public string Url { get; private set; }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="url">url</param>
            public LinkClickedEventArgs(string url)
            {
                Url = url;
            }
        }
        #endregion

        #region ContentText
        // To get property design time support we need to redirect the Content from Inlines (as in TextBlock)
        // to our own property so we can update the shown content.
        // Maybe there is a better way to do this, but for our case this works great.

        public static readonly DependencyProperty ContentTextProperty =
                DependencyProperty.Register(
                        "ContentText",
                        typeof(string),
                        typeof(TextBlockEx),
                        new FrameworkPropertyMetadata(
                                string.Empty,
                                FrameworkPropertyMetadataOptions.AffectsMeasure |
                                FrameworkPropertyMetadataOptions.AffectsRender,
                                OnContentTextChanged));

        /// <summary>
        /// The Text property defines the content (text) to be displayed.
        /// </summary>
        public string ContentText
        {
            get { return (string)GetValue(ContentTextProperty); }
            set { SetValue(ContentTextProperty, value); }
        }

        private static void OnContentTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textBlockEx = (TextBlockEx)d;
            textBlockEx.UpdateText((string)e.NewValue);
        }
        #endregion

        #region LinkForegroundProperty
        /// <summary>
        /// LinkForegroundProperty
        /// </summary>
        public static readonly DependencyProperty LinkForegroundProperty =
            DependencyProperty.Register("LinkForeground", typeof(Brush), typeof(TextBlockEx));

        /// <summary>
        /// LinkForeground set link foreground brush.
        /// </summary>
        public Brush LinkForeground
        {
            get { return (Brush)GetValue(LinkForegroundProperty); }
            set { SetValue(LinkForegroundProperty, value); }
        }
        #endregion


        /// <summary>
        /// LinkClickedEventHandler
        /// </summary>
        /// <param name="sender">TextBlockEx</param>
        /// <param name="e">LinkClickedEventArgs</param>
        public delegate void LinkClickedEventHandler(TextBlockEx sender, LinkClickedEventArgs e);

        /// <summary>
        /// LinkClicked event is fired when user clicks on any link - if there are any subscribers than links are not automatically handled by TextBlockEx
        /// </summary>
        public event LinkClickedEventHandler LinkClicked;


        /// <summary>
        /// CreateCustomInlineEventHandler
        /// </summary>
        /// <param name="sender">TextBlockEx</param>
        /// <param name="e">CreateCustomInlineEventArgs</param>
        public delegate void CreateCustomInlineEventHandler(TextBlockEx sender, CreateCustomInlineEventArgs e);

        // It would be probably more correct to make CreateCustomInlineCallback this a delegate,
        // but having an event allows us to make the handler from XAML editor which improves usability (and TextBlockEx is all about usability)

        /// <summary>
        /// Callback method that is called when a custom action (for example "\1" or "\2") is present in text.
        /// The handler must set the CreatedInline property in the CreateCustomInlineEventArgs.
        /// </summary>
        public event CreateCustomInlineEventHandler CreateCustomInlineCallback;

        /// <summary>
        /// Gets or sets the foreground brush that is used to show bullet symbols.
        /// </summary>
        public Brush BulletForeground { get; set; }


        public TextBlockEx()
        {
            BulletForeground = Brushes.Orange;
        }

        public TextBlockEx(Inline inline)
            : base(inline)
        {
            BulletForeground = Brushes.Orange;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            BeginInit(); // We cannot put that into UpdateText, because UpdateText is also called from OnContentTextChanged that is already inside Begin/EndInit

            if (!string.IsNullOrEmpty(Text))
                UpdateText(Text);
            else
                UpdateText(ContentText);

            EndInit();
        }

        private void UpdateText(string originalText)
        {
            if (string.IsNullOrEmpty(originalText))
            {
                Text = "";
                return;
            }

            Inlines.Clear();

            bool isBold = false;
            string part = "";
            var originalTextLength = originalText.Length;

            Brush customForegroundBrush = null;

            int pos1 = 0;
            while (pos1 != -1 || pos1 > originalText.Length - 1)
            {
                int pos2 = originalText.IndexOf('\\', pos1);

                if (pos2 == -1)
                {
                    part = originalText.Substring(pos1);
                    break;
                }

                part = originalText.Substring(pos1, pos2 - pos1);

                if (originalTextLength < pos2 + 2)
                    break;

                char command = originalText[pos2 + 1];

                var run = new Run(part);
                if (isBold)
                    run.FontWeight = FontWeights.Bold;

                if (customForegroundBrush != null)
                    run.Foreground = customForegroundBrush;

                Inlines.Add(run);

                if (command == 'n') // new paragraph
                {
                    Inlines.Add(new LineBreak());
                    if (originalTextLength > pos2 + 2 && originalText[pos2 + 2] == ' ') // Usually when text is entered in xaml, a new line in the xaml is converted into one space - just skip it
                        pos2++;
                }
                else if (command == 'b' || command == '!') // Toggle bold
                {
                    isBold = !isBold;
                }
                else if (command == '*') // Orange square bullet sign
                {
                    var bulletRun = new Run(" ▪ ")
                    {
                        Foreground = BulletForeground
                    };
                    Inlines.Add(bulletRun);
                }
                else if (command == '_')
                {
                    int spacesCount = CountChars(originalText, '_', pos2 + 1);
                    Inlines.Add(new Run(new string(' ', spacesCount)));
                    pos2 += spacesCount - 1;
                }
                else if (command == '#')
                {
                    if (originalText[pos2 + 2] == '_')
                    {
                        customForegroundBrush = null;
                        pos2++;
                    }
                    else
                    {
                        string colorHexText = originalText.Substring(pos2 + 2, 6);

                        try
                        {
                            byte red = Convert.ToByte(colorHexText.Substring(0, 2), 16);
                            byte green = Convert.ToByte(colorHexText.Substring(2, 2), 16);
                            byte blue = Convert.ToByte(colorHexText.Substring(4, 2), 16);

                            customForegroundBrush = new SolidColorBrush(Color.FromRgb(red, green, blue));
                        }
                        catch
                        {
                            customForegroundBrush = null;
                        }

                        pos2 += 6;
                    }
                }
                else if (command == '\\')
                {
                    // Add backslash
                    Inlines.Add(new Run("\\"));
                }
                else if (command == '@')
                {
                    int pos3 = originalText.IndexOf(':', pos2 + 1);
                    int pos4 = originalText.IndexOf('|', pos3 + 1);

                    string anchorText = originalText.Substring(pos2 + 2, pos3 - pos2 - 2);
                    string urlAddress = originalText.Substring(pos3 + 1, pos4 - pos3 - 1);

                    var hyperlink = new Hyperlink(new Run(anchorText));

                    if (urlAddress.Length > 0)
                        hyperlink.NavigateUri = new Uri(urlAddress);

                    if (LinkForeground != null)
                        hyperlink.Foreground = LinkForeground;

                    if (isBold)
                        hyperlink.FontWeight = FontWeights.Bold;

                    hyperlink.ToolTip = urlAddress;

                    hyperlink.RequestNavigate += HyperlinkOnRequestNavigate;

                    Inlines.Add(hyperlink);

                    pos2 = pos4 - 1;
                }
                else if (command >= '0' && command <= '9')
                {
                    // Custom Inline action
                    // We trigger CreateCustomInline event and send the index of the commands (0 for '0', 1 for '1')
                    // and text content between start and end command markers.
                    // The event handler should create a custom Inline and set it to CreatedInline property.
                    int endPos = originalText.IndexOf("\\" + command, pos2 + 1, StringComparison.Ordinal); // find end of text for this custom action
                    if (endPos > 0)
                    {
                        var createCustomInlineEventArgs = new CreateCustomInlineEventArgs(customActionIndex: command - '0',
                                                                                          contentText: originalText.Substring(pos2 + 2, endPos - pos2 - 2));

                        OnCreateCustomInlineCallback(createCustomInlineEventArgs);

                        if (createCustomInlineEventArgs.CreatedInline != null)
                        {
                            Inlines.Add(createCustomInlineEventArgs.CreatedInline);

                            pos1 = endPos + 2;
                            continue;
                        }
                    }
                }

                pos1 = pos2 + 2;
            }

            if (!string.IsNullOrEmpty(part))
                Inlines.Add(part);
        }

        private static int CountChars(string text, char ch, int startPos)
        {
            int pos = startPos;
            int length = text.Length;

            while (pos < length && text[pos] == ch)
            {
                pos++;
            }

            return pos - startPos;
        }

        private void HyperlinkOnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            string url;
            if (e.Uri == null)
                url = "";
            else
                url = e.Uri.ToString();

            bool isHandled = OnLinkClicked(url);

            if (!isHandled)
            {
                // For CORE3 project we need to set UseShellExecute to true,
                // otherwise a "The specified executable is not a valid application for this OS platform" exception is thrown.
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }

            e.Handled = true;
        }

        /// <summary>
        /// OnLinkClicked fires the LinkClicked event - returns true if there are any subscribers (in this case the link was already handled)
        /// </summary>
        /// <param name="url">url</param>
        /// <returns>returns true if there are any subscribers (in this case the link was already handled)</returns>
        protected bool OnLinkClicked(string url)
        {
            if (LinkClicked != null)
            {
                LinkClicked(this, new LinkClickedEventArgs(url));
                return true;
            }

            return false;
        }

        /// <summary>
        /// OnCreateCustomInlineCallback
        /// </summary>
        /// <param name="e">CreateCustomInlineEventArgs</param>
        protected void OnCreateCustomInlineCallback(CreateCustomInlineEventArgs e)
        {
            if (CreateCustomInlineCallback != null)
                CreateCustomInlineCallback(this, e);
        }
    }
}