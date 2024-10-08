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
    
    public LineCapsSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        CreateSampleLines();

        if (targetPositionCamera != null)
        {
            targetPositionCamera.TargetPosition = new Vector3(-50, -15, 0);
            targetPositionCamera.Heading = 17;
            targetPositionCamera.Attitude = -16;
            targetPositionCamera.Distance = 1100;
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
                var polyLineVisual3D = new PolyLineNode()
                {
                    Positions = _multiLinePositions.Select(p => position + p + offset).ToArray(),
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

        var lineThicknesses = new float[] { 0.5f, 0.8f, 1, 2, 3, 5, 10 };
        ui.CreateComboBox(
            lineThicknesses.Select(t => t.ToString(CultureInfo.InvariantCulture)).ToArray(), 
            (selectedIndex, selectedText) =>
            {
                _selectedLineThickness = lineThicknesses[selectedIndex];
                CreateSampleLines();
            }, 
            selectedItemIndex: 3, keyText: "LineThickness:");

        //ui.AddSeparator();

        ui.CreateLabel("Global arrow settings:", isHeader: true);

        var arrowAngles = new float[] { 10, 15, 30, 45 };
        ui.CreateComboBox(
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
            keyText: "Arrow angle: (?):Specifies the angle of the standard Arrow line cap.\nThe angle of other arrows will be adjusted accordingly.",
            keyTextWidth: 130);
        
        var maxArrowLengths = new float[] { 0.1f, 0.25f, 0.33f, 0.4f, 0.5f };
        ui.CreateComboBox(
            maxArrowLengths.Select(t => t.ToString(CultureInfo.InvariantCulture)).ToArray(), 
            (selectedIndex, selectedText) =>
            {
                // Update the static MaxLineArrowLength static field in LineNode.
                // MaxLineArrowLength specifies the maximum arrow length set as fraction of the line length - e.g. 0.333 means that the maximum arrow length will be 1 / 3 (=0.333) of the line length.
                // If the line is short so that the arrow length exceeds the amount defined by MaxLineArrowLength, the arrow is shortened (the arrow angle is increased).
                // Default value is 0.333 (1 / 3 of the line's length)
                LineNode.MaxLineArrowLength = maxArrowLengths[selectedIndex];
                CreateSampleLines();
            }, 
            selectedItemIndex: 2, 
            width: 60,
            keyText: "Max arrow length: (?):Specifies the maximum arrow length set as fraction of the line length.\nFor example: 0.2 means that the maximum arrow length will be 1 / 5 (=0.2) of the line length.\nIf the line is short so that the arrow length exceeds the amount defined by MaxLineArrowLength,\nthe arrow is shortened (the arrow angle is increased).",
            keyTextWidth: 130);

        ui.AddSeparator();

        ui.CreateButton("Randomize line caps", () => RandomizeLineCaps());
    }
}