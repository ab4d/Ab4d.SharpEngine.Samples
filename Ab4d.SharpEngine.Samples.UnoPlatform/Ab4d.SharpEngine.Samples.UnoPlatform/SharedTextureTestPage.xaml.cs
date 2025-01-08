using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.Vulkan;
using Microsoft.UI.Composition;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using Uno.WinUI.Graphics2DSK;
using static Ab4d.SharpEngine.Samples.UnoPlatform.SharedTextureTestPage;

namespace Ab4d.SharpEngine.Samples.UnoPlatform
{
    // The SharedTextureTestPage tries to provide a way to show shared Vulkan texture with Uno platform.
    // 
    // The code currently only shows a gray rectangle. This shows that the TestSharedTextureElement is rendered by using canvas.DrawRect.
    // Instead of that, a Vulkan texture should be rendered. The texture is a color gradient that is generated in CreateCustomTextureByteArray.
    // This does not work because the SKSurface cannot be created from GRBackendRenderTarget.
    
    // NOTE:
    // To test using SharpEngine with Uno platform by coping the rendered texture to SKBitmap,
    // uncomment the line 44 and comment line 45 in App.xaml.cs

    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
	public sealed partial class SharedTextureTestPage : Page
	{
		public SharedTextureTestPage()
		{
			this.InitializeComponent();


            var vulkanDevice = Ab4d.SharpEngine.Vulkan.VulkanDevice.Create(new EngineCreateOptions());

            var customVulkanTexture = CreateCustomVulkanTexture(gpuDevice: vulkanDevice, width: 256, height: 256, alphaValue: 1, generateMipMaps: false);


            _testSharedTextureElement = new TestSharedTextureElement();
            _testSharedTextureElement.Image = customVulkanTexture;
            _testSharedTextureElement.Width = 256;
            _testSharedTextureElement.Height = 256;
            _testSharedTextureElement.HorizontalAlignment = HorizontalAlignment.Left;
            _testSharedTextureElement.VerticalAlignment = VerticalAlignment.Top;

            TestGrid.Children.Add(_testSharedTextureElement);

            _testSharedTextureElement.Invalidate();
        }

        //// Error:
        //// System.NotSupportedException: 'SKSwapChainPanel is not supported for Skia based platforms'
        //public class TestSwapChainPanel : SKSwapChainPanel
        //{
        //    /// <inheritdoc />
        //    public TestSwapChainPanel()
        //    {
        //        this.Loaded += TestSwapChainPanel_Loaded;
        //    }

        //    private void TestSwapChainPanel_Loaded(object sender, RoutedEventArgs e)
        //    {
        //    }

        //    /// <inheritdoc />
        //    protected override void OnPaintSurface(SKPaintGLSurfaceEventArgs e)
        //    {
        //        //var grGlFramebufferInfo = e.BackendRenderTarget.GetGlFramebufferInfo();
        //        //var framebufferObjectId = grGlFramebufferInfo.FramebufferObjectId;

        //        base.OnPaintSurface(e);
        //    }
        //}

        public class TestSharedTextureElement : SKCanvasElement
        {
            public GpuImage? Image;
            private SKSurface? _skSurface;

            /// <inheritdoc />
            public TestSharedTextureElement()
            {
                this.Loaded += OnLoaded;
            }

            private void OnLoaded(object sender, RoutedEventArgs e)
            {
                this.Invalidate();
            }

            /// <inheritdoc />
            protected override void RenderOverride(SKCanvas canvas, Size area)
            {
                if (Image == null)
                    return;

                if (_skSurface == null)
                    InitSkiaSurface();

                if (_skSurface == null)
                    canvas.DrawRect(0, 0, (float)area.Width, (float)area.Height, new SKPaint() { Color = new SKColor(100, 100, 100)});
                else
                    canvas.DrawSurface(_skSurface, 0, 0);
            }

