using Ab4d.SharpEngine.Common;
using System.Numerics;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.Lines;

public class MiterLimitSample : CommonSample
{
    public override string Title => "PolyLineNode with MiterLimit vs MultiLineNode";
    public override string Subtitle => "MiterLimit in PolyLineNode defines when a mitered joint changes to a beveled joint.\nThe last two lines show the difference when line is rendered by using MultiLineNode.";

    public MiterLimitSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        var textBlockFactory = context.GetTextBlockFactory();
        textBlockFactory.TextColor = new Color4(0.3f, 0.3f, 0.3f, 1);
        textBlockFactory.FontSize = 30;

        
        var positions = CreateSnakePositions(new Vector3(-100, 0, 0), 50, 20, 80);
        
        AddMiterLimitsSample(scene, positions, textBlockFactory, zOffset: -100, miterLimit: 0);
        AddMiterLimitsSample(scene, positions, textBlockFactory, zOffset: 0, miterLimit: 2);
        AddMiterLimitsSample(scene, positions, textBlockFactory, zOffset: 100, miterLimit: 4);
        AddMiterLimitsSample(scene, positions, textBlockFactory, zOffset: 200, miterLimit: 10);
                                  
        AddMiterLimitsSample(scene, positions, textBlockFactory, zOffset: 350, isPolyLine: false, isLineStrip: true);
        AddMiterLimitsSample(scene, positions, textBlockFactory, zOffset: 430, isPolyLine: false, isLineStrip: false);


        var textNode0 = textBlockFactory.CreateTextBlock("PolyLineNode:", new Vector3(-380, 0, -100), positionType: PositionTypes.Right);
        scene.RootNode.Add(textNode0);
        
        var textNode1 = textBlockFactory.CreateTextBlock("Mitered joint:", new Vector3(-150, 0, -220), positionType: PositionTypes.Right);
        scene.RootNode.Add(textNode1);

        var polyLineNode1 = new PolyLineNode()
        {
            Positions = new Vector3[] { new Vector3(-130, 0, -200), new Vector3(-110, 0, -240), new Vector3(-90, 0, -200) },
            LineThickness = 15,
            MiterLimit = 10,
        };

        scene.RootNode.Add(polyLineNode1);
        
        
        var textNode2 = textBlockFactory.CreateTextBlock("Beveled joint:", new Vector3(200, 0, -220), positionType: PositionTypes.Right);
        scene.RootNode.Add(textNode2);

        var polyLineNode2 = new PolyLineNode()
        {
            Positions = new Vector3[] { new Vector3(220, 0, -200), new Vector3(240, 0, -240), new Vector3(260, 0, -200) },
            LineThickness = 15,
            MiterLimit = 0,
        };

        scene.RootNode.Add(polyLineNode2);


        var rectangleNode1 = new RectangleNode()
        {
            Position = new Vector3(-360, 0, 250),
            PositionType = PositionTypes.TopLeft,
            HeightDirection = new Vector3(0, 0, 1),
            WidthDirection = new Vector3(1, 0, 0),
            Size = new Vector2(710, 430),
            LineColor = Colors.Gray,
            LineThickness = 2
        };

        scene.RootNode.Add(rectangleNode1);


        var textNode3 = textBlockFactory.CreateTextBlock("MultiLineNode:", new Vector3(-380, 0, 350), positionType: PositionTypes.Right);
        scene.RootNode.Add(textNode3);

        var rectangleNode2 = new RectangleNode()
        {
            Position = new Vector3(-360, 0, 280),
            PositionType = PositionTypes.BottomLeft,
            HeightDirection = new Vector3(0, 0, 1),
            WidthDirection = new Vector3(1, 0, 0),
            Size = new Vector2(710, 200),
            LineColor = Colors.Gray,
            LineThickness = 2
        };

        scene.RootNode.Add(rectangleNode2);




        if (targetPositionCamera != null)
        {
            targetPositionCamera.TargetPosition = new Vector3(-100, -70, 70);
            targetPositionCamera.Heading = 0;
            targetPositionCamera.Attitude = -50;
            targetPositionCamera.Distance = 1450;
        }
    }

    private void AddMiterLimitsSample(Scene scene, Vector3[] positions, TextBlockFactory textBlockFactory, float zOffset, float miterLimit = 0, bool isPolyLine = true, bool isLineStrip = true)
    {
        var groupNode = new GroupNode();
        groupNode.Transform = new TranslateTransform(0, 0, zOffset);
        scene.RootNode.Add(groupNode);

        
        LineBaseNode lineNode;
        string text;

        if (isPolyLine)
        {
            lineNode = new PolyLineNode()
            {
                Positions = positions,
                MiterLimit = miterLimit
            };

            text = $"MiterLimit: {miterLimit}";
        }
        else
        {
            lineNode = new MultiLineNode()
            {
                Positions = positions,
                IsLineStrip = isLineStrip // when true, then Positions define connected lines; if false than lines are not connected and each line is defined by two positions
            };

            text = $"IsLineStrip: {isLineStrip}";
        }

        lineNode.LineColor = Colors.Black;
        lineNode.LineThickness = 15;

        groupNode.Add(lineNode);


        var textNode2 = textBlockFactory.CreateTextBlock(text, new Vector3(-120, 0, 0), positionType: PositionTypes.Right);
        groupNode.Add(textNode2);
    }

    private Vector3[] CreateSnakePositions(Vector3 startPosition, float segmentsLength, float startAngle, float maxAngle)
    {
        var positions = new List<Vector3>();
        positions.Add(startPosition);

        float angle = startAngle;
        bool negateAngle = false;

        var currentPosition = startPosition;

        var initialDirectionVector = new Vector3(segmentsLength, 0, 0);

        while (angle <= maxAngle)
        {
            var usedAngle = negateAngle ? -angle : angle;
            var transformMatrix = Matrix4x4.CreateFromAxisAngle(new Vector3(0, 1, 0), MathUtils.DegreesToRadians(usedAngle));

            var currentDirectionVector = Vector3.TransformNormal(initialDirectionVector, transformMatrix);

            currentPosition += currentDirectionVector;

            positions.Add(currentPosition);

            angle += 5;
            negateAngle = !negateAngle;
        }

        return positions.ToArray();
    }
}