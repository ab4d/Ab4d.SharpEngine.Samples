using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.Lines;

public class TubeLinesSample : CommonSample
{
    public override string Title => "TubeLines";
    public override string Subtitle => "Tube lines are 3D lines that are defined by a fixed mesh (triangles) and are not generated on the graphics card as standard 3D lines.\nAnother difference is that radius is defined in world coordinates and not screen coordinates as with standard lines. This means that the line thickness appears smaller as the camera is farther away.";

    // _segmentsCount defines how many segments each tube mesh has.
    private int _segmentsCount = 6;

    private bool _isSolidColorMaterial = true;

    // For another sample that uses tube lines see also AdvancedModels/StreamlinesSample

    public TubeLinesSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        RecreateScene();
        
        if (targetPositionCamera != null)
        {
            targetPositionCamera.TargetPosition = new Vector3(10, -20, 0);
            targetPositionCamera.Heading = 30;
            targetPositionCamera.Attitude = -20;
            targetPositionCamera.Distance = 700;
        }
    }

    private void RecreateScene()
    {
        if (Scene == null)
            return;

        Scene.RootNode.Clear();


        
        var orangeLineMaterial = CreateLineMaterial(Colors.Orange);

        // Add tube lines with different radiuses
        var radiuses = new float[] { 0.1f, 0.25f, 0.5f, 1, 2, 3 };
        var startPosition = new Vector3(-200, 0, 240);
        var lineVector = new Vector3(100, 0, 0);

        for (int i = 0; i < radiuses.Length; i++)
        {
            var tubeLineNode = new TubeLineModelNode(startPosition: startPosition, 
                                                     endPosition: startPosition + lineVector,
                                                     radius: radiuses[i], 
                                                     segments: _segmentsCount, 
                                                     generateTextureCoordinates: false, 
                                                     isStartPositionClosed: true,
                                                     isEndPositionClosed: true, 
                                                     material: orangeLineMaterial);

            Scene.RootNode.Add(tubeLineNode);

            startPosition += new Vector3(0, 0, -20);
        }

        // 3 tube lines in a different direction
        Scene.RootNode.Add(new TubeLineModelNode(startPosition: new Vector3(-200,0,100), endPosition: new Vector3(-200,0,0), radius: 2f, segments: _segmentsCount, generateTextureCoordinates: false, isStartPositionClosed: true, isEndPositionClosed: true, material: orangeLineMaterial));
        Scene.RootNode.Add(new TubeLineModelNode(startPosition: new Vector3(-150,0,100), endPosition: new Vector3(-150,0,0), radius: 2f, segments: _segmentsCount, generateTextureCoordinates: false, isStartPositionClosed: true, isEndPositionClosed: true, material: orangeLineMaterial));
        Scene.RootNode.Add(new TubeLineModelNode(startPosition: new Vector3(-100,0,100), endPosition: new Vector3(-100,0,0), radius: 2f, segments: _segmentsCount, generateTextureCoordinates: false, isStartPositionClosed: true, isEndPositionClosed: true, material: orangeLineMaterial));

        // Add for tube lines to create a rectangle
        //Scene.RootNode.Add(new TubeLineModelNode(startPosition: new Vector3(-200,0,-150), endPosition: new Vector3(-100,0,-150), radius: 2f, segments: _segmentsCount, generateTextureCoordinates: false, isStartPositionClosed: true, isEndPositionClosed: true, material: orangeEmissiveMaterial));
        //Scene.RootNode.Add(new TubeLineModelNode(startPosition: new Vector3(-100,0,-150), endPosition: new Vector3(-100,0,-50),  radius: 2f, segments: _segmentsCount, generateTextureCoordinates: false, isStartPositionClosed: true, isEndPositionClosed: true, material: orangeEmissiveMaterial));
        //Scene.RootNode.Add(new TubeLineModelNode(startPosition: new Vector3(-100,0,-50),  endPosition: new Vector3(-200,0,-50),  radius: 2f, segments: _segmentsCount, generateTextureCoordinates: false, isStartPositionClosed: true, isEndPositionClosed: true, material: orangeEmissiveMaterial));
        //Scene.RootNode.Add(new TubeLineModelNode(startPosition: new Vector3(-200,0,-50),  endPosition: new Vector3(-200,0,-150), radius: 2f, segments: _segmentsCount, generateTextureCoordinates: false, isStartPositionClosed: true, isEndPositionClosed: true, material: orangeEmissiveMaterial));

        var rectanglePositions = new Vector3[]
        {
            new Vector3(-200,0,-150),
            new Vector3(-100,0,-150),
            new Vector3(-100,0,-50),
            new Vector3(-200,0,-50),
        };

        var rectangleTubePathMesh = MeshFactory.CreateTubeMeshAlongPath(pathPositions: rectanglePositions,
                                                                        pathPositionTextureCoordinates: null,
                                                                        radius: 2f,
                                                                        isTubeClosed: true,
                                                                        isPathClosed: true,
                                                                        segments: _segmentsCount,
                                                                        generateTextureCoordinates: false);

        var rectangleModelNode = new MeshModelNode(rectangleTubePathMesh, orangeLineMaterial);

        Scene.RootNode.Add(rectangleModelNode);


        // Create curve through those points
        var controlPoints = new Vector3[]
        {
            new Vector3(-50, 0, 20),
            new Vector3(100, 0, 0),
            new Vector3(100, 0, 200),
            new Vector3(150, 0, 200),
            new Vector3(150, 0, -50),
            new Vector3(-50, 0, -30),
        };

        var curvePositions = BezierCurve.CreateBezierCurvePositionsThroughPoints(controlPoints, positionsPerSegment: 30, curveScale: 0.25f);

        var tubePathMesh = MeshFactory.CreateTubeMeshAlongPath(pathPositions: curvePositions,
                                                               pathPositionTextureCoordinates: null,
                                                               radius: 1f,
                                                               isTubeClosed: true,
                                                               isPathClosed: false,
                                                               segments: _segmentsCount,
                                                               generateTextureCoordinates: false);

        // Use EmissiveColor so that the tube lines are always rendered with the same color regardless of the lights.
        var greenLineMaterial = CreateLineMaterial(Colors.Green);

        var meshModelNode = new MeshModelNode(tubePathMesh, greenLineMaterial);

        Scene.RootNode.Add(meshModelNode);



        // Use EmissiveColor so that the tube lines are always rendered with the same color regardless of the lights.
        var arrowLineMaterial = CreateLineMaterial(Colors.Yellow);

        for (int i = 0; i < 5; i++)
        {
            var p1 = new Vector3(-50, 0, 220 - i * 30);
            var p2 = p1 + new Vector3(100, 0, 0);

            var radius = 0.5f + i * 0.5f;
            
            var arrowModelNode = new ArrowModelNode()
            {
                StartPosition = p1,
                EndPosition = p2,
                Radius = radius,
                ArrowAngle = 20,
                Segments = _segmentsCount,
                Material = arrowLineMaterial,
                MaxArrowLength = 0.33f, // maximum arrow length is 33% of the line
                GenerateTextureCoordinates = false
            };

            Scene.RootNode.Add(arrowModelNode);
        }
    }

    private Material CreateLineMaterial(Color4 lineColor)
    {
        if (_isSolidColorMaterial)
        {
            // Use EmissiveColor so that the tube lines are always rendered with the same color regardless of the lights.
            return new SolidColorMaterial(lineColor);
        }

        // When no emissive material is used, then tube lines will be shaded based on light positions
        return new StandardMaterial(lineColor);
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateSlider(minValue: 3, maxValue: 30, () => _segmentsCount, slicerValue =>
            {
                _segmentsCount = (int)slicerValue;
                RecreateScene();
            },
            width: 100, keyText: "SegmentsCount (?):defines how many segments each tube mesh has. Zoom into one line to see the difference when using low segments count", 
            keyTextWidth: 120, 
            formatShownValueFunc: sliderValue => sliderValue.ToString("F0"));

        ui.AddSeparator();

        ui.CreateCheckBox("Use SolidColorMaterial (?):When SolidColorMaterial is used, then the tube lines are always rendered with the same color regardless of the lights.",
            _isSolidColorMaterial,
            (isChecked) =>
            {
                _isSolidColorMaterial = isChecked;
                RecreateScene();
            });
    }
}