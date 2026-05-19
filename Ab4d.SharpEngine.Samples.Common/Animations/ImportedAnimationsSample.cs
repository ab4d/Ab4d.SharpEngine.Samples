using Ab4d.SharpEngine.Animation;
using Ab4d.SharpEngine.Assimp;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.glTF;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using System.Numerics;
using System.Text;

namespace Ab4d.SharpEngine.Samples.Common.Animations;

public class ImportedAnimationsSample : CommonSample
{
    public override string Title => "Animations imported by using glTF or Assimp importer";
    private string _subtitle = "";
    public override string? Subtitle => _subtitle;
    
    private readonly string _initialFileName = "Resources/Models/soldier.x";
    
    private glTFImporter? _gltfImporter;
    private AssimpImporter? _assimpImporter;
    
    private GroupNode? _importedModelNodes;
    private GroupNode? _boneMarkersGroupNode;

    private List<AnimationGroup>? _importedAnimations;
    private List<Skeleton>? _importedSkeletons;
    private List<Ab4d.SharpEngine.Meshes.Mesh>? _importedAnimatedSkeletonMeshes;
    
    private Dictionary<SkeletonNode, LineNode> _skeletonNodeBoneMarkerLines = new();
    
    private bool _isAnimationStarted;
    private bool _isAnimationTimeManuallyChanged;
    private bool _showBones = true;
    private int _initialAnimationIndex = 2;
    
    private ICommonSampleUIElement? _animationCombobox;
    private ICommonSampleUIElement? _animationTimeLabel;
    private ICommonSampleUIElement? _animationTimeSlider;
    private ICommonSampleUIElement? _startStopAnimationButton;
    private bool _isInternalTimeUpdate;

    private ICommonSampleUIElement? _textBoxElement;
    private ICommonSampleUIPanel? _infoPanel;
    private ICommonSampleUIElement? _infoLabel;
    private Vector2? _savedAxisPanelPosition;

    private AnimationGroup? _selectedAnimation;
    private IAnimation? _subscribedAnimation;
    private float _animationDuration;
    private float _animationTimeRelative;


    public ImportedAnimationsSample(ICommonSamplesContext context)
        : base(context)
    {
        ShowCameraAxisPanel = true;
    }

    protected override async Task OnCreateSceneAsync(Scene scene)
    {
        ImportFile(_initialFileName, scene);
    }

    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        base.OnSceneViewInitialized(sceneView);
        
