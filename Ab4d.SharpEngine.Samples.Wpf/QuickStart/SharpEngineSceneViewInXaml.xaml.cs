using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Ab4d.SharpEngine.Samples.Wpf;
using Ab4d.SharpEngine;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Wpf;
using Ab4d.SharpEngine.Samples.Wpf.Common;
using Ab4d.SharpEngine.Transformations;
using System.Windows.Media.Imaging;
using System.IO;
using Ab4d.SharpEngine.PostProcessing;
using Ab4d.SharpEngine.RenderingSteps;
using Ab4d.Vulkan;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Meshes;
using System.Windows.Media.Media3D;
using Colors = System.Windows.Media.Colors;

namespace Ab4d.SharpEngine.Samples.Wpf.QuickStart
{
    /// <summary>
    /// Interaction logic for SharpEngineSceneViewInXaml.xaml
    /// </summary>
    public partial class SharpEngineSceneViewInXaml : Page
    {
        private GroupNode? _groupNode;
        
        private PointerCameraController? _pointerCameraController;

        private TargetPositionCamera? _targetPositionCamera;

        private int _newObjectsCounter;
        private BlackAndWhitePostProcess _blackAndWhitePostProcess;

        public SharpEngineSceneViewInXaml()
        {
#if DEBUG
            // Enable standard validation that provides additional error information when Vulkan SDK is installed on the system.


            // Logging was already enabled in SamplesWindow constructor
            //Utilities.Log.LogLevel = LogLevels.Trace;
            //Utilities.Log.LogFileName = @"c:\temp\SharpEngineFromRenderDoc.log";
            //Utilities.Log.IsLoggingToDebugOutput = true;

            //AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs e)
            //{
            //    System.IO.File.AppendAllText(Utilities.Log.LogFileName, $"EXCEPTION: {e.ExceptionObject.GetType().Name}: {((Exception)e.ExceptionObject).Message}\r\n{((Exception)e.ExceptionObject).StackTrace}");
            //};
#endif

            //Ab4d.SharpEngine.Wpf.Controls.OverlaySurfaceHost.RenderAsManyFramesAsPossible = true;

            InitializeComponent();

            // This sample shows how to create SharpEngineSceneView in XAML.
            // To see how do create SharpEngineSceneView in code, see the SharpEngineSceneViewInCode sample.


            MainSceneView.CreateOptions.PreferredSwapChainImagesCount = 3;
            MainSceneView.CreateOptions.EnableStandardValidation = true;

            // In case when VulkanDevice cannot be created, show an error message
            // If this is not handled by the user, then SharpEngineSceneView will show its own error message
            MainSceneView.GpuDeviceCreationFailed += delegate (object sender, DeviceCreateFailedEventArgs args)
            {
                ShowDeviceCreateFailedError(args.Exception); // Show error message
                args.IsHandled = true;                       // Prevent showing error by SharpEngineSceneView
            };


            MainSceneView.BackgroundColor = Colors.White;

            MainSceneView.PreferredMultiSampleCount = 0;
            MainSceneView.PreferredSupersamplingCount = 4;



            //// Super-sampling:
            //MainSceneView.GpuDeviceCreated += (sender, args) =>
            //{
            //    var downSampleRenderingStep = new ResolveSupersamplingRenderingStep(MainSceneView.SceneView);
            //    MainSceneView.SceneView.RenderingSteps.AddBefore(MainSceneView.SceneView.DefaultCompleteRenderingStep, downSampleRenderingStep);
            //};


            // B&W post-process:
            //MainSceneView.GpuDeviceCreated += (sender, args) =>
            //{
            //    var copyTexturePostProcess = new CopyTexturePostProcess();
            //    MainSceneView.SceneView.PostProcesses.Add(copyTexturePostProcess);

            //    _blackAndWhitePostProcess = new BlackAndWhitePostProcess();
            //    _blackAndWhitePostProcess.ChangeViewport(0.1f, 0.1f, 0.5f, 0.5f);
            //    MainSceneView.SceneView.PostProcesses.Add(_blackAndWhitePostProcess);

            //    var renderTestPostProcessRenderingStep = new RenderPostProcessesRenderingStep(MainSceneView.SceneView);
            //    MainSceneView.SceneView.RenderingSteps.AddBefore(MainSceneView.SceneView.DefaultCompleteRenderingStep, renderTestPostProcessRenderingStep);
            //};


            // We can also manually initialize the SharpEngineSceneView ba calling Initialize method - see commented code below.
            // This would immediately create the VulkanDevice.
            // If this is not done, then Initialize is automatically called when the SharpEngineSceneView is loaded.

            //// Call Initialize method that creates the Vulkan device, Scene and SceneView
            //try
            //{
            //    var gpuDevice = _sharpEngineSceneView.Initialize();
            //}
            //catch (SharpEngineException ex)
            //{
            //    ShowDeviceCreateFailedError(ex);
            //    return;
            //}


            CreateTestScene();
            SetupPointerCameraController();

            this.Unloaded += (sender, args) => MainSceneView.Dispose();
        }

