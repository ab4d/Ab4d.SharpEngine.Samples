using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.StandardModels;

public class TorusKnotModelNodeSample : StandardModelsSampleBase
{
    public override string Title => "TorusKnotModelNode";
    
    // Default values:
    private int _p = 4;
    private int _q = 3;
    private float _radius1 = 50;
    private float _radius2 = 20;
    private float _radius3 = 10;
    private int _uSegmentsCount = 200;
    private int _vSegmentsCount = 30;
    
    private ICommonSampleUIElement? _qSlider;
    private ICommonSampleUIElement? _pSlider;
    private ICommonSampleUIElement? _radius1Slider;
    private ICommonSampleUIElement? _radius2Slider;
    private ICommonSampleUIElement? _radius3Slider;

    private TorusKnotModelNode? _torusKnotModelNode;

    public TorusKnotModelNodeSample(ICommonSamplesContext context) : base(context)
    {
        isTextureCheckBoxShown = false;
        isShowTrianglesChecked = false;
        isShowNormalsChecked = false;
        isSemiTransparentMaterialChecked = false;
    }

    protected override ModelNode CreateModelNode()
    {
        _torusKnotModelNode = new TorusKnotModelNode("TorusKnot")
        {
            CenterPosition = new Vector3(0, 0, 0),
            P = _p,
            Q = _q,
            Radius1 = _radius1,
            Radius2 = _radius2,
            Radius3 = _radius3,
            USegmentsCount = _uSegmentsCount,
            VSegmentsCount = _vSegmentsCount,
        };

        // Use MeshFactory.CreatePyramidMesh to create a pyramid mesh, for example:
        //StandardMesh torusKnotMesh = MeshFactory.CreateTorusKnotMesh(centerPosition: new Vector3(0, -25, 0), _p, _q, _radius1, _radius2, _radius3, _uSegmentsCount, _vSegmentsCount, name: "PyramidMesh");

        UpdateModelNode();

        return _torusKnotModelNode;
    }

    protected override void OnCreateScene(Scene scene)
    {
        base.OnCreateScene(scene);

        var wireCrossNode = new WireCrossNode()
        {
            Position = new Vector3(0, 0, 0),
            LineColor = Colors.Red,
            LineThickness = 2,
            LinesLength = 50
        };

        scene.RootNode.Add(wireCrossNode);

        if (targetPositionCamera != null)
            targetPositionCamera.Distance = 330;
    }

    protected override void UpdateModelNode()
    {
        if (_torusKnotModelNode == null)
            return;

        _torusKnotModelNode.P              = _p;
        _torusKnotModelNode.Q              = _q;
        _torusKnotModelNode.Radius1        = _radius1;
        _torusKnotModelNode.Radius2        = _radius2;
        _torusKnotModelNode.Radius3        = _radius3;
        _torusKnotModelNode.USegmentsCount = _uSegmentsCount;
        _torusKnotModelNode.VSegmentsCount = _vSegmentsCount;

        base.UpdateModelNode();
    }

    private void CreateStandardTorus()
    {
        _p       = 1;
        _q       = 0;
        _radius1 = 0;    // Set _radius1 to prevent offsetting the center position

        // Preserve the following values
        //_radius2 = 20; // Torus radius is set by _radius2
        //_radius3 = 10; // Radius of torus tube is set by _radius2

        UpdateModelNode();

        _pSlider?.UpdateValue();
        _qSlider?.UpdateValue();
        _radius1Slider?.UpdateValue();
    }
    
    private void CreateInitialTorus()
    {
        _p       = 4;
        _q       = 3;
        _radius1 = 50;
        _radius2 = 20;
        _radius3 = 10;

        UpdateModelNode();

        _pSlider?.UpdateValue();
        _qSlider?.UpdateValue();
        _radius1Slider?.UpdateValue();
        _radius2Slider?.UpdateValue();
        _radius3Slider?.UpdateValue();
    }

    protected override void OnCreatePropertiesUI(ICommonSampleUIProvider ui)
    {
        ui.CreateKeyValueLabel("CenterPosition:", () => "(0, 0, 0)", keyTextWidth: 100).SetColor(Colors.Red);

        ui.AddSeparator();

        _pSlider = ui.CreateSlider(-5, 20,
            () => _p,
            newValue =>
            {
                if (_p == (int)newValue)
                    return; // Do no update when only decimal part of the value is changed

                _p = (int)newValue;
                UpdateModelNode();
            },
            width: 120,
            keyText: "P:",
            keyTextWidth: 100,
            formatShownValueFunc: newValue => ((int)newValue).ToString());
        
        _qSlider = ui.CreateSlider(-5, 20,
            () => _q,
            newValue =>
            {
                if (_q == (int)newValue)
                    return; // Do no update when only decimal part of the value is changed

                _q = (int)newValue;
                UpdateModelNode();
            },
            width: 120,
            keyText: "Q:",
            keyTextWidth: 100,
            formatShownValueFunc: newValue => ((int)newValue).ToString());
        
        _radius1Slider = ui.CreateSlider(0, 100,
            () => _radius1,
            newValue =>
            {
                _radius1 = (int)newValue;
                UpdateModelNode();
            },
            width: 120,
            keyText: "Radius1:",
            keyTextWidth: 100,
            formatShownValueFunc: newValue => ((int)newValue).ToString());
        
        _radius2Slider = ui.CreateSlider(0, 100,
            () => _radius2,
            newValue =>
            {
                _radius2 = (int)newValue;
                UpdateModelNode();
            },
            width: 120,
            keyText: "Radius2:",
            keyTextWidth: 100,
            formatShownValueFunc: newValue => ((int)newValue).ToString());
        
        _radius3Slider = ui.CreateSlider(0, 100,
            () => _radius3,
            newValue =>
            {
                _radius3 = (int)newValue;
                UpdateModelNode();
            },
            width: 120,
            keyText: "Radius3:",
            keyTextWidth: 100,
            formatShownValueFunc: newValue => ((int)newValue).ToString());

        ui.CreateSlider(3, 500,
            () => _uSegmentsCount,
            newValue =>
            {
                if (_uSegmentsCount == (int)newValue)
                    return; // Do no update when only decimal part of the value is changed
                
                _uSegmentsCount = (int)newValue;
                UpdateModelNode();
            },
            width: 120,
            keyText: "U Segments:",
            keyTextWidth: 100,
            formatShownValueFunc: newValue => ((int)newValue).ToString());
        
        ui.CreateSlider(3, 50,
            () => _vSegmentsCount,
            newValue =>
            {
                if (_vSegmentsCount == (int)newValue)
                    return; // Do no update when only decimal part of the value is changed
                
                _vSegmentsCount = (int)newValue;
                UpdateModelNode();
            },
            width: 120,
            keyText: "V Segments:",
            keyTextWidth: 100,
            formatShownValueFunc: newValue => ((int)newValue).ToString());

        AddMeshStatisticsControls(ui, addSharpEdgeInfo: false);
        
        ui.AddSeparator();

        ui.CreateButton("Create standard torus", () => CreateStandardTorus());
        ui.CreateButton("Create initial torus knot", () => CreateInitialTorus());
    }
}