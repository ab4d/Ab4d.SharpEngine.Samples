using System.Runtime.CompilerServices;
using Ab4d.SharpEngine.Common;
using System.Diagnostics;

namespace Ab4d.SharpEngine.Samples.Common.Utils;

// This is a basic Markdown parser and text creator
// Supported features:
// "\r\n" or "\n" will add a line break
// "\t" will add TabSize spaces
// Headers up to level 3 (# ## ###)
// Bold text (**bold text**)
// Bullets (-bullet1 +bullet2 *bullet3)
// Code (`code` or ```code```)
// Link: [ab4d.com](https://www.ab4d.com) or only text displayed as a link [ab4d]
// Image ![Tree texture](Resources/Textures/TreeTexture.png)

public abstract class MarkdownTextCreator<T> where T : class
{
    public Action<string>? ErrorWriterAction;

    protected int currentLineIndex;
    protected int currentCharIndex;
    protected int currentCommandCharIndex;

    protected bool isBold;
    protected bool isCode;


    public float FontSize = 16;

    public int TabSize = 4;

    public Color3 TextColor = Colors.Black;

    public Color3 BulletColor = Colors.Orange;
    
    public Color3 LinkColor = Colors.DarkBlue;
    
    public Color3 CodeBackgroundColor = Colors.LightGray;

    public string BulletText = " ▪ ";

    /// <summary>
    /// LinkClicked event is fired when user clicks on any link - if there are any subscribers than links are not automatically handled by MarkdownTextCreator
    /// </summary>
    public event EventHandler<string>? LinkClicked;


    public T Create(string markdownText)
    {
        var textElementToShow = UpdateOrCreate(markdownText, createNew: true);

        return textElementToShow;
    }

    public void Update(string markdownText)
    {
        UpdateOrCreate(markdownText, createNew: false);
    }
    
    private T UpdateOrCreate(string markdownText, bool createNew)
    {
        if (markdownText == null)
            throw new ArgumentNullException(nameof(markdownText));


        isBold = false;
        isCode = false;
        BeginMarkdown(markdownText, createNew);

        // Replace "\r\n" and "\n" text into actual new line
        markdownText = markdownText.Replace("\\r\\n", "\n");
        markdownText = markdownText.Replace("\\n", "\n");

        // Replace tabs
        if (TabSize > 0)
        {
            string tabText = new String(' ', TabSize);
            markdownText = markdownText.Replace("\\t", tabText);
            markdownText = markdownText.Replace("\t", tabText); // Also convert actual tabs into spaces
        }

        // Convert windows new line into single \n
        markdownText = markdownText.Replace("\r\n", "\n");

        // Get lines
        var textLines = markdownText.Split('\n');

        for (currentLineIndex = 0; currentLineIndex < textLines.Length; currentLineIndex++)
        {
            var oneLine = textLines[currentLineIndex];

            try
            {
                ParseOneLine(oneLine);

                if (currentLineIndex < textLines.Length - 1)
                    AddLineBreak();
            }
            catch (Exception ex)
            {
                WriteError(currentLineIndex, currentCharIndex, "Error parsing line: " + ex.Message);
            }
        }

        var textElementToShow = CompleteMarkdown();

        return textElementToShow;
    }

    private void ParseOneLine(string lineText)
    {
        bool isCommand;
        bool isStartOfLine = true;
        int commandLength;

        currentCharIndex = 0;
        while (currentCharIndex < lineText.Length)
        {
            char ch = lineText[currentCharIndex];

            switch (ch)
            {
                case '-':
                case '+':
                case '#':
                case '*':
                case '`':
                case '[':
                case '!':
                    isCommand = true;
                    break;

                case '\\': 
                    // get next char
                    currentCharIndex++;

                    if (currentCharIndex >= lineText.Length)
                        continue;

                    ch = lineText[currentCharIndex];
                    isCommand = false;
                    break;

                default:
                    isCommand = false;
                    break;
            }


            if (isCommand)
            {
                commandLength = CountChars(lineText, ch, currentCharIndex + 1) + 1;

                bool commandEndsWithSpace = lineText.Length > currentCharIndex + commandLength ? lineText[currentCharIndex + commandLength] == ' ' : false;

                switch (ch)
                {
                    case '-':
                    case '+': 
                    //case '*': // * is handled below
                        if (isStartOfLine && commandLength == 1 && commandEndsWithSpace)
                            AddBullet();
                        else
                            isCommand = false; // write as normal char

                        break;

                    case '#':
                        if (isStartOfLine && commandEndsWithSpace)
                        {
                            AddHeading(lineText.Substring(currentCharIndex + commandLength + 1), headingLevel: commandLength);
                            return;
                        }

                        // if # is not at the beginning of the line, then just write it as a char
                        isCommand = false;

                        break;

                    case '*':
                        if (commandLength == 1)
                        {
                            if (isStartOfLine && commandEndsWithSpace)
                                AddBullet();
                            else
                                isCommand = false; // write as normal char
                        }
                        else if (commandLength == 2)
                        {
                            WriteCurrentText();
                            isBold = !isBold;
                            currentCharIndex ++; // skip second *
                        }
                        else
                        {
                            isCommand = false; // write as normal char
                        }

                        break;
                    
                    case '`':
                        if (commandLength == 1 || commandLength == 3)
                        {
                            WriteCurrentText();
                            isCode = !isCode;

                            if (commandLength > 1)
                                currentCharIndex += commandLength - 1; // skip command
                        }
                        else
                        {
                            isCommand = false; // write as normal char
                        }

                        break;

                    case '[':
                        bool isValidLink = ParseLink(lineText, currentCharIndex, out var linkUrl, out var linkText, out int endOfLinkPosition);

                        if (!isValidLink)
                        {
                            isCommand = false; // write as normal char
                        }
                        else
                        {
                            AddLink(linkUrl, linkText);
                            currentCharIndex = endOfLinkPosition - 1;
                        }

                        break;
                    
                    case '!':
                        bool isValidImageLink = ParseLink(lineText, currentCharIndex + 1, out var imageSource, out var imageAltText, out int endOfImagePosition);

                        if (!isValidImageLink || imageSource == null)
                        {
                            isCommand = false; // write as normal char
                        }
                        else
                        {
                            AddImage(imageSource, imageAltText);
                            currentCharIndex = endOfImagePosition - 1;
                        }

                        break;
                }

                isStartOfLine = false;
            }

            if (!isCommand)
            {
                AddChar(ch);

                if (ch != ' ')
                    isStartOfLine = false;
            }

            currentCharIndex++;
        }
    }