        private void SetupPointerCameraController()
        {
            _targetPositionCamera = new TargetPositionCamera()
            {
                Heading = -40,
                Attitude = -30,
                Distance = 500,
                ViewWidth = 500,
                TargetPosition = new Vector3(0, 0, 0),
                ShowCameraLight = ShowCameraLightType.Always,
            };

            MainSceneView.SceneView.Camera = _targetPositionCamera;


            _pointerCameraController = new PointerCameraController(MainSceneView)
            {
                RotateCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed,                                                       // this is already the default value but is still set up here for clarity
                MoveCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.ControlKey,               // this is already the default value but is still set up here for clarity
                QuickZoomConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.RightPointerButtonPressed, // quick zoom is disabled by default
                ZoomMode = CameraZoomMode.PointerPosition,
                RotateAroundPointerPosition = true
            };
        }

        private void CreateTestScene()
        {
            var planeModelNode = new PlaneModelNode(centerPosition: new Vector3(0, 0, 0), 
                                                    size: new Vector2(400, 300), 
                                                    normal: new Vector3(0, 1, 0), 
                                                    heightDirection: new Vector3(0, 0, -1), 
                                                    name: "BasePlane")
            {
                Material = StandardMaterials.Gray,
                BackMaterial = StandardMaterials.Black
            };

            MainSceneView.Scene.RootNode.Add(planeModelNode);

            
            // Create a GroupNode that will group all created objects
            _groupNode = new GroupNode("GroupNode");
            _groupNode.Transform = new StandardTransform(translateX: 50, translateZ: 30);
            MainSceneView.Scene.RootNode.Add(_groupNode);
            
            for (int i = 1; i <= 8; i++)
            {
                var boxModelNode = new BoxModelNode($"BoxModelNode_{i}")
                {
                    Position = new Vector3(-240 + i * 40, 5, 50),
                    PositionType = PositionTypes.Bottom,
                    Size = new Vector3(30, 20, 50),
                    Material = new StandardMaterial(new Color3(1f, i * 0.0625f + 0.5f, i * 0.125f)), // orange to white
                };

                _groupNode.Add(boxModelNode);


                var sphereModelNode = new SphereModelNode($"SphereModelNode_{i}")
                {
                    CenterPosition = new Vector3(-240 + i * 40, 20, -10),
                    Radius = 15,
                    Material = new StandardMaterial(new Color3(1f, i * 0.0625f + 0.5f, i * 0.125f)), // orange to white
                };

                _groupNode.Add(sphereModelNode);
            }


            AddLinesFan(_groupNode, startPosition: new Vector3(-210, -5, 0), lineThickness: 0.6f, linesLength: 100);
            AddLinesFan(_groupNode, startPosition: new Vector3(-50, -5, 0), lineThickness: 0.8f, linesLength: 100);
        }

        private void ShowDeviceCreateFailedError(Exception ex)
        {
            var errorTextBlock = new TextBlock()
            {
                Text = "Error creating VulkanDevice:\r\n" + ex.Message,
                Foreground = Brushes.Red,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            RootGrid.Children.Add(errorTextBlock);
        }

        private void AddNewButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_groupNode == null)
                return;

