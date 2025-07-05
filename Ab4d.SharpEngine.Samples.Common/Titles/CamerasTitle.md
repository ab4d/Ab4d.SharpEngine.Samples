# Cameras

The following camera types are available:
- **TargetPositionCamera** TargetPositionCamera is a camera that looks at the specified position (TargetPosition) from the specified angle (Heading, Attitude, Bank) and distance (Distance property). The camera is rotated around the up axis in the current coordinate system.
- **FirstPersonCamera** is a camera that simulates the person's view of the world. The position is defined by the CameraPosition and the direction of the camera is defined by Heading, Attitude and Bank properties. 
- **FreeCamera** is a camera that is not defined by heading, attitude and bank angle. The camera is not limited to rotation around the up axis. Instead, the camera is defined by CameraPosition, TargetPosition and UpDirection.
- **MatrixCamera** is a simple camera that is defined by the View and Projection matrices.
- **TwoDimensionalCamera** can be used to easily show 2D graphics with Ab4d.SharpEngine. The source code for this camera is defined in the Ab4d.SharpEngine.Cameras project in the Cameras folder.
