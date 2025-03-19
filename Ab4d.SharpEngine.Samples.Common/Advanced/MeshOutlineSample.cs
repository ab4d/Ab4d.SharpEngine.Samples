using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

public class MeshOutlineSample : CommonSample
{
    public override string Title => "Mesh outlines created by inverted hull method";
    public override string Subtitle =>
@"This method increases the mesh geometry by moving all positions in the direction of the normal.
This works well for models with rounded edges, but is not that nice on objects with sharp edges, especially when bigger outline distance is used.";

    private bool _isDragonSelected = true;
    private bool _isSphereSelected = true;
    private bool _isBoxSelected = true;

    private float _outlineDistance = 0.7f;

    private SolidColorMaterial _outlineMaterial;

    private MeshModelNode? _dragonModelNode;
    private SphereModelNode? _sphereModelNode;
    private BoxModelNode? _boxModelNode;

    private GroupNode _objectsGroupNode;
    private GroupNode _outlinesGroupNode;

    public MeshOutlineSample(ICommonSamplesContext context)
        : base(context)
    {
        _outlineMaterial = new SolidColorMaterial(Colors.Black);

        _objectsGroupNode = new GroupNode("ObjectsGroup");
        _outlinesGroupNode = new GroupNode("OutlinesGroup");
    }

    protected override void OnCreateScene(Scene scene)
    {
        var dragonMesh = TestScenes.GetTestMesh(TestScenes.StandardTestScenes.Dragon, position: new Vector3(-50, 0, 0), positionType: PositionTypes.Bottom, finalSize: new Vector3(100, 100, 100));

        _dragonModelNode = new MeshModelNode(dragonMesh, StandardMaterials.Silver, "DragonModel");
        _objectsGroupNode.Add(_dragonModelNode);


        _sphereModelNode = new SphereModelNode(centerPosition: new Vector3(50, 35, 0), radius: 30, StandardMaterials.Silver.SetSpecular(specularPower: 32), "SphereModel");
        _objectsGroupNode.Add(_sphereModelNode);

        _boxModelNode = new BoxModelNode(centerPosition: new Vector3(0, -15, 0), size: new Vector3(300, 20, 130), StandardMaterials.Green, "BoxModel");
        _objectsGroupNode.Add(_boxModelNode);

        scene.RootNode.Add(_objectsGroupNode);


        RecreateOutlines();

        scene.RootNode.Add(_outlinesGroupNode);

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 25;
            targetPositionCamera.Attitude = -20;
            targetPositionCamera.Distance = 500;
        }
    }
    
    private void RecreateOutlines()
    {
        _outlinesGroupNode.Clear();

        if (_isDragonSelected)
            AddOutlineMesh(_dragonModelNode);

        if (_isSphereSelected)
            AddOutlineMesh(_sphereModelNode);

        if (_isBoxSelected)
            AddOutlineMesh(_boxModelNode);
    }

    private void AddOutlineMesh(ModelNode? modelNode)
    {
        if (modelNode == null)
            return;

        var outlineMesh = CreateOutlineMesh(modelNode, _outlineDistance, meshName: modelNode.Name + "OutlineMesh");

        if (outlineMesh == null)
            return;


        var outlineNode = new MeshModelNode(outlineMesh, _outlineMaterial, modelNode.Name + "OutlineNode");

        if (Scene != null)
            outlineNode.CustomRenderingLayer = Scene.BackgroundRenderingLayer;

        _outlinesGroupNode.Add(outlineNode);
    }

    public static TriangleMesh<Vector3>? CreateOutlineMesh(ModelNode modelNode, float outlineDistance, string? meshName = null)
    {
        var standardMesh = modelNode.GetMesh() as StandardMesh;

        if (standardMesh == null)
            return null;

        var meshTransform = modelNode.GetMeshTransform();

        return MeshUtils.CreateOutlinePositionsMesh(standardMesh, outlineDistance, meshTransform, meshName);
    }
    
    /// <inheritdoc />
    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("Objects with outline:");

        ui.CreateCheckBox("Dragon", _isDragonSelected, isChecked =>
        {
            _isDragonSelected = isChecked;
            RecreateOutlines();
        });

        ui.CreateCheckBox("Sphere", _isSphereSelected, isChecked =>
        {
            _isSphereSelected = isChecked;
            RecreateOutlines();
        });

        ui.CreateCheckBox("Box", _isBoxSelected, isChecked =>
        {
            _isBoxSelected = isChecked;
            RecreateOutlines();
        });

        
        ui.AddSeparator();

        var distanceLabel = ui.CreateKeyValueLabel("Distance: ", () => _outlineDistance.ToString("F1"));

        ui.CreateSlider(0, 5, () => _outlineDistance, newValue =>
        {
            _outlineDistance = newValue;
            distanceLabel.UpdateValue();

            RecreateOutlines();
        });

        

        ui.AddSeparator();

        ui.CreateCheckBox("Is orange outline", false, isChecked =>
        {
            if (isChecked)
                _outlineMaterial = new SolidColorMaterial(Colors.Orange);
            else
                _outlineMaterial = new SolidColorMaterial(Colors.Black);

            ModelUtils.ChangeMaterial(_outlinesGroupNode, _outlineMaterial);
        });
    }
}