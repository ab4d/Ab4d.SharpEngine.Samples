using System;
using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using static Ab4d.SharpEngine.SceneNodes.CurveLineNode;

namespace Ab4d.SharpEngine.Samples.Common.Lines;
 
public class CurveLineSample : CommonSample
{
    public override string Title => "3D curve lines";

    private bool _isUsingAngleBasedCurveGeneration = true;
    private bool _showIndividualPositions = true;
    
    private int _controlPointsCount = 10;
    private int _totalCreatedPositionsCount;

    private Vector3[]? _controlPoints;
    private float[]? _weights;

    private CurveLineNode? _curveNode;
    private GroupNode _controlPointsGroupNode;
    private GroupNode _individualPositionGroupNode;

    private WireCrossNode? _positionOnCurveWireCross;
    private float _positionOnCurveT = 0.5f;

    
    private BezierCurve? _bezierCurve;
    private BSpline? _bSpline;
    private ICommonSampleUIElement? _positionsPerSegmentComboBox;
    private ICommonSampleUIElement? _angleThresholdComboBox;
    private ICommonSampleUIElement? _minSegmentLengthComboBox;
    private ICommonSampleUIElement? _totalPositionsLabel;

    public CurveLineSample(ICommonSamplesContext context)
        : base(context)
    {
        _controlPointsGroupNode = new GroupNode("ControlPointsMarkers");
        _individualPositionGroupNode = new GroupNode("IndividualPositionMarkers");
    }

    protected override void OnCreateScene(Scene scene)
    {
        // CurveLineNode is a PolyLine Node that shows the curve with the specified type and control points
        _curveNode = new CurveLineNode("Curve")
        {
            LineColor = Colors.Black,
            LineThickness = 2,
            CurveType = CurveLineNode.CurveTypes.CurveThroughPoints,

            GenerationAlgorithm = CurveGenerationAlgorithms.AngleBased,

            // The following properties are used when GenerationAlgorithm is AngleBased (all values are default values):
            AngleThreshold = 5,
            MinSegmentLength = 2,
            MaxSubdivisionsCount = 6,

            // The following is used when GenerationAlgorithm is FixedPositionsPerSegment:
            PositionsPerSegment = 10, // this is also the default value
        };

        scene.RootNode.Add(_curveNode);
        
        scene.RootNode.Add(_controlPointsGroupNode);
        scene.RootNode.Add(_individualPositionGroupNode);

        // To manually get positions for each curve type, see the GetPositionManually method
        //var positions = GetPositionManually();

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Distance = 350;
            targetPositionCamera.Heading = 60;
            targetPositionCamera.Attitude = -40;
        }


        // Initial update
        GenerateNewCurve();

