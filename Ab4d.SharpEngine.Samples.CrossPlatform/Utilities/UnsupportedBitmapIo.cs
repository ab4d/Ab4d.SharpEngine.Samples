using System;
using System.IO;
using Ab4d.SharpEngine.Common;

namespace Ab4d.SharpEngine.Samples.Utilities
{
    // See code comments in IBitmapIO.cs for some options to provide bitmap operations

    /// <summary>
    /// UnsupportedBitmapIO class does not support any bitmap IO operations.
    /// The IsFileFormatImportSupported, IsFileFormatExportSupported and IsStreamSupported return false.
    /// All other methods throw NotSupportedException exception.
    /// </summary>
    public class UnsupportedBitmapIO : IBitmapIO
    {
        /// <inheritdoc />
        public Func<string, string?>? FileNotFoundResolver { get; set; }
        
        /// <inheritdoc />
        public Func<string, Stream?>? FileStreamResolver { get; set; }

        /// <summary>
        /// When true (by default) then the loaded images are converted into 32 bit BGRA format.
        /// </summary>
        public bool ConvertToBgra { get; set; }


        /// <inheritdoc />
        public bool IsFileFormatImportSupported(string fileExtension)
        {
            return false;
        }

        /// <inheritdoc />
        public bool IsFileFormatExportSupported(string fileExtension)
        {
            return false;
        }

        /// <inheritdoc />
        public bool IsStreamSupported()
        {
            return false;
        }

        /// <inheritdoc />
        public RawImageData LoadBitmap(string fileName)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public RawImageData LoadBitmap(Stream fileStream, string fileExtension)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public void SaveBitmap(RawImageData imageData, string fileName)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public void SaveBitmap(RawImageData imageData, Stream fileStream, string fileExtension)
        {
            throw new NotSupportedException();
        }
    }
}