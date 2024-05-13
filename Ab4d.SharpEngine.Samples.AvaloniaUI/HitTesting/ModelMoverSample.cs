using System;
using System.Numerics;
using Ab4d.SharpEngine.AvaloniaUI;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using Avalonia;
using Avalonia.Input;
using Colors = Ab4d.SharpEngine.Common.Colors;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.HitTesting;

public class ModelMoverSample : CommonSample
{
    public override string Title => "ModelMover sample";

    private InputEventsManager? _inputEventsManager;
    private ArrowModelNode? _xAxis;
    private ModelMover _modelMover;

    public ModelMoverSample(ICommonSamplesContext context)
        : base(context)
    {
        RotateCameraConditions = MouseAndKeyboardConditions.RightMouseButtonPressed;
        MoveCameraConditions = MouseAndKeyboardConditions.RightMouseButtonPressed | MouseAndKeyboardConditions.ControlKey;

        ShowCameraAxisPanel = true;
    }

    protected override void OnCreateScene(Scene scene)
    {
        if (context.CurrentSharpEngineSceneView is not SharpEngineSceneView sharpEngineSceneView)
            return;

        var eventsSourceElement = sharpEngineSceneView.Parent as IInputElement;
        _inputEventsManager = new InputEventsManager(sharpEngineSceneView, eventsSourceElement);


        //_xAxis = new MovableAxis(_inputEventsManager, new Vector3(1, 0, 0), length: 50, 5, 10, Colors.Red);
        //scene.RootNode.Add(_xAxis.AxisModelNode);


        _modelMover = new ModelMover(_inputEventsManager);

        _modelMover.ModelMoved += (sender, args) =>
        {
            
        };


        var groupNode = new GroupNode("TestGroupNode");
        groupNode.Transform = new AxisAngleRotateTransform(new Vector3(0, 1, 0), 180);
        groupNode.Add(_modelMover.ModelMoverGroupNode);
        scene.RootNode.Add(groupNode);

        //scene.RootNode.Add(modelMover.ModelMoverGroupNode);
        

        var wireGridNode = new WireGridNode()
        {
            Size = new Vector2(300, 300),
        };

        scene.RootNode.Add(wireGridNode);


        if (targetPositionCamera != null)
            targetPositionCamera.Distance = 400;
    }

    /// <inheritdoc />
    protected override void OnDisposed()
    {
        _xAxis?.Dispose();
        _modelMover?.Dispose();

        base.OnDisposed();
    }
}