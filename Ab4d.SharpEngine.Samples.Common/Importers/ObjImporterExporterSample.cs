using System.IO;
using System.Numerics;
using System.Reflection.Metadata;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.glTF;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.Importers;

public class ObjImporterExporterSample : CommonSample
{
    public override string Title => "Obj Importer and Exporter";

    private string _subtitle = "ObjImporter and ObjExporter classes are part of the core Ab4d.SharpEngine library.\nThey can import from .obj and .mtl (contains material definitions) files and export the current scene to that file format.";
    public override string? Subtitle => _subtitle;

    private readonly string _initialFileName = "Resources\\Models\\robotarm.obj";

    private ICommonSampleUIElement? _textBoxElement;
    private MultiLineNode? _objectLinesNode;
    private SceneNode? _importedModelNodes;

    private EdgeLinesFactory? _edgeLinesFactory;
    
    private Vector2? _savedAxisPanelPosition;

    private ICommonSampleUIElement? _exportFileNameTextBox;
    private ICommonSampleUIElement? _exportSuccessfulLabel;
    
    private string? _importedFileName;
    private string? _exportFileName;

    private enum ViewTypes
    {
        SolidObjectsOnly = 0,
        SolidObjectWithEdgeLines = 1,
        SolidObjectWithWireframe = 2
    }

    private ViewTypes _currentViewType = ViewTypes.SolidObjectWithEdgeLines;
    


    public ObjImporterExporterSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
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
        if (Scene == null || fileName == null)
            return;

        Scene.RootNode.Clear();
        _importedFileName = null;

        string fileExtension = System.IO.Path.GetExtension(fileName);
        if (!fileExtension.Equals(".obj", StringComparison.OrdinalIgnoreCase))
        {
            ShowErrorMessage("ObjImporter support only obj files and does not support importing files from file extension: " + fileExtension);
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


        // FixDirectorySeparator method returns file path with correctly sets backslash or slash as directory separator based on the current OS.
        fileName = Ab4d.SharpEngine.Utilities.FileUtils.FixDirectorySeparator(fileName);

        // Create a ObjImporter object
        // To read texture images we also need to provide BitmapIO and 
        // it is also recommended to set GpuDevice (if not, then textures will be created later when GpuDevice is initialized).
        var objImporter = new ObjImporter(this.BitmapIO, this.GpuDevice);

        try
        {
            _importedModelNodes = objImporter.Import(fileName);
            
            _importedFileName = fileName;
            UpdateExportFileName();
        }
        catch (Exception ex)
        {
            ShowErrorMessage("Error importing file:\n" + ex.Message);
            return;
        }


        // By default, textures are read from the same directory as the obj file.
        // If they are not stored in some other folder, then the folder can be specified in the texturesDirectory parameter.
        // 
        // It is also possible to change the default material. When it is not specified then StandardMaterials.Silver is used.
        //
        //string texturesDirectory;
        //var defaultMaterial = StandardMaterials.Orange;
        //var readSceneNodes = objImporter.Import(fileName, texturesDirectory, defaultMaterial);


        // To read obj file from stream use the following
        // (GetResourceStream should return the Stream of the specified resourceFileName)
        //using (var fileStream = System.IO.File.OpenRead(fileName))
        //{
        //    _importedModelNodes = objImporter.Import(fileStream, resourceFileName => GetResourceStream(resourceFileName));
        //}

        //Stream? GetResourceStream(string resourceName)
        //{
        //    var directoryName = System.IO.Path.GetDirectoryName(_importedFileName);
        //    var fileName = System.IO.Path.Combine(directoryName, resourceName);

        //    if (System.IO.File.Exists(fileName))
        //        return System.IO.File.OpenRead(fileName);

        //    return null;
        //}


        // It is also possible to read only obj file data without converting that into SharpEngine's objects:
        //var objFileData = objImporter.ReadObjFileData(fileName);


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
            targetPositionCamera.Distance = _importedModelNodes.WorldBoundingBox.GetDiagonalLength() * 2;
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
            _edgeLinesFactory ??= new EdgeLinesFactory();
            var edgeLinePositions = _edgeLinesFactory.CreateEdgeLines(_importedModelNodes, 15);

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
        if (string.IsNullOrEmpty(_importedFileName))
            _exportFileName = "Export.obj";
        else
            _exportFileName = System.IO.Path.GetFileName(_importedFileName);

        _exportFileNameTextBox?.SetText(_exportFileName);
    }
    
    private void ExportScene()
    {
        if (_importedFileName == null || _exportFileName == null || Scene == null)
            return;

        _exportSuccessfulLabel?.SetIsVisible(false);

        string fullExportFileName;
        if (System.IO.Path.IsPathRooted(_exportFileName))
        {
            fullExportFileName = _exportFileName;
        }
        else
        {
            if (!_importedFileName.EndsWith(_initialFileName)) // User opened file?
                fullExportFileName = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_importedFileName)!, _exportFileName);
            else // initial file name - do not save to the bin/debug folder but to desktop
                fullExportFileName = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), _exportFileName);
        }

        // First create an instance of ObjExporter
        var objExporter = new ObjExporter();

        // Then add Scene to the exporter
        objExporter.AddScene(Scene);

        // We could also export only a selected SceneNode (can be a GroupNode)
        //objExporter.AddSceneNode(sceneNode);

        try
        {
            objExporter.Export(fullExportFileName);

            // To export to stream use:
            //objExporter.Export(objFileStream, (filename, data) =>
            //{
            //    // export data with fileName to some custom stream
            //});

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
        ui.CreateLabel("Export file name:");
        _exportFileNameTextBox = ui.CreateTextBox(width: 0, initialText: _exportFileName, textChangedAction: (fileName) => _exportFileName = fileName);

        ui.AddSeparator();
        ui.CreateButton("Export scene", ExportScene);

        ui.AddSeparator();
        _exportSuccessfulLabel = ui.CreateLabel("Exported to:", width: 230).SetColor(Colors.Green).SetIsVisible(false);


        // Try to register for file drag-and-drop
        bool isDragAndDropSupported = ui.RegisterFileDropped(".obj", ImportFile);

        if (isDragAndDropSupported)
        {
            _subtitle = "Drag and drop .obj file here to open it.\n\n" + _subtitle;
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