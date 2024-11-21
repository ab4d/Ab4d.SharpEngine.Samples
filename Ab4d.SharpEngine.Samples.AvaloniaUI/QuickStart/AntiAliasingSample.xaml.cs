using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Windows;
using Ab4d.SharpEngine;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using System.IO;
using System.Runtime.InteropServices;
using Ab4d.SharpEngine.AvaloniaUI;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Samples.AvaloniaUI.Common;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.Vulkan;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.QuickStart
{
    /// <summary>
    /// Interaction logic for AntiAliasingSample.xaml
    /// </summary>
    public partial class AntiAliasingSample : UserControl
    {
        private GroupNode? _groupNode;
        
        private List<SharpEngineSceneView> _sharpEngineSceneViews = new ();
        private List<TextBlock> _infoTexts = new ();

        private float _lineThickness = 1;

        private readonly int[] _possibleMultiSamplingValues = new int[] { 1, 2, 4, 8 };
        private readonly float[] _possibleSuperSamplingValues = new float[] { 1, 2, 3, 4, 9, 16 };

        public AntiAliasingSample()
        {
            InitializeComponent();

            LineThicknessComboBox.ItemsSource = new float[] { 0.4f, 0.6f, 0.8f, 1f, 2f, 3f };
            LineThicknessComboBox.SelectedIndex = 3;

            var sharpEngineSceneView = AddSharpEngineSceneView(gpuDevice: null, columnIndex: 0, rowIndex: 1, multiSampleCount: 1, supersamplingCount: 1);

            sharpEngineSceneView.Initialize();

            if (sharpEngineSceneView.GpuDevice != null)
            {
                AddSharpEngineSceneView(gpuDevice: sharpEngineSceneView.GpuDevice, columnIndex: 1, rowIndex: 1, multiSampleCount: 4, supersamplingCount: 1);
                AddSharpEngineSceneView(gpuDevice: sharpEngineSceneView.GpuDevice, columnIndex: 0, rowIndex: 2, multiSampleCount: 4, supersamplingCount: 2);
                AddSharpEngineSceneView(gpuDevice: sharpEngineSceneView.GpuDevice, columnIndex: 1, rowIndex: 2, multiSampleCount: 4, supersamplingCount: 4);
            }
            
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
            //sharpEngineSceneView.CreateOptions.EnableStandardValidation = true;

            sharpEngineSceneView.MultisampleCount   = multiSampleCount;
            sharpEngineSceneView.SupersamplingCount = supersamplingCount;

            if (gpuDevice != null)
                sharpEngineSceneView.Initialize(gpuDevice);


            var innerGrid = new Grid();
            innerGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            innerGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });

            var stackPanel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(5, 3, 0, 0) };
            Grid.SetRow(stackPanel, 0);
            innerGrid.Children.Add(stackPanel);

            Grid.SetRow(sharpEngineSceneView, 1);
            innerGrid.Children.Add(sharpEngineSceneView);


            var msaaTextBlock = new TextBlock() { Text = "MSAA: ", FontWeight = FontWeight.Bold, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 4, 0, 0) };
            stackPanel.Children.Add(msaaTextBlock);
            
            var msaaComboBox = new ComboBox() { ItemsSource = _possibleMultiSamplingValues, VerticalAlignment = VerticalAlignment.Top };
            msaaComboBox.SelectedIndex = Array.IndexOf(_possibleMultiSamplingValues, multiSampleCount);
            msaaComboBox.SelectionChanged += (sender, args) => sharpEngineSceneView.MultisampleCount = (int)msaaComboBox.SelectedItem;
            stackPanel.Children.Add(msaaComboBox);
            
            var saaTextBlock = new TextBlock() { Text = "SSAA: ", FontWeight = FontWeight.Bold, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(10, 4, 0, 0) };
            stackPanel.Children.Add(saaTextBlock);
            
            var ssaaComboBox = new ComboBox() { ItemsSource = _possibleSuperSamplingValues, VerticalAlignment = VerticalAlignment.Top };
            ssaaComboBox.SelectedIndex = Array.IndexOf(_possibleSuperSamplingValues, supersamplingCount);
            ssaaComboBox.SelectionChanged += (sender, args) => sharpEngineSceneView.SupersamplingCount = (float)ssaaComboBox.SelectedItem;
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


            _sharpEngineSceneViews.Add(sharpEngineSceneView);

            SetupPointerCameraController(sharpEngineSceneView);
            CreateTestScene(sharpEngineSceneView.Scene, _lineThickness);

            return sharpEngineSceneView;
        }

        private void UpdateInfoText(SharpEngineSceneView sharpEngineSceneView, TextBlock infoTextBlock)
        {
            var sceneView = sharpEngineSceneView.SceneView;
            
            if (sceneView.Width == 0 || sceneView.Height == 0) 
                return; // Not yet initialized
            
            var multiSampleCount = sceneView.MultisampleCount;
            var supersamplingCount = sceneView.SupersamplingCount;

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
                infoText += "\nDefault setting for a desktop computer with integrated GPU";
            else if (multiSampleCount == 4 && supersamplingCount == 4)
                infoText += "\nDefault setting for a desktop computer with discrete GPU";

            if (infoText.Contains('\n'))
                infoTextBlock.Margin = new Thickness(10, -2, 0, 0);
            else    
                infoTextBlock.Margin = new Thickness(10, 4, 0, 0);

            infoTextBlock.Text = infoText;
        }

        private void SetupPointerCameraController(SharpEngineSceneView sharpEngineSceneView)
        {
            var targetPositionCamera = new TargetPositionCamera()
            {
                Heading = 0,
                Attitude = 0,
                Distance = 420,
                ViewWidth = 420,
                TargetPosition = new Vector3(0, 0, 0),
                ProjectionType = ProjectionTypes.Orthographic,
            };

            sharpEngineSceneView.SceneView.Camera = targetPositionCamera;

            var pointerCameraController = new PointerCameraController(sharpEngineSceneView)
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
        
        private MultiLineNode GetLinesFan(Vector3 startPosition, float lineThickness, float linesLength)
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

            return multiLineVisual3D;
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
