using Ab4d.SharpEngine.Common;
using System.Numerics;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.Lines;

public class MiterLimitSample : CommonSample
{
    public override string Title => "MiterLimit";
    public override string Subtitle => "MiterLimit in PolyLineNode defines when mitered joint changes to beveled joint.";

    public MiterLimitSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        var textBlockFactory = context.GetTextBlockFactory();
        textBlockFactory.TextColor = new Color4(0.3f, 0.3f, 0.3f, 1);
        textBlockFactory.FontSize = 30;


        AddMiterLimitsSample(scene, textBlockFactory, 0, -100);
        AddMiterLimitsSample(scene, textBlockFactory, 2, 0);
        AddMiterLimitsSample(scene, textBlockFactory, 4, 100);
        AddMiterLimitsSample(scene, textBlockFactory, 10, 200);
        
        
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


        var rectangleNode = new RectangleNode()
        {
            Position = new Vector3(-350, 0, 250),
            PositionType = PositionTypes.TopLeft,
            HeightDirection = new Vector3(0, 0, 1),
            WidthDirection = new Vector3(1, 0, 0),
            Size = new Vector2(700, 420),
            LineColor = Colors.Gray,
            LineThickness = 2
        };

        scene.RootNode.Add(rectangleNode);


        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 0;
            targetPositionCamera.Attitude = -40;
            targetPositionCamera.Distance = 1000;
        }
    }

    private void AddMiterLimitsSample(Scene scene, TextBlockFactory textBlockFactory, float miterLimit, float zOffset)
    {
        var groupNode = new GroupNode();
        groupNode.Transform = new TranslateTransform(0, 0, zOffset);
        scene.RootNode.Add(groupNode);


        var positions = CreateSnakePositions(new Vector3(-100, 0, 0), 50, 20, 80);
        var polyLineNode = new PolyLineNode()
        {
            Positions = positions,
            LineColor = Colors.Black,
            LineThickness = 15,
            MiterLimit = miterLimit
        };

        groupNode.Add(polyLineNode);


        var textNode2 = textBlockFactory.CreateTextBlock($"MiterLimit = {miterLimit}", new Vector3(-120, 0, 0), positionType: PositionTypes.Right);
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