            private void InitSkiaSurface()
            {
                if (Image == null)
                    return;

                var vulkanDevice = Image.GpuDevice;

                uint vkFormat = (uint)Ab4d.Vulkan.Format.B8G8R8A8Srgb; //(uint)Image.ImageCreateInfo.Format;  (uint)SKColorType.Bgra8888 = 6
                

                GRVkImageInfo imageInfo = new GRVkImageInfo()
                {
                    CurrentQueueFamily = vulkanDevice.QueueFamilyIndices.GraphicsFamily,
                    Format = vkFormat,
                    Image = Image.Image.Handle,
                    ImageLayout = (uint)ImageLayout.TransferSrcOptimal,
                    ImageTiling = (uint)ImageTiling.Optimal,
                    LevelCount = 1,
                    Protected = false,
                    Alloc = new GRVkAlloc()
                    {
                        Memory = Image.ImageMemory.DeviceMemory.Handle,
                        
                        // enum Flag {
                        //     kNoncoherent_Flag     = 0x1,   // memory must be flushed to device after mapping
                        //     kMappable_Flag        = 0x2,   // memory is able to be mapped.
                        //     kLazilyAllocated_Flag = 0x4,   // memory was created with lazy allocation
                        // };
                        Flags = 0,
                        Offset = Image.ImageMemory.MemoryOffset,
                        Size = Image.ImageMemory.Size
                    },
                    SharingMode = 0, // SharingMode.Exclusive
                    ImageUsageFlags = (uint)Image.ImageCreateInfo.Usage,
                    SampleCount = 1,
                    //YcbcrConversionInfo = new GrVkYcbcrConversionInfo() {}
                };

                var renderTarget = new GRBackendRenderTarget(Image.Width, Image.Height, sampleCount: 1, imageInfo);

                if (!renderTarget.IsValid)
                    return;

                // UH: The following returns empty struct
                var grGlFramebufferInfo = renderTarget.GetGlFramebufferInfo();


                // VulkanContext cannot be created (grContext is null)
                //var grContext = GRContext.CreateVulkan(new GRVkBackendContext());

                //var grContextOptions = new GRContextOptions()
                //{
                    // Nothing special to define here
                //};
                var grContext = GRContext.CreateGl();

                // The following is called:
                // public static SKSurface Create(GRRecordingContext context, GRBackendRenderTarget renderTarget, GRSurfaceOrigin origin, SKColorType colorType, SKColorSpace colorspace, SKSurfaceProperties props)
                // {
                // 	if (context == null)
                // 	{
                // 		throw new ArgumentNullException("context");
                // 	}
                // 	if (renderTarget == null)
                // 	{
                // 		throw new ArgumentNullException("renderTarget");
                // 	}
                // 	return GetObject(SkiaApi.sk_surface_new_backend_render_target(context.Handle, renderTarget.Handle, origin, colorType.ToNative(), colorspace?.Handle ?? IntPtr.Zero, props?.Handle ?? IntPtr.Zero));
                // }
                //
                // I do not know what sk_surface_new_backend_render_target is.
                // It seems that it is:
                // Skia\skia\include\gpu\ganesh\SkSurfaceGanesh.h
                // /** Wraps a GPU-backed buffer into SkSurface. Caller must ensure backendRenderTarget
                //     is valid for the lifetime of returned SkSurface.
                // 
                //     SkSurface is returned if all parameters are valid. backendRenderTarget is valid if
                //     its pixel configuration agrees with colorSpace and context; for instance, if
                //     backendRenderTarget has an sRGB configuration, then context must support sRGB,
                //     and colorSpace must be present. Further, backendRenderTarget width and height must
                //     not exceed context capabilities, and the context must be able to support
                //     back-end render targets.
                // 
                //     Upon success releaseProc is called when it is safe to delete the render target in the
                //     backend API (accounting only for use of the render target by this surface). If SkSurface
                //     creation fails releaseProc is called before this function returns.
                // 
                //     @param context                  GPU context
                //     @param backendRenderTarget      GPU intermediate memory buffer
                //     @param colorSpace               range of colors
                //     @param surfaceProps             LCD striping orientation and setting for device independent
                //                                     fonts; may be nullptr
                //     @param releaseProc              function called when backendRenderTarget can be released
                //     @param releaseContext           state passed to releaseProc
                //     @return                         SkSurface if all parameters are valid; otherwise, nullptr
                // */
                // SK_API sk_sp<SkSurface> WrapBackendRenderTarget(GrRecordingContext* context,
                //                                                 const GrBackendRenderTarget& backendRenderTarget,
                //                                                 GrSurfaceOrigin origin,
                //                                                 SkColorType colorType,
                //                                                 sk_sp<SkColorSpace> colorSpace,
                //                                                 const SkSurfaceProps* surfaceProps,
                //                                                 RenderTargetReleaseProc releaseProc = nullptr,
                //                                                 ReleaseContext releaseContext = nullptr);
                //
                // This is defined in:
                // Skia\skia\src\gpu\ganesh\GrSurface.cpp
                // sk_sp<SkSurface> WrapBackendRenderTarget(GrRecordingContext* rContext,
                //                                          const GrBackendRenderTarget& rt,
                //                                          GrSurfaceOrigin origin,
                //                                          SkColorType colorType,
                //                                          sk_sp<SkColorSpace> colorSpace,
                //                                          const SkSurfaceProps* props,
                //                                          RenderTargetReleaseProc relProc,
                //                                          ReleaseContext releaseContext) {
                //     auto releaseHelper = skgpu::RefCntedCallback::Make(relProc, releaseContext);
                // 
                //     if (!rContext || !rt.isValid()) {
                //         return nullptr;
                //     }
                // 
                //     GrColorType grColorType = SkColorTypeToGrColorType(colorType);
                //     if (grColorType == GrColorType::kUnknown) {
                //         return nullptr;
                //     }
                // 
                //     if (!validate_backend_render_target(rContext->priv().caps(), rt, grColorType)) {
                //         return nullptr;
                //     }
                // 
                //     auto proxyProvider = rContext->priv().proxyProvider();
                //     auto proxy = proxyProvider->wrapBackendRenderTarget(rt, std::move(releaseHelper));
                //     if (!proxy) {
                //         return nullptr;
                //     }
                // 
                //     auto device = rContext->priv().createDevice(grColorType,
                //                                                 std::move(proxy),
                //                                                 std::move(colorSpace),
                //                                                 origin,
                //                                                 SkSurfacePropsCopyOrDefault(props),
                //                                                 skgpu::ganesh::Device::InitContents::kUninit);
                //     if (!device) {
                //         return nullptr;
                //     }
                // 
                //     return sk_make_sp<SkSurface_Ganesh>(std::move(device));
                // }
                //
                // bool validate_backend_render_target(const GrCaps* caps,
                //                                     const GrBackendRenderTarget& rt,
                //                                     GrColorType grCT) {
                //     if (!caps->areColorTypeAndFormatCompatible(grCT, rt.getBackendFormat())) {
                //         return false;
                //     }
                // 
                //     if (!caps->isFormatAsColorTypeRenderable(grCT, rt.getBackendFormat(), rt.sampleCnt())) {
                //         return false;
                //     }
                // 
                //     // We require the stencil bits to be either 0, 8, or 16.
                //     int stencilBits = rt.stencilBits();
                //     if (stencilBits != 0 && stencilBits != 8 && stencilBits != 16) {
                //         return false;
                //     }
                // 
                //     return true;
                // }
                //
                // bool GrCaps::areColorTypeAndFormatCompatible(GrColorType grCT,
                //                                              const GrBackendFormat& format) const {
                //     if (GrColorType::kUnknown == grCT) {
                //         return false;
                //     }
                // 
                //     SkTextureCompressionType compression = GrBackendFormatToCompressionType(format);
                //     if (compression != SkTextureCompressionType::kNone) {
                //         return grCT == (SkTextureCompressionTypeIsOpaque(compression) ? GrColorType::kRGB_888x
                //                                                                       : GrColorType::kRGBA_8888);
                //     }
                // 
                //     return this->onAreColorTypeAndFormatCompatible(grCT, format);
                // }
                //
                // virtual bool onAreColorTypeAndFormatCompatible(GrColorType, const GrBackendFormat&) const = 0;
                //
                // bool GrVkCaps::onAreColorTypeAndFormatCompatible(GrColorType ct,
                //                                                  const GrBackendFormat& format) const {
                //     VkFormat vkFormat;
                //     if (!GrBackendFormats::AsVkFormat(format, &vkFormat)) {
                //         return false;
                //     }
                //     const skgpu::VulkanYcbcrConversionInfo* ycbcrInfo =
                //             GrBackendFormats::GetVkYcbcrConversionInfo(format);
                //     SkASSERT(ycbcrInfo);
                // 
                //     if (ycbcrInfo->isValid() && !skgpu::VkFormatNeedsYcbcrSampler(vkFormat)) {
                //         // Format may be undefined for external images, which are required to have YCbCr conversion.
                //         if (VK_FORMAT_UNDEFINED == vkFormat && ycbcrInfo->fExternalFormat != 0) {
                //             return true;
                //         }
                //         return false;
                //     }
                // 
                //     const auto& info = this->getFormatInfo(vkFormat);
                //     for (int i = 0; i < info.fColorTypeInfoCount; ++i) {
                //         if (info.fColorTypeInfos[i].fColorType == ct) {
                //             return true;
                //         }
                //     }
                //     return false;
                // }
                //
                // Skia\skia\src\gpu\ganesh\vk\GrVkBackendSurface.cpp:
                // GrBackendFormat getBackendFormat() const override {
                //     auto info = GrVkImageInfoWithMutableState(fVkInfo, fMutableState.get());
                //     bool usesDRMModifier = info.fImageTiling == VK_IMAGE_TILING_DRM_FORMAT_MODIFIER_EXT;
                //     if (info.fYcbcrConversionInfo.isValid()) {
                //         SkASSERT(info.fFormat == info.fYcbcrConversionInfo.fFormat);
                //         return GrBackendFormats::MakeVk(info.fYcbcrConversionInfo, usesDRMModifier);
                //     }
                //     return GrBackendFormats::MakeVk(info.fFormat, usesDRMModifier);
                // }
                //
                // Skia\skia\src\gpu\ganesh\vk\GrVkBackendSurface.cpp
                // GrBackendFormat MakeVk(VkFormat format, bool willUseDRMFormatModifiers) {
                //     return GrBackendSurfacePriv::MakeGrBackendFormat(
                //             GrTextureType::k2D,
                //             GrBackendApi::kVulkan,
                //             GrVkBackendFormatData(format, skgpu::VulkanYcbcrConversionInfo{}));
                // }


                // It seems that no combination of SRGB / Unorm solves the problem (_skSurface is still null)
                //_skSurface = SKSurface.Create(grContext, renderTarget, GRSurfaceOrigin.TopLeft, SKColorType.Bgra8888, SKColorSpace.CreateRgb(SKColorSpaceTransferFn.Srgb, SKColorSpaceXyz.Srgb));
                
                _skSurface = SKSurface.Create(grContext, renderTarget, GRSurfaceOrigin.TopLeft, SKColorType.Bgra8888, SKColorSpace.CreateSrgb());

                if (_skSurface == null)
                {

                }

                //   See also: https://github.com/mono/SkiaSharp/issues/1502
                //             https://groups.google.com/g/skia-discuss/c/AuOhKGPe0BI?pli=1
            }
        }



