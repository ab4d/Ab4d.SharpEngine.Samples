using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using System.Numerics;
using System.Text;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.HitTesting;

// This sample must be derived from to subscribe to mouse events - this is platform specific and 
// needs to be done differently for WPF, Avalonia and WinUI

public abstract class HitTestingSample : CommonSample
{
    public override string Title => "Simple hit-testing sample";

    private const int MaxShownHitTestResults = 100;

    private int _wireCrossIndex;
    private List<WireCrossNode>? _wireCrosses;

    private LineMaterial? _wireCrossLineMaterial;

    private GroupNode? _testObjectsGroup;
    private GroupNode? _wireCrossesGroup;

    private Vector2 _lastMousePosition;
    
    private GroupNode? _teapotModelNode;

    private bool _getAllHitObjects = false;
    
    private ICommonSampleUIElement? _startStopCameraButton;
    private ICommonSampleUIElement? _hitPositionsTextBox;
    
    private List<Vector3> _lastHitPositions = new List<Vector3>();
    private StringBuilder _hitPositionsText = new StringBuilder();

    public HitTestingSample(ICommonSamplesContext context)
        : base(context)
    {
        RotateCameraConditions = MouseAndKeyboardConditions.RightMouseButtonPressed;
        MoveCameraConditions= MouseAndKeyboardConditions.RightMouseButtonPressed | MouseAndKeyboardConditions.ControlKey;
    }


    // The following method need to be implemented in a derived class:
    protected abstract void SubscribeMouseEvents(ISharpEngineSceneView sharpEngineSceneView);

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

            targetPositionCamera.CameraChanged += (sender, args) => TestHitObjects(_lastMousePosition);
        }

        ShowCameraAxisPanel = true;
    }

    protected void ProcessMouseMove(Vector2 mousePosition)
    {
        TestHitObjects(mousePosition);
        _lastMousePosition = mousePosition;
    }

    private void TestHitObjects(Vector2 mousePosition)
    {
        if (SceneView == null)
            return;

        _lastHitPositions.Clear();
        _hitPositionsText.Clear();

        if (_getAllHitObjects)
        {
            var allHitTestResults = SceneView.GetAllHitObjects(mousePosition.X, mousePosition.Y);

            foreach (var hitTestResult in allHitTestResults)
                _lastHitPositions.Add(hitTestResult.HitPosition);
        }
        else
        {
            var closestHitTestResult = SceneView.GetClosestHitObject(mousePosition.X, mousePosition.Y);
            
            if (closestHitTestResult != null)
                _lastHitPositions.Add(closestHitTestResult.HitPosition);
        }

        if (_hitPositionsTextBox != null)
        {
            if (_lastHitPositions.Count == 0)
            {
                _hitPositionsText.Append("No hit");
            }
            else if (_lastHitPositions.Count == 1)
            {
                _hitPositionsText.AppendLine("Hit position:");
                _hitPositionsText.AppendLine($"{_lastHitPositions[0].X:F0} {_lastHitPositions[0].Y:F0} {_lastHitPositions[0].Z:F0}");
            }
            else
            {
                _hitPositionsText.AppendLine("Hit positions:");

                foreach (var hitPosition in _lastHitPositions)
                    _hitPositionsText.AppendLine($"{hitPosition.X:F0} {hitPosition.Y:F0} {hitPosition.Z:F0}");
            }

            _hitPositionsTextBox.SetText(_hitPositionsText.ToString());
        }

        foreach (var hitPosition in _lastHitPositions)
            AddWireCross(hitPosition);
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

        var readerObj = new ReaderObj();
        _teapotModelNode = readerObj.ReadSceneNodes(fileName);

        ModelUtils.PositionAndScaleSceneNode(_teapotModelNode,
                                             position: new Vector3(0, -20, 0),
                                             positionType: PositionTypes.Center,
                                             finalSize: new Vector3(300, 200, 300));

        _testObjectsGroup.Add(_teapotModelNode);
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateRadioButtons(new string[] { "Get only closest hit positions", "Get all hit positions" }, (selectedIndex, selectedText) => _getAllHitObjects = selectedIndex == 1, 0);

        _hitPositionsTextBox = ui.CreateTextBox(width: 0, height: 140);

        _startStopCameraButton = ui.CreateButton("Stop camera rotation", () => StartStopCameraRotation());

        if (context.CurrentSharpEngineSceneView != null)
            SubscribeMouseEvents(context.CurrentSharpEngineSceneView);
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
}
