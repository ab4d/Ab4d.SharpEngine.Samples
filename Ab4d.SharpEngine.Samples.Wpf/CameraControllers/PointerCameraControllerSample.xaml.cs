﻿using System;
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

namespace Ab4d.SharpEngine.Samples.Wpf.CameraControllers
{
    /// <summary>
    /// Interaction logic for PointerCameraControllerSample.xaml
    /// </summary>
    public partial class PointerCameraControllerSample : Page
    {
        private PointerCameraController? _pointerCameraController;

        private TargetPositionCamera? _targetPositionCamera;

        public PointerCameraControllerSample()
        {
            InitializeComponent();

            PointerMoveThresholdComboBox.ItemsSource = new float[] { 0.0f, 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 10.0f };
            PointerMoveThresholdComboBox.SelectedIndex = 0;

            PointerWheelDistanceChangeFactorComboBox.ItemsSource = new float[] { 1.01f, 1.025f, 1.05f, 1.075f, 1.1f, 1.2f };
            PointerWheelDistanceChangeFactorComboBox.SelectedIndex = 2;

            ZoomModeInfoControl.InfoText =
@"ViewCenter: Zooms into the center of the SceneView.
CameraRotationCenterPosition: Zooms into the 3D position defined by the TargetPositionCamera.RotationCenterPosition or FreeCamera.RotationCenterPosition property (not defined in this sample).
PointerPosition: Zooms into the 3D position that is 'behind' current mouse position. If there is no 3D object behind mouse position, then camera is zoomed into the SceneView's center.";

            UsePointerPositionForMovementSpeedInfoControl.InfoText =
@"When UsePointerPositionForMovementSpeed is true (CheckBox is checked) then the camera movement speed is determined by the distance to the 3D object behind the mouse. When no 3D object is behind the mouse or when UsePointerPositionForMovementSpeed is set to false, then movement speed is determined by the distance from the camera to the TargetPosition is used. Default value is true.";

            MaxCameraDistanceInfoControl.InfoText =
@"When MaxCameraDistance is set to a value that is not float.NaN, than it specifies the maximum Distance of the camera or the maximum CameraWidth when OrthographicCamera is used.
This property can be set to a reasonable number to prevent float imprecision when the camera distance is very big. Default value is float.NaN.";

            PointerMoveThresholdInfoControl.InfoText =
@"This property specifies how much user needs to move the mouse before rotation, movement or quick zoom are started.

Because PointerCameraController does not handle mouse events until mouse is moved for the specified amount, the events can be get by the user code (for example to handle mouse click; it is not needed to use Preview mouse events for that).

When 0 (by default), then rotation, movement or quick zoom are started immediately when the correct mouse buttons and keyboard modifiers are pressed (no mouse movement needed).";


            SetupPointerCameraController();
            CreateTestScene();

            this.Unloaded += delegate (object sender, RoutedEventArgs args)
            {
                MainSceneView.Dispose();
            };
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


            _pointerCameraController = new PointerCameraController(MainSceneView)
            {
                RotateCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed,                                                       // this is already the default value but is still set up here for clarity
                MoveCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.ControlKey,               // this is already the default value but is still set up here for clarity
                QuickZoomConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.RightPointerButtonPressed, // quick zoom is disabled by default
                RotateAroundPointerPosition = true,
                ZoomMode = CameraZoomMode.PointerPosition
            };

            UpdateRotateCameraConditions();
            UpdateMoveCameraConditions();
            UpdateQuickZoomCameraConditions();
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

        private void OnUsePointerPositionForMovementSpeedCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (_pointerCameraController == null)
                return;

            _pointerCameraController.UsePointerPositionForMovementSpeed = UsePointerPositionForMovementSpeedCheckBox.IsChecked ?? false;
        }

        private void UpdateRotateCameraConditions()
        {
            if (_pointerCameraController == null)
                return;

            var rotateConditions = PointerAndKeyboardConditions.Disabled;

            if (LeftButtonCheckBox1.IsChecked ?? false)
                rotateConditions |= PointerAndKeyboardConditions.LeftPointerButtonPressed;

            if (MiddleButtonCheckBox1.IsChecked ?? false)
                rotateConditions |= PointerAndKeyboardConditions.MiddlePointerButtonPressed;

            if (RightButtonCheckBox1.IsChecked ?? false)
                rotateConditions |= PointerAndKeyboardConditions.RightPointerButtonPressed;


            if (ShiftKeyCheckBox1.IsChecked ?? false)
                rotateConditions |= PointerAndKeyboardConditions.ShiftKey;

            if (ControlKeyCheckBox1.IsChecked ?? false)
                rotateConditions |= PointerAndKeyboardConditions.ControlKey;

            if (AltKeyCheckBox1.IsChecked ?? false)
                rotateConditions |= PointerAndKeyboardConditions.AltKey;

            _pointerCameraController.RotateCameraConditions = rotateConditions;
        }

