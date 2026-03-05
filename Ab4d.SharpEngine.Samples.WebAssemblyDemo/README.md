# Ab4d.SharpEngine WebAssembly demo for non-Blazor websites

This sample shows how to create a .Net WebAssembly project with Ab4d.SharpEngine library that can be shown on a website that does not use Blazor WebAssembly client rendering.

When using Ab4d.SharpEngine for the browser the development experience is best when the library is used in a Blazor WebAssembly project.
This is demonstrated by the [Ab4d.SharpEngine.Samples.BlazorWebAssembly project](../Ab4d.SharpEngine.Samples.BlazorWebAssembly/README.md).

But to add 3D graphics to an existing website that does not use Blazor WebAssembly, then a .Net WebAssembly project needs to be created (`TargetFramework` is set to `net9.0-browser` and `RuntimeIdentifier` is set to `browser-wasm`).
When such project is compiled, the WebAssembly (wasm) files are generated and they can be loaded in an existing website.

For example, this solution shows how to show this WebAssembly project in:
- [Asp.Net Core website](../Ab4d.SharpEngine.Samples.AspNetCoreApp/README.md)
- [Simple Html web page](../Ab4d.SharpEngine.Samples.HtmlWebPage/README.md)

Because it is not possible to debug the code from this project when it is started in non-Blazor project, 
there is also [Ab4d.SharpEngine.Samples.BlazorWebAssemblyTesterApp project](../Ab4d.SharpEngine.Samples.BlazorWebAssemblyTesterApp/README.md) that uses linked
`SharpEngineTest.cs` file from this project and can be used to debug the code in this class.


### Quick start guide

To start this samples project, open `Ab4d.SharpEngine.Samples.NoBlazorBrowserDemo` solution in any .Net IDE. 
Then you can set `Ab4d.SharpEngine.Samples.AspNetCoreApp` as startup project and start it.

You can also start the Ab4d.SharpEngine.Samples.AspNetCoreApp from CLI by executing `dotnet run .` or similar command in the Ab4d.SharpEngine.Samples.AspNetCoreApp folder.

To run the "Simple Html web page" sample, first generate the wasm files by starting the `compile_debug_version.bat` and `compile_publish_version.bat` batch scripts
and the start the web server by starting `start_debug_local_web_server.bat` and `start_publish_local_web_server.bat` scripts.


### Usage in your own project

To use Ab4d.SharpEngine.Web library in your own Blazor WebAssembly project, follow the guides on [Ab4d.SharpEngine.Samples.BlazorWebAssembly project](../Ab4d.SharpEngine.Samples.BlazorWebAssembly/README.md).

To use the Ab4d.SharpEngine.Web library in your own non-Blazor WebAssembly project, follow these steps:
- Create a new "Console application" project (use .Net 9 or newer). Note: if a class library is created, then wasm files are not generated.
- Add reference to Ab4d.SharpEngine.Web NuGet package.
- Copy the following files from this samples project to your project:
    - `CanvasInterop.cs` (copy to the root folder of your project)
    - `Native/libEGL.c` (create a new Native folder in your project and copy the libEGL.c file there)
- Open the csproj file of your project and add or update the existing propertes to the following:
    - In the first `PropertyGroup`:
      ```
      <TargetFramework>net9.0-browser</TargetFramework>
      <RuntimeIdentifier>browser-wasm</RuntimeIdentifier>
      <OutputType>Exe</OutputType>

      <!-- unsafe code is required to use JSExport in CanvasInterop -->
      <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
     
      <!-- The following emscripten flags are required for Ab4d.SharpEngine to use WebGL from the browser -->
      <EmccFlags>-lGL -s FULL_ES3=1 -sMAX_WEBGL_VERSION=2</EmccFlags>

      <!-- Blazor WebAssembly supports SIMD instructions (when supported by the browser), so it is recommended to enable that -->
      <WasmEnableSIMD>true</WasmEnableSIMD>
      ```
    - Optionally, you can set additional properties for the DEBUG and RELEASE builds. See the csproj file in this sample for example PropertyGroup blocks.
    - Add the following two ItemGroups to the csproj file:
      ```
      <ItemGroup>
        <!--The following NativeFileReference is required for Ab4d.SharpEngine.Web to use WebGL from the browser -->
        <NativeFileReference Include="Native/libEGL.c" ScanForPInvokes="true" />
      </ItemGroup>   
      ```
- Open Program.cs and replace all the code with a simple "return;" (see `Program.cs` file in this project).
- Create a new .Net class file and add methods that generate the 3D scene by using Ab4d.SharpEngine objects (see `SharpEngineTest.cs` file in this project).
- Add public methods that can be called from javascript on the website.
- Create a new .Net class file that will be used as an interop class for communinucation between JavaScript and .Net (see `SharpEngineTest.cs` file in this project).


### See also

- [Troubleshooting](../README.md?tab=readme-ov-file#troubleshooting)
- [Ab4d.SharpEngine samples for desktop and mobile device (demonstrate all features of the engine)](https://github.com/ab4d/Ab4d.SharpEngine.Samples)
- [Online help (for desktop and mobile version of the library)](https://www.ab4d.com/help/SharpEngine/html/R_Project_Ab4d_SharpEngine.htm)
