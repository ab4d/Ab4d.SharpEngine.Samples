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
using System.Windows.Media.Animation;
using Ab4d.SharpEngine.PostProcessing;
using Ab4d.SharpEngine.RenderingSteps;
using Ab4d.Vulkan;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Meshes;
using System.Windows.Media.Media3D;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Colors = System.Windows.Media.Colors;

namespace Ab4d.SharpEngine.Samples.Wpf.QuickStart
{
    /// <summary>
    /// Interaction logic for SupersamplingSample.xaml
    /// </summary>
    public partial class SupersamplingSample : Page
    {
        private GroupNode? _groupNode;
        
        private PointerCameraController? _pointerCameraController;

        private TargetPositionCamera? _targetPositionCamera;

        //public SharpEngineSceneView? MainSceneView;
        //private TextBlockFactory _textBlockFactory;

        private List<SharpEngineSceneView> _sharpEngineSceneViews = new ();
        private List<TextBlock> _infoTexts = new ();
        //private SharpEngineSceneView? _customSettingsSharpEngineSceneView;

        private float _dpiScale;

        private float _lineThickness = 1;

        private readonly int[] _possibleMultiSamplingValues = new int[] { 0, 2, 4, 8 };
        private readonly float[] _possibleSuperSamplingValues = new float[] { 1, 2, 3, 4, 9, 16 };

        public SupersamplingSample()
        {
            // Logging was already enabled in SamplesWindow constructor
            Utilities.Log.LogLevel = LogLevels.Warn;
            Utilities.Log.IsLoggingToDebugOutput = true;


            InitializeComponent();

            // This sample shows how to create SharpEngineSceneView in XAML.
            // To see how do create SharpEngineSceneView in code, see the SharpEngineSceneViewInCode sample.

            //CreateMainSceneView();

            LineThicknessComboBox.ItemsSource = new float[] { 0.4f, 0.6f, 0.8f, 1f, 2f, 3f };
            LineThicknessComboBox.SelectedIndex = 3;

            (_dpiScale, _) = SharpEngineSceneView.GetDpiScale(this);


            var sharpEngineSceneView = AddSharpEngineSceneView(gpuDevice: null, columnIndex: 0, rowIndex: 1, multiSampleCount: 0, supersamplingCount: 1);

            sharpEngineSceneView.Initialize();

            if (sharpEngineSceneView.GpuDevice != null)
            {
                AddSharpEngineSceneView(gpuDevice: sharpEngineSceneView.GpuDevice, columnIndex: 1, rowIndex: 1, multiSampleCount: 4, supersamplingCount: 1);
                AddSharpEngineSceneView(gpuDevice: sharpEngineSceneView.GpuDevice, columnIndex: 0, rowIndex: 2, multiSampleCount: 4, supersamplingCount: 2);
                AddSharpEngineSceneView(gpuDevice: sharpEngineSceneView.GpuDevice, columnIndex: 1, rowIndex: 2, multiSampleCount: 4, supersamplingCount: 4);
            }


            //var sharpEngineSceneView = AddSharpEngineSceneView(gpuDevice: null, columnIndex: 0, multiSampleCount: 0, supersamplingCount: 1, 
            //                                                   title: "No multi-sampling (MSAA)\nNo super-sampling (SSAA)",
            //                                                   subTitle: "Default for software rendering");

            //sharpEngineSceneView.Initialize();

            //if (sharpEngineSceneView.GpuDevice != null)
            //{
            //    AddSharpEngineSceneView(gpuDevice: sharpEngineSceneView.GpuDevice, columnIndex: 1, multiSampleCount: 4, supersamplingCount: 1, 
            //                            title: "4x Multi-sampling (MSAA)\nNo super-sampling (SSAA)",
            //                            subTitle: "Default for software rendering");

            //    _customSettingsSharpEngineSceneView = AddSharpEngineSceneView(gpuDevice: sharpEngineSceneView.GpuDevice, columnIndex: 2, multiSampleCount: 4, supersamplingCount: 4, 
            //                                                                  title: "4x multi-sampling\n4x super-sampling",
            //                                                                  subTitle: "Default for software rendering");
            //}


            this.Unloaded += (sender, args) =>
            {
                // Dispose in reverse order so the GpuDevice gets disposed last (in the fist created SharpEngineSceneViews)
                for (var i = _sharpEngineSceneViews.Count - 1; i >= 0; i--)
                {
                    var oneSharpEngineSceneView = _sharpEngineSceneViews[i];
                    RootGrid.Children.Remove(oneSharpEngineSceneView);
                    oneSharpEngineSceneView.Dispose();
                }
            };
        }