        ShowCameraAxisPanel = true;
    }

    private void GenerateRandomControlPoints()
    {
        if (_controlPoints == null || _controlPoints.Length != _controlPointsCount)
            _controlPoints = new Vector3[_controlPointsCount];

        if (_weights == null || _weights.Length != _controlPointsCount)
            _weights = new float[_controlPointsCount];


        var point = new Vector3(-(20 * _controlPointsCount / 2), 0, 0);

        for (var i = 0; i < _controlPointsCount; i++)
        {
            var vector = new Vector3(20, 40 * GetRandomFloat() - 20, 40 * GetRandomFloat() - 20);
            point += vector;

            _controlPoints[i] = point;
            _weights[i]       = GetRandomFloat() + 0.5f; // from 0.5 to 1.5
        }
    }

    private void UpdateControlPoints()
    {
        if (_curveNode == null || _controlPoints == null)
            return;

        Vector3[] controlPoints;
        if (_curveNode.CurveType == CurveTypes.BezierCurve)
        {
            var bezierCurve = BezierCurve.CreateBezierCurveThroughPoints(_controlPoints);
            controlPoints = bezierCurve.ControlPoints;
        }
        else
        {
            controlPoints = _controlPoints;
        }

        // Instead of setting ControlPoints and Weights separately ...
        //_curveNode.ControlPoints = controlPoints;
        //_curveNode.Weights = _weights;

        // ... you can also set them together by using SetControlPointsAndWeights method:
        _curveNode.SetControlPointsAndWeights(controlPoints, _weights);
    }

    private void UpdateShownMarkers()
    {
        if (_curveNode == null)
            return;

        // *** Update markers for control points ***
        UpdateControlPointMarkers(_curveNode.ControlPoints);
        UpdateIndividualPositions();

        // Reset curves so we will create new ones
        _bezierCurve = null;
        _bSpline = null;

        UpdatePositionOnCurve(_positionOnCurveT);

        _totalPositionsLabel?.UpdateValue();
    }

    private void UpdateControlPointMarkers(Vector3[]? controlPoints)
    {
        _controlPointsGroupNode.Clear();

        if (controlPoints == null)
            return;

        var cureType = _curveNode?.CurveType ?? CurveLineNode.CurveTypes.None;

        for (var i = 0; i < controlPoints.Length; i++)
        {
            var controlPoint = controlPoints[i];
            
            Color4 lineColor = Colors.Red;
            var lineThickness = 1f;
            var lineLength = 8;

            // Bezier curve is a curve where each curve segment is defined by 4 control point
            // (2 for start and end of the curve and 2 for controlling the curvature - tangents to the curve)
            // Color the tangent points GREEN
            if (cureType == CurveTypes.BezierCurve && (i % 3 == 1 || i % 3 == 2))
            {
                lineLength = 5;
                lineColor = Colors.Green;
            }
            else if (cureType == CurveTypes.NURBSCurve && _curveNode != null && _curveNode.Weights != null)
            {
                lineThickness = (_curveNode.Weights[i] - 0.4f) * 2; // weights are from 0.5 to 1.5 => lineThickness is from 0.2 to 2.2
            }

            var marker = new WireCrossNode(name: $"Control point marker #{i}")
            {
                LinesLength = lineLength,
                LineThickness = lineThickness,
                LineColor = lineColor,
                Transform = new TranslateTransform(controlPoint.X, controlPoint.Y, controlPoint.Z)
            };

            _controlPointsGroupNode.Add(marker);
        }
    }

    private void UpdateIndividualPositions()
    {
        _individualPositionGroupNode.Clear();

        if (_curveNode == null)
            return;

        var positions = _curveNode.GetPositions();

        if (positions != null)
        {
            _totalCreatedPositionsCount = positions.Length;

            if (_showIndividualPositions)
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    var wireCrossNode = new WireCrossNode(positions[i], lineColor: Colors.Gray, lineThickness: 1, lineLength: 3);
                    _individualPositionGroupNode.Add(wireCrossNode);
                }
            }
        }
        else
        {
            _totalCreatedPositionsCount = 0; 
        }

        _totalPositionsLabel?.UpdateValue();
    }

    private void GenerateNewCurve()
    {
        GenerateRandomControlPoints();
        UpdateControlPoints();
        UpdateShownMarkers();

        if (targetPositionCamera != null && _positionOnCurveWireCross != null)
            targetPositionCamera.TargetPosition = _positionOnCurveWireCross.Position;
    }

    private void ChangeCurveType(CurveLineNode.CurveTypes curveType)
    {
        if (_curveNode == null)
            return;

        _curveNode.Weights = null; // Disconnect Weights before changing curve type. This prevents error is weights count is not the same as control points count (for example when changing from NURBS curve to B-spline or Bezier curve).
        _curveNode.CurveType = curveType;

        UpdateControlPoints();

        UpdateShownMarkers();
    }

    private void UpdateCurrentCurve()
    {
        if (_curveNode == null)
            return;

        // Reset curves so we will create new ones
        _bezierCurve = null;
        _bSpline = null;

        UpdatePositionOnCurve(_positionOnCurveT);

        UpdateControlPointMarkers(_curveNode.ControlPoints);
        UpdateIndividualPositions();
    }

    private void UpdatePositionOnCurve(float t)
    {
        if (_curveNode == null || _curveNode.ControlPoints == null)
            return;

        var controlPoints = _curveNode.ControlPoints;

        Vector3 positionOnCurve;

        switch (_curveNode.CurveType)
        {
            case CurveTypes.BezierCurve:
            case CurveTypes.CurveThroughPoints:

                if (_bezierCurve == null)
                {
                    if (_curveNode.CurveType == CurveTypes.BezierCurve)
                        _bezierCurve = new BezierCurve(controlPoints);
                    else
                        _bezierCurve = BezierCurve.CreateBezierCurveThroughPoints(controlPoints, _curveNode.CurveScale);
                }

                positionOnCurve = _bezierCurve.GetPositionOnCurve(t);
                break;

            case CurveTypes.BSpline:
            case CurveTypes.NURBSCurve:
                // B-spline
                if (_curveNode.CurveType == CurveTypes.BSpline || _curveNode.Weights == null)
                {
                    _bSpline ??= new BSpline(controlPoints, curveDegree: 3);
                    positionOnCurve = _bSpline.GetPositionOnBSpline(t);
                }
                else
                {
                    _bSpline ??= new BSpline(controlPoints, _curveNode.Weights, curveDegree: 3);
                    positionOnCurve = _bSpline.GetPositionOnNURBSCurve(t);
                }
                break;

            default:
                positionOnCurve = new Vector3(float.NaN, float.NaN, float.NaN);
                break;
        }

        if (_positionOnCurveWireCross == null && Scene != null)
        {
            _positionOnCurveWireCross = new WireCrossNode()
            {
                LineColor = Colors.Yellow,
                LineThickness = 3,
                LinesLength = 15
            };

            Scene.RootNode.Add(_positionOnCurveWireCross);
        }

        if (_positionOnCurveWireCross != null && !float.IsNaN(positionOnCurve.X))
            _positionOnCurveWireCross.Position = positionOnCurve;
    }

    // Instead of using CurveLineNode to generate curve positions, you can also generate them manually by using the static methods in BezierCurve and BSpline classes.
    private Vector3[]? GetPositionManually()
    {
        if (_curveNode == null || _curveNode.ControlPoints == null)
            return null;

        var controlPoints        = _curveNode.ControlPoints;
        var positionsPerSegment  = _curveNode.PositionsPerSegment;
        var angleThreshold       = _curveNode.AngleThreshold;
        var minSegmentLengths    = _curveNode.MinSegmentLength;
        var maxSubdivisionsCount = _curveNode.MaxSubdivisionsCount;

        Vector3[] positions;

        switch (_curveNode.CurveType)
        {
            case CurveTypes.BezierCurve:
                // Bezier curve - interpret the given control points as complete Bezier control points!
                if (_isUsingAngleBasedCurveGeneration)
                    positions = BezierCurve.CreateBezierCurvePositions(controlPoints, angleThreshold, minSegmentLengths, maxSubdivisionsCount);
                else
                    positions = BezierCurve.CreateBezierCurvePositions(controlPoints, positionsPerSegment);

                break;

            case CurveTypes.CurveThroughPoints:
                // Bezier curve through points - interpret the given control points as positions on Bezier curve!
                if (_isUsingAngleBasedCurveGeneration)
                    positions = BezierCurve.CreateBezierCurvePositionsThroughPoints(controlPoints, angleThreshold, minSegmentLengths, maxSubdivisionsCount);
                else
                    positions = BezierCurve.CreateBezierCurvePositionsThroughPoints(controlPoints, positionsPerSegment, _curveNode.CurveScale);

                break;

            case CurveTypes.BSpline:
                // B-spline
                if (_isUsingAngleBasedCurveGeneration)
                    positions = BSpline.CreateBSplinePositions(controlPoints, angleThreshold, minSegmentLengths, maxSubdivisionsCount);
                else
                    positions = BSpline.CreateBSplinePositions(controlPoints, positionsPerSegment);

                break;

            case CurveTypes.NURBSCurve:
                // NURBS curve. Requires weights - if not available, fall back to B-spline
                if (_isUsingAngleBasedCurveGeneration)
                {
                    if (_curveNode.Weights == null)
                        positions = BSpline.CreateBSplinePositions(controlPoints, angleThreshold, minSegmentLengths, maxSubdivisionsCount);
                    else
                        positions = BSpline.CreateNURBSCurvePositions(controlPoints, _curveNode.Weights, angleThreshold, minSegmentLengths, maxSubdivisionsCount);
                }
                else
                {
                    if (_curveNode.Weights == null)
                        positions = BSpline.CreateBSplinePositions(controlPoints, positionsPerSegment);
                    else
                        positions = BSpline.CreateNURBSCurvePositions(controlPoints, _curveNode.Weights, positionsPerSegment);
                }

                break;

            default:
                // Undefined - linear line segments
                // Generate new positions
                positions = new Vector3[controlPoints.Length];

                for (var i = 0; i < controlPoints.Length; i++)
                    positions[i] = controlPoints[i];

                break;
        }

        return positions;
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);


        ui.CreateLabel("Position on curve (yellow cross):");
        ui.CreateSlider(0, 1, () => _positionOnCurveT, delegate (float t)
            {
                _positionOnCurveT = t;
                UpdatePositionOnCurve(t);
            }, 
            width: 140,
            formatShownValueFunc: t => $"t: {t:F2}");

        ui.AddSeparator();
        ui.AddSeparator();


        ui.CreateLabel("Curve type:");
        ui.CreateRadioButtons(new string[]
            {
                "BSpline (?):B-spline is a curve that is defined by control points and without using any weights",
                "NURBSCurve (?): NURBS curve is a Non-Uniform Rational B-spline. The difference between normal B-spline\nand NURBS curve is that NURBS curve uses weighted control points.\nIn this sample Weights are shown by different LineThickness.",
                "BezierCurve (?): Bezier curve is a curve where each curve segment is defined by 4 control point\n(2 for start and end of the curve and 2 for controlling the curvature - tangents to the curve).\nTangents points are shows with GREEN color.",
                "CurveThroughPoints (?): CurveThroughPoints is a special Bezier curve that has tangent points\ndefined in such a way that the curve goes through the specified control points."
            }, (selectedIndex, selectedText) =>
            {
                ChangeCurveType((CurveLineNode.CurveTypes)(selectedIndex + 1));
            }, 
            selectedItemIndex: 3);

        ui.AddSeparator();


        ui.CreateLabel("Curve generation algorithm:");
        ui.CreateRadioButtons(new string[]
            {
                "Fixed positions per segment (?):When the curve is generated based on the positions per segment,\nthen the number of generated points if fixed regardless of the curvature.\nThis method is very fast, but produce too many points along\nnearly straight sections and too few where the curve bends sharply.\n\nFinal number of curve points is: (curve_points - 1) * points_per_segment + 1",
                "Angle based (?):Angle based curve generation generates the curve points by recursively\nsubdividing each cubic segment until two tolerances are satisfied:\n`angleThresholdDegrees` limits how sharp the turn may be between consecutive chords,\nwhile `minSegmentLength` guarantees that no chord grows longer than the specified length.",
            }, (selectedIndex, selectedText) =>
            {
                _isUsingAngleBasedCurveGeneration = selectedIndex == 1;
                
                if (_curveNode != null)
                    _curveNode.GenerationAlgorithm = _isUsingAngleBasedCurveGeneration ? CurveGenerationAlgorithms.AngleBased : CurveGenerationAlgorithms.FixedPositionsPerSegment;

                UpdateCurrentCurve();

                _positionsPerSegmentComboBox?.SetIsVisible(!_isUsingAngleBasedCurveGeneration);
                _angleThresholdComboBox?.SetIsVisible(_isUsingAngleBasedCurveGeneration);
                _minSegmentLengthComboBox?.SetIsVisible(_isUsingAngleBasedCurveGeneration);
            }, 
            selectedItemIndex: _isUsingAngleBasedCurveGeneration ? 1 : 0);


        var positionsPerSegmentValues = new int[] { 1, 2, 5, 10, 20, 30 };
        _positionsPerSegmentComboBox = ui.CreateComboBox(positionsPerSegmentValues.Select(v => v.ToString()).ToArray(), (selectedIndex, selectedText) =>
            {
                if (_curveNode != null)
                    _curveNode.PositionsPerSegment = Int32.Parse(selectedText ?? "0");

                UpdateCurrentCurve();
            }, 
            selectedItemIndex: 3,
            width: 50,
            "PositionsPerSegment:", keyTextWidth: 170).SetIsVisible(false);

        var angleThresholds = new float[] { 0.2f, 0.5f, 1, 2, 5, 10, 20, 30, 90 };
        _angleThresholdComboBox = ui.CreateComboBox(angleThresholds.Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray(), (selectedIndex, selectedText) =>
            {
                if (_curveNode != null)
                    _curveNode.AngleThreshold = angleThresholds[selectedIndex];
                
                UpdateCurrentCurve();
            }, 
            selectedItemIndex: 4,
            width: 50,
            "Angle Threshold (degrees):", keyTextWidth: 170);

        var minSegmentLengths = new float[] { 1, 2, 5, 10, 20, 50, 100 };
        _minSegmentLengthComboBox = ui.CreateComboBox(minSegmentLengths.Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray(), (selectedIndex, selectedText) =>
            {
                if (_curveNode != null)
                    _curveNode.MinSegmentLength = minSegmentLengths[selectedIndex];

                UpdateCurrentCurve();
            }, 
            selectedItemIndex: 1,
            width: 50,
            "Min segment length:", keyTextWidth: 170);

        ui.AddSeparator();

        _totalPositionsLabel = ui.CreateKeyValueLabel("Total curve positions:", () => _totalCreatedPositionsCount.ToString());

        ui.CreateCheckBox("Show individual positions", _showIndividualPositions, isChecked =>
        {
            _showIndividualPositions = isChecked;
            UpdateCurrentCurve();
        });

        ui.AddSeparator();
        ui.AddSeparator();
        
        var pointsCountValues = new int[] { 5, 10, 15, 20 };
        ui.CreateComboBox(pointsCountValues.Select(v => v.ToString()).ToArray(), (selectedIndex, selectedText) =>
            {
                _controlPointsCount = pointsCountValues[selectedIndex];
                GenerateNewCurve();
            }, 
            selectedItemIndex: Array.IndexOf(pointsCountValues, _controlPointsCount),
            width: 50,
            "Created points count:");
        
        ui.CreateButton("Generate new curve", () => GenerateNewCurve());
    }
}
