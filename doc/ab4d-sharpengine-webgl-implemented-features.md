# Ab4d.SharpEngine.WebGL implementation details

The Ab4d.SharpEngine.WebGL does not yet have all the features of the Ab4d.SharpEngine version.

Namespace implementation status:
- **Animation**: 100% implemented :heavy_check_mark:
- **Cameras**: 100% implemented :heavy_check_mark:
- **Materials**:
    - StandardEffect - 100% implemented :heavy_check_mark:
    - ThickLineEffect - LineThickness, line patterns and line caps and hidden lines are not supported.   
      WebGL does not support thick lines or geometry shader so this requires a different approach (probably CPU based mesh generation). This will be supported after v1.0. Use TubeLineModelNode and TubePathModelNode with SolidColorMaterial for thick lines (here the line thickness in not in screen space values).
    - PixelEffect - planned for next version :hourglass_flowing_sand:
    - SpriteEffect - planned for next version :hourglass_flowing_sand:
    - VertexColorEffect - planned for next version :hourglass_flowing_sand:
    - VolumeRenderingEffect - supported later :two:
- **Lights**: 100% implemented :heavy_check_mark:
- **Materials**: 
    - StandardMaterial - 100% implemented :heavy_check_mark:
    - SolidColorMaterial - (using StandardEffect) - 100% implemented :heavy_check_mark:
    - LineMaterial - Rendering colored lines with 1px line thickness. See comment with ThickLineEffect for more info.
    - PolyLineMaterial - Polylines are rendered as multiple individual lines. Because line thickness is limited to 1px, no mitered and beveled joints are required.
    - PositionColoredLineMaterial - supported later :two:
    - VertexColorMaterial - planned for next version :hourglass_flowing_sand:
    - PrimitiveIdMaterial - planned after v1.1 :hourglass_flowing_sand:
    - DepthOnlyMaterial - supported later :two:
    - VolumeMaterial - supported later :two:
- **Meshes**: all supported except SubMesh (planned for next version) :hourglass_flowing_sand:
- **OverlayPanels**: CameraAxisPanel planned for next version :hourglass_flowing_sand:
- **PostProcessing**: planned after v1.1 :hourglass_flowing_sand:
- **SceneNodes**: all supported except: MultiMaterialModelNode and PixelsNode. All planned for next version :hourglass_flowing_sand:
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
    - ObjExporter - planned for next version :hourglass_flowing_sand:
    - StlImporter - 100% implemented :heavy_check_mark:
    - StlExporter - planned for next version :hourglass_flowing_sand:
    - glTFImporter - 100% implemented :heavy_check_mark:
    - TextureLoader, TextureFactory - 100% implemented :heavy_check_mark:
    - Triangulator - 100% implemented :heavy_check_mark:
    - TrueTypeFontLoader, VectorFontFactory - 100% implemented :heavy_check_mark:
    - SpriteBatch - planned for next version :hourglass_flowing_sand:
   
Other not implemented features:
- Super-sampling (planned for later)