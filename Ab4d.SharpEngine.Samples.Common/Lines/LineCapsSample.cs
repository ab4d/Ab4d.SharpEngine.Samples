using System.Globalization;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.Lines;

public class LineCapsSample : CommonSample
{
    public override string Title => "Lines with caps";
    public override string Subtitle => "Ab4d.SharpEngine can fully hardware accelerate rendering lines with line caps.\nThis allows rendering millions of such lines.";

    private readonly Vector3[] _multiLinePositions = new Vector3[]
    {
        new Vector3(10, 0, 0),
        new Vector3(10, 60, 0),
        new Vector3(30, 20, 0),
        new Vector3(30, 80, 0),
    };

    private List<GroupNode> _textGroupNodes = new();

    private float _selectedLineThickness = 2;

    private bool _isRandomized;

    private bool _showMultiLines = true;
    private bool _showPolyLines = true;
    private bool _showLineArc = false;

    private List<ICommonSampleUIElement> _globalSettingControls = new();
    private ICommonSampleUIElement? _showGlobalSettingsButton;

    public LineCapsSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        CreateSampleLines();

        if (targetPositionCamera != null)
        {
            targetPositionCamera.TargetPosition = new Vector3(-50, 15, 0);
            targetPositionCamera.Heading = 17;
            targetPositionCamera.Attitude = -16;
            targetPositionCamera.Distance = 1050;
        }
    }

    private void CreateSampleLines()
    {
        if (Scene == null)
            return;

        Scene.RootNode.Clear();


        var lineCapNames = Enum.GetNames(typeof(LineCap));

        float lineOffset = 20;
        float startX = -lineCapNames.Length * lineOffset * 4 / 2;

        var position = new Vector3(startX, -100, 0);
        var lineVector = new Vector3(0, 100, 0);
        var lineOffsetVector = new Vector3(20, 0, 0);

        var textBlockFactory = context.GetTextBlockFactory();
        textBlockFactory.BackgroundColor = Colors.LightYellow;
        textBlockFactory.BorderThickness = 1;
        textBlockFactory.BorderColor = Colors.DimGray;
        textBlockFactory.FontSize = 14;


        for (var i = 0; i < lineCapNames.Length; i++)
        {
            var lineCapName = lineCapNames[i];
            var lineCapValue = (LineCap)Enum.Parse(typeof(LineCap), lineCapName);


            string displayText = lineCapName.Replace("Anchor", "");

            float isOffset = (i % 2) == 0 ? 0 : 1;


            var textPosition = position + lineOffsetVector + new Vector3(0, isOffset * -20, 40 + isOffset * 20);
            var textNode = textBlockFactory.CreateTextBlock(displayText, textPosition, textAttitude: 30);
            Scene.RootNode.Add(textNode);

            _textGroupNodes.Add(textNode);


            var line = new LineNode()
            {
                StartPosition = position,
                EndPosition = position + lineVector,
                StartLineCap = lineCapValue,
                LineColor = Colors.Black,
                LineThickness = _selectedLineThickness
            };

            Scene.RootNode.Add(line);


            position += lineOffsetVector;

            line = new LineNode()
            {
                StartPosition = position,
                EndPosition = position + lineVector,
                EndLineCap = lineCapValue,
                LineColor = Colors.Black,
                LineThickness = _selectedLineThickness
            };

            Scene.RootNode.Add(line);


            position += lineOffsetVector;

            line = new LineNode()
            {
                StartPosition = position,
                EndPosition = position + lineVector,
                StartLineCap = lineCapValue,
                EndLineCap = lineCapValue,
                LineColor = Colors.Black,
                LineThickness = _selectedLineThickness
            };

            Scene.RootNode.Add(line);

            position += lineOffsetVector;
            position += lineOffsetVector;


            var offset = new Vector3(-4 * lineOffset, 120, 0);

            if (_showMultiLines)
            {
                var multiLineVisual3D = new MultiLineNode()
                {
                    Positions = _multiLinePositions.Select(p => position + p + new Vector3(-4 * lineOffset, 120, 0)).ToArray(),
                    LineColor = Colors.Orange,
                    LineThickness = _selectedLineThickness,
                    StartLineCap = lineCapValue,
                    EndLineCap = lineCapValue,
                };

                Scene.RootNode.Add(multiLineVisual3D);

                offset += new Vector3(0, 100, 0);
            }


            if (_showPolyLines)
            {
                Vector3[] polyLinePositions;
                if (_showLineArc)
                {
                    // Get positions that define a line arc
                    polyLinePositions = EllipseLineNode.GetArc3DPoints(centerPosition: new Vector3(15, 15, 0), 
                                                                       normalDirection: new Vector3(0, 0, 1), 
                                                                       zeroAngleDirection: new Vector3(1, 0, 0), 
                                                                       xRadius: 15, 
                                                                       yRadius: 15, 
                                                                       startAngle: 30, 
                                                                       endAngle: 300, 
                                                                       segments: 30);
                }
                else
                {
                    // Same positions as for multi-line
                    polyLinePositions = _multiLinePositions;
                }

                var polyLineVisual3D = new PolyLineNode()
                {
                    Positions = polyLinePositions.Select(p => position + p + offset).ToArray(),
                    LineColor = Colors.Green,
                    LineThickness = _selectedLineThickness,
                    StartLineCap = lineCapValue,
                    EndLineCap = lineCapValue,
                };

                Scene.RootNode.Add(polyLineVisual3D);
            }
        }

        if (_isRandomized)
            RandomizeLineCaps();
    }

    private void RandomizeLineCaps()
    {
        if (Scene == null)
            return;

        var rnd = new Random();

        int maxValue = Enum.GetValues(typeof(LineCap)).Length;

        Scene.RootNode.ForEachChild<LineBaseNode>(lineNode =>
        {
            lineNode.StartLineCap = (LineCap)rnd.Next(maxValue);
            lineNode.EndLineCap = (LineCap)rnd.Next(maxValue);
        });

        // Hide TextBlockVisual3D as they are not valid anymore
        foreach (var textGroupNode in _textGroupNodes)
            textGroupNode.Visibility = SceneNodeVisibility.Hidden;

        _isRandomized = true;
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateCheckBox("Show poly-lines (green)", isInitiallyChecked: _showPolyLines, isChecked =>
        {
            _showPolyLines = isChecked;
            CreateSampleLines();
        });
        
        ui.CreateCheckBox("Show multi-lines (orange)", isInitiallyChecked: _showMultiLines, isChecked =>
        {
            _showMultiLines = isChecked;
            CreateSampleLines();
        });
        
        ui.CreateCheckBox("Show line arc for poly-lines", isInitiallyChecked: _showLineArc, isChecked =>
        {
            _showLineArc = isChecked;
            CreateSampleLines();
        });

        var lineThicknesses = new float[] { 0.5f, 0.8f, 1, 2, 3, 5, 10 };
        ui.CreateComboBox(
            lineThicknesses.Select(t => t.ToString(CultureInfo.InvariantCulture)).ToArray(), 
            (selectedIndex, selectedText) =>
            {
                _selectedLineThickness = lineThicknesses[selectedIndex];
                CreateSampleLines();
            }, 
            selectedItemIndex: 3, keyText: "LineThickness:");

        ui.AddSeparator();

        ui.CreateButton("Randomize line caps", () => RandomizeLineCaps());

        _showGlobalSettingsButton = ui.CreateButton("Show global arrow settings", () =>
        {
            _showGlobalSettingsButton?.SetIsVisible(false);
            foreach (var oneControl in _globalSettingControls)
                oneControl.SetIsVisible(true);
        });


        _globalSettingControls.Add(ui.CreateLabel("Global arrow settings:", isHeader: true).SetIsVisible(false));

        var arrowAngles = new float[] { 10, 15, 30, 45 };
        _globalSettingControls.Add(ui.CreateComboBox(
            arrowAngles.Select(t => t.ToString(CultureInfo.InvariantCulture)).ToArray(), 
            (selectedIndex, selectedText) =>
            {
                // Update the static LineArrowAngle property static field in LineNode.
                // LineArrowAngle is the angle of the line arrows. Default value is 15 degrees.
                // Note that if the line is short so that the arrow length exceeds the amount defined by MaxLineArrowLength, the arrow is shortened which increased the arrow angle.
                LineNode.LineArrowAngle = arrowAngles[selectedIndex];
                CreateSampleLines();
            }, 
            selectedItemIndex: 1, 
            width: 60,
            keyText: 
@"Arrow angle: (?):Specifies the angle of the standard Arrow line cap.
The angle of other arrows will be adjusted accordingly.",
            keyTextWidth: 180).SetIsVisible(false));
        
        var maxArrowLengths = new float[] { 0.1f, 0.25f, 0.33f, 0.4f, 0.5f };
        _globalSettingControls.Add(ui.CreateComboBox(
            maxArrowLengths.Select(t => t.ToString(CultureInfo.InvariantCulture)).ToArray(), 
            (selectedIndex, selectedText) =>
            {
                LineNode.MaxLineArrowLength = maxArrowLengths[selectedIndex];
                CreateSampleLines();

                // This set the global MinLineStripArrowLength for all lines.
                // To use different value of MinLineStripArrowLength for only a few lines, 
                // call the SetMinLineArrowLength method.
                // For example:
                //var lineNode = new LineNode(/* all parameters */);
                //lineNode.SetMaxLineArrowLength(4);
            }, 
            selectedItemIndex: 2, 
            width: 60,
            keyText: 
@"Max arrow length: (?):Specifies the maximum arrow length set as fraction of the line length.
For example: 0.2 means that the maximum arrow length will be 1/5 (=0.2) of the line length.
If the line is short so that the arrow length exceeds the amount defined by MaxLineArrowLength,
the arrow is shortened (the arrow angle is increased).

To set a custom MaxLineArrowLength for a LineNode or derived class, call its SetMaxLineArrowLength method.",
            keyTextWidth: 180).SetIsVisible(false));
        
        
        var minArrowLengths = new float[] { 0, 1, 2, 3, 4, 6, 10 };
        
        _globalSettingControls.Add(ui.CreateComboBox(
            minArrowLengths.Select(t => t.ToString(CultureInfo.InvariantCulture)).ToArray(), 
            (selectedIndex, selectedText) =>
            {
                LineNode.MinLineStripArrowLength = minArrowLengths[selectedIndex];
                CreateSampleLines();

                // This set the global MinLineStripArrowLength for all lines.
                // To use different value of MinLineStripArrowLength for only a few lines, 
                // call the SetMinLineArrowLength method.
                // For example:
                //var polyLineNode = new PolyLineNode(/* all parameters */);
                //polyLineNode.SetMinLineArrowLength(4);
            }, 
            selectedItemIndex: 2, 
            width: 60,
            keyText: 
@"MinLineStripArrowLength: (?):Specifies the minimum arrow length set as a multiplier of the line thickness.
For example 2 means that the line arrow will not be shorter than 2 times the line arrow.
This can be used for line arcs and curves where the line segments are very short.
This is applied after the MaxLineArrowLength property and only for poly-lines and connected lines (IsLineStrip is true).
Default value is 2 that always shows the arrow size at least 2 line thicknesses long for connected lines.
This prevents hiding the line arrow for line arcs where individual line segments are very short.

To set a custom MinLineArrowLength for a LineNode or a derived class, call its SetMinLineArrowLength method.",
            keyTextWidth: 180).SetIsVisible(false));
        
        _globalSettingControls.Add(ui.CreateComboBox(
            minArrowLengths.Select(t => t.ToString(CultureInfo.InvariantCulture)).ToArray(), 
            (selectedIndex, selectedText) =>
            {
                LineNode.MinLineListArrowLength = minArrowLengths[selectedIndex];
                CreateSampleLines();

                // This set the global MinLineStripArrowLength for all lines.
                // To use different value of MinLineStripArrowLength for only a few lines, 
                // call the SetMinLineArrowLength method.
                // For example:
                //var multiLineNode = new MultiLineNode(/* all parameters */);
                //multiLineNode.SetMinLineArrowLength(4);
            }, 
            selectedItemIndex: 0, 
            width: 60,
            keyText: 
@"MinLineListArrowLength: (?):This is the same as MinLineStripArrowLength but is used only for individual lines and disconnected lines (IsLineStrip is false)
Default value is 0 that does not limit how small the arrow can be (it will disappear when the line is very short).

To see the effect of this value, change it to a bigger value (for example 4)
and then rotate the camera to view the lines from above.
This will reduce the length of the lines on the screen,
but the size of the arrow will be at least 4 times the line thickness.

To set a custom MinLineArrowLength for a LineNode or a derived class, call its SetMinLineArrowLength method.",
            keyTextWidth: 180).SetIsVisible(false));
    }
}