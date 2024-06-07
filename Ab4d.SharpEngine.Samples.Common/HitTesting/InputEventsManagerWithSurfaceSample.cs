using System;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.HitTesting;

public class InputEventsManagerWithSurfaceSample : CommonSample
{
    public override string Title => "InputEventsManager with surface dragging";
    
    public override string Subtitle => "Drag the yellow sphere around the specified drag surface.\nThe following pointer / mouse events are demonstrated here:\n- BeginPointerDrag\n- PointerDrag\n- EndPointerDrag";


    private SphereModelNode? _movableSphere;
    private Material? _savedMaterial;
    
    private ManualInputEventsManager? _inputEventsManager;

    private BoxModelNode? _baseBox;
    private SphereModelNode? _baseSphere;
    private PlaneModelNode? _dragPlane;

    private Vector3 _startDragPosition;

    private WireCrossNode? _startDragWireCrossNode;
    private LineNode? _mainDragLine;
    private LineNode? _xDragLine, _yDragLine, _zDragLine;
    
    private bool _isPlaneDragSurface = true;
    private bool _isBottomBoxDragSurface = true;
    private bool _isBottomSphereDragSurface = true;
    private int _registeredDragPlaneIndex;

    private readonly float _rightAngleLineLength = 10;
    private readonly Vector3[] _zyRightAnglePositions = new Vector3[] { Vector3.Zero, Vector3.Zero, Vector3.Zero }; // define some temp positions, that will be later replaces by actual positions
    private readonly Vector3[] _yxRightAnglePositions = new Vector3[] { Vector3.Zero, Vector3.Zero, Vector3.Zero };
    private readonly Vector3[] _xzRightAnglePositions = new Vector3[] { Vector3.Zero, Vector3.Zero, Vector3.Zero };
    private PolyLineNode? _zyRightAngleLine, _yxRightAngleLine, _xzRightAngleLine;
    

    public InputEventsManagerWithSurfaceSample(ICommonSamplesContext context)
        : base(context)
    {
        RotateCameraConditions = PointerAndKeyboardConditions.RightPointerButtonPressed;
        MoveCameraConditions = PointerAndKeyboardConditions.RightPointerButtonPressed | PointerAndKeyboardConditions.ControlKey;

        ShowCameraAxisPanel = true;
    }

    protected override void OnCreateScene(Scene scene)
    {
        _baseBox = new BoxModelNode(centerPosition: new Vector3(0, -5, 25), size: new Vector3(1000, 10, 650), material: StandardMaterials.LightGray, "Box base");
        scene.RootNode.Add(_baseBox);


        _baseSphere = new SphereModelNode(new Vector3(0, -100, 0), 200, material: StandardMaterials.LightGray, "Sphere base");
        scene.RootNode.Add(_baseSphere);


        _dragPlane = new PlaneModelNode(centerPosition: new Vector3(0, 0, -200), 
                                        size: new Vector2(1000, 1000), 
                                        normal: new Vector3(0, 0, 1), 
                                        heightDirection: new Vector3(0, 1, 0), 
                                        name: "DragPlane")
        {
            Material = StandardMaterials.Gray.SetOpacity(opacity: 0.2f),
        };

        _dragPlane.BackMaterial = _dragPlane.Material;

        scene.RootNode.Add(_dragPlane);
    }

