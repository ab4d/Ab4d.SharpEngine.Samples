using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Windows;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Effects;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.Samples.Wpf.Common;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Wpf;
using Ab4d.Vulkan;
using Cyotek.Drawing.BitmapFont;
using Page = System.Windows.Controls.Page;

namespace Ab4d.SharpEngine.Samples.Wpf.TestScenes
{
    /// <summary>
    /// Interaction logic for LightsTestScene.xaml
    /// </summary>
    public partial class LightsTestScene : Page
    {
        private Scene? _scene;
        private SceneView? _sceneView;

        private WpfBitmapIO _bitmapIO;

        private MouseCameraController? _mouseCameraController;

        private TargetPositionCamera? _targetPositionCamera;

        private AmbientLight? _ambientLight;
        private DirectionalLight? _directionalLight1;
        private PointLight? _pointLight1;
        private SpotLight? _spotLight1;

        private GroupNode? _lightsGroup;
        private List<SceneNode>? _lightsModels;
        private StandardMaterial? _lightEmissiveMaterial;

        private Random _rnd = new Random();
        private PlaneModelNode? _planeModelNode;

        public LightsTestScene()
        {
            InitializeComponent();

            _bitmapIO = new WpfBitmapIO(); // _bitmapIO provides a cross-platform way to read bitmaps (it uses WPF as backend)

            // Setup logger
            LogHelper.SetupSharpEngineLogger(enableFullLogging: false); // Set enableFullLogging to true in case of problems and then please send the log text with the description of the problem to AB4D company


            // MainSceneView is defined in XAML

            // Set EnableStandardValidation to true, but the Vulkan validation will be enabled only when the Vulkan SDK is installed on the system.
            MainSceneView.CreateOptions.EnableStandardValidation = true;

            MainSceneView.SceneViewInitialized += delegate (object? o, EventArgs args)
            {
                _scene = MainSceneView.Scene;
                _sceneView = MainSceneView.SceneView;

                if (_scene == null || _sceneView == null)
                    return; // This should not happen in SceneViewInitialized

                SetupMouseCameraController();
                CreateLights();

                CreateTestScene(_scene);
            };

            //MainSceneView.SceneUpdating += delegate(object? sender, EventArgs args)
            //{
            //    // Do animations
            //};

            Unloaded += delegate (object sender, RoutedEventArgs args)
            {
                MainSceneView.Dispose();
            };
        }

        private void SetupMouseCameraController()
        {
            if (MainSceneView == null || MainSceneView.SceneView == null)
                return;


            _targetPositionCamera = new TargetPositionCamera()
            {
                Heading = -80,
                Attitude = -10,
                Distance = 1000,
                TargetPosition = new Vector3(0, 0, 0),
                ShowCameraLight = ShowCameraLightType.Auto
            };

            MainSceneView.SceneView.Camera = _targetPositionCamera;


            _mouseCameraController = new MouseCameraController(MainSceneView)
            {
                RotateCameraConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed,                                                   // this is already the default value but is still set up here for clarity
                MoveCameraConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed | MouseAndKeyboardConditions.ControlKey,             // this is already the default value but is still set up here for clarity
                QuickZoomConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed | MouseAndKeyboardConditions.RightMouseButtonPressed, // quick zoom is disabled by default
                ZoomMode = CameraZoomMode.MousePosition,
                RotateAroundMousePosition = true
            };
        }

        private void CreateTestScene(Scene scene)
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

            var textureMaterial = TextureLoader.CreateTextureMaterial(@"Resources\uvchecker2.jpg", _bitmapIO, scene.GpuDevice, generateMipMaps: true);
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
        }

        public void AddDefaultLights()
        {
            if (_scene == null)
                return;

            _scene.Lights.Clear();


            SetAmbientLight(0);


            _directionalLight1 ??= new DirectionalLight(new Vector3(-1, -0.3f, 0));

            if (!_scene.Lights.Contains(_directionalLight1))
                _scene.Lights.Add(_directionalLight1);


            //_pointLight1 ??= new PointLight(new Vector3(100, 0, -100), range: 10000) { Attenuation = new Vector3(1, 0, 0) };
            _pointLight1 ??= new PointLight(new Vector3(100, 0, -100));

            if (!_scene.Lights.Contains(_pointLight1))
                _scene.Lights.Add(_pointLight1);


            _spotLight1 ??= new SpotLight(new Vector3(300, 0, 200), new Vector3(-1, -0.3f, 0));

            if (!_scene.Lights.Contains(_spotLight1))
                _scene.Lights.Add(_spotLight1);


            UpdateLightModels();
        }

        public void AddDirectionalLight(bool randomColor = true)
        {
            if (_scene == null)
                return;

            // additional lights have random direction
            var newLight = new DirectionalLight(GetRandomDirection());
            if (randomColor)
                newLight.Color = GetRandomColor3();

            _scene.Lights.Add(newLight);
        }

