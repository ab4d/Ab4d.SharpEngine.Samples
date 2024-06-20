using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;

namespace Ab4d.SharpEngine.Samples.WinForms.UIProvider;

public class StackPanelUIElement : WinFormsUIPanel
{
    private Panel? _borderPanel;

    private FlowLayoutPanel _flowLayoutPanel;

    private PositionTypes _alignment;

    public override Panel? WinFormsPanel => _flowLayoutPanel;

    public override bool IsVertical => _flowLayoutPanel.FlowDirection == FlowDirection.TopDown;

    public int BorderThickness { get; private set; }


    public StackPanelUIElement(WinFormsUIProvider winFormsUIProvider, object parentObject, PositionTypes alignment, bool isVertical, bool addBorder, bool isSemiTransparent)
        : base(winFormsUIProvider)
    {
        _flowLayoutPanel = new FlowLayoutPanel()
        {
            FlowDirection = isVertical ? FlowDirection.TopDown : FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.White,
            Padding = new Padding(8, 6, 8, 6),
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            AutoSize = true,
        };

        // UH:
        // How to align FlowLayoutPanel to bottom-right
        // Using Anchor does not work, so we are manually updating the location when size of _flowLayoutPanel or its parent changes
        _flowLayoutPanel.SizeChanged += (sender, args) => UpdateLocation();

        _alignment = alignment;

        if (addBorder)
        {
            BorderThickness = 2;

            _flowLayoutPanel.Location = new Point(BorderThickness, BorderThickness);
            _flowLayoutPanel.Margin = new Padding(BorderThickness);

            _borderPanel = new Panel()
            {
                Margin = new Padding(5),
                BackColor = Color.Black,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                AutoSize = true,
            };

            _borderPanel.Controls.Add(_flowLayoutPanel);

            WinFormsControl = _borderPanel;
        }
        else
        {
            _flowLayoutPanel.Margin = new Padding(5);

            BorderThickness = 0;
            WinFormsControl = _flowLayoutPanel;
        }
    }

    // This is called from WinFormsUIProvider when the size of root panel is changed
    public void UpdateLocation()
    {
        Size parentSize;
        Size preferredSize;
        Padding margin;

        if (_borderPanel != null)
        {
            parentSize = _borderPanel.Parent?.ClientSize ?? Size.Empty;
            margin = _borderPanel.Margin;
            preferredSize = _borderPanel.PreferredSize;
        }
        else
        {
            parentSize = _flowLayoutPanel.Parent?.ClientSize ?? Size.Empty;
            margin = _flowLayoutPanel.Margin;
            preferredSize = _flowLayoutPanel.PreferredSize;
        }

        if (parentSize.IsEmpty)
            return;

        var alignment = _alignment;

        int x = alignment.HasFlag(PositionTypes.Left) ? margin.Left :
                alignment.HasFlag(PositionTypes.Right) ? parentSize.Width - preferredSize.Width - margin.Right :
                (parentSize.Width - preferredSize.Width) / 2;

        var y = alignment.HasFlag(PositionTypes.Top) ? margin.Top :
                alignment.HasFlag(PositionTypes.Bottom) ? parentSize.Height - preferredSize.Height - margin.Bottom :
                (parentSize.Height - preferredSize.Height) / 2;

        if (_borderPanel != null)
            _borderPanel.Location = new Point(x, y);
        else
            _flowLayoutPanel.Location = new Point(x, y);
    }
}