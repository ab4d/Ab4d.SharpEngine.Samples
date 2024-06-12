using System.Diagnostics;
using Ab4d.SharpEngine.Common;
using System.Numerics;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Meshes;
using Ab4d.Vulkan;

namespace Ab4d.SharpEngine.Samples.Common.Lines;

public class LinesPerformanceSample : CommonSample
{
    public override string Title => "3D Lines performance";

    // This sample can be used to test the performance of rendering many 3D lines.
    // A general rule to improve performance is to reduce the number of LineNode objects and instead use add more lines to single multi-line positions.
    //
    // By default, SharpEngine uses Geometry Shader to generate the 3D lines.
    // This provides support for rendering polygon lines with miter limit and stippled lines.
    //
    // But it is also possible to use Vulkan line rasterizer (this is also used when geometry shader is not supported - on some older mobile devices).
    // This may provide slightly better performance when rendering lines, but does not support polygon lines.
    // To test Vulkan line rasterizer, it needs to be configured before the Scene is initialized.
    // This can be done by adding the following code to the Common/CommonWpfSamplePage.xaml.cs file in the Ab4d.SharpEngine.Samples.Wpf project
    // (or similar file in the WinUI, Avalonia or other platform project) in the constructor after the "MainSceneView.CreateOptions.EnableStandardValidation = true;" add the following:
    //
    //MainSceneView.CreateOptions.EnableVulkanLineRasterization = true;
    //MainSceneView.CreateOptions.EnableVulkanStippleLineRasterization = true;
    //MainSceneView.Scene.LineRasterizationMode = LineRasterizationModes.VulkanRectangular; // See https://www.ab4d.com/help/SharpEngine/html/T_Ab4d_SharpEngine_Common_LineRasterizationModes.htm
    //
    // There are multiple Vulkan LineRasterizationModes - see online help for more info:  https://www.ab4d.com/help/SharpEngine/html/T_Ab4d_SharpEngine_Common_LineRasterizationModes.htm

    // When trying to achieve the best possible performance, then it is recommended to use OverlayTexture as PresentationType.
    // This mode is currently (v1.0) available only of Ab4d.SharpEngine.Wpf library.
    // A disadvantage of this presentation type is but does not allow rendering any WPF controls over the 3D graphics.
    // The advantage is that SharpEngine does not need to wait until the 3D scene is rendered to compose the result with the UI platform.
    // To test performance of this PresentationType uncomment the following lines in the Common/CommonWpfSamplePage.xaml.cs file:
    //
    //MainSceneView.PresentationType = PresentationTypes.OverlayTexture;
    //MainSceneView.Margin = new Thickness(0, 0, 350, 0); // We need to add some right margin so the sample settings will be still visible


    private int _numLinesInSpiralSliderValue = 5;
    private int _numLinesInSpiral = 5000;
    private int _xSpiralCount = 5;
    private int _ySpiralCount = 5;
    private bool _isMeshReused = true;

    private enum SampleLineTypes
    {
        MultiLineNoLineStrip,
        MultiLineLineStrip,
        PolyLine,
    }

    private SampleLineTypes _sampleLineType = SampleLineTypes.MultiLineLineStrip;
    
    private bool _linesDirty;
    private int _regenerateLinesUpdatesCount;
    
    private Stopwatch _initializationTimeStopwatch = new Stopwatch();
    private double _lastInitializationTimeMs;
    
    private ICommonSampleUIElement? _lineSegmentsCountLabel;
    private ICommonSampleUIElement? _linesCountLabel;
    private ICommonSampleUIElement? _initializeTimeLabel;
    private ICommonSampleUIElement? _startStopCameraButton;

    public LinesPerformanceSample(ICommonSamplesContext context)
        : base(context)
    {
    }
    
