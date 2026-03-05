# Simple HTML and JavaScript web page with Ab4d.SharpEngine

This project demonstrates how to create a simple web page that uses only HTML and JavaScript 
to start the WebAssembly that is compiled from the `Ab4d.SharpEngine.Samples.WebAssemblyDemo` project.

This project is similar to `Ab4d.SharpEngine.Samples.AspNetCoreApp` project,
but instead of using Asp.Net Core to as a web server, this project uses a **simple Python web server** (defined in `server.py`)
or a **more advanced Express node.js web server** (defined in `server.js`).


### Prepare required files

Before the web server can be started, the required files must be prepared in the `wwwroot` folder.

The wwwroot folder must contain the **HTML and JavaScript files** that load and start the WebAssembly.
Those files are copied from the wwwroot folder of the Ab4d.SharpEngine.Samples.AspNetCoreApp project.

Then, the wwwroot folder must also contain the _framework folder with **WebAssembly files** that are compiled from the Ab4d.SharpEngine.Samples.WebAssemblyDemo project.

Compiling and copying the required files can be done by executing one of the following scripts:
- `compile_debug_version.bat` that compiles the Ab4d.SharpEngine.Samples.WebAssemblyDemo project in debug mode and copies the required files to the wwwroot and wwwroot\_framework folders.
- `compile_publish_version.bat` that compiles the Ab4d.SharpEngine.Samples.WebAssemblyDemo project in release mode, compresses the .js and .wasm files into Brotli compressed files (requires ThirdParty brotli tool) and copies the required files to the wwwroot and wwwroot\_framework folders.


### Starting web server

This project provides two options to start a local web server that serves the web page from wwwroot:

1. **Simple Python web server** - this option requires that Python is installed.
   This server is started by using server.py script.
   This is a very simple web server that does not support HTTP/2 and can be quite slow, especially when serving many files (for example, for debug build).
   This server also does not support Brotli compressed files.

2. **Using Express Node.js web server** - this option requires that Node.js and the express npm package are installed.
   Express is much faster than the simple Python web server. What is more, the specified script also supports serving Brotli compressed files if they are available
   (when `compile_publish_version.bat` is called and Brotil compression utility is available).
   Brotli compressed files are much smaller and therefore the web page loads faster
   (for example, serving only 2.2 MB when Brotli compressed instead of 9.3 MB of uncompressed data).

To see how to use **Asp.Net Core** project to serve as a web server, see the [Ab4d.SharpEngine.Samples.AspNetCoreApp project](../Ab4d.SharpEngine.Samples.AspNetCoreApp/README.md).

The following sections are the same as for the Asp.Net Core project.


## Create 3D scene with Ab4d.SharpEngine library

The 3D scene is created in the Ab4d.SharpEngine.Samples.WebAssemblyDemo project. This is a .Net project with `TargetFramework` set to `net9.0-browser` and `RuntimeIdentifier` set to `browser-wasm`. The project does not require any Blazor references. The project is compiled into WebAssembly (wasm) files that can be used on any web page (no need for Blazor).

To show 3D graphics, the Ab4d.SharpEngine.Samples.WebAssemblyDemo project references the Ab4d.SharpEngine.Web library. The WebAssemblyDemo project defines the 3D scene by adding SceneNodes objects to the Scene.RootNode. This is done in the `SharpEngineTest.cs` file.

To communicate with the web page, this project exports a few methods to JavaScript. The exported methods are defined in the `JavaScriptInterop.cs` file. For example, `ToggleCameraRotationJSExport` method that is defined in `JavaScriptInterop.cs` is called from the following html:
```
<a href="javascript:toggleCameraRotation();">Toggle camera rotation</a><br />
```

The WebAssemblyDemo project also subscribes to mouse and resize events on the canvas element. This is done by using the standard `CanvasInterop.cs` and `sharp-engine.js` files (those two files are also used in the Ab4d.SharpEngine.Samples.BlazorWebAssembly project).


## Startup

In the standard Ab4d.SharpEngine.Samples.BlazorWebAssembly project,  Blazor handles the startup procedure. There, we only need to override the `OnAfterRender` or `OnAfterRenderAsync` method to start initialization of the SharpEngine.

When Blazor is not used, then we need to initialize the SharpEngine from JavaScript. This is done in the `webassembly-demo.js` file that is loaded from `index.html`.

There, the code first initializes the .NET WebAssembly runtime by loading the `./_framework/dotnet.js`. Then the exported functions from .Net are retrieved and after that we can call the `InitSharpEngineJSExport` method that initializes the SharpEngine and generates the initial 3D scene.


## Debugging

When the web server is started by using Asp.Net Core, a python script or in some other way, it is not possible (at least to my knowledge) to debug the c# code that was used to generate the WebAssembly files.

Therefore, it is recommended to create a **Blazor WebAssembly web page** that uses linked .cs files from the main project (Ab4d.SharpEngine.Samples.WebAssemblyDemo) and can be started as a Blazor WebAssembly web page, allowing for full code debugging.

In this solution, this is done by the **Ab4d.SharpEngine.Samples.BlazorWebAssemblyTesterApp** project.
