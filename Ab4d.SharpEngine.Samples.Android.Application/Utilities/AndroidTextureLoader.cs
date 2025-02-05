//#define ADVANCED_TIME_MEASUREMENT

using System;
using System.IO;
using System.Xml.Linq;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.Vulkan;
using Android.Content.Res;

namespace Ab4d.SharpEngine.Utilities;

public class AndroidTextureLoader
{
#if ADVANCED_TIME_MEASUREMENT
    public static double LoadBitmapTimeMs;
    public static double CreateGpuImageTimeMs;
#endif

    public static StandardMaterial CreateTextureMaterial(Resources resources,
                                                         int drawableId,
                                                         AndroidBitmapIO bitmapIO,
                                                         VulkanDevice gpuDevice,
                                                         CommonSamplerTypes samplerType = CommonSamplerTypes.Wrap,
                                                         bool generateMipMaps = true,
                                                         bool isDeviceLocal = true,
                                                         float alphaClipThreshold = 0)
    {
        var gpuTexture = CreateTexture(resources, drawableId, bitmapIO, gpuDevice, generateMipMaps, isDeviceLocal);
        var materialWithTexture = new StandardMaterial(gpuTexture, samplerType, name: gpuTexture.Name);

        if (alphaClipThreshold > 0)
            materialWithTexture.AlphaClipThreshold = alphaClipThreshold;

        return materialWithTexture;
    }

    public static GpuImage CreateTexture(Resources resources,
                                         int drawableId,
                                         AndroidBitmapIO bitmapIO,
                                         VulkanDevice gpuDevice,
                                         bool generateMipMaps = true,
                                         bool cacheGpuTexture = false)
    {
        ArgumentNullException.ThrowIfNull(resources);

        GpuImage? gpuImage;
        string? imageName = $"android_drawable_{drawableId}";

        if (cacheGpuTexture)
            gpuImage = gpuDevice.GetCachedObject<GpuImage?>(imageName);
        else
            gpuImage = null;

        if (gpuImage == null)
        {
#if ADVANCED_TIME_MEASUREMENT
            var startTime = DateTime.Now;
#endif

            var rawImageData = bitmapIO.LoadBitmap(resources, drawableId);

            if (rawImageData == null)
                throw new System.IO.FileNotFoundException($"Cannot find texture with drawable id {drawableId}");

#if ADVANCED_TIME_MEASUREMENT
            var loadedTime = DateTime.Now;
            LoadBitmapTimeMs += (loadedTime - startTime).TotalMilliseconds;
#endif

            gpuImage = new GpuImage(gpuDevice, rawImageData, generateMipMaps, imageSource: imageName);

#if ADVANCED_TIME_MEASUREMENT
            CreateGpuImageTimeMs += (DateTime.Now - loadedTime).TotalMilliseconds;
#endif

            if (cacheGpuTexture)
                gpuDevice.CacheObject(imageName, gpuImage);
        }

        return gpuImage;
    }

    public static async Task<GpuImage> CreateTextureAsync(Resources resources,
                                                          int drawableId,
                                                          AndroidBitmapIO bitmapIO,
                                                          VulkanDevice gpuDevice,
                                                          bool generateMipMaps = true,
                                                          bool cacheGpuTexture = false)
    {
        ArgumentNullException.ThrowIfNull(resources);

        GpuImage? gpuImage;
        string? imageName = $"android_drawable_{drawableId}";

        if (cacheGpuTexture)
            gpuImage = gpuDevice.GetCachedObject<GpuImage?>(imageName);
        else
            gpuImage = null;

        if (gpuImage == null)
        {
#if ADVANCED_TIME_MEASUREMENT
            var startTime = DateTime.Now;
#endif

            RawImageData? imageData = null;

            await Task.Run(() =>
            {
                imageData = bitmapIO.LoadBitmap(resources, drawableId);
            });
            
            if (imageData == null)
                throw new System.IO.FileNotFoundException($"Cannot find texture with drawable id {drawableId}");


#if ADVANCED_TIME_MEASUREMENT
            var loadedTime = DateTime.Now;
            LoadBitmapTimeMs += (loadedTime - startTime).TotalMilliseconds;
#endif

            gpuImage = new GpuImage(gpuDevice,
                                    width: imageData.Width,
                                    height: imageData.Height,
                                    format: imageData.Format,
                                    usage: generateMipMaps ? ImageUsageFlags.TransferSrc | ImageUsageFlags.TransferDst | ImageUsageFlags.Sampled : ImageUsageFlags.TransferDst | ImageUsageFlags.Sampled,
                                    memoryProperties: MemoryPropertyFlags.DeviceLocal, // MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent is not supported on NVIDIA
                                    mipsCount: generateMipMaps ? 0 : 1,         // when 0 then get mips count from size
                                    sampleCount: SampleCountFlags.SampleCount1,
                                    tiling: ImageTiling.Optimal,
                                    initialImageLayout: generateMipMaps ? ImageLayout.Preinitialized : ImageLayout.Undefined,
                                    aspectMask: 0,
                                    createImageView: true,
                                    memorySizeAlignment: 0,
                                    name: imageName)
            {
                Source = imageName,
                HasTransparentPixels = imageData.HasTransparentPixels ?? false,
                IsPreMultipliedAlpha = imageData.IsPreMultipliedAlpha ?? false
            };

            await gpuImage.CopyDataToImageAsync(imageData, 
                                                newLayout: ImageLayout.ShaderReadOnlyOptimal,
                                                newAccessFlags: AccessFlags.ShaderRead,
                                                newStageFlags: PipelineStageFlags.FragmentShader);

#if ADVANCED_TIME_MEASUREMENT
            CreateGpuImageTimeMs += (DateTime.Now - loadedTime).TotalMilliseconds;
#endif

            if (cacheGpuTexture)
                gpuDevice.CacheObject(imageName, gpuImage);
        }

        return gpuImage;
    }
}