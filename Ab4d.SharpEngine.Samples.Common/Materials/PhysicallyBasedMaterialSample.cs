using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.Materials;

public class PhysicallyBasedMaterialSample : CommonSample
{
    public override string Title => "Physically Based Rendering (PBR)";
    public override string Subtitle => "PBR provides a much more physically accurate rendering than standard rendering (used for StandardMaterial).\nBesides color its main properties are Metalness and Roughness that define how light is reflected from the surface.\nPhysicallyBasedMaterial supports base color, metalness-roughness, normal and environment maps.";

    private readonly string _environmentMapBaseFolder;

    private bool _isSimpleScene = true;

    private bool _useEnvironmentMap = true;
    private bool _useBaseColorTexture = false;
    private bool _useNormalMap = false;
    private bool _useMetalnessRoughnessMap = false;

    private float _metalness = 0.5f;
    private float _roughness = 0.2f;

    private PointLight? _topPointLight;
    private DirectionalLight? _frontDirectionalLight;
    private SpotLight? _sideSpotLight;
    private AmbientLight? _ambientLight;

    private PhysicallyBasedMaterial? _physicallyBasedMaterial;
    
    private Color4 _baseColor = Colors.Silver;


    private GpuImage? _environmentCubeMapImage;
    private GpuImage? _baseColorGpuImage;
    private GpuImage? _normalMapGpuImage;
    private GpuImage? _metalnessRoughnessMapGpuImage;

    private GroupNode? _testObjectsGroupNode;

    private MultiMaterialModelNode? _skyBoxNode;
    private TranslateTransform? _skyboxTransform;
    private StandardMesh? _teapotMesh;
    private TextBlockFactory? _textBlockFactory;

    private ICommonSampleUIElement? _metalnessSlider;
    private ICommonSampleUIElement? _roughnessSlider;
    private ICommonSampleUIElement? _metalnessRoughnessMapCheckBox;


    public PhysicallyBasedMaterialSample(ICommonSamplesContext context)
        : base(context)
    {
        _environmentMapBaseFolder = GetCommonTexturePath("CloudyLightRays");// System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources/EnvironmentMaps/");

        RotateAroundPointerPosition = true;
        ZoomMode = CameraZoomMode.PointerPosition;
    }

    protected override async Task OnCreateSceneAsync(Scene scene)
    {
        if (scene.GpuDevice == null)
            return;


        _testObjectsGroupNode = new GroupNode("TestObjectsGroupNode");
        scene.RootNode.Add(_testObjectsGroupNode);


        // Because the images in the _environmentMapFolder follow  expected naming convention
        // (e.g. file names end with face-specific suffices: _posx, _negx, _posy, _negy, _posz, and _negz),
        // we can pass the path to directory to LoadCubeMap.
        // Otherwise, we would need to use an overload that gets 6 file names.
        _environmentCubeMapImage = CubeMapLoader.LoadCubeMap(_environmentMapBaseFolder, scene.GpuDevice, name: "CloudyLightRays");


        _physicallyBasedMaterial = new PhysicallyBasedMaterial(baseColor: _baseColor, metalness: _metalness, roughness: _roughness, name: "PhysicallyBasedMaterial")
            { IsTwoSided = true}; // use two sided rendering to show the inside of the teapot

        _physicallyBasedMaterial.SetTextureMap(TextureMapTypes.EnvironmentCubeMap, _environmentCubeMapImage);


        _teapotMesh = await base.GetCommonMeshAsync(scene, CommonMeshes.Teapot, position: new Vector3(0, 0, 0), positionType: PositionTypes.Center, finalSize: new Vector3(80, 80, 80));

        _textBlockFactory = await context.GetTextBlockFactoryAsync();


        // After the required resources are loaded, we can create the main scene objects.
        if (_isSimpleScene)
            SetupSimpleScene();
        else
            SetupMultipleTeapots();

        SetupSkybox(scene);


        // After the main scene is created, load the additional textures that can be enabled by CheckBoxes:

        string metalPlateFolderName = GetCommonTexturePath("metal_plate_1k/");
        
        // Base color texture is the same as diffuse texture. It provides the color of the pixels.
        _baseColorGpuImage = await TextureLoader.CreateTextureAsync(metalPlateFolderName + "metal_plate_diff_1k.png", scene);

        // Normal map is used to adjust the normals of the pixels to create more detailed lighting effects.
        // It is usually stored in a special RGB format where RGB values represent XYZ components of the normal vector.
        _normalMapGpuImage = await TextureLoader.CreateTextureAsync(metalPlateFolderName + "metal_plate_nor_gl_1k.png", scene);

        // Metalness and roughness maps are used to define how the light is reflected from the surface.
        var metalnessRawImage = scene.GpuDevice.DefaultBitmapIO.LoadBitmap(metalPlateFolderName + "metal_plate_metal_1k.png");
        var roughnessRawImage = scene.GpuDevice.DefaultBitmapIO.LoadBitmap(metalPlateFolderName + "metal_plate_rough_1k.png");

        // The current version of the PBR shader requires that metalness and roughness values are stored in the same texture (metalness in blue channel and roughness in green channel).
        // This is also standard for glTF models that use a combined metalness-roughness map.
        //
        // But if we have separate metalness and roughness textures, then we need to combine them into one texture:
        var metalnessRoughnessRawImage = PhysicallyBasedMaterial.CreateMetalnessRoughnessImage(metalnessRawImage, roughnessRawImage);
        _metalnessRoughnessMapGpuImage = new GpuImage(scene.GpuDevice, metalnessRoughnessRawImage, imageSource: "MetalnessRoughness");
    }

