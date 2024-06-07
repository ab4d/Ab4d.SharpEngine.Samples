using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.HitTesting;

public class LineSelectionSample : CommonSample
{
    public override string Title => "Line Selection sample";

    private float _linePositionsRange = 100;
    
    private Random _rnd = new Random();

    private ICommonSampleUIElement? _startStopCameraButton;

    private Vector2 _lastMousePosition;
    private List<LineSelectorData>? _lineSelectorData;

    private LineSelectorData? _lastSelectedLineSelector;

    private bool _isCameraChanged;

    private float _closestLineDistance = -1;
    private int _lineSegmentIndex = -1;

    private float _maxSelectionDistance = 15;
    private bool _orderByDistance = true;
    private SphereModelNode? _closestPositionSphereNode;
    private Color4 _savedLineColor;

    private ICommonSampleUIElement? _maxSelectionDistanceLabel;
    private ICommonSampleUIElement? _closestDistanceLabel;
    private ICommonSampleUIElement? _lineSegmentIndexLabel;

    public LineSelectionSample(ICommonSamplesContext context)
        : base(context)
    {
        RotateCameraConditions = PointerAndKeyboardConditions.RightPointerButtonPressed;
        MoveCameraConditions = PointerAndKeyboardConditions.RightPointerButtonPressed | PointerAndKeyboardConditions.ControlKey;
    }

    protected override void OnCreateScene(Scene scene)
    {
    }

    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        // LineSelectorData is a utility class that can be used to get the closest distance 
        // of a specified screen position to the 3D line.
        _lineSelectorData = new List<LineSelectorData>();

        for (int i = 0; i < 15; i++)
        {
            // Create random 3D lines
            var randomColor = GetRandomColor();
            var lineNode = GenerateRandomLine(randomColor, 10);

            // Create LineSelectorData from each line.
            // When adjustLineDistanceWithLineThickness is true, then distance is measured from line edge.
            // If adjustLineDistanceWithLineThickness is false, then distance is measured from center of the line.
            var lineSelectorData = new LineSelectorData(lineNode, sceneView, adjustLineDistanceWithLineThickness: true);

            _lineSelectorData.Add(lineSelectorData);
        }


        _isCameraChanged = true; // When true, the CalculateViewPositions method is called before calculating line distances

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Distance = 400;

            targetPositionCamera.CameraChanged += OnCameraChanged;

