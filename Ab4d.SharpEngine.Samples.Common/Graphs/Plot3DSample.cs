using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.Graphs;

public class Plot3DSample : CommonSample
{
    public override string Title => "3D Plot";

    private int _arraySize = 80;
    
    private HeightMapSurfaceNode? _heightMapSurfaceNode;
    private HeightMapContoursNode? _heightMapContoursNode;
    private HeightMapContoursNode? _bottomContoursNode;
    private HeightMapWireframeNode? _heightMapWireframeNode;

    private AxesBoxNode? _axesBoxNode;

    private int _selectedFunctionIndex;

    private enum HeightMapLinesTypes
    {
        None,
        ContourLines,
        Wireframe,
    }
    
    private HeightMapLinesTypes _selectedHeightMapLines = HeightMapLinesTypes.ContourLines;


    public Plot3DSample(ICommonSamplesContext context)
        : base(context)
    {

    }

    protected override async Task OnCreateSceneAsync(Scene scene)
    {
        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 35;
            targetPositionCamera.Attitude = -20;
            targetPositionCamera.Distance = 350;

            // Adjust the camera so the 3D scene is moved to the left (see Cameras/OffCenterCameraSample for more info)
            targetPositionCamera.TargetPosition = new Vector3(30, 0, 0);
            targetPositionCamera.RotationCenterPosition = new Vector3(0, 0, 0);
        }


        var textBlockFactory = await context.GetTextBlockFactoryAsync();

        var gradientStops = new GradientStop[]
        {
            new GradientStop(Colors.Red, 1.0f),
            new GradientStop(Colors.Yellow, 0.75f),
            new GradientStop(Colors.LightGreen, 0.5f),
            new GradientStop(Colors.Aqua, 0.25f),
            new GradientStop(Colors.Blue, 0.0f)
        };


        StandardMaterial? gradientMaterial;

        if (scene.GpuDevice != null)
        {
            var gradientGpuImage = TextureFactory.CreateGradientTexture(scene.GpuDevice, gradientStops, textureSize: 256);
            gradientMaterial = new StandardMaterial(gradientGpuImage, CommonSamplerTypes.Clamp, name: $"GradientTexture");
        }
        else
        {
            gradientMaterial = StandardMaterials.Gray; 
        }

        var data = GetFunctionData(selectedFunctionIndex: 0);
        
        _heightMapSurfaceNode = new HeightMapSurfaceNode(centerPosition: new Vector3(0, 10, 0),
                                                         size: new Vector3(100, 20, 100),
                                                         heightData: data,
                                                         name: "HeightMapSurface")
        {
            Material = gradientMaterial,
            BackMaterial = StandardMaterials.Gray,
            UseHeightValuesAsTextureCoordinates = true,
        };
        
        scene.RootNode.Add(_heightMapSurfaceNode);



        // Create height map contours, and tie its properties to the height map surface
        // Set all available parameters in the constructor, because changing those values later will call UpdateMesh on each change.
        _heightMapContoursNode = new HeightMapContoursNode(_heightMapSurfaceNode,
                                                           numContourLines: 14,
                                                           majorLinesFrequency: 0,
                                                           verticalOffset: 0.05f, // lift the grid slightly on top of the HeightMap
                                                           combineContourLines: true, // all contour lines will have the same color and thickness, so we can draw them with one line
                                                           name: "HeightMapContoursNode");

        scene.RootNode.Add(_heightMapContoursNode);


        _bottomContoursNode = new HeightMapContoursNode(_heightMapSurfaceNode,
                                                        numContourLines: 14,
                                                        majorLinesFrequency: 0,
                                                        combineContourLines: false, // Each contour line will have its own color so we must not combine them
                                                        name: "HeightMapContoursNode")
        {
            // Setting LineThickness and LineColor does not call UpdateMesh
            MinorLineThickness = 1f,
            MajorLineThickness = 2f,
        };

        // Color each contour line based on its height. This will create a gradient colored contour lines on the bottom of the graph.
        _bottomContoursNode.GetCustomLineColorCallback = contourHeight => TextureFactory.GetGradientColor(contourHeight, gradientStops);

        // Flatten all the lines by setting the same Y value (-40) for all positions in the contour lines.
        // This will show colored contour lines on the bottom of the graph.
        _bottomContoursNode.FlattenContours(fixedHeightValue: -40);

