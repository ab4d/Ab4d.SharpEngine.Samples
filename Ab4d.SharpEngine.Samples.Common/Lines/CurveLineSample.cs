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

    private int _controlPointsCount = 19; // This defines 6 Bezier curve segments

    private CurveLineNode? _curveNode;
    private WireCrossNode[]? _controlPointMarkers;

    private WireCrossNode? _positionOnCurveWireCross;
    private float _positionOnCurveT = 0.5f;
    
    private BezierCurve? _bezierCurve;
    private BSpline? _bSpline;

    public CurveLineSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        // CurveLineNode is a PolyLine Node that shows the curve with the specified type and control points
        _curveNode = new CurveLineNode("Curve")
        {
            LineColor = Colors.Black,
            LineThickness = 2,
            CurveType = CurveLineNode.CurveTypes.CurveThroughPoints,
            PositionsPerSegment = 30, // this is also the default value
        };

        scene.RootNode.Add(_curveNode);

        // To manually get positions for each curve type, see the GetPositionManually method
        //var positions = GetPositionManually();


        _controlPointMarkers = Array.Empty<WireCrossNode>();

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Distance = 600;
            targetPositionCamera.Heading = 45;
        }


        // Initial update
        GenerateRandomControlPoints(out var initialControlPoints, out var initialWeights);
        UpdateControlPoints(initialControlPoints, initialWeights);

        UpdatePositionOnCurve(_positionOnCurveT);

        // Axis arrows for direction visualization
        var axesNode = new AxisLineNode();
        scene.RootNode.Add(axesNode);
    }

    private void GenerateRandomControlPoints(out Vector3[] controlPoints, out float[] weights)
    {
        controlPoints = new Vector3[_controlPointsCount];
        weights = new float[_controlPointsCount];

        var point = new Vector3(-(20 * _controlPointsCount / 2), 0, 0);

        for (var i = 0; i < _controlPointsCount; i++)
        {
            var vector = new Vector3(20, 40 * GetRandomFloat() - 20, 40 * GetRandomFloat() - 20);
            point += vector;

            controlPoints[i] = point;
            weights[i]       = GetRandomFloat() + 0.5f; // from 0.5 to 1.5
        }
    }

    private void UpdateControlPoints(Vector3[] controlPoints, float[] weights)
    {
        if (_curveNode == null || _controlPointMarkers == null)
            return;

        // Update controls points on the curve
        _curveNode.ControlPoints = controlPoints;
        _curveNode.Weights = weights;

        // *** Update markers for control points ***
        UpdateControlPointMarkers(controlPoints);

        // Reset curves so we will create new ones
        _bezierCurve = null;
        _bSpline = null;

        UpdatePositionOnCurve(_positionOnCurveT);
    }

    private void UpdateControlPointMarkers(Vector3[] controlPoints)
    {
        if (_controlPointMarkers == null || Scene == null)
            return;

        // Remove redundant markers
        for (var i = controlPoints.Length; i < _controlPointMarkers.Length; i++)
        {
            var marker = _controlPointMarkers[i];
            Scene.RootNode.Remove(marker);
        }

        // Allocate new array and fill it with existing/additional markers
        var newMarkers = new WireCrossNode[controlPoints.Length];
        for (var i = 0; i < controlPoints.Length; i++)
        {
            var controlPoint = controlPoints[i];
            WireCrossNode marker;

            if (i < _controlPointMarkers.Length)
            {
                // Update existing marker
                marker = _controlPointMarkers[i];
                marker.Transform = new TranslateTransform(controlPoint.X, controlPoint.Y, controlPoint.Z);
            }
            else
            {
                // Create new marker
                marker = new WireCrossNode(name: $"Control point marker #{i}")
                {
                    LinesLength = 10,
                    LineThickness = 1.5f,
                    LineColor = Colors.Red,
                    Transform = new TranslateTransform(controlPoint.X, controlPoint.Y, controlPoint.Z)
                };
                Scene.RootNode.Add(marker);
            }

            newMarkers[i] = marker;
        }

        // Replace the array
        _controlPointMarkers = newMarkers;
    }

    private void GenerateNewCurve()
    {
        GenerateRandomControlPoints(out var controlPoints, out var weights);
        UpdateControlPoints(controlPoints, weights);
    }

    private void ChangeCurveType(CurveLineNode.CurveTypes curveType)
    {
        if (_curveNode == null)
            return;

        _curveNode.CurveType = curveType;

        if (_controlPointMarkers != null)
        {
            var alternateWireCrossColor = curveType == CurveLineNode.CurveTypes.BezierCurve ? Colors.Green : Colors.Red;

            for (var i = 0; i < _controlPointMarkers.Length; i++)
            {
                // Bezier curve is a curve where each curve segment is defined by 4 control point
                // (2 for start and end of the curve and 2 for controlling the curvature - tangents to the curve)
                // Color the tangent points GREEN
                if (i % 3 == 1 || i % 3 == 2)
                    _controlPointMarkers[i].LineColor = alternateWireCrossColor;

                if (curveType == CurveLineNode.CurveTypes.NURBSCurve && _curveNode.Weights != null)
                    _controlPointMarkers[i].LineThickness = _curveNode.Weights[i]; // weights are from 0.5 to 1.5
                else
                    _controlPointMarkers[i].LineThickness = 1;
            }
        }

        // Reset curves so we will create new ones
        _bezierCurve = null;
        _bSpline = null;

        UpdatePositionOnCurve(_positionOnCurveT);
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

    private Vector3[]? GetPositionManually()
    {
        if (_curveNode == null || _curveNode.ControlPoints == null)
            return null;

        var controlPoints = _curveNode.ControlPoints;
        var positionsPerSegment = _curveNode.PositionsPerSegment;

        Vector3[] positions;

        switch (_curveNode.CurveType)
        {
            case CurveTypes.BezierCurve:
                // Bezier curve - interpret the given control points as complete Bezier control points!
                positions = BezierCurve.CreateBezierCurvePositions(controlPoints, positionsPerSegment);
                break;

            case CurveTypes.CurveThroughPoints:
                // Bezier curve through points - interpret the given control points as positions on Bezier curve!
                positions = BezierCurve.CreateBezierCurvePositionsThroughPoints(controlPoints, positionsPerSegment, _curveNode.CurveScale);
                break;

            case CurveTypes.BSpline:
                // B-spline
                positions = BSpline.CreateBSplinePositions(controlPoints, positionsPerSegment);
                break;

            case CurveTypes.NURBSCurve:
                // NURBS curve. Requires weights - if not available, fall back to B-spline
                if (_curveNode.Weights == null)
                    positions = BSpline.CreateBSplinePositions(controlPoints, positionsPerSegment);
                else
                    positions = BSpline.CreateNURBSCurvePositions(controlPoints, _curveNode.Weights, positionsPerSegment);
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

        ui.CreateLabel("CurveType:");
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
        
        ui.CreateComboBox(new string[] { "2", "4", "8", "15", "30", "50" }, (selectedIndex, selectedText) =>
            {
                if (_curveNode != null)
                    _curveNode.PositionsPerSegment = Int32.Parse(selectedText ?? "0");
            }, 
            selectedItemIndex: 4,
            width: 50,
            "PositionsPerSegment:");

        ui.AddSeparator();

        ui.CreateLabel("Position on curve:");
        ui.CreateSlider(0, 1, () => _positionOnCurveT, delegate (float t)
            {
                _positionOnCurveT = t;
                UpdatePositionOnCurve(t);
            }, 
            width: 140,
            formatShownValueFunc: t => $"t: {t:F2}");

        ui.AddSeparator();

        ui.CreateButton("Generate new curve", () => GenerateNewCurve());
    }
}