    /// <inheritdoc />
    protected override void OnInputEventsManagerInitialized(ManualInputEventsManager inputEventsManager)
    {
        _inputEventsManager = inputEventsManager;

        // UpdateDragSurfaces registers or removes drag surfaces from InputEventsManager
        UpdateDragSurfaces();


        var scene = inputEventsManager.SceneView.Scene;
        
        //
        // Define lines that will show where we moved the sphere
        //
        _startDragWireCrossNode = new WireCrossNode(new Vector3(0, 0, 0), lineColor: Colors.Yellow, lineLength: 40, lineThickness: 3, name: "StartDragWireCross");
        scene.RootNode.Add(_startDragWireCrossNode);

        _mainDragLine = new LineNode(new Vector3(0, 0, 0), new Vector3(10, 10, 10), Colors.Blue, lineThickness: 2, name: "MainDragLine");
        scene.RootNode.Add(_mainDragLine);

        // Drag lines are shows in the following order: z, y and x
        var smallDragLineMaterial = new LineMaterial(Colors.DeepSkyBlue, lineThickness: 1)
        {
            DepthBias = 0.0005f, // defining DepthBias will show lines on top of 3D models 
        };

        _zDragLine = new LineNode(new Vector3(0, 0, 0), new Vector3(0, 0, 10), smallDragLineMaterial, name: "ZDragLine");
        _yDragLine = new LineNode(new Vector3(0, 0, 0), new Vector3(0, 10, 0), smallDragLineMaterial, name: "YDragLine");
        _xDragLine = new LineNode(new Vector3(0, 0, 0), new Vector3(10, 0, 0), smallDragLineMaterial, name: "ZDragLine");

        scene.RootNode.Add(_zDragLine);
        scene.RootNode.Add(_yDragLine);
        scene.RootNode.Add(_xDragLine);


        // Add 3 poly-lines that will show the right angle markers
        var smallDragPolyLineMaterial = new PolyLineMaterial(smallDragLineMaterial.LineColor, smallDragLineMaterial.LineThickness)
        {
            DepthBias = smallDragLineMaterial.DepthBias,
            MiterLimit = 2
        };

        _zyRightAngleLine = new PolyLineNode(_zyRightAnglePositions, smallDragPolyLineMaterial, name: "ZYRightAngleLine");
        _yxRightAngleLine = new PolyLineNode(_yxRightAnglePositions, smallDragPolyLineMaterial, name: "YXRightAngleLine");
        _xzRightAngleLine = new PolyLineNode(_xzRightAnglePositions, smallDragPolyLineMaterial, name: "XZRightAngleLine");
        
        scene.RootNode.Add(_zyRightAngleLine);
        scene.RootNode.Add(_yxRightAngleLine);
        scene.RootNode.Add(_xzRightAngleLine);

        HideDragLines();



        //
        // Define a yellow sphere that user can drag around
        //
        _movableSphere = new SphereModelNode(centerPosition: new Vector3(0, 0, 0), radius: 20, material: StandardMaterials.Yellow, name: "MovableSphere")
        {
            UseSharedSphereMesh = false,
            Transform = new TranslateTransform(x: 300, y: 0, z: 100)
        };
        scene.RootNode.Add(_movableSphere);


        //
        // The yellow sphere will be an event's source so user can subscribe to many pointer or mouse events
        //
        var modelNodeEventsSource = new ModelNodeEventsSource(_movableSphere);
        modelNodeEventsSource.PointerEntered += (sender, args) =>
        {
            _savedMaterial = _movableSphere.Material;
            _movableSphere.Material = StandardMaterials.Orange;
        };

        modelNodeEventsSource.PointerExited += (sender, args) =>
        {
            _movableSphere.Material = _savedMaterial;
        };

        modelNodeEventsSource.PointerPressed += (sender, args) =>
        {
            _movableSphere.Material = StandardMaterials.Red;
        };
        
        modelNodeEventsSource.PointerReleased += (sender, args) =>
        {
            _movableSphere.Material = StandardMaterials.Orange;
        };
        
        modelNodeEventsSource.BeginPointerDrag += (sender, args) =>
        {
            _startDragPosition = args.RayHitResult.HitPosition;

            if (_movableSphere.Transform is TranslateTransform translateTransform)
                translateTransform.SetTranslate(_startDragPosition);
            else
                _movableSphere.Transform = new TranslateTransform(_startDragPosition);

            HideDragLines();

            // To continue getting mouse events when mouse leaves the SharpEngineSceneView or the current Windows,
            // we need to capture the mouse. This is required in WPF, WinUI and WinForms,
            // but on Avalonia this is done automatically by Avalonia (here calling CapturePointer is a no-op).
            _inputEventsManager.CapturePointer();
        };
        
        modelNodeEventsSource.EndPointerDrag += (sender, args) =>
        {
            _movableSphere.Material = _savedMaterial;

            // Release mouse capture
            _inputEventsManager.EndPointerCapture();
        };

        modelNodeEventsSource.PointerDrag += (sender, args) =>
        {
            var newTranslatePosition = _startDragPosition + args.SurfaceHitPointDiff;

            if (_movableSphere.Transform is TranslateTransform translateTransform)
                translateTransform.SetTranslate(newTranslatePosition);
            else
                _movableSphere.Transform = new TranslateTransform(newTranslatePosition);

            UpdateDragLines(newTranslatePosition);
        };

        inputEventsManager.RegisterEventsSource(modelNodeEventsSource);
    }

