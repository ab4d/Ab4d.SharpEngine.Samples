//#define TESTS

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Ab4d.SharpEngine.Common;
using Android.Content.Res;
using Android.Graphics;
using FileUtils = Ab4d.SharpEngine.Utilities.FileUtils;
using Format = Ab4d.Vulkan.Format;

namespace Ab4d.SharpEngine.Samples.Utilities
{
    /// <summary>
    /// AndroidBitmapIO uses Android to support bitmap IO operations.
    /// </summary>
    public class AndroidBitmapIO : IBitmapIO
    {
        // https://developer.android.com/guide/topics/media/media-formats#core
        private static readonly string[] SupportedLoadFileExtensions = { "png", "jpg", "gif", "bmp", "webp", "heic", "heif" };

        /// <inheritdoc/>
        public bool ConvertToSupportedFormat { get; set; } = true;

        /// <summary>
        /// When true (by default), then the inScaled option in the call to BitmapFactory.DecodeResource is set to false.
        /// This prevents Android from scaling the bitmap based on screen pixel density.
        /// </summary>
        public bool PreventAndroidBitmapScale { get; set; } = true;


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
            return false;
        }

        /// <inheritdoc />
        public bool IsStreamSupported()
        {
            return true;
        }

        /// <inheritdoc />
        public RawImageData LoadBitmap(string fileName)
        {
            var androidBitmap = LoadAndroidBitmap(fileName);

            if (androidBitmap == null)
                return RawImageData.Empty;

            return ConvertAndroidBitmapToRawImageData(androidBitmap);
        }

        /// <inheritdoc />
        public RawImageData LoadBitmap(Stream fileStream, string fileExtension)
        {
            var options = new BitmapFactory.Options();
            if (PreventAndroidBitmapScale)
                options.InScaled = false; // see: https://developer.android.com/reference/android/graphics/BitmapFactory.Options#inScaled

            var androidBitmap = BitmapFactory.DecodeStream(fileStream, null, options);
            return ConvertAndroidBitmapToRawImageData(androidBitmap);
        }

        public RawImageData LoadBitmap(Resources resources, int drawableId, BitmapFactory.Options? options = null)
        {
            if (options == null)
            {
                options = new BitmapFactory.Options();
                //{
                //    InPremultiplied = true, // this is also a default (see https://developer.android.com/reference/android/graphics/BitmapFactory.Options)
                //    InPreferredColorSpace = ColorSpace.Get(ColorSpace.Named.LinearSrgb!)
                //};

                if (PreventAndroidBitmapScale)
                    options.InScaled = false; // see: https://developer.android.com/reference/android/graphics/BitmapFactory.Options#inScaled
            }

            var androidBitmap = BitmapFactory.DecodeResource(resources, drawableId, options);
            return ConvertAndroidBitmapToRawImageData(androidBitmap);
        }

        public Bitmap? LoadAndroidBitmap(Resources resources, int drawableId, BitmapFactory.Options? options = null)
        {
            if (options == null)
            {
                options = new BitmapFactory.Options();
                //{
                //    InPremultiplied = true, // this is also a default (see https://developer.android.com/reference/android/graphics/BitmapFactory.Options)
                //    InPreferredColorSpace = ColorSpace.Get(ColorSpace.Named.LinearSrgb!)
                //};

                if (PreventAndroidBitmapScale)
                    options.InScaled = false; // see: https://developer.android.com/reference/android/graphics/BitmapFactory.Options#inScaled
            }

            var androidBitmap = BitmapFactory.DecodeResource(resources, drawableId, options);
            return androidBitmap;
        }

