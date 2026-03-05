using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);


string rootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");


//
// Ensure that WebAssembly files are available.
// If not, copy them from Ab4d.SharpEngine.Samples.HtmlWebPage or Ab4d.SharpEngine.Samples.WebAssemblyDemo projects.
//

if (!System.IO.File.Exists(rootPath + "/_framework/dotnet.native.wasm"))
{
    // First try to copy the wwwroot from Ab4d.SharpEngine.Samples.HtmlWebPage to this project's wwwroot
    // There the files are created by starting compile_debug_version.bat or compile_publish_version.bat scripts.
    var sourceFolder = Path.Combine(builder.Environment.ContentRootPath, $"../Ab4d.SharpEngine.Samples.HtmlWebPage/wwwroot/_framework/");
    if (!System.IO.Directory.Exists(sourceFolder))
    {
#if DEBUG
        string build = "Debug";
#else
        string build = "Release";
#endif

        sourceFolder = Path.Combine(builder.Environment.ContentRootPath, $"..\\Ab4d.SharpEngine.Samples.WebAssemblyDemo\\bin\\{build}\\net9.0-browser\\browser-wasm\\AppBundle\\");
        if (!Directory.Exists(sourceFolder))
        {
            sourceFolder = null;
        }
    }

    if (sourceFolder != null)
    {
        var destinationFilePath = Path.Combine(rootPath, "_framework");

        var allFiles = Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories);
        foreach (var sourceFilePath in allFiles)
        {
            var fileName = sourceFilePath.Substring(sourceFolder.Length);
            var destinationFileName = Path.Combine(destinationFilePath, fileName);

            var directoryName = Path.GetDirectoryName(destinationFileName)!;
            if (!Directory.Exists(directoryName))
                Directory.CreateDirectory(directoryName);

            File.Copy(sourceFilePath, destinationFileName, overwrite: true);
        }

        Console.WriteLine($"Copied {allFiles.Length} files to wwwroot/_framework/ folder from {sourceFolder}");
    }
    else
    {
        throw new Exception("No files found for the wwwroot/_framework folder.");
    }
}


//
// Create web application
//

var app = builder.Build();

// The following code serves .wasm.br and .js.br files instead of .wasm and .js files when a .br file exists and when they are accepted by the client.
app.Use(async (context, next) =>
{
    var filePath = context.Request.Path.Value;

    // Check if the request is for a .wasm file and client accepts Brotli
    if (filePath != null && (filePath.EndsWith(".wasm", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith(".js", StringComparison.OrdinalIgnoreCase)) &&
        context.Request.Headers["Accept-Encoding"].ToString().Contains("br"))
    {
        var brotliFilePath = filePath + ".br";
        var fileProvider = new PhysicalFileProvider(rootPath);
        var fileInfo = fileProvider.GetFileInfo(brotliFilePath);

        if (fileInfo.Exists)
        {
            context.Response.Headers["Accept-ranges"] = "bytes";
            context.Response.Headers["Content-Encoding"] = "br";
            context.Response.ContentType = filePath.EndsWith(".wasm", StringComparison.OrdinalIgnoreCase) ? "application/wasm" : "application/javascript";
            await context.Response.SendFileAsync(fileInfo);
            return;
        }
    }

    await next();
});



app.UseDefaultFiles(); // Serve Index.html by default


var staticFileOptions = new StaticFileOptions()
{
    FileProvider = new PhysicalFileProvider(rootPath),
};

// Add .pdb MIME type (if this is not added then pdb files are not served and this produces an error when checking integrity)
// NOTE: This still does not provide debugging support (to be able to put a breakpoint into the c# code)
var contentTypeProvider = new FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".pdb"] = "application/octet-stream";
staticFileOptions.ContentTypeProvider = contentTypeProvider;

app.UseStaticFiles(staticFileOptions);

app.Run();