        private Ab4d.SharpEngine.Core.GpuImage? _testCustomTexture;
        private TestSharedTextureElement _testSharedTextureElement;

        private Ab4d.SharpEngine.Core.GpuImage CreateCustomVulkanTexture(VulkanDevice gpuDevice, int width, int height, float alphaValue, bool generateMipMaps)
        {
            int imageStride = width * 4;
            var imageBytes = CreateCustomTextureByteArray(width, height, alphaValue);

            var rawImageData = new RawImageData(width, height, imageStride, Ab4d.Vulkan.Format.B8G8R8A8Unorm, imageBytes, checkTransparency: false);

            var gpuImage = new Ab4d.SharpEngine.Core.GpuImage(
                gpuDevice,
                rawImageData,
                generateMipMaps,
                createImageView: false,
                imageUsage: ImageUsageFlags.ColorAttachment | ImageUsageFlags.TransferDst | ImageUsageFlags.TransferSrc | ImageUsageFlags.Sampled,
                accessFlags: AccessFlags.TransferWrite,
                imageLayout: ImageLayout.TransferSrcOptimal,
                stageFlags: PipelineStageFlags.Transfer,
                imageSource: "CustomTexture")
            {
                IsPreMultipliedAlpha = true,
                HasTransparentPixels = alphaValue < 1,
            };

            return gpuImage;
        }


