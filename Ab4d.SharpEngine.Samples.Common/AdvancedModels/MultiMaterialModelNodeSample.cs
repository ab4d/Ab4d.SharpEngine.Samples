using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class MultiMaterialModelNodeSample : CommonSample
{
    public override string Title => "MultiMaterialModelNode";
    public override string Subtitle => "Show multiple materials on the same mesh";

    private SubMesh? _redSubMesh;
    private SubMesh? _greenSubMesh;
    private SubMesh? _blueSubMesh;
    private MultiMaterialModelNode? _multiMaterialModelNode;

    private int _triangleIndicesPerSubMesh;
    private int _sphereTriangleIndicesCount;
    private bool _isInternalChange;

    private ICommonSampleUIElement? _redSubMeshIndexCountSlider;
    private ICommonSampleUIElement? _redSubMeshStartIndexLocationSlider;
    private ICommonSampleUIElement? _greenSubMeshStartIndexLocationSlider;
    private ICommonSampleUIElement? _greenSubMeshIndexCountSlider;
    private ICommonSampleUIElement? _blueSubMeshStartIndexLocationSlider;
    private ICommonSampleUIElement? _blueSubMeshIndexCountSlider;

    private StandardMaterial _backMaterial;

    public MultiMaterialModelNodeSample(ICommonSamplesContext context)
        : base(context)
    {
        _backMaterial = StandardMaterials.Gray;
    }

    protected override void OnCreateScene(Scene scene)
    {
        var sphereMesh = MeshFactory.CreateSphereMesh(new Vector3(0, 0, 0), 50, 40);

        _multiMaterialModelNode = new MultiMaterialModelNode(sphereMesh, "SphereMultiMaterialModelNode");

        _sphereTriangleIndicesCount = sphereMesh.TrianglesCount * 3;
        _triangleIndicesPerSubMesh = sphereMesh.TrianglesCount;

        _redSubMesh = _multiMaterialModelNode.AddSubMesh(startIndexLocation: 0, 
                                                         indexCount: _triangleIndicesPerSubMesh, 
                                                         material:StandardMaterials.Red, 
                                                         backMaterial: _backMaterial);

        _greenSubMesh = _multiMaterialModelNode.AddSubMesh(startIndexLocation: _triangleIndicesPerSubMesh, 
                                                           indexCount: _triangleIndicesPerSubMesh, 
                                                           material:StandardMaterials.Green, 
                                                           backMaterial: _backMaterial);

        _blueSubMesh = _multiMaterialModelNode.AddSubMesh(startIndexLocation: _triangleIndicesPerSubMesh * 2, 
                                                          indexCount: _triangleIndicesPerSubMesh, 
                                                          material:StandardMaterials.Blue, 
                                                          backMaterial: _backMaterial);

        scene.RootNode.Add(_multiMaterialModelNode);


        if (targetPositionCamera != null)
        {
            targetPositionCamera.Attitude = -20;
            targetPositionCamera.Distance = 200;
        }
    }

    private void AddRemoveSubMesh(SubMesh? subMesh, bool addSubMesh)
    {
        if (subMesh == null || _multiMaterialModelNode == null)
            return;

        if (addSubMesh)
            _multiMaterialModelNode.AddSubMesh(subMesh);
        else
            _multiMaterialModelNode.RemoveSubMesh(subMesh, checkMaterials: false); // set checkMaterials to prevent removing the material from the _multiMaterialModelNode
    }

    private void SetSubMeshStartIndexLocation(SubMesh? subMesh, int startIndexLocation, ICommonSampleUIElement? otherSliderUIElement)
    {
        if (subMesh == null || _isInternalChange)
            return;

        startIndexLocation -= startIndexLocation % 3; // align to number that is dividable by 3 so we start at correct triangle index (at the start of triangle)

        subMesh.StartIndexLocation = startIndexLocation;

        if (subMesh.StartIndexLocation + subMesh.IndexCount > _sphereTriangleIndicesCount)
        {
            _isInternalChange = true;
            subMesh.IndexCount = _sphereTriangleIndicesCount - startIndexLocation;
            otherSliderUIElement?.UpdateValue();
            _isInternalChange = false;
        }
    }

    private void SetSubMesIndexCount(SubMesh? subMesh, int indexCount, ICommonSampleUIElement? otherSliderUIElement)
    {
        if (subMesh == null || _isInternalChange)
            return;

        indexCount -= indexCount % 3; // align to number that is dividable by 3 so we start at correct triangle index (at the start of triangle)

        subMesh.IndexCount = indexCount;

        if (subMesh.StartIndexLocation + subMesh.IndexCount > _sphereTriangleIndicesCount)
        {
            _isInternalChange = true;
            subMesh.StartIndexLocation = _sphereTriangleIndicesCount - indexCount;
            otherSliderUIElement?.UpdateValue();
            _isInternalChange = false;
        }
    }
    
    private void ResetSubMeshes()
    {
        if (_multiMaterialModelNode == null || _redSubMesh == null || _greenSubMesh == null || _blueSubMesh == null)
            return;

        _isInternalChange = true;

        _redSubMesh.StartIndexLocation = 0;
        _redSubMesh.IndexCount = _triangleIndicesPerSubMesh;
        
        _greenSubMesh.StartIndexLocation = _triangleIndicesPerSubMesh;
        _greenSubMesh.IndexCount = _triangleIndicesPerSubMesh;

        _blueSubMesh.StartIndexLocation = _triangleIndicesPerSubMesh * 2;
        _blueSubMesh.IndexCount = _triangleIndicesPerSubMesh;

        _multiMaterialModelNode.AddSubMesh(_redSubMesh); // Adding already added SubMesh does not change anythings
        _multiMaterialModelNode.AddSubMesh(_greenSubMesh);
        _multiMaterialModelNode.AddSubMesh(_blueSubMesh);

        _redSubMeshStartIndexLocationSlider?.UpdateValue();
        _redSubMeshIndexCountSlider?.UpdateValue();

        _greenSubMeshStartIndexLocationSlider?.UpdateValue();
        _greenSubMeshIndexCountSlider?.UpdateValue();

        _blueSubMeshStartIndexLocationSlider?.UpdateValue();
        _blueSubMeshIndexCountSlider?.UpdateValue();

        _isInternalChange = false;
    }
    
    private void ShowBackMaterial(bool isBackMaterialShown)
    {
        if (_multiMaterialModelNode == null || _redSubMesh == null || _greenSubMesh == null || _blueSubMesh == null)
            return;

        if (isBackMaterialShown)
        {
            var backMaterialIndex = _multiMaterialModelNode.GetMaterialIndex(_backMaterial);

            _redSubMesh.BackMaterialIndex   = backMaterialIndex;
            _greenSubMesh.BackMaterialIndex = backMaterialIndex;
            _blueSubMesh.BackMaterialIndex  = backMaterialIndex;
        }
        else
        {
            _redSubMesh.BackMaterialIndex   = -1; // remove back material
            _greenSubMesh.BackMaterialIndex = -1;
            _blueSubMesh.BackMaterialIndex  = -1;
        }
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("Render one sphere mesh\nby using three SubMeshes:");
        ui.AddSeparator();

        ui.CreateCheckBox("SubMesh[0]: Red", true, isChecked => AddRemoveSubMesh(_redSubMesh, isChecked));

        _redSubMeshStartIndexLocationSlider = ui.CreateSlider(0, _sphereTriangleIndicesCount, () => _redSubMesh?.StartIndexLocation ?? 0, newValue => SetSubMeshStartIndexLocation(_redSubMesh, (int)newValue, _redSubMeshIndexCountSlider), 100, keyText: "StartIndexLocation", keyTextWidth: 120, formatShownValueFunc: newValue => $"{newValue:F0}");
        _redSubMeshIndexCountSlider = ui.CreateSlider(0, _sphereTriangleIndicesCount, () => _redSubMesh?.IndexCount ?? 0, newValue => SetSubMesIndexCount(_redSubMesh, (int)newValue, _redSubMeshStartIndexLocationSlider), 100, keyText: "IndexCount", keyTextWidth: 120, formatShownValueFunc: newValue => $"{newValue:F0}");

        ui.AddSeparator();
        ui.CreateCheckBox("SubMesh[1]: Green", true, isChecked => AddRemoveSubMesh(_greenSubMesh, isChecked));

        _greenSubMeshStartIndexLocationSlider = ui.CreateSlider(0, _sphereTriangleIndicesCount, () => _greenSubMesh?.StartIndexLocation ?? 0, newValue => SetSubMeshStartIndexLocation(_greenSubMesh, (int)newValue, _greenSubMeshIndexCountSlider), 100, keyText: "StartIndexLocation", keyTextWidth: 120, formatShownValueFunc: newValue => $"{newValue:F0}");
        _greenSubMeshIndexCountSlider = ui.CreateSlider(0, _sphereTriangleIndicesCount, () => _greenSubMesh?.IndexCount ?? 0, newValue => SetSubMesIndexCount(_greenSubMesh, (int)newValue, _greenSubMeshStartIndexLocationSlider), 100, keyText: "IndexCount", keyTextWidth: 120, formatShownValueFunc: newValue => $"{newValue:F0}");

        ui.AddSeparator();
        ui.CreateCheckBox("SubMesh[2]: Blue", true, isChecked => AddRemoveSubMesh(_blueSubMesh, isChecked));

        _blueSubMeshStartIndexLocationSlider = ui.CreateSlider(0, _sphereTriangleIndicesCount, () => _blueSubMesh?.StartIndexLocation ?? 0, newValue => SetSubMeshStartIndexLocation(_blueSubMesh, (int)newValue, _blueSubMeshIndexCountSlider), 100, keyText: "StartIndexLocation", keyTextWidth: 120, formatShownValueFunc: newValue => $"{newValue:F0}");
        _blueSubMeshIndexCountSlider = ui.CreateSlider(0, _sphereTriangleIndicesCount, () => _blueSubMesh?.IndexCount ?? 0, newValue => SetSubMesIndexCount(_blueSubMesh, (int)newValue, _blueSubMeshStartIndexLocationSlider), 100, keyText: "IndexCount", keyTextWidth: 120, formatShownValueFunc: newValue => $"{newValue:F0}");

        ui.AddSeparator();
        ui.CreateButton("Reset SubMeshes", () => ResetSubMeshes());

        ui.AddSeparator();
        ui.CreateCheckBox("Show gray back material", true, isChecked => ShowBackMaterial(isChecked));
    }
}