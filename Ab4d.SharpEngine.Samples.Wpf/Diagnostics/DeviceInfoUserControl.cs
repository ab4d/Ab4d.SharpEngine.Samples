using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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
using Ab4d.SharpEngine.Wpf;

namespace Ab4d.SharpEngine.Samples.Wpf.Diagnostics
{
    /// <summary>
    /// Interaction logic for DeviceInfoUserControl.xaml
    /// </summary>
    public class DeviceInfoUserControl : TextBlock
    {
        private bool _isSceneViewInitializedSubscribed;

        private ISharpEngineSceneView? _sharpEngineSceneView;

        public ISharpEngineSceneView? SharpEngineSceneView
        {
            get { return _sharpEngineSceneView; }
            set
            {
                if (ReferenceEquals(_sharpEngineSceneView, value))
                    return;

                if (_sharpEngineSceneView != null)
                {
                    _sharpEngineSceneView.Disposing       -= SharpEngineSceneViewOnDisposing;
                    _sharpEngineSceneView.ViewSizeChanged -= SharpEngineSceneViewOnViewSizeChanged;
                }

                _sharpEngineSceneView = value;

                try
                {
                    Update();
                }
                catch
                {
                    // This can happen in case of DXEngineSnoop that is started with wrong SharpDX reference  
                }

                if (_sharpEngineSceneView != null)
                {
                    _sharpEngineSceneView.Disposing       += SharpEngineSceneViewOnDisposing;
                    _sharpEngineSceneView.ViewSizeChanged += SharpEngineSceneViewOnViewSizeChanged;
                }
            }
        }

        public bool ShowViewSize { get; set; }
        public bool ShowAntialiasingSettings { get; set; }

        public DeviceInfoUserControl()
        {
            ShowViewSize             = true;
            ShowAntialiasingSettings = true;

            this.Unloaded += (sender, args) => UnsubscribeSceneViewCreated();
        }

        private void Update()
        {
            if (_sharpEngineSceneView == null || !_sharpEngineSceneView.SceneView.BackBuffersInitialized)
            {
                this.Text = "SharpEngineSceneView is not initialized";
                SubscribeSceneViewInitialized();
                return;
            }


            string viewInfo;
            var sceneView = _sharpEngineSceneView.SceneView;

            if (sceneView.BackBuffersInitialized)
            {
                int width = sceneView.Width;
                int height = sceneView.Height;

                if (ShowViewSize)
                    viewInfo = string.Format("{0} x {1}", width, height);
                else
                    viewInfo = "";

                var multisampleCount    = sceneView.UsedMultiSampleCount;
                var supersamplingCount  = sceneView.SupersamplingCount;  // number of pixels used for one final pixel

                if (ShowAntialiasingSettings)
                {
                    if (multisampleCount > 1)
                        viewInfo += string.Format(" x {0}xMSAA", multisampleCount);
                    
                    if (supersamplingCount > 1)
                        viewInfo += string.Format(" x {0}xSSAA", supersamplingCount);
                }

                viewInfo += $" ({_sharpEngineSceneView.PresentationType})";
            }
            else
            {
                viewInfo = "";
            }


            if (_sharpEngineSceneView.GpuDevice != null)
            {
                string deviceInfoText = _sharpEngineSceneView.GpuDevice.GpuName;
                viewInfo = deviceInfoText + Environment.NewLine + viewInfo;
            }

            this.Text = viewInfo;
        }

        private void OnSceneViewInitialized(object? sender, EventArgs eventArgs)
        {
            UnsubscribeSceneViewCreated();

            Update();
        }

        private void SubscribeSceneViewInitialized()
        {
            if (_isSceneViewInitializedSubscribed)
                return;

            if (_sharpEngineSceneView != null)
            {
                _sharpEngineSceneView.SceneViewInitialized += OnSceneViewInitialized;
                _isSceneViewInitializedSubscribed = true;
            }
        }

        private void UnsubscribeSceneViewCreated()
        {
            if (!_isSceneViewInitializedSubscribed)
                return;

            if (_sharpEngineSceneView != null)
                _sharpEngineSceneView.SceneViewInitialized -= OnSceneViewInitialized;

            _isSceneViewInitializedSubscribed = false;
        }

        private void SharpEngineSceneViewOnDisposing(object? sender, bool e)
        {
            try
            {
                this.Text = "SharpEngineSceneView is disposed";
            }
            catch (InvalidOperationException)
            {
                // In case SharpEngineSceneView was disposed in background thread
            }
        }

        private void SharpEngineSceneViewOnViewSizeChanged(object sender, ViewSizeChangedEventArgs e)
        {
            Update();
        }
    }
}
