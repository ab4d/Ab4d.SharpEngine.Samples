using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.IfcLite;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using System.Diagnostics;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.Importers;

// This sample shows how to import 3D models from the ifc files.
//
// To do this the sample is using Ab4d.SharpEngine.IfcLite library that can use the native IFClite library from https://github.com/ab4d/ifc-lite.
// This is a forked library from the original IFClite from https://github.com/LTplus-AG/ifc-lite.
// The forked code adds additional functions that provide support for getting the mesh data directly from the native memory.
// If the native library from the original code is used, then only ImportAsJson method is supported that reads the ifc file
// and returns a json string with all the data for the 3D models.
//
// The compiled versions for Windows x64, macOS arm64 and Linux x64 are available in the lib/ifc_lite folder.

public class IfcImporterSample : CommonSample
{
    public override string Title => "IFC import can import BIM models from ifc files";

    private string _subtitle = "IfcImporter uses a native IFClite library to read the ifc file. See comments in the code for more info.";
    public override string? Subtitle => _subtitle;

    // Ifc4_SampleHouse.ifc file from https://github.com/youshengCode/IfcSampleFiles
    private readonly string _initialFileName = "Resources\\Models\\Ifc4_SampleHouse.ifc";

    private bool _showEdgeLines = true;
    private bool _useOptimizedGroupNode = true;
    private bool _loadAsJson = false;
    private bool _hitTesting = false;
    private string? _lastImportedFileName;

    private Stopwatch _loadStopwatch = new();
    private double _ifcLoadTime;
    private double _ifcObjectCreateTime;

    private ICommonSampleUIElement? _infoTextBox;
    private ICommonSampleUIElement? _textBoxElement;
    private MultiLineNode? _objectLinesNode;

    private GroupNode _importedGroupNode;
    private RayHitTestResult? _lastHitObject;

    private EdgeLinesFactory? _edgeLinesFactory;

    private Vector2? _savedAxisPanelPosition;

    public IfcImporterSample(ICommonSamplesContext context)
        : base(context)
    {
        InitializeIfcLiteLibrary();

        ZoomMode = CameraZoomMode.PointerPosition;
        RotateAroundPointerPosition = true;

        _importedGroupNode = new GroupNode("IfcObjects");
    }

    protected override void OnCreateScene(Scene scene)
    {
        // Use Z-up coordinate system
        scene.SetCoordinateSystem(CoordinateSystems.ZUpRightHanded);
        
        scene.SetAmbientLight(0.3f);

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 40;
            targetPositionCamera.Attitude = -10;
        }

        scene.RootNode.Add(_importedGroupNode);


