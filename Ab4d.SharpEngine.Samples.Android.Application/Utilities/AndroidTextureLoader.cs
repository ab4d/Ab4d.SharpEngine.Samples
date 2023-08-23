using System;
using System.Xml.Linq;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Android.Content.Res;

namespace Ab4d.SharpEngine.Utilities;

public class AndroidTextureLoader
{
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
                                         bool isDeviceLocal = true,
                                         bool cacheGpuTexture = false)
    {
        if (resources == null) throw new ArgumentNullException(nameof(resources));

        var rawImageData = bitmapIO.LoadBitmap(resources, drawableId);

        if (rawImageData == null)
            throw new System.IO.FileNotFoundException($"Cannot find texture with drawable id {drawableId}");

        GpuImage? gpuImage;
        string? imageName = $"android_drawable_{drawableId}";

        if (cacheGpuTexture)
            gpuImage = gpuDevice.GetCachedValue<GpuImage?>(imageName);
        else
            gpuImage = null;

        if (gpuImage == null)
        {
#if ADVANCED_TIME_MEASUREMENT
            var startTime = DateTime.Now;
#endif

            var imageData = bitmapIO.LoadBitmap(resources, drawableId);

#if ADVANCED_TIME_MEASUREMENT
            var loadedTime = DateTime.Now;
            LoadBitmapTimeMs += (loadedTime - startTime).TotalMilliseconds;
#endif

            gpuImage = new GpuImage(gpuDevice, imageData, generateMipMaps, isDeviceLocal, imageSource: imageName);

#if ADVANCED_TIME_MEASUREMENT
            CreateGpuImageTimeMs += (DateTime.Now - loadedTime).TotalMilliseconds;
#endif

            if (cacheGpuTexture)
                gpuDevice.CacheValue(imageName, gpuImage);
        }

        return gpuImage;
    }
}