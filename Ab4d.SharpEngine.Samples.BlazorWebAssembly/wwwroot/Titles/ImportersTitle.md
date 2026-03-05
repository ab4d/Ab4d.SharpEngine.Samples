# 3D model importers

Ab4d.SharpEngine includes a **ObjImporter** and **ObjExporter** that can be used to import 3D models from obj files and export 3D scene to obj files. Obj file is a very common and simple text based file format that usually comes with a separate mtl file that defines materials. Note that the obj file cannot store object hierarchies and transformation.

The Ab4d.SharpEngine also includes a **StlImporter** and **StlExporter** that can be used to import a 3D model from stl files and export a 3D object to stl files. Stl file is file format that is commonly used for 3D printing. It can store only triangle meshes and does not support materials or object hierarchies.

The **Ab4d.SharpEngine.glTF.Web library** (available as a separate NuGet package) can read 3D objects from glTF files. glTF is a standard file format for three-dimensional scenes and models and is defined by the KhronosGroup. The file format uses one of two possible file extensions: .gltf or .glb. Both .gltf and .glb files may reference external binary and texture resources. Alternatively, both formats may be self-contained by directly embedding binary data buffers.
