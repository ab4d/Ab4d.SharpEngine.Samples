using Ab4d.SharpEngine.AvaloniaUI;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.AvaloniaUI.UIProvider;
using Ab4d.SharpEngine.Samples.Common;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.Common
{
    public partial class CommonAvaloniaSampleUserControl : UserControl
    {
        private CommonSample? _currentCommonSample;
        private CommonSample? _lastInitializedSample;
        private MouseCameraController? _mouseCameraController;
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

            MainSceneView.Scene.RootNode.Clear();
            MainSceneView.Scene.Lights.Clear();
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

            if (_mouseCameraController != null)
                _currentCommonSample.InitializeMouseCameraController(_mouseCameraController);
            
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

            if (_mouseCameraController == null) // if _mouseCameraController is not null, then InitializeMouseCameraController was already called from InitializeCommonSample
            {
                // Because we render a gradient in background RootBorder and we have set MainSceneView.IsHitTestVisible to false
                // we need to set a custom eventsSourceElement when creating the MouseCameraController
                _mouseCameraController ??= new MouseCameraController(MainSceneView, eventsSourceElement: RootBorder);

                if (_currentCommonSample != null)
                    _currentCommonSample.InitializeMouseCameraController(_mouseCameraController);
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
                HorizontalAlignment = HorizontalAlignment.Center
            };

            RootGrid.Children.Add(errorTextBlock);
        }
    }
}