            targetPositionCamera.StartRotation(headingChangeInSecond: 20);
        }

        base.OnSceneViewInitialized(sceneView);
    }

    protected override void OnDisposed()
    {
        if (targetPositionCamera != null)
            targetPositionCamera.CameraChanged -= OnCameraChanged;

        base.OnDisposed();
    }

    private void OnCameraChanged(object? sender, EventArgs args)
    {
        _isCameraChanged = true;
        UpdateClosestLine();
    }

    protected void ProcessPointerMove(Vector2 mousePosition)
    {
        _lastMousePosition = mousePosition;
        UpdateClosestLine();
    }

    private void UpdateClosestLine()
    {
        if (_lineSelectorData == null || SceneView == null || _lineSelectorData.Count == 0)
            return;

        if (_isCameraChanged)
        {
            // Each time camera is changed, we need to call CalculateViewPositions method.
            // This will update the 2D screen positions of the 3D lines.
            for (var i = 0; i < _lineSelectorData.Count; i++)
                _lineSelectorData[i].CalculateViewPositions(SceneView);

            _isCameraChanged = false;
        }


        // Now we can call the GetClosestDistance method.
        // This method calculates the closest distance from the _lastMousePosition to the line that was used to create the LineSelectorData.
        // GetClosestDistance also sets the LastDistance, LastLinePositionIndex properties on the LineSelectorData.
        for (var i = 0; i < _lineSelectorData.Count; i++)
            _lineSelectorData[i].GetClosestDistance(_lastMousePosition);


        // Get the closest line
        IEnumerable<LineSelectorData> usedLineSelectors;

        // If we limit the distance of the specified position to the line, then we can filter all the line with Where
        if (_maxSelectionDistance >= 0)
            usedLineSelectors = _lineSelectorData.Where(l => l.LastDistance <= _maxSelectionDistance).ToList();
        else
            usedLineSelectors = _lineSelectorData;


        List<LineSelectorData> orderedLineSelectors;
        if (_orderByDistance)
        {
            // Order by camera distance
            orderedLineSelectors = usedLineSelectors.OrderBy(l => l.LastDistanceFromCamera).ToList();
        }
        else
        {
            // Order by distance to the specified position
            orderedLineSelectors = usedLineSelectors.OrderBy(l => l.LastDistance).ToList();
        }

        // Get the closest LineSelectorData
        LineSelectorData? closestLineSelector;
        if (orderedLineSelectors.Count > 0)
            closestLineSelector = orderedLineSelectors[0];
        else
            closestLineSelector = null;


        // The closest position on the line is shown with a SphereVisual3D
        if (_closestPositionSphereNode == null && Scene != null)
        {
            _closestPositionSphereNode = new SphereModelNode()
            {
                Radius = 2,
                Material = StandardMaterials.Red
            };

            Scene.RootNode.Add(_closestPositionSphereNode);
        }

        if (closestLineSelector == null)
        {
            _closestLineDistance = -1;
            _lineSegmentIndex = -1;

            if (_closestPositionSphereNode != null)
                _closestPositionSphereNode.Visibility = SceneNodeVisibility.Hidden;
        }
        else
        {
            _closestLineDistance = closestLineSelector.LastDistance;
            _lineSegmentIndex = closestLineSelector.LastLinePositionIndex;

            if (_closestPositionSphereNode != null)
            {
                _closestPositionSphereNode.CenterPosition = closestLineSelector.LastClosestPositionOnLine;
                _closestPositionSphereNode.Visibility = SceneNodeVisibility.Visible;
            }
        }

        UpdateClosestPositionInfo();


        // Show the closest line as red
        if (!ReferenceEquals(_lastSelectedLineSelector, closestLineSelector))
        {
            if (_lastSelectedLineSelector != null && _lastSelectedLineSelector.LineNode != null)
                _lastSelectedLineSelector.LineNode.LineColor = _savedLineColor;

            if (closestLineSelector != null && closestLineSelector.LineNode != null)
            {
                _savedLineColor = closestLineSelector.LineNode.LineColor;
                closestLineSelector.LineNode.LineColor = Colors.Red;
            }

            _lastSelectedLineSelector = closestLineSelector;
        }
    }


    private LineBaseNode GenerateRandomLine(Color4 lineColor, int lineSegmentsCount)
    {
        var positions = CreateRandomPositions(lineSegmentsCount);
        
        LineBaseNode createdLine;

        if (_rnd.Next(4) == 1) // 25% chance to return a simple Line instead of PolyLine
        {
            createdLine = new LineNode()
            {
                StartPosition = positions[0],
                EndPosition = positions[^1],
                LineColor = lineColor,
                LineThickness = _rnd.NextSingle() * 5 + 1,
            };
        }
        else
        {
            createdLine = new PolyLineNode()
            {
                LineColor = lineColor,
                LineThickness = _rnd.NextSingle() * 5 + 1,
                Positions = positions
            };
        }


        Scene?.RootNode.Add(createdLine);

        return createdLine;
    }

    private Color4 GetRandomColor()
    {
        float amount = _rnd.NextSingle() * 0.7f;
        return new Color4(amount, amount, amount, 1);
    }

    private Vector3[] CreateRandomPositions(int pointsCount)
    {
        var positions = new Vector3[pointsCount];

        var onePosition = GetRandomPosition(new Vector3(0, 0, 0), new Vector3(_linePositionsRange, _linePositionsRange, _linePositionsRange));

        // direction in range from -1 ... +1
        var lineDirection = GetRandomPosition(new Vector3(0, 0, 0), new Vector3(2, 2, 2));

        var lineRightDirection = new Vector3(lineDirection.Z, lineDirection.Y, lineDirection.X); // switch X and Z to get vector to the right of lineDirection
        var lineUpDirection = new Vector3(0, 1, 0);

        var positionAdvancement = _linePositionsRange * 1.5f / pointsCount;
        var displacementRange = _linePositionsRange * 0.15f;

        for (int i = 0; i < pointsCount; i++)
        {
            var vector = lineDirection * positionAdvancement;
            vector += lineUpDirection * displacementRange * (_rnd.NextSingle() * 2.0f - 1.0f);
            vector += lineRightDirection * displacementRange * (_rnd.NextSingle() * 2.0f - 1.0f);

            onePosition += vector;

            positions[i] = onePosition;
        }

        return positions;
    }

    private void UpdateClosestPositionInfo()
    {
        _closestDistanceLabel?.UpdateValue();
        _lineSegmentIndexLabel?.UpdateValue();
    }

    private void StartStopCameraRotation()
    {
        if (targetPositionCamera == null || _startStopCameraButton == null)
            return;

        if (targetPositionCamera.IsRotating)
        {
            targetPositionCamera.StopRotation();
            _startStopCameraButton.SetText("Start camera rotation");
        }
        else
        {
            targetPositionCamera.StartRotation(headingChangeInSecond: 20);
            _startStopCameraButton.SetText("Stop camera rotation");
        }
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        _closestDistanceLabel = ui.CreateKeyValueLabel("Closest line distance:", () => _closestLineDistance >= 0 ? _closestLineDistance.ToString("F1") : "");
        _lineSegmentIndexLabel = ui.CreateKeyValueLabel("Line segment index:", () => _lineSegmentIndex >= 0 ? _lineSegmentIndex.ToString() : "");

        ui.AddSeparator();


        _maxSelectionDistanceLabel = ui.CreateKeyValueLabel("Max selection distance:", () => _maxSelectionDistance.ToString("F1"));

        ui.CreateSlider(0, 20,
            () => _maxSelectionDistance,
            newValue =>
            {
                _maxSelectionDistance = newValue;
                _maxSelectionDistanceLabel.UpdateValue();
            },
            200);


        ui.CreateCheckBox("Order by distance", _orderByDistance, delegate (bool isChecked) { _orderByDistance = isChecked; })
            .SetToolTip("When unchecked the line that is closest to the mouse will be selected even if it is behind some other line in 3D space.");


        ui.AddSeparator();

        _startStopCameraButton = ui.CreateButton("Stop camera rotation", () => StartStopCameraRotation());

        // Subscribe to mouse (pointer) moved
        ui.RegisterPointerMoved(ProcessPointerMove); 
    }
}