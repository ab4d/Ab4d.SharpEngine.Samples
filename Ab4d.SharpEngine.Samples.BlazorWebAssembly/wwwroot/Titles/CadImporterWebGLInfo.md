# CAD files importer

By using **Ab4d.OpenCascade library** it is possible to import **3D objects from STEP** (.step and .stp) and **IGES** (.iges and .igs) files.
            
But because Ab4d.OpenCascade library uses a native third-part Open CASCADE library it can run only on Windows.

Therefore, to show 3D models from CAD files in a browser, you will need to use Ab4d.OpenCascade library or ([CadImporter app](https://github.com/ab4d/CadImporter))
to open the CAD models and then save them into glTF files that can be imported by Ab4d.SharpEngine.WebGL.

If you need any additional parts and exact edge lines data, then you will need to export that to your own file format and then import that into the Blazor app.

Screenshots of CadImporter:

![CadImporter for Ab4d.SharpEngine screenshot](Resources/CadImporter-for-SharpEngine-1.png)

![CadImporter for Ab4d.SharpEngine screenshot](Resources/CadImporter-for-SharpEngine-2.png)
