using System.Diagnostics;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using Ab4d.Vulkan;

namespace Ab4d.SharpEngine.Samples.Common.HitTesting;

public class LineSelectionSample : CommonSample
{
    public override string Title => "Line Selection sample";
    public override string Subtitle => "Select 3D line when pointer is close to the line";

    private static readonly bool AddTranslate = false;      // Set to true to test adding transformation to positions
    private static readonly float LinePositionsRange = 100; // defines the length of the generated lines

    private GroupNode? _linesGroupNode;

    private List<LineSelectorData> _allLineSelectorData;
    private List<LineSelectorData> _selectedLineSelectorData;

    private LineSelectorData? _lastSelectedLineSelector;

    private bool _isCameraChanged;
    private Vector2 _lastMousePosition;

    private float _closestLineDistance = -1;
    private int _lineSegmentIndex;
    private double _updateTime;
    
    private float _maxSelectionDistance = 15;
    
    private int _simpleLinesCount = 10;
    private int _polyLinesCount = 20;
    private int _multiLinesCount = 0;

    private int _lineSegmentsCount = 10;

    private bool _checkBoundingBox = true;
    private bool _isMultiThreaded = false;
    private bool _orderByDistance = true;

    private SphereModelNode? _closestPositionSphereNode;
    private Color4 _savedLineColor;


    private ICommonSampleUIElement? _maxSelectionDistanceLabel;
    private ICommonSampleUIElement? _closestDistanceLabel;
    private ICommonSampleUIElement? _lineSegmentIndexLabel;
    private ICommonSampleUIElement? _updateTimeLabel;
    private ICommonSampleUIElement? _startStopCameraButton;

    private Stopwatch _stopwatch = new();

    public LineSelectionSample(ICommonSamplesContext context)
        : base(context)
    {
        RotateCameraConditions = PointerAndKeyboardConditions.RightPointerButtonPressed;
        MoveCameraConditions = PointerAndKeyboardConditions.RightPointerButtonPressed | PointerAndKeyboardConditions.ControlKey;

        _allLineSelectorData = new List<LineSelectorData>();
        _selectedLineSelectorData = new List<LineSelectorData>();
    }

    protected override void OnCreateScene(Scene scene)
    {
        // Create the scene from OnSceneViewInitialized where SceneView property is also defined.
    }

    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        CreateSampleLines();

        // Add red sphere that will show the closest position on the closest line
        _closestPositionSphereNode = new SphereModelNode()
        {
            Radius = 2,
            Material = StandardMaterials.Red,
            Visibility = SceneNodeVisibility.Hidden
        };

        if (Scene != null)
            Scene.RootNode.Add(_closestPositionSphereNode);


        _isCameraChanged = true; // When true, the CalculateViewPositions method is called before calculating line distances

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Distance = 400;

            targetPositionCamera.CameraChanged += OnCameraChanged;

