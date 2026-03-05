using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Utilities;
using System.Runtime.Versioning;

namespace Ab4d.SharpEngine.WebGL;

/// <summary>
/// BrowserBitmapIO uses browser's bitmap load and to support cross platform bitmap IO operations that are defined in IBitmapIO interface.
/// </summary>
[SupportedOSPlatform("browser")]
public class BrowserBitmapIO : IBitmapIO
{
    private static readonly string[] SupportedLoadFileExtensions = { "png", "jpg", "bmp", "gif" };

    private CanvasInterop _canvasInterop;

    /// <inheritdoc/>
    public bool ConvertToSupportedFormat { get; set; } = true;

    /// <inheritdoc />
    public Func<string, string?>? FileNotFoundResolver { get; set; }

    /// <inheritdoc />
    public Func<string, Stream?>? FileStreamResolver { get; set; }


    public BrowserBitmapIO(CanvasInterop canvasInterop)
    {
        _canvasInterop = canvasInterop;
    }


    /// <inheritdoc />
    public bool IsFileFormatImportSupported(string fileExtension)
    {
        return IsFileFormatSupported(fileExtension, SupportedLoadFileExtensions);
    }

    /// <inheritdoc />
    public bool IsFileFormatExportSupported(string fileExtension)
    {
        return false; // currently we do not support saving to file in the browser
    }

    /// <inheritdoc />
    public bool IsStreamSupported() => false;

    /// <inheritdoc />
    public RawImageData LoadBitmap(string fileName, BitmapLoadOptions? options = null)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public RawImageData LoadBitmap(Stream fileStream, string fileExtension, BitmapLoadOptions? options = null)
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

    private static string FixDirectorySeparator(string filePath)
    {
        if (Path.DirectorySeparatorChar == '\\')
            return filePath.Replace('/', '\\');

        return filePath.Replace('\\', '/');
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