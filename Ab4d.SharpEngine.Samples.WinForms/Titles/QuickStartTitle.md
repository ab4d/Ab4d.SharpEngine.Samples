# Quick Start

Quick start section shows how to use the **SharpEngineSceneView** control.

SharpEngineSceneView is a **WinForms control** that can show SharpEngine's SceneView in a WinForms application.
The SharpEngineSceneView creates the VulkanDevice, Scene and SceneView objects.

**Scene** is used to define the 3D objects (added to Scene.RootNode) and lights (added to Scene.Lights collection).
**SceneView** is a view of the Scene and can render the objects in the Scene. It provides a Camera and size of the view. Scene and SceneView objects are created in SharpEngineSceneView's constructor.

**VulkanDevice** object is created when the SharpEngineSceneView is initialized (OnLoaded) or when Initialize method is called.
It is also possible to call Initialize and pass an existing VulkanDevice as parameter. This is used to share resources.

**PresentationType** property defines how the rendered 3D scene is presented to the Avalonia UI. SharpEngineSceneView for Avalonia supports the following presentation types:
- **SharedTexture**: The 3D scene will be rendered to the Handle of the SharpEngineSceneView. To render other WinForms controls on top of SharpEngineSceneView, set the index of the SharpEngineSceneView as the last control in the Form.
- **WriteableBitmap**: In this mode, the rendered texture is copied to main memory into a WinForms's Bitmap. This is much slower because of additional memory traffic.
- **OverlayTexture**: This mode is currently not supported by SharpEngineSceneView for WinForms.