        private SharpEngineSceneView AddSharpEngineSceneView(VulkanDevice? gpuDevice, int columnIndex, int rowIndex, int multiSampleCount, float supersamplingCount)
        {
            var sharpEngineSceneView = new SharpEngineSceneView($"SharpEngineSceneView_{columnIndex}_{rowIndex}");

            // Enable standard validation that provides additional error information when Vulkan SDK is installed on the system.
            sharpEngineSceneView.CreateOptions.EnableStandardValidation = true;

            //sharpEngineSceneView.BackgroundColor = Colors.White;

            sharpEngineSceneView.PreferredMultiSampleCount   = multiSampleCount;
            sharpEngineSceneView.PreferredSupersamplingCount = supersamplingCount;

            //// TODO: Add DownSampleRenderingStep automatically
            //if (supersamplingCount > 1)
            //{
            //    if (sharpEngineSceneView.SceneView.DefaultCompleteRenderingStep != null)
            //    {
            //        var downSampleRenderingStep = new ResolveSupersamplingRenderingStep(sharpEngineSceneView.SceneView);
            //        sharpEngineSceneView.SceneView.RenderingSteps.AddBefore(sharpEngineSceneView.SceneView.DefaultCompleteRenderingStep, downSampleRenderingStep);
            //    }
            //}

            //MainSceneView.GpuDeviceCreated += OnMainSceneViewOnGpuDeviceCreated;
            

            //sharpEngineSceneView.SceneViewInitialized += (sender, args) =>
            //{
            //    // Wait until SceneView is initialized so we get the correct DpiScale value
            //    CreateTestScene();
            //};

            // Subscribe to SceneRendered to get the render size and final size
            //sharpEngineSceneView.SceneRendered += MainSceneView_SceneRendered;

            
            if (gpuDevice != null)
            {
                sharpEngineSceneView.Initialize(gpuDevice);
            }
            //else
            //{
            //    if (MainSceneView.SceneView.DefaultCompleteRenderingStep != null)
            //    {
            //        var downSampleRenderingStep = new DownSampleRenderingStep(MainSceneView.SceneView);
            //        MainSceneView.SceneView.RenderingSteps.AddBefore(MainSceneView.SceneView.DefaultCompleteRenderingStep, downSampleRenderingStep);
            //    }
            //}




            var innerGrid = new Grid();
            innerGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            innerGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });

            var stackPanel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(5, 3, 0, 0) };
            Grid.SetRow(stackPanel, 0);
            innerGrid.Children.Add(stackPanel);

            Grid.SetRow(sharpEngineSceneView, 1);
            innerGrid.Children.Add(sharpEngineSceneView);


            var msaaTextBlock = new TextBlock() { Text = "MSAA: ", FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 4, 0, 0) };
            stackPanel.Children.Add(msaaTextBlock);
            
            var msaaComboBox = new ComboBox() { ItemsSource = _possibleMultiSamplingValues, VerticalAlignment = VerticalAlignment.Top };
            msaaComboBox.SelectedIndex = Array.IndexOf(_possibleMultiSamplingValues, multiSampleCount);
            msaaComboBox.SelectionChanged += (sender, args) => sharpEngineSceneView.PreferredMultiSampleCount = (int)msaaComboBox.SelectedItem;
            stackPanel.Children.Add(msaaComboBox);
            
            var saaTextBlock = new TextBlock() { Text = "SSAA: ", FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(10, 4, 0, 0) };
            stackPanel.Children.Add(saaTextBlock);
            
            var ssaaComboBox = new ComboBox() { ItemsSource = _possibleSuperSamplingValues, VerticalAlignment = VerticalAlignment.Top };
            ssaaComboBox.SelectedIndex = Array.IndexOf(_possibleSuperSamplingValues, supersamplingCount);
            ssaaComboBox.SelectionChanged += (sender, args) => sharpEngineSceneView.PreferredSupersamplingCount = (float)ssaaComboBox.SelectedItem;
            stackPanel.Children.Add(ssaaComboBox);


            var infoTextBlock = new TextBlock() { VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(10, 4, 0, 0) };
            stackPanel.Children.Add(infoTextBlock);
            _infoTexts.Add(infoTextBlock);

            sharpEngineSceneView.SceneView.FirstFrameRendered += (sender, args) => UpdateInfoText(sharpEngineSceneView, infoTextBlock);
            sharpEngineSceneView.SceneView.ViewResized += (sender, args) => UpdateInfoText(sharpEngineSceneView, infoTextBlock);
            
            var rootBorder = new Border()
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(2)
            };

            rootBorder.Child = innerGrid;

            Grid.SetColumn(rootBorder, columnIndex);
            Grid.SetRow(rootBorder, rowIndex);
            RootGrid.Children.Add(rootBorder);


            //Grid.SetColumn(sharpEngineSceneView, columnIndex);
            //RootGrid.Children.Add(sharpEngineSceneView);

            _sharpEngineSceneViews.Add(sharpEngineSceneView);

            SetupPointerCameraController(sharpEngineSceneView);
            CreateTestScene(sharpEngineSceneView.Scene, _lineThickness);




            //if (title != null || subTitle != null)
            //{
            //    var stackPanel = new StackPanel()
            //    {
            //        Margin = new Thickness(15, 5, 15, 5),
            //        Orientation = Orientation.Vertical
            //    };
                
            //    Grid.SetColumn(stackPanel, columnIndex);
            //    RootGrid.Children.Add(stackPanel);

            //    if (title != null)
            //    {
            //        var textBlock = new TextBlock()
            //        {
            //            Text = title,
            //            FontSize = 16,
            //            FontWeight = FontWeights.Bold
            //        };

            //        stackPanel.Children.Add(textBlock);
            //    }

            //    if (subTitle != null)
            //    {
            //        var textBlock = new TextBlock()
            //        {
            //            Text = subTitle,
            //            FontSize = 13,
            //            Margin = new Thickness(0, 5, 0, 0)
            //        };

            //        stackPanel.Children.Add(textBlock);
            //    }
            //}


            return sharpEngineSceneView;
        }

        //private void UpdateAllInfoTexts()
        //{
        //    for (int i = 0; i < _sharpEngineSceneViews.Count; i++)
        //    {
        //        var sceneView = _sharpEngineSceneViews[i].SceneView;

        //        var multiSampleCount = sceneView.UsedMultiSampleCount;
        //        var supersamplingCount = sceneView.UsedSupersamplingCount;

        //        string infoText = (multiSampleCount == 0 ? "No" : "{multiSampleCount}x") + " multi-sampling + " +
        //                          (supersamplingCount <= 1 ? "No" : "{supersamplingCount}x") + " super-sampling: ";

        //        if (supersamplingCount > 1)
        //            infoText += $"RenderSize: {sceneView.RenderWidth} x {sceneView.RenderHeight}); Final";

        //        infoText += $"Size: {sceneView.Width} x {sceneView.Height})";

        //        _infoTexts[i].Text = infoText;
        //    }
        //}
        
        private void UpdateInfoText(SharpEngineSceneView sharpEngineSceneView, TextBlock infoTextBlock)
        {
            var sceneView = sharpEngineSceneView.SceneView;
            
            if (sceneView.Width == 0 || sceneView.Height == 0) 
                return; // Not yet initialized
            
            var multiSampleCount = sceneView.UsedMultiSampleCount;
            var supersamplingCount = sceneView.UsedSupersamplingCount;

            //string infoText = (multiSampleCount == 0 ? "No" : $"{multiSampleCount}x") + " multi-sampling, " +
            //                  (supersamplingCount <= 1 ? "No" : $"{supersamplingCount}x") + " super-sampling";


            string infoText;
            if (supersamplingCount > 1)
                infoText = $"RenderSize: ({sceneView.RenderWidth} x {sceneView.RenderHeight}) => Final";
            else
                infoText = "Render";

            infoText += $"Size: ({sceneView.Width} x {sceneView.Height})";


            if (multiSampleCount <= 1 && supersamplingCount <= 1)
                infoText += "\nDefault setting for software rendering";
            else if (multiSampleCount == 4 && supersamplingCount <= 1)
                infoText += "\nDefault setting for mobile and low-level devices";
            else if (multiSampleCount == 4 && supersamplingCount == 2)
                infoText += "\nDefault setting for desktop computer with integrated GPU";
            else if (multiSampleCount == 4 && supersamplingCount == 4)
                infoText += "\nDefault setting for a discrete GPU";


            infoTextBlock.Text = infoText;
        }

        //private void OnMainSceneViewOnGpuDeviceCreated(object sender, GpuDeviceCreatedEventArgs args)
        //{
        //    var sharpEngineSceneView = (SharpEngineSceneView)sender;
        //    if (sharpEngineSceneView.SceneView.DefaultCompleteRenderingStep != null)
        //    {
        //        var downSampleRenderingStep = new DownSampleRenderingStep(sharpEngineSceneView.SceneView);
        //        sharpEngineSceneView.SceneView.RenderingSteps.AddBefore(sharpEngineSceneView.SceneView.DefaultCompleteRenderingStep, downSampleRenderingStep);
        //    }
        //}

        //private void CreateMainSceneView()
        //{
        //    if (_pointerCameraController != null)
        //        _pointerCameraController.EventsSourceElement = null; // unsubscribe events from existing PointerCameraController

        //    if (MainSceneView != null)
        //    {
        //        RootBorder.Child = null;
        //        MainSceneView.Dispose();
        //    }

        //    MainSceneView = new SharpEngineSceneView();

        //    // Enable standard validation that provides additional error information when Vulkan SDK is installed on the system.
        //    MainSceneView.CreateOptions.EnableStandardValidation = true;

        //    // In case when VulkanDevice cannot be created, show an error message
        //    // If this is not handled by the user, then SharpEngineSceneView will show its own error message
        //    MainSceneView.GpuDeviceCreationFailed += delegate (object sender, DeviceCreateFailedEventArgs args)
        //    {
        //        ShowDeviceCreateFailedError(args.Exception); // Show error message
        //        args.IsHandled = true;                       // Prevent showing error by SharpEngineSceneView
        //    };


        //    MainSceneView.BackgroundColor = Colors.White;

        //    MainSceneView.PreferredMultiSampleCount   = GetSelectedMSAA();
        //    MainSceneView.PreferredSupersamplingCount = GetSelectedSSAA();

        //    // Super-sampling:
        //    MainSceneView.GpuDeviceCreated += (sender, args) =>
        //    {
        //        if (MainSceneView.SceneView.DefaultCompleteRenderingStep != null)
        //        {
        //            var downSampleRenderingStep = new DownSampleRenderingStep(MainSceneView.SceneView);
        //            MainSceneView.SceneView.RenderingSteps.AddBefore(MainSceneView.SceneView.DefaultCompleteRenderingStep, downSampleRenderingStep);
        //        }
        //    };

        //    MainSceneView.SceneViewInitialized += (sender, args) =>
        //    {
        //        // Wait until SceneView is initialized so we get the correct DpiScale value
        //        CreateTestScene();
        //    };

        //    MainSceneView.SceneRendered += MainSceneView_SceneRendered;


        //    // B&W post-process:
        //    //MainSceneView.GpuDeviceCreated += (sender, args) =>
        //    //{
        //    //    var copyTexturePostProcess = new CopyTexturePostProcess();
        //    //    MainSceneView.SceneView.PostProcesses.Add(copyTexturePostProcess);

        //    //    _blackAndWhitePostProcess = new BlackAndWhitePostProcess();
        //    //    _blackAndWhitePostProcess.ChangeViewport(0.1f, 0.1f, 0.5f, 0.5f);
        //    //    MainSceneView.SceneView.PostProcesses.Add(_blackAndWhitePostProcess);

        //    //    var renderTestPostProcessRenderingStep = new RenderPostProcessesRenderingStep(MainSceneView.SceneView);
        //    //    MainSceneView.SceneView.RenderingSteps.AddBefore(MainSceneView.SceneView.DefaultCompleteRenderingStep, renderTestPostProcessRenderingStep);
        //    //};

        //    RootBorder.Child = MainSceneView;

            
        //    SetupPointerCameraController();
        //}

        //private void MainSceneView_SceneRendered(object? sender, EventArgs e)
        //{
        //    MainSceneView.SceneRendered -= MainSceneView_SceneRendered;

        //    InfoTextBlock.Text = $"{MainSceneView.SceneView.RenderWidth} x {MainSceneView.SceneView.RenderHeight}\n{MainSceneView.SceneView.Width} x {MainSceneView.SceneView.Height}";
        //}


        private void SetupPointerCameraController(SharpEngineSceneView sharpEngineSceneView)
        {
            _targetPositionCamera = new TargetPositionCamera()
            {
                Heading = 0,
                Attitude = 0,
                Distance = 400,
                ViewWidth = 400,
                TargetPosition = new Vector3(0, 0, 0),
                ProjectionType = ProjectionTypes.Orthographic,
            };

            sharpEngineSceneView.SceneView.Camera = _targetPositionCamera;


            _pointerCameraController = new PointerCameraController(sharpEngineSceneView)
            {
                RotateCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed,                                                       // this is already the default value but is still set up here for clarity
                MoveCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.ControlKey,               // this is already the default value but is still set up here for clarity
                QuickZoomConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.RightPointerButtonPressed, // quick zoom is disabled by default
                ZoomMode = CameraZoomMode.PointerPosition,
                RotateAroundPointerPosition = true
            };
        }

        private void CreateTestScene(Scene scene, float lineThickness)
        {
            //_textBlockFactory = new TextBlockFactory(scene);
            //_textBlockFactory.FontSize = 10;

            // Create a GroupNode that will group all created objects
            _groupNode = new GroupNode("GroupNode");
            scene.RootNode.Add(_groupNode);

            
            // Add lines fan
            var linesFan = GetLinesFan(startPosition: new Vector3(-120, 10, 0), lineThickness, linesLength: 100);
            _groupNode.Add(linesFan);


            // Add lines that start from vertical lines and then continue with slightly angled lines
            var angledLines = GetSlightlyAngledLines(startPosition: new Vector3(20, 10, 0), offset: new Vector3(6, 0, 0), lineThickness: 1, linesCount: 16);
            _groupNode.Add(angledLines);

            // Add line circles
            var circleLines = GetCircleLines(centerPosition: new Vector3(0, 0, 0), lineThickness: 1, minRadius: 10, middleRadius: 20, maxRadius: 40);
            circleLines.Transform = new StandardTransform()
            {
                TranslateX = -80,
                TranslateY = -60f,
                TranslateZ = 0,

                ScaleX = 1.5f,
                ScaleY = 1,
                ScaleZ = 1,

                RotateZ = 45
            };
            _groupNode.Add(circleLines);


            // Add spiral with varying line thickness (goes from 0 to maxLineThickness and then back to 0)
            var spiralLines = GetSpiralLines(centerPosition: new Vector3(0, 0, 0), maxLineThickness: 1, outerRadius: 80, lastAngle: 360 * 10);
            spiralLines.Transform = new StandardTransform()
            {
                TranslateX = 60,
                TranslateY = -60f,
                TranslateZ = 0,

                ScaleX = 1.5f,
                ScaleY = 1,
                ScaleZ = 1,

                RotateZ = 45
            };
            _groupNode.Add(spiralLines);

            //float maxLineThickness = 1;
            //float outerRadius = 100;

            //float xCenter = 0;
            //float yCenter = 0;

            //float lastAngle = 360 * 10;

            //Vector3 lastPosition = Vector3.Zero;

            //for (float angle = 0; angle < lastAngle; angle += 10)
            //{
            //    var (sin, cos) = MathF.SinCos(angle / 180 * MathF.PI);

            //    float t = angle / lastAngle;
            //    float r = t * outerRadius;

            //    Vector3 position = new Vector3(sin * r + xCenter, cos * r + yCenter, 0);

            //    float lineThickness = MathF.Sin(t * MathF.PI * 2) * maxLineThickness; // from 0 to maxLineThickness and then back to 0

            //    if (lineThickness > 0.01) // Skip ultra-thin lines
            //    {
            //        var lineNode = new LineNode(lastPosition, position, Color4.Black, lineThickness);
            //        lineNode.Transform = new StandardTransform()
            //        {
            //            TranslateX = -80,
            //            TranslateY = -250f,
            //            TranslateZ = 0,

            //            ScaleX = 1.5f,
            //            ScaleY = 1,
            //            ScaleZ = 1,

            //            RotateZ = 45
            //        };
            //        _groupNode.Add(lineNode);
            //    }

            //    lastPosition = position;
            //}
        }

        private MultiLineNode GetSlightlyAngledLines(Vector3 startPosition, Vector3 offset, float lineThickness, int linesCount)
        {
            var linePositions = new List<Vector3>();

            var position = startPosition;

            for (int i = 0; i <= linesCount; i++)
            {
                linePositions.Add(position);
                linePositions.Add(position + new Vector3(i, 100, 0));
                
                position += offset;
            }

            var multiLineNode = new MultiLineNode()
            {
                Positions     = linePositions.ToArray(),
                LineColor     = Color4.Black,
                LineThickness = lineThickness
            };

            return multiLineNode;
        }

        private GroupNode GetCircleLines(Vector3 centerPosition, float lineThickness, float minRadius, float middleRadius, float maxRadius)
        {
            var groupNode = new GroupNode("CircleLines");

            float radius = minRadius;
            while (radius <= maxRadius)
            {
                var lineArcNode = new CircleLineNode()
                {
                    CenterPosition = centerPosition,
                    Radius = radius,
                    LineColor = Color4.Black,
                    LineThickness = lineThickness,
                    Segments = 60,
                    Normal = new Vector3(0, 0, 1)
                };

                radius += radius <= middleRadius ? 2 : 3;

                groupNode.Add(lineArcNode);
            }

            return groupNode;
        }

        private GroupNode GetSpiralLines(Vector3 centerPosition, float maxLineThickness, float outerRadius, float lastAngle)
        {
            var groupNode = new GroupNode("SpiralLines");

            Vector3 lastPosition = Vector3.Zero;

            for (float angle = 0; angle < lastAngle; angle += 10)
            {
                var (sin, cos) = MathF.SinCos(angle / 180 * MathF.PI);

                float t = angle / lastAngle;
                float r = t * outerRadius;

                Vector3 position = new Vector3(sin * r + centerPosition.X, cos * r + centerPosition.Y, centerPosition.Z);

                float lineThickness = MathF.Sin(t * MathF.PI * 2) * maxLineThickness; // from 0 to maxLineThickness and then back to 0

                if (lineThickness > 0.02) // Skip ultra-thin lines (and also the first line from (0,0,0))
                {
                    var lineNode = new LineNode(lastPosition, position, Color4.Black, lineThickness);
                    groupNode.Add(lineNode);
                }

                lastPosition = position;
            }

            return groupNode;
        }
        
        //private void ShowDeviceCreateFailedError(Exception ex)
        //{
        //    var errorTextBlock = new TextBlock()
        //    {
        //        Text = "Error creating VulkanDevice:\r\n" + ex.Message,
        //        Foreground = Brushes.Red,
        //        VerticalAlignment = VerticalAlignment.Center,
        //        HorizontalAlignment = HorizontalAlignment.Center
        //    };

        //    RootGrid.Children.Add(errorTextBlock);
        //}

        //private void ResetCameraButton_OnClick(object sender, RoutedEventArgs e)
        //{
        //    if (_targetPositionCamera == null)
        //        return;

        //    _targetPositionCamera.Heading = 0;
        //    _targetPositionCamera.Attitude = 0;
        //    _targetPositionCamera.Distance = 800;
        //    _targetPositionCamera.ViewWidth = 800;
        //}

        //private void RenderToBitmapButton_OnClick(object sender, RoutedEventArgs e)
        //{
        //    // Call SharpEngineSceneView.RenderToBitmap to the get WPF's WritableBitmap.
        //    // This will create a new WritableBitmap on each call. To reuse the WritableBitmap,
        //    // call the RenderToBitmap and pass the WritableBitmap by ref as the first parameter.
        //    // It is also possible to call SceneView.RenderToXXXX methods - this give more low level bitmap objects.
        //    var renderedBitmap = MainSceneView.RenderToBitmap(renderNewFrame: true);

        //    if (renderedBitmap == null)
        //        return;


        //    string fileName = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SharpEngine.png");

        //    using (FileStream fs = new FileStream(fileName, FileMode.Create))
        //    {
        //        PngBitmapEncoder enc = new PngBitmapEncoder();
        //        BitmapFrame bitmapImage = BitmapFrame.Create(renderedBitmap, null, null, null);
        //        enc.Frames.Add(bitmapImage);
        //        enc.Save(fs);
        //    }

        //    System.Diagnostics.Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
        //}

        //private void ResizeButton_OnClick(object sender, RoutedEventArgs e)
        //{
        //    _blackAndWhitePostProcess.ChangeViewport(_blackAndWhitePostProcess.ViewportOffset.X + 0.1f, 0.1f, 0.5f, 0.5f);

        //    //if (_blackAndWhitePostProcess.IsFullScreenPostProcess)
        //    //    _blackAndWhitePostProcess.ChangeViewport(0.1f, 0.1f, 0.5f, 0.5f);
        //    //else
        //    //    _blackAndWhitePostProcess.ChangeViewport(0f, 0f, 1f, 1f);

        //    //Window.GetWindow(this).Width += 100;
        //}

        private MultiLineNode GetLinesFan(Vector3 startPosition, float lineThickness, float linesLength)
        {
            //if (titleText == null)
            //    titleText = string.Format(System.Globalization.CultureInfo.InvariantCulture, "LineThickness {0:0.0}:", lineThickness);


            //var textBlockVisual3D = new TextBlockVisual3D()
            //{
            //    Position         = startPosition + new Vector3D(-2, linesLength + 5, 0),
            //    PositionType     = PositionTypes.Bottom | PositionTypes.Left,
            //    FontSize         = 11,
            //    RenderBitmapSize = new Size(1024, 256),
            //    Text             = titleText
            //};

            //parentModelVisual3D.Children.Add(textBlockVisual3D);

            //var textNode = _textBlockFactory.CreateTextBlock(titleText, position: startPosition + new Vector3(-2, linesLength + 5, 0), positionType: PositionTypes.BottomLeft, textAttitude: 90);
            //parentNode.Add(textNode);


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

            return multiLineVisual3D;
        }

        private void MsaaComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (!this.IsLoaded || _customSettingsSharpEngineSceneView == null)
            //    return;

            //_customSettingsSharpEngineSceneView.PreferredMultiSampleCount = GetSelectedMSAA();
        }
        
        private void SsaaComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (!this.IsLoaded || _customSettingsSharpEngineSceneView == null)
            //    return;

            //_customSettingsSharpEngineSceneView.PreferredSupersamplingCount = GetSelectedSSAA();
        }

        private void ResetCamerasButton_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (var sharpEngineSceneView in _sharpEngineSceneViews)
            {
                if (sharpEngineSceneView.SceneView.Camera is TargetPositionCamera targetPositionCamera)
                {
                    targetPositionCamera.Heading = 0;
                    targetPositionCamera.Attitude = 0;
                    targetPositionCamera.ViewWidth = 400;
                }
            }
        }

        private void LineThicknessComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            float newLineThickness = (float)LineThicknessComboBox.SelectedValue;

            foreach (var sharpEngineSceneView in _sharpEngineSceneViews)
            {
                sharpEngineSceneView.Scene.RootNode.ForEachChild<LineBaseNode>(lineNode =>
                {
                    if (lineNode.Parent!.Name != "SpiralLines")
                        lineNode.LineThickness = newLineThickness;
                });

                var spiralLinesGroupNode = sharpEngineSceneView.Scene.RootNode.GetChild<GroupNode>(name: "SpiralLines");
                if (spiralLinesGroupNode != null)
                {
                    spiralLinesGroupNode.ForEachChild<LineNode>(lineNode =>
                    {
                        // Set new LineThickness based on previous line thickness percent
                        var thicknessPercent = lineNode.LineThickness / _lineThickness;
                        lineNode.LineThickness = newLineThickness * thicknessPercent; 
                    });
                }
            }

            _lineThickness = newLineThickness;
        }
    }
}
