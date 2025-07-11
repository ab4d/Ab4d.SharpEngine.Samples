﻿using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using System.Runtime.InteropServices;
using Ab4d.Assimp;
using Ab4d.SharpEngine.Assimp;
using Ab4d.SharpEngine.Cameras;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.Importers;

public class AssimpImporterSample : CommonSample
{
    public override string Title => "AssimpImporter - import 3D models from almost any file format";

    private string _subtitle = "AssimpImporter uses third-party native Assimp library (https://github.com/assimp/assimp).\nClick on 'Show supported formats' button to see all supported file formats.";
    public override string? Subtitle => _subtitle;

    private readonly string _initialFileName = "Resources\\Models\\planetary-gear.FBX";

    private AssimpImporter? _assimpImporter;
    private ICommonSampleUIElement? _textBoxElement;
    private ICommonSampleUIPanel? _infoPanel;
    private ICommonSampleUIElement? _infoLabel;
    private MultiLineNode? _objectLinesNode;
    private GroupNode? _importedModelNodes;

    private EdgeLinesFactory? _edgeLinesFactory;

    private Vector2? _savedAxisPanelPosition;

    private string? _importedFileName;
    private bool _isAssimpLoggingEnabled = false;

    private enum ViewTypes
    {
        SolidObjectsOnly = 0,
        SolidObjectWithEdgeLines = 1,
        SolidObjectWithWireframe = 2
    }

    private ViewTypes _currentViewType = ViewTypes.SolidObjectWithEdgeLines;


    public AssimpImporterSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        _assimpImporter = InitAssimpLibrary(scene.GpuDevice, this.BitmapIO, "assimp-lib", ShowErrorMessage);

        // Show used Assimp native version
        if (_assimpImporter != null)
            _subtitle = _subtitle.Replace("library (https", $"library v{_assimpImporter.AssimpVersionString} (https");
        
        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 50;
            targetPositionCamera.Attitude = 12;
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

    // When calling InitAssimpLibrary, the gpuDevice may be null.
    // In this case the textures will be created later when the materials with textures are initialized.
    public static AssimpImporter? InitAssimpLibrary(VulkanDevice? gpuDevice, IBitmapIO? bitmapIO, string? assimpFolder, Action<string>? showErrorMessageAction)
    {
        if (assimpFolder == null)
            assimpFolder = AppDomain.CurrentDomain.BaseDirectory;
        else
            assimpFolder = FileUtils.FixDirectorySeparator(assimpFolder);

        string? assimpLibFileName = null;
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            assimpLibFileName = Environment.Is64BitProcess ? "Assimp64.dll" : "Assimp32.dll";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && Environment.Is64BitProcess)
            assimpLibFileName = "libassimp.so*"; // this will be uses as a search parameter to get the latest version

        if (assimpLibFileName == null)
        {
            showErrorMessageAction?.Invoke("AssimpImporter is not supported on this OS");
            return null;
        }
        

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

