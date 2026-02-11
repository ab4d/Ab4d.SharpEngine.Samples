using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using System.Numerics;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.Transformations;

namespace Ab4d.SharpEngine.Samples.Common.Lights;

public class LightsSample : CommonSample
{
    public override string Title => "Lights sample";

    private DirectionalLight? _directionalLight1;
    private PointLight? _pointLight1;
    private SpotLight? _spotLight1;

    private GroupNode? _lightsGroup;
    private List<SceneNode>? _lightsModels;
    private SolidColorMaterial? _lightMaterial;

    private PlaneModelNode? _planeModelNode;
    private StandardMaterial? _textureMaterial;
    private StandardMaterial? _specularTextureMaterial;

    private string? _lightsInfoText;
    private ICommonSampleUIElement? _lightsInfoLabel;
    private WorldColorInstanceData[]? _instancesData;
    private StandardMesh? _sphereMesh;

    public LightsSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        _planeModelNode = new PlaneModelNode("bottomPlane")
        {
            Position = new Vector3(0, -100, 0),
            Size = new Vector2(1000, 800),
            Material = StandardMaterials.Gray,
            BackMaterial = StandardMaterials.Black
        };

        scene.RootNode.Add(_planeModelNode);


        var standardMaterial = new StandardMaterial(Colors.Silver);
        var specularMaterial = new StandardMaterial(Colors.Silver) { SpecularPower = 64 };

        _textureMaterial = new StandardMaterial(Colors.Gray);

        _specularTextureMaterial = (StandardMaterial)_textureMaterial.Clone();
        _specularTextureMaterial.SpecularPower = 64;


        for (int i = 0; i < 3; i++)
        {
            var sphere = new SphereModelNode($"sphere_1_{i + 1}")
            {
                CenterPosition = new Vector3(50 + i * 100, 0, 0),
                Radius = 20 + i * 10,
                Material = standardMaterial
            };

            scene.RootNode.Add(sphere);


            sphere = new SphereModelNode($"sphere-2_{i + 1}")
            {
                CenterPosition = new Vector3(-250 + i * 100, 0, 0),
                Radius = 40 - i * 10,
                Material = specularMaterial
            };

            scene.RootNode.Add(sphere);


            sphere = new SphereModelNode($"sphere_3_{i + 1}")
            {
                CenterPosition = new Vector3(50 + i * 100, 100, 0),
                Radius = 20 + i * 10,
                Material = _textureMaterial
            };

            scene.RootNode.Add(sphere);


            sphere = new SphereModelNode($"sphere-4_{i + 1}")
            {
                CenterPosition = new Vector3(-250 + i * 100, 100, 0),
                Radius = 40 - i * 10,
                Material = _specularTextureMaterial
            };

            scene.RootNode.Add(sphere);
        }

        

        _sphereMesh = Meshes.MeshFactory.CreateSphereMesh(new Vector3(0, 0, 0), 30, 30);

        var instancedMeshNode1 = new InstancedMeshNode("InstancedMeshNode1");
        instancedMeshNode1.Mesh = _sphereMesh;

        _instancesData = new WorldColorInstanceData[]
        {
            new WorldColorInstanceData(new Vector3(-200, 200, 0), Colors.Yellow),
            new WorldColorInstanceData(new Vector3(-80, 200, 0), Colors.Orange),
        };

        instancedMeshNode1.SetInstancesData(_instancesData);

        scene.RootNode.Add(instancedMeshNode1);
        


