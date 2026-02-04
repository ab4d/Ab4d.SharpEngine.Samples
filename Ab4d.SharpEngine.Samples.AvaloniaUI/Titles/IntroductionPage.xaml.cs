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
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia;
using Avalonia.Media.Imaging;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.Titles
{
    public partial class IntroductionPage : UserControl
    {
        private static readonly bool PlayAnimationOnStartup = true;       // Set to false to prevent automatically playing the animation
        private static readonly bool SkipInitializingSharpEngine = false; // When true, then no SharpEngine object will be created (only Avalonia objects will be shown)
        
        private SharpEngineLogoAnimation? _sharpEngineLogoAnimation;

        public IntroductionPage()
        {
            InitializeComponent();

            
            // Dispose MainSceneView even when the animation is not started in if below
            this.Unloaded += delegate (object? sender, RoutedEventArgs args)
            {
                _sharpEngineLogoAnimation?.Dispose();
                MainSceneView.Dispose();
            };
            
            if (SkipInitializingSharpEngine)
            {
                RootGrid.Children.Remove(MainSceneView); // Remove SharpEngineSceneView before it is loaded to prevent creating any Vulkan resources

                ShowStaticSharpEngineLogo();
                ShowInfoTextBlock();

                return;
            }


            // To enable Vulkan's standard validation, set EnableStandardValidation and install Vulkan SDK (this may slightly reduce performance)
            MainSceneView.CreateOptions.EnableStandardValidation = SamplesWindow.EnableStandardValidation;

            // Use 4xMSAA (multi-sample anti-aliasing) and no SSAA (super-sampling anti-aliasing)
            MainSceneView.MultisampleCount = 4;
            MainSceneView.SupersamplingCount = 1;
            
            // Apply and advanced settings from the SettingsWindow
            SamplesWindow.ConfigureSharpEngineSceneViewAction?.Invoke(MainSceneView);

            var scene = MainSceneView.Scene;
            var sceneView = MainSceneView.SceneView;

            _sharpEngineLogoAnimation = new SharpEngineLogoAnimation(scene, bitmapIO: null);

            scene.RootNode.Add(_sharpEngineLogoAnimation.LogoPlaneModel);
            scene.RootNode.Add(_sharpEngineLogoAnimation.HashModel);


            var targetPositionCamera = new TargetPositionCamera();
            _sharpEngineLogoAnimation.ResetCamera(targetPositionCamera);
            _sharpEngineLogoAnimation.SetupLights(scene);

            sceneView.Camera = targetPositionCamera;

            MainSceneView.SceneUpdating += MainSceneViewOnSceneUpdating;

            _sharpEngineLogoAnimation.AnimationCompleted += delegate (object? sender2, EventArgs args2)
            {
                ShowInfoTextBlock();
                PlayAgainButton.IsVisible = true;
            };


            if (PlayAnimationOnStartup)
            {
                // Start with going to first frame
                _sharpEngineLogoAnimation.GotoFirstFrame();

                if (sceneView.IsLicenseLogoShown)
                {
                    sceneView.OnLicenseLogoRemoved = () => _sharpEngineLogoAnimation.StartAnimation();
                }
                else
                {
                    // Now wait until Application is Idle and then start the animation
                    Dispatcher.UIThread.Post(() => _sharpEngineLogoAnimation.StartAnimation(), DispatcherPriority.Background);
                }
            }
            else
            {
                _sharpEngineLogoAnimation.GotoLastFrame();

                PlayAgainButton.Content = "Play animation"; // replace "Play again" because animation was not yet played
                PlayAgainButton.IsVisible = true;
            }
        }

        private void ShowInfoTextBlock()
        {
            // Avalonia does not have Visibility with Hidden, so we need to first show two empty lines of text and then set the actual text
            InfoTextBlock.Text = "Ab4d.SharpEngine is an easy to use general purpose\n3D rendering engine for desktop, mobile and browser apps.";
        }

        private void HideInfoTextBlock()
        {
            // Avalonia does not have Visibility with Hidden, so to hide the text we only set it to two empty lines
            InfoTextBlock.Text = "\n";
        }

        private void MainSceneViewOnSceneUpdating(object? sender, EventArgs e)
        {
            _sharpEngineLogoAnimation?.UpdateAnimation();
        }

        private void ShowStaticSharpEngineLogo()
        {
            string fileName = AppDomain.CurrentDomain.BaseDirectory + @"Resources\Textures\sharp-engine-logo.png";
            fileName = FileUtils.FixDirectorySeparator(fileName);
            var bitmapImage = new Bitmap(fileName);

            var image = new Image()
            {
                Source = bitmapImage,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(30)
            };

            Grid.SetRow(image, 0);

            RootGrid.Children.Add(image);
        }

        private void PlayAgainButton_OnClick(object sender, RoutedEventArgs e)
        {
            HideInfoTextBlock();
            PlayAgainButton.IsVisible = false;

            _sharpEngineLogoAnimation?.StartAnimation();
        }
    }
}