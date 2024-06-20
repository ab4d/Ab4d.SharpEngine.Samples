using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.WinForms.Common;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.WinForms;

namespace Ab4d.SharpEngine.Samples.WinForms.QuickStart
{
    public partial class SharpEngineSceneViewControl : UserControl
    {
        private GroupNode? _groupNode;
        
        private PointerCameraController? _pointerCameraController;

        private TargetPositionCamera? _targetPositionCamera;

        private int _newObjectsCounter;

        public SharpEngineSceneViewControl()
        {
            InitializeComponent();

            // This sample shows how to create SharpEngineSceneView in XAML.
            // To see how do create SharpEngineSceneView in code, see the SharpEngineSceneViewInCode sample.
            //
            // When SharpEngineSceneView is defined in XAML, then the Initialize method that creates the Scene and SceneView
            // is called when the SharpEngineSceneView is loaded (this way it is possible to set CreateOptions and other properties).
            // To get the Scene and SceneView event when they are created, we can use the SceneViewCreated event.
            //

#if DEBUG
            // Enable standard validation that provides additional error information when Vulkan SDK is installed on the system.
            mainSceneView.CreateOptions.EnableStandardValidation = true;

            // Logging was already enabled in SamplesWindow constructor
            //Utilities.Log.LogLevel = LogLevels.Warn;
            //Utilities.Log.IsLoggingToDebugOutput = true;
#endif

            // In case when VulkanDevice cannot be created, show an error message
            // If this is not handled by the user, then SharpEngineSceneView will show its own error message
            mainSceneView.GpuDeviceCreationFailed += delegate (object sender, DeviceCreateFailedEventArgs args)
            {
                ShowDeviceCreateFailedError(args.Exception); // Show error message
                args.IsHandled = true;                       // Prevent showing error by SharpEngineSceneView
            };

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

            this.HandleDestroyed += OnHandleDestroyed;
        }

        private void OnHandleDestroyed(object? sender, EventArgs e)
        {
            if (!mainSceneView.IsDisposed)
                mainSceneView.Dispose();
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!mainSceneView.IsDisposed)
                    mainSceneView.Dispose();

                if (components != null)
                    components.Dispose();
            }

            base.Dispose(disposing);
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

            mainSceneView.Scene.RootNode.Add(planeModelNode);

            // Create a GroupNode that will group all created objects
            _groupNode = new GroupNode("GroupNode");
            _groupNode.Transform = new StandardTransform(translateX: 50, translateZ: 30);
            mainSceneView.Scene.RootNode.Add(_groupNode);
            
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
        }

        private void ShowDeviceCreateFailedError(Exception ex)
        {
            var errorLabel = new Label()
            {
                Text = "Error creating VulkanDevice:\r\n" + ex.Message,
                ForeColor = Color.Red,
            };

            if (!panel2.ClientSize.IsEmpty)
                errorLabel.Location = new Point((panel2.ClientSize.Width + errorLabel.PreferredWidth) / 2, (panel2.ClientSize.Height + errorLabel.PreferredHeight) / 2);

            panel2.Controls.Add(errorLabel);
        }


        private void addNewButton_Click(object sender, EventArgs e)
        {
            if (_groupNode == null)
                return;

            var boxModel3D = new BoxModelNode($"BoxModel3D_{_newObjectsCounter}")
            {
                Position = new Vector3(-140, _newObjectsCounter * 30 + 20, -100),
                Size = new Vector3(50, 20, 50),
                Material = StandardMaterials.Gold,
            };

            _groupNode.Add(boxModel3D);

            _newObjectsCounter++;
        }

        private void removeButton_Click(object sender, EventArgs e)
        {
            if (_groupNode == null || _groupNode.Count == 0)
                return;

            _groupNode.RemoveAt(_groupNode.Count -1);

            if (_newObjectsCounter > 0)
                _newObjectsCounter--;
        }

        private void changeMaterial1Button_Click(object sender, EventArgs e)
        {
            if (_groupNode == null || _groupNode.Count == 0)
                return;

            var index = WinFormsSamplesContext.Current.GetRandomInt(_groupNode.Count - 1);

            if (_groupNode[index] is ModelNode modelNode)
            {
                modelNode.Material = WinFormsSamplesContext.Current.GetRandomStandardMaterial();
            }
        }

        private void changeMaterial2Button_Click(object sender, EventArgs e)
        {
            if (_groupNode == null || _groupNode.Count == 0)
                return;

            var index = WinFormsSamplesContext.Current.GetRandomInt(_groupNode.Count - 1);

            if (_groupNode[index] is ModelNode modelNode)
            {
                if (modelNode.Material is StandardMaterial standardMaterial)
                    standardMaterial.DiffuseColor = WinFormsSamplesContext.Current.GetRandomColor3();
            }
        }

        private void changeBackgroundButton_Click(object sender, EventArgs e)
        {
            if (mainSceneView == null)
                return;

            mainSceneView.BackColor = WinFormsSamplesContext.Current.GetRandomWinFormsColor();
        }

        private void renderToBitmapButton_Click(object sender, EventArgs e)
        {
            // Call SharpEngineSceneView.RenderToBitmap to the get Avalonia's WritableBitmap.
            // This will create a new WritableBitmap on each call. To reuse the WritableBitmap,
            // call the RenderToBitmap and pass the WritableBitmap by ref as the first parameter.
            // It is also possible to call SceneView.RenderToXXXX methods - this give more low level bitmap objects.
            var renderedSceneBitmap = mainSceneView.RenderToBitmap(renderNewFrame: true);

            if (renderedSceneBitmap != null)
            {
                string fileName = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "AvaloniaSharpEngineScene.png");
                renderedSceneBitmap.Save(fileName);

                System.Diagnostics.Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
            }
        }
    }
}