        public Bitmap? LoadAndroidBitmap(string fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            // Use backslash on Windows and slash in other OS
            fileName = FileUtils.FixDirectorySeparator(fileName);

            var decodeFileOptions = new BitmapFactory.Options();
            if (PreventAndroidBitmapScale)
                decodeFileOptions.InScaled = false; // see: https://developer.android.com/reference/android/graphics/BitmapFactory.Options#inScaled

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
                        var bitmapFromStream = BitmapFactory.DecodeStream(fileStream, null, decodeFileOptions);
                        fileStream.Close();

                        return bitmapFromStream;
                    }
                }

                if (!isResolved)
                    throw new FileNotFoundException("File not found: " + fileName, fileName); // Throw exception here because System.Drawing.Bitmap throws "Parameter invalid" exception when file does not exist (?!)
            }

            var skBitmap = BitmapFactory.DecodeFile(fileName, decodeFileOptions);

            return skBitmap;
        }

        public RawImageData ConvertAndroidBitmapToRawImageData(Bitmap? androidBitmap)
        {
            if (androidBitmap == null || androidBitmap.Width == 0 || androidBitmap.Height == 0)
                return RawImageData.Empty;

            int intArrayLength = androidBitmap.Width * androidBitmap.Height;
            var bitmapInts = new int[intArrayLength];
            
            androidBitmap.GetPixels(bitmapInts, 0, androidBitmap.Width, 0, 0, androidBitmap.Width, androidBitmap.Height);

            // UH UH:
            // We already have the content of the bitmap in int array, but to create RawImageData, we need to have byte array.
            // There does not seem to be a way to convert that without copying.
            //
            // Using Unsafe.As crashes on Android:
            //var imageBytes = new byte[androidBitmap.Width * 4 * androidBitmap.Height];
            //var imageInts = Unsafe.As<byte[], int[]>(ref imageBytes);
            //
            // MemoryMarshal.Cast creates a Span<byte> that also cannot be used to create RawImageData (this will be improved in the future)
            //var imageInts = MemoryMarshal.Cast<byte, int>(imageBytes);

            int byteArrayLength = intArrayLength * 4;
            var imageBytes = new byte[byteArrayLength];

            System.Buffer.BlockCopy(bitmapInts, 0, imageBytes, 0, byteArrayLength);

            var gpuImageData = new RawImageData(androidBitmap.Width, androidBitmap.Height, androidBitmap.Width * 4, Format.B8G8R8A8Unorm, imageBytes, checkTransparency: androidBitmap.HasAlpha)
            {
                IsPreMultipliedAlpha = androidBitmap.IsPremultiplied
            };

            if (!androidBitmap.HasAlpha)
            {
                gpuImageData.HasTransparentPixels = false;
                gpuImageData.IsPreMultipliedAlpha = true; // If no alpha is used, then set IsPreMultipliedAlpha to true (all colors are multiplied by 1)
            }

            return gpuImageData;
        }

        public void SaveBitmap(RawImageData imageData, string fileName)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void SaveBitmap(RawImageData imageData, Stream fileStream, string fileExtension)
        {
            throw new NotImplementedException();
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

#if TESTS
        //
        // The following test FAILS!
        // It seems that Android is loaded the images with slightly different colors, but when using the test images for actual textures, the results seems correct
        // (I also tried to force some other color space, but this did not fixed the problems)
        //
        // The following files must be added to Resources/Drawable folder (from SharpEngine.Tests\Ab4d.SharpEngine.Tests.UnitTests\ReferencedImages\EngineUnitTests\BitmapIOTest):
        //blue.png
        //grayscale.png
        //grayscale_8_bit.png
        //green.png
        //list.txt
        //red.png
        //rgba.png
        //rgba_8_bit.png
        //rgb_no_alpha_jpg.jpg
        //rgb_no_alpha_png.png
        //rgb_no_alpha_8_bit.png
        //transparent_gradient.png
        //
        // !!! rename:
        // rgb_no_alpha.jpg => rgb_no_alpha_jpg.jpg
        // rgb_no_alpha.png => rgb_no_alpha_png.png

        public static void Test(Resources resources)
        {
            var androidBitmapIo = new AndroidBitmapIO();
            CheckLoadingSingleColorBitmap(resources, androidBitmapIo);
            CheckLoadingDifferentBitmap(resources, androidBitmapIo);
        }

        private static void CheckLoadingSingleColorBitmap(Resources resources, AndroidBitmapIO bitmapIO)
        {
            var rawImageData11 = bitmapIO.LoadBitmap(resources, Ab4d.SharpEngine.Samples.Android.Application.Resource.Drawable.red);
            var rawImageData21 = bitmapIO.LoadBitmap(resources, Ab4d.SharpEngine.Samples.Android.Application.Resource.Drawable.green);
            var rawImageData31 = bitmapIO.LoadBitmap(resources, Ab4d.SharpEngine.Samples.Android.Application.Resource.Drawable.blue);

            // by default the image format should be BGRA
            Assert.AreEqual((int)Format.B8G8R8A8Unorm, (int)rawImageData11.Format);
            Assert.AreEqual((int)Format.B8G8R8A8Unorm, (int)rawImageData21.Format);
            Assert.AreEqual((int)Format.B8G8R8A8Unorm, (int)rawImageData31.Format);

            // is red
            Assert.AreEqual((byte)0, rawImageData11.Data[0]);
            Assert.AreEqual((byte)0, rawImageData11.Data[1]);
            Assert.AreEqual((byte)255, rawImageData11.Data[2]);
            Assert.AreEqual((byte)255, rawImageData11.Data[3]);

            // is green
            Assert.AreEqual((byte)0, rawImageData21.Data[0]);
            Assert.AreEqual((byte)255, rawImageData21.Data[1]);
            Assert.AreEqual((byte)0, rawImageData21.Data[2]);
            Assert.AreEqual((byte)255, rawImageData21.Data[3]);

            // is blue
            Assert.AreEqual((byte)255, rawImageData31.Data[0]);
            Assert.AreEqual((byte)0, rawImageData31.Data[1]);
            Assert.AreEqual((byte)0, rawImageData31.Data[2]);
            Assert.AreEqual((byte)255, rawImageData31.Data[3]);
        }

        private static void CheckLoadingDifferentBitmap(Resources resources, AndroidBitmapIO bitmapIO)
        {
            bitmapIO.ConvertToBgra = true;

            var rawImageData = bitmapIO.LoadBitmap(resources, Ab4d.SharpEngine.Samples.Android.Application.Resource.Drawable.grayscale);
            CheckRawImageColors(rawImageData, hasTransparency: false, topLeftColor: 0xf0f0f0ff, centerColor: 0x7b7b7bff, bottomRightColor: 0x020202ff);


            // common colors (color at the middle can be different for each image)            
            uint topLeftColor_no_alpha = 0xffffffff;
            uint bottomRightColor_no_alpha = 0x000202ff;

            uint topLeftColor_alpha = 0x0;
            uint bottomRightColor_alpha = bottomRightColor_no_alpha;


            rawImageData = bitmapIO.LoadBitmap(resources, Ab4d.SharpEngine.Samples.Android.Application.Resource.Drawable.rgb_no_alpha_jpg);
            CheckRawImageColors(rawImageData, hasTransparency: false, topLeftColor: topLeftColor_no_alpha, centerColor: 0x92c8c8ff, bottomRightColor: bottomRightColor_no_alpha);

            rawImageData = bitmapIO.LoadBitmap(resources, Ab4d.SharpEngine.Samples.Android.Application.Resource.Drawable.rgb_no_alpha_png);
            CheckRawImageColors(rawImageData, hasTransparency: false, topLeftColor: topLeftColor_no_alpha, centerColor: 0x92c8c7ff, bottomRightColor: bottomRightColor_no_alpha);

            rawImageData = bitmapIO.LoadBitmap(resources, Ab4d.SharpEngine.Samples.Android.Application.Resource.Drawable.rgb_no_alpha_8_bit);
            CheckRawImageColors(rawImageData, hasTransparency: false, topLeftColor: topLeftColor_no_alpha, centerColor: 0x8dc5c8ff, bottomRightColor: 0x000a09ff);

            rawImageData = bitmapIO.LoadBitmap(resources, Ab4d.SharpEngine.Samples.Android.Application.Resource.Drawable.rgba);
            CheckRawImageColors(rawImageData, hasTransparency: true, topLeftColor: topLeftColor_alpha, centerColor: 0x0036366d, bottomRightColor: bottomRightColor_alpha);

            rawImageData = bitmapIO.LoadBitmap(resources, Ab4d.SharpEngine.Samples.Android.Application.Resource.Drawable.rgba_8_bit);
            CheckRawImageColors(rawImageData, hasTransparency: true, topLeftColor: topLeftColor_alpha, centerColor: 0x00342d68, bottomRightColor: 0x00090aff);

            // Check that color (white) is correctly premultipled by alpha
            rawImageData = bitmapIO.LoadBitmap(resources, Ab4d.SharpEngine.Samples.Android.Application.Resource.Drawable.transparent_gradient);
            CheckRawImageColors(rawImageData, hasTransparency: true, topLeftColor: 0x0, centerColor: 0x6d6d6d6d, bottomRightColor: 0xffffffff);
        }

        private static void CheckRawImageColors(RawImageData imageData, bool hasTransparency, uint topLeftColor, uint centerColor, uint bottomRightColor)
        {
            Assert.IsNotNull(imageData.HasTransparentPixels, "HasTransparency is null"); // HasTransparency value must be set
            Assert.AreEqual(hasTransparency, imageData.HasTransparentPixels ?? false);

            Assert.IsNotNull(imageData.IsPreMultipliedAlpha, "IsPreMultipliedAlpha is null");      // IsPreMultipliedAlpha value must be set
            Assert.AreEqual(true, imageData.IsPreMultipliedAlpha ?? false); // IsPreMultipliedAlpha should be always true

            var color = imageData.GetColor(0, 0);
            Assert.AreEqual(color, topLeftColor, "Color at top left");

            color = imageData.GetColor(imageData.Width / 2, imageData.Height / 2);
            Assert.AreEqual(color, centerColor, "Color at center");

            color = imageData.GetColor(imageData.Width - 1, imageData.Height - 1);
            Assert.AreEqual(color, bottomRightColor, "Color at bottom right");
        }

        private static class Assert
        {
            public static void AreEqual(byte a, byte b, string? message = null)
            {
                System.Diagnostics.Debug.Assert(a == b, message);
            }
            
            public static void AreEqual(int a, int b, string? message = null)
            {
                System.Diagnostics.Debug.Assert(a == b, message);
            }
            
            public static void AreEqual(uint a, uint b, string? message = null)
            {
                System.Diagnostics.Debug.Assert(a == b, message);
            }
            
            public static void AreEqual(bool a, bool b, string? message = null)
            {
                System.Diagnostics.Debug.Assert(a == b, message);
            }
            
            public static void IsNotNull(object? test, string? message = null)
            {
                System.Diagnostics.Debug.Assert(test != null, message);
            }
        }
#endif
    }
}
