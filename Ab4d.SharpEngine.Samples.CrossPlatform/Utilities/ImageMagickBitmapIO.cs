#if IMAGE_MAGIC

using System;
using System.IO;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Utilities;
using Ab4d.Vulkan;
using ImageMagick;

namespace Ab4d.SharpEngine.Samples.Utilities
{
    /// <summary>
    /// ImageMagickBitmapIO uses ImageMagick to support bitmap IO operations.
    /// </summary>
    public class ImageMagickBitmapIO : IBitmapIO
    {
        public static readonly string[] SupportedFileExtensions = { "png", "jpg", "bmp", "tiff", "gif" };

        public ErrorMetric CompareErrorMetric = ErrorMetric.Absolute;
        public Percentage CompareColorFull = new Percentage(5);

        /// <inheritdoc />
        public Func<string, string?>? FileNotFoundResolver { get; set; }

        /// <inheritdoc />
        public Func<string, Stream?>? FileStreamResolver { get; set; }

        /// <summary>
        /// When true (by default) then the loaded images are converted into 32 bit BGRA format.
        /// When false then the loader tries to preserve the format of the bitmap (for example 8 bit for grayscale) but this is not guaranteed and the loader may still convert the image to BGRA.
        /// </summary>
        public bool ConvertToBgra { get; set; } = true;


        /// <inheritdoc />
        public bool IsFileFormatImportSupported(string fileExtension)
        {
            return IsFileFormatSupported(fileExtension, SupportedFileExtensions);
        }

        /// <inheritdoc />
        public bool IsFileFormatExportSupported(string fileExtension)
        {
            return IsFileFormatSupported(fileExtension, SupportedFileExtensions);
        }

        /// <inheritdoc />
        public bool IsStreamSupported()
        {
            return false;
        }

        /// <inheritdoc />
        public RawImageData LoadBitmap(string fileName)
        {
            var image = LoadImage(fileName);
            var gpuImageData = CreateRawImageData(image);

            return gpuImageData;
        }

        /// <inheritdoc />
        public RawImageData LoadBitmap(Stream fileStream, string fileExtension)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public void SaveBitmap(RawImageData imageData, string fileName)
        {
            var image = CreateImage(imageData);
            SaveImage(image, fileName);
        }

        /// <inheritdoc />
        public void SaveBitmap(RawImageData imageData, Stream fileStream, string fileExtension)
        {
            throw new System.NotImplementedException();
        }


        public void SaveImage(MagickImage? image, string? fileName)
        {
            if (image == null || fileName == null) 
                return;

            // Use backslash on Windows and slash in other OS
            fileName = FileUtils.FixDirectorySeparator(fileName);

            image.Write(fileName);
        }

        public MagickImage LoadImage(string fileName)
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
                        var imageFromStream = new MagickImage(fileStream);
                        fileStream.Close();

                        return imageFromStream;
                    }
                }

                if (!isResolved)
                    throw new FileNotFoundException("File not found: " + fileName, fileName); // Throw exception here because System.Drawing.Bitmap throws "Parameter invalid" exception when file does not exist (?!)
            }

            var image = new MagickImage(fileName);

            return image;
        }

        public MagickImage CreateImage(RawImageData imageData)
        {
            MagickReadSettings mr = new MagickReadSettings();
            mr.ColorType = ColorType.TrueColor;
            mr.Width = imageData.Width;
            mr.Height = imageData.Height;
            mr.Format = imageData.Format == Format.R8G8B8A8Unorm ? MagickFormat.Rgba : MagickFormat.Bgra;

            var image = new MagickImage(imageData.Data, mr);

            return image;
        }

        public RawImageData CreateRawImageData(MagickImage image)
        {
            var pixels = image.GetPixels();
            var imageBytes = pixels.ToByteArray("BGRA");

            if (imageBytes == null)
                return RawImageData.OneBlackPixelImage;
            
            var rawImageData = new RawImageData(image.Width, image.Height, image.Width * 4, Format.B8G8R8A8Unorm, imageBytes, checkTransparency: false)
            {
                IsPreMultipliedAlpha = true
            };

            if (image.HasAlpha)
                rawImageData.CheckTransparencyAndConvertToPremultipliedAlpha();
            else
                rawImageData.HasTransparentPixels = false;

            return rawImageData;
        }


        public double GetSignificantDiff(int bitmapWidth, int bitmapHeight)
        {
            return 1;
        }

        public double CompareBitmaps(object bitmap1, object bitmap2, string? diffImageFileName = null)
        {
            var magickImage1 = bitmap1 as MagickImage;
            var magickImage2 = bitmap2 as MagickImage;

            return CompareBitmaps(magickImage1, magickImage2, diffImageFileName);
        }

        public double CompareBitmaps(RawImageData imageData1, RawImageData imageData2, string? diffImageFileName = null)
        {
            var magickImage1 = CreateImage(imageData1);
            var magickImage2 = CreateImage(imageData2);
            
            return CompareBitmaps(magickImage1, magickImage2, diffImageFileName);
        }

        public double CompareBitmaps(RawImageData imageData1, string bitmap2FileName, string? diffImageFileName = null)
        {
            var magickImage1 = CreateImage(imageData1);
            var magickImage2 = LoadImage(bitmap2FileName);

            return CompareBitmaps(magickImage1, magickImage2, diffImageFileName);
        }

        public double CompareBitmaps(object bitmap1, string bitmap2FileName, string? diffImageFileName = null)
        {
            var magickImage1 = bitmap1 as MagickImage;
            var magickImage2 = LoadImage(bitmap2FileName);

            return CompareBitmaps(magickImage1, magickImage2, diffImageFileName);
        }

        private double CompareBitmaps(MagickImage? bitmap1, MagickImage? bitmap2, string? diffImageFileName = null)
        {
            if (bitmap1 == null)
                throw new ArgumentException("bitmap1 is null or not MagickImage type");

            if (bitmap2 == null)
                throw new ArgumentException("bitmap2 is null or not MagickImage type");

            bitmap1.ColorFuzz = CompareColorFull;


            double diffValue;
            
            if (diffImageFileName == null)
            {
                diffValue = bitmap1.Compare(bitmap2, CompareErrorMetric);
            }
            else
            {
                using (MagickImage diffImage = new MagickImage())
                {
                    diffValue = bitmap1.Compare(bitmap2, CompareErrorMetric, diffImage);
                    diffImage.Write(diffImageFileName);
                }
            }

            return diffValue;
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
    }
}

#endif