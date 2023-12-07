# Ab4d.SharpEngine.Samples

![Ab4d.SharpEngine logo](https://www.ab4d.com/images/SharpEngine/sharp-engine_512x218.png)

Welcome to the Samples for Ab4d.SharpEngine.

**Ab4d.SharpEngine is a cross-platform Vulkan based 3D rendering engine for desktop and mobile .Net applications.**

Vulkan is a high performance graphics and cross-platform API that is similar to DirectX 12 but can run on multiple platforms.

The following features are supported by the current version:
- Using any coordinate system (y-up or z-up, right-handed or left-handed)
- Many SceneNode objects (boxes, spheres, planes, cones, lines, poly-lines, curves, etc.)
- Object instancing (InstancedMeshNode)
- Cameras: TargetPositionCamera, FirstPersonCamera, FreeCamera, MatrixCamera
- Camera controllers with rotate around mouse position, zoom to position and other advanced functions
- Lights: AmbientLight, DirectionalLight, PointLight, SpotLight, CameraLight
- Effects: StandardEffect, SolidColorEffect, VertexColorEffect, ThickLineEffect
- ReaderObj to read 3D models from obj files
- Assimp importer that uses third-party library to import 3D models from almost any file format


### Platforms and UI frameworks:

**Windows:**
  - WPF full composition support with SharpEngineSceneView control (Ab4d.SharpEngine.Wpf library)
  - AvaloniaUI support with SharpEngineSceneView control (Ab4d.SharpEngine.AvaloniaUI library)
  - WinUI 3 support with SharpEngineSceneView control (Ab4d.SharpEngine.WinUI library)
  - Using SDL or Glfw (using third-party Silk.Net library; the same project also works on Linux)
  - MAUI
  - WinForms support (coming soon)
  
**Linux** (including Raspberry PI 4):
  - AvaloniaUI support with SharpEngineSceneView control (Ab4d.SharpEngine.AvaloniaUI library)
  - Using SDL or Glfw (using third-party Silk.Net library; the same project also works on Windows)
  - Off-screen rendering combined with Linux framebuffer display (FbDev or DRM/KMS).
  - See ["Vulkan on Resberry Pi 4"](https://www.ab4d.com/SharpEngine/Vulkan-rendering-engine-on-Raspberry-Pi-4.aspx) guide on how to use SharpEngine on Resberry Pi 4 with an external monitor.
  
**Android:**
  - Using SurfaceView in C# Android Application
  - Using SDL (using third-party Silk.Net library)
  - MAUI
  
**macOS:**
  - Using AvaloniaUI with SharpEngineSceneView control (Ab4d.SharpEngine.AvaloniaUI library). Requires MoltenVK library - see special project for macos.
  - Using MAUI - requires MoltenVK library - see Building for macOS and iOS below.
   
**iOS:**
  - Using MAUI - requires .Net 8 and MoltenVK library - see "Building for macOS and iOS" below.


Online help:
[Online Ab4d.SharpEngine Reference help](https://www.ab4d.com/help/SharpEngine/html/R_Project_Ab4d_SharpEngine.htm)



### Dependencies:
- The core Ab4d.SharpEngine library has NO EXTERNAL dependencies.
- The Ab4d.SharpEngine.Wpf has NO EXTERNAL dependencies.
- The Ab4d.SharpEngine.WinUI library requires SharpDX.DXGI and SharpDX.Direct3D11 libraries (this will be removed in the future).
- The Ab4d.SharpEngine.AvaloniaUI library requires Avalonia library.


### System requirements to run the samples:
- NET 6.0+
- NET 8.0 is required to use MAUI


### System requirements to open the sample projects:
- Visual Studio 2022 on Windows (VS 2019 does not support .Net 6)
- Rider from JetBrains on Windows, Linux and macOS
- Visual Studio for mac on macOS
- Visual Studio Code on Windows, Linux and macOS


### Expiration date:
The beta version of Ab4d.SharpEngine will expire around 6 months after publishing. See warning log message for the exact date of expiration.



## Sample solutions

The following Visual Studio solutions are available:

- **Ab4d.SharpEngine.Samples.Wpf**
  This solution provides the samples for WPF and can run only on Windows.
  The samples also use Ab4d.SharpEngine.Wpf library that provides SharpEngineSceneView control for WPF.
  The SharpEngineSceneView provides a WPF control that is very easy to be used and can 
  compose the 3D scene with the WPF objects (for example showing buttons on top of 3D scene).

- **Ab4d.SharpEngine.Samples.AvaloniaUI**
  This sample uses Avalonia UI (https://avaloniaui.net/) that provides WPF-like object model to 
  build UI controls and can run on Windows, Linux and macOS.
  This sample uses Ab4d.SharpEngine.AvaloniaUI library that provides SharpEngineSceneView control.
  The SharpEngineSceneView provides an Avalonia control that is very easy to be used and can 
  compose the 3D scene with the Avalonia UI objects (for example showing buttons on top of 3D scene).
  The sample can be started on Windows, Linux and on macOS (use special macos solution).
  See also "Building for macOS and iOS" section for more information on how to compile for macOS.
  
- **Ab4d.SharpEngine.Samples.WinUI**
  This sample uses WinUI 3.0 that provides the latest UI technology to create applications for Windows.
  This sample uses Ab4d.SharpEngine.WinUI library that provides SharpEngineSceneView control.
  The SharpEngineSceneView provides an WinUI control that is very easy to be used and can 
  compose the 3D scene with the WinUI UI objects (for example showing buttons on top of 3D scene).

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
  The 3D scene is shown on the part of the view that is defined by SurfaceView.

- **Ab4d.SharpEngine.Samples.Maui**
  This solution uses a NET Maui and can work on Windows, Android, macOS and iOS.
  Compiling for Windows, Android and macOS requires .Net 7 or newer.
  Compiling for iOS requires .Net 8 (at the time of writing this preview7 is required).
  Because Vulkan is not natively supported on macOS and iOS, the MoltenVK library is required to translate the Vulkan calls to Molten API calls.
  See "Building for macOS and iOS" section for more information on how to compile for macOS and iOS.

- **Ab4d.SharpEngine.Samples.LinuxFramebuffer**
  This solution uses SharpEngine with off-screen Vulkan renderer, and displays
  the rendered frames on Linux framebuffer display (FbDev or DRM/KMS). See
  [the example's README](Ab4d.SharpEngine.Samples.LinuxFramebuffer/README.md)
  for details.


## Quick Start

The main two objects in SharpEngine are:
- Scene object that defines the 3D scene with a hierarchy of 3D objects that are added to the RootNode object.
  It also defines the Lights collection.
- SceneView object is used to show the objects that are defined by the Scene object. SceneView also defines the Camera and provides the size of the view.

When using WPF, Avalonia or WinUI, then Scene and SceneView are created by the SharpEngineSceneView control.

3D objects are defined in the SceneNodes namespace, for example BoxModelNode, SphereModelNode, LineNode, MeshModelNode, ect.

Common materials are defined by using StandardMaterial object. 
For each color there are predefined StandardMaterials, for example StandardMaterials.Blue.

Use ReaderObj to read 3D models from obj files.
To read 3D models from other file formats, use AssimpImporter.



## Building for macOS and iOS
  
The following changes are required to use Ab4d.SharpEngine on macOS and iOS:
- .Net 8 is requried to use Ab4d.SharpEngine on iOS (because function pointers do not work with .Net 7 on iOS). Mac Catalyst can run on .Net 7, but it is recommended to use .Net 8. It is possible to use preview version of .Net 8 - at the time of writing this preview 7 was used.

- To use preview version of .Net 8 in Visual Studio for Mac, you need to enable .Net 8. This is done in Preferences / Preview Featrues / check "Use the .NEt 8 SDK if installed".

- The 3D scene that is rendered by Ab4d.SharpEngine is shown by using SKCanvasView. To use that control, add reference to SkiaSharp.Views.Maui.Controls NuGet package. The add ".UseSkiaSharp()" to the bulder setup in the MauiProgram.cs file.

- Add libMoltenVK.dylib from the Vulkan SDK to the projects so that the library can be loaded at runtime. Note that there are different builds for iOS and for Catalyst (the lates use the version of macOS). When running the verson of Mac Catalyst the sample app can also use the library from the installed Vulkan SDK. The preview 7 version of .Net 8 and the Visual Studio for Mac v17.6 can sometimes produce "clang++ exited with code 1" error when compiling. I do not know why this happens and how to solve that. Sometimes it helps to delete obj and bin folder and restart the Visual Studio. If this do not help, remove the inclusion of libMoltenVK.dylib for catalyst - in this case install the Vulkan SKD to the computer and the Catalyst app will use the library from Vulkan SKD folder.

- To run the app in iOS, the application need to have provisionining profile set. One option is to follow the instructions on the following page: https://learn.microsoft.com/en-us/dotnet/maui/ios/capabilities?tabs=vs Another option is to open the project in the Rider IDE, then right click on the project and select "Open in Xcode". Rider will create the Xcode project file and open it in Xcode. There you can click on the project file and in the "Certificates, Identifiers & Profiles" tab create an ad-hoc provisioning profile (allow having up to 3 development apps installed at the same time). See more: https://developer.apple.com/help/account/manage-profiles/create-a-development-provisioning-profile/ Note that to create the provisioning profile, the ApplicationId (in csproj file) needs to be in a form of "com.companyName.appName" - this is then used as a Bundle Id.



### Comparing to Ab3d.PowerToys and Ab3d.DXEngine

BoxVisual3D, SphereVisual3D and other objects derived from BaseVisual3D are defined in SceneNodes namespace
(for example BoxVisual3D => BoxModelNode; SphereVisual3D => SphereModelNode).

GeometryModel3D with custom MeshGeometry3D from WPF 3D are now defiend by MeshModelNode and StandardMesh.
Meshes for standard objects can be created by using Meshes.MeshFactory.

Cameras and lights are almost the same as in Ab3d.PowerToys, for example there is also TargetPositionCamera with the same properties are in Ab3d.PowerToys.

MouseCameraController for WPF, Avalonia or WinUI is almost the same as in Ab3d.PowerToys.
For Android you can use AndroidCameraController.
For other platforms you can use ManualMouseCameraController and then call the ProcessMouseDown, ProcessMouseUp and ProcessMouseMove methods.


### Advantages of Ab3d.DXEngine with Ab3d.PowerToys

- Ab3d.DXEngine and Ab3d.PowerToys are very mature products that are tested and proven in the "field" by many customers.
- Those two libraries provide more features and come with more samples that can be used as code templates for your needs.
- Ab3d.DXEngine supports multi-threading and currently provides faster 3D rendering in many use cases.
- Ab3d.DXEngine can use software rendering when there is no graphics card present (for example in virtual machines or on a server).
- Ab3d.DXEngine and Ab3d.PowerToys can run on older .Net versions including .Net framework 4.5+.


### Advantages of Ab4d.SharpEngine

- Ab4d.SharpEngine can run on multiple platforms. You can start writing code for Windows and later simply add support for Linux, macOS, Android and iOS. Or port just a smaller part of the application to other platforms.
- Ab4d.SharpEngine uses Vulkan API that is the most advanced graphics API that is actively developed and gets new features as new versions of graphics cards are released. This provides options to support all current and future graphics features (for example Ray tracing - not possible with DirectX 11).
- Ab4d.SharpEngine was built from the ground up and therefore has a very clean and easy to use programming API. For example, there is only a single set of 3D models (SceneNodes, Camera, Lights). When using Ab3d.DXEngine and Ab3d.PowerToys, the API is not very nice in all the cases. The Ab3d.PowerToy was built on top of WPF 3D objects that are not very extendable so some compromises were needed (for example cameras are derived from FrameworkElement and not from Camera). Also, Ab3d.DXEngine converts all WPF 3D and Ab3d.PowerToys objects into its own objects so the application has 2 versions of each object. In other cases, some tricks must be used to provide Ab3d.DXEngine features to Ab3d.PowerToys and WPF 3D objects (for example using SetDXAttribute).
- Working with WPF objects is very slow (accessing DependencyProperties has a lot of overhead). Also, Ab3d.DXEngine needs to convert all WPF objects into its own objects. Working with objects in Ab4d.SharpEngine is much faster.
- Vulkan is a significantly faster graphics API than DirectX 11. Though the Ab4d.SharpEngine does not use all the fastest algorithms yet (no multi-threading), in the future the engine will be significantly faster than Ab3d.DXEngine.
- Ab4d.SharpEngine is built on top of .NET 6 and that provides many performance benefits because of using System.Numerics, Span and other improved .NET features.
- In the future Ab4d.SharpEngine will provide more functionality than Ab3d.DXEngine with Ab3d.PowerToys.

NOTE:
Ab3d.PowerToys and Ab3d.DXEngine will still be actively developed, will get new releases and features and will have full support in the future!


## Troubleshooting

Known issues:
- Some Intel graphics cards may not work with shared texture in WPF's SharpEngineSceneView control (writable bitmap is used instead, but this is slower).

- When using SharedTexture in a WPF application, some older graphics cards may produce WPF's UCEERR_RENDERTHREADFAILURE (0x88980406) error. Set SharpEngineSceneView.PresentationType to WriteableBitmap as a workaround.



To enable Vulkan validation, install the Vulkan SDK from: https://vulkan.lunarg.com/
When Vulkan validation is installed and enabled by the SharpEngine (EnableStandardValidation is set to true when creating the engine),
then each Vulkan call is checked by the validation error and this can give much better error reports
(all Vulkan validation reports are logged at Warn log level).


The beta versions of Ab4d.SharpEngine are compiled with release build options but support full logging.
This means that it is possible to get Trace level log messages
(production version will have only Warning and Error logging compiled into the assembly).

When you have some problems, then please enable Trace level logging and writing log messages to a file or debug output.
To do this please find the existing code that sets up logging and change it to:
  
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

**v1.0.8740** (2023-12-07):
- Simplified GetChild, GetAllChildren and ForEachChild in GroupNode. Removed search by regular expression. The wildcard (using '*') search is now automatically determined from the specified name.
- Removed SerializeToJson and DeserializeJson from Camera because they were rarely used. This removed reference to System.Text.Json assembly.
- Added Camera.Name property that can be set when creating the camera.
- Prevented throwing "Value cannot be null" exception when CreateOptions.ApplicationName was null or empty string.
- Fixed rendering semi-transparent rectangles with SpriteBatch
- Fixed WpfBitmapIO to set HasTransparency property
- Fixed WinUIBitmapIO by converting to pre-multiplied alpha
- Changed default sampler type from Wrap to Mirror.
- Documented many additional classes, properties and methods. See online help here: https://www.ab4d.com/help/SharpEngine/html/R_Project_Ab4d_SharpEngine.htm

Breaking changes:
- Change the order of parameters in the VulkanDevice.Create methods - the EngineCreateOptions parameter was moved after surface parameters because it is now optional.
- Removed IRotatedViewCamera interface and moved ViewRotation property from camera to SceneView
- Removed public VulkanInstance and VulkanDevice constructors. Now it is possible to create VulkanInstance and VulkanDevice objects only by using static Create methods (before both constructor and Create method were available).
- Renamed some parameter names in some methods in transformation classes (uniformScale to scale)
- Renamed FreeCamera.CalculateUpDirectionFromPositions to CalculateCurrentUpDirection
  
**v0.9.20 RC1** (2023-11-15):
- Added support for custom coordinate system - it can be changed by calling Scene.SetCoordinateSystem. Supported coordinate systems: YUpRightHanded (default), YUpLeftHanded, ZUpRightHanded, ZUpLeftHanded. There are also new methods in Scene and CameraUtils that can help you get information about the coordinate system.
- Added CameraAxisPanel, which can show a small panel displaying the orientation of the X, Y, and Z axes.
- Added PngBitmapIO class to SharpEngine. It can read or write png images so no third-party library is needed anymore to import textures or save rendered bitmap to disk.
- Added SolidColorMaterial to make it easier to use solid color material (before user need to use StandardMaterial and set Effect to SolidColorEffect).
- Added PlaneModelNode.AlignWithCamera method that orients the plane model so that it is facing the camera.
- Added GetCameraPlaneOrientation to camera classes.
- Added support to load textures from stream with new overloads to LoadDiffuseTexture method in StandardMaterialBase (base class for StandardMaterial and SolidColorMaterial). Before TextureLoader was needed to create a texture from stream.
- bitmapIO parameter is now optional in the LoadDiffuseTexture method in StandardMaterialBase. When bitmapIO is null, then DefaultBitmapIO from GpuDevice is used.
- Removed SharpDX dependency from Ab4d.SharpEngine.WinUI library (add DirectX 11 interop code to the library).
- Added SharpEngineSceneView.DisableWpfResizingOfRenderedImage in Ab4d.SharpEngine.Wpf. When set to default true value, it produces sharper rendered image.
- Added EngineCreateOptions.AdditionalValidationFeatures
- Fixed using model or parent Group transformation on InstancedMeshNode.
- Fixed moving camera with MouseCameraController in some cases when using an orthographic camera
- Fixed disposing DirectX 11 device (used by Ab4d.SharpEngine.Avalonia on Windows and can be used by Ab4d.SharpEngine.Wpf with Intel gpu).
- Improved LineSelectorData so that in case the LineSelectorData is created with LineNode, then LineNode's WorldMatrix will be used to transform all the line positions.
- Improved AssimpImporter - names that are assigned to created GroupNode and MeshModelNode from Assump's Nodes are assigned more correctly.
- Updated SpriteBatch class:
-   Renamed Draw method to DrawSprite
-   Added DrawBitmapText method to render a 2D text behind or on top of 3D scene
-   Added DrawRectangle method to render a 2D rectangle behind or on top of 3D scene

Breaking changes:
- Changed the order of parameters in TextureLoader.CreateTexture method - the bitmapIO is now optional and was moved after scene or gpuDevice parameters. When bitmapIO is not set, then DefaultBitmapIO from GpuDevice is used.
- Removed CreateTextureMaterial methods from TextureLoader. StandardMaterial and SolidColorMaterial with texture can be easily created by using class constructor and providing file name or file stream.
- Removed Scene.BitmapIO property and added VulkanDevice.DefaultBitmapIO property that is set to an instance of PngBitmapIO. This provides the default (and fallback) png loader to load textures from png files so not other third-party BitmapIO is needed.
- Renamed IBitmapIO.ConvertToBgra to ConvertToSupportedFormat and updated the code accordingly (now the rgba images are not converted to bgra anymore but are shown by the engine in its original format).
- Ranamed AssimpImporter.ImportSceneNodes method to Import. Also changed the return type from SceneNode to GroupNode.


**v0.9.18 beta6** (2023-10-20):
- TextBlockFactory, BitmapTextCreator and BitmapFont are now part of SharpEngine. Also there is a build-in bitmap font that can be used without the need to provide your font.
- Improved ways to manually dispose objects and resources by adding the following methods: GroupNode.DisposeAllChildren, GroupNode.DisposeWithAllChildren, GroupNode.DisposeChildren, ModelNode.DisposeWithMaterial, MeshModelNode.DisposeWithMeshAndMaterial, LineBaseNode.DisposeWithMaterial and StandardMaterial.DisposeWithTexture (see online help for more info: https://www.ab4d.com/help/SharpEngine/html/R_Project_Ab4d_SharpEngine.htm)
- Added support for rendering sprites - see SpritesSample
- Renamed GroupNode.GetFirstChild to GetChild
- Improved support for using SharedTexture for WPF on Intel graphics cards. Before WritableBitmap was used because Intel's Vulkan driver do not support sharing DirectX 9 texture (created by WPF) with Vulkan. Note that this requries copying to another texture and this means that for integrated Intel GPU this is not faster so for now WritableBitamp is used by default. SharedTexture can be forece by setting IsUsingSharedTextureForIntegratedIntelGpu to true.
- When calling RenderToBitmap or similar method and when format parameter is omitted, them the currently used Format from SceneView is used (before Bgra was used).
- Updated RenderingSteps that render objects: FillCommandBufferRenderingStep is now abstract; there are new RenderObjectsRenderingStep and RenderSpritesRenderingStep; renamed SceneView.DefaultFillCommandBufferRenderingStep to SceneView.DefaultRenderObjectsRenderingStep; renamed CompleteRenderingStep to CompleteRenderingRenderingStep; renamed SceneView.DefaultCompleteRenderingRenderingStep to SceneView.DefaultCompleteRenderingStep
- Added ClampNoInterpolation to CommonSamplerTypes - can be used for height maps with hard gradient.
- Fixed hit-testing for InstancedMeshNode
- Fixed using transparency in WPF's WriteableBitmap.
- Updated TextureLoader to add options to cache a loaded texture (GpuImage) in a Scene's cache and not only in GpuDevice's cache. This way the textures are disposed when the Scene is disposed.
- ReaderObj and AssimpImporter now have new constructors that take Scene object. When used, then textures are cached by the Scene and not by the GpuDevice.

- Updated native Assimp importer to v5.3.1.
- Updated Ab3d.Assimp library to correctly read file with non-ascii file names

Breaking change:
- The cacheGpuTexture paramter in TextureLoader.CreateTexture has been renamed to useGpuDeviceCache. Note that the new version also allows using Scene's cache for texture. To use that call CreateTexture by providing the Scene object and setting the useSceneCache to true.

**v0.9.16 beta5** (2023-09-15):
- Improved ReaderObj to also read textures.
- Swapped AssimpImporter constructor parameters (first parameter is now BitmapIO and not GpuDevice - BitmapIO is more important becuse it is required to load textures)
- AssimpImporter can now read textures even when it is not created with a valid GpuDevice (in this case textures are lazily loaded)
- Added RenderToBitmap method to SharpEngineSceneView that take WritableBitmap as parameter

- Ab4d.SharpEngine.AvaloniaUI: removed dependency from SharpDX.DXGI and SharpDX.Direct3D11

**v0.9.15 beta4** version (2023-08-23):
- Engine can load the vulkan loader from the path that is set to the VK_DRIVER_FILES environment variable (see the following on how to use SharpEngine in a virtual machine or a web server: https://www.ab4d.com/SharpEngine/using-vulkan-in-virtual-machine-mesa-llvmpipe.aspx)
- Removed isDeviceLocal parameter from GpuImage constructor and TextureLoader.CreateTexture method.
- By default MSAA (multi-sampling anti-aliasing) is disabled for software renderer (Mesa's llvmpipe).
- Renamed SharpEngineSceneView.RequiredDeviceExtensionNames to RequiredDeviceExtensionNamesForSharedTexture
- Added DesiredInstanceExtensionNames and DesiredDeviceExtensionNames to EngineCreateOptions class (before there were only RequiredInstanceExtensionNames and RequiredDeviceExtensionNames).
- Moved methods to create edge lines from Ab4d.SharpEngine.Utilities.EdgeLinesFactory class to Ab4d.SharpEngine.Utilities.LineUtils class.
- Many other improvements and fixes

**v0.9.10 beta3** version (2023-05-10):
- Many improvements and fixes

**v0.9.7 beta2** version (2023-04-14): 
- Added many samples to help you understand the SharpEngine and provide code templates for your projects
- Improve SharedTexture support for integrated Intel graphic cards and older graphics cards
- Using SwapChainPanel for WinUI instead of SurfaceImageSource - this is faster and better supported by WinUI
- Helped design [External GPU memory interop (OpenGL, Vulkan, DirectX)](https://github.com/AvaloniaUI/Avalonia/issues/9925) and then implemented a new and much better way to share Vulkan texture with Avalonia
- Objects and camera animation similar to Anime.js
- Breaking change: Renamed StandardMaterial.Alpha property to Opacity

**v0.9.0 beta1** version (2022-12-14): 
- first beta version



## Plans for later versions

- Pixel and point cloud rendering
- Supersampling
- Add support for PhysicallyBasedRendering effect
- Multi-threaded rendering
- Rendering 3D lines with arrows (currently arrow is created by additional lines that define the arrow)
- Shadows
- Python binding and samples


### Distant Future

- Add support for WebGPU (or WebGL) with Blazor WebAssembly so the engine can work in a web browser. 
  This technology is currently not yet ready to provide good support for complex applications such as 3D rendering engine.
