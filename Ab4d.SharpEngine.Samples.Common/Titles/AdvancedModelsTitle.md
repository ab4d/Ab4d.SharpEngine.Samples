# Advanced 3D models

The samples in this section show some advanced 3D models.

The following are some of those objects:
- **InstancedMeshNode** is used to provide the best performance when rendering many instances. It can be used to render a single Mesh multiple times as instances where each instance can have its own color and its own world matrix (defines scale, rotation and translation).
- **Boolean operations** can be used to create new objects by using subtract, intersect or union boolean operations.
- **HeightMapSurfaceNode** shows a 3D height map. It can be combined with HeightMapWireframeNode and HeightMapContoursNode to add wireframe or contour lines.
- **3D tubes** can be used to show different 3D tubes.
- **Lathe** 3D objects are created by rotating a 2D shape around an axis.
- **Extruded** 3D objects are created by extruding a 2D shape into the thrid dimension.
- **Planar shadows** are special 3D objects that are collapsed onto a 2D plane and rendered as a solid color dark object to create an illusion of a shadow.
- **Sprites** can be rendered on top or below the 3D scene and can show 2D images or text that is not affected by the camera and lights.