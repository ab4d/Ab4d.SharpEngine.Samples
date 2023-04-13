using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.Common.Utils;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class AdvancedHeightMapSample : CommonSample
{
    public override string Title => "Advanced HeightMap sample";

    private float[,]? _heightData;

    private HeightMapSurfaceNode? _heightMapSurfaceNode1;
    private HeightMapSurfaceNode? _heightMapSurfaceNode2;

    private StandardMaterial? _heigthMapMaterial1;
    private StandardMaterial? _heigthMapMaterial2;

    private HeightMapContoursNode? _heightMapContoursNode;
    private HeightMapWireframeNode? _heightMapWireframeNode;

    private GradientType _gradientType = GradientType.GeographicalHard;
    private LinesType _linesType = LinesType.CombinedContourLines;

    private bool _useTransparentColor;
    private bool _useHeightTexture = false;
    
    private AmbientLight? _ambientLight;
    private GradientStop[]? _gradientData;


    public enum GradientType
    {
        Technical,
        GeographicalSmooth,
        GeographicalHard,
    }
    
    public enum LinesType
    {
        None, 
        WireGrid,
        CombinedContourLines,
        IndividualContourLines,
        ColoredContourLines,
    }

    public AdvancedHeightMapSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        // Load height data from image
        // _heightData array should contain values from 0 to 1
        var heightImageData = BitmapIO.LoadBitmap("Resources/HeightMaps/simpleHeightMap.png");
        _heightData = HeightMapSurfaceNode.CreateHeightDataFromImageData(heightImageData);

        var graySpecularMaterial = StandardMaterials.Gray.SetSpecular(Color3.White, 16);

        // Create height map surface
        _heightMapSurfaceNode1 = new HeightMapSurfaceNode(centerPosition: new Vector3(0, 0, 0),
                                                          size: new Vector3(100, 20, 100),
                                                          heightData: _heightData,
                                                          useHeightValuesAsTextureCoordinates: false,
                                                          name: "HeightMapSurface")
        {
            Material = graySpecularMaterial,
            BackMaterial = graySpecularMaterial,
        };

        _heightMapSurfaceNode2 = new HeightMapSurfaceNode(centerPosition: new Vector3(0, 0, 0),
                                                          size: new Vector3(100, 20, 100),
                                                          heightData: _heightData,
                                                          useHeightValuesAsTextureCoordinates: true,
                                                          name: "HeightMapSurface")
        {
            Material     = graySpecularMaterial,
            BackMaterial = graySpecularMaterial,
        };

        UpdateTexture();


        // Create height map wireframe, and tie its properties to the height map surface
        _heightMapWireframeNode = new HeightMapWireframeNode(_heightMapSurfaceNode1, name: "HeightMapWireframe")
        {
            VerticalLineFrequency = 5,
            HorizontalLineFrequency = 5,
            LineColor = Colors.Black,
            LineThickness = 1,
            WireframeOffset = 0.05f, // lift the grid slightly on top of the HeightMap
            Visibility = SceneNodeVisibility.Hidden
        };

        var wireBoxNode = new WireBoxNode()
        {
            Position = _heightMapSurfaceNode1.CenterPosition,
            PositionType = PositionTypes.Center,
            Size = _heightMapSurfaceNode1.Size,
            LineColor = Colors.Silver,
            LineThickness = 2,
        };

        scene.RootNode.Add(_heightMapSurfaceNode1);
        scene.RootNode.Add(_heightMapSurfaceNode2);
        scene.RootNode.Add(_heightMapWireframeNode);
        scene.RootNode.Add(wireBoxNode);

        UpdateContourLines();


        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading   = -30;
            targetPositionCamera.Attitude  = -20;
            targetPositionCamera.Distance  = 200;
        }


        _ambientLight = new AmbientLight(0.2f);
        scene.Lights.Add(_ambientLight);
    }

    private void UpdateTexture()
    {
        if (GpuDevice == null || _heightData == null || _heightMapSurfaceNode1 == null || _heightMapSurfaceNode2 == null)
            return;

        // Create gradient
        _gradientData = CreateSampleGradient(_gradientType, _useTransparentColor);

        if (_useHeightTexture)
        {
            // Create 2D height data texture
            var texture2 = TextureFactory.CreateHeightTexture(GpuDevice, _heightData, _gradientData);

            if (_heigthMapMaterial1 == null)
            {
                _heigthMapMaterial1              = new StandardMaterial(texture2, CommonSamplerTypes.Clamp, $"2D height-data texture material ({_gradientType}, {_useTransparentColor})");
                _heightMapSurfaceNode1.Material = _heigthMapMaterial1;
            }
            else
            {
                _heigthMapMaterial1.DiffuseTexture = texture2;
            }

            _heightMapSurfaceNode1.Visibility = SceneNodeVisibility.Visible;
            _heightMapSurfaceNode2.Visibility = SceneNodeVisibility.Hidden;
        }
        else
        {
            // Create 1D texture; "linear" texture that requires special texture coordinates
            var texture1 = TextureFactory.CreateGradientTexture(GpuDevice, _gradientData);

            if (_heigthMapMaterial2 == null)
            {
                _heigthMapMaterial2              = new StandardMaterial(texture1, CommonSamplerTypes.Clamp, $"1D texture material ({_gradientType}, {_useTransparentColor})");
                _heightMapSurfaceNode2.Material = _heigthMapMaterial2;
            }
            else
            {
                _heigthMapMaterial2.DiffuseTexture = texture1;
            }

            _heightMapSurfaceNode1.Visibility = SceneNodeVisibility.Hidden;
            _heightMapSurfaceNode2.Visibility = SceneNodeVisibility.Visible;
        }
    }

    private void UpdateLines()
    {
        if (_heightMapWireframeNode == null || _heightMapContoursNode == null)
            return;

        switch (_linesType)
        {
            case LinesType.None:
                _heightMapWireframeNode.Visibility = SceneNodeVisibility.Hidden;
                _heightMapContoursNode.Visibility = SceneNodeVisibility.Hidden;
                break;
            
            case LinesType.WireGrid:
                _heightMapWireframeNode.Visibility = SceneNodeVisibility.Visible;
                _heightMapContoursNode.Visibility = SceneNodeVisibility.Hidden;
                break;
            
            case LinesType.IndividualContourLines:
            case LinesType.CombinedContourLines:
            case LinesType.ColoredContourLines:
                UpdateContourLines();

                _heightMapWireframeNode.Visibility = SceneNodeVisibility.Hidden;
                _heightMapContoursNode.Visibility = SceneNodeVisibility.Visible;
                break;
        }
    }

    private void UpdateContourLines()
    {
        if (Scene == null || _heightMapSurfaceNode1 == null || _gradientData == null)
            return;
        
        if (_heightMapContoursNode != null)
            Scene.RootNode.Remove(_heightMapContoursNode);


        // Create height map contours, and tie its properties to the height map surface
        _heightMapContoursNode = new HeightMapContoursNode(_heightMapSurfaceNode1, name: "Contours")
        {
            NumContourLines = 20,
            MajorLinesFrequency = 5,
            MinorLineThickness = 1f,
            MajorLineThickness = 2f,
            VerticalOffset = 0.05f, // lift the grid slightly on top of the HeightMap
            CombineContourLines = _linesType == LinesType.CombinedContourLines
        };

        if (_linesType == LinesType.ColoredContourLines)
        {
            var contourLineHeights = _heightMapContoursNode.GetAvailableLineHeights();

            if (contourLineHeights != null)
            {
                if (float.IsNaN(_heightMapSurfaceNode1.MinTextureHeight))
                    _heightMapSurfaceNode1.Update();

                float minHeight = 0;
                float heightRange = 1;

                for (var i = 0; i < contourLineHeights.Count; i++)
                {
                    var contourLineHeightValue = contourLineHeights[i];
                    var heightPercent = (contourLineHeightValue - minHeight) / heightRange;

                    var gradientColor = TextureFactory.GetGradientColor(heightPercent, _gradientData);

                    var multiLineNode = _heightMapContoursNode.GetLineNode(contourLineHeightValue);
                    
                    if (multiLineNode != null)
                        multiLineNode.LineColor = gradientColor;
                }
            }
        }


        Scene.RootNode.Add(_heightMapContoursNode);
    }

    public static GradientStop[] CreateSampleGradient(GradientType type, bool addTransparentColor)
    {
        GradientStop[] stops;
        var index = 0;

        switch (type)
        {
            case GradientType.Technical:
                {
                    stops = new GradientStop[addTransparentColor ? 7 : 5];

                    stops[index++] = new GradientStop(Colors.Red, 1.0f);
                    stops[index++] = new GradientStop(Colors.Yellow, 0.75f);
                    stops[index++] = new GradientStop(Colors.LightGreen, 0.5f);
                    stops[index++] = new GradientStop(Colors.Aqua, 0.25f);

                    if (addTransparentColor)
                    {
                        // All values below 0.01 will be transparent
                        stops[index++] = new GradientStop(Colors.Blue, 0.01f);
                        stops[index++] = new GradientStop(Colors.Blue, 0.009f);
                        stops[index] = new GradientStop(Color4.Transparent, 0.0f);
                    }
                    else
                    {
                        stops[index] = new GradientStop(Colors.Blue, 0.0f);
                    }
                    break;
                }
            case GradientType.GeographicalSmooth:
                {
                    stops = new GradientStop[addTransparentColor ? 8 : 6];

                    stops[index++] = new GradientStop(Colors.White, 1.0f);
                    stops[index++] = new GradientStop(Colors.Gray, 0.8f);
                    stops[index++] = new GradientStop(Colors.SandyBrown, 0.6f);
                    stops[index++] = new GradientStop(Colors.LightGreen, 0.4f);
                    stops[index++] = new GradientStop(Colors.Aqua, 0.2f);

                    if (addTransparentColor)
                    {
                        // All values below 0.01 will be transparent
                        stops[index++] = new GradientStop(Colors.Blue, 0.01f);
                        stops[index++] = new GradientStop(Color4.Transparent, 0.009f);
                        stops[index] = new GradientStop(Color4.Transparent, 0.0f);
                    }
                    else
                    {
                        stops[index] = new GradientStop(Colors.Blue, 0.0f);
                    }
                    break;
                }
            case GradientType.GeographicalHard:
                {
                    stops = new GradientStop[addTransparentColor ? 12 : 10];

                    // The gradient with hard transition is defined by making the transition from one color to another very small (for example from 0.799 to 0.8)
                    stops[index++] = new GradientStop(Colors.White, 1.0f);
                    stops[index++] = new GradientStop(Colors.White, 0.8f);
                    stops[index++] = new GradientStop(Colors.SandyBrown, 0.799f);
                    stops[index++] = new GradientStop(Colors.SandyBrown, 0.6f);
                    stops[index++] = new GradientStop(Colors.LightGreen, 0.599f);
                    stops[index++] = new GradientStop(Colors.LightGreen, 0.400f);
                    stops[index++] = new GradientStop(Colors.Aqua, 0.399f);
                    stops[index++] = new GradientStop(Colors.Aqua, 0.2f);
                    stops[index++] = new GradientStop(Colors.Blue, 0.199f);

                    if (addTransparentColor)
                    {
                        // All values below 0.01 will be transparent
                        stops[index++] = new GradientStop(Colors.Blue, 0.01f);
                        stops[index++] = new GradientStop(Color4.Transparent, 0.009f);
                        stops[index] = new GradientStop(Color4.Transparent, 0.0f);
                    }
                    else
                    {
                        stops[index] = new GradientStop(Colors.Blue, 0.0f);
                    }
                    break;
                }
            default:
                {
                    throw new ArgumentException($"Invalid gradient type: {type}!", nameof(type));
                }
        }

        return stops;
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        
        ui.CreateLabel("Gradient:", isHeader: true);
        ui.CreateRadioButtons(new string[] { "Technical", "GeographicalSmooth", "GeographicalHard" }, (itemIndex, itemText) =>
        {
            _gradientType = (GradientType)itemIndex;
            UpdateTexture();
        }, 2); 

        ui.CreateCheckBox("Transparent colors", false, isChecked =>
        {
            _useTransparentColor = isChecked;
            UpdateTexture();
        });


        ui.AddSeparator();

        ui.CreateCheckBox("Use TextureCoordinates as height values", false, isChecked =>
        {
            _useHeightTexture = isChecked;
            UpdateTexture();
        });


        ui.CreateLabel("Lines:", isHeader: true);

        ui.CreateRadioButtons(new string[]
        {
            "None", 
            "WireGrid",
            "Combined contours lines (?):Combined contour lines show all contour lines with two MultiLineNodes: one for major lines and one for minor lines. This is the best for performance, but contour lines cannot be colored by height.",
            "Individual contours lines (?):Individual contour lines create one MultiLineNode for each contour line.",
            //"Colored contours lines (?): Colored contour lines show how to color each individual contour line." // TODO
        }, (itemIndex, itemText) =>
        {
            _linesType = (LinesType)itemIndex;
            UpdateLines();
        }, 2);

        ui.CreateLabel("View:", isHeader: true);

        ui.CreateSlider(0, 1, () => 0.2f, ambientValue => _ambientLight?.SetIntensity(ambientValue), width: 150, 
            keyText: "Ambient light:", 
            formatShownValueFunc: ambientValue => $"{ambientValue * 100:F0}%");

        //ui.CreateLabel("Show:", isHeader: true);
    }
}