    protected void WriteError(string message)
    {
        WriteError(-1, -1, message);
    }

    protected void WriteError(int line, int column, string message)
    {
        if (ErrorWriterAction == null)
            return;

        string finalMessage;

        if (line <= 0 && column <= 0)
            finalMessage = message;
        else
            finalMessage = $"Error in line {line}, column {column}: {message}";

        ErrorWriterAction(finalMessage);
    }

    protected void WriteInvalidCommand(string command)
    {
        WriteError(currentLineIndex, currentCommandCharIndex, "Invalid command: " + command);
    }

    // Returns false if ths is not a valid link markdown
    protected bool ParseLink(string lineText, int currentPosition, out string? linkUrl, out string? linkText, out int endOfLinkPosition)
    {
        // default values
        linkUrl = null;
        linkText = null;
        endOfLinkPosition = currentPosition;

        if (lineText[currentPosition] == '[')
        {
            int pos2 = lineText.IndexOf(']', currentPosition + 1);

            if (pos2 != -1)
            {
                linkText = lineText.Substring(currentPosition + 1, pos2 - currentPosition - 1);

                endOfLinkPosition = pos2 + 1;

                // Do we also have url in brackets
                if (lineText.Length > pos2 + 1 && lineText[pos2 + 1] == '(')
                {
                    int pos3 = lineText.IndexOf(')', pos2 + 2);

                    if (pos3 != -1)
                    {
                        linkUrl = lineText.Substring(pos2 + 2, pos3 - pos2 - 2);
                        endOfLinkPosition = pos3 + 1;
                    }
                }

                return true; // we have a valid link
            }
        }
        
        return false;
    }

    protected int CountChars(string text, char ch, int startIndex)
    {
        int count = 0;
        int index = startIndex;

        while (index < text.Length)
        {
            if (text[index] != ch)
                break;
            
            count++;
            index++;
        }

        return count;
    }

    protected float GetHeadingFontSize(int headingLevel)
    {
        if (headingLevel == 1)
            return FontSize * 1.6f;
        
        if (headingLevel == 2)
            return FontSize * 1.4f;

        return FontSize * 1.2f;
    }
    
    protected Color3 GetHeadingTextColor(int headingLevel)
    {
        // Only adjust heading color if TextColor is black
        if (TextColor != Color3.Black)
            return TextColor;
        
        if (headingLevel == 1)
            return new Color3(0.15f, 0.15f, 0.15f);
        
        if (headingLevel == 2)
            return new Color3(0.1f, 0.1f, 0.1f);

        return new Color3(0.1f, 0.1f, 0.1f);
    }
    
    protected Color3 GetBoldTextColor()
    {
        // Only adjust heading color if TextColor is black
        if (TextColor != Color3.Black)
            return TextColor;

        return new Color3(0.1f, 0.1f, 0.1f);
    }

    protected virtual void OnLinkClicked(string url)
    {
        if (LinkClicked != null)
            LinkClicked(this, url);
        else
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    protected abstract void BeginMarkdown(string markdownText, bool createNew);

    protected abstract T CompleteMarkdown();

    protected abstract void AddLineBreak();

    protected abstract void AddChar(char ch);
    
    protected abstract void AddBullet();

    protected abstract void AddHeading(string? headingText, int headingLevel);
    
    protected abstract void AddLink(string? url, string? text);
    
    protected abstract void AddImage(string source, string? altText);

    protected abstract void WriteCurrentText();
}