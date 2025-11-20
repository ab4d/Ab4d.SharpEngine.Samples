using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Ab4d.SharpEngine.AvaloniaUI;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Effects;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.OverlayPanels;
using Ab4d.SharpEngine.RenderingLayers;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Samples.AvaloniaUI.Common;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Vulkan;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using TranslateTransform = Ab4d.SharpEngine.Transformations.TranslateTransform;

// This sample demonstrates how to show one Scene objects with different SharpEngineSceneView objects.
// This creates new SceneView objects for each SharpEngineSceneView objects.
//
// Know issues:
// - Transparency sorting works only on the first SharpEngineSceneView objects.
//   This means that when rendering semi-transparent objects, they will be correctly shown (sorted) only by the first SharpEngineSceneView.
//   Other SharpEngineSceneView objects may not correctly show the objects behind semi-transparent objects.


namespace Ab4d.SharpEngine.Samples.AvaloniaUI.Advanced
{
    /// <summary>
    /// Interaction logic for MultipleSceneViewsSample.xaml
    /// </summary>
    public partial class MultipleSceneViewsSample : UserControl
    {
        private List<SharpEngineSceneView> _sceneViews = new ();

        private VulkanDevice? _gpuDevice;
        private Scene? _mainScene;

        private GroupNode? _testScene;
        private TranslateTransform? _manTransform;

        private WireframeRenderingEffectTechnique? _wireframeRenderingColorLinesEffectTechnique;
        private WireframeRenderingEffectTechnique? _wireframeRenderingBlackLinesEffectTechnique;
        private RenderingLayer? _customRenderingLayer;

        private enum RenderingTypes
        {
            Standard,
            WireframeColored,
            WireframeBlack,
            FilerByRenderingQueue,
            FilterByObjects
        }

        private readonly string[] RenderingTypeStrings = new string[] {"Standard", "Wirefame (colored lines)", "Wirefame (black lines)", "Filer by RenderingQueue", "Filter by object name"};


        public MultipleSceneViewsSample()
        {
            InitializeComponent();

            InitializeMainScene();
            CreateSceneViews();
        }
        
        private void InitializeMainScene()
        {
            //Ab4d.SharpEngine.Utilities.Log.LogLevel = LogLevels.Warn;
            //Ab4d.SharpEngine.Utilities.Log.IsLoggingToDebugOutput = true;

            //
            // Create VulkanDevice
            //

            var engineCreateOptions = new EngineCreateOptions(enableStandardValidation: SamplesWindow.EnableStandardValidation);

            // Add Vulkan extension names that are required for using SharedTexture in SharpEngineSceneView
            engineCreateOptions.RequiredDeviceExtensionNames.AddRange(SharpEngineSceneView.RequiredDeviceExtensionNamesForSharedTexture);

            _gpuDevice = VulkanDevice.Create(engineCreateOptions);

            if (_gpuDevice == null)
                return; // Cannot create VulkanDevice

            //
            // Create Scene
            //

            _mainScene = new Scene(_gpuDevice, "SharedScene");

            CreateTestScene(_mainScene);

            this.Unloaded += (sender, args) =>
            {
                ViewsGrid.Children.Clear();

                _wireframeRenderingColorLinesEffectTechnique?.Dispose();
                _wireframeRenderingBlackLinesEffectTechnique?.Dispose();

                foreach (var sharpEngineSceneView in _sceneViews)
                    sharpEngineSceneView.Dispose();

                // Also dispose the _mainScene and _gpuDevice that were created here
                _mainScene.Dispose();
                _gpuDevice.Dispose();
            };
        }

        private void CreateSceneViews()
        {
            if (_mainScene == null)
                return;

            for (int columnIndex = 0; columnIndex < 2; columnIndex++)
                ViewsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(columnIndex == 0 ? 2 : 1, GridUnitType.Star) }); // make first row twice as wide as second row

