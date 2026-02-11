using Ab4d.Assimp;
using Ab4d.SharpEngine.Assimp;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.glTF;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Openize.Drako;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Ab4d.SharpEngine.Samples.Common.Importers;

public class GltfImporterExporterSample : CommonSample
{
    public override string Title => "glTF 2 Importer and Exporter";

    private string _subtitle = "glTF 2 file importer and exporter are defined in the Ab4d.SharpEngine.glTF NuGet package.\nThey can import from glTF 2 files (.gltf or .glb) and export the current scene to that file format.\nTo export 3D scene for any other example, open Diagnostics window and select 'Export Scene to glTF'.\n\nThis sample also used a third-party Openize.Drako library to read Draco compressed glTF files.";
    public override string? Subtitle => _subtitle;

    private readonly string _initialFileName = "Resources\\Models\\voyager.gltf";

    private ICommonSampleUIElement? _textBoxElement;
    private MultiLineNode? _objectLinesNode;
    private GroupNode? _importedModelNodes;

    private string? _importedFileName;
    private string? _exportFileName;
    private ICommonSampleUIElement? _exportFileNameTextBox;
    private ICommonSampleUIElement? _exportSuccessfulLabel;

    private EdgeLinesFactory? _edgeLinesFactory;

    private Vector2? _savedAxisPanelPosition;

    private bool _isFullLoggingEnabled;

    private enum ViewTypes
    {
        SolidObjectsOnly = 0,
        SolidObjectWithEdgeLines = 1,
        SolidObjectWithWireframe = 2
    }
    
    private enum ExportType
    {
        GltfWithBin = 0,
        GltfWithEmbeddedData = 1,
        BinaryGlb = 2
    }

    private ViewTypes _currentViewType = ViewTypes.SolidObjectsOnly;
    private ExportType _currentExportType = ExportType.BinaryGlb;