        scene.RootNode.Add(_bottomContoursNode);
        
        
        // Create height map wireframe, and tie its properties to the height map surface.
        // Set all available parameters in the constructor, because changing those values later will call UpdateMesh on each change.
        _heightMapWireframeNode = new HeightMapWireframeNode(_heightMapSurfaceNode, 
                                                             verticalLineFrequency: 5,
                                                             horizontalLineFrequency: 5,
                                                             wireframeOffset: 0.05f, // lift the grid slightly on top of the HeightMap
                                                             name: "HeightMapWireframe")
        {
            // Changing LineColor and Visibility will not call UpdateMesh
            LineColor = Colors.Black,
            Visibility = SceneNodeVisibility.Hidden,
        };
        
        scene.RootNode.Add(_heightMapWireframeNode);
        
        
        _axesBoxNode = new AxesBoxNode(textBlockFactory.BitmapTextCreator)
        {
            CenterPosition = new Vector3(0, 50, 0),
            Size = new Vector3(100, 80, 100),

            // Show only Z axis
            IsXAxis1Visible=false, IsXAxis2Visible=false,
            IsYAxis1Visible=false, IsYAxis2Visible=false,
            IsZAxis1Visible=true, IsZAxis2Visible=true,
            
            IsWireBoxFullyClosed = true,
            Camera = targetPositionCamera, // It is important to set the camera for correct axis labels and back lines
        };
        
        _axesBoxNode.SetAxisDataRange(AxesBoxNode.AxisTypes.XAxis, minimumValue: 0, maximumValue: _arraySize, majorTicksStep: 1, minorTicksStep: 0, snapMaximumValueToMajorTicks: false);
        _axesBoxNode.SetAxisDataRange(AxesBoxNode.AxisTypes.YAxis, minimumValue: 0, maximumValue: _arraySize, majorTicksStep: 1, minorTicksStep: 0, snapMaximumValueToMajorTicks: false);
        
        scene.RootNode.Add(_axesBoxNode);
        