            for (int rowIndex = 0; rowIndex < 2; rowIndex++)
                ViewsGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });

            
            var initialCameraSettings = new (float heading, float attitude, float distance, RenderingTypes renderingType)[]
                {
                    (30, -30, 600, RenderingTypes.Standard),
                    (0,  -90, 800, RenderingTypes.WireframeBlack),
                    (30, -30, 600, RenderingTypes.Standard),     // this cell is skipped
                    (0,    0, 600, RenderingTypes.WireframeColored),
                };

            for (int rowIndex = 0; rowIndex < 2; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < 2; columnIndex++)
                {
                    if (columnIndex == 0 && rowIndex == 1)
                        continue; // The left SharpEngineSceneView has RowSpan set to 2 so skip this cell

                    var sharpEngineSceneView = new SharpEngineSceneView(_mainScene, $"SceneView_{rowIndex + 1}_{columnIndex + 1}");

                    // Apply and advanced settings from the SettingsWindow
                    SamplesWindow.ConfigureSharpEngineSceneViewAction?.Invoke(sharpEngineSceneView);
                    
                    var (cameraHeading, cameraAttitude, distance, renderingType) = initialCameraSettings[(rowIndex * 2 + columnIndex) % initialCameraSettings.Length];

                    SetupPointerCameraController(sharpEngineSceneView, cameraHeading, cameraAttitude, distance);

                    if (renderingType != RenderingTypes.Standard)
                        SetSpecialRenderingType(sharpEngineSceneView, renderingType);


                    Grid.SetColumn(sharpEngineSceneView, columnIndex);
                    Grid.SetRow(sharpEngineSceneView, rowIndex);

                    // Left column uses both rows
                    if (columnIndex == 0 && rowIndex == 0) 
                        Grid.SetRowSpan(sharpEngineSceneView, 2);

                    ViewsGrid.Children.Add(sharpEngineSceneView);

                    _sceneViews.Add(sharpEngineSceneView);


                    var comboBox = new ComboBox()
                    {
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment   = VerticalAlignment.Top,
                        Margin              = new Thickness(0, 3, 5, 0)
                    };

                    comboBox.ItemsSource   = RenderingTypeStrings;
                    comboBox.SelectedIndex = (int)renderingType;

                    comboBox.SelectionChanged += delegate(object? sender, SelectionChangedEventArgs args)
                    {
                        var newRenderingType = (RenderingTypes)comboBox.SelectedIndex;
                        SetSpecialRenderingType(sharpEngineSceneView, newRenderingType);
                    };

                    Grid.SetColumn(comboBox, columnIndex);
                    Grid.SetRow(comboBox, rowIndex);
                    ViewsGrid.Children.Add(comboBox);


                    // Add Title and "Change scene" button to the left cell
                    if (columnIndex == 0 && rowIndex == 0)
                    {
                        var titleTextBlock = new TextBlock()
                        {
                            Text = "Rendering the same Scene with different SharpEngineSceneView objects",
                            FontWeight = FontWeight.Bold,
                            FontSize = 16,
                            Foreground = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                            Margin = new Thickness(10),
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top,
                        };

                        Grid.SetColumn(titleTextBlock, 0);
                        Grid.SetRow(titleTextBlock, 0);
                        ViewsGrid.Children.Add(titleTextBlock);


                        var button = new Button()
                        {
                            Content = "Change scene",
                            Padding = new Thickness(10, 3, 10, 3),
                            Margin = new Thickness(10),
                            HorizontalAlignment = HorizontalAlignment.Right,
                            VerticalAlignment = VerticalAlignment.Bottom,
                        };

                        button.Click += (sender, args) =>
                        {
                            if (_manTransform != null)
                                _manTransform.Z -= 2;
                        };
                    
                        Grid.SetColumn(button, 0);
                        Grid.SetRow(button, 1);
                        ViewsGrid.Children.Add(button);
                    }
                }
            }

            
            var verticalGridSplitter = new GridSplitter()
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Stretch,
                Width = 2,
                Background = Brushes.Gray,
            };

            Grid.SetColumn(verticalGridSplitter, 0);
            Grid.SetRow(verticalGridSplitter, 0);
            Grid.SetRowSpan(verticalGridSplitter, 2);

            ViewsGrid.Children.Add(verticalGridSplitter);
    

            var horizontalGridSplitter = new GridSplitter()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Bottom,
                Height = 2,
                Background = Brushes.Gray,
            };

            Grid.SetColumn(horizontalGridSplitter, 1);
            Grid.SetRow(horizontalGridSplitter, 0);

            ViewsGrid.Children.Add(horizontalGridSplitter);
        }

        private void SetSpecialRenderingType(SharpEngineSceneView sharpEngineSceneView, RenderingTypes renderingType)
        {
            // Reset settings to default:
            var renderObjectsRenderingStep = sharpEngineSceneView.SceneView.DefaultRenderObjectsRenderingStep!;

            renderObjectsRenderingStep.OverrideEffectTechnique = null;
            renderObjectsRenderingStep.FilterObjectsFunction = null;
            renderObjectsRenderingStep.FilterRenderingLayersFunction = null;


            switch (renderingType)
            {
                case RenderingTypes.Standard:
                    // Everything already reset
                    break;

                case RenderingTypes.WireframeColored:
                case RenderingTypes.WireframeBlack:
                    // Override effect to render all objects as wireframes
                    renderObjectsRenderingStep.OverrideEffectTechnique = renderingType == RenderingTypes.WireframeColored ?
                        _wireframeRenderingColorLinesEffectTechnique ??= CreateWireframeRenderingEffectTechnique(sharpEngineSceneView.Scene, useLineColorFromDiffuseColor: true) : 
                        _wireframeRenderingBlackLinesEffectTechnique ??= CreateWireframeRenderingEffectTechnique(sharpEngineSceneView.Scene, useLineColorFromDiffuseColor: false);
                    break;

                case RenderingTypes.FilerByRenderingQueue:
                    // Render only objects in the _customRenderingLayer (we manually set CustomRenderingLayer in CreateTestScene)
                    // Note that this is much faster than using FilterObjectsFunction, because this check is called only
                    // for each RenderingLayer and not for each object.
                    renderObjectsRenderingStep.FilterRenderingLayersFunction = renderingLayer => ReferenceEquals(renderingLayer, _customRenderingLayer);
                    break;

                case RenderingTypes.FilterByObjects:
                    // Render only objects whose name start by "Sphere" or "Cylinder" (trees).
                    // Note that this is much slower than using FilterRenderingLayersFunction because FilterObjectsFunction is called for each object.
                    renderObjectsRenderingStep.FilterObjectsFunction = renderingItem =>
                    {
                        string? sceneNodeName = renderingItem.ParentSceneNode?.Name ?? null;
                        if (sceneNodeName != null)
                            return sceneNodeName.StartsWith("Sphere") || sceneNodeName.StartsWith("Cylinder");
                        return false;
                    };
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(renderingType), renderingType, null);
            }
        }

        private WireframeRenderingEffectTechnique CreateWireframeRenderingEffectTechnique(Scene scene, bool useLineColorFromDiffuseColor) =>
            new WireframeRenderingEffectTechnique(scene, "CustomWireframeRenderingEffectTechnique")
            {
                UseLineColorFromDiffuseColor = useLineColorFromDiffuseColor,

                LineColor = Color4.Black,
                LineThickness = 1,

                // Use default values:
                DepthBias = 0,
                LinePattern = 0,
                LinePatternScale = 1,
                LinePatternOffset = 0,
            };

        private void SetupPointerCameraController(SharpEngineSceneView sharpEngineSceneView, float cameraHeading, float cameraAttitude, float distance)
        {
            // Define the camera
            var camera = new TargetPositionCamera()
            {
                Heading = cameraHeading,
                Attitude = cameraAttitude,
                Distance = distance,
                TargetPosition = new Vector3(0, 0, 0),
                ShowCameraLight = ShowCameraLightType.Never
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

            _ = new CameraAxisPanel(sharpEngineSceneView.SceneView, alignment: PositionTypes.BottomLeft);
        }

        private void CreateTestScene(Scene scene)
        {
            _testScene = TestScenes.GetTestScene(TestScenes.StandardTestScenes.HouseWithTrees, new Vector3(0, -10, 0), PositionTypes.Bottom | PositionTypes.Center, finalSize: new Vector3(400, 400, 400));
            scene.RootNode.Add(_testScene);

            // To see the hierarchy of read objects call testScene.DumpHierarchy() in Visual Studio Immediate Window


            // Create a custom RenderingLayer and move "Box01", "House" and "Roof" to that rendering layer.
            // This is used to demonstrate rendering only objects from selected RenderingLayer - see SetSpecialRenderingType method.
            
            _customRenderingLayer = new RenderingLayer("CustomRenderingQueue");
            scene.AddRenderingLayerAfter(_customRenderingLayer, scene.StandardGeometryRenderingLayer!);

            _testScene.GetChild<MeshModelNode>("Box01")!.CustomRenderingLayer = _customRenderingLayer;
            _testScene.GetChild<MeshModelNode>("House")!.CustomRenderingLayer = _customRenderingLayer;
            _testScene.GetChild<MeshModelNode>("Roof")!.CustomRenderingLayer = _customRenderingLayer;


            // To demonstrate changing the scene, we add TranslateTransform to all Man objects.
            // This transformation is than changed when user clicks on "Change scene" button.

            _manTransform = new TranslateTransform();
            _testScene.ForEachChild<MeshModelNode>("Man*", meshModelNode => meshModelNode.Transform = _manTransform);


            // Add DirectionalLight
            var directionalLight = new Ab4d.SharpEngine.Lights.DirectionalLight(new Vector3(1, -0.4f, -0.2f));
            scene.Lights.Add(directionalLight);

            // Set ambient light (illuminates the objects from all directions)
            scene.SetAmbientLight(intensity: 0.3f);
        }
    }
}
