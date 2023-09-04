using System.Numerics;
using Ab4d.SharpEngine.Assimp;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.Importers;

public class ReaderObjSample : CommonSample
{
    public override string Title => "ReaderObj - import 3D models from obj files";
    public override string? Subtitle => "ReaderObj is written in C# and is part of the Ab4d.SharpEngine library.";

    private readonly string _initialFileName = "Resources\\Models\\robotarm.obj";

    private ICommonSampleUIElement? _textBoxElement;
    private MultiLineNode? _objectLinesNode;
    private SceneNode? _importedModelNodes;

    private enum ViewTypes
    {
        SolidObjectsOnly = 0,
        SolidObjectWithEdgeLines = 1,
        SolidObjectWithWireframe = 2
    }

    private ViewTypes _currentViewType = ViewTypes.SolidObjectWithEdgeLines;


    public ReaderObjSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _initialFileName);
        ImportFile(fileName);
    }

    protected void ImportFile(string? fileName)
    {
        if (Scene == null || fileName == null)
            return;

        Scene.RootNode.Clear();

        string fileExtension = System.IO.Path.GetExtension(fileName);
        if (!fileExtension.Equals(".obj", StringComparison.OrdinalIgnoreCase))
        {
            ShowErrorMessage("ReaderObj support only obj files and does not support importing files from file extension: " + fileExtension);
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

        var readerObj = new ReaderObj();

        try
        {
            _importedModelNodes = readerObj.ReadSceneNodes(fileName);
        }
        catch (Exception ex)
        {
            ShowErrorMessage("Error importing file:\n" + ex.Message);
            return;
        }

        // By default textures are read from the same directory as the obj file.
        // If they are stored in some other folder, then this can be specified in the texturesDirectory parameter.
        // 
        // It is also possible to change the default material. When it is not specified then StandardMaterials.Silver is used.
        //
        //string texturesDirectory;
        //var defaultMaterial = StandardMaterials.Orange;
        //var readSceneNodes = readerObj.ReadSceneNodes(fileName, texturesDirectory, defaultMaterial);

        // To read obj file from stream use the following
        // (GetResourceStream should return the Stream of the specified resourceFileName)
        //readerObj.ReadSceneNodes(FileStream, resourceFileName => GetResourceStream(resourceFileName));

        // It is also possible to read only obj file data without converting that into SharpEngine's objects:
        //var objFileData = readerObj.ReadObjFileData(fileName);


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
            var edgeLinePositions = new List<Vector3>();
            LineUtils.AddEdgeLinePositions(_importedModelNodes, 15, edgeLinePositions);

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

    // Drag and drop is platform specific function and needs to be implemented on per-platform sample
    protected virtual bool SetupDragAndDrop(ICommonSampleUIProvider ui)
    {
        return false; // no drag and drop not supported
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


        bool isDragAndDropSupported = SetupDragAndDrop(ui);

        if (!isDragAndDropSupported)
        {
            // If drag and drop is not supported, then show TextBox so user can enter file name to import

            ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Left, isVertical: false);

            ui.CreateLabel("FileName:");
            _textBoxElement = ui.CreateTextBox(width: 400, initialText: FileUtils.FixDirectorySeparator(_initialFileName));

            ui.CreateButton("Load", () =>
            {
                ImportFile(_textBoxElement.GetText());
            });
        }
    }
}