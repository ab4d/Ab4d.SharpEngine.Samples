using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ab4d.SharpEngine.Effects;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.SharpEngine.Wpf;
using Ab4d.SharpEngine.glTF.Schema;
using Ab4d.SharpEngine.Samples.Common;
using Colors = Ab4d.SharpEngine.Common.Colors;

namespace Ab4d.SharpEngine.Samples.Wpf.Advanced
{
    /// <summary>
    /// Interaction logic for MultipleSceneViewsSample.xaml
    /// </summary>
    public partial class MultipleSceneViewsSample : Page
    {
        private List<SharpEngineSceneView> _sceneViews = new ();

        private VulkanDevice? _gpuDevice;
        private Scene? _mainScene;

        public MultipleSceneViewsSample()
        {
            InitializeComponent();

            InitializeMainScene();
            CreateSceneViews(columnsCount: 2, rowsCount: 1);
        }
        
        private void InitializeMainScene()
        {
            Ab4d.SharpEngine.Utilities.Log.LogLevel = LogLevels.Warn;
            Ab4d.SharpEngine.Utilities.Log.IsLoggingToDebugOutput = true;

            var engineCreateOptions = new EngineCreateOptions(enableStandardValidation: true);
            engineCreateOptions.RequiredDeviceExtensionNames.Add("VK_KHR_external_memory_win32");

            _gpuDevice = VulkanDevice.Create(engineCreateOptions);

            if (_gpuDevice == null)
                return; // Cannot create VulkanDevice


            _mainScene = new Scene(_gpuDevice, "SharedScene");

            CreateSceneObjects(_mainScene);

            this.Unloaded += (sender, args) =>
            {
                ViewsGrid.Children.Clear();

                foreach (var sharpEngineSceneView in _sceneViews)
                    sharpEngineSceneView.Dispose();

                // Also dispose the _mainScene and _gpuDevice that were created here
                _mainScene.Dispose();
                _gpuDevice.Dispose();
            };
        }

        private void CreateSceneViews(int columnsCount, int rowsCount)
        {
            if (_mainScene == null)
                return;

            for (int i = 0; i < columnsCount; i++)
                ViewsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            for (int i = 0; i < rowsCount; i++)
                ViewsGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });

            if (columnsCount > 1)
            {
                for (int i = 0; i < columnsCount - 1; i++)
                {
                    var verticalGridSplitter = new GridSplitter()
                    {
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        Width = 2,
                        Background = Brushes.Gray,
                    };

                    Grid.SetColumn(verticalGridSplitter, i);

                    if (rowsCount > 1)
                    {
                        Grid.SetRow(verticalGridSplitter, 0);
                        Grid.SetRowSpan(verticalGridSplitter, rowsCount);
                    }

                    ViewsGrid.Children.Add(verticalGridSplitter);
                }
            }