        string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _initialFileName);
        ImportFile(fileName);

        ShowCameraAxisPanel = true;
    }
    
    /// <inheritdoc />
    protected override void OnDisposed()
    {
        if (_savedAxisPanelPosition != null && CameraAxisPanel != null)
            CameraAxisPanel.Position = _savedAxisPanelPosition.Value;

        // Revert to default coordinate system
        Scene?.SetCoordinateSystem(CoordinateSystems.YUpRightHanded);

        base.OnDisposed();
    }

    private void InitializeIfcLiteLibrary()
    {
        // Calling Initialize will try to load default ifl-light library.
        // If this does not succeed, then an exception is thrown.
        //IfcLiteLibrary.Instance.Initialize();

        // We have more options when using TryInitialize 
        var libraryFilePath = IfcLiteLibrary.Instance.GetDefaultLibraryPath();
        bool success = IfcLiteLibrary.Instance.TryInitialize(libraryFilePath);

        if (!success)
        {
            // When we are not able to initialize IfcLite library, the reason for failure is written to InitializationFailedReason property.
            ShowErrorMessage(IfcLiteLibrary.Instance.InitializationFailedReason!);
            return;
        }

        //
        // Success
        //

        // Show used IFClite native version
        _subtitle = _subtitle.Replace("IFClite library", $"IFClite library v{IfcLiteLibrary.Instance.GetIfcLiteVersionText()}");


        // Usually we want to use the native ifc-lite library that is updated so it has custom functions that 
        // can be used to read geometry mesh directly from the library's memory (see https://github.com/ab4d/ifc-lite)
        // If instead, an unchanged ifc-light library is used, then we can only use ImportAsJson method that returns json string.
        // In this case the IfcLiteLibrary.Instance.IsDirectMeshAccessSupported is false

        if (!IfcLiteLibrary.Instance.IsDirectMeshAccessSupported)
            ShowErrorMessage("IFClite library loaded but because this build does not include the\nadded function to directly access geometry mesh.\nUsing ImportAsJson method instead.");
    }

    protected void ImportFile(string? fileName)
    {
        if (!IfcLiteLibrary.Instance.IsInitialized || fileName == null)
            return;

        _importedGroupNode.Clear();
        _lastImportedFileName = null;

        _ifcLoadTime = 0;
        _ifcObjectCreateTime = 0;


        _loadStopwatch.Restart();

        // Create IfcImporter object and set import properties (here the values are set to the default options)
        var ifcImporter = new IfcImporter()
        {
            // Elements in the first batch (default: 20 for faster first triangles).
            InitialBatchSize = 20,

            // Elements in subsequent batches (default: 50).
            ThroughputBatchSize = 50,

            // When true (default) sort walls/slabs first so the most visible geometry arrives earliest.
            FastFirstBatch = false,

            // False: skip IFC property-set parsing (faster). True (default): include properties.
            IncludeProperties = true,

            // False: skip presentation-layer names. True (default): include layer names.
            IncludePresentationLayers = true,

            // Opening filter mode.
            OpeningFilterMode = OpeningFilterModes.Default,

            // Tessellation quality
            TessellationQuality = TessellationQualities.Default
        };

        if (!System.IO.Path.IsPathRooted(fileName))
            fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

        fileName = FileUtils.FixDirectorySeparator(fileName);
        
        if (!File.Exists(fileName))
        {
            ShowErrorMessage("File does not exist:\n" + fileName);
            return;
        }


        ClearErrorMessage();

        if (_showEdgeLines)
        {
            var fileInfo = new FileInfo(fileName);
            if (fileInfo.Length > 10000000) // > 10 MB
            {
                // Switch to SolidObjectsOnly because analysis of large models that is required to get the edge lines can take very long time
                _showEdgeLines = false;
                ShowErrorMessage("Disabling edge lines because analysis of large models that is required to get the edge lines can take very long time", showTimeMs: 3000);
            }
        }

        try
        {
            // We can use the Import method to get the imported objects as GroupNode:
            //var importedGroupNode = ifcImporter.Import(fileName);

            // But here we use LoadIfcGeometry (or ImportAsJson and IfcObject.Create) to get a ifcObject.
            // This way we can access additional ifc mesh properties

            IfcObject ifcObject;

            if (_loadAsJson)
            {
                var json = ifcImporter.ImportAsJson(fileName);
                ifcObject = IfcObject.Create(json);
            }
            else
            {

                ifcObject = ifcImporter.LoadIfcGeometry(fileName);
            }

            _ifcLoadTime = _loadStopwatch.Elapsed.TotalMilliseconds;
            _loadStopwatch.Restart();


            if (_useOptimizedGroupNode)
                ShowOptimizedIfcObject(ifcObject, _importedGroupNode);
            else
                ShowIfcObject(ifcObject, _importedGroupNode);

            _ifcObjectCreateTime = _loadStopwatch.Elapsed.TotalMilliseconds;


            _lastImportedFileName = fileName;

            UpdateLastLoadTimes();

            // To see the hierarchy of the imported models, execute the following in the Visual Studio's Immediate Window
            //_importedGroupNode.DumpHierarchy();
        }
        catch (Exception ex)
        {
            ShowErrorMessage("Error importing file:\n" + ex.Message);
            return;
        }

        if (Scene != null)
        {
            if (_objectLinesNode == null)
            {
                var lineMaterial = new LineMaterial(Color3.Black, 1)
                {
                    DepthBias = 0.005f
                };

                _objectLinesNode = new MultiLineNode(isLineStrip: false, lineMaterial, "ObjectLines");
                Scene.RootNode.Add(_objectLinesNode);

            }

            UpdateShownLines();


            Scene.RootNode.Update();

            if (targetPositionCamera != null && !Scene.RootNode.WorldBoundingBox.IsUndefined)
            {
                targetPositionCamera.TargetPosition = Scene.RootNode.WorldBoundingBox.GetCenterPosition();
                targetPositionCamera.Distance = Scene.RootNode.WorldBoundingBox.GetDiagonalLength() * 1.5f;
            }
        }
    }
    
    private void UpdateShownLines()
    {
        if (_objectLinesNode == null || _importedGroupNode.Count == 0)
            return; // no model imported

        if (!_showEdgeLines)
        {
            _objectLinesNode.Visibility = SceneNodeVisibility.Hidden;
            return;
        }


        // Reuse the instance of EdgeLinesFactory
        // This can reuse the lists and array that are internally used by the EdgeLinesFactory
        // See comments in Lines/EdgeLinesSample.cs for more info about edge lines generation.

        _edgeLinesFactory ??= new EdgeLinesFactory();
        var edgeLinePositions = _edgeLinesFactory.CreateEdgeLines(_importedGroupNode, 15);

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
    private void ShowIfcObject(IfcObject ifcObject, GroupNode groupNode)
    {
        foreach (var ifcMesh in ifcObject.Meshes)
        {
            var meshModelNode = CreateMeshModelNode(ifcMesh);
                
            if (meshModelNode != null)
                groupNode.Add(meshModelNode);
        }
    }
    
    private void ShowOptimizedIfcObject(IfcObject ifcObject, GroupNode groupNode)
    {
        var optimizedGroupNode = new OptimizedGroupNode();

        foreach (var ifcMesh in ifcObject.Meshes)
        {
            var meshModelNode = CreateMeshModelNode(ifcMesh);
                
            if (meshModelNode != null)
                optimizedGroupNode.Add(meshModelNode);
        }
        
        groupNode.Add(optimizedGroupNode);
    }

    private MeshModelNode? CreateMeshModelNode(IfcMesh ifcMesh)
    {
        if (ifcMesh.IfcType == "IfcSpace") // Skip showing IfcSpace model as it represents a bounded area or volume—such as a room, corridor, or functional zone—within a building or site structure.
            return null;

        var mesh = new StandardMesh(ifcMesh.Vertices, ifcMesh.TriangleIndices, ifcMesh.Name);
        var material = new StandardMaterial(ifcMesh.Color, name: ifcMesh.MaterialName);

        var meshModelNode = new MeshModelNode(mesh, material, ifcMesh.Name);

        if (ifcMesh.OriginVector3 != Vector3.Zero)
            meshModelNode.Transform = new TranslateTransform(ifcMesh.OriginVector3);

        return meshModelNode;
    } 

    private void UpdateLastLoadTimes()
    {
        _infoTextBox?.SetText($"IfcLoad: {_ifcLoadTime:F2} ms\nCreate IfcObject: {_ifcObjectCreateTime:F2} ms");  
    }

    private void ProcessPointerMove(Vector2 pointerPosition)
    {
        if (!_hitTesting || SceneView == null || _infoTextBox == null)
            return;

        var hitObject = SceneView.GetClosestHitObject(pointerPosition.X, pointerPosition.Y);

        if (hitObject != _lastHitObject)
        {
            if (hitObject != null)
                _infoTextBox.SetText("Hit object:\n" + hitObject.HitSceneNode?.Name);
            else
                _infoTextBox.SetText("No object hit");

            _lastHitObject = hitObject;
        }
    }

    private void ReloadLastFile()
    {
        if (_lastImportedFileName == null)
            return;

        ImportFile(_lastImportedFileName);
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right, isVertical: true);

        ui.CreateCheckBox("Show edge lines", _showEdgeLines, isChecked =>
        {
            _showEdgeLines = isChecked;
            UpdateShownLines();
        });

        ui.CreateCheckBox("Use OptimizedGroupNode (?):When checked then the imported models are shown by using OptimizedGroupNode.\nThis renders all the meshes with a single draw call\n(two draw calls are used when using transparent objects).\nThis significantly improves the rendering performance.",
            _useOptimizedGroupNode, isChecked =>
        {
            _useOptimizedGroupNode = isChecked;
            ReloadLastFile();
        });

        ui.CreateCheckBox("Load as json (?):When checked, then ImportAsJson method is used. This loads the ifc file and returns json text of all the meshes.\nThen the json is parsed and converted into 3D models.\nThis method is the only method that is available from the original IFClite library.\nThe version that is used by this samples is updated to support reading meshes directly from native memory without using json\n(see https://github.com/ab4d/ifc-lite)", _loadAsJson, isChecked =>
        {
            _loadAsJson = isChecked;
            ReloadLastFile();
        });

        ui.CreateCheckBox("Hit testing", _hitTesting, isChecked => _hitTesting = isChecked);

        ui.AddSeparator();


        _infoTextBox = ui.CreateTextBox(width: 250, height: 100);
        UpdateLastLoadTimes(); // _infoTextBox is created after loading the initial ifc file


        // Subscribe to mouse (pointer) moved
        ui.RegisterPointerMoved(ProcessPointerMove);


        // Try to register for file drag-and-drop
        bool isDragAndDropSupported = ui.RegisterFileDropped(".*", ImportFile);

        if (isDragAndDropSupported)
        {
            _subtitle += "\n\nDrag and drop ifc file here to open it.";
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
}