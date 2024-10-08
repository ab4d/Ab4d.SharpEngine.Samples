using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

public class TrianglesSortingSample : CommonSample
{
    public override string Title => "Triangles sorting";
    public override string Subtitle => "Upper model: SORTED triangles\nLower model: UNSORTED triangles";

    private StandardMesh? _meshToSort;
    private MeshTrianglesSorter? _meshTrianglesSorter;

    private bool _isSortingEnabled = true;

    private int _sortCount;

    private ICommonSampleUIElement? _sortButton;
    private ICommonSampleUIElement _sortedCountLabel;

    public TrianglesSortingSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        ShowModel(modelIndex: 0);

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 150;
            targetPositionCamera.Attitude = -10;
            targetPositionCamera.Distance = 500;

            targetPositionCamera.CameraChanged += TargetPositionCameraOnCameraChanged;
        }
    }

    /// <inheritdoc />
    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        sceneView.BackgroundColor = Colors.White;

        base.OnSceneViewInitialized(sceneView);
    }

    /// <inheritdoc />
    protected override void OnDisposed()
    {
        if (targetPositionCamera != null)
            targetPositionCamera.CameraChanged -= TargetPositionCameraOnCameraChanged;

        if (SceneView != null)
            SceneView.BackgroundColor = Colors.Transparent; // Revert to default

        base.OnDisposed();
    }

    private void TargetPositionCameraOnCameraChanged(object? sender, EventArgs e)
    {
        if (_isSortingEnabled)
            SortTriangles();
    }


    private void SortTriangles()
    {
        if (targetPositionCamera == null || _meshToSort == null)
            return;


        var cameraPosition = targetPositionCamera.GetCameraPosition();

        if (_meshTrianglesSorter == null)
            _meshTrianglesSorter = new MeshTrianglesSorter(_meshToSort.Vertices!, _meshToSort.TriangleIndices!);

        // SortTrianglesByCameraDistance method will sort triangles so that the triangles that are farther away from the camera will be rendered first.
        // The triangles are defined by Vertices and TriangleIndices that were set in the constructor.
        // The method returns an array of integers that represents a sorted triangle indices array.
        // Note that this array is reused and the same instance is returned after each call to SortByCameraDistance.
        var sortedTriangleIndices = _meshTrianglesSorter.SortByCameraDistance(cameraPosition, checkIfAlreadySorted: true, out bool isSorted);

        if (_meshToSort.TriangleIndices != sortedTriangleIndices)
        {
            // After first sort, change the TriangleIndices to the sorted array.
            // This will also call RecreateIndexBuffer.
            _meshToSort.TriangleIndices = sortedTriangleIndices; 
        }
        else if (_meshToSort.Scene != null && _meshToSort.Scene.GpuDevice != null)
        {
            // When the TriangleIndices is already set to the sortedTriangleIndices,
            // then we need to manually recreate the used IndexBuffer.
            _meshToSort.RecreateIndexBuffer();

            // We could also call UpdateMesh, but this would also recreate the vertex buffer. But this is not needed.
            //_meshToSort.UpdateMesh();
        }

        if (isSorted) // If the camera is not changed enough then the triangles order may not be changed
        {
            _sortCount++;
            _sortedCountLabel?.UpdateValue();
        }
    }

    private void ShowModel(int modelIndex)
    {
        if (Scene == null)
            return;

        var mesh1 = GetMesh(modelIndex);

        // Get the same mesh again. This gets new vertices and triangle indices.        
        var mesh2 = GetMesh(modelIndex);

        if (mesh1 == null || mesh2 == null)
            return;

        
        Scene.RootNode.Clear();

        var material = StandardMaterials.DimGray.SetOpacity(0.5f);

        var meshModelNode1 = new MeshModelNode(mesh1, material);
        meshModelNode1.BackMaterial = material;
        
        ModelUtils.PositionAndScaleSceneNode(meshModelNode1, position: new Vector3(0, 60, 0), positionType: PositionTypes.Center, finalSize: new Vector3(1000, 100, 1000));

        Scene.RootNode.Add(meshModelNode1);
        
        
        var meshModelNode2 = new MeshModelNode(mesh2, material);
        meshModelNode2.BackMaterial = material;

        ModelUtils.PositionAndScaleSceneNode(meshModelNode2, position: new Vector3(0, -60, 0), positionType: PositionTypes.Center, finalSize: new Vector3(1000, 100, 1000));

        Scene.RootNode.Add(meshModelNode2);

        
        _meshToSort = mesh1;
        _meshTrianglesSorter = null; // Set _meshTrianglesSorter to null so we will create a new instance on next sort

        SortTriangles();
    }
    
    private StandardMesh? GetMesh(int modelIndex)
    {
        StandardMesh? mesh = modelIndex switch
        {
            0 => MeshFactory.CreateTorusKnotMesh(centerPosition: new Vector3(0, 0, 0), p: 5, q: 3, radius1: 40, radius2: 20, radius3: 7, uSegmentsCount: 300, vSegmentsCount: 30),
            1 => TestScenes.GetTestMesh(TestScenes.StandardTestScenes.Teapot, new Vector3(100, 100, 100)),
            2 => TestScenes.GetTestMesh(TestScenes.StandardTestScenes.Dragon, new Vector3(100, 100, 100)),
            _ => null
        };

        return mesh;
    }

    /// <inheritdoc />
    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateComboBox(new string[] { "Torus", "Teapot", "Dragon" }, (selectedIndex, selectedText) => ShowModel(selectedIndex), selectedItemIndex: 0);

        ui.AddSeparator();

        ui.CreateCheckBox("Is automatically sorting", _isSortingEnabled, isChecked =>
        {
            _isSortingEnabled = isChecked;

            if (_sortButton != null)
                _sortButton.SetIsVisible(!isChecked);
        });

        _sortButton = ui.CreateButton("Sort upper mesh", () => SortTriangles()).SetIsVisible(false);

        ui.AddSeparator();

        _sortedCountLabel = ui.CreateKeyValueLabel("Mesh sorted count: ", () => _sortCount.ToString());
    }
}