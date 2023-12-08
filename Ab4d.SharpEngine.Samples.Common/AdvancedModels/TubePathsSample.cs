using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class TubePathsSample : CommonSample
{
    public override string Title => "3D Tube paths";
    public override string Subtitle => "TubePathModelNode can be used to create a 3D tube along a path";

    public TubePathsSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        var pathPositions = CreateHelixPath(startCenter: new Vector3(0, 20, 70),
                                            radius: 30,
                                            height: 100,
                                            totalDegrees: 360 * 3,
                                            totalPathPositions: 100);


        var polyLine = new PolyLineNode()
        {
            Positions = pathPositions,
            LineThickness = 3,
            LineColor = Colors.Red,
            IsClosed = false
        };

        scene.RootNode.Add(polyLine);


        var outerTubePathNode = new TubePathModelNode()
        {
            PathPositions = pathPositions,
            Radius = 10,
            Segments = 10,
            IsTubeClosed = false,
            IsPathClosed = polyLine.IsClosed,
            GenerateTextureCoordinates = false,
            Material = StandardMaterials.LightGreen.SetOpacity(0.5f)
        };

        outerTubePathNode.BackMaterial = outerTubePathNode.Material;

        scene.RootNode.Add(outerTubePathNode);


        pathPositions = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(0, 10, 0),
            new Vector3(0, 20, 0),
            new Vector3(0, 30, 0),
            new Vector3(-20, 60, 0),
            new Vector3(-50, 100, 0),
            new Vector3(-120, 100, 0),
        };

        var openedTubePathNode = CreateOpenedTubePathNode(pathPositions: pathPositions,
                                                          outerRadius: 16,
                                                          innerRadius: 14,
                                                          segmentsCount: 20,
                                                          outerMaterial: StandardMaterials.Green,
                                                          innerMaterial: StandardMaterials.DimGray);

        openedTubePathNode.Transform = new TranslateTransform(50, 0, -70);

        scene.RootNode.Add(openedTubePathNode);



        var boxModelNode = new BoxModelNode(centerPosition: new Vector3(50, 0.5f, 0), size: new Vector3(200, 10, 300), material: StandardMaterials.LightGray);
        scene.RootNode.Add(boxModelNode);


        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 70;
            targetPositionCamera.Attitude = -20;
            targetPositionCamera.Distance = 500;
            targetPositionCamera.TargetPosition = new Vector3(0, 50, 0);
        }

        ShowCameraAxisPanel = true;
    }

    private static GroupNode CreateOpenedTubePathNode(Vector3[] pathPositions, float outerRadius, float innerRadius, int segmentsCount, Material outerMaterial, Material innerMaterial)
    {
        var rootGroupNode = new GroupNode();

        // CreateOpenedTubePath is created from 4 different models:
        // 1) Outer tube: TubePathModelNode with outer radius and Material set
        // 2) Inner tube: TubePathModelNode with inner radius and BackMaterial - this means that triangles will be visible from inside the tube
        // 3) start tube: TubeModelNode that will close the start of the tube - in the direction of the first path segment
        // 4) end tube: TubeModelNode that will close the end of the tube - in the direction of the last path segment

        // 1) Outer tube: TubePathModelNode with outer radius and Material set
        var outerTubePathNode = new TubePathModelNode()
        {
            PathPositions = pathPositions,
            Radius = outerRadius,
            Segments = segmentsCount,
            Material = outerMaterial,
            IsTubeClosed = false,
            IsPathClosed = false
        };

        rootGroupNode.Add(outerTubePathNode);

        // 2) Inner tube: TubePathModelNode with inner radius and BackMaterial - this means that triangles will be visible from inside the tube
        var innerTubePathNode = new TubePathModelNode()
        {
            PathPositions = pathPositions,
            Radius = innerRadius,
            Segments = segmentsCount,
            BackMaterial = innerMaterial,
            IsTubeClosed = false,
            IsPathClosed = false
        };

        rootGroupNode.Add(innerTubePathNode);

        // 3) start tube: TubeModelNode that will close the start of the tube - in the direction of the first path segment
        var startTubeNode = new TubeModelNode()
        {
            BottomCenterPosition = pathPositions[0],
            Height = 0.01f, // Current version (v7.4) does not allow to have 0 height - this will be improved in the next version
            BottomOuterRadius = outerRadius,
            TopOuterRadius = outerRadius,
            BottomInnerRadius = innerRadius,
            TopInnerRadius = innerRadius,
            Segments = segmentsCount,
            HeightDirection = pathPositions[1] - pathPositions[0], // direction of the first path segment
            Material = outerMaterial
        };

        rootGroupNode.Add(startTubeNode);

        // 4) end tube: TubeModelNode that will close the end of the tube - in the direction of the last path segment
        var endTubeNode = new TubeModelNode()
        {
            BottomCenterPosition = pathPositions[^1],
            Height = 0.01f,  // Current version (v7.4) does not allow to have 0 height - this will be improved in the next version
            BottomOuterRadius = outerTubePathNode.Radius,
            TopOuterRadius = outerTubePathNode.Radius,
            BottomInnerRadius = innerTubePathNode.Radius,
            TopInnerRadius = innerTubePathNode.Radius,
            Segments = segmentsCount,
            HeightDirection = pathPositions[^1] - pathPositions[^2], // direction of the last path segment
            Material = outerMaterial
        };

        rootGroupNode.Add(endTubeNode);

        return rootGroupNode;
    }

    // See: https://en.wikipedia.org/wiki/Helix
    private static Vector3[] CreateHelixPath(Vector3 startCenter, float radius, float height, int totalDegrees, int totalPathPositions)
    {
        float onePositionAngleRad = ((float) totalDegrees / (float) totalPathPositions) * MathF.PI / 180.0f;

        var positions = new Vector3[totalPathPositions];

        Vector3 currentCenterPoint = startCenter;
        float currentAngleRad = 0;

        var oneStepDirection = new Vector3(0, 1, 0) * (height / (float)totalPathPositions);

        for (int i = 0; i < totalPathPositions; i++)
        {
            float x = currentCenterPoint.X + MathF.Sin(currentAngleRad) * radius;
            float z = currentCenterPoint.Z + MathF.Cos(currentAngleRad) * radius;

            positions[i] = new Vector3(x, currentCenterPoint.Y, z);

            currentAngleRad += onePositionAngleRad;
            currentCenterPoint += oneStepDirection;
        }

        return positions;
    }
}