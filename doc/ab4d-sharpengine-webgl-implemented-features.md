# Ab4d.SharpEngine.WebGL implementation details

The Ab4d.SharpEngine.WebGL does not yet have all the features of the Ab4d.SharpEngine version.

Namespace implementation status:
- **Animation**: 100% implemented :heavy_check_mark:
- **Cameras**: 100% implemented :heavy_check_mark:
- **Materials**:
    - StandardEffect - 100% implemented :heavy_check_mark:
    - ThickLineEffect - LineThickness, line patterns and basic line caps and planned for the next version.   
      Until then, you can use TubeLineModelNode and TubePathModelNode with SolidColorMaterial for thick lines (the line thickness is not in screen-space values).
    - PixelEffect - planned after the next version :hourglass_flowing_sand:
    - SpriteEffect - planned after the next version :hourglass_flowing_sand:
    - VertexColorEffect - planned for next version :hourglass_flowing_sand:
    - VolumeRenderingEffect - supported later :two:
- **Lights**: 100% implemented :heavy_check_mark:
- **Materials**: 
    - StandardMaterial - 100% implemented :heavy_check_mark:
    - SolidColorMaterial - (using StandardEffect) - 100% implemented :heavy_check_mark:
    - LineMaterial - Rendering colored lines with 1px line thickness. See comment with ThickLineEffect for more info.
    - PolyLineMaterial - planned for the next version :hourglass_flowing_sand:
    - PositionColoredLineMaterial - supported later :two:
    - VertexColorMaterial - planned for next version :hourglass_flowing_sand:
    - PrimitiveIdMaterial - planned after the next version :hourglass_flowing_sand:
    - DepthOnlyMaterial - supported later :two:
    - VolumeMaterial - supported later :two:
- **Meshes**: all supported except SubMesh (planned for the next version) :hourglass_flowing_sand:
- **OverlayPanels**: CameraAxisPanel planned after the next version :hourglass_flowing_sand:
- **PostProcessing**: planned after the next version :hourglass_flowing_sand:
- **SceneNodes**: all supported except MultiMaterialModelNode and PixelsNode. All planned for the next version :hourglass_flowing_sand:
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
    - ModelMover, ModelRotator and ModelScalar - planned for next version :hourglass_flowing_sand:
    - ObjImporter - 100% implemented :heavy_check_mark:
    - ObjExporter - planned after the next version :hourglass_flowing_sand:
    - StlImporter - 100% implemented :heavy_check_mark:
    - StlExporter - planned after the next version :hourglass_flowing_sand:
    - glTFImporter - 100% implemented :heavy_check_mark:
    - TextureLoader, TextureFactory - 100% implemented :heavy_check_mark:
    - Triangulator - 100% implemented :heavy_check_mark:
    - TrueTypeFontLoader, VectorFontFactory - 100% implemented :heavy_check_mark:
    - SpriteBatch - planned after the next version :hourglass_flowing_sand:
   
Other not implemented features:
- Super-sampling (planned for later)
