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
using Ab4d.SharpEngine.Samples.Common.Animations;
using Ab4d.SharpEngine.Vulkan;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.Titles
{
    public partial class IntroductionPage : UserControl
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

            var bitmapIO = new SkiaSharpBitmapIO(); // _bitmapIO provides a cross-platform way to read bitmaps - in this sample we use WPF as backend

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
                ShowInfoTextBlockAndPlayButton();
            };


            if (PlayAnimationOnStartup)
            {
                // Start with going to first frame
                _sharpEngineLogoAnimation.GotoFirstFrame();

                // Now wait until Application is Idle and then start the animation
                Dispatcher.UIThread.Post(() => _sharpEngineLogoAnimation.StartAnimation(), DispatcherPriority.Background);
            }
            else
            {
                _sharpEngineLogoAnimation.GotoLastFrame();

                PlayAgainButton.Content = "Play animation"; // replace "Play again" because animation was not yet played
                ShowInfoTextBlockAndPlayButton();
            }
        }

        private void ShowInfoTextBlockAndPlayButton()
        {
            // Avalonia does not have Visibility with Hidden, so we need to first show two empty lines of text and then set the actual text
            InfoTextBlock.Text = "Ab4d.SharpEngine is a blazing fast and cross platform\n3D rendering engine for desktop and mobile .Net applications.";
            PlayAgainButton.IsVisible = true;
        }

        private void HideInfoTextBlockAndPlayButton()
        {
            // Avalonia does not have Visibility with Hidden, so to hide the text we only set it to two empty lines
            InfoTextBlock.Text = "\n";
            PlayAgainButton.IsVisible = false;
        }

        private void MainSceneViewOnSceneUpdating(object? sender, EventArgs e)
        {
            _sharpEngineLogoAnimation?.UpdateAnimation();
        }

        private void PlayAgainButton_OnClick(object sender, RoutedEventArgs e)
        {
            HideInfoTextBlockAndPlayButton();

            _sharpEngineLogoAnimation?.StartAnimation();
        }
    }
}