using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Utilities;
using SkiaSharp;
using Format = Ab4d.Vulkan.Format;

namespace Ab4d.SharpEngine.Samples.Utilities
{
    /// <summary>
    /// SkiaSharpBitmapIO uses SkiaSharp to support bitmap IO operations.
    /// </summary>
    public class SkiaSharpBitmapIO : IBitmapIO
    {
        private static readonly string[] SupportedLoadFileExtensions = { "png", "jpg", "bmp", "tiff", "gif" }; // TODO: Add other skia supported formats
        private static readonly (string, SKEncodedImageFormat)[] SupportedSaveFileExtensions = {
            ("png",SKEncodedImageFormat.Png),
            ("jpg", SKEncodedImageFormat.Jpeg),
            ("gif", SKEncodedImageFormat.Gif),
            ("ico", SKEncodedImageFormat.Ico)
            // TODO: Add other skia supported formats
        };

        /// <summary>
        /// When true (by default) then the loaded images are converted into 32 bit BGRA format.
        /// When false then the loader tries to preserve the format of the bitmap (for example 8 bit for grayscale) but this is not guaranteed and the loader may still convert the image to BGRA.
        /// </summary>
        public bool ConvertToBgra { get; set; } = true;


        /// <inheritdoc />
        public Func<string, string?>? FileNotFoundResolver { get; set; }

        /// <inheritdoc />
        public Func<string, Stream?>? FileStreamResolver { get; set; }


        /// <inheritdoc />
        public bool IsFileFormatImportSupported(string fileExtension)
        {
            return IsFileFormatSupported(fileExtension, SupportedLoadFileExtensions);
        }

        /// <inheritdoc />
        public bool IsFileFormatExportSupported(string fileExtension)
        {
            return IsFileFormatSupported(fileExtension, SupportedSaveFileExtensions.Select(t => t.Item1).ToArray());
        }

        /// <inheritdoc />
        public bool IsStreamSupported()
        {
            return false;
        }

        /// <inheritdoc />
        public RawImageData LoadBitmap(string fileName)
        {
            var skBitmap = LoadSkBitmap(fileName);


            RawImageData gpuImageData;

            if (skBitmap.BytesPerPixel == 4)
            {
                if (skBitmap.ColorType == SKColorType.Bgra8888)
                {
                    // The most common
                    gpuImageData = new RawImageData(skBitmap.Width, skBitmap.Height, skBitmap.RowBytes, Format.B8G8R8A8Unorm, skBitmap.Bytes, checkTransparency: false);
                }
                else
                {
                    Format vkFormat;
                    if (!ConvertToBgra && skBitmap.ColorType == SKColorType.Rgba8888)
                        vkFormat = Format.R8G8B8A8Unorm; // preserve RGBA format
                    else
                        vkFormat = Format.B8G8R8A8Unorm; // Otherwise set format to BGRA

                    gpuImageData = new RawImageData(skBitmap.Width, skBitmap.Height, skBitmap.RowBytes, vkFormat, skBitmap.Bytes, checkTransparency: false);

                    if (skBitmap.ColorType == SKColorType.Rgba8888 && ConvertToBgra)
                        ConvertRgbaToBgra(gpuImageData.Data);
                }

                if (skBitmap.AlphaType == SKAlphaType.Opaque)
                {
                    gpuImageData.HasTransparentPixels = false;
                    gpuImageData.IsPreMultipliedAlpha = true;
                }
                else
                {
                    // if we already have premultipled alpha then we just need to check for transparency
                    if (skBitmap.AlphaType == SKAlphaType.Premul)
                    {
                        gpuImageData.CheckTransparency(); // check transparency is faster then CheckTransparencyAndConvertToPremultipliedAlpha
                        gpuImageData.IsPreMultipliedAlpha = true;
                    }
                    else
                    {
                        gpuImageData.CheckTransparencyAndConvertToPremultipliedAlpha();
                    }
                }
            }
            else if (skBitmap.BytesPerPixel == 1) //.ColorType == SKColorType.Gray8)
            {
                // Single-channel grayscale image; needs to be converted to RGBA for compatibility with the rest of the code...
                if (ConvertToBgra)
                {
                    var rgbaBytes = ConvertGrayscaleImageDataToBgra(skBitmap.Width, skBitmap.Height, skBitmap.RowBytes, skBitmap.Bytes);
                    gpuImageData = new RawImageData(skBitmap.Width, skBitmap.Height, skBitmap.Width * 4, Format.R8G8B8A8Unorm, rgbaBytes, checkTransparency: false);
                }
                else
                {
                    gpuImageData = new RawImageData(skBitmap.Width, skBitmap.Height, skBitmap.RowBytes, Format.R8Unorm, skBitmap.Bytes, checkTransparency: false);
                }

                // No transparency in grayscale (8 bit) images
                gpuImageData.HasTransparentPixels = false;
                gpuImageData.IsPreMultipliedAlpha = true;
            }
            else
            {
                throw new NotSupportedException("Unsupported bitmap image format. Only images with four bytes (32 bit) per pixel or one byte (8 bit) per pixel are supported");
            }

            return gpuImageData;
        }

        /// <inheritdoc />
        public RawImageData LoadBitmap(Stream fileStream, string fileExtension)
        {
            throw new System.NotImplementedException();
        }