        sceneView.SceneUpdating += SceneViewOnSceneUpdating;
    }
        
    /// <inheritdoc />
    protected override void OnDisposed()
    {
        if (SceneView != null)
            SceneView.SceneUpdating -= SceneViewOnSceneUpdating;
        
        if (Scene != null)
        {
            Scene.RemoveAllAnimations();
            Scene.RemoveAllSkeletonAnimations();
        }
        
        if (_savedAxisPanelPosition != null && CameraAxisPanel != null)
            CameraAxisPanel.Position = _savedAxisPanelPosition.Value;
        
        base.OnDisposed();
    }

    private void SceneViewOnSceneUpdating(object? sender, EventArgs e)
    {
        if ((!_isAnimationStarted && !_isAnimationTimeManuallyChanged) || SceneView == null)
            return;

        if (_isAnimationStarted && !_isAnimationTimeManuallyChanged)
        {
            if (!IsAnyAnimationRunning())
            {
                _isAnimationStarted = false;
                UpdateStartStopAnimationButton();
                return;
            }
        }

        UpdateBoneMarkers();
        
        _isAnimationTimeManuallyChanged = false;
    }

    private void ImportFile(string? fileName)
    {
        if (Scene != null && fileName != null)
            ImportFile(fileName, Scene);
    }

    private void ImportFile(string fileName, Scene scene)
    {
        scene.RemoveAllAnimations();
        scene.RemoveAllSkeletonAnimations();
        scene.RootNode.Clear();
        _boneMarkersGroupNode = null;
        _skeletonNodeBoneMarkerLines.Clear();

        if (_subscribedAnimation != null)
        {
            _subscribedAnimation.Updated -= OnAnimationUpdated;
            _subscribedAnimation = null;
        }

        _selectedAnimation = null;
        _animationDuration = 0;


        if (fileName.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".glb", StringComparison.OrdinalIgnoreCase))
        {
            ImportGltfFile(fileName, scene);    
        }
        else
        {
            ImportAssimpFile(fileName, scene); 
        }
        

        if (_importedModelNodes == null)
            return;
        
        // Use the "Run" animation for the initial solder.x file (do start the "Idle" animation because it is still)
        int initialAnimationIndex = fileName.EndsWith("soldier.x") ? 2 : 0;
        
        if (_animationCombobox != null)
        {
            // Update the animations ComboBox
            var importedAnimationNames = GetImportedAnimationNames();
            _animationCombobox.SetValue(importedAnimationNames); // passing a string array will set the ItemsSource
            
            _animationCombobox.SetValue(importedAnimationNames[initialAnimationIndex]); // passing a single string will set the selected item
        }
        
        ChangeSelectedAnimation(initialAnimationIndex);
        _initialAnimationIndex = initialAnimationIndex;

        StartAnimation();
        
        // Set camera to show the imported model
        scene.UpdateAnimations();
        
        // Before getting the bones matrices, we need to manually call Update to get the correct parent transformations.                
        scene.RootNode.Update();
        
        
        UpdateShownBones();
        
        if (_importedModelNodes.WorldBoundingBox.IsUndefined)
            _importedModelNodes.Update();
        
        if (targetPositionCamera != null && !_importedModelNodes.WorldBoundingBox.IsUndefined)
        {
            targetPositionCamera.Heading = 30;
            targetPositionCamera.Attitude = -10;
            targetPositionCamera.TargetPosition = _importedModelNodes.WorldBoundingBox.GetCenterPosition();
            targetPositionCamera.Distance = _importedModelNodes.WorldBoundingBox.GetDiagonalLength() * 2f;
        }
    }

    private void ImportGltfFile(string fileName, Scene scene)
    {
        _gltfImporter = new glTFImporter(this.BitmapIO);
        
        
        // The following two properties control if animation is read by the AssimpImporter
        // By default they are already set to true, but are still written here for clearance.
        _gltfImporter.ReadAnimations = true;
        _gltfImporter.ReadSkeletalAnimation = true;
        

        _importedModelNodes = _gltfImporter.Import(fileName);
        
        if (_importedModelNodes == null)
            return;
        
        
        // Add importer model to the Scene
        scene.RootNode.Add(_importedModelNodes);

        _importedAnimations = _gltfImporter.Animations;
        _importedAnimatedSkeletonMeshes = _gltfImporter.AnimatedSkeletonMeshes;

        if (_gltfImporter.AnimatedSkeletonMeshes != null)
        {
            foreach (var oneMesh in _gltfImporter.AnimatedSkeletonMeshes)
                scene.AddAnimatedSkeletonMesh(oneMesh, oneMesh.Skeleton!);
        }

        if (_gltfImporter.Skeletons != null)
        {
            var skeletonsCount = _gltfImporter.Skeletons.Count;

            // Convert from an array of glTFSkeleton to an array of Skeleton (base classes)
            _importedSkeletons = new List<Skeleton>(skeletonsCount);
            for (int i = 0; i < skeletonsCount; i++)
                _importedSkeletons.Add(_gltfImporter.Skeletons[i]); 
        }
    }

    private void ImportAssimpFile(string fileName, Scene scene)
    {
        if (_assimpImporter == null)
        {
            _assimpImporter = Importers.AssimpImporterSample.InitAssimpLibrary(scene.GpuDevice, this.BitmapIO, "assimp-lib", ShowErrorMessage);

            if (_assimpImporter == null)
                return;

            _assimpImporter.PreserveNativeResourcesAfterImporting = false;
        }


        // The following two properties control if animation is read by the AssimpImporter
        // By default they are already set to true, but are still written here for clearance.
        _assimpImporter.ReadAnimations = true;
        _assimpImporter.ReadSkeletalAnimation = true;
        

        _importedModelNodes = _assimpImporter.Import(fileName);
        
        if (_importedModelNodes == null)
            return;
        
        // Add importer model to the Scene
        scene.RootNode.Add(_importedModelNodes);
              

        _importedAnimations = _assimpImporter.Animations;
        _importedAnimatedSkeletonMeshes = _assimpImporter.AnimatedSkeletonMeshes;
        
        // IMPORTANT:
        // Before the animations are started, we also need to add the animated skeleton meshes to the Scene.
        // This is done by calling AddAnimatedSkeletonMeshes to get all the meshes with skeleton in some GroupNode,
        // or by calling AddAnimatedSkeletonMesh to add each mesh and its skeleton individually.
        if (_assimpImporter.AnimatedSkeletonMeshes != null)
        {
            // The easiest way to add all skeleton meshes is to call AddAnimatedSkeletonMeshes and pass the parent GroupNode.
            //scene.AddAnimatedSkeletonMeshes(_importedModelNodes);

            // But because we have a list of all meshes with skeletons, it is faster to add each individual mesh:
            foreach (var oneMesh in _assimpImporter.AnimatedSkeletonMeshes)
                scene.AddAnimatedSkeletonMesh(oneMesh, oneMesh.Skeleton!);
            
            // To remove the skeleton meshes from the scene, call
            //scene.RemoveAnimatedSkeleton, scene.RemoveAnimatedSkeletonMesh or scene.RemoveAllSkeletonAnimations
        }

        if (_assimpImporter.Skeletons != null)
        {
            var skeletonsCount = _assimpImporter.Skeletons.Count;

            // Convert from an array of AssimpSkeleton to an array of Skeleton (base classes)
            _importedSkeletons = new List<Skeleton>(skeletonsCount);
            for (int i = 0; i < skeletonsCount; i++)
                _importedSkeletons.Add(_assimpImporter.Skeletons[i]); 
        }
    }

    private void OnAnimationUpdated(object? sender, EventArgs e)
    {
        if (_selectedAnimation == null)
            return;
        
        _animationTimeRelative = _selectedAnimation.Animations.Max(a => a.Progress);

        if (!_isInternalTimeUpdate)
        {
            _isInternalTimeUpdate = true;
            _animationTimeSlider?.SetValue(_animationTimeRelative);
            _isInternalTimeUpdate = false;
        }

        UpdateAnimationTimeLabel();
    }

    private void ShowSkeletonsInfo()
    {
        if (_importedSkeletons == null)
            return;


        var sb = new StringBuilder();

        foreach (var skeleton in _importedSkeletons)
        {
            sb.AppendFormat("Skeleton '{0}'", skeleton.Name);
            sb.AppendLine();

            if (skeleton.RootSkeletonNode != null)
                AddBonesInfo(skeleton.RootSkeletonNode, sb, 0, skipNodesWithoutName: false);

            sb.AppendLine();
        }

        System.Diagnostics.Debug.WriteLine(sb.ToString());
    }

    private void AddBonesInfo(SkeletonNode skeletonNode, StringBuilder sb, int indent, bool skipNodesWithoutName)
    {
        if (skipNodesWithoutName && string.IsNullOrEmpty(skeletonNode.Name))
            return;

        string indentString = indent == 0 ? "" : new string(' ', indent);


        sb.Append(indentString)
          .Append('"' + skeletonNode.Name + '"')
          .AppendLine();


        var matrices = new List<Matrix4x4>(4);
        var matrixTitles = new List<string>(4);

        matrices.Add(skeletonNode.CurrentNodeMatrix);
        matrixTitles.Add("NodeMatrix:");

        matrices.Add(skeletonNode.CurrentWorldMatrix);
        matrixTitles.Add("CurrentWorldMatrix:");

        var allMatricesText = matrices.ToArray().FormatMatricesHorizontally(matrixTitles.ToArray(), new string(' ', indent));
        sb.AppendLine(allMatricesText);

        foreach (var skeletonNodeChild in skeletonNode.Children)
            AddBonesInfo(skeletonNodeChild, sb, indent + 4, skipNodesWithoutName);
    }
    
    private string[] GetImportedAnimationNames()
    {
        if (_importedAnimations == null)
            return new string[] { "" };
        
        return _importedAnimations.Select(a => string.IsNullOrEmpty(a.Name) ? "UNNAMED" : a.Name).ToArray();
    }
    
    private void SetAnimationTime(float relativeAnimationTime)
    {
        if (_isInternalTimeUpdate || _selectedAnimation == null)
            return;
        
        _animationTimeRelative = relativeAnimationTime;

        _isInternalTimeUpdate = true;
        
        for (var i = 0; i < _selectedAnimation.Animations.Length; i++)
            _selectedAnimation.Animations[i].Seek(_selectedAnimation.Duration * relativeAnimationTime);

        // After we manually updated the animation time (calling Seek), we also need to manually call UpdateAnimatedSkeletons.
        if (Scene != null)
            Scene.UpdateAnimatedSkeletons();
        
        _isInternalTimeUpdate = false;
        _isAnimationTimeManuallyChanged = true; // This will update the matrices and positions
        
        UpdateAnimationTimeLabel();
    }

    private void UpdateAnimationTimeLabel()
    {
        if (_animationTimeLabel == null)
            return;

        string animationTimeInfo = "Animation time:";

        if (_animationDuration > 0)
        {
            var currentAnimationTime = _animationTimeRelative * _animationDuration;
            var timeSpan = TimeSpan.FromMilliseconds(currentAnimationTime);
            animationTimeInfo += $" {timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}.{(timeSpan.Milliseconds/10):D2}";
        }

        _animationTimeLabel.SetText(animationTimeInfo);
    }

    private void ChangeSelectedAnimation(int selectedAnimationIndex)
    {
        if (_importedAnimations == null || selectedAnimationIndex < 0 || _importedAnimations.Count <= selectedAnimationIndex)
            return;
        
        if (_subscribedAnimation != null)
            _subscribedAnimation.Updated -= OnAnimationUpdated;
            
        _selectedAnimation = _importedAnimations[selectedAnimationIndex];

        _subscribedAnimation = _selectedAnimation.Animations[0];
        _subscribedAnimation.Updated += OnAnimationUpdated;
            
        for (var i = 0; i < _selectedAnimation.Animations.Length; i++)
        {
            // Initialize the animation to calculate the Duration value
            _selectedAnimation.Animations[i].Initialize();
            _animationDuration = MathF.Max(_animationDuration, _selectedAnimation.Animations[i].TotalDuration);
        }
    }

    private bool IsAnyAnimationRunning()
    {
        if (_selectedAnimation == null)
            return false;

        return _selectedAnimation.Animations.Any(a => a.IsRunning);
    }
    
    private void UpdateStartStopAnimationButton()
    {
        if (_startStopAnimationButton == null)
            return;

        _startStopAnimationButton.SetText(IsAnyAnimationRunning() ? "Stop animation" : "Start animation");
    }

    private void StartAnimation()
    {
        if (_selectedAnimation == null)
            return;

        // IMPORTANT:
        // Before the animations are started, we also need to add the animated skeleton meshes to the Scene.
        // This is done by calling AddAnimatedSkeletonMeshes to get all the meshes with skeleton in some GroupNode,
        // or by calling AddAnimatedSkeletonMesh to add each mesh and its skeleton individually.
        // See the code after the objects are imported.
        
        for (var i = 0; i < _selectedAnimation.Animations.Length; i++)
        {
            _selectedAnimation.Animations[i].Stop();
            _selectedAnimation.Animations[i].Rewind();
            _selectedAnimation.Animations[i].Start();
        }
        
        _isAnimationStarted = true;
            
        UpdateStartStopAnimationButton();
    }
    
    private void StopAnimation()
    {
        if (_selectedAnimation == null)
            return;

        for (var i = 0; i < _selectedAnimation.Animations.Length; i++)
            _selectedAnimation.Animations[i].Stop();

        _isAnimationStarted = false;
            
        UpdateStartStopAnimationButton();
    }

    private void UpdateBoneMarkers()
    {
        if (!_showBones)
        {
            if (_boneMarkersGroupNode != null && Scene != null)
            {
                _skeletonNodeBoneMarkerLines.Clear();
                Scene.RootNode.Remove(_boneMarkersGroupNode);
                _boneMarkersGroupNode = null;
            }
            
            return;
        }
        

        if (_selectedAnimation == null)
            return;


        if (_boneMarkersGroupNode == null && Scene != null)
        {
            _boneMarkersGroupNode = new GroupNode("BoneMarkersGroup");
            Scene.RootNode.Add(_boneMarkersGroupNode);
        }

        if (_importedAnimatedSkeletonMeshes != null)
        {
            foreach (var animatedSkeletonMesh in _importedAnimatedSkeletonMeshes)
            {
                if (animatedSkeletonMesh.Skeleton == null)
                    continue;
                
                var rootSkeletonNode = animatedSkeletonMesh.Skeleton.RootSkeletonNode;

                if (rootSkeletonNode != null)
                {
                    Transform? parentTransform = null;

                    if (Scene != null)
                    {
                        SceneNode? usedSceneNode = null;
                        Scene.RootNode.ForEachChild<MeshModelNode>(meshModelNode =>
                        {
                            if (meshModelNode.Mesh == animatedSkeletonMesh)
                                usedSceneNode = meshModelNode;
                        });

                        if (usedSceneNode != null && !usedSceneNode.IsWorldMatrixIdentity)
                            parentTransform = new MatrixTransform(usedSceneNode.WorldMatrix);
                    }

                    AddBoneMarkers(rootSkeletonNode, lineThickness: 6, parentTransform);
                }
            }
        }
    }
    
    private void AddBoneMarkers(SkeletonNode skeletonNode, float lineThickness, Transform? parentTransform)
    {
        if (_boneMarkersGroupNode == null)
            return;
        
        if (skeletonNode.ParentSkeletonNode != null)
        {
            var startPositionMatrix = skeletonNode.ParentSkeletonNode.CurrentWorldMatrix;
            var endPositionMatrix   = skeletonNode.CurrentWorldMatrix;

            var startPosition = new Vector3(startPositionMatrix.M41, startPositionMatrix.M42, startPositionMatrix.M43);
            var endPosition = new Vector3(endPositionMatrix.M41, endPositionMatrix.M42, endPositionMatrix.M43);

            if (Vector3.DistanceSquared(startPosition, endPosition) > 0)
            {
                // Try to reuse the LineNodes for bone markers
                if (_skeletonNodeBoneMarkerLines.TryGetValue(skeletonNode, out var lineNode))
                {
                    lineNode.StartPosition = startPosition;
                    lineNode.EndPosition = endPosition;
                }
                else
                {
                    lineNode = new LineNode($"SkeletonNode_{(skeletonNode.Name ?? "")}")
                    {
                        StartPosition = startPosition,
                        EndPosition = endPosition,
                        LineThickness = lineThickness,
                        LineColor = Colors.Red,
                        EndLineCap = LineCap.ArrowAnchor,
                        Transform = parentTransform
                    };

                    _boneMarkersGroupNode.Add(lineNode);

                    _skeletonNodeBoneMarkerLines.Add(skeletonNode, lineNode);
                }

                lineThickness = Math.Max(1, lineThickness - 1); // reduce line thickness for deeper skeleton levels
            }
        }

        foreach (var childBone in skeletonNode.Children)
            AddBoneMarkers(childBone, lineThickness, parentTransform);
    }
    
    private void UpdateShownBones()
    {
        if (_importedModelNodes == null || _importedSkeletons == null)
            return;
        
        if (_showBones)
            Utilities.ModelUtils.SetMaterialOpacity(_importedModelNodes, 0.7f);
        else
            Utilities.ModelUtils.SetMaterialOpacity(_importedModelNodes, 1f);
        
        UpdateBoneMarkers();
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(alignment: PositionTypes.BottomRight);

        ui.CreateLabel("Selected animation:");
        
        var animationNames = GetImportedAnimationNames();
        _animationCombobox = ui.CreateComboBox(animationNames, 
                                               (selectedIndex, selectedText) => ChangeSelectedAnimation(selectedIndex), 
                                               _initialAnimationIndex, 
                                               width: 250);

        ui.CreateCheckBox("Show skeleton bones", _showBones, isChecked =>
        {
            _showBones = isChecked;
            UpdateShownBones();
        });
        
        ui.AddSeparator();
        

        _animationTimeLabel = ui.CreateLabel("Animation time: ");
        _animationTimeSlider = ui.CreateSlider(0, 1, () => _animationTimeRelative,
                                               newValue => SetAnimationTime(newValue),
                                               width: 250);
        
        _startStopAnimationButton = ui.CreateButton("Start animation", () =>
        {
            if (_isAnimationStarted)
                StopAnimation();
            else
                StartAnimation();
        });

        UpdateStartStopAnimationButton();

        
        ui.AddSeparator();
        
        ui.CreateButton("Dump animation to VS Output", () =>
        {
            if (_selectedAnimation == null)
                return;

            for (var i = 0; i < _selectedAnimation.Animations.Length; i++)
            {
                if (!_selectedAnimation.Animations[i].IsRunning)
                    _selectedAnimation.Animations[i].Initialize(); 
                
                var infoText = _selectedAnimation.Animations[i].GetInfoText();
                System.Diagnostics.Debug.WriteLine(infoText);
            }
        });
        
        ui.CreateButton("Dump skeletons info to VS Output", () => ShowSkeletonsInfo());
        
        
        // Try to register for file drag-and-drop
        bool isDragAndDropSupported = ui.RegisterFileDropped(".*", ImportFile);

        if (isDragAndDropSupported)
        {
            _subtitle += "Drag and drop a file here to open it.";
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