    private void UpdateDragSurfaces()
    {
        if (_inputEventsManager == null)
            return;

        // Register baseBox and baseSphere as ModelNodes that user can drag on

        if (_baseBox != null)
        {
            if (_isBottomBoxDragSurface)
                _inputEventsManager.RegisterDragSurface(_baseBox);
            else
                _inputEventsManager.RemoveDragSurface(_baseBox);
        }

        if (_baseSphere != null)
        {
            if (_isBottomSphereDragSurface)
                _inputEventsManager.RegisterDragSurface(_baseSphere);
            else
                _inputEventsManager.RemoveDragSurface(_baseSphere);
        }

        if (_dragPlane != null)
        {
            // We also register an INFINITE PLANE as a drag surface (defined by normal and position on a plane)
            // Note that if we would register the dragPlane (as PlaneModelNode), then the drag surface would be limiter to only that model, 
            // but in case of registering a pane with normal and position, that drag surface is infinite.
            //
            // When RegisterDragSurface is called with planeNormal and pointOnPlane as parameters,
            // the method returns an index (int) that can be passed to the RemoveDragSurface to remove the drag plane.
            if (_isPlaneDragSurface)
                _registeredDragPlaneIndex = _inputEventsManager.RegisterDragSurface(planeNormal: _dragPlane.Normal, pointOnPlane: _dragPlane.Position);
            else
                _inputEventsManager.RemoveDragSurface(_registeredDragPlaneIndex);
        }
    }

