using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Microsoft.UI.Xaml;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Transformations;
using Microsoft.UI;
using System;
using Windows.ApplicationModel;
using Ab4d.Assimp;
using Ab4d.SharpEngine.Assimp;
using Ab4d.SharpEngine.WinUI;
using Microsoft.UI.Windowing;
using Colors = Microsoft.UI.Colors;
using Ab4d.SharpEngine.Vulkan;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using TranslateTransform = Ab4d.SharpEngine.Transformations.TranslateTransform;
using System.Runtime.InteropServices;
using Ab4d.SharpEngine.Samples.WinUI.Common;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.WinUI.UIProvider;
using Microsoft.UI.Xaml.Input;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.


namespace Ab4d.SharpEngine.Samples.WinUI.Common
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CommonWinUISampleUserControl : UserControl
    {
        private CommonSample? _currentCommonSample;
        private CommonSample? _lastInitializedSample;
        private MouseCameraController? _mouseCameraController;

        private WinUIProvider _wpfUiProvider;

        private bool _isLoaded;

        public CommonSample? CurrentCommonSample
        {
            get => _currentCommonSample;
            set
            {
                _currentCommonSample = value;

                if (_isLoaded)
                    InitializeCommonSample();
            }
        }
        
        public SharpEngineSceneView MainSharpEngineSceneView => MainSceneView;

        public CommonWinUISampleUserControl()
        {
            InitializeComponent(); // To generate the source for InitializeComponent include XamlNameReferenceGenerator

            // Setup logger
            // Set enableFullLogging to true in case of problems and then please send the log text with the description of the problem to AB4D company
            LogHelper.SetupSharpEngineLogger(enableFullLogging: false);


            _wpfUiProvider = new WinUIProvider(RootGrid, MainSceneView);

            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;

            MainSceneView.GpuDeviceCreated += MainSceneViewOnGpuDeviceCreated;

            // In case when VulkanDevice cannot be created, show an error message
            // If this is not handled by the user, then SharpEngineSceneView will show its own error message
            MainSceneView.GpuDeviceCreationFailed += delegate (object sender, DeviceCreateFailedEventArgs args)
            {
                ShowDeviceCreateFailedError(args.Exception); // Show error message
                args.IsHandled = true;                       // Prevent showing error by SharpEngineSceneView
            };
        }

        private void ResetSample()
        {
            TitleTextBlock.Text = null;
            SubtitleTextBlock.Text = null;

            MainSceneView.Scene.RootNode.Clear();
            MainSceneView.Scene.Lights.Clear();
            MainSceneView.Visibility = Visibility.Collapsed;

            _lastInitializedSample = null;
        }

        private void InitializeCommonSample()
        {
            if (_lastInitializedSample == _currentCommonSample) 
                return; // already initialized

            ResetSample();

            if (_currentCommonSample == null)
                return;

            TitleTextBlock.Text = _currentCommonSample.Title;
            SubtitleTextBlock.Text = _currentCommonSample.Subtitle;
            
            _currentCommonSample.InitializeSharpEngineView(MainSceneView); // This will call InitializeScene and InitializeSceneView

            _currentCommonSample.CreateUI(_wpfUiProvider);

            //MainSceneView.Scene.SetCoordinateSystem(CoordinateSystems.ZUpRightHanded);

            if (_mouseCameraController != null)
                _currentCommonSample.InitializeMouseCameraController(_mouseCameraController);

            // Show MainSceneView - this will also render the scene
            MainSceneView.Visibility = Visibility.Visible;

            _lastInitializedSample = _currentCommonSample;
        }

        private void MainSceneViewOnGpuDeviceCreated(object sender, GpuDeviceCreatedEventArgs e)
        {

        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            InitializeCommonSample();

            if (_mouseCameraController == null) // if _mouseCameraController is not null, then InitializeMouseCameraController was already called from InitializeCommonSample
            {
                _mouseCameraController = new MouseCameraController(MainSceneView);

                if (_currentCommonSample != null)
                    _currentCommonSample.InitializeMouseCameraController(_mouseCameraController);
            }

            _isLoaded = true;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;
        }

        private void ShowDeviceCreateFailedError(Exception ex)
        {
            var errorTextBlock = new TextBlock()
            {
                Text = "Error creating VulkanDevice:\r\n" + ex.Message,
                Foreground = new SolidColorBrush(Colors.Red),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            RootGrid.Children.Add(errorTextBlock);
        }
    }
}
