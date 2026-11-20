# Ab4d.SharpEngine.WebGL implementation details

The Ab4d.SharpEngine.WebGL does not yet have all the features of the Ab4d.SharpEngine version.

Namespace implementation status:
- **Animation**: 100% implemented :heavy_check_mark:
- **Cameras**: 100% implemented :heavy_check_mark:
- **Materials**:
    - StandardEffect - 100% implemented :heavy_check_mark:
    - ThickLineEffect - LineThickness, line patterns and world-space line thickness are implemented. Rendering connected lines (PolyLines) with mitered and beveled joints and rendering line caps (arrows and other shapes) is still not supported.
    - PixelEffect - 100% implemented :heavy_check_mark:
    - SpriteEffect - planned for next version :hourglass_flowing_sand:
    - VertexColorEffect - 100% implemented :heavy_check_mark:
    - VolumeRenderingEffect - supported later :two:
- **Lights**: 100% implemented :heavy_check_mark:
- **Materials**: 
    - StandardMaterial - 100% implemented :heavy_check_mark:
    - SolidColorMaterial - (using StandardEffect) - 100% implemented :heavy_check_mark:
    - LineMaterial - See comment with ThickLineEffect for more info.
    - PolyLineMaterial - See comment with ThickLineEffect for more info.
    - PositionColoredLineMaterial - supported later :two:
    - VertexColorMaterial - 100% implemented :heavy_check_mark:
    - PrimitiveIdMaterial -  planned for the next version :hourglass_flowing_sand:
    - DepthOnlyMaterial - supported later :two:
    - VolumeMaterial - supported later :two:
- **Meshes**: 100% implemented :heavy_check_mark:
- **OverlayPanels**: CameraAxisPanel planned for next version :hourglass_flowing_sand:
- **PostProcessing**:  planned for the next version :hourglass_flowing_sand:
- **SceneNodes**:  100% implemented :heavy_check_mark:
- **Transformations**: 100% implemented :heavy_check_mark:
- **Utilities**: implemented all except:
    - BezierCurve, BSpline - 100% implemented :heavy_check_mark:
    - BitmapTextCreator - 100% implemented :heavy_check_mark:
    - CameraController - 100% implemented :heavy_check_mark:
    - EdgeLinesFactory - 100% implemented :heavy_check_mark:
    - CameraUtils, LineUtils, MathUtils, MeshUtils, ModelUtils, TransformationUtils - 100% implemented :heavy_check_mark:
    - LineSelectorData (used for line selection) - 100% implemented :heavy_check_mark:
    - MeshBooleanOperations - 100% implemented :heavy_check_mark:
    - MeshOctree - 100% implemented :heavy_check_mark:
    - MeshTrianglesSorter - 100% implemented :heavy_check_mark:
    - ModelMover, ModelRotator and ModelScalar -  100% implemented :heavy_check_mark:
    - ObjImporter - 100% implemented :heavy_check_mark:
    - ObjExporter - planned for the next version :hourglass_flowing_sand:
    - StlImporter - 100% implemented :heavy_check_mark:
    - StlExporter - planned for the next version :hourglass_flowing_sand:
    - glTFImporter - 100% implemented :heavy_check_mark:
    - TextureLoader, TextureFactory - 100% implemented :heavy_check_mark:
    - Triangulator - 100% implemented :heavy_check_mark:
    - TrueTypeFontLoader, VectorFontFactory - 100% implemented :heavy_check_mark:
    - SpriteBatch - planned for the next version :hourglass_flowing_sand:
   
Other not implemented features:
- Super-sampling (planned for later)
