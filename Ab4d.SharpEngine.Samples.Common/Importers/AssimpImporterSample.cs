using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using System.Runtime.InteropServices;
using Ab4d.Assimp;
using Ab4d.SharpEngine.Assimp;
using Ab4d.SharpEngine.Cameras;

namespace Ab4d.SharpEngine.Samples.Common.Importers;

public class AssimpImporterSample : CommonSample
{
    public override string Title => "AssimpImporter - import 3D models from almost any file format";
    public override string? Subtitle => "AssimpImporter uses third-party native Assimp library (https://github.com/assimp/assimp).";

    private readonly string _initialFileName = "Resources\\Models\\planetary-gear.FBX";

    private AssimpImporter? _assimpImporter;
    private ICommonSampleUIElement? _textBoxElement;
    private ICommonSampleUIPanel? _infoPanel;
    private ICommonSampleUIElement? _infoLabel;
    private MultiLineNode? _wireframeLineNode;

    public AssimpImporterSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        if (scene.GpuDevice != null)
            InitAssimpLibrary(scene.GpuDevice, "assimp-lib");

        string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _initialFileName);
        ImportFile(fileName);
    }

    private void InitAssimpLibrary(VulkanDevice gpuDevice, string? assimpFolder)
    {
        string? assimpLibFileName;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            assimpLibFileName = Environment.Is64BitProcess ? "Assimp64.dll" : "Assimp32.dll";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && Environment.Is64BitProcess)
        {
            assimpLibFileName = "libassimp.so.5";
        }
        else
        {
            assimpLibFileName = null;
        }

        if (assimpLibFileName == null)
        {
            ShowErrorMessage("AssimpImporter is not supported on this OS");
            return;
        }

        if (assimpFolder == null)
            assimpFolder = AppDomain.CurrentDomain.BaseDirectory;
        else
            assimpFolder = FileUtils.FixDirectorySeparator(assimpFolder);


        if (assimpFolder.Contains(assimpLibFileName))
        {
            // if provided folder also contains the file name, then use that of the file actually exists

            if (!System.IO.File.Exists(assimpFolder))
                throw new NotSupportedException($"The specified assimp library does not exist: {assimpFolder}");

            assimpLibFileName = assimpFolder;
        }
        else
        {
            if (!System.IO.Path.IsPathRooted(assimpFolder))
                assimpFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assimpFolder);

            // try to find the assimp library file
            var assimpLibraries = System.IO.Directory.GetFiles(assimpFolder, assimpLibFileName, SearchOption.AllDirectories);

            if (assimpLibraries == null || assimpLibraries.Length == 0)
            {
                ShowErrorMessage($"Cannot find the Assimp library ({assimpLibFileName}) in the folder {assimpFolder}");
                return;
            }

            assimpLibFileName = assimpLibraries[0];
        }

        try
        {
            AssimpLibrary.Instance.Initialize(assimpLibFileName, throwException: true);
        }
        catch (Exception ex)
        {
            ShowErrorMessage(@$"Error loading native Assimp library:
{ex.Message}

The most common cause of this error is that the Visual C++ Redistributable for Visual Studio 2019 is not installed on the system. 
See the following web page for more info:
https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170");

            return;
        }

        if (!AssimpLibrary.Instance.IsInitialized)
            throw new Exception("Cannot initialize native Assimp library");

        _assimpImporter = new AssimpImporter(gpuDevice, BitmapIO);
    }

    private void ImportFile(string? fileName)
    {
        if (_assimpImporter == null || Scene == null || fileName == null)
            return;

        Scene.RootNode.Clear();


        string fileExtension = System.IO.Path.GetExtension(fileName);
        if (!_assimpImporter.IsImportFormatSupported(fileExtension))
        {
            ShowErrorMessage("Assimp does not support importing files from file extension: " + fileExtension);
            return;
        }

        if (!System.IO.Path.IsPathRooted(fileName))
            fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

        if (!File.Exists(fileName))
        {
            ShowErrorMessage("File does not exist:\n" + fileName);
            return;
        }


        // FixDirectorySeparator method returns file path with correctly sets backslash or slash as directory separator based on the current OS.
        fileName = Ab4d.SharpEngine.Utilities.FileUtils.FixDirectorySeparator(fileName);

        SceneNode? importedModelNodes;

        try
        {
            importedModelNodes = _assimpImporter.ImportSceneNodes(fileName);
        }
        catch (Exception ex)
        {
            ShowErrorMessage("Error importing file:\n" + ex.Message);
            return;
        }

        if (importedModelNodes != null)
        {
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
                targetPositionCamera.Distance = importedModelNodes.WorldBoundingBox.GetDiagonalLength() * 1.5f;
            }
        }
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        var rootStackPanel = ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Left, isVertical: false);

        ui.CreateLabel("FileName:");
        _textBoxElement = ui.CreateTextBox(width: 400, initialText: FileUtils.FixDirectorySeparator(_initialFileName));

        ui.CreateButton("Load", () =>
        {
            _infoPanel?.SetIsVisible(false);
            ImportFile(_textBoxElement.GetText());
        });
        
        ui.AddSeparator();
        ui.CreateButton("Show supported import formats", () =>
        {
            if (_assimpImporter == null)
                return;

            var supportedExtensions = string.Join(", ", _assimpImporter.SupportedImportFileExtensions);

            _infoLabel?.SetText("Supported import file formats: " + supportedExtensions);
            _infoPanel?.SetIsVisible(true);
        });

        if (_wireframeLineNode != null)
        {
            ui.AddSeparator();
            ui.CreateCheckBox("Show wireframe", true, isChecked => _wireframeLineNode.Visibility = isChecked ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden);
        }
        
        _infoPanel = ui.CreateStackPanel(PositionTypes.Center, addBorder: true, isSemiTransparent: true);
        _infoPanel.SetIsVisible(false);

        _infoLabel = ui.CreateLabel("");
    }
}