# Ab4d.SharpEngine.Samples

![Ab4d.SharpEngine logo](doc/animated-samples2.png)

Welcome to the Samples for Ab4d.SharpEngine.

**Ab4d.SharpEngine is a cross-platform Vulkan based 3D rendering engine for desktop and mobile .Net applications.**

Vulkan is a high performance graphics and cross-platform API that is similar to DirectX 12 but can run on multiple platforms.

The following features are supported by the current version:
- Using any coordinate system (y-up or z-up, right-handed or left-handed)
- Many SceneNode objects (boxes, spheres, planes, cones, lines, poly-lines, curves, etc.)
- Render line caps (arrows, etc.), line with pattern, poly-lines with mitter or bevel connections, hidden lines
- Object instancing (InstancedMeshNode)
- Cameras: TargetPositionCamera, FirstPersonCamera, FreeCamera, MatrixCamera
- Camera controllers with rotate around the mouse position, zoom to position and other advanced functions
- Lights: AmbientLight, DirectionalLight, PointLight, SpotLight, CameraLight
- Effects: StandardEffect, SolidColorEffect, VertexColorEffect, ThickLineEffect
- Improved visual quality with super-sampling and multi-sampling
- Render vector and bitmap text
- ReaderObj to read 3D models from obj files
- Import 3D objects from glTF files and export the scene to glTF file by using [Ab4d.SharpEngine.glTF](https://www.nuget.org/packages/Ab4d.SharpEngine.glTF)
- Assimp importer that uses a third-party library to import 3D models from almost any file format


Ab4d.SharpEngine library is a **commercial library** but it is also **free for non-commercial open-source projects**.

The commercial license can be purchased from [purchase web page](https://www.ab4d.com/Purchase.aspx). With a commercial license, you also get priority email support and other benefits (feature requests, online support on your projects with sharing screen, etc.).
To get a trial license for your own projects (not needed for the sample projects in this repo) or to apply for the free open-source license, see the [ab4d.com/trial](https://www.ab4d.com/trial).

### Platforms and UI frameworks:

**Windows:**
  - AvaloniaUI support with SharpEngineSceneView control (Ab4d.SharpEngine.AvaloniaUI library)
  - WPF full composition support with SharpEngineSceneView control (Ab4d.SharpEngine.Wpf library)
  - WinUI 3 support with SharpEngineSceneView control (Ab4d.SharpEngine.WinUI library)
  - WinForms support with SharpEngineSceneView control (Ab4d.SharpEngine.WinForms library)
  - Uno Platform
  - MAUI
  - Using SDL or Glfw (using a third-party Silk.Net library; the same project also works on Linux)
  - ImGui (using a third-party ImGui.NET library)
  
**Linux** (including Raspberry PI 4 and similar devices):
  - AvaloniaUI support with SharpEngineSceneView control (Ab4d.SharpEngine.AvaloniaUI library)
  - Uno Platform
  - Using SDL or Glfw (using third-party Silk.Net library; the same project also works on Windows)
  - Off-screen rendering combined with Linux framebuffer display (FbDev or DRM/KMS).
  - ImGui (using a third-party ImGui.NET library)
  - See ["Vulkan on Raspberry Pi 4"](https://www.ab4d.com/SharpEngine/Vulkan-rendering-engine-on-Raspberry-Pi-4.aspx) guide on how to use SharpEngine on Raspberry Pi 4 with an external monitor.
  
**Android:**
  - Using AvaloniaUI with SharpEngineSceneView control (Ab4d.SharpEngine.AvaloniaUI library).
  - Using SurfaceView in C# Android Application
  - MAUI
  
**macOS:**
  - Using AvaloniaUI with SharpEngineSceneView control (Ab4d.SharpEngine.AvaloniaUI library). It requires the MoltenVK library. See "Building for macOS and iOS" below.
  - Using MAUI - requires MoltenVK library - see "Building for macOS and iOS" below.
   
**iOS:**
  - AvaloniaUI with SharpEngineSceneView control (Ab4d.SharpEngine.AvaloniaUI library). It requires the MoltenVK library. See "Building for macOS and iOS" below.
  - Using MAUI - requires MoltenVK library - see "Building for macOS and iOS" below.


Online help:
[Online Ab4d.SharpEngine Reference help](https://www.ab4d.com/help/SharpEngine/html/R_Project_Ab4d_SharpEngine.htm)



### Dependencies:
- The core Ab4d.SharpEngine library has NO EXTERNAL dependencies.
- The Ab4d.SharpEngine.Wpf has NO EXTERNAL dependencies.
- The Ab4d.SharpEngine.WinUI has NO EXTERNAL dependencies.
- The Ab4d.SharpEngine.WinForms has NO EXTERNAL dependencies.
- The Ab4d.SharpEngine.AvaloniaUI library requires Avalonia library.
- The Ab4d.SharpEngine.glTF.


### System requirements to run the samples:
- NET 6.0+
- NET 8.0+ is required to use MAUI


### System requirements to open the sample projects:
- Visual Studio 2022 on Windows
- Rider from JetBrains on Windows, Linux and macOS
- Visual Studio Code on Windows, Linux and macOS


## Sample solutions

The following Visual Studio solutions are available:

- **Ab4d.SharpEngine.Samples.AvaloniaUI**\
  This sample uses Avalonia UI (https://avaloniaui.net/) that provides WPF-like object model to 
  build UI controls and can run on Windows, Linux and macOS.
  This sample uses Ab4d.SharpEngine.AvaloniaUI library that provides SharpEngineSceneView control.
  The SharpEngineSceneView provides an Avalonia control that is very easy to use and can 
  compose the 3D scene with the Avalonia UI objects (for example, showing buttons on top of the 3D scene).
  The sample can be started on Windows, Linux, and macOS.
  See also "Building for macOS and iOS" section for more information on how to compile for macOS.

- **Ab4d.SharpEngine.Samples.AvaloniaUI.VulkanBackend**\
  This sample uses Avalonia UI, which uses Vulkan as a backend, so the whole application is using Vulkan API
  (the UI controls are also rendered by Vulkan instead of DirectX or OpenGL as by default).
  Vulkan backend is setup in the Program.cs file.
  This provides the best integration of 2D UI and 3D graphics.
  This sample can run only on Windows.

- **Ab4d.SharpEngine.Samples.AvaloniaUI.CrossPlatform**\
  This sample shows how to create an Avalonia app that can run on Windows, Linux, macOS, Android and iOS.
  This sample uses Ab4d.SharpEngine.AvaloniaUI library that provides SharpEngineSceneView control.
  Because Vulkan is not natively supported on macOS and iOS, the MoltenVK library is required to translate the Vulkan calls to Molten API calls.
  See also "Building for macOS and iOS" section for more information on how to compile for iOS.
  Note that folder and file names in this solution have been shortened to prevent problems with max path size on Windows (260 chars).

- **Ab4d.SharpEngine.Samples.Wpf**\
  This solution provides the samples for WPF and can run only on Windows.
  The samples also use Ab4d.SharpEngine.Wpf library that provides SharpEngineSceneView control for WPF.
  The SharpEngineSceneView provides a WPF control that is very easy to use and can 
  compose the 3D scene with the WPF objects (for example, showing buttons on top of the 3D scene).
  
- **Ab4d.SharpEngine.Samples.WinUI**\
  This sample uses WinUI 3.0, which provides the latest UI technology to create applications for Windows.
  This sample uses Ab4d.SharpEngine.WinUI library that provides SharpEngineSceneView control.
  The SharpEngineSceneView provides a WinUI control that is very easy to use and can 
  compose the 3D scene with the WinUI UI objects (for example, showing buttons on top of the 3D scene).
  
- **Ab4d.SharpEngine.Samples.UnoPlatform**\
  This sample uses Uno Platform, which provides a cross-plaform UI technology to create applications.
  Ab4d.SharpEngine samples for Uno Platform can run on Windows, macOS and Linux.
  Because this sample uses centralized .Net project configuration, the solution file (.sln) is located in 
  the Ab4d.SharpEngine.Samples.UnoPlatform folder.

- **Ab4d.SharpEngine.Samples.WinForms**\
  This solution provides the samples for WinForms and can run only on Windows.
  The samples also use Ab4d.SharpEngine.WinForms library that provides SharpEngineSceneView Control for WinForms.
  The SharpEngineSceneView provides a WinForms Control that is very easy to use and can 
  compose the 3D scene with other UI Controls (for example, showing buttons on top of the 3D scene).
  
- **Ab4d.SharpEngine.Samples.SilkNet.Sdl / .SilkNet.Glfw**\
  This sample uses a third-party Silk.Net library that provides support for SDL and GLFW.
  SDL and GLFW are used to get platform-independent ways to create windows and views.
  The 3D scene here is shown in the whole window area.
  This project can work on Windows and Linux.

- **Ab4d.SharpEngine.Samples.Android.Application**\
  This solution uses an Android.Application project template for .Net.
  The 3D scene is shown on the part of the view that is defined by SurfaceView.

- **Ab4d.SharpEngine.Samples.Maui**\
  This solution uses a NET Maui and can work on Windows, Android, macOS and iOS.
  Because Vulkan is not natively supported on macOS and iOS, the MoltenVK library is required to translate the Vulkan calls to Molten API calls.
  See "Building for macOS and iOS" section for more information on how to compile for macOS and iOS.

- **Ab4d.SharpEngine.Samples.LinuxFramebuffer**\
  This solution uses SharpEngine with an off-screen Vulkan renderer and displays
  the rendered frames on a Linux framebuffer display (FbDev or DRM/KMS). See
  [the example's README](Ab4d.SharpEngine.Samples.LinuxFramebuffer/README.md)
  for details.

- **Ab4d.SharpEngine.Samples.ImGui**
  This solution shows how to render a user interface that is defined by ImGui.
  It includes the ImGuiRenderingStep class with full source code that shows how to render ImGui by using Ab4d.SharpEngine.
  The solution is using a third-party ImGui.NET library (https://github.com/ImGuiNET/ImGui.NET).


## Quick Start

The two main objects in SharpEngine are:
- **Scene** object that defines the 3D scene with a hierarchy of 3D objects that are added to the RootNode object.
  It also defines the Lights collection.
- **SceneView** object is used to show the objects that are defined by the Scene object. SceneView also defines the Camera and provides the size of the view.

When using WPF, Avalonia, WinUI or WinForms, then Scene and SceneView are created by the **SharpEngineSceneView control**.

3D objects are defined in the SceneNodes namespace, for example: BoxModelNode, SphereModelNode, LineNode, MeshModelNode, etc.

Common materials are defined by using StandardMaterial object. 
For each color there are predefined StandardMaterials, for example StandardMaterials.Blue.

Use ReaderObj to read 3D models from obj files.
To read 3D models from glTF files use Ab4d.SharpEngine.glTF libray. For other file formats, use AssimpImporter.



### Migration guide for Ab3d.PowerToys and Ab3d.DXEngine users

Ab4d.SharpEngine is built on the same concepts as the Ab3d.PowerToys and Ab3d.DXEngine libraries. So users of those two libraries should feel very familiar. But there are some main differences:

For WPF, Avalonia, WinUI and WinForms there is a special library ([Ab4d.SharpEngine.Wpf](https://www.nuget.org/packages/Ab4d.SharpEngine.Wpf), [Ab4d.SharpEngine.AvaloniaUI](https://www.nuget.org/packages/Ab4d.SharpEngine.AvaloniaUI), [Ab4d.SharpEngine.WinUI](https://www.nuget.org/packages/Ab4d.SharpEngine.WinUI), [Ab4d.SharpEngine.WinForms](https://www.nuget.org/packages/Ab4d.SharpEngine.WinForms)) that defines the `SharpEngineSceneView` class. This class hides the differences between those platforms under the hood and provides the same API for all platforms. The class also initializes the engine by creating the `VulkanDevice`. The two main properties that `SharpEngineSceneView` provides are `Scene` and `SceneView`. The `Scene` is used to define the scene by adding the SceneNodes to the `Scene.RootNode` (similar as Viewport3D.Children in WPF) and adding lights to `Scene.Lights` collection. The `SceneView` defines the view of the scene and provides a camera that is set to the `SceneView.Camera` property. When working with `SharpEngineSceneView`, then **100% of the code** to show 3D graphics **can be the same for WPF, Avalonia, WinUI and WinForms**. Other platforms and UI frameworks require some special setup code that is different for each platform. But from there on, the code is the same regardless of the platform. See samples for more info.

Some other differences:

`BoxVisual3D`, `SphereVisual3D` and other objects derived from `BaseVisual3D` are defined in `Ab4d.SharpEngine.SceneNodes` namespace
(for example `BoxVisual3D` => `BoxModelNode`; `SphereVisual3D` => `SphereModelNode`).

`GeometryModel3D` with custom `MeshGeometry3D` from WPF 3D is now defined by `MeshModelNode` and `StandardMesh` (see [MeshModelNodeSample](https://github.com/ab4d/Ab4d.SharpEngine.Samples/blob/main/Ab4d.SharpEngine.Samples.Common/StandardModels/MeshModelNodeSample.cs).
Meshes for standard objects (box, sphere, cone, etc) can be created by using `Meshes.MeshFactory`.

Cameras and lights are almost the same as in Ab3d.PowerToys. The cameras are `TargetPositionCamera`, `FirstPersonCamera`, `FreeCamera` and `MatrixCamera` with the same properties as in Ab3d.PowerToys. Also lights (`DirectionalLight`, `PointLight`, `Spotlight`, `AmbientLight` are the same as in Ab3d.PowerToys.

`MouseCameraController` for WPF, Avalonia or WinUI is almost the same as in Ab3d.PowerToys.
For Android you can use `AndroidCameraController`.
For other platforms you can use `ManualMouseCameraController` and then call the `ProcessMouseDown`, `ProcessMouseUp` and `ProcessMouseMove methods` - see samples.

Just as Ab3d.PowerToys, the Ab3d.SharpEngine also defines the `ReaderObj` for reading 3D models from obj files. Also, to import models from other files, use the `Ab4d.SharpEngine.Assimp` library (similar to `Ab3d.PowerToys.Assimp` and `Ab3d.DXEngine.Assimp`).

To provide cross-platform reading of texture files (2D bitmap) the Ab4d.SharpEngine uses the `IBitmapIO` interface that provides the common bitmap IO operations. Then, there are platform specific implementations, for example `WpfBitmapIO`, `WinUIBitmapIO`, `SystemDrawingBitmapIO`, `SkiaSharpBitmapIO`. There is also a build-in `PngBitmapIO` that can read or write png images and does not require any third-party or platform-specific implementation.

Ab4d.SharpEngine uses `float` as its main value type and `System.Numerics` for base math objects and functions. This means that you need to convert all `double` values to `float` values. Also, `Point3D` and `Vector3D` structs need to be converted to `Vector3`.

In my opinion, if you already have a complex application that is built by using Ab3d.PowerToys and Ab3d.DXEngine and you are not required to use any other platform except Windows, then it is not worth converting that application to Ab4d.SharpEngine. But if you need to create a simpler version of the application that would also work on mobile devices, then Ab4d.SharpEngine gives you a great opportunity to port only a part of the code. Also, if you are starting to create an application that requires 3D graphics, then it is probably better to start with Ab4d.SharpEngine.

### Advantages of Ab3d.DXEngine with Ab3d.PowerToys

- Ab3d.DXEngine and Ab3d.PowerToys are very mature products that are tested and proven in the "field" by many customers.
- Ab3d.DXEngine supports multi-threading and currently provides faster 3D rendering in many use cases.
- Ab3d.DXEngine and Ab3d.PowerToys can run on older .Net versions including .Net framework 4.8.

Those two libraries provide more features and come with more samples that can be used as code templates for your needs.
The following is a list of major features from Ab3d.DXEngine and Ab3d.PowerToys that are missing in Ab4d.SharpEngine (v3.0; this is not the full list):
- Effects: PhysicallyBasedRendering, XRay, multi-map material, environment map and face color effect
- Shadows
- Post-processing


### Advantages of Ab4d.SharpEngine

- Ab4d.SharpEngine can run on multiple platforms. You can start writing code for Windows and later simply add support for Linux, macOS, Android and iOS. Or port just a smaller part of the application to other platforms.
- Ab4d.SharpEngine uses Vulkan API that is the most advanced graphics API that is actively developed and gets new features as new versions of graphics cards are released. This provides options to support all current and future graphics features (for example Ray tracing - not possible with DirectX 11).
- Ab4d.SharpEngine was built from the ground up and therefore has a very clean and easy-to-use programming API. For example, there is only a single set of 3D models (SceneNodes, Camera, Lights). When using Ab3d.DXEngine and Ab3d.PowerToys, the API is not very nice in all the cases. The Ab3d.PowerToy was built on top of WPF 3D objects that are not very extendable so some compromises were needed (for example, cameras are derived from FrameworkElement and not from Camera). Also, Ab3d.DXEngine converts all WPF 3D and Ab3d.PowerToys objects into its own objects so the application has 2 versions of each object. In other cases, some tricks must be used to provide Ab3d.DXEngine features to Ab3d.PowerToys and WPF 3D objects (for example using SetDXAttribute).
- Working with WPF objects is very slow (accessing DependencyProperties has a lot of overhead). Also, Ab3d.DXEngine needs to convert all WPF objects into its own objects. Working with objects in Ab4d.SharpEngine is much faster.
- Vulkan is a significantly faster graphics API than DirectX 11. Though the Ab4d.SharpEngine does not use all the fastest algorithms yet (no multi-threading), in the future the engine will be significantly faster than Ab3d.DXEngine.
- Ab4d.SharpEngine is built on top of .NET 6 and that provides many performance benefits because of using System.Numerics, Span and other improved .NET features.
- In the future Ab4d.SharpEngine will provide more functionality than Ab3d.DXEngine with Ab3d.PowerToys.

NOTE:
Ab3d.PowerToys and Ab3d.DXEngine will still be actively developed, will get new releases and features and will have full support in the future!



## Quick building instructions for macOS and iOS

The following projects can be started on **macOS**:
- Ab4d.SharpEngine.Samples.AvaloniaUI
- Ab4d.SharpEngine.Samples.AvaloniaUI.CrossPlatform/Desktop
- Ab4d.SharpEngine.Samples.Maui

The following projects can be started on **iOS**:
- Ab4d.SharpEngine.Samples.AvaloniaUI.CrossPlatform/iOS
- Ab4d.SharpEngine.Samples.Maui
 
The following changes are required to use Ab4d.SharpEngine on macOS and iOS:

- Add libMoltenVK.dylib from the Vulkan SDK to the project so that the library can be loaded at runtime. Note that there are different builds for iOS and for macOS / MacCatalyst. The libMoltenVK.dylib is also available in the libs folder in this repository. For example, in the Ab4d.SharpEngine.Samples.AvaloniaUI.CrossPlatform solution the csproj file for the Desktop project has the following:
  ```
  <!-- MacOS and iOS require libMoltenVK.dylib to be able to use SharpEngine with Vulkan -->
  <ItemGroup Condition="$([MSBuild]::IsOSPlatform('macos'))">
    <Content Include="../../lib/MoltenVK/macos-arm64_x86_64/libMoltenVK.dylib" PublishFolderType="Assembly">
      <Link>libMoltenVK.dylib</Link>
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>    
  ```
  The following is used for iOS project:
  ```
  <!-- iOS requires libMoltenVK.dylib to be able to use SharpEngine with Vulkan -->
  <!-- When starting iOS simulator, use the dylib from ios-arm64_x86_64-simulator folder -->
  <ItemGroup>
    <Content Include="../../lib/MoltenVK/ios-arm64/libMoltenVK.dylib">
      <Link>libMoltenVK.dylib</Link>
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>  
  ```
  Another option for macOS (not for iOS) is to install the Vulkan SDK to the computer (https://vulkan.lunarg.com/sdk/home#mac). In this case the Ab4d.SharpEngine should be able to find the libMoltenVK.dylib from the default location and you do not need to add it to the csproj. But in this case you will not be able to distribute the app to another computer that does not have Vulkan SDK installed.

- For MAUI apps, the 3D scene that is rendered by Ab4d.SharpEngine is shown by using SKCanvasView. To use that control, add a reference to SkiaSharp.Views.Maui.Controls NuGet package. Then add ".UseSkiaSharp()" to the builder setup in the MauiProgram.cs file.
  
- To run the app on iOS (not required for the iOS simulator), the application needs to have a provisioning profile set. One option is to follow the instructions on the following page: [Create a provisioning profile](https://learn.microsoft.com/en-us/dotnet/maui/ios/capabilities?view=net-maui-8.0&tabs=vs#create-a-provisioning-profile). Another option is to open the project in the Rider IDE, then right-click on the project and select "Open in Xcode". Rider will create the Xcode project file and open it in Xcode. There you can click on the project file and in the "Certificates, Identifiers & Profiles" tab create an ad-hoc provisioning profile (allow having up to 3 development apps installed at the same time). See more: [Create a development provisioning profile](https://developer.apple.com/help/account/manage-profiles/create-a-development-provisioning-profile/). Note that to create the provisioning profile, the ApplicationId (in csproj file) needs to be in the form of "com.companyName.appName" - this is then used as a Bundle Id.

- In iOS project make sure that the `ApplicationId` property is defined in the csproj file. Typically, a reverse-DNS format is used for this value, for example: com.company_name.app_name. Also, its value must be the same as the `CFBundleIdentifier` property in the Info.plist (in MAUI project this file is located in the Platforms/iOS folder). Also, the value for `SupportedOSPlatformVersion` in csproj must match the value of `MinimumOSVersion` in Info.plist.

- .Net 8 is required to use Ab4d.SharpEngine on iOS (because function pointers do not work with .Net 7 and previous .Net versions on iOS).

See detailed instructions below for more information.

## Step by step instructions to run the samples on macOS and iOS ##

**Starting on macOS**:
1. Download the latest version of dotnet - see https://dotnet.microsoft.com/en-us/download
2. Download SharpEngine samples from this repo and extract them to any folder.
3. Right click on a folder from the extracted files (for example, on Ab4d.SharpEngine.Samples.AvaloniaUI) and select "New Terminal at Folder"
4. Run the following in terminal (if there is only one project in the folder, then you can skip the ´--project´ parameter; replace the project name with some other project name if needed):
   ´dotnet run --project Ab4d.SharpEngine.Samples.AvaloniaUI.macos.csproj´
5. This will start the sample application with SharpEngine on macOS.

You can also easily start the project from **Visual Studio Code** or **Rider** from JetBrains.


**Starting on iOS**:
1. Install XCode
2. When XCode is started, install iOS platform support
3. Connect your iPhone by USB cable and then open the following menu in XCode: Window - Devices and Simulators - allow the connection on iPhone and check that you see something similar:
![XCode devices](doc/xcode-devices.png)
4. In terminal open the Ab4d.SharpEngine.Samples.AvaloniaUI.CrossPlatform/iOS folder
5. Run the following: `sudo dotnet workload install ios`
6. To compile and run the app, use the following command (replace the device name with the Identifier from the XCode devices - on the screen that is shown on the screenshot above right click on the Identifier text and select Copy):
   `dotnet build -t:Run -p:Configuration=Debug -r ios-arm64 /p:_DeviceName=xxxxxxxxxx-xxxxxxxxxxxxxxxxxxxx`
7. At that point you should get the following compiler error:
   `No valid iOS code signing keys found in keychain. You need to request a code signing certificate from https://developer.apple.com.`
8. To add the certificate, make sure that you are signed into XCode. Go to XCode - Settings - check the Accounts tab. If no account is listed, click + to add your account. To create a new Apple developer account, visit https://developer.apple.com (you can also create a free account).
9. If you are using Rider from JetBrains, then you can open the solution there. Then right click on the iOS project and select "Open with XCode". Then proceed to the step 17.
10. When using CLI or Visual Studio Code, you can create the provisioning profile with the certificate by using the xcsync (this tool came with .Net 9; see https://learn.microsoft.com/en-us/dotnet/maui/macios/xcsync?view=net-maui-9.0&tabs=cli). To install and use xcsync do the following:
11. Run the following (2025-05-15):
    ```
    dotnet tool install dotnet-xcsync -g --prerelease --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet9/nuget/v3/index.json
    ```
13. As instructed by the output, run the following:
	   ```
    cat << \EOF >> ~/.zprofile
	   # Add .NET Core SDK tools
	   export PATH="$PATH:/Users/abenedik/.dotnet/tools"
	   EOF
    ```
14. Then run: `zsh -l`
15. After that you should be able to run:
	   `xcsync generate  --target-framework-moniker net9.0-ios --open`
16. This generates the xcode project for the first time - for example the following output will be shown:
	   `Generated Xcode project at 'obj/Debug/net9.0-ios/iossimulator-arm64/xcsync'`
17. To update the xcode project later, run the following:
	   `xcsync sync`
18. The XCode will open with the specified project. On the left click on the project name. Then click on the "Signing & Capabilities":
![XCode provisioning - no certificate](doc/xcode-provisioning1.png)
19. In the Team dropdown select your provisioning team. After a few seconds the Signing Certificate should change to your certificate:
![XCode provisioning - valid certificate](doc/xcode-provisioning2.png)
20. If you are using Rider, then you can start the iOS project (before that select the iOS device from the dropdown on the top line left to the project name).
21. If you are using CLI or Visual Studio Code, do the following:
22. Extract the CodesignKey by executing the following command in the terminal:
    `security find-identity -p codesigning -v`
23. This prints the following (actual values are replaced by x and X):
    `xxxxxxxxxxxxxxxxxxxxxx "Apple Development: axxxxxxk@xxxx.com (XXXXXXX)"`
24. Open AvaloniaUI.CrossPlatform.iOS.csproj and add the following (copy the text from the terminal):
    ```
    <PropertyGroup>
        <CodesignKey>Aple Development: axxxxxxk@xxxx.com (XXXXXXX)</CodesignKey>
        <CodesignProvision>Automatic</CodesignProvision>
    </PropertyGroup>
    ```
25. Now you should be able to run the app by using the following command (replace the device name with your Identity):
`dotnet build -t:Run -p:Configuration=Debug -r ios-arm64 /p:_DeviceName=xxxxxxxxxx-xxxxxxxxxxxxxxxxxxxx`
26. When the app is deployed to iPhone, you will get a warning that the developer is not trusted. To allow the developer on the phone go to Settings > General > VPN & Device Management. In the Enterprise App section, tap the name of the app developer. Tap "Trust [developer name]" to continue. In iOS 18, iPadOS 18, and visionOS 2 and later, tap "Allow & Restart" to proceed with establishing trust. Then start the app again.

To get **better error diagnostics when running dotnet build**, add the following parameters: `-v diag /p:WarningLevel=4 --tl:off`

## Troubleshooting

If you get build errors on Windows (for example 'project.assets.json' not found) than maybe the total path length is larger than max path (260 chars). Move the samples solution to a folder with a shorter path and try compiling again.

The latest version of branches that start with "version/" may not compile with the latest published NuGet package and require the latest development version of the engine. If you need a feature from that branch, you can contact support to get the pre-release version.

When using `dotnet build` command and you get an error message that you do not understand, it is possible to get additional error details by adding the following parameters: `-v diag /p:WarningLevel=4 --tl:off`.

Some Intel graphics cards may not work with shared texture in WPF's SharpEngineSceneView control (WritableBitmap is used instead, but this is slower).

To enable Vulkan validation, install the Vulkan SDK from: https://vulkan.lunarg.com/
When Vulkan validation is installed and enabled by the SharpEngine (EnableStandardValidation is set to true when creating the engine),
then each Vulkan call is checked by the validation error and this can give much better error reports
(all Vulkan validation reports are logged at Warn log level).

To enable logging use the following code:
```
Ab4d.SharpEngine.Utilities.Log.LogLevel = LogLevels.Warn;
```


Then you have multiple options to display or save log messages:
```
// Write log to file
Ab4d.SharpEngine.Utilities.Log.LogFileName = @"c:\SharpEngine.log";
  
// Write log messages to the output window (for example, Visual Studio Debug window) 
// Ab4d.SharpEngine.Utilities.Log.IsLoggingToDebugOutput = true; 
  
// Write to local StringBuilder
private System.Text.StringBuilder _logStringBuilder;
Ab4d.SharpEngine.Utilities.Log.AddLogListener((logLevel, message) => _logStringBuilder.AppendLine(message));
```

To get simplified log messages (without timestamp, thread ID and some other details) you can use:
```
Ab4d.SharpEngine.Utilities.Log.WriteSimplifiedLogMessage = true;
```


## Change log
See https://www.ab4d.com/SharpEngine-history.aspx.


## Plans for later versions

- PhysicallyBasedRendering effect
- Multi-threaded rendering
- Post-processing
- Simplified creation of custom effects
- Shadows
- Add support for WebGL so that the SharpEngine can work with Blazor WebAssembly in a web browser (first alpha version is planned for the end of Q2 2025).


### Notice:
Ab4d.SharpEngine.glTF library uses source code from glTF2Loader with PR from ZingBallyhoo (https://github.com/KhronosGroup/glTF-CSharp-Loader/pull/51).
glTF2Loader library is published under the following MIT license:

This license is for the C# reference loader, not the rest of the repository.

Copyright (c) 2015, Matthew and Xueru McMullan All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.

Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
