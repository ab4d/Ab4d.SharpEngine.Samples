using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Samples.AvaloniaUI.Common;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.Advanced
{
    /// <summary>
    /// Interaction logic for AvaloniaMultiTouchSample.xaml
    /// </summary>
    public partial class AvaloniaMultiTouchSample : UserControl
    {
        private GesturesCameraController? _mouseCameraController;

        private TargetPositionCamera? _targetPositionCamera;

        public AvaloniaMultiTouchSample()
        {
            InitializeComponent();

            CreateTestScene();
            SetupMouseCameraController();

            this.Unloaded += (sender, args) => MainSceneView.Dispose();
        }

        private void SetupMouseCameraController()
        {
            _targetPositionCamera = new TargetPositionCamera()
            {
                Heading = -40,
                Attitude = -30,
                Distance = 500,
                ViewWidth = 500,
                TargetPosition = new Vector3(0, 0, 0),
                ShowCameraLight = ShowCameraLightType.Always
            };

            MainSceneView.SceneView.Camera = _targetPositionCamera;

            // Create GesturesCameraController that is defined in Common folder
            _mouseCameraController = new GesturesCameraController(MainSceneView)
            {
                IsPinchGestureEnabled         = PinchGestureCheckBox.IsChecked ?? false,
                IsScrollGestureEnabled        = ScrollGestureCheckBox.IsChecked ?? false,
                RotateCameraWithScrollGesture = RotateWithScrollCheckBox.IsChecked ?? false,

                RotateCameraConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed,                                                   // this is already the default value but is still set up here for clarity
                MoveCameraConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed | MouseAndKeyboardConditions.ControlKey,             // this is already the default value but is still set up here for clarity
                QuickZoomConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed | MouseAndKeyboardConditions.RightMouseButtonPressed, // quick zoom is disabled by default
                ZoomMode = CameraZoomMode.MousePosition,
                RotateAroundMousePosition = true
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


            var boxModel = new BoxModelNode(centerPosition: new Vector3(0, 20, 0), 
                size: new Vector3(80, 40, 60), 
                name: "Gold BoxModel")
            {
                Material = StandardMaterials.Gold,
            };

            MainSceneView.Scene.RootNode.Add(boxModel);
        }

        private void ScrollGestureCheckBox_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            if (_mouseCameraController == null)
                return;

            _mouseCameraController.IsScrollGestureEnabled = ScrollGestureCheckBox.IsChecked ?? false;
        }
        
        private void PinchGestureCheckBox_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            if (_mouseCameraController == null)
                return;

            _mouseCameraController.IsPinchGestureEnabled = PinchGestureCheckBox.IsChecked ?? false;
        }        

        private void RotateWithScrollCheckBox_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            if (_mouseCameraController == null)
                return;

            _mouseCameraController.RotateCameraWithScrollGesture = RotateWithScrollCheckBox.IsChecked ?? false;
        }
    }
}
