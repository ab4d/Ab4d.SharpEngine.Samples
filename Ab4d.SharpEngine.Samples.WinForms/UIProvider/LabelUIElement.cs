using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Samples.Common;
using System;

namespace Ab4d.SharpEngine.Samples.WinForms.UIProvider;

public class LabelUIElement : WinFormsUIElement
{
    private Label _label;

    private string? _styleString;

    public LabelUIElement(WinFormsUIProvider winFormsUIProvider, string text, bool isHeader, float width = 0, float height = 0, float maxWidth = 0)
        : base(winFormsUIProvider)
    {
        var (textToShow, toolTip) = winFormsUIProvider.ParseTextAndToolTip(text);

        _label = new Label()
        {
            Text = textToShow,
        };

        if (width > 0 || height > 0)
        {
            // See https://stackoverflow.com/questions/1204804/word-wrap-for-a-label-in-windows-forms
            _label.AutoSize = true;
            _label.MaximumSize = new Size((int)(width * winFormsUIProvider.UIScale), (int)(height * winFormsUIProvider.UIScale));
        }
        else
        {
            _label.AutoSize = true;
        }

        if (toolTip != null)
            winFormsUIProvider.SetToolTip(_label, toolTip);

        if (isHeader)
        {
            _label.Font = winFormsUIProvider.BoldFont;

            double topMargin = winFormsUIProvider.HeaderTopMargin;
            if (winFormsUIProvider.CurrentPanel != null && winFormsUIProvider.CurrentPanel.ChildrenCount > 0)
                topMargin += 8;

            _label.Margin = new Padding(0, (int)topMargin, 0, (int)winFormsUIProvider.HeaderBottomMarin);

            _styleString = "bold";
        }
        else
        {
            _label.Font = winFormsUIProvider.Font;
        }

        WinFormsControl = _label;
    }


    public override string? GetText() => _label.Text;

    public override ICommonSampleUIElement SetText(string? text)
    {
        var (textToShow, toolTip) = winFormsUIProvider.ParseTextAndToolTip(text);
        _label.Text = textToShow;
        winFormsUIProvider.SetToolTip(_label, toolTip);        
        return this;
    }


    protected override void OnSetColor(Color winFormsColor)
    {
        _label.ForeColor = winFormsColor;
    }


    public override string? GetStyle() => _styleString;

    public override ICommonSampleUIElement SetStyle(string style)
    {
        _styleString = style;

        Font? font;
        if (style.Contains("italic"))
        {
            font = winFormsUIProvider.ItalicFont;
        }
        else
        {
            font = style.Contains("bold", StringComparison.OrdinalIgnoreCase) ? winFormsUIProvider.BoldFont : winFormsUIProvider.Font;
        }

        _label.Font = font;

        return this;
    }
}