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
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.WinForms;

namespace Ab4d.SharpEngine.Samples.WinForms.CameraControllers
{
    public partial class PointerCameraControllerSample : UserControl
    {
        private PointerCameraController? _pointerCameraController;

        private TargetPositionCamera? _targetPositionCamera;

        public PointerCameraControllerSample()
        {
            InitializeComponent();

            zoomModeComboBox.SelectedIndex = 2;

            mouseMoveThresholdComboBox.Items.AddRange(new object[] { 0.0f, 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 10.0f });
            mouseMoveThresholdComboBox.SelectedIndex = 0;

            mouseWheelDistanceChangeFactorComboBox.Items.AddRange(new object[] { 1.01f, 1.025f, 1.05f, 1.075f, 1.1f, 1.2f });
            mouseWheelDistanceChangeFactorComboBox.SelectedIndex = 2;

            maxCameraDistanceComboBox.SelectedIndex = 0;

            CreateTestScene();
            SetupPointerCameraController();

            this.HandleDestroyed += OnHandleDestroyed;
        }

        private void OnHandleDestroyed(object? sender, EventArgs e)
        {
            if (!mainSceneView.IsDisposed)
                mainSceneView.Dispose();
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

            mainSceneView.SceneView.Camera = _targetPositionCamera;


            _pointerCameraController = new PointerCameraController(mainSceneView)
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
            string fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\Models\\house with trees.obj");

            var readerObj = new ReaderObj();
            var readGroupNode = readerObj.ReadSceneNodes(fileName);

            ModelUtils.PositionAndScaleSceneNode(readGroupNode, new Vector3(0, -10, 0), PositionTypes.Bottom | PositionTypes.Center, finalSize: new Vector3(400, 400, 400));

            mainSceneView.Scene.RootNode.Add(readGroupNode);
        }

        private void UpdateRotateCameraConditions()
        {
            if (_pointerCameraController == null)
                return;

            var rotateConditions = PointerAndKeyboardConditions.Disabled;

            if (leftButtonCheckBox1.Checked)
                rotateConditions |= PointerAndKeyboardConditions.LeftPointerButtonPressed;

            if (middleButtonCheckBox1.Checked)
                rotateConditions |= PointerAndKeyboardConditions.MiddlePointerButtonPressed;

            if (rightButtonCheckBox1.Checked)
                rotateConditions |= PointerAndKeyboardConditions.RightPointerButtonPressed;


            if (shiftKeyCheckBox1.Checked)
                rotateConditions |= PointerAndKeyboardConditions.ShiftKey;

            if (controlKeyCheckBox1.Checked)
                rotateConditions |= PointerAndKeyboardConditions.ControlKey;

            if (altKeyCheckBox1.Checked)
                rotateConditions |= PointerAndKeyboardConditions.AltKey;

            _pointerCameraController.RotateCameraConditions = rotateConditions;
        }

        private void UpdateMoveCameraConditions()
        {
            if (_pointerCameraController == null)
                return;

            var moveConditions = PointerAndKeyboardConditions.Disabled;

            if (leftButtonCheckBox2.Checked)
                moveConditions |= PointerAndKeyboardConditions.LeftPointerButtonPressed;

            if (middleButtonCheckBox2.Checked)
                moveConditions |= PointerAndKeyboardConditions.MiddlePointerButtonPressed;

            if (rightButtonCheckBox2.Checked)
                moveConditions |= PointerAndKeyboardConditions.RightPointerButtonPressed;


            if (shiftKeyCheckBox2.Checked)
                moveConditions |= PointerAndKeyboardConditions.ShiftKey;

            if (controlKeyCheckBox2.Checked)
                moveConditions |= PointerAndKeyboardConditions.ControlKey;

            if (altKeyCheckBox2.Checked)
                moveConditions |= PointerAndKeyboardConditions.AltKey;

            _pointerCameraController.MoveCameraConditions = moveConditions;
        }

        private void UpdateQuickZoomCameraConditions()
        {
            if (_pointerCameraController == null)
                return;

            var quickZoomConditions = PointerAndKeyboardConditions.Disabled;

            if (leftButtonCheckBox3.Checked)
                quickZoomConditions |= PointerAndKeyboardConditions.LeftPointerButtonPressed;

            if (middleButtonCheckBox3.Checked)
                quickZoomConditions |= PointerAndKeyboardConditions.MiddlePointerButtonPressed;

            if (rightButtonCheckBox3.Checked)
                quickZoomConditions |= PointerAndKeyboardConditions.RightPointerButtonPressed;


            if (shiftKeyCheckBox3.Checked)
                quickZoomConditions |= PointerAndKeyboardConditions.ShiftKey;

            if (controlKeyCheckBox3.Checked)
                quickZoomConditions |= PointerAndKeyboardConditions.ControlKey;

            if (altKeyCheckBox3.Checked)
                quickZoomConditions |= PointerAndKeyboardConditions.AltKey;

            _pointerCameraController.QuickZoomConditions = quickZoomConditions;
        }

        private void OnRotateCheckBoxChanged(object sender, EventArgs e)
        {
            UpdateRotateCameraConditions();
        }

        private void OnMoveCheckBoxChanged(object sender, EventArgs e)
        {
            UpdateMoveCameraConditions();
        }

        private void OnQuickZoomCheckBoxChanged(object sender, EventArgs e)
        {
            UpdateQuickZoomCameraConditions();
        }

        private void RotateAroundPointerPositionCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_pointerCameraController == null)
                return;

            _pointerCameraController.RotateAroundPointerPosition = rotateAroundMousePositionCheckBox.Checked;
        }

        private void IsXAxisInvertedCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_pointerCameraController == null)
                return;

            _pointerCameraController.IsXAxisInverted = isXAxisInvertedCheckBox.Checked;
        }

        private void IsYAxisInvertedCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_pointerCameraController == null)
                return;

            _pointerCameraController.IsYAxisInverted = isYAxisInvertedCheckBox.Checked;
        }

        private void UsePointerPositionForMovementSpeedCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_pointerCameraController == null)
                return;

            _pointerCameraController.UsePointerPositionForMovementSpeed = useMousePositionForMovementSpeedCheckBox.Checked;
        }

        private void IsPointerWheelZoomEnabledCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_pointerCameraController == null)
                return;

            _pointerCameraController.IsPointerWheelZoomEnabled = isMouseWheelZoomEnabledCheckBox.Checked;
        }

        private void ZoomModeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_pointerCameraController == null)
                return;

            _pointerCameraController.ZoomMode = (CameraZoomMode)zoomModeComboBox.SelectedIndex;
        }

        private void PointerWheelDistanceChangeFactorComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_pointerCameraController == null)
                return;

            var selectedItem = mouseWheelDistanceChangeFactorComboBox.SelectedItem;

            if (selectedItem != null)
                _pointerCameraController.PointerWheelDistanceChangeFactor = (float)selectedItem;
        }

        private void PointerMoveThresholdComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_pointerCameraController == null)
                return;

            var selectedItem = mouseMoveThresholdComboBox.SelectedItem;

            if (selectedItem != null)
                _pointerCameraController.PointerMoveThreshold = (float)selectedItem;
        }

        private void MaxCameraDistanceComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_pointerCameraController == null)
                return;

            float newMaxCameraDistance;
            switch (maxCameraDistanceComboBox.SelectedIndex)
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
    }
}