            if (assimpLibraries.Length == 0)
            {
                showErrorMessageAction?.Invoke($"Cannot find the Assimp library ({assimpLibFileName}) in the folder {assimpFolder}");
                return null;
            }
            else if (assimpLibraries.Length == 1)
            {
                assimpLibFileName = assimpLibraries[0];
            }
            else // more than 1 library found - get the last one (we may have files with different version - get the last one)
            {
                Array.Sort(assimpLibraries);
                assimpLibFileName = assimpLibraries[^1]; // get last
            }
        }

        try
        {
            AssimpLibrary.Instance.Initialize(assimpLibFileName, throwException: true);
        }
        catch (Exception ex)
        {
            showErrorMessageAction?.Invoke(@$"Error loading native Assimp library:
{ex.Message}

The most common cause of this error is that the Visual C++ Redistributable for Visual Studio 2019 is not installed on the system. 
See the following web page for more info:
https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170");

            return null;
        }

        if (!AssimpLibrary.Instance.IsInitialized)
            throw new Exception("Cannot initialize native Assimp library");

        var assimpImporter = new AssimpImporter(bitmapIO, gpuDevice); // It is also possible to create AssimpImporter without GpuDevice - in this case the textures will be created later when the materials with textures are initialized
        
        return assimpImporter;
    }

    protected void ImportFile(string? fileName)
    {
        if (_assimpImporter == null || Scene == null || fileName == null)
            return;

        Scene.RootNode.Clear();
        _importedFileName = null;


        string fileExtension = System.IO.Path.GetExtension(fileName);
        if (!_assimpImporter.IsImportFormatSupported(fileExtension))
        {
            ShowErrorMessage("Assimp does not support importing files from file extension: " + fileExtension);
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

        // Setting PreserveNativeResourcesAfterImporting will preserve the _assimpImporter.NativeAssimpScene after calling Import method.
        // Otherwise (by default) the NativeAssimpScene is disposed in the Import method.
        // Preserving the NativeAssimpScene can be useful to read the scene data from the Assimp object model
        // or to export the Assimp scene to another file format (see commented ExportScene below).
        _assimpImporter.PreserveNativeResourcesAfterImporting = true;

        if (_isAssimpLoggingEnabled)
        {
            _assimpImporter.LoggerCallback = (message, data) => System.Diagnostics.Debug.WriteLine($"Assimp: {message}");
            _assimpImporter.IsVerboseLoggingEnabled = true;
        }
        else
        {
            _assimpImporter.LoggerCallback = null;
            _assimpImporter.IsVerboseLoggingEnabled = false;
        }

        try
        {
            // To import file from stream use the following code:
            //using (var fs = System.IO.File.OpenRead(fileName))
            //    _importedModelNodes = _assimpImporter.Import(fs, formatHint: System.IO.Path.GetExtension(fileName));

            _importedModelNodes = _assimpImporter.Import(fileName);
            _importedFileName = fileName;

            // To see the hierarchy of the imported models, execute the following in the Visual Studio's Immediate Window (first check that _importedModelNodes is GroupNode and not ModelMeshNode):
            //((GroupNode)_importedModelNodes).DumpHierarchy();
        }
        catch (Exception ex)
        {
            ShowErrorMessage("Error importing file:\n" + ex.Message);
            return;
        }

        if (_importedModelNodes != null)
        {
            // Check animations:
            if (_assimpImporter.NativeAssimpScene != null) // This should not be null because we set PreserveNativeResourcesAfterImporting to true
            {
                int animationsCount = (int)_assimpImporter.NativeAssimpScene.Scene.NumAnimations;
                if (animationsCount > 0)
                {
                    for (int animationIndex = 0; animationIndex < animationsCount; animationIndex++)
                    {
                        var assimpAnimation = _assimpImporter.NativeAssimpScene.Scene.GetAnimation(animationIndex);

                        int channelsCount = (int)assimpAnimation.NumChannels;
                        for (int channelIndex = 0; channelIndex < channelsCount; channelIndex++)
                        {
                            var assimpChannel = assimpAnimation.GetChannel(channelIndex);

                            var nodeName = assimpChannel.NodeName; // nodeName defines which object is animated by this channel

                            if (assimpChannel.NumPositionKeys > 0)
                            {
                                var positionKeys = assimpChannel.PositionKeys;
                                // TODO:
                            }

                            if (assimpChannel.NumScalingKeys > 0)
                            {
                                var scalingKeys = assimpChannel.ScalingKeys;
                                // TODO:
                            }

                            if (assimpChannel.NumRotationKeys > 0)
                            {
                                var rotationKeys = assimpChannel.RotationKeys;
                                // TODO:
                            }
                        }
                    }
                }

                // When reading the file and PreserveNativeResourcesAfterImporting is true, then we need to manually dispose the native assimp scene.
                _assimpImporter.DisposeNativeAssimpScene();
            }


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

    private string GetSupportedFormatsInfo(AssimpImporter assimpImporter)
    {
        string assimpVersionText = assimpImporter.AssimpVersionString;
        var gitCommitHash = assimpImporter.GitCommitHash;

        // When Assimp library is complied from a source that if get the GutHub, then GitCommitHash is set to the source's last commit hash.
        // When Assimp library is complied from a zip file in GitHub's releases, then the GitCommitHash is zero. In this case do not show the hash.
        if (gitCommitHash != 0)
            assimpVersionText += string.Format("; Git commit hash: {0:x7}", gitCommitHash);

        var supportedImporterExtensions = string.Join(", ", assimpImporter.SupportedImportFileExtensions);

        // AssimpExporter provides information about supported export formats.
        // The current version does not support exporting SharpEngine Scene objects.
        // But it is possible to export and imported Assimp scene by using _assimpImporter.NativeAssimpScene.ExportScene method, for example commented ExportScene method below.

        var assimpExporter = new AssimpExporter();
        var supportedExporterExtensions = string.Join(", ", assimpExporter.SupportedExportFileExtensions);

        string supportedFormatsInfo = 
$@"Using native Assimp library v{assimpVersionText}

Supported import file formats:
{supportedImporterExtensions}

Supported export file formats:
{supportedExporterExtensions}

Note: The current version does not support exporting SharpEngine Scene objects, but it is possible to export and imported Assimp scene by using AssimpImporter.NativeAssimpScene.ExportScene method (see commented ExportScene method).";

        return supportedFormatsInfo;
    }
    
    // To test this method uncomment the code below and the declaration of export button in OnCreateUI
    //private void ExportScene()
    //{
    //    // To preserve the NativeAssimpScene after importing it, set the _assimpImporter.PreserveNativeResourcesAfterImporting to true
    //    // and do not call _assimpImporter.DisposeNativeAssimpScene()
    //    if (_assimpImporter == null || _assimpImporter.NativeAssimpScene == null)
    //        return;

    //    string exportedFileExtension = "obj"; // This value should be from assimpExporter.SupportedExportFileExtensions
    //    string exportedFileName = System.IO.Path.ChangeExtension(_importedFileName, exportedFileExtension)!;

    //    bool success = _assimpImporter.NativeAssimpScene.ExportScene(exportedFileName, exportedFileExtension);
    //}

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right, isVertical: true);

        ui.CreateLabel("View", isHeader: true);
        ui.CreateRadioButtons(new string[] { "Solid objects only", "Solid + generated EdgeLines", "Solid + Wireframe" }, (selectedIndex, selectedText) =>
        {
            _currentViewType = (ViewTypes)selectedIndex;
            UpdateShownLines();
        }, selectedItemIndex: (int)_currentViewType);

        ui.AddSeparator();
        ui.CreateCheckBox("Assimp logging (see VS Output)", _isAssimpLoggingEnabled, isChecked => _isAssimpLoggingEnabled = isChecked);

        ui.AddSeparator();
        ui.CreateButton("Show supported formats", () =>
        {
            if (_infoPanel != null)
            {
                var currentlyVisible = _infoPanel.GetIsVisible();
                _infoPanel.SetIsVisible(!currentlyVisible);
            }
        });

        //ui.AddSeparator();
        //ui.CreateButton("Export scene", ExportScene);


        _infoPanel = ui.CreateStackPanel(PositionTypes.Center, addBorder: true, isSemiTransparent: true);
        _infoPanel.SetIsVisible(false);

        if (_assimpImporter != null)
        {
            var supportedFormatsInfo = GetSupportedFormatsInfo(_assimpImporter);
            _infoLabel = ui.CreateLabel(supportedFormatsInfo);
        }


        // Try to register for file drag-and-drop
        bool isDragAndDropSupported = ui.RegisterFileDropped(".*", ImportFile);

        if (isDragAndDropSupported)
        {
            _subtitle += "\nDrag and drop file here to open it.";
        }
        else
        {
            // If drag and drop is not supported, then show TextBox so user can enter file name to import

            ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Left, isVertical: false);

            ui.CreateLabel("FileName:");
            _textBoxElement = ui.CreateTextBox(width: 400, initialText: FileUtils.FixDirectorySeparator(_initialFileName));

            ui.CreateButton("Load", () =>
            {
                _infoPanel?.SetIsVisible(false);
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