    private void SetupSimpleScene()
    {
        if (_testObjectsGroupNode == null || _teapotMesh == null || _physicallyBasedMaterial == null)
            return;

        _testObjectsGroupNode.Clear();

        var teapotModel = new MeshModelNode(_teapotMesh, _physicallyBasedMaterial, "TeapotModel");

        _testObjectsGroupNode.Add(teapotModel);


        var sphereNode = new SphereModelNode()
        {
            CenterPosition = new Vector3(-90, 0, 0),
            Radius = 20,
            Segments = 50,
            Material = _physicallyBasedMaterial,
            UseSharedSphereMesh = false,
        };

        _testObjectsGroupNode.Add(sphereNode);
        
        
        var boxModelNode = new BoxModelNode(new Vector3(100, 0, 0), new Vector3(60, 40, 50), _physicallyBasedMaterial, "BoxModelNode");
        _testObjectsGroupNode.Add(boxModelNode);


        if (targetPositionCamera != null)
        {
            targetPositionCamera.TargetPosition = new Vector3(0, 0, 0);
            targetPositionCamera.Heading = 40;
            targetPositionCamera.Attitude = -14;
            targetPositionCamera.Distance = 350;
        }
    }
    
    private void SetupMultipleTeapots()
    {
        if (_testObjectsGroupNode == null || _teapotMesh == null || _physicallyBasedMaterial == null)
            return;

        _testObjectsGroupNode.Clear();

        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 5; y ++)
            {
                var position = new Vector3(x * 100 - 200, y * 70 - 140, 0);

                var metalness = (float)x / 4; // from 0 to 1
                var roughness = (float)y / 4; // from 0 to 1

                var physicallyBasedMaterial = new PhysicallyBasedMaterial(baseColor: _baseColor,
                                                                          metalness: metalness,
                                                                          roughness: roughness,
                                                                          name: $"PhysicallyBasedMaterial-{x}-{y}")

                {
                    IsTwoSided = true // Render the inside of the teapot
                };

                physicallyBasedMaterial.SetTextureMap(TextureMapTypes.EnvironmentCubeMap, _environmentCubeMapImage);


                var teapotModel = new MeshModelNode(_teapotMesh, physicallyBasedMaterial, $"TeapotModel-{x}-{y}")
                {
                    Transform = new TranslateTransform(position)
                };

                _testObjectsGroupNode.Add(teapotModel);
            }
        }


        if (_textBlockFactory != null)
        {
            _textBlockFactory.BackgroundColor = Colors.Transparent;
            _textBlockFactory.BorderThickness = 0;
            _textBlockFactory.FontSize = 20;

            var label1 = _textBlockFactory.CreateTextBlock("Metalness =>  (0 ... 1)", new Vector3(-230, -200, 0), positionType: PositionTypes.Left, textAttitude: 90);
            _testObjectsGroupNode.Add(label1);

            var label2 = _textBlockFactory.CreateTextBlock(new Vector3(-280, -160, 0), positionType: PositionTypes.Left, "Roughness =>  (0 ... 1)", textDirection: new Vector3(0, 1, 0), upDirection: new Vector3(-1, 0, 0));
            _testObjectsGroupNode.Add(label2);
        }

        if (targetPositionCamera != null)
        {
            targetPositionCamera.TargetPosition = new Vector3(0, 0, 0);
            targetPositionCamera.Heading = 35;
            targetPositionCamera.Attitude = -5;
            targetPositionCamera.Distance = 1000;
        }
    }

    private void SetupSkybox(Scene scene)
    {
        // To create a skybox effect, we create a MultiMaterialModelNode with a special box mesh
        // and assign correct skybox texture to each of the box sides.

        // CreateSkyBoxMesh creates a box mesh with inverted normals, adjusted texture coordinates and flipped triangle order to correctly show the sky box
        var skyBoxMesh = MeshFactory.CreateSkyBoxMesh(size: 5000);

        _skyBoxNode = new MultiMaterialModelNode(skyBoxMesh, "SkyboxMultiMaterialModelNode")
        {
            IsHitTestVisible = false // IMPORTANT: Disable hit testing to prevent rotating around positions on the skybox and zooming to skybox
        };

        // The textures for the sky box are correctly visible only when the camera is positioned in the center of the sky box.
        // But because we render all the objects in the scene with the same camera, we cannot move the camera only for the sky box.
        // To solve this, we update the position of the sky box node in each frame so it is always centered on the camera position.
        // This way we achieve the same effect as we would move the camera to the center of the sky box, but without actually moving the camera.
        // The _skyboxTransform is updated in the OnCameraChanged event handler that is called each time the camera moves.
        _skyboxTransform = new TranslateTransform(targetPositionCamera?.GetCameraPosition() ?? new Vector3(0, 0, 0));
        _skyBoxNode.Transform = _skyboxTransform;

        if (scene.BackgroundRenderingLayer != null)
        {
            // Set the CustomRenderingLayer of the skybox node to the BackgroundRenderingLayer so it is rendered before other 3D objects.
            _skyBoxNode.CustomRenderingLayer = scene.BackgroundRenderingLayer;

            // Clear the depth buffer after rendering the skybox so the rest of the scene would render correctly in front of the skybox
            scene.BackgroundRenderingLayer.ClearDepthStencilBufferAfterRendering = true; 
        }

        // Now set the sky box textures for each of the 6 sides of the box mesh.
        // For that we can reuse the
        // _environmentCubeMapImage and created new GpuImages that would share the same image memory
        // but have different ImageView objects - each with a different view on the cube map.

        if (_environmentCubeMapImage != null)
        {
            GpuImage[] faceGpuImages = CubeMapLoader.CreateIndividualFaceImages(_environmentCubeMapImage);

            // To correctly set the textures on the sky box, we need to know the start triangle index location for each of the 6 sides of the box mesh.
            int[] subMeshStartLocations = new[]
            {
                18, 12, // +- X
                0, 30, // +- Y
                6, 24, // +- Z
            };

            for (int i = 0; i < 6; i++)
            {
                var faceMaterial = new SolidColorMaterial(faceGpuImages[i]); // use SolidColorMaterial to prevent shading the material by the lights
                _skyBoxNode.AddSubMesh(subMeshStartLocations[i], 6, material: faceMaterial); // render 2 triangles (6 triangle indides) by the specified faceMaterial
            }
        }
        else
        {
            // If we do not have a cube map GpuImage, then we can manually create the materials for each of the sky box sides
            // by loading the textures from files and creating GpuImages for each of them.

            // The first parameter to AddSubMesh is startIndexLocation and the next is indexCount
            // We leave the material parameter with null value and only set backMaterial.
            // This can be done with the following code:
            
            _skyBoxNode.AddSubMesh(0, 6,  material: GetSkyboxFaceMaterial("_posy"));
            _skyBoxNode.AddSubMesh(6, 6,  material: GetSkyboxFaceMaterial("_posz"));
            _skyBoxNode.AddSubMesh(12, 6, material: GetSkyboxFaceMaterial("_negx"));
            _skyBoxNode.AddSubMesh(18, 6, material: GetSkyboxFaceMaterial("_posx"));
            _skyBoxNode.AddSubMesh(24, 6, material: GetSkyboxFaceMaterial("_negz"));
            _skyBoxNode.AddSubMesh(30, 6, material: GetSkyboxFaceMaterial("_negy"));
        }

        scene.RootNode.Add(_skyBoxNode);


        // Subscribe to camera changes to update the _skyboxTransform
        if (targetPositionCamera != null)
            targetPositionCamera.CameraChanged += OnCameraChanged;

        // IMPORTANT:
        // Do not forget to unsubscribe from CameraChanged
    }

    private Material? GetSkyboxFaceMaterial(string sideName)
    {
        if (Scene == null || Scene.GpuDevice == null)
            return null;

        var fileName = System.IO.Path.Combine(_environmentMapBaseFolder, $"CloudyLightRays{sideName}.png");
        var singleFaceGpuImage= TextureLoader.CreateTexture(fileName, Scene.GpuDevice);

        return new SolidColorMaterial(singleFaceGpuImage); // Use SolidColorMaterial so there is no shading based on lights
    }

    private void OnCameraChanged(object? sender, EventArgs e)
    {
        if (targetPositionCamera == null || _skyboxTransform == null)
            return;

        // Update the position of the sky box so it is always centered on the camera position.
        // See comment in the OnCreateSceneAsync for more info.
        var cameraPosition = targetPositionCamera.GetCameraPosition();

        // Only update the transformation when the camera position is changed.
        
        // ReSharper disable CompareOfFloatsByEqualityOperator
        if (cameraPosition.X != _skyboxTransform.X || cameraPosition.Y != _skyboxTransform.Y || cameraPosition.Z != _skyboxTransform.Z)
            _skyboxTransform.SetTranslate(cameraPosition);
        // ReSharper restore CompareOfFloatsByEqualityOperator
    }

    protected override void OnCreateLights(Scene scene)
    {
        scene.Lights.Clear();

        _topPointLight = new PointLight(new Vector3(0, 300, 0));
        scene.Lights.Add(_topPointLight);

        _frontDirectionalLight = new DirectionalLight(new Vector3(0, 0, -1));
        scene.Lights.Add(_frontDirectionalLight);

        _sideSpotLight = new SpotLight(position: new Vector3(-200, 100, 50),
            direction: new Vector3(2, -1, 0))
        {
            InnerConeAngle = 30,
            OuterConeAngle = 40
        };
        scene.Lights.Add(_sideSpotLight);

        _ambientLight = new AmbientLight(0); // disabled by default
        scene.Lights.Add(_ambientLight);

        if (targetPositionCamera != null)
            targetPositionCamera.ShowCameraLight = ShowCameraLightType.Never;

        // Do not call base method to prevet adding default lights
        //base.OnCreateLights(scene);
    }

    private void UpdatePbrMaps()
    {
        if (_testObjectsGroupNode == null)
            return;

        foreach (var meshModelNode in _testObjectsGroupNode.OfType<MeshModelNode>())
        {
            if (meshModelNode.Material is PhysicallyBasedMaterial physicallyBasedMaterial)
            {
                if (_useEnvironmentMap)
                    physicallyBasedMaterial.SetTextureMap(TextureMapTypes.EnvironmentCubeMap, _environmentCubeMapImage);
                else
                    physicallyBasedMaterial.RemoveTextureMap(TextureMapTypes.EnvironmentCubeMap);
                
                if (_useBaseColorTexture && _baseColorGpuImage != null)
                {
                    physicallyBasedMaterial.SetTextureMap(TextureMapTypes.BaseColor, _baseColorGpuImage);
                    physicallyBasedMaterial.BaseColor = Colors.White; // When using BaseColor map, then BaseColor property value is multiplied by the texture's color.
                }
                else
                {
                    physicallyBasedMaterial.RemoveTextureMap(TextureMapTypes.BaseColor);
                    physicallyBasedMaterial.BaseColor = _baseColor;
                }
                
                if (_useNormalMap && _normalMapGpuImage != null)
                    physicallyBasedMaterial.SetTextureMap(TextureMapTypes.NormalMap, _normalMapGpuImage);
                else
                    physicallyBasedMaterial.RemoveTextureMap(TextureMapTypes.NormalMap);

                if (_isSimpleScene) // do not change Metalness and Roughness when showing multiple teapots because they are used to show different combinations of Metalness and Roughness values
                {
                    if (_useMetalnessRoughnessMap && _metalnessRoughnessMapGpuImage != null)
                    {
                        // It is also possible to store both metalness and roughness in the same texture (e.g. metalness in R channel and roughness in G channel)
                        physicallyBasedMaterial.SetTextureMap(TextureMapTypes.MetalnessRoughness, _metalnessRoughnessMapGpuImage);

                        // We cannot use separate Metalness and Roughness maps.
                        // We need to generate a combined map that would store metalness and roughness in one texture.
                        // See OnCreateSceneAsync for more info.
                        //physicallyBasedMaterial.SetTextureMap(TextureMapTypes.Metalness, _metalnessMapGpuImage);
                        //physicallyBasedMaterial.SetTextureMap(TextureMapTypes.Roughness, _roughnessMapGpuImage);

                        // We also need to set Metalness and Roughness to 1 because when MetalnessRoughness map is set, they are used as factors.
                        physicallyBasedMaterial.Metalness = 1;
                        physicallyBasedMaterial.Roughness = 1;
                    }
                    else
                    {
                        physicallyBasedMaterial.RemoveTextureMap(TextureMapTypes.MetalnessRoughness);

                        // Restore Metalness and Roughness
                        physicallyBasedMaterial.Metalness = _metalness;
                        physicallyBasedMaterial.Roughness = _roughness;
                    }
                }
            }
        }
    }

    /// <inheritdoc />
    protected override void OnDisposed()
    {
        // Unsubscribe from CameraChanged - there we updated the position of the sky box to always be centered on the camera position.
        if (targetPositionCamera != null)
            targetPositionCamera.CameraChanged -= OnCameraChanged;
        
        base.OnDisposed();
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);


        ui.CreateLabel("PBR properties:", isHeader: true);

        _metalnessSlider = ui.CreateSlider(minValue: 0, maxValue: 1, getValueFunc: () => _metalness, 
            setValueAction: newValue =>
            {
                _metalness = newValue;
                _physicallyBasedMaterial!.Metalness = _metalness;
            }, 
            width: 110, keyText: "Metalness:", formatShownValueFunc: sliderValue => $"{sliderValue:F2}", keyTextWidth: 80);
        
        _roughnessSlider = ui.CreateSlider(minValue: 0, maxValue: 1, getValueFunc: () => _roughness, 
            setValueAction: newValue =>
            {
                _roughness = newValue;
                _physicallyBasedMaterial!.Roughness = _roughness;
            }, 
            width: 110, keyText: "Roughness:", formatShownValueFunc: sliderValue => $"{sliderValue:F2}", keyTextWidth: 80);

        ui.AddSeparator();
        
        
        _metalnessRoughnessMapCheckBox = ui.CreateCheckBox(text: "Use Metalness & Roughness Map", isInitiallyChecked: _useMetalnessRoughnessMap, checkedChangedAction: isChecked =>
        {
            _useMetalnessRoughnessMap = isChecked;
            _metalnessSlider.SetIsVisible(!isChecked);
            _roughnessSlider.SetIsVisible(!isChecked);
            UpdatePbrMaps();
        });

        ui.CreateCheckBox(text: "Use Environment Map", isInitiallyChecked: _useEnvironmentMap, checkedChangedAction: isChecked =>
            {
                _useEnvironmentMap = isChecked;
                UpdatePbrMaps();
            });
        
        ui.CreateCheckBox(text: "Use BaseColor texture", isInitiallyChecked: _useBaseColorTexture, checkedChangedAction: isChecked =>
            {
                _useBaseColorTexture = isChecked;
                UpdatePbrMaps();
            });   

        
        ui.CreateCheckBox(text: "Use Normal Map", isInitiallyChecked: _useNormalMap, checkedChangedAction: isChecked =>
            {
                _useNormalMap = isChecked;
                UpdatePbrMaps();
            });        

        ui.AddSeparator();


        ui.CreateLabel("Lights:", isHeader: true);

        ui.CreateCheckBox("Top light", true, isChecked => _topPointLight!.IsEnabled = isChecked);
        ui.CreateCheckBox("Font light", true, isChecked => _frontDirectionalLight!.IsEnabled = isChecked);
        ui.CreateCheckBox("Side light", true, isChecked => _sideSpotLight!.IsEnabled = isChecked);
        ui.CreateCheckBox("Camera light", false, isChecked => targetPositionCamera!.ShowCameraLight = isChecked ? ShowCameraLightType.Always : ShowCameraLightType.Never);
        ui.CreateCheckBox("Ambient light (30%)", false, isChecked => _ambientLight!.SetIntensity(isChecked ? 0.3f : 0));

        ui.AddSeparator();


        ui.CreateLabel("Scene type:", isHeader: true);

        ui.CreateRadioButtons(new string[] { "Simple scene", "Multiple teapots" }, (selectedIndex, selectedText) =>
        {
            _isSimpleScene = selectedIndex == 0;

            if (_isSimpleScene)
                SetupSimpleScene();
            else
                SetupMultipleTeapots();

            UpdatePbrMaps();

            _metalnessSlider?.SetIsVisible(_isSimpleScene && !_useMetalnessRoughnessMap);
            _roughnessSlider?.SetIsVisible(_isSimpleScene && !_useMetalnessRoughnessMap);
            _metalnessRoughnessMapCheckBox?.SetIsVisible(_isSimpleScene);

        }, selectedItemIndex: 0);
    }
}