    private void UpdateDragLines(Vector3 currentDragPosition)
    {
        if (_startDragWireCrossNode != null)
        {
            _startDragWireCrossNode.Position = _startDragPosition;            // Note that setting a property to the same value is a no-op (is returned immediately from the setter)
            _startDragWireCrossNode.Visibility = SceneNodeVisibility.Visible;
        }
        
        if (_mainDragLine != null)
        {
            _mainDragLine.StartPosition = _startDragPosition;
            _mainDragLine.EndPosition = currentDragPosition;
            _mainDragLine.Visibility = SceneNodeVisibility.Visible;
        }

        if (_xDragLine != null && _yDragLine != null && _zDragLine != null)
        {
            _zDragLine.StartPosition = _startDragPosition;
            _zDragLine.EndPosition = new Vector3(_startDragPosition.X, _startDragPosition.Y, currentDragPosition.Z);

            _yDragLine.StartPosition = _zDragLine.EndPosition;
            _yDragLine.EndPosition = new Vector3(_startDragPosition.X, currentDragPosition.Y, currentDragPosition.Z);

            _xDragLine.StartPosition = _yDragLine.EndPosition;
            _xDragLine.EndPosition = currentDragPosition;


            _zDragLine.Visibility = SceneNodeVisibility.Visible;
            _yDragLine.Visibility = SceneNodeVisibility.Visible;
            _xDragLine.Visibility = SceneNodeVisibility.Visible;

            if (_zyRightAngleLine != null && _yxRightAngleLine != null && _xzRightAngleLine != null)
            {
                var dx = currentDragPosition.X - _startDragPosition.X;
                var dy = currentDragPosition.Y - _startDragPosition.Y;
                var dz = currentDragPosition.Z - _startDragPosition.Z;

                var dxSign = Math.Sign(dx);
                var dySign = Math.Sign(dy);
                var dzSign = Math.Sign(dz);

                var minDiff = _rightAngleLineLength * 2;

                if (Math.Abs(dz) > minDiff && Math.Abs(dy) > minDiff)
                {
                    var zyPosition = _zDragLine.EndPosition;

                    _zyRightAnglePositions[0] = new Vector3(zyPosition.X, zyPosition.Y,                                  zyPosition.Z - _rightAngleLineLength * dzSign);
                    _zyRightAnglePositions[1] = new Vector3(zyPosition.X, zyPosition.Y + _rightAngleLineLength * dySign, zyPosition.Z - _rightAngleLineLength * dzSign);
                    _zyRightAnglePositions[2] = new Vector3(zyPosition.X, zyPosition.Y + _rightAngleLineLength * dySign, zyPosition.Z);

                    _zyRightAngleLine.UpdatePositions();

                    _zyRightAngleLine.Visibility = SceneNodeVisibility.Visible;
                }
                else
                {
                    _zyRightAngleLine.Visibility = SceneNodeVisibility.Hidden;
                }

                if (Math.Abs(dx) > minDiff && Math.Abs(dy) > minDiff)
                {
                    var yxPosition = _yDragLine.EndPosition;

                    _yxRightAnglePositions[0] = new Vector3(yxPosition.X + _rightAngleLineLength * dxSign, yxPosition.Y,                                  yxPosition.Z);
                    _yxRightAnglePositions[1] = new Vector3(yxPosition.X + _rightAngleLineLength * dxSign, yxPosition.Y - _rightAngleLineLength * dySign, yxPosition.Z);
                    _yxRightAnglePositions[2] = new Vector3(yxPosition.X,                                  yxPosition.Y - _rightAngleLineLength * dySign, yxPosition.Z);
                     
                    _yxRightAngleLine.UpdatePositions();
                     
                    _yxRightAngleLine.Visibility = SceneNodeVisibility.Visible;
                }
                else
                {
                    _yxRightAngleLine.Visibility = SceneNodeVisibility.Hidden;
                }

                // show xz right angle ONLY when _zyRightAngleLine is not shown (Abs(dy) <= minDiff)
                if (Math.Abs(dy) <= minDiff && Math.Abs(dx) > minDiff && Math.Abs(dz) > minDiff) 
                {
                    var xzPosition = _zDragLine.EndPosition;

                    _xzRightAnglePositions[0] = new Vector3(xzPosition.X,                                  xzPosition.Y, xzPosition.Z - _rightAngleLineLength * dzSign);
                    _xzRightAnglePositions[1] = new Vector3(xzPosition.X + _rightAngleLineLength * dxSign, xzPosition.Y, xzPosition.Z - _rightAngleLineLength * dzSign);
                    _xzRightAnglePositions[2] = new Vector3(xzPosition.X + _rightAngleLineLength * dxSign, xzPosition.Y, xzPosition.Z);

                    _xzRightAngleLine.UpdatePositions();

                    _xzRightAngleLine.Visibility = SceneNodeVisibility.Visible;
                }
                else
                {
                    _xzRightAngleLine.Visibility = SceneNodeVisibility.Hidden;
                }
            }
        }
    }

    private void HideDragLines()
    {
        if (_startDragWireCrossNode != null)
            _startDragWireCrossNode.Visibility = SceneNodeVisibility.Hidden;
        
        if (_mainDragLine != null)
            _mainDragLine.Visibility = SceneNodeVisibility.Hidden;

        if (_xDragLine != null)
            _xDragLine.Visibility = SceneNodeVisibility.Hidden;

        if (_yDragLine != null)
            _yDragLine.Visibility = SceneNodeVisibility.Hidden;

        if (_zDragLine != null)
            _zDragLine.Visibility = SceneNodeVisibility.Hidden;
        
        if (_zyRightAngleLine != null)
            _zyRightAngleLine.Visibility = SceneNodeVisibility.Hidden;
        
        if (_yxRightAngleLine != null)
            _yxRightAngleLine.Visibility = SceneNodeVisibility.Hidden;
        
        if (_xzRightAngleLine != null)
            _xzRightAngleLine.Visibility = SceneNodeVisibility.Hidden;
    }


    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("Drag surfaces:", isHeader: true);

        ui.CreateCheckBox("Plane N: (0,0,1); P: (0,0,-200)", _isPlaneDragSurface, isChecked => { _isPlaneDragSurface = isChecked; UpdateDragSurfaces(); });
        ui.CreateCheckBox("Bottom gray box", _isBottomBoxDragSurface, isChecked => { _isBottomBoxDragSurface = isChecked; UpdateDragSurfaces(); });
        ui.CreateCheckBox("Bottom gray sphere", _isBottomSphereDragSurface, isChecked => { _isBottomSphereDragSurface = isChecked; UpdateDragSurfaces(); });
    }
}