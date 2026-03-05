# Lights

The following lights are available:
- **DirectionalLight** is a light that shines from infinity and has a direction vector set.
- **PointLight** is a light that emits light in all directions from a specified position.
- **SpotLight** is a light that is positioned at the specified position and emits light in a specified direction and with the specified cone.
- **AmbientLight** is a light that adds the specified color to all objects in the scene (as a light would be illuminating the scene from all directions).
- **CameraLight** is a directional light that has the same direction as the camera's LookDirection. It can be created by changing the camera's ShowCameraLight property. By default this property is set to Auto which creates the CameraLight when there are no other real lights in the scene (ambient light does not count as a real light).
