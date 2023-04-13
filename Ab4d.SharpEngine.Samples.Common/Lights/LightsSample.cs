using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.Lights;

public class LightsSample : CommonSample
{
    public override string Title => "Lights sample";

    private AmbientLight? _ambientLight;
    private DirectionalLight? _directionalLight1;
    private PointLight? _pointLight1;
    private SpotLight? _spotLight1;

    private GroupNode? _lightsGroup;
    private List<SceneNode>? _lightsModels;
    private StandardMaterial? _lightEmissiveMaterial;

    private Random _rnd = new Random();
    private PlaneModelNode? _planeModelNode;

    public LightsSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        _planeModelNode = new PlaneModelNode("bottomPlane")
        {
            Position = new Vector3(0, -100, 0),
            Size = new Vector2(800, 1000),
            Material = StandardMaterials.Gray,
            BackMaterial = StandardMaterials.Black
        };

        scene.RootNode.Add(_planeModelNode);


        var standardMaterial = new StandardMaterial(Colors.Silver);
        var specularMaterial = new StandardMaterial(Colors.Silver) { SpecularPower = 64 };

        var textureMaterial = new StandardMaterial(@"Resources\Textures\uvchecker2.jpg", BitmapIO);
        var specularTextureMaterial = (StandardMaterial)textureMaterial.Clone();
        specularTextureMaterial.SpecularPower = 64;

        for (int i = 0; i < 3; i++)
        {
            var sphere = new SphereModelNode($"sphere_1_{i + 1}")
            {
                CenterPosition = new Vector3(0, 0, -250 + i * 100),
                Radius = 40 - i * 10,
                Material = standardMaterial
            };

            scene.RootNode.Add(sphere);


            sphere = new SphereModelNode($"sphere-2_{i + 1}")
            {
                CenterPosition = new Vector3(0, 0, 50 + i * 100),
                Radius = 20 + i * 10,
                Material = specularMaterial
            };

            scene.RootNode.Add(sphere);


            sphere = new SphereModelNode($"sphere_3_{i + 1}")
            {
                CenterPosition = new Vector3(0, 100, -250 + i * 100),
                Radius = 40 - i * 10,
                Material = textureMaterial
            };

            scene.RootNode.Add(sphere);


            sphere = new SphereModelNode($"sphere-4_{i + 1}")
            {
                CenterPosition = new Vector3(0, 100, 50 + i * 100),
                Radius = 20 + i * 10,
                Material = specularTextureMaterial
            };

            scene.RootNode.Add(sphere);
        }

        AddDefaultLights();

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = -80;
            targetPositionCamera.Attitude = -10;
            targetPositionCamera.Distance = 1200;
            targetPositionCamera.ShowCameraLight = ShowCameraLightType.Never;
        }
    }


    public void AddDefaultLights()
    {
        if (Scene == null)
            return;

        Scene.Lights.Clear();


        SetAmbientLight(0);


        _directionalLight1 ??= new DirectionalLight(new Vector3(-1, -0.3f, 0));

        if (!Scene.Lights.Contains(_directionalLight1))
            Scene.Lights.Add(_directionalLight1);


        //_pointLight1 ??= new PointLight(new Vector3(100, 0, -100), range: 10000) { Attenuation = new Vector3(1, 0, 0) };
        _pointLight1 ??= new PointLight(new Vector3(100, 0, -100));

        if (!Scene.Lights.Contains(_pointLight1))
            Scene.Lights.Add(_pointLight1);


        _spotLight1 ??= new SpotLight(new Vector3(300, 0, 200), new Vector3(-1, -0.3f, 0));

        if (!Scene.Lights.Contains(_spotLight1))
            Scene.Lights.Add(_spotLight1);


        UpdateLightModels();
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

        UpdateLightModels();
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

        UpdateLightModels();
    }

    public void SetAmbientLight(float intensityInPercent)
    {
        if (Scene == null)
            return;

        float intensity = intensityInPercent / 100.0f;

        if (_ambientLight == null)
            _ambientLight = new AmbientLight(intensity);
        else
            _ambientLight.SetIntensity(intensity);

        if (!Scene.Lights.Contains(_ambientLight))
            Scene.Lights.Add(_ambientLight);

        //UpdateAmbientLightTextBlock();
    }

    private float GetAmbientLightIntensity()
    {
        if (_ambientLight == null)
            return 0;

        return (_ambientLight.Color.Red + _ambientLight.Color.Green + _ambientLight.Color.Blue) / 3.0f;
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

        _lightEmissiveMaterial ??= new StandardMaterial("YellowLightEmissiveMaterial") { EmissiveColor = Colors.Yellow.ToColor3() };


        for (var i = 0; i < Scene.Lights.Count; i++)
        {
            var oneLight = Scene.Lights[i];

            ModelNode? lightModelNode;

            if (oneLight is ISpotLight spotLight)
            {
                var spotLightDirection = Vector3.Normalize(spotLight.Direction);

                lightModelNode = new ArrowModelNode(_lightEmissiveMaterial, $"SpotLightModel_{i}")
                {
                    StartPosition = spotLight.Position,
                    EndPosition = spotLight.Position + spotLightDirection * 20,
                    Radius = 2
                };
            }
            else if (oneLight is IPointLight pointLight)
            {
                lightModelNode = new SphereModelNode(_lightEmissiveMaterial, $"PointLightModel_{i}")
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
        UpdateLightModels();
    }


    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateSlider(0, 100, () => GetAmbientLightIntensity() * 100, value => SetAmbientLight(value), 80, false, "AmbientLight:", 0, value => $"{value:F0}%");
        ui.AddSeparator();

        ui.CreateButton("Add spot light", () => AddSpotLight(randomColor: true));
        ui.CreateButton("Add point light", () => AddPointLight(randomColor: true));
        ui.CreateButton("Add directional light", () => AddDirectionalLight(randomColor: true));

        ui.AddSeparator();
        ui.CreateButton("Remove all lights", () => RemoveAllLights());
        ui.CreateButton("Use default sample lights", () => AddDefaultLights());

        ui.AddSeparator();
        ui.CreateLabel("camera.ShowCameraLight:");
        ui.CreateRadioButtons(new string[] { "Never (?):Never add additional camera light", "Auto (?):Show camera light only if there is no other light defined in the Scene", "Always (?):Always add a camera light" }, (selectedIndex, selectedText) =>
            {
                if (targetPositionCamera != null)
                    targetPositionCamera.ShowCameraLight = (ShowCameraLightType)selectedIndex;
            }, 0);
    }
}