        public SKBitmap LoadSkBitmap(string fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            // Use backslash on Windows and slash in other OS
            fileName = FileUtils.FixDirectorySeparator(fileName);

            if (!System.IO.File.Exists(fileName))
            {
                bool isResolved;

                if (FileNotFoundResolver != null)
                {
                    var resolvedFileName = FileNotFoundResolver(fileName);

                    if (resolvedFileName != null)
                    {
                        isResolved = File.Exists(resolvedFileName);

                        if (isResolved)
                            fileName = resolvedFileName;
                    }
                    else
                    {
                        isResolved = false;
                    }
                }
                else
                {
                    isResolved = false;
                }

                if (!isResolved && FileStreamResolver != null)
                {
                    var fileStream = FileStreamResolver(fileName);

                    if (fileStream != null)
                    {
                        var imageFromStream = SkiaSharp.SKBitmap.Decode(fileStream);
                        fileStream.Close();

                        return imageFromStream;
                    }
                }

                if (!isResolved)
                    throw new FileNotFoundException("File not found: " + fileName, fileName); // Throw exception here because System.Drawing.Bitmap throws "Parameter invalid" exception when file does not exist (?!)
            }

            var skBitmap = SkiaSharp.SKBitmap.Decode(fileName);

            return skBitmap;
        }

        public void SaveBitmap(RawImageData imageData, string fileName, SKEncodedImageFormat fileFormat, int quality = 80)
        {
            using (var stream = File.OpenWrite(fileName))
            {
                SaveBitmap(imageData, stream, fileFormat, quality);
            }
        }

        public void SaveBitmap(RawImageData imageData, string fileName)
        {
            var fileExtension = System.IO.Path.GetExtension(fileName);
            var fileFormat = GetImageFormatFromFileExtension(fileExtension);
            SaveBitmap(imageData, fileName, fileFormat);
        }

        /// <inheritdoc />
        public void SaveBitmap(RawImageData imageData, Stream fileStream, string fileExtension)
        {
            var fileFormat = GetImageFormatFromFileExtension(fileExtension);
            SaveBitmap(imageData, fileStream, fileFormat);
        }

        public void SaveBitmap(RawImageData imageData, Stream fileStream, SKEncodedImageFormat fileFormat, int quality = 80)
        {
            var skBitmap = CreateSKBitmap(imageData);

            using (var data = skBitmap.Encode(fileFormat, quality))
            {
                data.SaveTo(fileStream);
            }
        }


        public SKBitmap CreateSKBitmap(RawImageData imageData)
        {
            // create an empty bitmap
            var bitmap = new SKBitmap();

            // install the pixels with the color type of the pixel data
            var info = new SKImageInfo(imageData.Width, imageData.Height,
                GetSkiaFormat(imageData.Format),
                (imageData.IsPreMultipliedAlpha ?? false) ? SKAlphaType.Premul : SKAlphaType.Unpremul);

            var dataHandle = GCHandle.Alloc(imageData.Data, GCHandleType.Pinned);

            try
            {
                bitmap.InstallPixels(info, dataHandle.AddrOfPinnedObject(), imageData.Stride);
            }
            finally
            {
                dataHandle.Free();
            }

            return bitmap;
        }

        public static SKColorType GetSkiaFormat(Format vulkanFormat)
        {
            if (vulkanFormat == Format.R8G8B8A8Unorm)
                return SKColorType.Rgba8888;

            return SKColorType.Bgra8888;
        }

        private SKEncodedImageFormat GetImageFormatFromFileExtension(string fileExtension)
        {
            if (string.IsNullOrEmpty(fileExtension))
                throw new ArgumentException("Value cannot be null or empty.", nameof(fileExtension));

            if (fileExtension.StartsWith('.'))
                fileExtension = fileExtension.Substring(1);

            for (var i = 0; i < SupportedSaveFileExtensions.Length; i++)
            {
                if (fileExtension.Equals(SupportedSaveFileExtensions[i].Item1, StringComparison.OrdinalIgnoreCase))
                    return SupportedSaveFileExtensions[i].Item2;
            }

            throw new Exception("Unrecognized file extension: " + fileExtension);
        }

        private static bool IsFileFormatSupported(string fileExtension, string[] fileFormats)
        {
            if (fileExtension == null) throw new ArgumentNullException(nameof(fileExtension));

            if (fileExtension.StartsWith('.'))
                fileExtension = fileExtension.Substring(1);

            for (var i = 0; i < fileFormats.Length; i++)
            {
                if (fileExtension.Equals(fileFormats[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static void ConvertRgbaToBgra(byte[] imageDataArray)
        {
            for (int i = 0; i < imageDataArray.Length; i += 4)
            {
                // ReSharper disable once SwapViaDeconstruction
                var temp = imageDataArray[i + 0];
                imageDataArray[i + 0] = imageDataArray[i + 2];
                imageDataArray[i + 2] = temp;
            }
        }

        private static byte[] ConvertGrayscaleImageDataToBgra(int width, int height, int stride, byte[] grayscaleImageData)
        {
            var rgbaData = new byte[width * height * 4];
            var outIdx = 0; // Output data index

            for (var y = 0; y < height; y++)
            {
                var idx = y * stride; // Start of the row
                for (var x = 0; x < width; x++)
                {
                    var value = grayscaleImageData[idx + x];

                    rgbaData[outIdx] = value;     // R
                    rgbaData[outIdx + 1] = value; // G
                    rgbaData[outIdx + 2] = value; // B
                    rgbaData[outIdx + 3] = 255;   // A

                    outIdx += 4;
                }
            }
            return rgbaData;
        }
    }
}
