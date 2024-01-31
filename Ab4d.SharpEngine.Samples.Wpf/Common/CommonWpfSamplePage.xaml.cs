using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Wpf.UIProvider;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Wpf;
using Ab4d.Vulkan;

namespace Ab4d.SharpEngine.Samples.Wpf.Common
{
    /// <summary>
    /// Interaction logic for CommonWpfSamplePage.xaml
    /// </summary>
    public partial class CommonWpfSamplePage : Page
    {
        private CommonSample? _currentCommonSample;
        private CommonSample? _lastInitializedSample;
        private MouseCameraController? _mouseCameraController;

        private WpfUIProvider _wpfUiProvider;

        public CommonSample? CurrentCommonSample
        {
            get => _currentCommonSample;
            set
            {
                _currentCommonSample = value;

                if (this.IsLoaded)
                    InitializeCommonSample();
            }
        }

        public CommonWpfSamplePage()
        {
            InitializeComponent();

            _wpfUiProvider = new WpfUIProvider(RootGrid);

            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;

            // By default enable Vulkan's standard validation and logging of warnings and errors (this may slightly reduce performance)
            Log.LogLevel = LogLevels.Warn;
            Log.IsLoggingToDebugOutput = true;
            MainSceneView.CreateOptions.EnableStandardValidation = true;

            // To use Vulkan line rasterizer, uncomment the following lines:
            //MainSceneView.CreateOptions.EnableVulkanLineRasterization = true;
            //MainSceneView.CreateOptions.EnableVulkanStippleLineRasterization = true;
            //MainSceneView.Scene.LineRasterizationMode = LineRasterizationModes.VulkanRectangular;

            // To test the OverlayTexture presentation type (has the best performance, but does not allow rendering any WPF controls over the 3D graphics),
            // uncomment the following code:
            //MainSceneView.PresentationType = PresentationTypes.OverlayTexture;
            //MainSceneView.Margin = new Thickness(0, 0, 350, 0); // We need to add some right margin so the sample settings will be still visible


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
            TitleTextBlock.Text    = null;
            SubtitleTextBlock.Text = null;

            // Remove all lights (new sample will set setup their own lights)
            MainSceneView.Scene.Lights.Clear();

            // Call DisposeAllChildren on RootNode.
            // Here we will also dispose all meshes and materials with textures (textures that are cached by the Scene or GpuDevice are not disposed).
            // We also set runSceneCleanup to true so the Scene.Cleanup method is also called to release free empty memory blocks.
            // Note that we must not dispose the RootNode without disposing the Scene.
            MainSceneView.Scene.RootNode.DisposeAllChildren(disposeMeshes: true, disposeMaterials: true, disposeTextures: true, runSceneCleanup: true);
            
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

            TitleTextBlock.Text    = _currentCommonSample.Title;
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
        }


        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            ResetSample();
        }

        private void ShowDeviceCreateFailedError(Exception ex)
        {
            var errorTextBlock = new TextBlock()
            {
                Text = "Error creating VulkanDevice:\r\n" + ex.Message,
                Foreground = Brushes.Red,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            RootGrid.Children.Add(errorTextBlock);
        }
    }
}
