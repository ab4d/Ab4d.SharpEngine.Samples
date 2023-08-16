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
using Ab4d.SharpEngine.AvaloniaUI;
using Ab4d.SharpEngine.Samples.AvaloniaUI.Common;
using Ab4d.SharpEngine.Vulkan;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Cyotek.Drawing.BitmapFont;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.CameraControllers
{
    /// <summary>
    /// Interaction logic for MouseCameraControllerSample.xaml
    /// </summary>
    public partial class MouseCameraControllerSample : UserControl
    {
        private MouseCameraController? _mouseCameraController;

        private TargetPositionCamera? _targetPositionCamera;

        public MouseCameraControllerSample()
        {
            InitializeComponent();

            MouseMoveThresholdComboBox.ItemsSource = new float[] { 0.0f, 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 10.0f };
            MouseMoveThresholdComboBox.SelectedIndex = 0;

            MouseWheelDistanceChangeFactorComboBox.ItemsSource = new float[] { 1.01f, 1.025f, 1.05f, 1.075f, 1.1f, 1.2f };
            MouseWheelDistanceChangeFactorComboBox.SelectedIndex = 2;

            ToolTip.SetTip(ZoomModeInfoControl, 
@"ViewCenter: Zooms into the center of the SceneView.
CameraRotationCenterPosition: Zooms into the 3D position defined by the TargetPositionCamera.RotationCenterPosition or FreeCamera.RotationCenterPosition property (not defined in this sample).
MousePosition: Zooms into the 3D position that is 'behind' current mouse position. If there is no 3D object behind mouse position, then camera is zoomed into the SceneView's center.");

            ToolTip.SetTip(UseMousePositionForMovementSpeedInfoControl,
@"When UseMousePositionForMovementSpeed is true (CheckBox is checked) then the camera movement speed is determined by the distance to the 3D object behind the mouse. When no 3D object is behind the mouse or when UseMousePositionForMovementSpeed is set to false, then movement speed is determined by the distance from the camera to the TargetPosition is used. Default value is true.");

            ToolTip.SetTip(MouseWheelDistanceChangeFactorInfoControl,
@"MouseWheelDistanceChangeFactor specifies a value that is used when zooming with mouse wheel. When zooming out the Camera's Distance or CameraWidth is multiplied with this value. When zooming in the Camera's Distance or CameraWidth is divided with this value. Default value is 1.05. Bigger value increases the speed of zooming with mouse wheel.");

            ToolTip.SetTip(MaxCameraDistanceInfoControl,
@"When MaxCameraDistance is set to a value that is not float.NaN, than it specifies the maximum Distance of the camera or the maximum CameraWidth when OrthographicCamera is used.
This property can be set to a reasonable number to prevent float imprecision when the camera distance is very big. Default value is float.NaN.");

            ToolTip.SetTip(MouseMoveThresholdInfoControl,
@"This property specifies how much user needs to move the mouse before rotation, movement or quick zoom are started.}

Because MouseCameraController does not handle mouse events until mouse is moved for the specified amount, the events can be get by the user code (for example to handle mouse click; it is not needed to use Preview mouse events for that).

When 0 (by default), then rotation, movement or quick zoom are started immediately when the correct mouse buttons and keyboard modifiers are pressed (no mouse movement needed).");


            SetupMouseCameraController();
            CreateTestScene();
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


            _mouseCameraController = new MouseCameraController(MainSceneView)
            {
                RotateCameraConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed,                                                   // this is already the default value but is still set up here for clarity
                MoveCameraConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed | MouseAndKeyboardConditions.ControlKey,             // this is already the default value but is still set up here for clarity
                QuickZoomConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed | MouseAndKeyboardConditions.RightMouseButtonPressed, // quick zoom is disabled by default
                RotateAroundMousePosition = true,
                ZoomMode = CameraZoomMode.MousePosition
            };

            UpdateRotateCameraConditions();
            UpdateMoveCameraConditions();
            UpdateQuickZoomCameraConditions();
            
            this.Unloaded += delegate (object? sender, RoutedEventArgs args)
            {
                MainSceneView.Dispose();
            };            
        }

        private void CreateTestScene()
        {
            var testScene = Ab4d.SharpEngine.Samples.Common.TestScenes.GetTestScene(Ab4d.SharpEngine.Samples.Common.TestScenes.StandardTestScenes.HouseWithTrees, finalSize: new Vector3(400, 400, 400));
            MainSceneView.Scene.RootNode.Add(testScene);
        }


        private void OnRotateCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            UpdateRotateCameraConditions();
        }

        private void OnMoveCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            UpdateMoveCameraConditions();
        }

        private void OnQuickZoomCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            UpdateQuickZoomCameraConditions();
        }

        private void OnUseMousePositionForMovementSpeedCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (_mouseCameraController == null)
                return;

            _mouseCameraController.UseMousePositionForMovementSpeed = UseMousePositionForMovementSpeedCheckBox.IsChecked ?? false;
        }

        private void UpdateRotateCameraConditions()
        {
            if (_mouseCameraController == null)
                return;

            var rotateConditions = MouseAndKeyboardConditions.Disabled;

            if (LeftButtonCheckBox1.IsChecked ?? false)
                rotateConditions |= MouseAndKeyboardConditions.LeftMouseButtonPressed;

            if (MiddleButtonCheckBox1.IsChecked ?? false)
                rotateConditions |= MouseAndKeyboardConditions.MiddleMouseButtonPressed;

            if (RightButtonCheckBox1.IsChecked ?? false)
                rotateConditions |= MouseAndKeyboardConditions.RightMouseButtonPressed;


            if (ShiftKeyCheckBox1.IsChecked ?? false)
                rotateConditions |= MouseAndKeyboardConditions.ShiftKey;

            if (ControlKeyCheckBox1.IsChecked ?? false)
                rotateConditions |= MouseAndKeyboardConditions.ControlKey;

            if (AltKeyCheckBox1.IsChecked ?? false)
                rotateConditions |= MouseAndKeyboardConditions.AltKey;

            _mouseCameraController.RotateCameraConditions = rotateConditions;
        }

        private void UpdateMoveCameraConditions()
        {
            if (_mouseCameraController == null)
                return;

            var moveConditions = MouseAndKeyboardConditions.Disabled;

            if (LeftButtonCheckBox2.IsChecked ?? false)
                moveConditions |= MouseAndKeyboardConditions.LeftMouseButtonPressed;

            if (MiddleButtonCheckBox2.IsChecked ?? false)
                moveConditions |= MouseAndKeyboardConditions.MiddleMouseButtonPressed;

            if (RightButtonCheckBox2.IsChecked ?? false)
                moveConditions |= MouseAndKeyboardConditions.RightMouseButtonPressed;


            if (ShiftKeyCheckBox2.IsChecked ?? false)
                moveConditions |= MouseAndKeyboardConditions.ShiftKey;

            if (ControlKeyCheckBox2.IsChecked ?? false)
                moveConditions |= MouseAndKeyboardConditions.ControlKey;

            if (AltKeyCheckBox2.IsChecked ?? false)
                moveConditions |= MouseAndKeyboardConditions.AltKey;

            _mouseCameraController.MoveCameraConditions = moveConditions;
        }

        private void UpdateQuickZoomCameraConditions()
        {
            if (_mouseCameraController == null)
                return;

            var quickZoomConditions = MouseAndKeyboardConditions.Disabled;

            if (LeftButtonCheckBox3.IsChecked ?? false)
                quickZoomConditions |= MouseAndKeyboardConditions.LeftMouseButtonPressed;

            if (MiddleButtonCheckBox3.IsChecked ?? false)
                quickZoomConditions |= MouseAndKeyboardConditions.MiddleMouseButtonPressed;

            if (RightButtonCheckBox3.IsChecked ?? false)
                quickZoomConditions |= MouseAndKeyboardConditions.RightMouseButtonPressed;


            if (ShiftKeyCheckBox3.IsChecked ?? false)
                quickZoomConditions |= MouseAndKeyboardConditions.ShiftKey;

            if (ControlKeyCheckBox3.IsChecked ?? false)
                quickZoomConditions |= MouseAndKeyboardConditions.ControlKey;

            if (AltKeyCheckBox3.IsChecked ?? false)
                quickZoomConditions |= MouseAndKeyboardConditions.AltKey;

            _mouseCameraController.QuickZoomConditions = quickZoomConditions;
        }

        private void OnIsMouseWheelZoomEnabledCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_mouseCameraController == null)
                return;

            _mouseCameraController.IsMouseWheelZoomEnabled = IsMouseWheelZoomEnabledCheckBox.IsChecked ?? false;
        }

        private void MouseWheelDistanceChangeFactorComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_mouseCameraController == null)
                return;

            _mouseCameraController.MouseWheelDistanceChangeFactor = (float)MouseWheelDistanceChangeFactorComboBox.SelectedItem;
        }

        private void MouseMoveThresholdComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_mouseCameraController == null)
                return;

            _mouseCameraController.MouseMoveThreshold = (float)MouseMoveThresholdComboBox.SelectedItem;
        }

        private void MaxCameraDistanceComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_mouseCameraController == null)
                return;

            float newMaxCameraDistance;
            switch (MaxCameraDistanceComboBox.SelectedIndex)
            {
                case 1:
                    newMaxCameraDistance = 500;
                    break;

                case 2:
                    newMaxCameraDistance = 1000;
                    break;

                case 3:
                    newMaxCameraDistance = 5000;
                    break;

                case 0:
                default:
                    newMaxCameraDistance = float.NaN;
                    break;
            }

            _mouseCameraController.MaxCameraDistance = newMaxCameraDistance;
        }

        private void OnIsXAxisInvertedCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_mouseCameraController == null)
                return;

            _mouseCameraController.IsXAxisInverted = IsXAxisInvertedCheckBox.IsChecked ?? false;
        }

        private void OnIsYAxisInvertedCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_mouseCameraController == null)
                return;

            _mouseCameraController.IsYAxisInverted = IsYAxisInvertedCheckBox.IsChecked ?? false;
        }

        private void OnRotateAroundMousePositionCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_mouseCameraController == null)
                return;

            _mouseCameraController.RotateAroundMousePosition = RotateAroundMousePositionCheckBox.IsChecked ?? false;
        }

        private void ZoomModeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_mouseCameraController == null)
                return;

            _mouseCameraController.ZoomMode = (CameraZoomMode)ZoomModeComboBox.SelectedIndex;
        }
    }
}
