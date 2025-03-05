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
        private GesturesCameraController? _gesturesCameraController;

        private TargetPositionCamera? _targetPositionCamera;

        public AvaloniaMultiTouchSample()
        {
            InitializeComponent();

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
                ShowCameraLight = ShowCameraLightType.Always
            };

            MainSceneView.SceneView.Camera = _targetPositionCamera;

            // Create GesturesCameraController that is defined in Common folder
            _gesturesCameraController = new GesturesCameraController(MainSceneView)
            {
                IsPinchGestureEnabled         = PinchGestureCheckBox.IsChecked ?? false,
                IsScrollGestureEnabled        = ScrollGestureCheckBox.IsChecked ?? false,
                RotateCameraWithScrollGesture = RotateWithScrollCheckBox.IsChecked ?? false,

                RotateCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed,                                                       // this is already the default value but is still set up here for clarity
                MoveCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.ControlKey,               // this is already the default value but is still set up here for clarity
                QuickZoomConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.RightPointerButtonPressed, // quick zoom is disabled by default
                ZoomMode = CameraZoomMode.PointerPosition,
                RotateAroundPointerPosition = true
            };
        }

        private void CreateTestScene()
        {
            var planeModelNode = new PlaneModelNode(centerPosition: new Vector3(0, -1, 0), 
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
            if (_gesturesCameraController == null)
                return;

            _gesturesCameraController.IsScrollGestureEnabled = ScrollGestureCheckBox.IsChecked ?? false;
        }
        
        private void PinchGestureCheckBox_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            if (_gesturesCameraController == null)
                return;

            _gesturesCameraController.IsPinchGestureEnabled = PinchGestureCheckBox.IsChecked ?? false;
        }        

        private void RotateWithScrollCheckBox_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            if (_gesturesCameraController == null)
                return;

            _gesturesCameraController.RotateCameraWithScrollGesture = RotateWithScrollCheckBox.IsChecked ?? false;
        }

        private void RotateWithPinchCheckBox_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            if (_gesturesCameraController == null)
                return;

            _gesturesCameraController.RotateWithPinchGesture = RotateWithPinchCheckBox.IsChecked ?? false;
        }
    }
}
