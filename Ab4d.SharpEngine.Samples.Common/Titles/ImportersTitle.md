# 3D model importers

Ab3d.SharpEngine includes a **ReaderObj** that can be used to import 3D models from obj files. Obj file is a very common and simple text based file format that usually comes with a separate mtl file that defines materials. Note that the obj file cannot store object hierarchies.

To read 3D models from many other file formats, use the **Assimp importer**. Assimp (Open Asset Import Library) is an open source library that can read many file formats with 3D objects. It is compiled into a native library. Therefore the Ab4d.SharpEngine.Assimp library is required to use Assimp importer.

These samples come with precompiled Assimp libraries that are available in the lib/assimp-lib folder. When using the Assimp library you need to make sure that the correct native dll is present in the same folder as the app or that you set the path to the dll by providing it in the call to the AssimpLibrary.Instance.Initialize method.