        public void AddPointLight(bool randomColor = true)
        {
            if (_scene == null)
                return;

            // additional lights have random direction
            var newLight = new PointLight(GetRandomPosition());
            if (randomColor)
                newLight.Color = GetRandomColor3();

            _scene.Lights.Add(newLight);

            UpdateLightModels();
        }

        public void AddSpotLight(bool randomColor = true)
        {
            if (_scene == null)
                return;

            var position = GetRandomPosition();
            var direction = Vector3.Normalize(position * -1); // toward the center of the scene
            var newLight = new SpotLight(position, direction);
            if (randomColor)
                newLight.Color = GetRandomColor3();

            _scene.Lights.Add(newLight);

            UpdateLightModels();
        }

        public void SetAmbientLight(float intensity)
        {
            if (_scene == null)
                return;

            if (_ambientLight == null)
                _ambientLight = new AmbientLight(intensity);
            else
                _ambientLight.SetIntensity(intensity);

            if (!_scene.Lights.Contains(_ambientLight))
                _scene.Lights.Add(_ambientLight);

            UpdateAmbientLightTextBlock();
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

        public Vector3 GetRandomPosition(Vector3 centerPosition, Vector3 areaSize)
        {
            var randomPosition = new Vector3((_rnd.NextSingle() - 0.5f) * areaSize.X + centerPosition.X,
                                             (_rnd.NextSingle() - 0.5f) * areaSize.Y + centerPosition.Y,
                                             (_rnd.NextSingle() - 0.5f) * areaSize.Z + centerPosition.Z);

            return randomPosition;
        }

        public Vector3 GetRandomDirection()
        {
            var randomVector = new Vector3(_rnd.NextSingle() * 2 - 1, _rnd.NextSingle() * 2 - 1, _rnd.NextSingle() * 2 - 1);
            randomVector = Vector3.Normalize(randomVector);
            return randomVector;
        }

        public Color3 GetRandomColor3()
        {
            var randomColor = new Color3(_rnd.NextSingle(), _rnd.NextSingle(), _rnd.NextSingle());
            return randomColor;
        }

        private void UpdateLightModels()
        {
            if (_scene == null)
                return;

            if (_lightsGroup == null)
            {
                _lightsGroup = new GroupNode("LightsGroup");
                _scene.RootNode.Add(_lightsGroup);
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


            for (var i = 0; i < _scene.Lights.Count; i++)
            {
                var oneLight = _scene.Lights[i];

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

        private void CreateLights()
        {
            AddDefaultLights();

            UpdateAmbientLightTextBlock();
            UpdateShowCameraLightTextBlock();
        }

        private float GetAmbientLightIntensity()
        {
            if (_ambientLight == null)
                return 0;

            return (_ambientLight.Color.Red + _ambientLight.Color.Green + _ambientLight.Color.Blue) / 3.0f;
        }

        private void UpdateAmbientLightTextBlock()
        {
            var ambientLightIntensity = GetAmbientLightIntensity() * 100;
            AmbientLightTextBlock.Text = $"Ambient light: {ambientLightIntensity:0}%";
        }

        private void UpdateShowCameraLightTextBlock()
        {
            if (_targetPositionCamera == null)
                return;

            ShowCameraLightTextBlock.Text = $"ShowCameraLight: {_targetPositionCamera.ShowCameraLight}";
        }

        private void ChangeShowCameraLightButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_targetPositionCamera == null)
                return;

            var showCameraLightNumber = (int)_targetPositionCamera.ShowCameraLight;

            showCameraLightNumber++;
            showCameraLightNumber %= 3;

            _targetPositionCamera.ShowCameraLight = (ShowCameraLightType)showCameraLightNumber;

            UpdateShowCameraLightTextBlock();
        }

        private void AddDefaultLightsButton_OnClick(object sender, RoutedEventArgs e)
        {
            AddDefaultLights();
        }

        private void AddDirectionalLightButton_OnClick(object sender, RoutedEventArgs e)
        {
            AddDirectionalLight(randomColor: true);
        }

        private void AddPointLightButton_OnClick(object sender, RoutedEventArgs e)
        {
            AddPointLight(randomColor: true);
        }

        private void AddSpotLightButton_OnClick(object sender, RoutedEventArgs e)
        {
            AddSpotLight(randomColor: true);
        }

        private void ChangeAmbientLightButton_OnClick(object sender, RoutedEventArgs e)
        {
            var ambientLightIntensity = GetAmbientLightIntensity();

            ambientLightIntensity += 0.333f;
            if (ambientLightIntensity > 1)
                ambientLightIntensity = 0;

            SetAmbientLight(ambientLightIntensity);
        }

        private void RemoveAllLightsButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_scene == null)
                return;

            _scene.Lights.Clear();
            UpdateLightModels();
        }
    }
}
