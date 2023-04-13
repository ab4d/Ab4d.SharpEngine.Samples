# Quick Start

Quick start section shows how to use the **SharpEngineSceneView** control.

SharpEngineSceneView is an **Avalonia control** that can show SharpEngine's SceneView in an Avalonia application.
The SharpEngineSceneView creates the VulkanDevice, Scene and SceneView objects.

**Scene** is used to define the 3D objects (added to Scene.RootNode) and lights (added to Scene.Lights collection).
**SceneView** is a view of the Scene and can render the objects in the Scene. It provides a Camera and size of the view. Scene and SceneView objects are created in SharpEngineSceneView's constructor.

**VulkanDevice** object is created when the SharpEngineSceneView is initialized (OnLoaded) or when Initialize method is called.
It is also possible to call Initialize and pass an existing VulkanDevice as parameter. This is used to share resources.

**PresentationType** property defines how the rendered 3D scene is presented to the Avalonia UI. SharpEngineSceneView for Avalonia supports the following presentation types:
- **SharedTexture**: The rendered 3D scene will be shared with Avalonia composition engine so that
the rendered image will stay on the graphics card.
This allows composition of 3D scene with other Avalonia UI controls.
- **WriteableBitmap**: If SharedTexture mode is not possible, then WriteableBitmap presentation type is used.
In this mode, the rendered texture is copied to main memory into a Avalonia's WriteableBitmap.
This is much slower because of additional memory traffic.
- **OverlayTexture**: This mode is currently not supported by SharpEngineSceneView for Avalonia.