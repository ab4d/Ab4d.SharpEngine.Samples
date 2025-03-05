using Ab4d.SharpEngine.AvaloniaUI;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.AvaloniaUI.UIProvider;
using Ab4d.SharpEngine.Samples.Common;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI
{
    public partial class CommonAvaloniaSampleUserControl : UserControl
    {
        private CommonSample? _currentCommonSample;
        private CommonSample? _lastInitializedSample;
        private PointerCameraController? _pointerCameraController;
        private InputEventsManager _inputEventsManager;

        private AvaloniaUIProvider _avaloniaUiProvider;

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

        public CommonAvaloniaSampleUserControl()
        {
            InitializeComponent();

            _avaloniaUiProvider = new AvaloniaUIProvider(RootGrid, pointerEventsSource: RootBorder);

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

            //MainSceneView.PreferredMultiSampleCount = 1; // Disable MSSA (multi-sample anti-aliasing)
            

            // Because we are rendering a background Border with a gradient, we can subscribe mouse events to that element.
            // In this case we can slightly improve performance when SharedTexture is by setting the IsHitTestVisible to false.
            // This prevents rendering a transparent background in SharpEngineSceneView control (this is required to enable mouse events on the control when SharedTexture is used).
            MainSceneView.IsHitTestVisible = false;

            MainSceneView.GpuDeviceCreated += MainSceneViewOnGpuDeviceCreated;

            // In case when VulkanDevice cannot be created, show an error message
            // If this is not handled by the user, then SharpEngineSceneView will show its own error message
            MainSceneView.GpuDeviceCreationFailed += delegate (object sender, DeviceCreateFailedEventArgs args)
            {
                ShowDeviceCreateFailedError(args.Exception); // Show error message
                args.IsHandled = true;                       // Prevent showing error by SharpEngineSceneView
            };


            _inputEventsManager = new InputEventsManager(MainSceneView, RootBorder);
        }

        private void ResetSample()
        {
            TitleTextBlock.Text = null;
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

            MainSceneView.IsVisible = false;

            _lastInitializedSample = null;
        }

        private void InitializeCommonSample()
        {
            if (_lastInitializedSample == _currentCommonSample)
                return; // already initialized

            ResetSample();

            if (_currentCommonSample == null)
                return;

            _currentCommonSample.InitializeScene(MainSceneView.Scene);
            _currentCommonSample.InitializeSceneView(MainSceneView.SceneView);
            _currentCommonSample.InitializeInputEventsManager(_inputEventsManager);

            _currentCommonSample.CreateUI(_avaloniaUiProvider);

            // Set Title and Subtitle after initializing UI, because they can be changed there
            TitleTextBlock.Text = _currentCommonSample.Title;
            SubtitleTextBlock.Text = _currentCommonSample.Subtitle;

            //MainSceneView.Scene.SetCoordinateSystem(CoordinateSystems.ZUpRightHanded);

            if (_pointerCameraController != null)
                _currentCommonSample.InitializePointerCameraController(_pointerCameraController);
            
            // Show MainSceneView - this will also render the scene
            MainSceneView.IsVisible = true;

            _lastInitializedSample = _currentCommonSample;
        }

        private void MainSceneViewOnGpuDeviceCreated(object sender, GpuDeviceCreatedEventArgs e)
        {

        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            InitializeCommonSample();

            if (_pointerCameraController == null) // if _pointerCameraController is not null, then InitializePointerCameraController was already called from InitializeCommonSample
            {
                // Because we render a gradient in background RootBorder and we have set MainSceneView.IsHitTestVisible to false
                // we need to set a custom eventsSourceElement when creating the PointerCameraController
                _pointerCameraController ??= new PointerCameraController(MainSceneView, eventsSourceElement: RootBorder);

                if (_currentCommonSample != null)
                    _currentCommonSample.InitializePointerCameraController(_pointerCameraController);
            }
        }

        private void OnUnloaded(object? sender, RoutedEventArgs e)
        {

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
