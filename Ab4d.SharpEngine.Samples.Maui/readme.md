# Ab4d.SharpEngine.Samples.Maui

This solution shows how to create an MAUI app with Ab4d.SharpEngine that can run on Windows, macOS, Android and iOS (Linux is not supported by MAUI).

**IMPORTANT:**

Ab4d.SharpEngine requires support for `SKCanvasView`.

This can be enabled by first adding reference to `SkiaSharp.Views.Maui.Controls`. For example, add the following to csproj file:
```
<PackageReference Include="SkiaSharp.Views.Maui.Controls" Version="4.153.1" />
```

Then update the code in the `MauiProgram.cs` and add the following line to the builder setup `.UseSkiaSharp()`. 


**NOTE:**

This sample shows only a few features of the Ab4d.SharpEngine library. See the samples for Avalonia, WFP, WinUI or WinForms to see the sample for more features of the engine.

## Build for Android  
For **Android** add the following lines to the `Platforms/Android/AndroidManifest.xml`:
```
<!-- Require Vulkan 1.0 -->
<!--<uses-feature android:name="android.hardware.vulkan.version" android:version="0x400003" android:required="true"/>-->

<!-- Require Vulkan 1.1 -->
<uses-feature android:name="android.hardware.vulkan.version" android:version="0x401000" android:required="true"/>
```

## Build for macOS and iOS

To see how to build the app for **macOS** and **iOS**, see 
[Quick building instructions for macOS and iOS](https://github.com/ab4d/Ab4d.SharpEngine.Samples/blob/main/README.md#quick-building-instructions-for-macos-and-ios) or
[Step by step instructions to run the samples on macOS and iOS](https://github.com/ab4d/Ab4d.SharpEngine.Samples#step-by-step-instructions-to-run-the-samples-on-macos-and-ios)



## Add Vulkan validation layers to the project

**WINDOWS:**

To enable Vulkan validation layers for Windows, installed the Vulkan SDK from https://vulkan.lunarg.com/sdk/home and enabled them by starting the vkconfig-gui in the Bin folder.

**ANDROID:**

To enable Vulkan validation layers for Android download the precompiled Android validation layers from https://github.com/KhronosGroup/Vulkan-ValidationLayers/releases.
Extract the files. Then create NativeLibs folder (can be in the Resources folder) and add arm64-v8a subfolder (optionally you can also create armeabi-v7a, x86 and x86_64 subfolders).
Then copy the extracted libVkLayer_khronos_validation.so to the new folder(s).
Then add set that file's build action as AndroidNativeLibrary.


Then enabled validation layers in MainPage.xaml.cs by using:
_sharpEngineSceneView.CreateOptions.EnableStandardValidation = true;