            if (rowsCount > 1)
            {
                for (int i = 0; i < rowsCount - 1; i++)
                {
                    var horizontalGridSplitter = new GridSplitter()
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Height = 2,
                        Background = Brushes.Gray,
                    };

                    Grid.SetRow(horizontalGridSplitter, i);

                    if (columnsCount > 1)
                    {
                        Grid.SetColumn(horizontalGridSplitter, 0);
                        Grid.SetColumnSpan(horizontalGridSplitter, columnsCount);
                    }

                    ViewsGrid.Children.Add(horizontalGridSplitter);
                }
            }

            // Add SharpEngineSceneView objects
            int usedColumnsCount = columnsCount > 1 ? columnsCount : 1;
            int usedRowsCount    = rowsCount > 1 ? rowsCount : 1;


            //int index = 1; // This is used to create different RenderingTypes; start with wireframe

            
            var initialCameraSettings = new (float heading, float attitude)[]
                {
                    (30, -30),
                    (0, 0),
                    (0, -90),
                    (90, 0)
                };

            for (int i = 0; i < usedRowsCount; i++)
            {
                for (int j = 0; j < usedColumnsCount; j++)
                {
                    var sharpEngineSceneView = new SharpEngineSceneView(_mainScene, $"SceneView_{i + 1}_{j + 1}");

                    var (cameraHeading, cameraAttitude) = initialCameraSettings[(i * usedColumnsCount + j) % initialCameraSettings.Length];

                    SetupPointerCameraController(sharpEngineSceneView, cameraHeading, cameraAttitude);

                    if (i == 0 && j == 0)
                        ((TargetPositionCamera)sharpEngineSceneView.SceneView.Camera).ShowCameraLight = ShowCameraLightType.Always;

                    if (i == 1 && j == 0)
                    {
                        var wireframeRenderingEffectTechnique = new WireframeRenderingEffectTechnique(sharpEngineSceneView.Scene, "CustomWireframeRenderingEffectTechnique")
                        {
                            UseLineColorFromDiffuseColor = true,

                            LineColor = Color4.Black,
                            LineThickness = 1,

                            // Use default values:
                            DepthBias = 0,
                            LinePattern = 0,
                            LinePatternScale = 1,
                            LinePatternOffset = 0,
                        };

                        sharpEngineSceneView.SceneView.DefaultRenderObjectsRenderingStep!.OverrideEffectTechnique = wireframeRenderingEffectTechnique;
                    }

                    Grid.SetColumn(sharpEngineSceneView, j);
                    Grid.SetRow(sharpEngineSceneView, i);
                    ViewsGrid.Children.Insert(0, sharpEngineSceneView); // Insert before GridSplitters

                    _sceneViews.Add(sharpEngineSceneView);
                }
            }
        }
                
        private void SetupPointerCameraController(SharpEngineSceneView sharpEngineSceneView, float cameraHeading, float cameraAttitude)
        {
            // Define the camera
            var camera = new TargetPositionCamera()
            {
                Heading = cameraHeading,
                Attitude = cameraAttitude,
                Distance = 600,
                TargetPosition = new Vector3(0, 0, 0),
                ShowCameraLight = ShowCameraLightType.Never // If there are no other light in the Scene, then add a camera light that illuminates the scene from the camera's position
            };

            sharpEngineSceneView.SceneView.Camera = camera;


            // PointerCameraController use pointer or mouse to control the camera
            _ = new PointerCameraController(sharpEngineSceneView)
            {
                RotateCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed,                                                       // this is already the default value but is still set up here for clarity
                MoveCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.ControlKey,               // this is already the default value but is still set up here for clarity
                QuickZoomConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.RightPointerButtonPressed, // quick zoom is disabled by default

                RotateAroundPointerPosition = false,
                ZoomMode = CameraZoomMode.ViewCenter,
            };
        }

        private void CreateSceneObjects(Scene scene)
        {
            //// The 3D objects in SharpEngine are defined in a hierarchical collection of SceneNode objects
            //// that are added to the Scene.RootNode object.
            //// The SceneNode object are defined in the Ab4d.SharpEngine.SceneNodes namespace.

            for (int i = 0; i < 5; i++)
            {
                var boxModel = new BoxModelNode(centerPosition: new Vector3(i * 100, 0, 0),
                                                size: new Vector3(80, 40, 60),
                                                name: "Gold BoxModel")
                {
                    Material = StandardMaterials.Gold.SetOpacity(0.3f),
                    //Material = new StandardMaterial(Colors.Gold),
                    //Material = new StandardMaterial(diffuseColor: new Color3(1f, 0.84313726f, 0f))
                };

                scene.RootNode.Add(boxModel);
            }

            //var testScene = TestScenes.GetTestScene(TestScenes.StandardTestScenes.HouseWithTrees, new Vector3(0, -10, 0), PositionTypes.Bottom | PositionTypes.Center, finalSize: new Vector3(400, 400, 400));
            //scene.RootNode.Add(testScene);


            //// Add lights
            //_directionalLight = new Ab4d.SharpEngine.Lights.DirectionalLight(new Vector3(-1, -0.3f, 0));
            _directionalLight = new Ab4d.SharpEngine.Lights.DirectionalLight(new Vector3(0.3f, -1f, 0));
            scene.Lights.Add(_directionalLight);

            //scene.Lights.Add(new Ab4d.SharpEngine.Lights.PointLight(new Vector3(500, 200, 100), range: 10000));


            // Set ambient light (illuminates the objects from all directions)
            //scene.SetAmbientLight(intensity: 0.3f);
        }

        Lights.DirectionalLight _directionalLight;

        private void ChangeSceneButton_OnClick(object sender, RoutedEventArgs e)
        {
            //var boxModelNode = new BoxModelNode(new Vector3(0, _mainScene.RootNode.Count * 40, 0), new Vector3(50, 20, 50), material: StandardMaterials.Green);
            //_mainScene.RootNode.Add(boxModelNode);

            //_directionalLight.Color = Colors.Red;
            if (_directionalLight != null)
                _directionalLight.Direction = new Vector3(_directionalLight.Direction.Z, _directionalLight.Direction.Y, _directionalLight.Direction.X);

            //_mainScene.SetAmbientLight(intensity: _mainScene.GetAmbientLightIntensity() + 0.2f);
        }
    }
}
