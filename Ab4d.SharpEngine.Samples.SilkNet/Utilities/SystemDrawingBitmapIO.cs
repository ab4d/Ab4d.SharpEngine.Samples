#if SYSTEM_DRAWING

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Utilities;
using Ab4d.Vulkan;

namespace Ab4d.SharpEngine.Samples.Utilities
{
    /// <summary>
    /// WinFormsBitmapIO uses System.Drawing namespace to support bitmap IO operations.
    /// </summary>
    public class SystemDrawingBitmapIO : IBitmapIO
    {
        private static readonly string[] SupportedLoadFileExtensions = { "png", "jpg", "bmp", "tiff", "gif" };
        private static readonly string[] SupportedSaveFileExtensions = { "png", "jpg", "gif" };

        /// <inheritdoc />
        public Func<string, string?>? FileNotFoundResolver { get; set; }

        /// <inheritdoc />
        public Func<string, Stream?>? FileStreamResolver { get; set; }

        /// <inheritdoc/>
        public bool ConvertToSupportedFormat { get; set; } = true;


        /// <inheritdoc />
        public bool IsFileFormatImportSupported(string fileExtension)
        {
            return IsFileFormatSupported(fileExtension, SupportedLoadFileExtensions);
        }

        /// <inheritdoc />
        public bool IsFileFormatExportSupported(string fileExtension)
        {
            return IsFileFormatSupported(fileExtension, SupportedSaveFileExtensions);
        }

        /// <inheritdoc />
        public bool IsStreamSupported()
        {
            return false;
        }

        /// <inheritdoc />
        public RawImageData LoadBitmap(string fileName)
        {
            var bitmap = LoadDrawingBitmap(fileName);

            // It looks like all bitmap formats are stored as Format32bppArgb
            // The only exception are indexed file formats (Format8bppIndexed) 
            // There we would need to lock bits and then copy the indexes that reference color values from Palette property (here the colors are in ARGB format).
            // So it is best to leave the convertion to Bitmap class and use Format32bppPArgb to also convert to premultiplied alpha.

            //if (!ConvertToBgra && bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
            //{
            //    var grayscaleBitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);

            //    int grayscaleImageBytesLength = grayscaleBitmapData.Stride * grayscaleBitmapData.Height;

            //    //var vkFormat = Format.R8G8B8A8Unorm;
            //    //var vkFormat = Format.B8G8R8A8Unorm;

            //    var grayscaleDataBytes = new byte[grayscaleImageBytesLength];

            //    System.Runtime.InteropServices.Marshal.Copy(grayscaleBitmapData.Scan0, grayscaleDataBytes, 0, grayscaleImageBytesLength);

            //    bitmap.UnlockBits(grayscaleBitmapData);

            //    // We need to convert RGB to BGR
            //    //BitmapUtils.ConvertBgraToRgba(dataBytes);

            //    var grayscaleGpuImageData = new RawImageData(bitmap.Width, bitmap.Height, grayscaleBitmapData.Stride, Format.R8Unorm, grayscaleDataBytes, checkTransparency: false)
            //    {
            //        IsPreMultipliedAlpha = false
            //    };

            //    bitmap.Dispose();

            //    return grayscaleGpuImageData;
            //}

            // NOTE:
            // System.Drawing.Bitmap always creates a 32 bit bitmap (even when reading 8 bit png file the PixelFormat will be Format32bppArgb)
            // When using WPF to read bitmap "new System.Windows.Media.Imaging.BitmapImage(new System.Uri(fileName));" then the bitmap has the original bitmap format (for example Gray8)

            // LockBits and if needed also convert to pre-multiplied alpha (Format32bppPArgb instead of Format32bppArgb)
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);

            int imageBytesLength = bitmapData.Stride * bitmapData.Height;

            // It looks like all bitmap formats (at least Format32bppArgb, Format24bppRgb, Format8bppIndexed) are stored as B8G8R8A8Unorm in raw format where file is read by System.Drawing.Bitmap
            var vkFormat = Format.B8G8R8A8Unorm;
            var dataBytes = new byte[imageBytesLength];

            System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, dataBytes, 0, imageBytesLength);

            bitmap.UnlockBits(bitmapData);

            var gpuImageData = new RawImageData(bitmap.Width, bitmap.Height, bitmapData.Stride, vkFormat, dataBytes, checkTransparency: false)
            {
                IsPreMultipliedAlpha = true // IsPreMultipliedAlpha can be set to true, because we LockBits with Format32bppPArgb and this converts image to pre-multiplied form is it is not already
            };

            // Now that we have the image data array filled, we can update HasTransparency property
            gpuImageData.CheckTransparency();

            bitmap.Dispose();

            return gpuImageData;
        }

        /// <inheritdoc />
        public RawImageData LoadBitmap(Stream fileStream, string fileExtension)
        {
            throw new NotImplementedException();
        }

        public System.Drawing.Bitmap LoadDrawingBitmap(string fileName)
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
                        var imageFromStream = new System.Drawing.Bitmap(fileStream);
                        fileStream.Close();

                        return imageFromStream;
                    }
                }

                if (!isResolved)
                    throw new FileNotFoundException("File not found: " + fileName, fileName); // Throw exception here because System.Drawing.Bitmap throws "Parameter invalid" exception when file does not exist (?!)
            }

            var bitmap = new System.Drawing.Bitmap(fileName);

            return bitmap;
        }

        /// <inheritdoc />
        public void SaveBitmap(RawImageData imageData, string fileName)
        {
            if (imageData == null)
                return;

            byte[] imageDataArray = imageData.Data;

            if (imageData.Format == Format.R8G8B8A8Unorm)
                 ConvertRgbaToBgra(imageDataArray); // converted RGBA to BGRA

            var bitmap = new System.Drawing.Bitmap(imageData.Width, imageData.Height, imageData.Stride,
                                                   System.Drawing.Imaging.PixelFormat.Format32bppArgb,
                                                   Marshal.UnsafeAddrOfPinnedArrayElement(imageDataArray, 0));

            bitmap.Save(fileName);
        }

        /// <inheritdoc />
        public void SaveBitmap(RawImageData imageData, Stream fileStream, string fileExtension)
        {
            throw new NotImplementedException();
        }


        // From BitmapUtils.cs:
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
    }
}


#endif