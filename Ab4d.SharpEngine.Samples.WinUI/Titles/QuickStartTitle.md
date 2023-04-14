# Quick Start

Quick start section shows how to use the **SharpEngineSceneView** control.

SharpEngineSceneView is a **WPF control** that can show SharpEngine's SceneView in a WPF application.
The SharpEngineSceneView creates the VulkanDevice, Scene and SceneView objects.

**Scene** is used to define the 3D objects (added to Scene.RootNode) and lights (added to Scene.Lights collection).
**SceneView** is a view of the Scene and can render the objects in the Scene. It provides a Camera and size of the view. Scene and SceneView objects are created in SharpEngineSceneView's constructor.

**VulkanDevice** object is created when the SharpEngineSceneView is initialized (OnLoaded) or when Initialize method is called.
It is also possible to call Initialize and pass an existing VulkanDevice as parameter. This is used to share resources.

**PresentationType** property defines how the rendered 3D scene is presented to the WPF. SharpEngineSceneView for WPF supports the following presentation types:
- **SharedTexture**: The rendered 3D scene will be shared with WPF composition engine so that
the rendered image will stay on the graphics card.
This allows composition of 3D scene with other WPF objects.
- **WriteableBitmap**: If SharedTexture mode is not possible, then WriteableBitmap presentation type is used.
In this mode, the rendered texture is copied to main memory into a WPF's WriteableBitmap.
This is much slower because of additional memory traffic.
- **OverlayTexture**: This mode is the fastest because the engine owns part of the screen and can show the rendered scene
independent of the main UI thread (no need to wait for the rendering to be completed).
A disadvantage of this mode is that the 3D scene cannot be composed with other WPF objects
(WPF objects cannot be rendered on top of 3D scene).