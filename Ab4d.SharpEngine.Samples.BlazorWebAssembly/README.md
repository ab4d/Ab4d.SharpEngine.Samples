# Ab4d.SharpEngine Blazor WebAssembly sample

This sample shows how to create a Blazor WebAssembly project with Ab4d.SharpEngine library.

Using Blazor WebAssembly project is the recommended way to use Ab4d.SharpEngine library because this provides
the best integration of the library and is the only way to debug the .Net code that uses the library.

But if you want to use Ab4d.SharpEngine in a website that does not use Blazor WebAssembly, then 
a .Net WebAssembly project needs to be created (`TargetFramework` is set to `net10.0-browser` and `RuntimeIdentifier` is set to `browser-wasm`).
Open the `Ab4d.SharpEngine.Samples.NoBlazorBrowserDemo.sln` solution to see a demonstration of that. Also read the following and related 
[readme file](../Ab4d.SharpEngine.Samples.WebAssemblyDemo/README.md).

### Quick start guide

To start this project, open `Ab4d.SharpEngine.Samples.BlazorWebAssembly` solution or project in any .Net IDE and start it. 

You can also start it from CLI by executing `dotnet run .` or similar command in the `Ab4d.SharpEngine.Samples.BlazorWebAssembly` folder.

Check a [live version of this sample in your browser](https://www.ab4d.com/sharp-engine-browser-demo).

### Usage in your own project

To use the Ab4d.SharpEngine.Web library in your own project Blazor WebAssembly project, follow these steps:
- Create a new "Blazor WebAssembly Standalone App" project (use .Net 10 or newer).
- Add reference to Ab4d.SharpEngine.Web NuGet package.
- Copy the following files from this samples project to your project:
    - `CanvasInterop.cs` (copy to the root folder of your project)
    - `SharpEngineSceneView.razor` (copy to the root folder of your project)
    - `wwwroot/sharp-engine.js` (copy to the wwwroot folder)
    - `Native/libEGL.c` (create a new Native folder in your project and copy the libEGL.c file there)
- Open the csproj file of your project and add the following:
    - Into the first `PropertyGroup`:
      ```
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
      
      <ItemGroup>
        <!--The following JavaScript file is required for CanvasInterop class to be able to communicate with the browser -->
        <WasmExtraFilesToDeploy Include="sharp-engine.js" />
      </ItemGroup>      
      ```
- Open the razor page that will host the 3D scene (for example Home.razor) and add the following:
    - Add using declarations to the start of the Razor file:
      ```
      @using System.Numerics
      @using Ab4d.SharpEngine.Cameras
      @using Ab4d.SharpEngine.Common
      @using Ab4d.SharpEngine.Materials
      @using Ab4d.SharpEngine.SceneNodes
      @using Ab4d.SharpEngine.Browser
      ```
    - Add SharpEngineSceneView to your razor file. For example, add the following to Home.razor (before the "@code"):
      ```
      <SharpEngineSceneView @ref="sharpEngineSceneView" style="width: 50%; height: 400px; margin-top: 10pt; border: solid black 1px"></SharpEngineSceneView>
      ```

    - Override the OnAfterRender method and create the 3D scene there. For example:
      ```
      @code {
          private SharpEngineSceneView sharpEngineSceneView = null!;
      
          /// <inheritdoc />
          protected override void OnAfterRender(bool firstRender)
          {
              if (firstRender)
                  CreateScene3D();
          }
      
          private void CreateScene3D()
          {
              var scene = sharpEngineSceneView.Scene;
              var sceneView = sharpEngineSceneView.SceneView;
      
              var boxModelNode = new BoxModelNode(centerPosition: new Vector3(0, 0, 0), size: new Vector3(100, 40, 80), material: StandardMaterials.Green);
              scene.RootNode.Add(boxModelNode);
      
              sceneView.BackgroundColor = Colors.SkyBlue;
      
              sceneView.Camera = new TargetPositionCamera()
              {
                  Heading = 30,
                  Attitude = -20,
                  Distance = 300
              };
      
              var pointerCameraController = new PointerCameraController(sceneView)
              {
                  RotateCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed,
                  MoveCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.ControlKey,
                  ZoomMode = CameraZoomMode.PointerPosition,
                  RotateAroundPointerPosition = true,
                  IsPinchZoomEnabled = true, // zoom with touch pinch gesture
                  IsPinchMoveEnabled = true  // move camera with two fingers
              };
          }
      }
      ```
    - Instead of using the `SharpEngineSceneView` component, you can also create the canvas DOM element and
      then manually connect to the canvas. After that you can create the WebGLDevice, Scene and SceneView objects.
      See the "ManualInitialization.razor" file on how to do that.

    
### Deployment

The samples are currently configured to run in the root folder ("http://localhost:5164/").

To deplay them into a subfolder, for example https://www.ab4d.com/sharp-engine-browser-demo/, you need to change the base path. 
To do that open the `wwwroot/index.html` file and change the
`<base href="/" />` to `<base href="/sharp-engine-browser-demo/" />`.

Then you can create the published version and deploy to the target subfolder.

To run the app in the subfolder **while debugging**, open the `Properties/launchSettings.json` file
and add the following two lines to each profile:
```
      "commandLineArgs": "--pathbase=/sharp-engine-browser-demo",
      "launchUrl": "sharp-engine-browser-demo"
```

After that you can run the Blazor WebAssembly app and it will start in a sharp-engine-browser-demo subfolder.


### See also

- [Troubleshooting](../README.md?tab=readme-ov-file#troubleshooting)
- [Ab4d.SharpEngine samples for desktop and mobile device (demonstrate all features of the engine)](https://github.com/ab4d/Ab4d.SharpEngine.Samples)
- [Online help (for desktop and mobile version of the library)](https://www.ab4d.com/help/SharpEngine/html/R_Project_Ab4d_SharpEngine.htm)
