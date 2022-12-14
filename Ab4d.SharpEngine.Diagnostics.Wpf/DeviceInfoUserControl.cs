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

namespace Ab4d.SharpEngine.Diagnostics.Wpf
{
    /// <summary>
    /// Interaction logic for DeviceInfoUserControl.xaml
    /// </summary>
    public class DeviceInfoUserControl : TextBlock
    {
        private bool _isSceneViewCreatedSubscribed;

        private SharpEngineSceneView? _sharpEngineSceneView;

        public SharpEngineSceneView? SharpEngineSceneView
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
        public bool ShowAntialiasinigSettings { get; set; }

        public DeviceInfoUserControl()
        {
            ShowViewSize              = true;
            ShowAntialiasinigSettings = true;

            this.Unloaded += (sender, args) => UnsubscribeSceneViewCreated();
        }

        private void Update()
        {
            if (_sharpEngineSceneView == null || _sharpEngineSceneView.Scene == null)
            {
                this.Text = "SharpEngineSceneView is not initialized";

                if (_sharpEngineSceneView != null && _sharpEngineSceneView.SceneView == null)
                    SubscribeSceneViewCreated();

                return;
            }


            string viewInfo;
            var sceneView = _sharpEngineSceneView.SceneView;

            if (sceneView != null)
            {
                int width = sceneView.Width;
                int height = sceneView.Height;

                if (ShowViewSize)
                    viewInfo = string.Format("{0} x {1}", width, height);
                else
                    viewInfo = "";

                var multisampleCount    = sceneView.UsedMultiSampleCount;
                var supersamplingCount  = sceneView.SupersamplingCount;  // number of pixels used for one final pixel

                if (ShowAntialiasinigSettings)
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


            string deviceInfoText = _sharpEngineSceneView.Scene.GpuDevice.GpuName;

            if (viewInfo.Length > 0)
                deviceInfoText += Environment.NewLine + viewInfo;

            this.Text = deviceInfoText;
        }

        private void DXSceneViewBaseOnDXSceneInitialized(object? sender, EventArgs eventArgs)
        {
            UnsubscribeSceneViewCreated();

            Update();
        }

        private void SubscribeSceneViewCreated()
        {
            if (_isSceneViewCreatedSubscribed)
                return;

            if (_sharpEngineSceneView != null)
            {
                _sharpEngineSceneView.SceneViewInitialized += DXSceneViewBaseOnDXSceneInitialized;
                _isSceneViewCreatedSubscribed = true;
            }
        }

        private void UnsubscribeSceneViewCreated()
        {
            if (!_isSceneViewCreatedSubscribed)
                return;

            if (_sharpEngineSceneView != null)
                _sharpEngineSceneView.SceneViewInitialized -= DXSceneViewBaseOnDXSceneInitialized;

            _isSceneViewCreatedSubscribed = false;
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