        AddDefaultLights();

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 20;
            targetPositionCamera.Attitude = -10;
            targetPositionCamera.Distance = 1200;
            targetPositionCamera.ShowCameraLight = ShowCameraLightType.Never;
        }

        //var gpuImage = await base.GetCommonTextureAsync(scene, CommonTextures.UVChecker);

        base.GetCommonTexture(scene, CommonTextures.UVChecker, gpuImage =>
        {
            if (_textureMaterial != null)
            {
                _textureMaterial.DiffuseTexture = gpuImage;
                _textureMaterial.DiffuseColor = Colors.White;
            }

            if (_specularTextureMaterial != null)
            {
                _specularTextureMaterial.DiffuseTexture = gpuImage;
                _specularTextureMaterial.DiffuseColor = Colors.White;
            }

            
        
            var instancedMeshNode2 = new InstancedMeshNode("InstancedMeshNode2");
            instancedMeshNode2.Mesh = _sphereMesh;
            instancedMeshNode2.Transform = new TranslateTransform(300, 0, 0);

            instancedMeshNode2.SetInstancesData(_instancesData);

            scene.RootNode.Add(instancedMeshNode2);
        
            instancedMeshNode2.SetDiffuseTexture(gpuImage, CommonSamplerTypes.Mirror);
        });

    }


    public void AddDefaultLights()
    {
        if (Scene == null)
            return;

        Scene.Lights.Clear();


        SetAmbientLight(0);


        _directionalLight1 ??= new DirectionalLight(direction: new Vector3(-1, -0.3f, 0)); // direction value will be normalized by the engine

        if (!Scene.Lights.Contains(_directionalLight1))
            Scene.Lights.Add(_directionalLight1);


        //_pointLight1 ??= new PointLight(new Vector3(100, 0, -100), range: 10000) { Attenuation = new Vector3(1, 0, 0) };
        _pointLight1 ??= new PointLight(position: new Vector3(-300, 80, 200));

        if (!Scene.Lights.Contains(_pointLight1))
            Scene.Lights.Add(_pointLight1);


        _spotLight1 ??= new SpotLight(position: new Vector3(150, 100, 250),
                                      direction: new Vector3(-0.75f, -0.5f, -1), // direction value will be normalized by the engine
                                      innerConeAngle: 30,  // 40 by default
                                      outerConeAngle: 40); // 50 by default

        if (!Scene.Lights.Contains(_spotLight1))
            Scene.Lights.Add(_spotLight1);


        OnLightsUpdated(); // Update light models and info text
    }

    public void AddDirectionalLight(bool randomColor = true)
    {
        if (Scene == null)
            return;

        // additional lights have random direction
        var newLight = new DirectionalLight(GetRandomDirection());
        if (randomColor)
            newLight.Color = GetRandomColor3();

        Scene.Lights.Add(newLight);

        OnLightsUpdated(); // Update light models and info text
    }

    public void AddPointLight(bool randomColor = true)
    {
        if (Scene == null)
            return;

        // additional lights have random direction
        var newLight = new PointLight(GetRandomPosition());
        if (randomColor)
            newLight.Color = GetRandomColor3();

        Scene.Lights.Add(newLight);

        OnLightsUpdated(); // Update light models and info text
    }

    public void AddSpotLight(bool randomColor = true)
    {
        if (Scene == null)
            return;

        var position = GetRandomPosition();
        var direction = Vector3.Normalize(position * -1); // toward the center of the scene
        var newLight = new SpotLight(position, direction);
        if (randomColor)
            newLight.Color = GetRandomColor3();

        Scene.Lights.Add(newLight);

        OnLightsUpdated(); // Update light models and info text
    }

    public void SetAmbientLight(float intensityInPercent)
    {
        if (Scene == null)
            return;

        var spotLight = Scene.Lights.OfType<SpotLight>().FirstOrDefault();
        if (spotLight != null)
        {
            spotLight.InnerConeAngle = 10 + 0.5f * intensityInPercent;
            spotLight.OuterConeAngle = 20 + 0.5f * intensityInPercent;
        }

        float intensity = intensityInPercent / 100.0f;

        Scene.SetAmbientLight(intensity);

        // We could also manually add AmbientLight:
        //if (_ambientLight == null)
        //    _ambientLight = new AmbientLight(intensity);
        //else
        //    _ambientLight.SetIntensity(intensity);

        //if (!Scene.Lights.Contains(_ambientLight))
        //    Scene.Lights.Add(_ambientLight);

        //UpdateAmbientLightTextBlock();

        OnLightsUpdated(); // Update light models and info text
    }

    // Get random position above the _planeModelNode
    private Vector3 GetRandomPosition()
    {
        if (_planeModelNode == null)
            return new Vector3(0, 100, 0);

        var centerPosition = new Vector3(_planeModelNode.Position.X, _planeModelNode.Position.Y + 150, _planeModelNode.Position.Z);
        var areaSize = new Vector3(_planeModelNode.Size.X, 300, _planeModelNode.Size.Y);

        return GetRandomPosition(centerPosition, areaSize);
    }

    private void UpdateLightModels()
    {
        if (Scene == null)
            return;

        if (_lightsGroup == null)
        {
            _lightsGroup = new GroupNode("LightsGroup");
            Scene.RootNode.Add(_lightsGroup);
        }

        if (_lightsModels == null)
        {
            _lightsModels = new List<SceneNode>();
        }
        else
        {
            foreach (var lightsModel in _lightsModels)
            {
                if (_lightsGroup != null)
                    _lightsGroup.Remove(lightsModel);
            }

            _lightsModels.Clear();
        }

        _lightMaterial ??= new SolidColorMaterial(Colors.Yellow, name: "YellowLightMaterial");


        for (var i = 0; i < Scene.Lights.Count; i++)
        {
            var oneLight = Scene.Lights[i];

            ModelNode? lightModelNode;

            if (oneLight is ISpotLight spotLight)
            {
                var spotLightDirection = Vector3.Normalize(spotLight.Direction);

                lightModelNode = new ArrowModelNode(_lightMaterial, $"SpotLightModel_{i}")
                {
                    StartPosition = spotLight.Position,
                    EndPosition = spotLight.Position + spotLightDirection * 20,
                    Radius = 2
                };
            }
            else if (oneLight is IPointLight pointLight)
            {
                lightModelNode = new SphereModelNode(_lightMaterial, $"PointLightModel_{i}")
                {
                    CenterPosition = pointLight.Position,
                    Radius = 3
                };
            }
            else
            {
                lightModelNode = null;
            }

            if (lightModelNode != null)
            {
                if (_lightsGroup != null)
                    _lightsGroup.Add(lightModelNode);

                if (_lightsModels != null)
                    _lightsModels.Add(lightModelNode);
            }
        }
    }

    private void RemoveAllLights()
    {
        if (Scene == null)
            return;

        Scene.Lights.Clear();
        OnLightsUpdated(); // Update light models and info text
    }

    private void OnLightsUpdated()
    {
        targetPositionCamera?.Update(); // This will immediately update the lights so the UpdateLightsInfoText will be able to see if CameraLight is present or not

        UpdateLightModels();
        UpdateLightsInfoText();
    }
    
    private void UpdateLightsInfoText()
    {
        if (Scene == null)
            return;

        int directionalLightsCount = Scene.Lights.Count(l => l is DirectionalLight);
        int spotLightCount = Scene.Lights.Count(l => l is SpotLight);
        int pointLightCount = Scene.Lights.Count(l => l is PointLight) - spotLightCount; // SpotLight is derived from PointLight
        bool hasCameraLights = Scene.Lights.Any(l => l is CameraLight);

        _lightsInfoText = $"AmbientLight: {Scene.GetAmbientLightIntensity() * 100:N0}%\nDirectionalLights: {directionalLightsCount}\nPointLights: {pointLightCount}\nSpotLights: {spotLightCount}\nHas CameraLight: {hasCameraLights}";

        _lightsInfoLabel?.UpdateValue();
    }


    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateSlider(0, 100, () => 0, value => SetAmbientLight(value), 80, false, "AmbientLight:", 0, value => $"{value:F0}%");
        ui.AddSeparator();

        ui.CreateButton("Add directional light", () => AddDirectionalLight(randomColor: true));
        ui.CreateButton("Add point light", () => AddPointLight(randomColor: true));
        ui.CreateButton("Add spot light", () => AddSpotLight(randomColor: true));

        ui.AddSeparator();
        ui.CreateButton("Remove all lights", () => RemoveAllLights());
        ui.CreateButton("Use default sample lights", () => AddDefaultLights());

        ui.AddSeparator();
        ui.CreateLabel("camera.ShowCameraLight:");
        ui.CreateRadioButtons(new string[] { "Never (?):Never add additional camera light", "Auto (?):Show camera light only if there is no other light defined in the Scene", "Always (?):Always add a camera light" }, (selectedIndex, selectedText) =>
            {
                if (targetPositionCamera != null)
                {
                    targetPositionCamera.ShowCameraLight = (ShowCameraLightType)selectedIndex;
                    OnLightsUpdated();
                }
            }, 0);

        ui.AddSeparator();
        _lightsInfoLabel = ui.CreateKeyValueLabel(keyText: null, () => _lightsInfoText ?? "");
    }
}