        private void UpdateMoveCameraConditions()
        {
            if (_pointerCameraController == null)
                return;

            var moveConditions = PointerAndKeyboardConditions.Disabled;

            if (LeftButtonCheckBox2.IsChecked ?? false)
                moveConditions |= PointerAndKeyboardConditions.LeftPointerButtonPressed;

            if (MiddleButtonCheckBox2.IsChecked ?? false)
                moveConditions |= PointerAndKeyboardConditions.MiddlePointerButtonPressed;

            if (RightButtonCheckBox2.IsChecked ?? false)
                moveConditions |= PointerAndKeyboardConditions.RightPointerButtonPressed;


            if (ShiftKeyCheckBox2.IsChecked ?? false)
                moveConditions |= PointerAndKeyboardConditions.ShiftKey;

            if (ControlKeyCheckBox2.IsChecked ?? false)
                moveConditions |= PointerAndKeyboardConditions.ControlKey;

            if (AltKeyCheckBox2.IsChecked ?? false)
                moveConditions |= PointerAndKeyboardConditions.AltKey;

            _pointerCameraController.MoveCameraConditions = moveConditions;
        }

        private void UpdateQuickZoomCameraConditions()
        {
            if (_pointerCameraController == null)
                return;

            var quickZoomConditions = PointerAndKeyboardConditions.Disabled;

            if (LeftButtonCheckBox3.IsChecked ?? false)
                quickZoomConditions |= PointerAndKeyboardConditions.LeftPointerButtonPressed;

            if (MiddleButtonCheckBox3.IsChecked ?? false)
                quickZoomConditions |= PointerAndKeyboardConditions.MiddlePointerButtonPressed;

            if (RightButtonCheckBox3.IsChecked ?? false)
                quickZoomConditions |= PointerAndKeyboardConditions.RightPointerButtonPressed;


            if (ShiftKeyCheckBox3.IsChecked ?? false)
                quickZoomConditions |= PointerAndKeyboardConditions.ShiftKey;

            if (ControlKeyCheckBox3.IsChecked ?? false)
                quickZoomConditions |= PointerAndKeyboardConditions.ControlKey;

            if (AltKeyCheckBox3.IsChecked ?? false)
                quickZoomConditions |= PointerAndKeyboardConditions.AltKey;

            _pointerCameraController.QuickZoomConditions = quickZoomConditions;
        }

        private void OnIsPointerWheelZoomEnabledCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_pointerCameraController == null)
                return;

            _pointerCameraController.IsPointerWheelZoomEnabled = IsPointerWheelZoomEnabledCheckBox.IsChecked ?? false;
        }

        private void PointerWheelDistanceChangeFactorComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_pointerCameraController == null)
                return;

            _pointerCameraController.PointerWheelDistanceChangeFactor = (float)PointerWheelDistanceChangeFactorComboBox.SelectedItem;
        }

        private void PointerMoveThresholdComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_pointerCameraController == null)
                return;

            _pointerCameraController.PointerMoveThreshold = (float)PointerMoveThresholdComboBox.SelectedItem;
        }

        private void MaxCameraDistanceComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_pointerCameraController == null)
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

            _pointerCameraController.MaxCameraDistance = newMaxCameraDistance;
        }

        private void OnIsXAxisInvertedCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_pointerCameraController == null)
                return;

            _pointerCameraController.IsXAxisInverted = IsXAxisInvertedCheckBox.IsChecked ?? false;
        }
        
        private void OnIsYAxisInvertedCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_pointerCameraController == null)
                return;

            _pointerCameraController.IsYAxisInverted = IsYAxisInvertedCheckBox.IsChecked ?? false;
        }
            
        private void OnRotateAroundPointerPositionCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_pointerCameraController == null)
                return;

            _pointerCameraController.RotateAroundPointerPosition = RotateAroundPointerPositionCheckBox.IsChecked ?? false;
        }

        private void ZoomModeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_pointerCameraController == null)
                return;

            _pointerCameraController.ZoomMode = (CameraZoomMode)ZoomModeComboBox.SelectedIndex;
        }
    }
}
