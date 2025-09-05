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
using Microsoft.UI.Xaml.Media.Imaging;

namespace Ab4d.SharpEngine.Samples.WinUI.Titles
{
    public partial class IntroductionPage : UserControl
    {
        private static readonly bool PlayAnimationOnStartup = true;       // Set to false to prevent automatically playing the animation
        private static readonly bool SkipInitializingSharpEngine = false; // When true, then no SharpEngine object will be created (only WinUI controls will be shown)
        
        private SharpEngineLogoAnimation? _sharpEngineLogoAnimation;

        public IntroductionPage()
        {
            InitializeComponent();
            
            if (SkipInitializingSharpEngine)
            {
                RootGrid.Children.Remove(MainSceneView); // Remove SharpEngineSceneView before it is loaded to prevent creating any Vulkan resources
                
                InfoTextBlock.Visibility = Visibility.Visible;
                ShowStaticSharpEngineLogo();
                
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

        private void ShowStaticSharpEngineLogo()
        {
            string fileName = AppDomain.CurrentDomain.BaseDirectory + @"Resources\Textures\sharp-engine-logo.png";
            var bitmapImage = new BitmapImage(new Uri(fileName));

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