            targetPositionCamera.StartRotation(headingChangeInSecond: 20);
        }
    }

    protected override void OnDisposed()
    {
        if (targetPositionCamera != null)
            targetPositionCamera.CameraChanged -= OnCameraChanged;

        base.OnDisposed();
    }

    private void CreateSampleLines()
    {
        if (Scene == null)
            return;

        _allLineSelectorData.Clear();
        _selectedLineSelectorData.Clear();

        if (_linesGroupNode == null)
        {
            _linesGroupNode = new GroupNode("LinesGroupNode");
            Scene.RootNode.Add(_linesGroupNode);
        }
        else
        {
            _linesGroupNode.Clear();
        }
        

        for (int i = 0; i < _simpleLinesCount; i++)
            AddRandomLineWithLineSelectorData(lineLength: 2, _checkBoundingBox, isPolyLine: false);

        for (int i = 0; i < _polyLinesCount; i++)
            AddRandomLineWithLineSelectorData(lineLength: _lineSegmentsCount, _checkBoundingBox, isPolyLine: true);

        for (int i = 0; i < _multiLinesCount; i++)
            AddRandomLineWithLineSelectorData(lineLength: _lineSegmentsCount, _checkBoundingBox, isPolyLine: false);


        _isCameraChanged = true; // This will force calling CalculateViewPositions again
    }

    private void AddRandomLineWithLineSelectorData(int lineLength, bool checkBoundingBox, bool isPolyLine)
    {
        if (_linesGroupNode == null)
            return;

        // Get random grayscale color
        float amount = GetRandomFloat() * 0.7f;
        var lineColor = new Color4(amount, amount, amount, 1);

        float lineThickness = GetRandomFloat() * 5 + 1;

        var linePositions = CreateRandomPositions(lineLength);


        LineBaseNode createdLineObject;

        if (lineLength == 2)
        {
            var lineNode = new LineNode()
            {
                StartPosition = linePositions[0],
                EndPosition = linePositions[^1],
                LineColor = lineColor,
                LineThickness = lineThickness,
            };

            createdLineObject = lineNode;
        }
        else
        {
            if (isPolyLine)
            {
                createdLineObject = new PolyLineNode()
                {
                    LineColor = lineColor,
                    LineThickness = lineThickness,
                    Positions = linePositions
                };
            }
            else
            {
                createdLineObject = new MultiLineNode()
                {
                    LineColor = lineColor,
                    LineThickness = lineThickness,
                    Positions = linePositions,
                    //IsLineStrip = true
                };
            }
        }


        _linesGroupNode.Add(createdLineObject);

        // Create LineSelectorData from each line.
        // When adjustLineDistanceWithLineThickness is true (by default), then distance is measured from line edge.
        // If adjustLineDistanceWithLineThickness is false, then distance is measured from center of the line.
        var lineSelectorData = new LineSelectorData(createdLineObject, SceneView, adjustLineDistanceWithLineThickness: true)
        {
            // When CheckBoundingBox is true this significantly improves performance for lines with many segments
            // because individual segment are checked only after initial bounding box check.
            CheckBoundingBox = checkBoundingBox,
        };

        // We could also use LineSelectorData constructor that takes array of Vector3 and isLineStrip (bool).
        //Vector3[] linePositions = new Vector3[] { };
        //var lineSelectorData = new LineSelectorData(linePositions, isLineStrip: false);

        if (AddTranslate)
        {
            // Test line selection with transformed lines
            var translation = new TranslateTransform(0, 50, 0);
            createdLineObject.Transform = translation;
            lineSelectorData.PositionsTransform = translation;
        }

        _allLineSelectorData.Add(lineSelectorData);
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
        if (SceneView == null || _allLineSelectorData.Count == 0)
            return;

        double calculateViewPositionsTime;

        _stopwatch.Restart();

        if (_isCameraChanged)
        {
            // Each time the camera is changed, we need to call CalculateViewPositions method.
            // This will update the 2D screen positions of the 3D lines or their bounding box (when CheckBoundingBox is true).
            //
            // When the SceneView is passed to the constructor of the LineSelectorData,
            // or when LineSelectorData.SceneView is manually set,
            // then the CalculateViewPositions can be called without any parameters.
            //
            // Another option is to call the CalculateViewPositions that takes worldToViewportMatrix and dpi scale.
            // This is a faster option because in this case the SceneView.GetWorldToViewportMatrix is called
            // only once (otherwise each instance of LineSelectorData calls GetWorldToViewportMatrix).

            bool isWorldToViewportMatrixValid = SceneView.GetWorldToViewportMatrix(out var worldToViewportMatrix, forceMatrixRefresh: false);

            if (!isWorldToViewportMatrixValid && SceneView.Camera != null)
            {
                // Try again
                SceneView.Camera.Update();
                isWorldToViewportMatrixValid = SceneView.GetWorldToViewportMatrix(out worldToViewportMatrix, forceMatrixRefresh: true);

                if (!isWorldToViewportMatrixValid)
                    return; // Probably the SceneView's size is not yet known because it is not yet initialized
            }

            float dpiScaleX = SceneView.DpiScaleX;
            float dpiScaleY = SceneView.DpiScaleY;


            if (_isMultiThreaded)
            {
                // This code demonstrates how to use call CalculateViewPositions from multiple threads.
                // This significantly improves performance when many 3D lines are used (thousands).
                //
                // When calling CalculateViewPositions it is recommended to get worldToViewportMatrix
                // and then call CalculateViewPositions by passing the worldToViewportMatrix.
                // This way we call GetWorldToViewportMatrix only once from the main thread.
                Parallel.For(0, _allLineSelectorData.Count,
                             i => _allLineSelectorData[i].CalculateViewPositions(worldToViewportMatrix, dpiScaleX, dpiScaleY));
            }
            else
            {
                for (var i = 0; i < _allLineSelectorData.Count; i++)
                    _allLineSelectorData[i].CalculateViewPositions(worldToViewportMatrix, dpiScaleX, dpiScaleY);
                

                // We could also call CalculateViewPositions without any parameter (this requires that SceneView is set in the constructor or manually):
                //
                // First make sure that SceneView is set
                //if (!_isSceneViewSetToLineSelectorData)
                //{
                //    for (var i = 0; i < _allLineSelectorData.Count; i++)
                //        _allLineSelectorData[i].SceneView = SceneView;

                //    _isSceneViewSetToLineSelectorData = true;
                //}
                //
                //for (var i = 0; i < _allLineSelectorData.Count; i++)
                //    _allLineSelectorData[i].CalculateViewPositions();
            }

            _isCameraChanged = false;

            calculateViewPositionsTime = _stopwatch.Elapsed.TotalMilliseconds;
            _stopwatch.Restart();
        }
        else
        {
            calculateViewPositionsTime = 0;
        }

        
        // Now we can call the GetClosestDistance method.
        // This method calculates the closest distance from the _lastMousePosition to the line that was used to create the LineSelectorData.
        // GetClosestDistance sets the LastDistance and LastLinePositionIndex properties on the LineSelectorData.

        if (_isMultiThreaded)
        {
            Parallel.For(0, _allLineSelectorData.Count, 
                         i => _allLineSelectorData[i].GetClosestDistance(_lastMousePosition, _maxSelectionDistance));
        }
        else
        {
            for (var i = 0; i < _allLineSelectorData.Count; i++)
                _allLineSelectorData[i].GetClosestDistance(_lastMousePosition, _maxSelectionDistance);
        }


        // Get the lines that are within _maxSelectionDistance and add them to _selectedLineSelectorData
        // We are reusing _selectedLineSelectorData list to prevent new allocations on each call of UpdateClosestLine
        
        _selectedLineSelectorData.Clear();
        foreach (var lineSelectorData in _allLineSelectorData)
        {
            if (lineSelectorData.LastDistance <= _maxSelectionDistance)
                _selectedLineSelectorData.Add(lineSelectorData);
        }
        

        LineSelectorData? closestLineSelector = null;
        Vector3 closestPositionOnLine = new Vector3();

        if (_selectedLineSelectorData.Count > 0)
        {
            // We need mouse ray from the mouse position to get the closest position on the line
            var mouseRay = SceneView.GetRayFromCamera(_lastMousePosition.X, _lastMousePosition.Y);

            if (!mouseRay.IsValid)
                return;


            float closestDistance = float.MaxValue;

            if (_orderByDistance && targetPositionCamera != null)
            {
                // Order by camera distance (line that is closest to the camera is selected)

                var cameraPosition = targetPositionCamera.GetCameraPosition();

                foreach (var oneLineSelectorData in _selectedLineSelectorData)
                {
                    var oneClosestPositionOnLine = oneLineSelectorData.GetClosestPositionOnLine(mouseRay.Position, mouseRay.Direction);

                    if (float.IsNaN(oneClosestPositionOnLine.X)) // if we cannot calculate the closest position, then NaN is returned
                        continue;

                    var distanceToCamera = (cameraPosition - oneClosestPositionOnLine).LengthSquared(); // We just use length for getting the closest item, so we can use squared values

                    if (distanceToCamera < closestDistance)
                    {
                        closestDistance = distanceToCamera;
                        closestLineSelector = oneLineSelectorData;
                        closestPositionOnLine = oneClosestPositionOnLine;
                    }
                }
            }
            else
            {
                // Order by distance to the specified position (line that is closes to the specified position is selected)

                foreach (var lineSelectorData in _selectedLineSelectorData)
                {
                    if (lineSelectorData.LastDistance < closestDistance)
                    {
                        closestDistance = lineSelectorData.LastDistance;
                        closestLineSelector = lineSelectorData;
                    }
                }

                if (closestLineSelector != null)
                    closestPositionOnLine = closestLineSelector.GetClosestPositionOnLine(mouseRay.Position, mouseRay.Direction);
            }
        }

        var getClosestDistanceTime = _stopwatch.Elapsed.TotalMilliseconds;


        // The closest position on the line is shown with a SphereModelNode
        
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
                _closestPositionSphereNode.CenterPosition = closestPositionOnLine;
                _closestPositionSphereNode.Visibility = SceneNodeVisibility.Visible;
            }
        }


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


        _updateTime = calculateViewPositionsTime + getClosestDistanceTime; 

        _closestDistanceLabel?.UpdateValue();
        _lineSegmentIndexLabel?.UpdateValue();
        _updateTimeLabel?.UpdateValue();
    }

    private Vector3[] CreateRandomPositions(int pointsCount)
    {
        var positions = new Vector3[pointsCount];

        var onePosition = GetRandomPosition(centerPosition: new Vector3(0, 0, 0),
                                            areaSize: new Vector3(LinePositionsRange, LinePositionsRange, LinePositionsRange));

        // direction in range from -1 ... +1
        var lineDirection = GetRandomPosition(centerPosition: new Vector3(0, 0, 0), areaSize: new Vector3(2, 2, 2));

        var lineRightDirection = new Vector3(lineDirection.Z, lineDirection.Y, lineDirection.X); // switch X and Z to get vector to the right of lineDirection
        var lineUpDirection = new Vector3(0, 1, 0);

        var positionAdvancement = LinePositionsRange / pointsCount;
        var displacementRange = (float)Math.Max(0.1, LinePositionsRange / pointsCount);

        for (int i = 0; i < pointsCount; i++)
        {
            var vector = lineDirection * positionAdvancement;
            vector += lineUpDirection * displacementRange * (GetRandomFloat() * 2.0f - 1.0f);
            vector += lineRightDirection * displacementRange * (GetRandomFloat() * 2.0f - 1.0f);

            onePosition += vector;

            positions[i] = onePosition;
        }

        return positions;
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
    
    private void UpdateLineSegmentsCount(string? selectedText)
    {
        if (selectedText != null)
            _lineSegmentsCount = int.Parse(selectedText);
        else
            _lineSegmentsCount = 10;

        CreateSampleLines();
    }

    private void UpdateShownLinesCount(int selectedIndex, string? selectedText)
    {
        if (selectedIndex == 0)
        {
            _simpleLinesCount = 10;
            _polyLinesCount = 20;
            _multiLinesCount = 0;
        }
        else if (selectedIndex == 1)
        {
            _simpleLinesCount = 10;
            _polyLinesCount = 0;
            _multiLinesCount = 20;
        }
        else
        {
            _simpleLinesCount = 0;
            _multiLinesCount = 0;

            if (selectedText != null)
            {
                var selectedTextParts = selectedText.Split(' ');
                _polyLinesCount = int.Parse(selectedTextParts[0]);
            }
            else
            {
                _polyLinesCount = 0;
            }
        }

        CreateSampleLines();
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        _closestDistanceLabel = ui.CreateKeyValueLabel("Distance to closest line:",
                                                       () => _closestLineDistance >= 0 ? _closestLineDistance.ToString("F1") : "");

        _lineSegmentIndexLabel = ui.CreateKeyValueLabel("Line segment index:", 
                                                        () => _lineSegmentIndex >= 0 ? _lineSegmentIndex.ToString() : "");

        _updateTimeLabel = ui.CreateKeyValueLabel("Update time:", 
                                                  () => _updateTime >= 0 ? $"{_updateTime:F2} ms" : "");

        
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


        ui.AddSeparator();
        ui.CreateCheckBox("Order by distance (?):When unchecked the line that is closest to the mouse will be selected even if it is behind some other line in 3D space.",
                          _orderByDistance, 
                          (isChecked) => _orderByDistance = isChecked);
        
        
        ui.AddSeparator();
        ui.CreateCheckBox("Check BoundingBox (?):When checked this significantly improves performance for lines with many segments\nbecause individual segment are checked only after initial bounding box check.\nIncrease number of shown lines and segments count to see this in effect.",
                          _checkBoundingBox,
                          (isChecked) =>
                          {
                              _checkBoundingBox = isChecked;
                              foreach (var lineSelectorData in _allLineSelectorData)
                                  lineSelectorData.CheckBoundingBox = _checkBoundingBox;
                          });
        
        
        ui.AddSeparator();
        ui.CreateCheckBox("Use multiple threads (?):When checked then Parallel.For is used to get the closest line (useful when there are many 3D lines in the scene).",
                          _isMultiThreaded,
                          (isChecked) => _isMultiThreaded = isChecked);


        ui.AddSeparator();
        ui.CreateLabel("Shown lines:");

        ui.CreateComboBox(new string[] { "10 lines + 20 poly-lines", "10 lines + 20 multi-lines", "50 poly-lines", "100 poly-lines", "500 poly-lines" },
            (selectedIndex, selectedText) => UpdateShownLinesCount(selectedIndex, selectedText),
            selectedItemIndex: 0);


        ui.AddSeparator();
        ui.CreateComboBox(new string[] { "10", "50", "100", "500", "1000", "5000" },
            (selectedIndex, selectedText) => UpdateLineSegmentsCount(selectedText),
            selectedItemIndex: 0,
            keyText: "Line segments count:");


        ui.AddSeparator();
        _startStopCameraButton = ui.CreateButton("Stop camera rotation", () => StartStopCameraRotation());


        // Subscribe to mouse (pointer) moved
        ui.RegisterPointerMoved(ProcessPointerMove); 
    }
}