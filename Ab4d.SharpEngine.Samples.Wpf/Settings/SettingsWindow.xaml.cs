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
using System.Windows.Shapes;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Wpf.QuickStart;
using Ab4d.SharpEngine.Vulkan;

namespace Ab4d.SharpEngine.Samples.Wpf.Settings
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public ISharpEngineSceneView? SharpEngineSceneView { get; set; }

        public bool IsChanged { get; private set; }

        private readonly float[] _possibleSuperSamplingValues = new float[] { 1, 2, 3, 4, 9, 16 };

        public SettingsWindow()
        {
            InitializeComponent();


            GpuDevicesInfoControl.InfoText =
@"When this samples project is started on laptop with multiple graphics cards,
then by default Windows will run the application by using the integrated graphics card.

To run the samples on another graphics card, open Windows Graphics settings, 
select the exe file of this sample project and then select 'High performance'.

By default the Ab4d.SharpEngine is using the primary graphics card (selected by OS),
but by changing the DeviceSelectionType property in the EngineCreateOptions,
it is possible to use any system graphics card. But note that if the graphics card
that is used by Ab4d.SharpEngine is not the same as the application's graphics card,
then WritableBitmap will need to be used (the rendered image will need to be copied 
to the main CPU memory and then back to the application's GPU).";

            MultisamplingInfoControl.InfoText =
@"Multi-sampling anti-aliasing (MSAA) is the first anti-aliasing technique in Ab4d.SharpEngine.

MSAA produce smoother edges by storing multiple color values for each pixel. To improve performance, pixel shader is executed once for each pixel and then its result (color) is shared across multiple pixels. This produces smoother edges but do not provide additional sub-pixel details.

For example, 4xMSAA runs pixel shader only once per each pixel but require 4 times the amount of memory.";


            SuperSamplingInfoControl.InfoText =
@"Super-sampling anti-aliasing (SSAA) is the second anti-aliasing technique in Ab4d.SharpEngine.

SSAA is a technique that renders the image at a higher resolution and then down-samples the rendered image to the final resolution. The Ab4d.SharpEngine can use smart down-sampling filter that improves smoothness of the final image. Super-sampling produces smoother edges and also provides additional sub-pixel details.

Examples:
4xSSAA renders the scene to a texture with 4 times more pixels (width and height are multiplied by 2 - SceneView.SupersamplingFactor is 2). This requires running the pixel shader 4 times for each final pixel and requires 4 times the amount of memory.

2xSSAA renders the scene to a texture with 2 times more pixels. In this case width and height are multiplied by 1.41 = sqrt(2) - SceneView.SupersamplingFactor is 1.41.";

            SetupDeviceInfo();
        }

        private void SetupDeviceInfo()
        {
            // Check if VulkanInstance was already created (created when the first VulkanDevice is created).
            VulkanInstance? vulkanInstance = VulkanInstance.FirstVulkanInstance;

            if (vulkanInstance == null)
            {
                var engineCreateOptions = new EngineCreateOptions();
                try
                {
                    vulkanInstance = VulkanInstance.Create(engineCreateOptions);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error creating VulkanInstance:\r\n" + ex.Message);
                    return;
                }
            }

            // Enumerate all graphics cards on the system
            var devicesCount = vulkanInstance.AllPhysicalDeviceDetails.Length;
            for (var i = 0; i < devicesCount; i++)
            {
                var deviceDetails = vulkanInstance.AllPhysicalDeviceDetails[i];

                var radioButton = new RadioButton()
                {
                    Content = deviceDetails.DeviceName,
                    Margin = new Thickness(0, 0, 0, 5),
                    IsEnabled = false // We cannot change the used device because SharpEngine uses the first device - set by the OS.
                };

                if (this.SharpEngineSceneView != null && this.SharpEngineSceneView.GpuDevice != null)
                    radioButton.IsChecked = deviceDetails.DeviceName == this.SharpEngineSceneView.GpuDevice.GpuName;
                else if (i == 0)
                    radioButton.IsChecked = true;

                AdapterStackPanel.Children.Add(radioButton);


                if (i == 0)
                {
                    // We are using the primary graphic card so fill ComboBoxes based on its capabilities
                    SetupAntialisingComboBoxes(deviceDetails);
                }
            }

            if (devicesCount > 1)
            {
                GpuDevicesTextBlock.Text = "GPU devices:";
                GpuDevicesInfoControl.Visibility = Visibility.Visible;
            }
        }

        private void SetupAntialisingComboBoxes(PhysicalDeviceDetails deviceDetails)
        {
            // Fill MultisamplingComboBox
            var maxMultiSampleCount = (int)deviceDetails.GetMaxSupportedMultiSamplingCount(64);

            int count = 1;
            var allSupportedMultiSampleCounts = new List<int>();
            while (count <= maxMultiSampleCount)
            {
                allSupportedMultiSampleCounts.Add(count);
                count *= 2;
            }

            var allSupportedMultiSampleCountArray = allSupportedMultiSampleCounts.ToArray();
            MultisamplingComboBox.ItemsSource = allSupportedMultiSampleCountArray;

            SuperSamplingComboBox.ItemsSource = _possibleSuperSamplingValues;


            // Read settings from the static class:
            var selectedMultiSampleCount = GlobalSharpEngineSettings.MultisampleCount;
            var selectedSuperSamplingCount = GlobalSharpEngineSettings.SupersamplingCount;


            // If SelectedMultiSampleCount is not one of the supported, then we will use default value that is set below
            if (Array.IndexOf(allSupportedMultiSampleCountArray, selectedMultiSampleCount) == -1)
                selectedMultiSampleCount = 0;
            
            if (Array.IndexOf(_possibleSuperSamplingValues, selectedSuperSamplingCount) == -1)
                selectedSuperSamplingCount = 0; 


            // Create ad-hoc SharpEngineSceneView so we can call GetDefaultMultiSampleCount and GetDefaultSuperSamplingCount
            var sharpEngineSceneView = this.SharpEngineSceneView ?? new Ab4d.SharpEngine.Wpf.SharpEngineSceneView();

            var defaultMultiSampleCount = sharpEngineSceneView.GetDefaultMultiSampleCount(deviceDetails);
            var defaultSuperSamplingCount = sharpEngineSceneView.GetDefaultSuperSamplingCount(deviceDetails);

            if (selectedMultiSampleCount == 0)
                selectedMultiSampleCount = defaultMultiSampleCount;

            if (selectedSuperSamplingCount == 0)
                selectedSuperSamplingCount = defaultSuperSamplingCount;


            MultisamplingComboBox.SelectedIndex = Array.IndexOf(allSupportedMultiSampleCountArray, selectedMultiSampleCount);
            SuperSamplingComboBox.SelectedIndex = Array.IndexOf(_possibleSuperSamplingValues, selectedSuperSamplingCount);

            DefaultInfoTextBlock.Text = $"Default values: {defaultMultiSampleCount}xMSAA, {defaultSuperSamplingCount}xSSAA";
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Save settings to static class
            var multiSampleCount   = (int)MultisamplingComboBox.SelectedValue;
            var superSamplingCount = (float)SuperSamplingComboBox.SelectedValue;

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            IsChanged = (multiSampleCount != GlobalSharpEngineSettings.MultisampleCount ||
                         superSamplingCount != GlobalSharpEngineSettings.SupersamplingCount);

            if (IsChanged)
            {
                GlobalSharpEngineSettings.MultisampleCount = multiSampleCount;
                GlobalSharpEngineSettings.SupersamplingCount = superSamplingCount;
            }

            this.Close();
        }
    }
}
