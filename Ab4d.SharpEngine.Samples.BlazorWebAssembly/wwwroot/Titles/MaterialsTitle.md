# Materials

Usually, 3D objects defines Material and BackMaterial properties. The Material defines how the front triangles are rendered. The BackMaterial defines how the back side of the triangles are rendered. 

The orientation of triangles is defined by the order of the triangle positions. By default the counter-clockwise orientation of the positions defines the front side of the triangle.

The following materials for solid objects are available:
- **StandardMaterial** is a material that defines the standard properties to show the diffuse, specular and emissive material properties and also supports diffuse textures.
- **SolidColorMaterial** is a material that is not shaded by lighting and is always rendered exactly as specified by the diffuse material properties.
