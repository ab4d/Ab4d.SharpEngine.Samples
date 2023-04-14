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
    private MultiLineNode? _wireframeLineNode;

    public ReaderObjSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _initialFileName);
        ImportFile(fileName);
    }

    private void ImportFile(string? fileName)
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

        GroupNode importedModelNodes;

        try
        {
            importedModelNodes = readerObj.ReadSceneNodes(fileName);
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


        Scene.RootNode.Add(importedModelNodes);


        var wireframePositions = LineUtils.GetWireframeLinePositions(importedModelNodes, removedDuplicateLines: false); // remove duplicates can take some time for bigger models

        var wireframeLineMaterial = new LineMaterial(Color3.Black, 1)
        {
            DepthBias = 0.005f
        };

        _wireframeLineNode = new MultiLineNode(wireframePositions, isLineStrip: false, wireframeLineMaterial, "Wireframe");

        Scene.RootNode.Add(_wireframeLineNode);


        if (importedModelNodes.WorldBoundingBox.IsEmpty)
            importedModelNodes.Update();

        if (targetPositionCamera != null && !importedModelNodes.WorldBoundingBox.IsEmpty)
        {
            targetPositionCamera.TargetPosition = importedModelNodes.WorldBoundingBox.GetCenterPosition();
            targetPositionCamera.Distance = importedModelNodes.WorldBoundingBox.GetDiagonalLength() * 2;
        }
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        var rootStackPanel = ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Left, isVertical: false);

        ui.CreateLabel("FileName:");
        _textBoxElement = ui.CreateTextBox(width: 400, initialText: FileUtils.FixDirectorySeparator(_initialFileName));

        ui.CreateButton("Load", () =>
        {
            ImportFile(_textBoxElement.GetText());
        });

        if (_wireframeLineNode != null)
        {
            ui.AddSeparator();
            ui.CreateCheckBox("Show wireframe", true, isChecked => _wireframeLineNode.Visibility = isChecked ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden);
        }
    }
}