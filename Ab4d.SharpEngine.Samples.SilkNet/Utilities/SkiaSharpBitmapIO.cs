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

        /// <inheritdoc/>
        [Obsolete("ConvertToSupportedFormat is obsolete. Please use the ConvertToSupportedFormat property in the BitmapLoadOptions structure that can be passed to the LoadBitmap method.")]
        public bool ConvertToSupportedFormat { get; set; } = true;

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
        public bool IsStreamSupported() => true;

        /// <inheritdoc />
        public RawImageData LoadBitmap(string fileName, BitmapLoadOptions? options = null)
        {
            #pragma warning disable CS0618 // ConvertToSupportedFormat is obsolete
            var convertToSupportedFormat = options?.ConvertToSupportedFormat ?? ConvertToSupportedFormat;
            #pragma warning restore CS0618

            var premultiplyAlpha = options?.PremultiplyAlpha ?? true;

            var skBitmap = LoadSkBitmap(fileName, premultiplyAlpha);
            var rawImageData = CreateRawImageData(skBitmap, convertToSupportedFormat, premultiplyAlpha);

            return rawImageData ;
        }

        /// <inheritdoc />
        public RawImageData LoadBitmap(Stream fileStream, string fileExtension, BitmapLoadOptions? options = null)
        {
            #pragma warning disable CS0618 // ConvertToSupportedFormat is obsolete
            var convertToSupportedFormat = options?.ConvertToSupportedFormat ?? ConvertToSupportedFormat;
            #pragma warning restore CS0618

            var premultiplyAlpha = options?.PremultiplyAlpha ?? true;

            var skBitmap = LoadSkBitmap(fileStream, premultiplyAlpha);
            var rawImageData = CreateRawImageData(skBitmap, convertToSupportedFormat, premultiplyAlpha);

            return rawImageData ;
        }


        private SKBitmap LoadSkBitmap(Stream fileStream, bool premultiplyAlpha)
        {
            // This is equivalent to the SKBitmap.Decode(string) method, which ends up calling SKBitmap.Decode(SKCodec),
            // which in turn automatically forces bitmapInfo.AlphaType to SKAlphaType.Premul. Instead, we control the
            // alpha premultiplication via PremultiplyAlpha setting.
            using var codec = SKCodec.Create(fileStream);
            var bitmapInfo = codec.Info;
            if (premultiplyAlpha && bitmapInfo.AlphaType == SKAlphaType.Unpremul)
                bitmapInfo.AlphaType = SKAlphaType.Premul;
            bitmapInfo.ColorSpace = null;
            return SKBitmap.Decode(codec, bitmapInfo);
        }

        /// <summary>
        /// LoadSkBitmap methods loads the file with the specified file name and returns a Skia's SKBitmap.
        /// </summary>
        /// <param name="fileName">fileName</param>
        /// <param name="premultiplyAlpha">premultiply data with alpha channel or not (enabled by default)</param>
        /// <returns>Skia's SKBitmap</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public SKBitmap LoadSkBitmap(string fileName, bool premultiplyAlpha = true)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            // Use backslash on Windows and slash in other OS
            fileName = FileUtils.FixDirectorySeparator(fileName);

            if (!File.Exists(fileName))
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
                        var imageFromStream = LoadSkBitmap(fileStream, premultiplyAlpha);
                        fileStream.Close();

                        return imageFromStream;
                    }
                }

                if (!isResolved)
                {
                    // Use CommonFileNotFoundResolver that tries to find the file
                    isResolved = FileUtils.CommonFileNotFoundResolver(ref fileName);
                }

                if (!isResolved)
                    throw new FileNotFoundException("File not found: " + fileName, fileName); // Throw exception here because System.Drawing.Bitmap throws "Parameter invalid" exception when file does not exist (?!)
            }

            // This is equivalent to the SKBitmap.Decode(string) method, which ends up calling SKBitmap.Decode(SKCodec),
            // which in turn automatically forces bitmapInfo.AlphaType to SKAlphaType.Premul. Instead, we control the
            // alpha premultiplication via PremultiplyAlpha setting.
            using var codec = SKCodec.Create(fileName);
            var bitmapInfo = codec.Info;
            if (premultiplyAlpha && bitmapInfo.AlphaType == SKAlphaType.Unpremul)
                bitmapInfo.AlphaType = SKAlphaType.Premul;
            bitmapInfo.ColorSpace = null;
            return SKBitmap.Decode(codec, bitmapInfo);
        }

        private RawImageData CreateRawImageData(SKBitmap skBitmap, bool convertToSupportedFormat, bool premultiplyAlpha)
        {
            RawImageData rawImageData ;

            if (skBitmap.BytesPerPixel == 4)
            {
                Format vkFormat = skBitmap.ColorType == SKColorType.Rgba8888 ? Format.R8G8B8A8Unorm
                                                                             : Format.B8G8R8A8Unorm;

                rawImageData  = new RawImageData(skBitmap.Width, skBitmap.Height, skBitmap.RowBytes, vkFormat, skBitmap.Bytes, checkTransparency: false);

                if (skBitmap.AlphaType == SKAlphaType.Opaque)
                {
                    rawImageData.HasTransparentPixels = false;
                    rawImageData.IsPreMultipliedAlpha = premultiplyAlpha; // be consistent with premultiplyAlpha setting
                }
                else
                {
                    // if we already have premultipled alpha then we just need to check for transparency
                    if (skBitmap.AlphaType == SKAlphaType.Premul)
                    {
                        rawImageData.CheckTransparency(); // check transparency is faster then CheckTransparencyAndConvertToPremultipliedAlpha
                        rawImageData.IsPreMultipliedAlpha = true;
                    }
                    else if (premultiplyAlpha)
                    {
                        rawImageData.CheckTransparencyAndConvertToPremultipliedAlpha();
                    }
                    else
                    {
                        rawImageData.CheckTransparency();
                        rawImageData.IsPreMultipliedAlpha = false;
                    }
                }
            }
            else if (skBitmap.BytesPerPixel == 1) //.ColorType == SKColorType.Gray8)
            {
                // Single-channel grayscale image; needs to be converted to RGBA for compatibility with the rest of the code...
                if (convertToSupportedFormat)
                {
                    var rgbaBytes = ConvertGrayscaleImageDataToBgra(skBitmap.Width, skBitmap.Height, skBitmap.RowBytes, skBitmap.Bytes);
                    rawImageData = new RawImageData(skBitmap.Width, skBitmap.Height, skBitmap.Width * 4, Format.R8G8B8A8Unorm, rgbaBytes, checkTransparency: false);
                }
                else
                {
                    rawImageData = new RawImageData(skBitmap.Width, skBitmap.Height, skBitmap.RowBytes, Format.R8Unorm, skBitmap.Bytes, checkTransparency: false);
                }

                // No transparency in grayscale (8 bit) images
                rawImageData.HasTransparentPixels = false;
                rawImageData.IsPreMultipliedAlpha = premultiplyAlpha; // be consistent with premultiplyAlpha setting
            }
            else
            {
                throw new NotSupportedException("Unsupported bitmap image format. Only images with four bytes (32 bit) per pixel or one byte (8 bit) per pixel are supported");
            }

            return rawImageData ;
        }

        /// <inheritdoc />
        public void SaveBitmap(RawImageData imageData, string fileName)
        {
            var fileExtension = Path.GetExtension(fileName);
            var fileFormat = GetImageFormatFromFileExtension(fileExtension);
            SaveBitmap(imageData, fileName, fileFormat);
        }

        /// <inheritdoc />
        public void SaveBitmap(RawImageData imageData, Stream fileStream, string fileExtension)
        {
            var fileFormat = GetImageFormatFromFileExtension(fileExtension);
            SaveBitmap(imageData, fileStream, fileFormat);
        }

        /// <summary>
        /// Save bitmap from the RawImageData to the specified file and by using special file format options that are defied by the fileFormat and quality parameters.
        /// </summary>
        /// <param name="imageData">RawImageData</param>
        /// <param name="fileName">fileName</param>
        /// <param name="fileFormat">SKEncodedImageFormat</param>
        /// <param name="quality">quality (80 by default)</param>
        public void SaveBitmap(RawImageData imageData, string fileName, SKEncodedImageFormat fileFormat, int quality = 80)
        {
            using (var stream = File.OpenWrite(fileName))
            {
                SaveBitmap(imageData, stream, fileFormat, quality);
            }
        }

        /// <summary>
        /// Save bitmap from the RawImageData to the specified fileStream and by using special file format options that are defied by the fileFormat and quality parameters.
        /// </summary>
        /// <param name="imageData">RawImageData</param>
        /// <param name="fileStream">fileStream</param>
        /// <param name="fileFormat">SKEncodedImageFormat</param>
        /// <param name="quality">quality (80 by default)</param>
        public void SaveBitmap(RawImageData imageData, Stream fileStream, SKEncodedImageFormat fileFormat, int quality = 80)
        {
            var skBitmap = CreateSKBitmap(imageData);

            using (var data = skBitmap.Encode(fileFormat, quality))
            {
                data.SaveTo(fileStream);
            }
        }

        /// <summary>
        /// CreateSKBitmap method takes the <see cref="RawImageData"/> and returns the Skia's SKBitmap.
        /// </summary>
        /// <param name="imageData">RawImageData</param>
        /// <returns>Skia's SKBitmap</returns>
        public unsafe SKBitmap CreateSKBitmap(RawImageData imageData)
        {
            // create an empty bitmap
            var bitmap = new SKBitmap();

            // install the pixels with the color type of the pixel data
            var info = new SKImageInfo(imageData.Width, imageData.Height,
                GetSkiaFormat(imageData.Format),
                imageData.IsPreMultipliedAlpha ?? false ? SKAlphaType.Premul : SKAlphaType.Unpremul);

            // We need to provide memory that will store the data for the SKBitmap.
            // To prevent moving the data around with GC, we need to create unmanaged memory for that.
            var dataLength = imageData.Data.Length;
            var nativeBytesPtr = (nint)NativeMemory.AlignedAlloc((nuint)dataLength, alignment: 64); // 64-byte alignment for SIMD

            var sourceSpan = new ReadOnlySpan<byte>(imageData.Data);
            var destSpan = new Span<byte>((void*)nativeBytesPtr, dataLength);
            sourceSpan.CopyTo(destSpan);

            // Set the SKBitmap size and back buffer that points to _renderedSceneBytesPtr
            // IMPORTANT: This method does not copy the data from _renderedSceneBytesPtr to some internal storage
            //            so we need to maintain the memory until this SKBitmap is released - in this case the releaseProc Action is called.
            bitmap.InstallPixels(info, nativeBytesPtr, imageData.Stride, releaseProc: (address, context) => NativeMemory.AlignedFree((void*)address));

            return bitmap;
        }

        /// <summary>
        /// GetSkiaFormat returns the Skia's SKColorType from the Vulkan's format
        /// </summary>
        /// <param name="vulkanFormat">Vulkan's format</param>
        /// <returns>Skia's SKColorType</returns>
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
