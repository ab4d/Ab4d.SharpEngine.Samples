using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common.Animations;
using Ab4d.SharpEngine.Samples.Wpf.Common;
using Ab4d.SharpEngine.Wpf;
using DispatcherPriority = System.Windows.Threading.DispatcherPriority;

namespace Ab4d.SharpEngine.Samples.Wpf.Titles
{
    public partial class IntroductionPage : Page
    {
        private bool PlayAnimationOnStartup = true; // Set to false to prevent automatically playing the animation
        private bool SkipInitializingSharpEngine = false; // When true, then no SharpEngine object will be created (only WPF objects will be shown)
        
        private SharpEngineLogoAnimation? _sharpEngineLogoAnimation;

        public IntroductionPage()
        {
            // Setup logger (before calling InitializeComponent so log events from SharpEngineSceneView can be also logged)
            // Set enableFullLogging to true in case of problems and then please send the log text with the description of the problem to AB4D company
            if (!SkipInitializingSharpEngine)
                LogHelper.SetupSharpEngineLogger(enableFullLogging: false);

            InitializeComponent();

            if (SkipInitializingSharpEngine)
            {
                MainSceneView.PresentationType = PresentationTypes.None;
                return;
            }


#if DEBUG
            // Enable standard validation that provides additional error information when Vulkan SDK is installed on the system.
            MainSceneView.CreateOptions.EnableStandardValidation = true;
#endif

            var scene = MainSceneView.Scene;
            var sceneView = MainSceneView.SceneView;

            var bitmapIO = new WpfBitmapIO(); // _bitmapIO provides a cross-platform way to read bitmaps - in this sample we use WPF as backend

            _sharpEngineLogoAnimation = new SharpEngineLogoAnimation(scene, bitmapIO);

            scene.RootNode.Add(_sharpEngineLogoAnimation.LogoPlaneModel);
            scene.RootNode.Add(_sharpEngineLogoAnimation.HashModel);


            var targetPositionCamera = new TargetPositionCamera();
            _sharpEngineLogoAnimation.ResetCamera(targetPositionCamera);
            _sharpEngineLogoAnimation.SetupLights(scene);

            sceneView.Camera = targetPositionCamera;

            MainSceneView.SceneUpdating += MainSceneViewOnSceneUpdating;

            _sharpEngineLogoAnimation.AnimationCompleted += delegate (object? sender2, EventArgs args2)
            {
                InfoTextBlock.Visibility = Visibility.Visible;
                PlayAgainButton.Visibility = Visibility.Visible;
            };


            if (PlayAnimationOnStartup)
            {
                // Start with going to first frame
                _sharpEngineLogoAnimation.GotoFirstFrame();

                // Now wait until Application is Idle and then start the animation
                this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => _sharpEngineLogoAnimation.StartAnimation()));
            }
            else
            {
                _sharpEngineLogoAnimation.GotoLastFrame();

                InfoTextBlock.Visibility = Visibility.Visible;

                PlayAgainButton.Content = "Play animation"; // replace "Play again" because animation was not yet played
                PlayAgainButton.Visibility = Visibility.Visible;
            }
        }

        private void MainSceneViewOnSceneUpdating(object? sender, EventArgs e)
        {
            _sharpEngineLogoAnimation?.UpdateAnimation();
        }

        private void PlayAgainButton_OnClick(object sender, RoutedEventArgs e)
        {
            InfoTextBlock.Visibility = Visibility.Hidden;
            PlayAgainButton.Visibility = Visibility.Hidden;

            _sharpEngineLogoAnimation?.StartAnimation();
        }
    }
}