    protected override void OnCreateScene(Scene scene)
    {
        RecreateLines();
        
        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 15;
            targetPositionCamera.Attitude = 7;
            targetPositionCamera.Distance = 800;
        }
    }

    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        sceneView.SceneUpdating += OnSceneUpdating;
        
        base.OnSceneViewInitialized(sceneView);
    }

    protected override void OnDisposed()
    {
        if (SceneView != null)
            SceneView.SceneUpdating -= OnSceneUpdating;

        base.OnDisposed();
    }

    private void OnSceneUpdating(object? sender, EventArgs args)
    {
        CheckToRegenerateLines();
    }

    private void RecreateLinesWithDelay()
    {
        _linesDirty = true;
        _regenerateLinesUpdatesCount = 20; // wait 20 times calling Updating (around 0.3 second)
    }
    
    private void CheckToRegenerateLines()
    {
        if (!_linesDirty)
            return;
        
        _regenerateLinesUpdatesCount --;
        
        if (_regenerateLinesUpdatesCount < 0)
        {
            RecreateLines();
        }
    }
    
    private void RecreateLines()
    {
        if (Scene == null)
            return;
        
        // Dispose existing lines with meshes
        Scene.RootNode.DisposeAllChildren(disposeMeshes: true, disposeMaterials: true);

        GC.Collect();
        GC.WaitForFullGCComplete();
        GC.Collect();
        
        
        _initializationTimeStopwatch.Restart();
        
        AddSpirals(Scene, _xSpiralCount, _ySpiralCount, _numLinesInSpiral, _sampleLineType, _isMeshReused);
        
        _initializationTimeStopwatch.Stop();
        _lastInitializationTimeMs = _initializationTimeStopwatch.Elapsed.TotalMilliseconds;
        
        _lineSegmentsCountLabel?.UpdateValue();
        _linesCountLabel?.UpdateValue();
        _initializeTimeLabel?.UpdateValue();
        
        _linesDirty = false;
    }
    
    private void AddSpirals(Scene scene, int xCount, int yCount, int spiralLength, SampleLineTypes sampleLineType, bool reuseMesh)
    {
        float circleRadius = 10;
        int spiralCircles = spiralLength / 20; // One circle in the spiral is created from 20 lines

        var spiralPositions = CreateSpiralPositions(startPosition: new Vector3(0, 0, 0),
                                                    circleXDirection: new Vector3(1, 0, 0),
                                                    circleYDirection: new Vector3(0, 1, 0),
                                                    oneSpiralCircleDirection: new Vector3(0, 0, -10),
                                                    circleRadius: circleRadius,
                                                    segmentsPerCircle: 20,
                                                    circles: spiralCircles);

        float xStart = -xCount * circleRadius * 3 / 2;
        float yStart = -yCount * circleRadius * 3 / 2;
        
        LineMaterial? lineMaterial; 
        PolyLineMaterial? polyLineMaterial;
        
        if (sampleLineType == SampleLineTypes.PolyLine)
        {
            polyLineMaterial = new PolyLineMaterial()
            {
                LineColor = Colors.OrangeRed,
                LineThickness = 2,
            };
            lineMaterial = null;
        }
        else
        {
            lineMaterial = new LineMaterial()
            {
                LineColor = Colors.OrangeRed,
                LineThickness = 2,
            };
            polyLineMaterial = null;
        }

        PositionsMesh? positionsMesh;
        if (reuseMesh)
        {
            // Using single PositionsMesh for all lines significantly improves performance.
            // This version of SharpEngine does not support creating a LineNode with providing a PositionsMesh.
            // Therefore there is a MeshLineNode class defined below. This will be improved in the next version.
            var positionsTopology = sampleLineType == SampleLineTypes.MultiLineNoLineStrip ? PrimitiveTopology.LineList : PrimitiveTopology.LineStrip;
            positionsMesh = new PositionsMesh(spiralPositions, positionsTopology, "SpiralPositionsMesh");
        }
        else
        {
            // When positionsMesh is null, then new PositionsMesh will be generated for each LineNode.
            // This will significantly increase initialization time and memory usage.
            positionsMesh = null;
        }

        
        for (int x = 0; x < xCount; x++)
        {
            for (int y = 0; y < yCount; y++)
            {
                var lineTransform = new TranslateTransform(x * circleRadius * 3 + xStart, y * circleRadius * 3 + yStart, 0);

                LineBaseNode lineNode;

                if (positionsMesh != null)
                {
                    Material material = sampleLineType == SampleLineTypes.PolyLine ? polyLineMaterial! : lineMaterial!;
                    lineNode = new MeshLineNode(positionsMesh, material);
                }
                else
                {
                    if (sampleLineType == SampleLineTypes.PolyLine)
                    {
                        lineNode = new PolyLineNode(spiralPositions, polyLineMaterial!);
                    }
                    else
                    {
                        bool isLineStrip = sampleLineType == SampleLineTypes.MultiLineLineStrip;
                        lineNode = new MultiLineNode(spiralPositions, isLineStrip, lineMaterial!);
                    }
                }

                lineNode.Transform = lineTransform;

                scene.RootNode.Add(lineNode);
            }
        }
    }
    
    private Vector3[] CreateSpiralPositions(Vector3 startPosition, Vector3 circleXDirection, Vector3 circleYDirection, Vector3 oneSpiralCircleDirection, float circleRadius, int segmentsPerCircle, int circles)
    {
        var oneCirclePositions = new Vector2[segmentsPerCircle];

        float angleStep = 2f * MathF.PI / segmentsPerCircle;
        float angle = 0;

        for (int i = 0; i < segmentsPerCircle; i++)
        {
            // Get x any y position on a flat plane
            (float xPos, float yPos) = MathF.SinCos(angle);

            angle += angleStep;

            var newPoint = new Vector2(xPos * circleRadius, yPos * circleRadius);
            oneCirclePositions[i] = newPoint;
        }


        var allPositions = new Vector3[segmentsPerCircle * circles];

        Vector3 onePositionDirection = oneSpiralCircleDirection / segmentsPerCircle;
        Vector3 currentCenterPoint = startPosition;

        int index = 0;
        for (int i = 0; i < circles; i++)
        {
            for (int j = 0; j < segmentsPerCircle; j++)
            {
                float xCircle = oneCirclePositions[j].X;
                float yCircle = oneCirclePositions[j].Y;

                var point3D = new Vector3(currentCenterPoint.X + (xCircle * circleXDirection.X) + (yCircle * circleYDirection.X),
                                          currentCenterPoint.Y + (xCircle * circleXDirection.Y) + (yCircle * circleYDirection.Y),
                                          currentCenterPoint.Z + (xCircle * circleXDirection.Z) + (yCircle * circleYDirection.Z));

                allPositions[index] = point3D;
                index++;

                currentCenterPoint += onePositionDirection;
            }
        }

        return allPositions;
    }
        
    private string GetLineSegmentsCountText()
    {
        int lineSegmentsCount = _xSpiralCount * _ySpiralCount * _numLinesInSpiral;
        
        string lineSegmentsDivisionText;
        if (_sampleLineType == SampleLineTypes.MultiLineNoLineStrip)
        {
            lineSegmentsCount /= 2;
            lineSegmentsDivisionText = " / 2";
        }
        else
        {
            lineSegmentsDivisionText = "";
        }
        
        return $"{_xSpiralCount} x {_ySpiralCount} x {_numLinesInSpiral:#,##0}{lineSegmentsDivisionText} = {lineSegmentsCount:#,##0}";
    }        
    
    private string GetLinesCountText()
    {
        return $"{_xSpiralCount} x {_ySpiralCount} = {(_xSpiralCount * _ySpiralCount):#,##0}";
    }
    
    private string GetInitializationTimeText()
    {
        return $"{_lastInitializationTimeMs:#,##0.00} ms";
    }
    
    private void StartStopCameraRotation()
    {
        if (targetPositionCamera == null || _startStopCameraButton == null)
            return;

        if (targetPositionCamera.IsRotating)
        {
            targetPositionCamera.StopRotation();
            _startStopCameraButton.SetText("Start camera rotation");
        }
        else
        {
            targetPositionCamera.StartRotation(30);
            _startStopCameraButton.SetText("Stop camera rotation");
        }
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);
        
        ui.CreateLabel("Lines count settings:", isHeader: true);
        
        ui.CreateSlider(0, 50, () => _numLinesInSpiralSliderValue, delegate (float newValue)
        {
            _numLinesInSpiralSliderValue = (int)newValue;
            _numLinesInSpiral = Math.Max(_numLinesInSpiralSliderValue * 500, 100);
            RecreateLinesWithDelay();
        }, 100, false, "No. lines in one spiral:", 120, sliderValue => _numLinesInSpiral.ToString("N0") + "  ");
        
        ui.CreateSlider(1, 50, () => _xSpiralCount, delegate (float newValue)
        {
            _xSpiralCount = (int)newValue;
            RecreateLinesWithDelay();
        }, 100, false, "X spirals count:", 120, sliderValue => sliderValue.ToString("F0"));
        
        ui.CreateSlider(1, 50, () => _ySpiralCount, delegate (float newValue)
        {
            _ySpiralCount = (int)newValue;
            RecreateLinesWithDelay();
        }, 100, false, "Y spirals count:", 120, sliderValue => sliderValue.ToString("F0"));
        
        ui.AddSeparator();

        ui.CreateRadioButtons(new string[]
        {
            "MultiLine (IsLineStrip: false)",
            "MultiLine (IsLineStrip: true)",
            "PolyLine",
        }, (itemIndex, itemText) =>
        {
            _sampleLineType = (SampleLineTypes)itemIndex;
            RecreateLines();
        }, selectedItemIndex: 1);

        ui.CreateCheckBox("Reuse mesh", _isMeshReused, isChecked =>
        {
            _isMeshReused = isChecked;
            RecreateLines();
        });

        ui.CreateLabel("Statistics:", isHeader: true);
        
        ui.CreateLabel("3D line segments count:");
        _lineSegmentsCountLabel = ui.CreateKeyValueLabel("", () => GetLineSegmentsCountText());

        ui.AddSeparator();
        
        ui.CreateLabel("Draw calls / line objects count:");
        _linesCountLabel = ui.CreateKeyValueLabel("", () => GetLinesCountText());

        ui.AddSeparator();
        
        _initializeTimeLabel = ui.CreateKeyValueLabel("Initialization time:", () => GetInitializationTimeText());
        ui.CreateLabel("Open Diagnostics to see performance");
        
        ui.AddSeparator();
        
        ui.CreateButton("Recreate lines", () => RecreateLines());

        _startStopCameraButton = ui.CreateButton("Start camera rotation", () => StartStopCameraRotation());
    }

    // This version of SharpEngine does not support creating a LineNode with providing a PositionsMesh.
    // This will be improved in the next version.
    public class MeshLineNode : LineBaseNode
    {
        public MeshLineNode(PositionsMesh mesh, Material lineMaterial, string? name = null)
            : base(lineMaterial, name)
        {
            this.SetMesh(mesh);
        }
    }
}