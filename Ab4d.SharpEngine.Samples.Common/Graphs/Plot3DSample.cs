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
    private HeightMapWireframeNode? _heightMapWireframeNode;

    //private HeightMapContoursNode? _bottomContoursNode;
    private GroupNode? _bottomContoursNode;

    private AxesBoxNode? _axesBoxNode;

    private int _selectedFunctionIndex;
    private bool _showBottomContours = true;
    
    private GradientStop[] _gradientStops;

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
        _gradientStops = new GradientStop[]
        {
            new GradientStop(Colors.Red, 1.0f),
            new GradientStop(Colors.Yellow, 0.75f),
            new GradientStop(Colors.LightGreen, 0.5f),
            new GradientStop(Colors.Aqua, 0.25f),
            new GradientStop(Colors.Blue, 0.0f)
        };
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

        StandardMaterial? gradientMaterial;

        if (scene.GpuDevice != null)
        {
            var gradientGpuImage = TextureFactory.CreateGradientTexture(scene.GpuDevice, _gradientStops, textureSize: 256);
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
                                                           combineContourLines: false, // even though all the contour lines are shown with the same color, we must not combine them, because we will use the data from individual lines to create the contour lines at the bottom of the graph
                                                           name: "HeightMapContoursNode");

        scene.RootNode.Add(_heightMapContoursNode);


        // The following commented code shows how to create flatten and colored contour lines
        // creating a new HeightMapContoursNode object. This is easier to be done, but it not as efficient as using one HeightMapContoursNode 
        // and using its calculated contour lines for the lines on the graph and at bottom of the graph.

        //_bottomContoursNode = new HeightMapContoursNode(_heightMapSurfaceNode,
        //                                                numContourLines: 14,
        //                                                majorLinesFrequency: 0,
        //                                                combineContourLines: false, // Each contour line will have its own color so we must not combine them
        //                                                name: "HeightMapContoursNode")
        //{
        //    // Setting LineThickness and LineColor does not call UpdateMesh
        //    MinorLineThickness = 1f,
        //    MajorLineThickness = 2f,
        //};

        //// Color each contour line based on its height. This will create a gradient colored contour lines on the bottom of the graph.
        //_bottomContoursNode.GetCustomLineColorCallback = contourHeight => TextureFactory.GetGradientColor(contourHeight, _gradientStops);

        //// Flatten all the lines by setting the same Y value (-40) for all positions in the contour lines.
        //// This will show colored contour lines on the bottom of the graph.
        //_bottomContoursNode.FlattenContours(fixedHeightValue: -40);


        // Instead of new HeightMapContoursNode we will group the lines in a GroupNode

        _bottomContoursNode = new GroupNode("BottomContoursNode");

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
            Visibility = SceneNodeVisibility.Hidden, // Initially hidden
        };
        
        scene.RootNode.Add(_heightMapWireframeNode);
        
        
        _axesBoxNode = new AxesBoxNode(textBlockFactory.BitmapTextCreator)
        {
            CenterPosition = new Vector3(0, 0, 0),
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
        _axesBoxNode.SetAxisDataRange(AxesBoxNode.AxisTypes.ZAxis, minimumValue: -2, maximumValue: 2, majorTicksStep: 1, minorTicksStep: 0, snapMaximumValueToMajorTicks: false);
        
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

        UpdateBottomContourLines();

        _selectedFunctionIndex = selectedFunctionIndex;
    }

    private void UpdateHeightMapLines()
    {
        if (_heightMapContoursNode != null)
            _heightMapContoursNode.Visibility = _selectedHeightMapLines == HeightMapLinesTypes.ContourLines ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
        
        if (_heightMapWireframeNode != null)
            _heightMapWireframeNode.Visibility = _selectedHeightMapLines == HeightMapLinesTypes.Wireframe ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
    }

    private void UpdateBottomContourLines()
    {
        if (_bottomContoursNode != null)
        {
            _bottomContoursNode.Visibility = _showBottomContours && _selectedHeightMapLines == HeightMapLinesTypes.ContourLines ? SceneNodeVisibility.Visible
                                                                                                                                : SceneNodeVisibility.Hidden;
        }


        if (_selectedHeightMapLines != HeightMapLinesTypes.ContourLines || _heightMapContoursNode == null || _bottomContoursNode == null)
            return;

        _bottomContoursNode.Clear();

        var allContourLineHeights = _heightMapContoursNode.GetAvailableLineHeights();

        if (allContourLineHeights == null)
        {
            _heightMapContoursNode.Update();
            allContourLineHeights = _heightMapContoursNode.GetAvailableLineHeights();
        }

        if (allContourLineHeights != null)
        {
            var bottomYPosition = -40;

            foreach (var contourLineHeight in allContourLineHeights)
            {
                var multiLineNode = _heightMapContoursNode.GetLineNode(contourLineHeight);
                var positions = multiLineNode?.Positions;

                if (positions != null)
                {
                    // Copy positions but set y value to bottomYPosition for all positions
                    var bottomContourLinePositions = new Vector3[positions.Length];
                    for (var i = 0; i < positions.Length; i++)
                        bottomContourLinePositions[i] = new Vector3(positions[i].X, bottomYPosition, positions[i].Z);

                    // Get color based on the height
                    var color = TextureFactory.GetGradientColor(contourLineHeight, _gradientStops);

                    // Create a new line
                    var lineNode = new MultiLineNode(bottomContourLinePositions, isLineStrip: false, lineColor: color, lineThickness: 1.5f);
                    _bottomContoursNode.Add(lineNode);
                }
            }
        }
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
        
        
        ui.CreateRadioButtons(new string[] { "No value lines", "Contour lines", "Wireframe lines" },
            (selectedIndex, selectedText) => 
            {
                _selectedHeightMapLines = (HeightMapLinesTypes)selectedIndex;
                UpdateHeightMapLines();
                UpdateBottomContourLines();
            },
            selectedItemIndex: 1);
             
        ui.AddSeparator();
        
        ui.CreateLabel("Data array size:");
        
        ui.CreateRadioButtons(new string[] { "40 x 40", "80 x 80", "160 x 160" },
            (selectedIndex, selectedText) => 
            {
                _arraySize = new int[] { 40, 80, 160 }[selectedIndex];
                UpdateAll(_selectedFunctionIndex);
            },
            selectedItemIndex: 1);      
        
        ui.AddSeparator();
        

        ui.CreateCheckBox("Show AxesBoxNode", true, isChecked => _axesBoxNode!.Visibility = isChecked ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden);
        
        ui.CreateCheckBox("Show bottom contours", _showBottomContours, isChecked =>
        {
            _showBottomContours = isChecked;
            UpdateBottomContourLines();
        });
    }
}