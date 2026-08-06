using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.SceneNodes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Ab4d.SharpEngine.Effects;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.OverlayPanels;
using Ab4d.SharpEngine.RenderingLayers;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Wpf.Common;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.SharpEngine.Wpf;

namespace Ab4d.SharpEngine.Samples.Wpf.Advanced
{
    /// <summary>
    /// Interaction logic for OverlayControlsSample.xaml
    /// </summary>
    public partial class OverlayControlsSample : Page
    {
        private WpfElementOverlay _wpfElementOverlay;

        public OverlayControlsSample()
        {
            InitializeComponent();

            var boxModelNode = new BoxModelNode(new Vector3(0, 0, 0), new Vector3(100, 20, 80), StandardMaterials.Green);
            MainSceneView.Scene.RootNode.Add(boxModelNode);
            
            
            var targetPositionCamera = new TargetPositionCamera()
            {
                Heading = -40,
                Attitude = -30,
                Distance = 400,
                TargetPosition = new Vector3(0, 0, 0),
                ShowCameraLight = ShowCameraLightType.Always,
            };

            MainSceneView.SceneView.Camera = targetPositionCamera;

            var pointerCameraController = new PointerCameraController(MainSceneView)
            {
                RotateCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed,                                                       // this is already the default value but is still set up here for clarity
                MoveCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.ControlKey,               // this is already the default value but is still set up here for clarity
                QuickZoomConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.RightPointerButtonPressed, // quick zoom is disabled by default
                ZoomMode = CameraZoomMode.PointerPosition,
                RotateAroundPointerPosition = true,
                CameraSmoothing = CameraController.CameraSmoothingPresets.Normal
            };
            

            // Create WpfElementOverlay that will render the specified WpfControlsBorder to a bitmap
            // and show it as a Sprite in the 3D scene.
            // NOTE: The source code for the WpfElementOverlay is available in the Common folder.
            _wpfElementOverlay = new WpfElementOverlay(WpfControlsBorder, MainSceneView);
        }

        private void AddBoxButton_OnClick(object sender, RoutedEventArgs e)
        {
            var boxModelNode = new BoxModelNode(new Vector3(0, MainSceneView.Scene.RootNode.Count * 30, 0), new Vector3(100, 20, 80), StandardMaterials.Orange);
            MainSceneView.Scene.RootNode.Add(boxModelNode);
        }
        
        private void TestSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.IsLoaded)
                return;
            
            InfoTextBlock.Text = $"Slider: {TestSlider.Value:F2}";
            _wpfElementOverlay.Update();
        }

        private void WpfControlsBorder_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!this.IsLoaded)
                return;
            
            if (UpdateOnMouseMoveCheckBox.IsChecked ?? false)
                _wpfElementOverlay?.Update();
        }
    }
}