    public GltfImporterExporterSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 120;
            targetPositionCamera.Attitude = -30;
        }

        string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _initialFileName);
        ImportFile(fileName);

        ShowCameraAxisPanel = true;
    }

    /// <inheritdoc />
    protected override void OnDisposed()
    {
        if (_savedAxisPanelPosition != null && CameraAxisPanel != null)
            CameraAxisPanel.Position = _savedAxisPanelPosition.Value;

        base.OnDisposed();
    }

    protected void ImportFile(string? fileName)
    {
        _ = ImportFileAsync(fileName);
    }

    protected async Task ImportFileAsync(string? fileName)
    {
        if (Scene == null || fileName == null)
            return;

        Scene.RootNode.Clear();
        _importedFileName = null;

        string fileExtension = System.IO.Path.GetExtension(fileName);
        if (!fileExtension.Equals(".gltf", StringComparison.OrdinalIgnoreCase) &&
            !fileExtension.Equals(".glb", StringComparison.OrdinalIgnoreCase))
        {
            ShowErrorMessage("glTFImporter can import only from .gltf and .glb files and does not support file extension " + fileExtension);
            return;
        }

        if (!System.IO.Path.IsPathRooted(fileName))
            fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

        fileName = FileUtils.FixDirectorySeparator(fileName);
        
        if (!File.Exists(fileName))
        {
            ShowErrorMessage("File does not exist:\n" + fileName);
            return;
        }


        ClearErrorMessage();

        if (_currentViewType == ViewTypes.SolidObjectWithEdgeLines)
        {
            var fileInfo = new FileInfo(fileName);
            if (fileInfo.Length > 5000000) // > 5 MB
            {
                // Switch to SolidObjectsOnly because analysis of large models that is required to get the edge lines can take very long time
                _currentViewType = ViewTypes.SolidObjectsOnly;
                ShowErrorMessage("Switching view to SolidObjectsOnly because analysis of large models that is required to get the edge lines can take very long time", showTimeMs: 3000);
            }
        }


        // FixDirectorySeparator method returns file path with correctly sets backslash or slash as directory separator based on the current OS.
        fileName = Ab4d.SharpEngine.Utilities.FileUtils.FixDirectorySeparator(fileName);


        // Create a new glTFImporter
        // Using the platforms specific BitmapIO (this.BitmapIO). If this is not provided, then PngBitmapIO is used but this can read only png files.
        // When passing a GpuDevice to the constructor, then the glTFImporter can immediately create the texture images.
        // So in case any error appears when reading textures, this is thrown here (and can be caught in the catch below).
        // If GpuDevice is not provide in the constructor then the textures will be created when the objects are added to the Scene.
        var glTfImporter = new glTFImporter(this.BitmapIO, Scene.GpuDevice);

        // We could also create the glTFImporter by using a custom imageReader and by providing a VulkanDevice (this immediately creates the textures):
        //var glTfImporter = new glTFImporter(imageReader: customBitmapIO, gpuDevice: Scene.GpuDevice);

        // Setup logger that will display log messages in Visual Studio's Output window
        // When "Enable full logging" CheckBox is checked, then we show full logging; otherwise only warnings and errors are shown
        glTfImporter.LogInfoMessages = _isFullLoggingEnabled;
        glTfImporter.LoggerCallback = (logLevel, message) => System.Diagnostics.Debug.WriteLine($"glTfImporter: {logLevel} {message}");
        
        
        // To support Draco compressed meshes we need to set the DracoMeshReaderFactory.
        // The MyDracoReader class is defined below and uses Openize.Drako library to read Draco compressed meshes.Add commentMore actions
        glTfImporter.DracoMeshReaderFactory = (dracoFileBytes) => new MyDracoReader(dracoFileBytes);

        try
        {
            // To import file from stream use the following code:
            //using (var fs = System.IO.File.OpenRead(fileName))
            //    _importedModelNodes = _assimpImporter.Import(fs, formatHint: System.IO.Path.GetExtension(fileName));

            _importedModelNodes = await glTfImporter.ImportAsync(fileName);
            //_importedModelNodes = glTfImporter.Import(fileName);
            
            // To see the hierarchy of the imported models, execute the following in the Visual Studio's Immediate Window (first check that _importedModelNodes is GroupNode and not ModelMeshNode):
            //_importedModelNodes.DumpHierarchy();
        }
        catch (Exception ex)
        {
            ShowErrorMessage("Error importing file:\n" + ex.Message);
            return;
        }

        if (_importedModelNodes != null)
        {
            Scene.RootNode.Add(_importedModelNodes);


            if (_objectLinesNode == null)
            {
                var lineMaterial = new LineMaterial(Color3.Black, 1)
                {
                    DepthBias = 0.005f
                };

                _objectLinesNode = new MultiLineNode(isLineStrip: false, lineMaterial, "ObjectLines");
            }

            // Add _objectLinesNode to the scene because before all the children of RootNode were cleared
            Scene.RootNode.Add(_objectLinesNode);

            UpdateShownLines();


            if (_importedModelNodes.WorldBoundingBox.IsUndefined)
                _importedModelNodes.Update();

            if (targetPositionCamera != null && !_importedModelNodes.WorldBoundingBox.IsUndefined)
            {
                targetPositionCamera.TargetPosition = _importedModelNodes.WorldBoundingBox.GetCenterPosition();
                targetPositionCamera.Distance = _importedModelNodes.WorldBoundingBox.GetDiagonalLength() * 1.5f;
            }

            _importedFileName = fileName;
            UpdateExportFileName();
        }
    }

    private void UpdateShownLines()
    {
        if (_importedModelNodes == null || _objectLinesNode == null)
            return; // no model imported

        if (_currentViewType == ViewTypes.SolidObjectsOnly)
        {
            _objectLinesNode.Visibility = SceneNodeVisibility.Hidden;
            return;
        }


        if (_currentViewType == ViewTypes.SolidObjectWithEdgeLines)
        {
            // Reuse the instance of EdgeLinesFactory
            // This can reuse the lists and array that are internally used by the EdgeLinesFactory
            // See comments in Lines/EdgeLinesSample.cs for more info about edge lines generation.
            _edgeLinesFactory = new EdgeLinesFactory();
            var edgeLinePositions = _edgeLinesFactory.CreateEdgeLines(_importedModelNodes, 15);

            // Because for complex files it may take some time to calculate edge lines, 
            // we can use the following code to save the edge lines so next time we can load the data:
            //if (_importedFileName != null)
            //{
            //    string edgeLinesFileName = System.IO.Path.ChangeExtension(_importedFileName, ".edgelines");

            //    // Save edge lines:
            //    using (var stream = File.Open(edgeLinesFileName, FileMode.Create))
            //    {
            //        using (var writer = new BinaryWriter(stream))
            //        {
            //            writer.Write(edgeLinePositions.Count);

            //            foreach (var edgeLinePosition in edgeLinePositions)
            //            {
            //                writer.Write(edgeLinePosition.X);
            //                writer.Write(edgeLinePosition.Y);
            //                writer.Write(edgeLinePosition.Z);
            //            }
            //        }
            //    }
                
            //    // Read edge lines:
            //    using (var stream = File.Open(edgeLinesFileName, FileMode.Open))
            //    {
            //        using (var reader = new BinaryReader(stream))
            //        {
            //            int count = reader.ReadInt32();

            //            var edgeLines = new Vector3[count];

            //            for (int i = 0; i < count; i++)
            //                edgeLines[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            //        }
            //    }
            //}

            _objectLinesNode.Positions = edgeLinePositions.ToArray();
            _objectLinesNode.Visibility = SceneNodeVisibility.Visible;
        }
        else if (_currentViewType == ViewTypes.SolidObjectWithWireframe)
        {
            var wireframePositions = LineUtils.GetWireframeLinePositions(_importedModelNodes, removedDuplicateLines: false); // remove duplicates can take some time for bigger models

            _objectLinesNode.Positions = wireframePositions;
            _objectLinesNode.Visibility = SceneNodeVisibility.Visible;
        }
    }

    private void UpdateExportFileName()
    {
        string fileExtension = _currentExportType == ExportType.BinaryGlb ? ".glb" : ".gltf";

        if (string.IsNullOrEmpty(_importedFileName))
            _exportFileName = "Export";
        else
            _exportFileName = System.IO.Path.GetFileNameWithoutExtension(_importedFileName);

        _exportFileName += fileExtension;

        _exportFileNameTextBox?.SetText(_exportFileName);
    }


    private void ExportScene()
    {
        if (_importedFileName == null || _exportFileName == null || Scene == null)
            return;

        _exportSuccessfulLabel?.SetIsVisible(false);

        
        string fullExportFileName = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_importedFileName)!, _exportFileName);

        // First create an instance of glTFExporter
        var glTfExporter = new glTFExporter();

        // Then add Scene to the exporter
        glTfExporter.AddScene(Scene);

        // We could also export only a selected SceneNode (can be a GroupNode)
        //glTfExporter.AddSceneNode(sceneNode);

        // Setup logger that will display log messages in Visual Studio's Output window
        // When "Enable full logging" CheckBox is checked, then we show full logging; otherwise only warnings and errors are shown
        glTfExporter.LogInfoMessages = _isFullLoggingEnabled;
        glTfExporter.LoggerCallback = (logLevel, message) => System.Diagnostics.Debug.WriteLine($"glTfExporter: {logLevel} {message}");

        try
        {
            switch (_currentExportType)
            {
                case ExportType.GltfWithBin:
                    // Export to .gltf file with json format + .bin file with binary format
                    // When no exportResourceFunc parameter is used, then the .bin file and textures are created in the same folder as the fullExportFileName.
                    // Use exportResourceFunc parameter to export to some other folder.
                    glTfExporter.Export(fullExportFileName);

                    // To export to stream use:
                    //glTfExporter.Export(gltfFileStream, (filename, data) =>
                    //{
                    //    // export data with fileName to some custom stream
                    //});
                    break;

                case ExportType.GltfWithEmbeddedData:
                    glTfExporter.ExportEmbedded(fullExportFileName);
                    
                    // To export to stream use:
                    //glTfExporter.ExportEmbedded(gltfFileStream);
                    break;

                case ExportType.BinaryGlb:
                    glTfExporter.ExportBinary(fullExportFileName);

                    // To export to stream use:
                    //glTfExporter.ExportBinary(gltfFileStream);
                    break;
            }

            _exportSuccessfulLabel?.SetText("Exported to:\n" + fullExportFileName);
            _exportSuccessfulLabel?.SetIsVisible(true);
        }
        catch (Exception ex)
        {
            ShowErrorMessage("Error exporting:\n" + ex.Message);
        }
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right, isVertical: true);

        ui.CreateLabel("View", isHeader: true);
        ui.CreateRadioButtons(new string[] { "Solid objects only", "Solid + EdgeLines", "Solid + Wireframe" }, (selectedIndex, selectedText) =>
        {
            _currentViewType = (ViewTypes)selectedIndex;
            UpdateShownLines();
        }, selectedItemIndex: (int)_currentViewType);

        ui.AddSeparator();
        ui.CreateLabel("Export", isHeader: true);
        
        ui.CreateLabel("Export type:");
        ui.CreateRadioButtons(new string[] { "json gltf + bin", "json gltf with embedded data", "binary glb" }, 
            (itemIndex, itemText) =>
            {
                _currentExportType = (ExportType)itemIndex;
                UpdateExportFileName();
            }, 
            selectedItemIndex: (int)_currentExportType);

        ui.AddSeparator();
        ui.CreateLabel("Export file name:");
        _exportFileNameTextBox = ui.CreateTextBox(width: 0, initialText: _exportFileName, textChangedAction: (fileName) => _exportFileName = fileName);

        ui.AddSeparator();
        ui.CreateButton("Export scene", ExportScene);

        ui.AddSeparator();
        ui.AddSeparator();
        ui.CreateCheckBox("Enable full logging (see VS Output)", _isFullLoggingEnabled, isChecked => _isFullLoggingEnabled = isChecked);


        ui.AddSeparator();
        _exportSuccessfulLabel = ui.CreateLabel("Exported to:", width: 230).SetColor(Colors.Green).SetIsVisible(false);


        // Try to register for file drag-and-drop
        bool isDragAndDropSupported = ui.RegisterFileDropped(".*", ImportFile);

        if (isDragAndDropSupported)
        {
            _subtitle = "Drag and drop .gltf or .glb file here to open it.\n\n" + _subtitle;
        }
        else
        {
            // If drag and drop is not supported, then show TextBox so user can enter file name to import

            ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Left, isVertical: false);

            ui.CreateLabel("FileName:");
            _textBoxElement = ui.CreateTextBox(width: 400, initialText: FileUtils.FixDirectorySeparator(_initialFileName));

            ui.CreateButton("Load", () =>
            {
                ImportFile(_textBoxElement.GetText());
            });

            // When File name TextBox is shown in the bottom left corner, then we need to lift the CameraAxisPanel above it
            if (CameraAxisPanel != null)
            {
                _savedAxisPanelPosition = CameraAxisPanel.Position;
                CameraAxisPanel.Position = new Vector2(10, 80); // CameraAxisPanel is aligned to BottomLeft, so we only need to increase the y position from 10 to 80
            }
        }
    }
    

    // Custom DracoMeshReader that uses Openize.Drako library to read Draco compressed meshes.
    class MyDracoReader : DracoMeshReader
    {
        private readonly DracoMesh _dracoMesh;
        
        public MyDracoReader(byte[] dracoFileBytes)
        {
            _dracoMesh = (DracoMesh)Draco.Decode(dracoFileBytes);
        }
        
        public override Vector3[] GetPositions()
        {
            var attribute = _dracoMesh.GetNamedAttribute(Openize.Drako.AttributeType.Position);

            // IndicesMap define the mapping from the original indices to the new indices in the attribute buffer,
            // for example for box.gltf, the IndicesMap is [2, 2, 2, 0, 0, 0, ...
            // and this means that the first 3 positions in the final positions array are the same as the 2nd position in the attribute (compressed buffer)
            var positionsCount = attribute.IndicesMap.Length;
            var positions = new Vector3[positionsCount];
            for (int i = 0; i < positionsCount; i++)
            {
                int index = attribute.IndicesMap[i];
                var pos = attribute.GetValueAsVector3(index);
                positions[i] = new Vector3(pos.X, pos.Y, pos.Z);
            }
            
            return positions;
        }

        public override Vector3[]? GetNormals()
        {
            var attribute = _dracoMesh.GetNamedAttribute(Openize.Drako.AttributeType.Normal);
            
            if (attribute == null)
                return null;

            // See comment in GetPositions
            var normalsCount = attribute.IndicesMap.Length;
            var normals = new Vector3[normalsCount];
            for (int i = 0; i < normalsCount; i++)
            {
                int index = attribute.IndicesMap[i];
                var n = attribute.GetValueAsVector3(index);
                normals[i] = new Vector3(n.X, n.Y, n.Z);
            }
            
            return normals;
        }
        
        public override Vector4[]? GetTangents()
        {
            // Openize.Drako does not define Tangent attribute type.
            // When Tangent data is present, it stores that as Generic attribute.
            // In this cas we need to check that the DataType is FLOAT32 and ComponentsCount is 4 (Vector4)
            var attribute = _dracoMesh.GetNamedAttribute(AttributeType.Generic);

            if (attribute == null || attribute.ComponentsCount != 4 || attribute.DataType != DataType.FLOAT32)
                return null;
            
            var oneVector4 = new float[4];
            
            var tangentsCount = attribute.IndicesMap.Length;
            var tangents = new Vector4[tangentsCount];
        
            for (int i = 0; i < tangentsCount; i++)
            {
                int index = attribute.IndicesMap[i];
                attribute.GetValue(index, oneVector4);
                tangents[i] = new Vector4(oneVector4[0], oneVector4[1], oneVector4[2], oneVector4[3]);
            }
            
            return tangents;
        }
        
        public override Vector2[]? GetTextureCoordinates()
        {
            var attribute = _dracoMesh.GetNamedAttribute(AttributeType.TexCoord);

            if (attribute == null)
                return null;
            
            var oneUV = new float[2];
            
            var textureCoordinatesCount = attribute.IndicesMap.Length;
            var textureCoordinates = new Vector2[textureCoordinatesCount];
        
            for (int i = 0; i < textureCoordinatesCount; i++)
            {
                int index = attribute.IndicesMap[i];
                attribute.GetValue(index, oneUV);
                textureCoordinates[i] = new Vector2(oneUV[0], oneUV[1]);
            }
            
            return textureCoordinates;
        }
        
        public override int[]? GetTriangleIndices()
        {
            if (_dracoMesh.Indices == null || _dracoMesh.Indices.Count == 0)
                return null;
            
            var indicesCount = _dracoMesh.Indices.Count;
            var indices = new int[indicesCount];
            for (int i = 0; i < indicesCount; i++)
                indices[i] = _dracoMesh.Indices[i];

            return indices;
        }
    }        
}