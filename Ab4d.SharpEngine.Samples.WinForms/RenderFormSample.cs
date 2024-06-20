using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.PrivateSamples.WinForms.Properties;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.SharpEngine.WinForms;

// This Forms shows how to use Ab4d.SharpEngine to render to the whole Form (Window) at the maximum speed (as many frames per second as possible).

namespace Ab4d.SharpEngine.PrivateSamples.WinForms
{
    public class RenderFormSample : IDisposable
    {
        private const int Width = 1024;
        private const int Height = 600;

        private const bool EnableStandardValidation = false;

        private const double TitleUpdateInterval = 0.05; // 0.05 seconds => 20 times per second


        private SharpEngineRenderForm _renderForm;

        public SharpEngineRenderForm RenderForm => _renderForm;

        private IWin32Window? _owner;

        private TargetPositionCamera? _targetPositionCamera;

        private DateTime _titleUpdatedTime;
        private int _lastFrameNumber;


        public RenderFormSample(IWin32Window? owner = null)
        {
            _owner = owner;

            // Create a new SharpEngineRenderForm with the specified size and window title
            _renderForm = new SharpEngineRenderForm(Width, Height, windowTitle: "Ab4d.SharpEngine RenderForm");
            
            // Set to false (true by default) to fix the window size and prevent user from resizing the window
            _renderForm.AllowUserResizing = false; 
            
            // Set to false (by default), to render only frames where there are any changes
            _renderForm.ForceRenderingEveryFrame = true; 

            // Set to true (false by default), to show the app in full screen (use ALT+F4 to close the running app).
            _renderForm.IsFullscreen = false; 

            // Set to true (false by default) to allow pressing ALT + ENTER to switch to full-screen mode and back.
            _renderForm.AllowFullScreenWithAltEnter = false; 

            // CreateOptions provide many options that are used to initialize the SharpEngine
            _renderForm.CreateOptions.EnableStandardValidation = EnableStandardValidation;
            

            // Get window icon from SamplesForm
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SamplesForm));
            _renderForm.Icon = (Icon)resources.GetObject("$this.Icon");


            _renderForm.GpuDeviceCreationFailed += (sender, args) =>
            {
                MessageBox.Show("Error creating Vulkan device:\r\n" + args.Exception.Message);
            };

            _renderForm.SceneView.SceneRendered += (sender, args) =>
            {
                // Update FPS 20 times per second
                var now = DateTime.Now;
                if ((now - _titleUpdatedTime).TotalSeconds > TitleUpdateInterval)
                {
                    var currentFrameNumber = _renderForm.SceneView.FrameNumber;
                    double fps = (currentFrameNumber - _lastFrameNumber) / TitleUpdateInterval;

                    _renderForm.Text = $"Ab4d.SharpEngine RenderForm ({fps:F0} FPS)";
                    
                    _titleUpdatedTime = now;
                    _lastFrameNumber = currentFrameNumber;
                }
            };


            CreateTestScene();
        }
        
        public void Run()
        {
            if (_owner != null)
                _renderForm.Show(_owner);

            // Start the render loop.
            // This will also show the form because the default value of automaticallyShowForm parameter is true.
            // But we can also manually call _renderForm.Show() before that. This allows calling Show with owner parameter (as shown before).
            
            // !!! IMPORTANT !!!
            // RunMessageLoop method will not return until the window is closed.
            _renderForm.RunMessageLoop(UpdateCallback);
        }

        private void UpdateCallback()
        {
            // Here we could do some animation or input processing
        }

        private void CreateTestScene()
        {
            var scene = _renderForm.Scene;
            var sceneView = _renderForm.SceneView;

            //var boxModel = new BoxModelNode(centerPosition: new Vector3(0, 0, 0),
            //                             size: new Vector3(80, 40, 60),
            //                             name: "Gold BoxModel")
            //{
            //    Material = StandardMaterials.Gold,
            //    //Material = new StandardMaterial(Colors.Gold),
            //    //Material = new StandardMaterial(diffuseColor: new Color3(1f, 0.84313726f, 0f))
            //};

            //scene.RootNode.Add(boxModel);

            // Create hash symbol similar to SharpEngine logo:

            float HashModelSize = 100;
            float HashModelBarThickness = 16;
            float HashModelBarOffset = 20;
            
            var hashSymbolMesh = MeshFactory.CreateHashSymbolMesh(centerPosition: new Vector3(0, HashModelBarThickness * -0.5f, 0),
                                                                  shapeYVector: new Vector3(0, 0, 1),
                                                                  extrudeVector: new Vector3(0, HashModelBarThickness, 0),
                                                                  size: HashModelSize,
                                                                  barThickness: HashModelBarThickness,
                                                                  barOffset: HashModelBarOffset,
                                                                  name: "HashSymbolMesh");

            var hashModelMaterial = new StandardMaterial(Color3.FromByteRgb(255, 197, 0));

            var hashModel = new Ab4d.SharpEngine.SceneNodes.MeshModelNode(hashSymbolMesh, "HashSymbolModel")
            {
                Material = hashModelMaterial,
            };

            scene.RootNode.Add(hashModel);


            // Define the camera
            _targetPositionCamera = new TargetPositionCamera()
            {
                Heading = -40,
                Attitude = -25,
                Distance = 300,
                TargetPosition = new Vector3(0, 0, 0),
                ShowCameraLight = ShowCameraLightType.Auto // If there are no other light in the Scene, then add a camera light that illuminates the scene from the camera's position
            };

            _targetPositionCamera.StartRotation(headingChangeInSecond: 90);

            sceneView.Camera = _targetPositionCamera;
        }
        
        public void Dispose()
        {
            _renderForm.Dispose();
        }
    }
}
