using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

public class FrustumCullingSample : CommonSample
{
    public override string Title => "Frustum culling";
    public override string Subtitle => "Frustum culling can be used to determine if object is visible by the current camera.\n\nInstead of checking individual objects (as done in this sample, but when thousand of objects are checked, that can take a long time), it is recommended to group objects into GroupNodes and then check only the visibility of GroupNodes.";

    private bool _isFrustumCullingEnabled = true;

    private GroupNode _culledObjectsGroup = new GroupNode("CulledObjects");

    private Material _fullyVisibleMaterial;
    private Material _partiallyVisibleMaterial;
    private Material _hiddenMaterial;
    
    private int _visibleCount;
    private int _partiallyVisibleCount;
    private int _notVisibleCount;
    private int _allObjectsCount;

    private ICommonSampleUIElement? _visibleLabel;
    private ICommonSampleUIElement? _partiallyVisibleLabel;
    private ICommonSampleUIElement? _notVisibleLabel;
    

    public FrustumCullingSample(ICommonSamplesContext context)
        : base(context)
    {
        _fullyVisibleMaterial     = StandardMaterials.Green;
        _partiallyVisibleMaterial = StandardMaterials.Orange;
        _hiddenMaterial           = StandardMaterials.Red.SetOpacity(0.3f);
    }

    protected override void OnCreateScene(Scene scene)
    {
        var halfBoxSize = 5;
        var boxSize = new Vector3(halfBoxSize * 2, halfBoxSize * 2, halfBoxSize * 2);

        for (int x = 0; x < 10; x++)
        {
            for (int z = 0; z < 10; z++)
            {
                for (int y = 0; y < 4; y++)
                {
                    var centerPosition = new Vector3(-200 + 40 * x, y * 40, -200 + 40 * z);

                    var boxModelNode = new BoxModelNode(centerPosition, boxSize, _fullyVisibleMaterial);
                    _culledObjectsGroup.Add(boxModelNode);
                }
            }
        }

        _allObjectsCount = _culledObjectsGroup.Count;

        scene.RootNode.Add(_culledObjectsGroup);

        // Manually call scene.Update. This will calculate WorldBoundingBox for each SceneNode.
        // This is needed for correct frustum checks.
        scene.Update();

        if (targetPositionCamera != null)
        {
            targetPositionCamera.TargetPosition = new Vector3(-26, 113, -22);
            targetPositionCamera.Heading = -100;
            targetPositionCamera.Attitude = -11;
            targetPositionCamera.Distance = 180;
            targetPositionCamera.CameraChanged += OnCameraChanged;
        }
    }

    private void OnCameraChanged(object? sender, EventArgs args)
    {
        UpdateVisibleBoxes();
    }

    /// <inheritdoc />
    protected override void OnDisposed()
    {
        if (targetPositionCamera != null)
            targetPositionCamera.CameraChanged -= OnCameraChanged;

        base.OnDisposed();
    }

    private void UpdateVisibleBoxes()
    {
        if (!_isFrustumCullingEnabled || Scene == null || SceneView?.Camera == null)
            return;


        // Before creating the BoundingFrustum we need to update the near and far planes so that 
        // the frustum will already incorporate the camera changes.
        SceneView.UpdateCameraNearAndFarPlanes();
        
        //
        // IMPORTANT TIP:
        //
        // When you have many 3D objects, do not check each object if it is visible or not.
        // Instead, group the objects into lower number of groups (into GroupNode objects).
        // Then calculate the bounding box of each group and hide the GroupNodes that are not visible.

        // Create BoundingFrustum from the current camera
        // BoundingFrustum is a struct, so here we do not create any new objects that would add pressure to GC
        var boundingFrustum = BoundingFrustum.FromCamera(SceneView.Camera, Scene.IsRightHandedCoordinateSystem);


        bool hasChanges = false;

        foreach (var modelNode in _culledObjectsGroup.OfType<ModelNode>())        
        {
            // Check if the modelNode is visible in the boundingFrustum.
            // Note that Contains method needs to perform any operations and because of this
            // it may take significant amount of time to do this check for thousands of objects.
            // In this case it is highly recommended to group object into GroupNodes and
            // then only check the visibilities of the GroupNodes.
            var frustumVisibility = boundingFrustum.Contains(modelNode.WorldBoundingBox);

            // NOTE:
            // Usually you would set Visibility of the modelNode based on frustumVisibility (see commented line below).
            // But in this sample we just change the material, so when you uncheck the "Is frustum culling enabled", 
            // you can see which objects would be hidden.
            // modelNode.Visibility = frustumVisibility != ContainmentType.Disjoint ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;

            var newMaterial = frustumVisibility switch
            {
                ContainmentType.Disjoint   => _hiddenMaterial,
                ContainmentType.Contains   => _fullyVisibleMaterial,
                ContainmentType.Intersects => _partiallyVisibleMaterial,
                _ => null
            };

            hasChanges |= modelNode.Material != newMaterial;
            modelNode.Material = newMaterial; // This is a noop when we are setting the material to the same material
        }

        if (hasChanges)
            UpdateStatistics();
    }

    private void UpdateStatistics()
    {
        _visibleCount = 0;
        _partiallyVisibleCount = 0;
        _notVisibleCount = 0;

        foreach (var modelNode in _culledObjectsGroup.OfType<ModelNode>())        
        {
            if (modelNode.Material == _fullyVisibleMaterial)
                _visibleCount++;
            else if (modelNode.Material == _partiallyVisibleMaterial)
                _partiallyVisibleCount++;
            else if (modelNode.Material == _hiddenMaterial)
                _notVisibleCount++;
        }

        _visibleLabel?.UpdateValue();
        _partiallyVisibleLabel?.UpdateValue();
        _notVisibleLabel?.UpdateValue();
    }

    private string GetCountWithPercent(int count) => $"{count} ({((count * 100) / _allObjectsCount):N0}%)";

    /// <inheritdoc />
    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateCheckBox("Is frustum culling enabled (?):Uncheck and then zoom out the camera to see which objects\nwere not visible in the frustum (have red material).", _isFrustumCullingEnabled, isChecked =>
        {
            _isFrustumCullingEnabled = isChecked;
            if (isChecked)
                UpdateVisibleBoxes();
        });

        ui.CreateLabel("Statistics:", isHeader: true);
        _visibleLabel          = ui.CreateKeyValueLabel("Visible:",           () => GetCountWithPercent(_visibleCount), 120).SetColor(Colors.Green);
        _partiallyVisibleLabel = ui.CreateKeyValueLabel("Partially visible:", () => GetCountWithPercent(_partiallyVisibleCount), 120).SetColor(Colors.Orange);
        _notVisibleLabel       = ui.CreateKeyValueLabel("Not visible:",       () => GetCountWithPercent(_notVisibleCount), 120).SetColor(Colors.Red);

        UpdateStatistics();
    }
}