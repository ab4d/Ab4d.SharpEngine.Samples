using System.Numerics;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.StandardModels;

public class IcosphereModelNodeSample : StandardModelsSampleBase
{
    public override string Title => "IcosphereModelNode";
    public override string Subtitle => "Icosphere represents a 3D sphere that consists of equally sized triangles.";

    private int _subdivisions = 3; // 3 is also the default value
    private bool _createTextureCoordinates = false;

    private ICommonSampleUIElement? _subdivisionsKeyValue;
    private IcosphereModelNode? _icosphereModelNode;

    public IcosphereModelNodeSample(ICommonSamplesContext context) : base(context)
    {
    }

    protected override ModelNode CreateModelNode()
    {
        _icosphereModelNode = new IcosphereModelNode("SampleSphere")
        {
            CenterPosition = new Vector3(0, 0, 0),
            Radius = 50,

            // The number of faces quadruple with every subdivision. This value must be between 0 and 8.
            // Some examples:
            // - 2: 162 positions, 320 triangles
            // - 3: 642 positions, 1280 triangles   (default value)
            // - 4: 2562 positions, 5120 triangles
            Subdivisions = _subdivisions,

            // When the texture coordinates are also generated this requires duplicating some positions and using a slower code path. Default value is true.
            CreateTextureCoordinates = _createTextureCoordinates
        };

        // Use MeshFactory.CreateSphereMesh to create a sphere mesh, for example:
        //StandardMesh icosphere = MeshFactory.CreateIcosphereMesh(centerPosition: new Vector3(0, 0, 0), radius: 50, subdivisions: _subdivisions, createTextureCoordinates: false, name: "SphereMesh");

        UpdateModelNode();

        return _icosphereModelNode;
    }

    protected override void UpdateModelNode()
    {
        if (_icosphereModelNode == null)
            return;

        _icosphereModelNode.Subdivisions = _subdivisions;
        _icosphereModelNode.CreateTextureCoordinates = _createTextureCoordinates;
        
        _subdivisionsKeyValue?.UpdateValue();

        base.UpdateModelNode();
    }

    protected override void OnIsTextureMaterialChanged(bool isChecked)
    {
        _createTextureCoordinates = isChecked;
        UpdateModelNode();

        base.OnIsTextureMaterialChanged(isChecked);
    }

    protected override void OnCreatePropertiesUI(ICommonSampleUIProvider ui)
    {
        ui.CreateKeyValueLabel("CenterPosition:", () => "(0, 0, 0)", keyTextWidth: 110);
        ui.CreateKeyValueLabel("Radius:", () => "50", keyTextWidth: 110);

        ui.AddSeparator();


        _subdivisionsKeyValue = ui.CreateKeyValueLabel("Subdivisions: (?):Subdivisions defines number of times a subdivision of the\ninitial (12 positions, 20 triangles) icosphere is subdivided.\nSome examples:\n-0: 12 positions, 20 triangles\n-1: 42 positions, 80 triangles\n-2: 162 positions, 320 triangles\n- 3: 642 positions, 1280 triangles (default value)\n- 4: 2562 positions, 5120 triangles", 
                                                       () => _subdivisions.ToString(), keyTextWidth: 110);


        ui.CreateSlider(0, 6,
                        () => _subdivisions,
                        newValue =>
                        {
                            var newSubdivisions = (int)Math.Round(newValue);
                            if (_subdivisions == newSubdivisions)
                                return; // Do no update when only decimal part of the value is changed
                            
                            _subdivisions = newSubdivisions;
                            UpdateModelNode();
                        },
                        200);

        AddMeshStatisticsControls(ui);
    }
}