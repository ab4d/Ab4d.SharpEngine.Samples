# Simple ASP.NET Core website with Ab4d.SharpEngine

This project demonstrates how use **ASP.NET Core web server** to start a simple web page that uses 
a `Ab4d.SharpEngine.Samples.WebAssemblyDemo` project that is compiled with `browser-wasm` RuntimeIdentifier.
This project does not require Blazor WebAssembly because it only uses HTML and JavaScript to load and start the WebAssembly.

To see how to use a Node.js web server or a simple Python web server, see the [Ab4d.SharpEngine.Samples.HtmlWebPage project](../Ab4d.SharpEngine.Samples.HtmlWebPage/README.md).

### Prepare required files

Before the web server can be started, the required files must be prepared in the `wwwroot` folder.

If the Ab4d.SharpEngine.Samples.WebAssemblyDemo project was not published before, then 
the `compile_WebAssemblyDemo.bat` can be used to do that.


### Starting web server

The web server is configured and started in the `Program.cs` file.

Before this is done, the code checks if the required files are available in the `wwwroot/_framework` folder
or if files there are older than in the published Ab4d.SharpEngine.Samples.WebAssemblyDemo folder.
In those two case the files are copied to local `wwwrooot/_framework` folder.

The web server is also configured to serve Brotli compressed files (.br) when they are available.
Brotli compressed files are much smaller and therefore the web page loads faster 
(for example, the size of compiled dotnet.native is only 1.9 MB when Brotli compressed instead of 8.8 MB of uncompressed wasm file).

## Create 3D scene with Ab4d.SharpEngine library

The 3D scene is created in the Ab4d.SharpEngine.Samples.WebAssemblyDemo project. This is a .Net project with `TargetFramework` set to `net11.0-browser`, `RuntimeIdentifier` set to `browser-wasm` and `PlatformTarget` to `AnyCPU`. The project does not require any Blazor references. The project is compiled into WebAssembly (wasm) files that can be used on any web page (no need for Blazor).

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

When the web server is started by using ASP.NET Core, a python script or in some other way, it is not possible (at least to my knowledge) to debug the c# code that was used to generate the WebAssembly files.

Therefore, it is recommended to create a **Blazor WebAssembly web page** that uses linked .cs files from the main project (Ab4d.SharpEngine.Samples.WebAssemblyDemo) and can be started as a Blazor WebAssembly web page, allowing for full code debugging.

In this solution, this is done by the **Ab4d.SharpEngine.Samples.BlazorWebAssemblyTesterApp** project.
