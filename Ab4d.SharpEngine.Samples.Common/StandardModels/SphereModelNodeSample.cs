using System.Numerics;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.StandardModels;

public class SphereModelNodeSample : StandardModelsSampleBase
{
    public override string Title => "SphereModelNode";

    private int _sphereSegmentsCount = 15;

    private ICommonSampleUIElement? _sphereSegmentsKeyValue;
    private SphereModelNode? _sphereModelNode;

    public SphereModelNodeSample(ICommonSamplesContext context) : base(context)
    {
    }

    protected override ModelNode CreateModelNode()
    {
        _sphereModelNode = new SphereModelNode("SampleSphere")
        {
            CenterPosition = new Vector3(0, 0, 0),
            Radius = 50,
            UseSharedSphereMesh = true // When true (by default) then a shared sphere mesh is used for all SphereModelNode - then the sphere is transformed to be positioned and scaled; when false then a new mesh is generated for this SphereModelNode
        };

        // Use MeshFactory.CreateSphereMesh to create a sphere mesh, for example:
        //StandardMesh sphere = MeshFactory.CreateSphereMesh(centerPosition: new Vector3(0, 0, 0), radius: 50, segments: 30, name: "SphereMesh");

        // Use GetSharedSphereMesh to get a shared sphere mesh with centerPosition: (0, 0, 0), radius: 1 and segments count set to 30.
        // This mesh cannot be changed and is by default used for SphereModelNode (when UseSharedSphereMesh is true - by default).
        //var sharedMesh = MeshFactory.GetSharedSphereMesh(Scene);

        UpdateModelNode();

        return _sphereModelNode;
    }

    protected override void UpdateModelNode()
    {
        if (_sphereModelNode == null)
            return;

        _sphereModelNode.Segments = _sphereSegmentsCount;
        
        _sphereSegmentsKeyValue?.UpdateValue();

        base.UpdateModelNode();
    }

    protected override void OnCreatePropertiesUI(ICommonSampleUIProvider ui)
    {
        ui.CreateKeyValueLabel("CenterPosition:", () => "(0, 0, 0)", keyTextWidth: 110);
        ui.CreateKeyValueLabel("Radius:", () => "50", keyTextWidth: 110);

        ui.AddSeparator();

        //ui.CreateSlider(3, 40,
        //    () => _sphereSegmentsCount,
        //    newValue =>
        //    {
        //        _sphereSegmentsCount = (int)newValue;
        //        UpdateModelNode();
        //    },
        //    100,
        //    keyText: "Segments:",
        //    formatShownValueFunc: newValue => newValue.ToString("F0"));

        _sphereSegmentsKeyValue = ui.CreateKeyValueLabel("Segments:", () => _sphereSegmentsCount.ToString(), keyTextWidth: 110);

        ui.CreateSlider(3, 40,
                        () => _sphereSegmentsCount,
                        newValue =>
                        {
                            _sphereSegmentsCount = (int)newValue;
                            UpdateModelNode();
                        },
                        200);

        ui.CreateLabel("(Default value for Segments is 30)").SetStyle("italic");
    }
}