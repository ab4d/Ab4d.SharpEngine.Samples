using System;
using System.Numerics;
using System.Threading.Tasks;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.Vulkan;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Rendering.Composition;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI
{
    // This window can be used to show what are the supported Vulkan features and extensions 
    // and what AvaloniaUI requires and supports for Vulkan interop.
    public partial class TestVulkanInteropWindow : Window
    {
        public TestVulkanInteropWindow()
        {
            InitializeComponent(); // To generate the source for InitializeComponent include XamlNameReferenceGenerator

            var avaloniaInteropInfoControl = new AvaloniaAvaloniaInteropInfoControl(ShowInfoMessage, ShowErrorMessage);
            avaloniaInteropInfoControl.Width = 10;
            avaloniaInteropInfoControl.Height = 8;

            RootGrid.Children.Add( avaloniaInteropInfoControl );


            this.Loaded += delegate(object? sender, RoutedEventArgs args)
            {
                TestVulkanSupport();
            };
        }

        private void ShowInfoMessage(string message)
        {
            InfoTextBox.Text += message + Environment.NewLine;
        }
        
        private void ShowErrorMessage(string message, Exception ex)
        {
            InfoTextBox.Text += message + Environment.NewLine;
            InfoTextBox.Text += "EXCEPTION.Message: " + ex.Message + Environment.NewLine;

            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
                InfoTextBox.Text += "  InnerException.Message: " + ex.Message + Environment.NewLine;
            }
        }


        private void TestVulkanSupport()
        {
            try
            {
                VulkanInstance vulkanInstance;

                try
                {
                    var engineCreateOptions = new EngineCreateOptions(applicationName: "TestVulkanInteropWindow", enableStandardValidation: false);
                    vulkanInstance = VulkanInstance.Create(engineCreateOptions);
                    
                    ShowInfoMessage($"VulkanInstance created. API version: {vulkanInstance.ApiVersion}");
                }
                catch (Exception ex)
                {
                    ShowErrorMessage("Error creating Vulkan instance", ex);
                    return;
                }
                

                try
                {
                    ShowInfoMessage("AllInstanceExtensionNames:");

                    var allInstanceExtensionNames = VulkanInstance.AllInstanceExtensionNames;

                    if (allInstanceExtensionNames != null)
                    {
                        foreach (var instanceExtensionName in allInstanceExtensionNames)
                            ShowInfoMessage("  " + instanceExtensionName);
                    }
                }
                catch (Exception ex)
                {
                    ShowErrorMessage("Error getting AllInstanceExtensionNames", ex);
                    return;
                }

                try
                {
                    ShowInfoMessage("AllAvailableInstanceLayerNames:");

                    var allAvailableInstanceLayerNames = VulkanInstance.GetAllAvailableInstanceLayerNames();

                    if (allAvailableInstanceLayerNames != null)
                    {
                        foreach (var instanceLayerName in allAvailableInstanceLayerNames)
                            ShowInfoMessage($"  {instanceLayerName}");
                    }
                }
                catch (Exception ex)
                {
                    ShowErrorMessage("Error calling GetAllAvailableInstanceLayerNames", ex);
                    return;
                }


                PhysicalDeviceDetails[] allPhysicalDeviceDetails;

                try
                {
                    allPhysicalDeviceDetails = vulkanInstance.AllPhysicalDeviceDetails;

                    ShowInfoMessage("AllPhysicalDeviceDetails:");
                    foreach (var onePhysicalDeviceDetail in allPhysicalDeviceDetails)
                    {
                        ShowInfoMessage($"  {onePhysicalDeviceDetail.DeviceName}: {onePhysicalDeviceDetail.DeviceProperties.DeviceType}, API: {onePhysicalDeviceDetail.DeviceApiVersion}");
                        ShowInfoMessage("  AllDeviceExtensionNames:");
                        foreach (var extensionName in onePhysicalDeviceDetail.AllDeviceExtensionNames)
                            ShowInfoMessage($"    {extensionName}");
                    }
                    
                }
                catch (Exception ex)
                {
                    ShowErrorMessage("Error creating AllPhysicalDeviceDetails", ex);
                    return;
                }

                if (allPhysicalDeviceDetails.Length > 0)
                {
                    try
                    {
                        var vulkanDevice = VulkanDevice.Create(vulkanInstance, allPhysicalDeviceDetails[0], vulkanInstance.CreateOptions, SurfaceKHR.Null);
                        ShowInfoMessage("VulkanDevice created for the first physical device");
                    }
                    catch (Exception ex)
                    {
                        ShowErrorMessage("Error creating Vulkan device", ex);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error creating Vulkan device", ex);
            }
        }
    }

    public class AvaloniaAvaloniaInteropInfoControl : Control
    {
        private Action<string> _logMessageAction;
        private Action<string, Exception> _logErrorAction;

        public AvaloniaAvaloniaInteropInfoControl(Action<string> logMessageAction, Action<string, Exception> logErrorAction)
        {
            _logMessageAction = logMessageAction;
            _logErrorAction = logErrorAction;
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (Design.IsDesignMode)
                return;

            _logMessageAction("AvaloniaInteropInfoControl: OnAttachedToVisualTree");

            OnControlLoaded();
        }

        private async void OnControlLoaded()
        {
            await InitializeAvaloniaGpuInterop();
        }

        private async Task InitializeAvaloniaGpuInterop()
        {
            try
            {
                var selfVisual = ElementComposition.GetElementVisual(this)!;
                var avaloniaCompositor = selfVisual.Compositor;

                _logMessageAction("AvaloniaInteropInfoControl: get Compositor");

                var avaloniaCompositionDrawingSurfaceSurface = avaloniaCompositor.CreateDrawingSurface();

                _logMessageAction("AvaloniaInteropInfoControl: CreateDrawingSurface");

                var avaloniaVisual = avaloniaCompositor.CreateSurfaceVisual();
                avaloniaVisual.Size = new Vector2((float)Bounds.Width, (float)Bounds.Height);
                avaloniaVisual.Surface = avaloniaCompositionDrawingSurfaceSurface;

                _logMessageAction($"AvaloniaInteropInfoControl: CreateSurfaceVisual ({Bounds.Width} x {Bounds.Height})");

                ElementComposition.SetElementChildVisual(this, avaloniaVisual);

                _logMessageAction("AvaloniaInteropInfoControl: SetElementChildVisual");


                var avaloniaGpuInterop = await avaloniaCompositor.TryGetCompositionGpuInterop();
                if (avaloniaGpuInterop == null)
                {
                    _logMessageAction("ERROR: AvaloniaInteropInfoControl: TryGetCompositionGpuInterop returned null");
                    return;
                }

                if (avaloniaGpuInterop.SupportedImageHandleTypes == null || avaloniaGpuInterop.SupportedImageHandleTypes.Count == 0)
                {
                    _logMessageAction("ERROR: AvaloniaInteropInfoControl: avaloniaGpuInterop.SupportedImageHandleTypes is null or empty");
                    return;
                }

                _logMessageAction("AvaloniaInteropInfoControl: avaloniaGpuInterop.SupportedImageHandleTypes: " + string.Join(", ", avaloniaGpuInterop.SupportedImageHandleTypes));

                foreach (var supportedImageHandleType in avaloniaGpuInterop.SupportedImageHandleTypes)
                {
                    _logMessageAction($"AvaloniaInteropInfoControl: avaloniaGpuInterop.GetSynchronizationCapabilities for {supportedImageHandleType}: " + avaloniaGpuInterop.GetSynchronizationCapabilities(supportedImageHandleType));
                }

                _logMessageAction("AvaloniaInteropInfoControl: avaloniaGpuInterop.SupportedSemaphoreTypes: " + string.Join(", ", avaloniaGpuInterop.SupportedSemaphoreTypes));
            }
            catch (Exception ex)
            {
                _logErrorAction("AvaloniaInteropInfoControl error", ex);
            }
        }
    }
}