        UpdateAll(selectedFunctionIndex: 0);
    }
    
    private float[,] GetFunctionData(int selectedFunctionIndex)
    {
        float[,] data;
        float minYValue, maxYValue;

        switch (selectedFunctionIndex)
        {
            case 0:
                data = CreateGraphData1(_arraySize, _arraySize, out minYValue, out maxYValue);
                break;

            case 1:
                data = CreateGraphData2(_arraySize, _arraySize, out minYValue, out maxYValue);
                break;

            case 2:
                data = CreateGraphData3(_arraySize, _arraySize, out minYValue, out maxYValue);
                break;

            default:
                throw new ArgumentException($"Invalid selectedFunctionIndex: {selectedFunctionIndex}!", nameof(selectedFunctionIndex));                
        }

        return data;
    }

    private void UpdateAll(int selectedFunctionIndex)
    {
        if (_heightMapSurfaceNode != null)
            _heightMapSurfaceNode.HeightData = GetFunctionData(selectedFunctionIndex);

        _selectedFunctionIndex = selectedFunctionIndex;
    }

    private void UpdateHeightMapLines()
    {
        if (_heightMapContoursNode != null)
            _heightMapContoursNode.Visibility = _selectedHeightMapLines == HeightMapLinesTypes.ContourLines ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
        
        if (_heightMapWireframeNode != null)
            _heightMapWireframeNode.Visibility = _selectedHeightMapLines == HeightMapLinesTypes.Wireframe ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
    }

    #region Math functions

    // cos(x*z)*(x^2-z^2)/2
    private static float[,] CreateGraphData1(int arrayWidth, int arrayHeight, out float minYValue, out float maxYValue)
    {
        float[,] data = new float[arrayWidth, arrayHeight];


        float xMin = -2;
        float xMax = 2;
        float zMin = -2;
        float zMax = 2;

        float xStep = (xMax - xMin) / arrayWidth;
        float zStep = (zMax - zMin) / arrayHeight;

        float xValue;
        float zValue = zMin;


        float yValue;
        minYValue = float.MaxValue;
        maxYValue = float.MinValue;


        for (int z = 0; z < arrayHeight; z++)
        {
            xValue = xMin;

            for (int x = 0; x < arrayWidth; x++)
            {
                // cos(x*y)*(x^2-y^2)
                yValue = MathF.Cos(xValue * zValue) * (xValue * xValue - zValue * zValue) / 2;

                data[x, z] = yValue;

                if (yValue > maxYValue)
                    maxYValue = yValue;

                if (yValue < minYValue)
                    minYValue = yValue;

                xValue += xStep;
            }

            zValue += zStep;
        }

        return data;
    }

    // (x * z^3 - z * x^3) * 4
    private static float[,] CreateGraphData2(int arrayWidth, int arrayHeight, out float minYValue, out float maxYValue)
    {
        float[,] data = new float[arrayWidth, arrayHeight];


        float xMin = -1;
        float xMax = 1;
        float zMin = -1;
        float zMax = 1;

        float xStep = (xMax - xMin) / arrayWidth;
        float zStep = (zMax - zMin) / arrayHeight;

        float xValue;
        float zValue = zMin;


        float yValue;
        minYValue = float.MaxValue;
        maxYValue = float.MinValue;


        for (int z = 0; z < arrayHeight; z++)
        {
            xValue = xMin;

            for (int x = 0; x < arrayWidth; x++)    
            {
                yValue = (xValue * zValue * zValue * zValue - zValue * xValue * xValue * xValue) * 4;

                data[x, z] = yValue;

                if (yValue > maxYValue)
                    maxYValue = yValue;

                if (yValue < minYValue)
                    minYValue = yValue;

                xValue += xStep;
            }

            zValue += zStep;
        }

        return data;
    }

    // cos(abs(x)+abs(z))*(abs(x)+abs(z)) * 2
    private static float[,] CreateGraphData3(int arrayWidth, int arrayHeight, out float minYValue, out float maxYValue)
    {
        float[,] data = new float[arrayWidth, arrayHeight];


        float xMin = -1;
        float xMax = 1;
        float zMin = -1;
        float zMax = 1;

        float xStep = (xMax - xMin) / arrayWidth;
        float zStep = (zMax - zMin) / arrayHeight;

        float xValue;
        float zValue = zMin;


        float yValue;
        minYValue = float.MaxValue;
        maxYValue = float.MinValue;


        for (int z = 0; z < arrayHeight; z++)
        {
            xValue = xMin;

            for (int x = 0; x < arrayWidth; x++)
            {
                yValue = MathF.Cos(MathF.Abs(xValue) + MathF.Abs(zValue)) * (MathF.Abs(xValue) + MathF.Abs(zValue)) * 2;

                data[x, z] = yValue;

                if (yValue > maxYValue)
                    maxYValue = yValue;

                if (yValue < minYValue)
                    minYValue = yValue;

                xValue += xStep;
            }

            zValue += zStep;
        }

        return data;
    }
    #endregion

    /// <inheritdoc />
    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);
        
        ui.CreateCheckBox("Show AxesBoxNode", true, isChecked => _axesBoxNode!.Visibility = isChecked ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden);
        
        ui.CreateCheckBox("Show bottom contours", true, isChecked => _bottomContoursNode!.Visibility = isChecked ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden);
        
        ui.AddSeparator();
        
        
        ui.CreateRadioButtons(new string[] { "No value lines", "Contour lines", "Wireframe lines" },
            (selectedIndex, selectedText) => 
            {
                _selectedHeightMapLines = (HeightMapLinesTypes)selectedIndex;
                UpdateHeightMapLines();
            },
            selectedItemIndex: 1);
             
        ui.AddSeparator();
        
        
        ui.CreateLabel("Function:");
        
        ui.CreateRadioButtons(new string[]
            {
                "y(x,z) = cos(x*z) * (x*x - z*z)",
                "y(x,z) = x * z^3 - z * x^3",
                "y(x,z) = cos(|x|+|z|) * (|x|+|z|)",
            },
            (selectedIndex, selectedText) => 
            {
                UpdateAll(selectedFunctionIndex: selectedIndex);
            },
            selectedItemIndex: 0);   
        
        ui.AddSeparator();
        
        
        ui.CreateRadioButtons(new string[] { "40 x 40", "80 x 80", "160 x 160" },
            (selectedIndex, selectedText) => 
            {
                _arraySize = new int[] { 40, 80, 160 }[selectedIndex];
                UpdateAll(_selectedFunctionIndex);
            },
            selectedItemIndex: 1);           
    }
}