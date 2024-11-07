using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.RenderingSteps;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

public class SuperSamplingSample : CommonSample
{
    public override string Title => "Super-sampling (SSAA) and Multi-sample (MSAA) anti-aliasing";

    private GroupNode? _groupNode;

    public SuperSamplingSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        sceneView.PreferredMultiSampleCount = 0;
        sceneView.PreferredSupersamplingCount = 4;

        base.OnSceneViewInitialized(sceneView);
    }

    protected override void OnCreateScene(Scene scene)
    {
        _groupNode = new GroupNode("GroupNode");
        scene.RootNode.Add(_groupNode);

        AddLinesFan(_groupNode, startPosition: new Vector3(-210, -5, 0), lineThickness: 0.6f, linesLength: 100);
        AddLinesFan(_groupNode, startPosition: new Vector3(-50, -5, 0), lineThickness: 0.8f, linesLength: 100);
    }

    private void AddLinesFan(GroupNode parentNode, Vector3 startPosition, float lineThickness, float linesLength)
    {
        var linePositions = new List<Vector3>();

        for (int a = 0; a <= 90; a += 5)
        {
            Vector3 endPosition = startPosition + new Vector3(linesLength * MathF.Cos(a / 180.0f * MathF.PI), linesLength * MathF.Sin(a / 180.0f * MathF.PI), 0);

            linePositions.Add(startPosition);
            linePositions.Add(endPosition);
        }

        var multiLineVisual3D = new MultiLineNode()
        {
            Positions     = linePositions.ToArray(),
            LineColor     = Color4.Black,
            LineThickness = lineThickness
        };

        parentNode.Add(multiLineVisual3D);
    }
}