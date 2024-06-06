using System;
using Ab4d.SharpEngine.Common;
using Microsoft.UI.Xaml;
using Ab4d.SharpEngine.WinUI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.WinUI.UIProvider;
using Colors = Microsoft.UI.Colors;

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
        private InputEventsManager _inputEventsManager;

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

            _wpfUiProvider = new WinUIProvider(RootGrid, MainSceneView);

            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;

            // By default, enable Vulkan's standard validation (this may slightly reduce performance)
            MainSceneView.CreateOptions.EnableStandardValidation = true;

            // Logging was already enabled in SamplesWindow constructor
            //Utilities.Log.LogLevel = LogLevels.Warn;
            //Utilities.Log.IsLoggingToDebugOutput = true;

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
            
            _currentCommonSample.InitializeSharpEngineView(MainSceneView); // This will call InitializeScene and InitializeSceneView
            _currentCommonSample.InitializeInputEventsManager(_inputEventsManager);

            _currentCommonSample.CreateUI(_wpfUiProvider);

            // Set Title and Subtitle after initializing UI, because they can be changed there
            TitleTextBlock.Text = _currentCommonSample.Title;
            SubtitleTextBlock.Text = _currentCommonSample.Subtitle;

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
