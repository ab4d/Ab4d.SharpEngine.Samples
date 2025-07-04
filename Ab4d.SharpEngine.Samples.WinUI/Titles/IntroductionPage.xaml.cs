using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Windows;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.Common.Animations;
using Ab4d.SharpEngine.Samples.WinUI.Common;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ab4d.SharpEngine.Samples.WinUI.Titles
{
    public partial class IntroductionPage : UserControl
    {
        private const bool PlayAnimationOnStartup = true; // Set to false to prevent automatically playing the animation
        private const bool SkipInitializingSharpEngine = false; // When true, then no SharpEngine object will be created (only WinUI controls will be shown)
        
        private SharpEngineLogoAnimation? _sharpEngineLogoAnimation;

#pragma warning disable CS0162 // Unreachable code detected

        public IntroductionPage()
        {
            InitializeComponent();
            
            if (SkipInitializingSharpEngine)
            {
                MainSceneView.PresentationType = PresentationTypes.None;
                return;
            }
            
            // To enable Vulkan's standard validation, set EnableStandardValidation and install Vulkan SDK (this may slightly reduce performance)
            MainSceneView.CreateOptions.EnableStandardValidation = SamplesWindow.EnableStandardValidation;

            // Use 4xMSAA (multi-sample anti-aliasing) and no SSAA (super-sampling anti-aliasing)
            MainSceneView.MultisampleCount = 4;
            MainSceneView.SupersamplingCount = 1;

            var scene = MainSceneView.Scene;
            var sceneView = MainSceneView.SceneView;

            var bitmapIO = new WinUIBitmapIO(); // _bitmapIO provides a cross-platform way to read bitmaps - in this sample we use WinUI as backend

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
                this.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => _sharpEngineLogoAnimation.StartAnimation());
            }
            else
            {
                _sharpEngineLogoAnimation.GotoLastFrame();

                InfoTextBlock.Visibility = Visibility.Visible;

                PlayAgainButton.Content = "Play animation"; // replace "Play again" because animation was not yet played
                PlayAgainButton.Visibility = Visibility.Visible;
            }

            this.Unloaded += delegate (object sender, RoutedEventArgs args)
            {
                _sharpEngineLogoAnimation.Dispose();
                MainSceneView.Dispose();
            };
        }

#pragma warning restore CS0162 // Unreachable code detected

        private void MainSceneViewOnSceneUpdating(object? sender, EventArgs e)
        {
            _sharpEngineLogoAnimation?.UpdateAnimation();
        }

        private void PlayAgainButton_OnClick(object sender, RoutedEventArgs e)
        {
            InfoTextBlock.Visibility = Visibility.Collapsed;
            PlayAgainButton.Visibility = Visibility.Collapsed;

            _sharpEngineLogoAnimation?.StartAnimation();
        }
    }
}