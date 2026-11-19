using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);


string rootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");


//
// Ensure that published files from Ab4d.SharpEngine.Samples.WebAssemblyDemo are available.
// If not, copy them from Ab4d.SharpEngine.Samples.WebAssemblyDemo project
//

var currentFrameworkFolder = Path.Combine(rootPath, @"_framework\");

var webAssemblyDemoFolder = Path.Combine(builder.Environment.ContentRootPath, @"..\Ab4d.SharpEngine.Samples.WebAssemblyDemo\");
var webAssemblyDemoOutputFolder = webAssemblyDemoFolder + $@"bin\Release\net{Environment.Version.Major}.0-browser\publish\wwwroot\_framework\";

var currentFileName = currentFrameworkFolder + "dotnet.js";
var latestFileName = webAssemblyDemoOutputFolder + "dotnet.js";

var currentFileInfo = new FileInfo(currentFileName!);
var latestFileInfo = new FileInfo(latestFileName!);


bool copyFiles = !currentFileInfo.Exists && latestFileInfo.Exists;

if (!copyFiles && currentFileInfo.Exists && latestFileInfo.Exists)
{
    // Check if latest file from Ab4d.SharpEngine.Samples.WebAssemblyDemo is newer than the current file in local wwwroot
    copyFiles = currentFileInfo.LastWriteTime < latestFileInfo.LastWriteTime;
}

if (copyFiles)
{
    var allFiles = Directory.GetFiles(webAssemblyDemoOutputFolder, "*.*", SearchOption.AllDirectories);
    foreach (var sourceFilePath in allFiles)
    {
        var fileName = sourceFilePath.Substring(webAssemblyDemoOutputFolder.Length);
        var destinationFileName = Path.Combine(currentFrameworkFolder, fileName);

        var directoryName = Path.GetDirectoryName(destinationFileName)!;
        if (!Directory.Exists(directoryName))
            Directory.CreateDirectory(directoryName);

        File.Copy(sourceFilePath, destinationFileName, overwrite: true);
    }

    Console.WriteLine($"Copied {allFiles.Length} files to wwwroot/_framework/ folder from {webAssemblyDemoOutputFolder}");
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




if (!copyFiles && !currentFileInfo.Exists && !latestFileInfo.Exists)
{
    app.MapGet("/{**path}", async context =>
    {
        context.Response.ContentType = "text/html";

        await context.Response.WriteAsync("""
                                          <!DOCTYPE html>
                                          <html>
                                          <body>
                                              <p>Published files from Ab4d.SharpEngine.Samples.WebAssemblyDemo are not available.</p>
                                              <p>Start <code>compile_WebAssemblyDemo.bat</code> to compile the project and then start the web server again.</p>
                                          </body>
                                          </html>
                                          """);
    });
}
else
{
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
}

app.Run();