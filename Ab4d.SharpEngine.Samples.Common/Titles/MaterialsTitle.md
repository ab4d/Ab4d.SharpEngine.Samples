# Materials

Usually, 3D objects defines Material and BackMaterial properties. The Material defines how the front triangles are rendered. The BackMaterial defines how the back side of the triangles are rendered. 

The orientation of triangles is defined by the order of the triangle positions. By default the counter-clockwise orientation of the positions defines the front side of the triangle.

The following materials for solid objects are available:
- **StandardMaterial** is a material that defines the standard properties to show the diffuse, specular and emissive material properties and also supports diffuse textures.
- **SolidColorMaterial** is a material that is not shaded by lighting and is always rendered exactly as specified by the diffuse material properties.
- **VertexColorMaterial** can be used to render 3D objects with specifying colors for each of its positions.

The common standard materials are defined in the **StandardMaterials**, for example: StandardMaterials.Green defines a StandardMaterial with a green diffuse color. It is possible to change the opacity by calling SetOpacity method or change specular color and power by calling SetSpecular method.

The following materials for 3D lines are available:
- **LineMaterial** is a material that can be used to render 3D lines.
- **PolyLineMaterial** is a material that can be used to render poly-lines.
- **PositionColoredLineMaterial** is a LineMaterial that contains additional properties that can be used to render 3D lines with different start and end colors.