            var boxModelNode = new BoxModelNode($"BoxModelNode_{_newObjectsCounter}")
            {
                Position = new Vector3(-140, _newObjectsCounter * 30 + 20, -100),
                Size = new Vector3(50, 20, 50),
                Material = StandardMaterials.Gold,
            };

            _groupNode.Add(boxModelNode);

            _newObjectsCounter++;
        }
        
        private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_groupNode == null || _groupNode.Count == 0)
                return;

            _groupNode.RemoveAt(_groupNode.Count -1);

            if (_newObjectsCounter > 0)
                _newObjectsCounter--;
        }

        private void ChangeBackgroundButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (MainSceneView == null)
                return;

            MainSceneView.BackgroundColor = WpfSamplesContext.Current.GetRandomWpfColor();
        }
        
        private void ChangeMaterial1Button_OnClick(object sender, RoutedEventArgs e)
        {
            if (_groupNode == null || _groupNode.Count == 0)
                return;

            var index = WpfSamplesContext.Current.GetRandomInt(_groupNode.Count - 1);

            if (_groupNode[index] is ModelNode modelNode)
            {
                if (modelNode.Material is StandardMaterial standardMaterial)
                    standardMaterial.DiffuseColor = WpfSamplesContext.Current.GetRandomColor3();
            }
        }
        
        private void ChangeMaterial2Button_OnClick(object sender, RoutedEventArgs e)
        {
            if (_groupNode == null || _groupNode.Count == 0)
                return;

            var index = WpfSamplesContext.Current.GetRandomInt(_groupNode.Count - 1);

            if (_groupNode[index] is ModelNode modelNode)
            {
                modelNode.Material = WpfSamplesContext.Current.GetRandomStandardMaterial();
            }
        }

        private void RenderToBitmapButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Call SharpEngineSceneView.RenderToBitmap to the get WPF's WritableBitmap.
            // This will create a new WritableBitmap on each call. To reuse the WritableBitmap,
            // call the RenderToBitmap and pass the WritableBitmap by ref as the first parameter.
            // It is also possible to call SceneView.RenderToXXXX methods - this give more low level bitmap objects.
            var renderedBitmap = MainSceneView.RenderToBitmap(renderNewFrame: true);

            if (renderedBitmap == null)
                return;


            string fileName = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SharpEngine.png");

            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                PngBitmapEncoder enc = new PngBitmapEncoder();
                BitmapFrame bitmapImage = BitmapFrame.Create(renderedBitmap, null, null, null);
                enc.Frames.Add(bitmapImage);
                enc.Save(fs);
            }

            System.Diagnostics.Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
        }

        private void ResizeButton_OnClick(object sender, RoutedEventArgs e)
        {
            _blackAndWhitePostProcess.ChangeViewport(_blackAndWhitePostProcess.ViewportOffset.X + 0.1f, 0.1f, 0.5f, 0.5f);

            //if (_blackAndWhitePostProcess.IsFullScreenPostProcess)
            //    _blackAndWhitePostProcess.ChangeViewport(0.1f, 0.1f, 0.5f, 0.5f);
            //else
            //    _blackAndWhitePostProcess.ChangeViewport(0f, 0f, 1f, 1f);

            //Window.GetWindow(this).Width += 100;
        }

        private void AddLinesFan(GroupNode parentNode, Vector3 startPosition, float lineThickness, float linesLength, string titleText = null)
        {
            var linePositions = new List<Vector3>();

            for (int a = 0; a <= 90; a += 5)
            {
                Vector3 endPosition = startPosition + new Vector3(linesLength * MathF.Cos(a / 180.0f * MathF.PI), linesLength * MathF.Sin(a / 180.0f * MathF.PI), 0);

                linePositions.Add(startPosition);
                linePositions.Add(endPosition);
            }

            var multiLineVisual3D = new MultiLineNode()
            {
                Positions     = linePositions.ToArray(),
                LineColor     = Color4.Black,
                LineThickness = lineThickness
            };

            parentNode.Add(multiLineVisual3D);
        }
    }
}
