using System.IO;
using System.Numerics;
using System.Reflection.Metadata;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.glTF;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.Importers;

public class StlImporterExporterSample : CommonSample
{
    public override string Title => "Stl Importer and Exporter";

    private string _subtitle = ".stl files are very commonly used for 3D printing. The initially imported file can be used to print a fan that can be connected to LEGO bricks.\n\nStlImporter and StlExporter classes are part of the core Ab4d.SharpEngine library.\nThey can import from text or binary .stl files and export a single ModelNode into a binary .stl file.";
    public override string? Subtitle => _subtitle;

    private readonly string _initialFileName = "Resources\\Models\\lego-fan.stl";
    //private readonly string _initialFileName = @"C:\Users\User\Downloads\STLdotNET-master\tests\Data\ASCII.stl";

    private bool _isTwoSidedMaterials = true;
    private bool _convertToYUp = true;
    
    private ICommonSampleUIElement? _textBoxElement;
    private MultiLineNode? _objectLinesNode;
    private MeshModelNode? _importedMeshModelNode;

    private EdgeLinesFactory? _edgeLinesFactory;
    
    private Vector2? _savedAxisPanelPosition;

    private ICommonSampleUIElement? _exportFileNameTextBox;
    private ICommonSampleUIElement? _exportSuccessfulLabel;
    
    private string? _importedFileName;
    private string? _exportFileName;

    private Color4? _importedModelColor;
    private bool _isImportedModelConvertedToYUp;


    private enum ViewTypes
    {
        SolidObjectsOnly = 0,
        SolidObjectWithEdgeLines = 1,
        SolidObjectWithWireframe = 2
    }

    private ViewTypes _currentViewType = ViewTypes.SolidObjectsOnly;
    


    public StlImporterExporterSample(ICommonSamplesContext context)
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
        
        ClearErrorMessage();
        

        string fileExtension = System.IO.Path.GetExtension(fileName);
        if (!fileExtension.Equals(".stl", StringComparison.OrdinalIgnoreCase))
        {
            ShowErrorMessage("StlImporter support only .stl files and does not support importing files from file extension: " + fileExtension);
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

        // Create a StlImporter object
        var stlImporter = new StlImporter()
        {
            UseTwoSidedMaterials = _isTwoSidedMaterials,
            ConvertToYUp = _convertToYUp
        };

        try
        {
            // Import the 3D model from the file into MeshModelNode
            _importedMeshModelNode = stlImporter.Import(fileName);
            
            // To import from a stream, use:
            //_importedModelNodes = stlImporter.Import(stlStream);
            
            _importedFileName = fileName;
            
            // When color is set in binary stl file (written in file header after the "COLOR=" text),
            // then save it so we can reuse that when exporting the model.
            _importedModelColor = stlImporter.LastReadModelColor;
            _isImportedModelConvertedToYUp = _convertToYUp;
            
            UpdateExportFileName();
        }
        catch (Exception ex)
        {
            ShowErrorMessage("Error importing file:\n" + ex.Message);
            return;
        }

        if (_importedMeshModelNode == null)
            return;
        
        
        Scene.RootNode.Add(_importedMeshModelNode);


        if (_objectLinesNode == null)
        {
            var lineMaterial = new LineMaterial(Color3.Black, 1)
            {
                DepthBias = 0.001f
            };

            _objectLinesNode = new MultiLineNode(isLineStrip: false, lineMaterial, "ObjectLines");
        }

        // Add _objectLinesNode to the scene because before all the children of RootNode were cleared
        Scene.RootNode.Add(_objectLinesNode);

        UpdateShownLines();


        if (_importedMeshModelNode.WorldBoundingBox.IsUndefined)
            _importedMeshModelNode.Update();

        if (targetPositionCamera != null && !_importedMeshModelNode.WorldBoundingBox.IsUndefined)
        {
            targetPositionCamera.TargetPosition = _importedMeshModelNode.WorldBoundingBox.GetCenterPosition();
            targetPositionCamera.Distance = _importedMeshModelNode.WorldBoundingBox.GetDiagonalLength() * 1.5f;
        }
    }


    private void UpdateShownLines()
    {
        if (_importedMeshModelNode == null || _objectLinesNode == null)
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
            var edgeLinePositions = _edgeLinesFactory.CreateEdgeLines(_importedMeshModelNode, 15);

            _objectLinesNode.Positions = edgeLinePositions.ToArray();
            _objectLinesNode.Visibility = SceneNodeVisibility.Visible;
        }
        else if (_currentViewType == ViewTypes.SolidObjectWithWireframe)
        {
            var wireframePositions = LineUtils.GetWireframeLinePositions(_importedMeshModelNode, removedDuplicateLines: false); // remove duplicates can take some time for bigger models

            _objectLinesNode.Positions = wireframePositions;
            _objectLinesNode.Visibility = SceneNodeVisibility.Visible;
        }
    }
    
    private void UpdateExportFileName()
    {
        if (string.IsNullOrEmpty(_importedFileName))
            _exportFileName = "Export.stl";
        else
            _exportFileName = System.IO.Path.GetFileName(_importedFileName);

        _exportFileNameTextBox?.SetText(_exportFileName);
    }
    
    private void ExportScene()
    {
        if (_importedMeshModelNode == null || _importedFileName == null || _exportFileName == null || Scene == null)
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

        
        // .stl file can contain only one mesh
        var mesh = _importedMeshModelNode.GetMesh();
        
        
        
        
        if (mesh == null)
            return;
        
        
        // First create an instance of StlExporter
        var stlExporter = new StlExporter();
        
        // If imported model was converted to Y-up, then convert that back to Z-up
        stlExporter.ConvertToZUp = _isImportedModelConvertedToYUp;
        
        // If we have read the model color, then we also save it
        stlExporter.ModelColor = _importedModelColor;
        
        // The following code can be used to export the current model color:
        //if (_importedMeshModelNode.Material is StandardMaterialBase standardMaterial)
        //    stlExporter.ModelColor = new Color4(standardMaterial.DiffuseColor, standardMaterial.Opacity);

        try
        {
            stlExporter.Export(fullExportFileName, mesh);
            
            // You can also export a ModelNode. 
            //stlExporter.Export(fullExportFileName, modelNode, exportModelColor: true);
            
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
        ui.CreateCheckBox("Two-sided materials", _isTwoSidedMaterials, isChecked =>
        {
            _isTwoSidedMaterials = isChecked;
            if (_importedFileName != null)
                ImportFile(_importedFileName);
        });
        
        ui.CreateCheckBox("Convert to Y-up (?):The models in stl files are usually defined in Z-up coordinate system.\nThis checkbox sets ConvertToYUp property to true to convert the model to Y-up coordinate system.", _convertToYUp, isChecked =>
        {
            _convertToYUp = isChecked;
            if (_importedFileName != null)
                ImportFile(_importedFileName);
        });
        
        ui.AddSeparator();
        ui.CreateLabel("Export file name:");
        _exportFileNameTextBox = ui.CreateTextBox(width: 0, initialText: _exportFileName, textChangedAction: (fileName) => _exportFileName = fileName);

        ui.AddSeparator();
        ui.CreateButton("Export scene", ExportScene);

        ui.AddSeparator();
        _exportSuccessfulLabel = ui.CreateLabel("Exported to:", width: 230).SetColor(Colors.Green).SetIsVisible(false);


        // Try to register for file drag-and-drop
        bool isDragAndDropSupported = ui.RegisterFileDropped(".stl", ImportFile);

        if (isDragAndDropSupported)
        {
            _subtitle = "Drag and drop .stl file here to open it.\n\n" + _subtitle;
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