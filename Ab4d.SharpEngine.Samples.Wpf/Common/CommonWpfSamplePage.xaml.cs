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
using Colors = Ab4d.SharpEngine.Common.Colors;

namespace Ab4d.SharpEngine.Samples.Wpf.Common
{
    /// <summary>
    /// Interaction logic for CommonWpfSamplePage.xaml
    /// </summary>
    public partial class CommonWpfSamplePage : Page
    {
        private CommonSample? _currentCommonSample;
        private CommonSample? _lastInitializedSample;
        private PointerCameraController? _pointerCameraController;
        private InputEventsManager _inputEventsManager;

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

            _wpfUiProvider = new WpfUIProvider(RootGrid, mouseEventsSource: MainSceneView);

            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;

            
            // When custom MultiSampleCount or SuperSamplingCount is set, use that values.
            // Otherwise, the default values will be used:
            // MSAA: 4x for fast desktop device; 1x otherwise
            // SSAA: 4x for dedicated desktop devices; 2x for integrated devices; 1x otherwise
            if (GlobalSharpEngineSettings.MultisampleCount > 0)
                MainSceneView.MultisampleCount = GlobalSharpEngineSettings.MultisampleCount;
            
            if (GlobalSharpEngineSettings.SupersamplingCount > 0)
                MainSceneView.SupersamplingCount = GlobalSharpEngineSettings.SupersamplingCount;

            // To enable Vulkan's standard validation, set EnableStandardValidation and install Vulkan SDK (this may slightly reduce performance)
            //MainSceneView.CreateOptions.EnableStandardValidation = true;

            // Logging was already enabled in SamplesWindow constructor
            //Utilities.Log.LogLevel = LogLevels.Warn;
            //Utilities.Log.IsLoggingToDebugOutput = true;

            // To use Vulkan line rasterizer, uncomment the following lines:
            //MainSceneView.CreateOptions.EnableVulkanLineRasterization = true;
            //MainSceneView.CreateOptions.EnableVulkanStippleLineRasterization = true;
            //MainSceneView.Scene.LineRasterizationMode = LineRasterizationModes.VulkanRectangular;

            // To test the OverlayTexture presentation type (has the best performance, but does not allow rendering any WPF controls over the 3D graphics),
            // uncomment the following code:
            //MainSceneView.PresentationType = PresentationTypes.OverlayTexture;
            //MainSceneView.Margin = new Thickness(0, 0, 350, 0); // We need to add some right margin so the sample settings will be still visible

            //MainSceneView.MultisampleCount = 1; // Disable MSSA (multi-sample anti-aliasing)

            MainSceneView.GpuDeviceCreated += MainSceneViewOnGpuDeviceCreated;

            // In case when VulkanDevice cannot be created, show an error message
            // If this is not handled by the user, then SharpEngineSceneView will show its own error message
            MainSceneView.GpuDeviceCreationFailed += delegate (object sender, DeviceCreateFailedEventArgs args)
            {
                ShowDeviceCreateFailedError(args.Exception); // Show error message
                args.IsHandled = true;                       // Prevent showing error by SharpEngineSceneView
            };

            _inputEventsManager = new InputEventsManager(MainSceneView);
        }

        private void ResetSample()
        {
            TitleTextBlock.Text    = null;
            SubtitleTextBlock.Text = null;

            // Remove all lights (new sample will set setup their own lights)
            MainSceneView.Scene.Lights.Clear();

            // Remove any registered event sources or drag surfaces
            _inputEventsManager.ResetEventSources();
            _inputEventsManager.RemoveAllDragSurfaces();

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
            
            _currentCommonSample.InitializeSharpEngineView(MainSceneView); // This will call InitializeScene and InitializeSceneView

            _currentCommonSample.InitializeInputEventsManager(_inputEventsManager);

            _currentCommonSample.CreateUI(_wpfUiProvider);

            // Set Title and Subtitle after initializing UI, because they can be changed there
            TitleTextBlock.Text    = _currentCommonSample.Title;
            SubtitleTextBlock.Text = _currentCommonSample.Subtitle;

            //MainSceneView.Scene.SetCoordinateSystem(CoordinateSystems.ZUpRightHanded);

            if (_pointerCameraController != null)
                _currentCommonSample.InitializePointerCameraController(_pointerCameraController);

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

            if (_pointerCameraController == null) // if _pointerCameraController is not null, then InitializePointerCameraController was already called from InitializeCommonSample
            {
                _pointerCameraController = new PointerCameraController(MainSceneView);

                if (_currentCommonSample != null)
                    _currentCommonSample.InitializePointerCameraController(_pointerCameraController);
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
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            RootGrid.Children.Add(errorTextBlock);
        }
    }
}
