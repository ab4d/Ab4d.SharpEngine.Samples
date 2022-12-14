# Ab4d.SharpEngine.Samples

![Ab4d.SharpEngine logo](https://www.ab4d.com/images/SharpEngine/sharp-engine_512x218.png)

Welcome to the Samples for Ab4d.SharpEngine.

**Ab4d.SharpEngine is a cross-platform Vulkan based 3D rendering engine for desktop and mobile .Net applications.**

Vulkan is a high performance graphics and cross-platform API that is similar to DirectX 12 but can run on multiple platforms.

The following features are supported by the current version:
- Core SharpEngine objects (Scene, SceneView)
- Many SceneNode objects (ported most of 3D objects from Ab3d.PowerToys)
- Object instancing (InstancedMeshNode)
- Cameras: TargetPositionCamera, FirstPersonCamera, FreeCamera, MatrixCamera
- Lights: AmbientLight, DirectionalLight, PointLight, SpotLight, CameraLight
- Effects: StandardEffect, SolidColorEffect, VertexColorEffect, ThickLineEffect
- ReaderObj to read 3D models from obj files
- Assimp importer that uses third-party library to import 3D models from almost any file format


### Platforms and UI frameworks:

**Windows:**
  - WPF full composition support with SharpEngineSceneView control (Ab4d.SharpEngine.Wpf library)
  - Avalonia UI support with SharpEngineSceneView control (Ab4d.SharpEngine.AvaloniaUI library)
  - WinUI 3 support with SharpEngineSceneView control (Ab4d.SharpEngine.WinUI library)
  - Using SDL or Glfw (using third-party Silk.Net library; the same project also works on Linux)
  - WinForms support (coming in the next beta version)
  - MAUI (coming soon)
  
**Linux** (including Raspberry PI 4):
  - Using SDL or Glfw (using third-party Silk.Net library; the same project also works on Windows)
  - See "Vulkan on Resberry Pi 4" guide on how to use SharpEngine on Resberry Pi 4 with an external monitor.
  
**Android:**
  - Using SurfaceView in C# Android Application
  - Using SDL (using third-party Silk.Net library)
  
**macOS:**
   - Using MoltenVK. See AvaloniaUI project for macOS. 
   
**iOS:** (planned for the next beta version)


Online help:
[Online Ab4d.SharpEngine Reference help](https://www.ab4d.com/help/SharpEngine/html/R_Project_Ab4d_SharpEngine.htm)



### Dependencies:
- The core Ab4d.SharpEngine library has NO EXTERNAL dependencies.
- The Ab4d.SharpEngine.Wpf has NO EXTERNAL dependencies.
- The Ab4d.SharpEngine.WinUI library requires SharpDX.DXGI and SharpDX.Direct3D11 libraries (this will be removed in the future).
- The Ab4d.SharpEngine.AvaloniaUI library requires Avalonia library.


### System requirements to run the samples:
- Net 6.0

### System requirements to open the sample projects:
- Visual Studio 2022 (VS 2019 does not support .Net 6) on Windows
- Rider from JetBrains on Windows, Linux or macOS
- Visual Studio Code on Windows, Linux or macOS


### Expiration date:
The beta version of Ab4d.SharpEngine will expire 6 months after publishing.



## Sample solutions

The following Visual Studio solutions are available:

- **Ab4d.SharpEngine.Samples.Wpf**
  This is the MAJOR SAMPLES SOLUTION because it demonstrates the most of the engine features.
  This solution provides the samples for WPF and can run only on Windows.
  The samples also use Ab4d.SharpEngine.Wpf library that provides SharpEngineSceneView control for WPF.
  The SharpEngineSceneView provides a WPF control that is very easy to be used and can 
  compose the 3D scene with the WPF objects (for example showing buttons on top of 3D scene).
  
  This solution also shows how to use AssimpImporter. 
  Note that this library can be also used on Linux and other systems (native library for Linux is in the libs folder).

- **Ab4d.SharpEngine.Samples.AvaloniaUI**
  This sample uses Avalonia UI (https://avaloniaui.net/) that provides WPF-like object model to 
  build UI controls and can run on Windows, Linux and macOS (macOS is not yet supported by SharpEngine).
  This sample uses Ab4d.SharpEngine.AvaloniaUI library that provides SharpEngineSceneView control.
  The SharpEngineSceneView provides an Avalonia control that is very easy to be used and can 
  compose the 3D scene with the WPF objects (for example showing buttons on top of 3D scene).
  The sample can be started on Windows or Linux.

- **Ab4d.SharpEngine.Samples.CrossPlatform**
  This sample uses third-party Silk.Net library that provides support for SDL and GLFW.
  SDL and GLFW are used to get platform independent way to create windows and views.
  The 3D scene here is shown in the whole window area.
  Because of this project can work on Windows and Linux.
  
- **Ab4d.SharpEngine.Samples.Android.Generic**
  This solution is similar to Ab4d.SharpEngine.Samples.CrossPlatform because it also uses Silk.Net library.
  To work on Android the code to initialize SharpEngine and define the 3D scene can be the same
  as for other platforms, but there needs to be some special startup code to create the Android Activity.
  The 3D scene here is shown on the whole view area.

- **Ab4d.SharpEngine.Samples.Android.Application**
  This solution uses a Xamarin based Android.Application project template for .Net 6.
  The 3D scene can be shown only on the part of the view (defined by SurfaceView).
  


## Quick Start

The main two objects in SharpEngine are:
- Scene object that defines the 3D scene with a hierarchy of 3D objects that are added to the RootNode object.
  It also defines the Lights collection.
- SceneView object is used to show the objects defined by the SceneObject. It defines the Camera and provides the size of the view.

When using WPF, Avalonia or WinUI, then Scene and SceneView are created by the SharpEngineSceneView control.

3D objects are defined in the SceneNodes namespace.

Common materials are defined by using StandardMaterial object. 
For each color there are predefined StandardMaterials, for example StandardMaterials.Blue.

Use ReaderObj to read 3D models from obj files.
To read 3D models from other file formats, use AssimpImporter (see Ab4d.SharpEngine.Samples.Wpf samples)



### Comparing to Ab3d.PowerToys and Ab3d.DXEngine

BoxVisual3D, SphereVisual3D and other objects derived from BaseVisual3D are defined in SceneNodes namespace
(for example BoxVisual3D => BoxModelNode; SphereVisual3D => SphereModelNode).

GeometryModel3D with custom MeshGeometry3D from WPF 3D are now defiend by MeshModelNode and StandardMesh.
Meshes for standard objects can be created by using Meshes.MeshFactory.

Cameras and lights are almost the same as in Ab3d.PowerToys.

MouseCameraController for WPF, Avalonia or WinUI is almost the same as in Ab3d.PowerToys.
For Android you can use AndroidCameraController.
For other platforms you can use ManualMouseCameraController and then call the ProcessMouseDown, ProcessMouseUp and ProcessMouseMove methods.


### Advantages of Ab3d.DXEngine with Ab3d.PowerToys

- Ab3d.DXEngine and Ab3d.PowerToys are very mature products that were tested in the "field" by many customers.
- They provide more features and more samples that can be used as code templates for your needs.
- Ab3d.DXEngine supports multi-threading and currently provides faster 3D rendering for many use cases.
- Ab3d.DXEngine can use software rendering when there is no graphics card present.
- Ab3d.DXEngine and Ab3d.PowerToys can run on older .Net versions including .Net framework 4.5+.


### Advantages of Ab4d.SharpEngine

- Ab4d.SharpEngine can run on multiple platforms. You can start writing code for Windows and later simply add support for Linux, macOS, Android and iOS.
- Ab4d.SharpEngine uses Vulkan API that is the most advanced graphics API that is actively developed as new versions of graphics cards are released. This provides options to support all current and future graphics features (for example Ray tracing).
- Ab4d.SharpEngine has a cleaner programming API because there is only a single set of 3D models (SceneNodes, Camera, Lights). On the other hand the API is not so nice. The Ab3d.PowerToy builds on top of WPF 3D objects that are not very extendable so some compromises were needed (for example cameras are derived from FrameworkElement and not Camera). Also, Ab3d.DXEngine converts all WPF 3D and Ab3d.PowerToys objects into its own objects so the application has 2 versions of each object. Also, some tricks (for example using SetDXAttribute) must be used to provide Ab3d.DXEngine features on Ab3d.PowerToys objects.
- Working with WPF objects is very slow. Also, Ab3d.DXEngine needs to convert all those objects into its own objects. Working with objects in Ab4d.SharpEngine is much faster.
- Vulkan is significantly faster graphics API than DirectX 11. In the future Ab4d.SharpEngine will be significantly faster than Ab3d.DXEngine in all cases.
- Ab4d.SharpEngine is built on top of .Net 6 and that provides many performance benefits because of using System.Numerics, Span and other improved features.
- In the future Ab4d.SharpEngine will provide more functionality than Ab3d.DXEngine with Ab3d.PowerToys.


## Troubleshooting

Known issues:
- Vulkan requires very complex memory and object lifecycle management. If the application crashes or you get an error warning, please provide details about that.

- Some Intel graphics cards may not work with shared texture in WPF's SharpEngineSceneView control (writable bitmap is used instead, but this is slower).

- When using SharedTexture in WPF application, some older graphics cards may produce WPF's UCEERR_RENDERTHREADFAILURE (0x88980406) error.

- ReaderObj cannot read obj files from a stream (this makes it impossible to read obj files Android)


To enable Vulkan validation, install the Vulkan SDK from: https://vulkan.lunarg.com/
When Vulkan validation is installed and enabled by the SharpEngine (EnableStandardValidation is set to true when creating the engine),
then each Vulkan call is checked by the validation error and this can give much better error reports
(all Vulkan validation reports are logged at Warn log level).


The alpha and beta version are compiled with release build options but support full logging.
This means that it is possible to get Trace level log messages
(production version will have only Warning and Error logging compiled into the assembly).

When you have some problems, then please enable Trace level logging and writing log messages to a file or debug output.
To do this please find the existing code that sets up logging an change it to:
  
    Ab4d.SharpEngine.Utilities.Log.LogLevel = LogLevels.Trace;       
    Ab4d.SharpEngine.Utilities.Log.WriteSimplifiedLogMessage = false; // write full log messages timestamp, thread id and other details
  
    // Use one of the following:
  
    // Write log to file
    Ab4d.SharpEngine.Utilities.Log.LogFileName = @"c:\SharpEngine.log";
  
    // Write log messages to output window (for example Visual Studio Debug window) 
    // Ab4d.SharpEngine.Utilities.Log.IsLoggingToDebugOutput = true; 
  
    // Write to local StringBuilder
    private System.Text.StringBuilder _logStringBuilder;
    Ab4d.SharpEngine.Utilities.Log.AddLogListener((logLevel, message) => _logStringBuilder.AppendLine(message));



## Change log

Beta1 version (2022-12-14): 
- first beta version


## Roadmap

### Future beta versions

- Improve stability
- Improve SharedTexture support for integrated Intel graphic cards
- Add many samples in a similar way as the samples for Ab3d.PowerToys and Ab3d.DXEngine
- Port more functionality from Ab3d.PowerToys and Ab3d.DXEngine
- Support for multiple SceneViews that show a single Scene
- RenderToBitmap with custom bitmap size
- SharpEngineSceneView control for WinForms and Android
- Improve sample for macOS
- Add sample for iOS
- Add MAUI support
- Improve Ab4d.SharpEngine.AvaloniaUI library so it will also work on Android, macOS and iOS.
- Add support for 2D sprites
- Improve xml documentation and online reference help

- Public tests project on GitHub


### v1.0 release (Q2-Q3 2023)

- Production ready for Windows, major Linux distributions and Android. Other platforms may still be in beta.

- Full reference documentation (documenting all public classes, methods, properties and fields)
- Supersampling
- Objects and camera animation similar to Anime.js
- Add support for PhysicallyBasedRendering effect


### Later versions

- Multi-threaded rendering
- Rendering 3D lines with arrows (currently arrow is created by additional lines that define the arrow)
- Gltf 2 file reader included in SharpEngine
- Shadows
- Python binding and samples


### Distant Future

- Add support for WebGPU (or WebGL) and WebAssembly so the engine can work in a web browser. 
  This technology is currently not yet ready to provide good support for complex applications such as 3D rendering engine.