        private byte[] CreateCustomTextureByteArray(int width, int height, float alphaValue)
        {
            int imageStride = width * 4;
            var imageBytes = new byte[imageStride * height];

            float widthFactor = 255.0f / width;
            float heightFactor = 255.0f / height;

            byte red, green, blue;
            green = 0;

            byte alpha = (byte)(255 * alphaValue);

            for (int y = 0; y < height; y++)
            {
                int pos = y * imageStride;

                red = (byte)(y * heightFactor);

                // Duplicate for loop to remove multiplying by alphaValue in non-transparent code path
                if (alphaValue < 1)
                {
                    for (int x = 0; x < width; x++)
                    {
                        blue = (byte)(x * widthFactor);

                        // we have B8G8R8A8 format and memory layout
                        // In case of using alpha we need to convert to pre-multiplied alpha value
                        imageBytes[pos] = (byte)(blue * alphaValue);
                        imageBytes[pos + 1] = (byte)(green * alphaValue);
                        imageBytes[pos + 2] = (byte)(red * alphaValue);
                        imageBytes[pos + 3] = alpha; // alpha

                        pos += 4;
                    }
                }
                else
                {
                    for (int x = 0; x < width; x++)
                    {
                        blue = (byte)(x * widthFactor);

                        // we have B8G8R8A8 format and memory layout
                        imageBytes[pos] = blue;
                        imageBytes[pos + 1] = green;
                        imageBytes[pos + 2] = red;
                        imageBytes[pos + 3] = alpha; // alpha

                        pos += 4;
                    }
                }
            }

            return imageBytes;
        }
	}
}
