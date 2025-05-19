using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using System.Numerics;
using System.Text;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.HitTesting;

public class HitTestingSample : CommonSample
{
    public override string Title => "Simple hit-testing sample";

    private const int MaxShownHitTestResults = 100;

    private int _wireCrossIndex;
    private List<WireCrossNode>? _wireCrosses;

    private LineMaterial? _wireCrossLineMaterial;

    private GroupNode? _testObjectsGroup;
    private GroupNode? _wireCrossesGroup;

    private Vector2 _lastPointerPosition;
    
    private GroupNode? _teapotModelNode;

    private bool _getAllHitObjects = false;
    
    private ICommonSampleUIElement? _startStopCameraButton;
    private ICommonSampleUIElement? _hitPositionsTextBox;
    
    private StringBuilder _hitPositionsText = new StringBuilder();

    public HitTestingSample(ICommonSamplesContext context)
        : base(context)
    {
        RotateCameraConditions = PointerAndKeyboardConditions.RightPointerButtonPressed;
        MoveCameraConditions= PointerAndKeyboardConditions.RightPointerButtonPressed | PointerAndKeyboardConditions.ControlKey;
    }

    protected override void OnCreateScene(Scene scene)
    {
        _testObjectsGroup = new GroupNode("TestObjectsGroup");
        _wireCrossesGroup = new GroupNode("WireCrossesGroup");

        scene.RootNode.Add(_testObjectsGroup);
        scene.RootNode.Add(_wireCrossesGroup);

        ShowTeapot();
        
        if (targetPositionCamera != null)
        {
            targetPositionCamera.Distance = 500;
            targetPositionCamera.StartRotation(50);

            targetPositionCamera.CameraChanged += OnCameraChanged;
        }

        ShowCameraAxisPanel = true;
    }

    protected override void OnDisposed()
    {
        if (targetPositionCamera != null)
            targetPositionCamera.CameraChanged -= OnCameraChanged;

        base.OnDisposed();
    }

    private void OnCameraChanged(object? sender, EventArgs args)
    {
        TestHitObjects(_lastPointerPosition);
    }

    private void TestHitObjects(Vector2 pointerPosition)
    {
        if (SceneView == null)
            return;

        _hitPositionsText.Clear();

        List<RayHitTestResult>? allHitTestResults;

        if (_getAllHitObjects)
        {
            allHitTestResults = SceneView.GetAllHitObjects(pointerPosition.X, pointerPosition.Y);
        }
        else
        {
            var closestHitTestResult = SceneView.GetClosestHitObject(pointerPosition.X, pointerPosition.Y);
            
            if (closestHitTestResult != null)
            {
                allHitTestResults = new List<RayHitTestResult>()
                {
                    closestHitTestResult
                };
            }
            else
            {
                allHitTestResults = null;
            }
        }

        if (_hitPositionsTextBox != null)
        {
            if (allHitTestResults == null || allHitTestResults.Count == 0)
            {
                _hitPositionsText.Append("No hit");
            }
            else if (allHitTestResults.Count == 1)
            {
                var hitPosition = allHitTestResults[0].HitPosition;

                _hitPositionsText.AppendLine("Hit position:");
                _hitPositionsText.AppendLine($"{hitPosition.X:F0} {hitPosition.Y:F0} {hitPosition.Z:F0}");
                
                AddWireCross(hitPosition);
            }
            else
            {
                _hitPositionsText.AppendLine("All hit positions:");

                foreach (var hitResult in allHitTestResults)
                {
                    var hitPosition = hitResult.HitPosition;

                    _hitPositionsText.Append($"{hitPosition.X:F0} {hitPosition.Y:F0} {hitPosition.Z:F0}");

                    if (hitResult.IsBackFacing)
                        _hitPositionsText.Append(" (back face)");

                    _hitPositionsText.AppendLine();

                    AddWireCross(hitPosition);
                }
            }

            _hitPositionsTextBox.SetText(_hitPositionsText.ToString());
        }
    }

    private void AddWireCross(Vector3 position)
    {
        _wireCrosses ??= new List<WireCrossNode>();

        if (_wireCrossLineMaterial == null)
            _wireCrossLineMaterial = new LineMaterial(Colors.Gold, 1);

        WireCrossNode wireCrossNode;

        if (_wireCrossIndex < MaxShownHitTestResults)
        {
            wireCrossNode = new WireCrossNode(position, linesLength: 10, _wireCrossLineMaterial);

            _wireCrosses.Add(wireCrossNode);

            if (_wireCrossesGroup != null)
                _wireCrossesGroup.Add(wireCrossNode);
        }
        else
        {
            int reusedIndex = _wireCrossIndex % MaxShownHitTestResults;
            wireCrossNode = _wireCrosses[reusedIndex];

            wireCrossNode.Position = position;
        }

        _wireCrossIndex++;
    }

    private void ShowTeapot()
    {
        if (_testObjectsGroup == null)
            return;

        _testObjectsGroup.Clear();


        string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\Models\teapot-hires.obj");

        var objImporter = new ObjImporter();
        _teapotModelNode = objImporter.Import(fileName);

        ModelUtils.PositionAndScaleSceneNode(_teapotModelNode,
                                             position: new Vector3(0, -20, 0),
                                             positionType: PositionTypes.Center,
                                             finalSize: new Vector3(300, 200, 300));

        _testObjectsGroup.Add(_teapotModelNode);
    }
    
    private void ProcessPointerMove(Vector2 pointerPosition)
    {
        TestHitObjects(pointerPosition);
        _lastPointerPosition = pointerPosition;
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
            targetPositionCamera.StartRotation(50);
            _startStopCameraButton.SetText("Stop camera rotation");
        }
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateRadioButtons(new string[] { "Get only closest hit positions", "Get all hit positions" }, (selectedIndex, selectedText) => _getAllHitObjects = selectedIndex == 1, 0);

        _hitPositionsTextBox = ui.CreateTextBox(width: 0, height: 140);

        _startStopCameraButton = ui.CreateButton("Stop camera rotation", () => StartStopCameraRotation());

        // Subscribe to mouse (pointer) moved
        ui.RegisterPointerMoved(ProcessPointerMove); 
    }
}