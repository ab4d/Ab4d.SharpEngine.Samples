using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Effects;
using Ab4d.SharpEngine.OverlayPanels;
using Ab4d.SharpEngine.RenderingLayers;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.SharpEngine.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ab4d.SharpEngine.Materials;

namespace Ab4d.SharpEngine.Samples.WinForms.Advanced
{
    public partial class MultipleSceneViewsSample : UserControl
    {
        private List<SharpEngineSceneView> _sceneViews = new();

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

        private readonly object[] RenderingTypeStrings = new object[] { "Standard", "Wirefame (colored lines)", "Wirefame (black lines)", "Filer by RenderingQueue", "Filter by object name" };


        public MultipleSceneViewsSample()
        {
            InitializeComponent();

            if (!DesignMode)
            {
                InitializeMainScene();
                CreateSceneViews();
            }
        }


        private void InitializeMainScene()
        {
            //Ab4d.SharpEngine.Utilities.Log.LogLevel = LogLevels.Warn;
            //Ab4d.SharpEngine.Utilities.Log.IsLoggingToDebugOutput = true;

            //
            // Create VulkanDevice
            //

            var engineCreateOptions = new EngineCreateOptions(enableStandardValidation: SamplesForm.EnableStandardValidation);

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

            this.HandleDestroyed += (sender, args) =>
            {
                this.Controls.Clear();

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

            var initialCameraSettings = new (float heading, float attitude, float distance, RenderingTypes renderingType)[]
                {
                    (30, -30, 600, RenderingTypes.Standard),
                    (0,  -90, 800, RenderingTypes.WireframeBlack),
                    (30, -30, 600, RenderingTypes.FilerByRenderingQueue),
                    (0,    0, 600, RenderingTypes.WireframeColored),
                };



            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2
            };

            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 50));


            for (int rowIndex = 0; rowIndex < 2; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < 2; columnIndex++)
                {
                    var sharpEngineSceneView = new SharpEngineSceneView(_mainScene, $"SceneView_{rowIndex + 1}_{columnIndex + 1}");

                    sharpEngineSceneView.MultisampleCount = 1;
                    sharpEngineSceneView.SupersamplingCount = 1;
                    sharpEngineSceneView.CreateOptions.PreferredSwapChainImagesCount = 3;
                    sharpEngineSceneView.CreateOptions.PreferredInFlightFramesCount = 3;

                    var (cameraHeading, cameraAttitude, distance, renderingType) = initialCameraSettings[(rowIndex * 2 + columnIndex) % initialCameraSettings.Length];

                    var camera = SetupPointerCameraController(sharpEngineSceneView, cameraHeading, cameraAttitude, distance);

                    if (renderingType != RenderingTypes.Standard)
                        SetSpecialRenderingType(sharpEngineSceneView, renderingType);


                    var viewPanel = new Panel()
                    {
                        Dock = DockStyle.Fill,
                        Margin = new Padding(2),
                        BorderStyle = BorderStyle.FixedSingle
                    };

                    sharpEngineSceneView.Dock = DockStyle.Fill;


                    var comboBox = new ComboBox()
                    {
                        Left = 10,
                        Top = 5,
                        Width = 300,
                        Height = 40,
                        DropDownStyle = ComboBoxStyle.DropDownList
                    };

                    comboBox.Items.AddRange(RenderingTypeStrings);
                    comboBox.SelectedItem = RenderingTypeStrings[(int)renderingType];

                    comboBox.SelectedIndexChanged += (sender, args) =>
                    {
                        var newRenderingType = (RenderingTypes)comboBox.SelectedIndex;
                        SetSpecialRenderingType(sharpEngineSceneView, newRenderingType);
                    };


                    var button = new Button()
                    {
                        AutoSize = true,
                        Text = "Camera rotation",
                        Left = 320,
                        Top = 5,
                    };

                    button.Click += (sender, args) =>
                    {
                        if (camera.IsRotating)
                            camera.StopRotation();
                        else
                            camera.StartRotation(50);
                    };

                    viewPanel.Controls.Add(button);
                    viewPanel.Controls.Add(comboBox);
                    viewPanel.Controls.Add(sharpEngineSceneView);

                    table.Controls.Add(viewPanel);
                }
            }

            panel2.Controls.Add(table);
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

        private TargetPositionCamera SetupPointerCameraController(SharpEngineSceneView sharpEngineSceneView, float cameraHeading, float cameraAttitude, float distance)
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

            return camera;
        }

        private void CreateTestScene(Scene scene)
        {
            string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\Models\house with trees.obj");

            var objImporter = new ObjImporter(scene);
            _testScene = objImporter.Import(fileName);

            ModelUtils.PositionAndScaleSceneNode(_testScene,
                                                 position: new Vector3(0, -10, 0),
                                                 positionType: PositionTypes.Center,
                                                 finalSize: new Vector3(400, 400, 400));

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
        
        private void button1_Click(object sender, EventArgs e)
        {
            // Adjusting transformation requires just a change in the matrices buffer and then the existing command buffer can be reused without regeneration.
            if (_manTransform != null)
                _manTransform.Z -= 2;
        }
        
        private void button2_Click(object sender, EventArgs e)
        {
            // Adding new object will require all commands buffers to regenerate
            if (_mainScene != null)
                _mainScene.RootNode.Add(new BoxModelNode(new Vector3(0, -40 - _mainScene.RootNode.Count * 20, 0), new Vector3(400, 5, 400), StandardMaterials.Green));
        }
    }
}
