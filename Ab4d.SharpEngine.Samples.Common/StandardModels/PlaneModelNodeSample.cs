﻿using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.StandardModels;

public class PlaneModelNodeSample : StandardModelsSampleBase
{
    public override string Title => "PlaneModelNode";

    private int _widthSegmentsCount = 5;
    private int _heighSegmentsCount = 4;

    private Vector3 _normal = new Vector3(0, 1, 0);
    private Vector3 _heightDirection = new Vector3(0, 0, -1);

    private PlaneModelNode? _planeModelNode;

    private LineNode? _normalLine;
    private LineNode? _heightDirectionLine;
    private float _directionLinesLength = 50;

    public PlaneModelNodeSample(ICommonSamplesContext context) : base(context)
    {
    }

    protected override ModelNode CreateModelNode()
    {
        _planeModelNode = new PlaneModelNode("SamplePlane")
        {
            Position = new Vector3(0, 0, 0),
            Size = new Vector2(100, 80),
        };

        UpdateModelNode();

        return _planeModelNode;
    }

    protected override void OnCreateScene(Scene scene)
    {
        base.OnCreateScene(scene);

        _normalLine = new LineNode("NormalLine")
        {
            LineColor = Colors.Red,
            LineThickness = 2,
            StartPosition = new Vector3(0, 0, 0),
            EndPosition = new Vector3(0, _directionLinesLength, 0),
            EndLineCap = LineCap.ArrowAnchor
        };

        scene.RootNode.Add(_normalLine);

        _heightDirectionLine = new LineNode("HeightDirectionLine")
        {
            LineColor = Colors.Green,
            LineThickness = 2,
            StartPosition = new Vector3(0, 0, 0),
            EndPosition = new Vector3(0, 0, -_directionLinesLength),
            EndLineCap = LineCap.ArrowAnchor
        };

        scene.RootNode.Add(_heightDirectionLine);
    }

    protected override void UpdateModelNode()
    {
        if (_planeModelNode == null)
            return;

        _planeModelNode.WidthSegmentsCount = _widthSegmentsCount;
        _planeModelNode.HeightSegmentsCount = _heighSegmentsCount;

        _planeModelNode.Normal = _normal;
        _planeModelNode.HeightDirection = _heightDirection;

        if (_normalLine != null)
            _normalLine.EndPosition = _normalLine.StartPosition + _normal * _directionLinesLength;

        if (_heightDirectionLine != null)
            _heightDirectionLine.EndPosition = _heightDirectionLine.StartPosition + _heightDirection * _directionLinesLength;

        base.UpdateModelNode();
    }

    protected override void OnCreatePropertiesUI(ICommonSampleUIProvider ui)
    {
        ui.CreateKeyValueLabel("Size:", () => "(100, 80)", keyTextWidth: 110);

        ui.CreateKeyValueLabel("Position:", () => "(0, 0, 0)", keyTextWidth: 110);

        var enumNames = Enum.GetNames<PositionTypes>().Where(e => e != "Front" && e != "Back").ToArray(); // remove Front and Back because plane is 2D and has only Left, Right, Top and Bottom
        ui.CreateComboBox(items: enumNames,
                          itemChangedAction: OnPositionTypeChanged, 
                          selectedItemIndex: 0,
                          width: 120,
                          keyText: "PositionType: ",
                          keyTextWidth: 110);

        ui.AddSeparator();


        CreateComboBoxWithVectors(ui: ui, vectors: new Vector3[] { new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 1) },
                                  itemChangedAction: OnNormalChanged,
                                  selectedItemIndex: 0,
                                  width: 120,
                                  keyText: "Normal: ",
                                  keyTextWidth: 110).SetColor(Colors.Red);

        CreateComboBoxWithVectors(ui: ui, vectors: new Vector3[] { new Vector3(0, 0, -1), new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1), new Vector3(1, 0, -1) },
                                  itemChangedAction: OnHeightDirectionChanged,
                                  selectedItemIndex: 0,
                                  width: 120,
                                  keyText: "HeightDirection: ",
                                  keyTextWidth: 110).SetColor(Colors.Green);

        ui.AddSeparator();

        ui.CreateSlider(1, 10,
            () => _widthSegmentsCount,
            newValue =>
            {
                _widthSegmentsCount = (int)newValue;
                UpdateModelNode();
            },
            100,
            keyText: "Width Segments:",
            keyTextWidth: 110,
            formatShownValueFunc: newValue => ((int)newValue).ToString());
        
        ui.CreateSlider(1, 10,
            () => _heighSegmentsCount,
            newValue =>
            {
                _heighSegmentsCount = (int)newValue;
                UpdateModelNode();
            },
            100,
            keyText: "Height Segments:",
            keyTextWidth: 110,
            formatShownValueFunc: newValue => ((int)newValue).ToString());

        ui.CreateLabel("(Default value for Segments is 1)").SetStyle("italic");
    }

    private void OnNormalChanged(int itemIndex, Vector3 selectedVector)
    {
        _normal = Vector3.Normalize(selectedVector);
        UpdateModelNode();
    }
    
    private void OnHeightDirectionChanged(int itemIndex, Vector3 selectedVector)
    {
        _heightDirection = Vector3.Normalize(selectedVector);
        UpdateModelNode();
    }

    private void OnPositionTypeChanged(int index, string? itemText)
    {
        if (_planeModelNode == null)
            return;

        PositionTypes positionType;

        if (itemText == null)
            positionType = PositionTypes.Center;
        else
            positionType = Enum.Parse<PositionTypes>(itemText);

        _planeModelNode.PositionType = positionType;

        UpdateModelNode();
    }
}