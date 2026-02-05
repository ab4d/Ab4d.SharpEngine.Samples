using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class AdvancedHeightMapSample : CommonSample
{
    public override string Title => "Advanced HeightMap sample";

    private readonly Vector3 _heightMapCenterPosition = new Vector3(0, 0, 0);
    private readonly Vector3 _heightMapSize = new Vector3(100, 10, 100);

    private float[,]? _heightData;

    private HeightMapSurfaceNode? _standardHeightMapNode;
    private HeightMapSurfaceNode? _gradientHeightMapNode;

    private StandardMaterial? _standardHeightMapMaterial;
    private StandardMaterial? _gradientHeightMapMaterial;
    private StandardMaterial _graySpecularMaterial;

    private HeightMapContoursNode? _heightMapContoursNode;
    private HeightMapWireframeNode? _heightMapWireframeNode;

    private GradientType _gradientType = GradientType.GeographicalSmooth;
    private LinesType _linesType = LinesType.CombinedContourLines;

    private bool _useTransparentColor;
    private bool _useGradientTexture = true;
    
    private GradientStop[]? _gradientData;

    public enum GradientType
    {
        None,
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
        _graySpecularMaterial = StandardMaterials.Gray.SetSpecular(Color3.White, 16);
    }

    protected override async Task OnCreateSceneAsync(Scene scene)
    {
        var wireBoxNode = new WireBoxNode("HeightMapWireBoxNode")
        {
            Position = _heightMapCenterPosition,
            PositionType = PositionTypes.Center,
            Size = _heightMapSize,
            LineColor = Colors.Silver,
            LineThickness = 2,
        };

        scene.RootNode.Add(wireBoxNode);

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading   = -30;
            targetPositionCamera.Attitude  = -20;
            targetPositionCamera.Distance  = 200;

            // Show height map on the left side so it is not behind the options:
            targetPositionCamera.TargetPosition = new Vector3(20, 0, 0);
            targetPositionCamera.RotationCenterPosition = new Vector3(0, 0, 0);
        }


        scene.SetAmbientLight(0.2f);


        // Load height data from image
        // _heightData array should contain values from 0 to 1
        //var heightImageData = BitmapIO.LoadBitmap("Resources/HeightMaps/simpleHeightMap.png");

#if VULKAN
        var heightDateFileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources/HeightMaps/vulkan-heightmap-cropped.png");
        var heightImageData = BitmapIO.LoadBitmap(heightDateFileName);
#else
        string heightDateFileName = GetCommonTexturePath("Resources/HeightMaps/vulkan-heightmap-cropped.png");
        var heightImageData = await scene.GpuDevice.CanvasInterop.LoadImageBytesAsync(heightDateFileName);
#endif

        _heightData = HeightMapSurfaceNode.CreateHeightDataFromImageData(heightImageData);


        var heightMapSurfaceNode = EnsureHeightMapSurfaceNode();

        UpdateTexture();


        // Create height map wireframe, and tie its properties to the height map surface.
        // Set all available parameters in the constructor, because changing those values later will call UpdateMesh on each change.
        _heightMapWireframeNode = new HeightMapWireframeNode(heightMapSurfaceNode, 
                                                             verticalLineFrequency: 5,
                                                             horizontalLineFrequency: 5,
                                                             wireframeOffset: 0.05f, // lift the grid slightly on top of the HeightMap
                                                             name: "HeightMapWireframe")
        {
            // Changing LineColor and Visibility will not call UpdateMesh
            LineColor = Colors.Black,
            Visibility = SceneNodeVisibility.Hidden
        };

        scene.RootNode.Add(_heightMapWireframeNode);

        UpdateContourLines();
    }

    private HeightMapSurfaceNode EnsureHeightMapSurfaceNode()
    {
        // Create height map surface
        // _standardHeightMapNode will have useHeightValuesAsTextureCoordinates: false 
        // _gradientHeightMapNode will have useHeightValuesAsTextureCoordinates: true 
        if (_useGradientTexture)
        {
            if (_gradientHeightMapNode == null)
            {
                _gradientHeightMapNode = new HeightMapSurfaceNode(_heightMapCenterPosition,
                                                                  _heightMapSize,
                                                                  heightData: _heightData,
                                                                  useHeightValuesAsTextureCoordinates: true,
                                                                  name: "HeightMapSurfaceNode-GradientTexture")
                {
                    Material = _graySpecularMaterial,
                    BackMaterial = _graySpecularMaterial,
                };

                if (Scene != null)
                    Scene.RootNode.Add(_gradientHeightMapNode);
            }

            return _gradientHeightMapNode;
        }


        if (_standardHeightMapNode == null)
        {
            _standardHeightMapNode = new HeightMapSurfaceNode(_heightMapCenterPosition,
                                                              _heightMapSize,
                                                              heightData: _heightData,
                                                              useHeightValuesAsTextureCoordinates: false,
                                                              name: "HeightMapSurfaceNode-StandardTexture")
            {
                Material = _graySpecularMaterial,
                BackMaterial = _graySpecularMaterial,
            };

            if (Scene != null)
                Scene.RootNode.Add(_standardHeightMapNode);
        }

        return _standardHeightMapNode;
    }

    private void UpdateHeightMapType()
    {
        if (Scene == null)
            return;

        EnsureHeightMapSurfaceNode();
        UpdateTexture();

        if (_useGradientTexture)
        {
            _gradientHeightMapNode!.Visibility = SceneNodeVisibility.Visible;

            if (_standardHeightMapNode != null)
                _standardHeightMapNode.Visibility = SceneNodeVisibility.Hidden;
        }
        else
        {
            _standardHeightMapNode!.Visibility = SceneNodeVisibility.Visible;

            if (_gradientHeightMapNode != null)
                _gradientHeightMapNode.Visibility = SceneNodeVisibility.Hidden;
        }
    }

    private void UpdateTexture()
    {
        if (GpuDevice == null || _heightData == null)
            return;

        // Create gradient
        _gradientData = CreateSampleGradient(_gradientType, _useTransparentColor);

        if (_useGradientTexture && _gradientHeightMapNode != null)
        {
            // When using gradient texture and height map, we get more accurate results when height values are used for texture coordinates.
            // In this case a one dimensional gradient texture is used (the color at position 0 defined the color for min value; the last position defines the color for max value).
            // Then the texture coordinates are defined to point to the correct color for the height value at the specific position, for example
            // texture coordinate (0, 0.5) is set the minimum height value and texture coordinate (1, 0.5) is set to the maximum height value.
            // The texture coordinates are defined by creating the HeightMapSurfaceNode and setting the useHeightValuesAsTextureCoordinates to true.
            // This should not be used for cases when a bitmap is shown on the height map.


            CommonSamplerTypes samplerType;

            // When using smooth gradient we need to create the gradient texture with interpolated colors.
            // When using hard gradient and if the colors in the gradient are evenly distributed (each have the same percentage of the gradient),
            // then we could create texture with only the number of colors (this is not done in this sample).

            if (_gradientType == GradientType.GeographicalHard)
                samplerType = CommonSamplerTypes.ClampNoInterpolation; // Disable interpolation of colors
            else
                samplerType = CommonSamplerTypes.Clamp;

            // Create 1D texture; "linear" texture that requires special texture coordinates
            var texture1 = TextureFactory.CreateGradientTexture(GpuDevice, _gradientData, textureSize: 256);

            if (_gradientHeightMapMaterial == null)
            {
                _gradientHeightMapMaterial = new StandardMaterial(texture1, samplerType, $"1D texture material ({_gradientType}, {_useTransparentColor})");
                _gradientHeightMapNode.Material = _gradientHeightMapMaterial;
            }
            else
            {
                _gradientHeightMapMaterial.DiffuseTexture = texture1;
            }

            //// Show _gradientHeightMapNode that was created by setting useHeightValuesAsTextureCoordinates to true
            //_gradientHeightMapNode.Visibility = SceneNodeVisibility.Visible;

            //if (_standardHeightMapNode != null)
            //    _standardHeightMapNode.Visibility = SceneNodeVisibility.Hidden;
        }
        else if (_standardHeightMapNode != null)
        {
            // Create 2D height data texture
            var texture2 = TextureFactory.CreateHeightTexture(GpuDevice, _heightData, _gradientData);

            if (_standardHeightMapMaterial == null)
            {
                _standardHeightMapMaterial = new StandardMaterial(texture2, CommonSamplerTypes.Clamp, $"2D height-data texture material ({_gradientType}, {_useTransparentColor})");
                _standardHeightMapNode.Material = _standardHeightMapMaterial;
            }
            else
            {
                _standardHeightMapMaterial.DiffuseTexture = texture2;
            }

            //// Show _standardHeightMapNode that was created by setting useHeightValuesAsTextureCoordinates to false
            //_standardHeightMapNode.Visibility = SceneNodeVisibility.Visible;

            //if (_gradientHeightMapNode != null)
            //    _gradientHeightMapNode.Visibility = SceneNodeVisibility.Hidden;
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
        if (Scene == null || _gradientData == null)
            return;

        var currentHeightMapNode = _gradientHeightMapNode ?? _standardHeightMapNode;

        if (currentHeightMapNode == null)
            return;

        if (_heightMapContoursNode != null)
            Scene.RootNode.Remove(_heightMapContoursNode);


        // Create height map contours, and tie its properties to the height map surface
        // Set all available parameters in the constructor, because changing those values later will call UpdateMesh on each change.
        _heightMapContoursNode = new HeightMapContoursNode(currentHeightMapNode, 
                                                           numContourLines: 20,
                                                           majorLinesFrequency: 5,
                                                           verticalOffset: 0.05f, // lift the grid slightly on top of the HeightMap
                                                           combineContourLines: _linesType == LinesType.CombinedContourLines,
                                                           name: "HeightMapContoursNode")
        {
            // Setting LineThickness and LineColor does not call UpdateMesh
            MinorLineThickness = 1f,
            MajorLineThickness = 2f,
        };

        if (_linesType == LinesType.ColoredContourLines)
        {
            var contourLineHeights = _heightMapContoursNode.GetAvailableLineHeights();

            if (contourLineHeights != null)
            {
                if (float.IsNaN(currentHeightMapNode.MinTextureHeight))
                    currentHeightMapNode.Update();

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
            case GradientType.None:
                stops = new GradientStop[1];
                stops[0] = new GradientStop(Colors.LightGray, 0);
                break;

            case GradientType.Technical:
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
                
            case GradientType.GeographicalSmooth:
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
                
            case GradientType.GeographicalHard:
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
                
            default:
                throw new ArgumentException($"Invalid gradient type: {type}!", nameof(type));
        }

        return stops;
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        
        ui.CreateLabel("Gradient:", isHeader: true);
        ui.CreateRadioButtons(new string[] { "None", "Technical", "GeographicalSmooth", "GeographicalHard" }, (itemIndex, itemText) =>
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

        ui.CreateCheckBox("Use TextureCoordinates as height values (?):When checked, then one dimensional gradient texture is used to define the gradient colors.\nThen the TextureCoordinates in the mesh are used to define the color of each position.\nThis usually produce more accurate results when rendering height maps.", 
            _useGradientTexture, isChecked =>
        {
            _useGradientTexture = isChecked;
            UpdateHeightMapType();
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

        ui.CreateSlider(0, 1, () => 0.2f, ambientValue => Scene?.SetAmbientLight(ambientValue), width: 150, 
            keyText: "Ambient light:", 
            formatShownValueFunc: ambientValue => $"{ambientValue * 100:F0}%");
    }
}