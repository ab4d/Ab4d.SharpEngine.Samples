using System.Numerics;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.Vulkan;

namespace Ab4d.SharpEngine.Samples.Common.StandardModels;

public class CylinderModelNodeSample : StandardModelsSampleBase
{
    public override string Title => "CylinderModelNode";

    private int _segmentsCount = 15;
    private bool _isSmooth = false;
    private float _radius = 50;
    private float _height = 50;

    private CylinderModelNode? _cylinderModelNode;

    public CylinderModelNodeSample(ICommonSamplesContext context) : base(context)
    {
    }

    protected override ModelNode CreateModelNode()
    {
        _cylinderModelNode = new CylinderModelNode("SampleCylinder")
        {
            BottomCenterPosition = new Vector3(0, -25, 0),
            Radius = 50,
            Height = 50,
        };

        // Use MeshFactory.CreateCylinderMesh to create a cylinder mesh, for example:
        //StandardMesh cylinderMesh = MeshFactory.CreateCylinderMesh(bottomCenterPosition: new Vector3(0, -25, 0), radius: 50, height: 50, _segmentsCount, _isSmooth, name: "CylinderMesh");

        UpdateModelNode();

        return _cylinderModelNode;
    }

    protected override void UpdateModelNode()
    {
        if (_cylinderModelNode == null)
            return;

        _cylinderModelNode.Segments = _segmentsCount;
        _cylinderModelNode.IsSmooth = _isSmooth;
        _cylinderModelNode.Radius = _radius;
        _cylinderModelNode.Height = _height;
        
        base.UpdateModelNode();
    }

    protected override void OnCreatePropertiesUI(ICommonSampleUIProvider ui)
    {
        ui.CreateLabel("BottomCenterPosition: (0, -25, 0)");

        ui.AddSeparator();

        ui.CreateCheckBox("IsSmooth", false, OnIsSmoothChanged);

        ui.AddSeparator();
        ui.CreateSlider(0, 80,
            () => _height,
            newValue =>
            {
                _height = newValue;
                UpdateModelNode();
            },
            120,
            keyText: "Height: ",
            keyTextWidth: 100,
            formatShownValueFunc: newValue => newValue.ToString("F0"));
        
        ui.AddSeparator();
        ui.CreateSlider(0, 60,
            () => _radius,
            newValue =>
            {
                _radius = newValue;
                UpdateModelNode();
            },
            120,
            keyText: "Radius: ",
            keyTextWidth: 100,
            formatShownValueFunc: newValue => newValue.ToString("F0"));

        ui.AddSeparator();
        ui.CreateSlider(3, 40,
                        () => _segmentsCount,
                        newValue =>
                        {
                            _segmentsCount = (int)newValue;
                            UpdateModelNode();
                        },
                        120,
                        keyText: "Segments: ",
                        keyTextWidth: 100,
                        formatShownValueFunc: newValue => ((int)newValue).ToString());

        ui.CreateLabel("(Default value for Segments is 30)").SetStyle("italic");

        ui.CreateLabel(
@"Cylinder is always oriented along Y axis (up).
If you want to define cylinder in some other 
direction use TubeLine").SetStyle("italic");
    }

    private void OnIsSmoothChanged(bool isChecked)
    {
        _isSmooth = isChecked;
        UpdateModelNode();
    }
}