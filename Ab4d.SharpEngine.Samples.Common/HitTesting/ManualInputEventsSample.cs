using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.HitTesting;

// This sample must be derived from to subscribe to mouse events - this is platform specific and 
// needs to be done differently for WPF, Avalonia and WinUI

public abstract class ManualInputEventsSample : CommonSample
{
    public override string Title => "Manually implemented mouse events and model collision";
    public override string Subtitle => "Use left mouse button to select and drag the boxes.\nUse right mouse button to rotate and move the camera (with CTRL key).";

    private readonly float _dragMouseDistance = 3;

    // Define the dragging plane
    private readonly Vector3 _dragPlaneNormal = new Vector3(0, 1, 0);
    private readonly Vector3 _dragPlanePoint = new Vector3(0, 0, 0); // position on a plane

    private StandardMaterial _normalMaterial = StandardMaterials.Silver;
    private StandardMaterial _selectedMaterial = StandardMaterials.Yellow;
    private StandardMaterial _clickedMaterial = StandardMaterials.Green;
    private StandardMaterial _draggedMaterial = StandardMaterials.Orange;
    private StandardMaterial _collidedMaterial = StandardMaterials.Red;

    protected bool isMouseDraggingEnabled = true;      // This is set in a derived class
    protected bool isCollisionDetectionEnabled = true; // This is set in a derived class
    
    protected bool isLeftMouseButtonPressed;           // This is set in a derived class
    
    private Vector2 _pressedMouseLocation;

    private bool _isMouseDragging;
    private bool _isCollided;
    private Vector3? _dragStartPosition;
    private Vector3 _startModelOffset;
    private TranslateTransform? _modelTranslateTransform;

    private ModelNode? _lastHitModel;
    private ModelNode? _pressedHitModel;

    private HashSet<ModelNode> _clickedModels = new HashSet<ModelNode>();

    private GroupNode? _boxesGroupNode;

    public ManualInputEventsSample(ICommonSamplesContext context)
        : base(context)
    {
        RotateCameraConditions = MouseAndKeyboardConditions.RightMouseButtonPressed;
        MoveCameraConditions= MouseAndKeyboardConditions.RightMouseButtonPressed | MouseAndKeyboardConditions.ControlKey;
    }


    // The following methods need to be implemented in a derived class:
    protected abstract void ShowMessage(string message);

    protected abstract void SubscribeMouseEvents(ISharpEngineSceneView sharpEngineSceneView);

    protected abstract void CreateCustomUI(ICommonSampleUIProvider ui);


    protected override void OnCreateScene(Scene scene)
    {
        var wireGridNode = new WireGridNode()
        {
            Size = new Vector2(1000, 1000),
            HeightCellsCount = 10,
            WidthCellsCount = 10,
            MajorLineThickness = 3
        };

        scene.RootNode.Add(wireGridNode);


        // Create 7 x 7 boxes with different height

        _boxesGroupNode = new GroupNode("BoxesGroup");

        for (int y = -3; y <= 3; y++)
        {
            for (int x = -3; x <= 3; x++)
            {
                // Height is based on the distance from the center
                float height = (5 - MathF.Sqrt(x * x + y * y)) * 60;

                // Create the 3D Box visual element
                var boxModel = new BoxModelNode()
                {
                    Position = new Vector3(x * 100, height / 2, y * 100),
                    Size = new Vector3(80, height, 80),
                    Material = _normalMaterial,
                    Name = $"Box_{x + 4}_{y + 4}"
                };

                _boxesGroupNode.Add(boxModel);
            }
        }

        scene.RootNode.Add(_boxesGroupNode);
    }
    
    #region Mouse events processing
    protected void ProcessMouseButtonPress(Vector2 mousePosition)
    {
        ShowMessage("Left mouse button pressed");

        _pressedHitModel = _lastHitModel;
        _pressedMouseLocation = new Vector2(mousePosition.X, mousePosition.Y);
    }

    protected void ProcessMouseButtonRelease(Vector2 mousePosition)
    {
        ShowMessage("Left mouse button released");

        // Mouse click occurs when the mouse was pressed and released on the same object
        if (_pressedHitModel != null && _lastHitModel == _pressedHitModel && !_isMouseDragging)
        {
            ShowMessage($"{_pressedHitModel.Name} CLICKED");
            OnModelMouseClick(_pressedHitModel, mousePosition);
        }

        if (_isMouseDragging && _pressedHitModel != null)
        {
            ShowMessage($"{_pressedHitModel.Name} DRAGGING ENDED");
            OnModelEndMouseDrag(_pressedHitModel, mousePosition);

            _isMouseDragging = false;
        }

        _pressedHitModel = null;
    }

    protected void ProcessMouseMove(Vector2 mousePosition)
    {
        ShowMessage($"Mouse moved to {mousePosition.X:F1} {mousePosition.Y:F1}");

        if (_isMouseDragging)
        {
            if (_pressedHitModel != null)
                OnModelMouseDrag(_pressedHitModel, mousePosition);

            return;
        }

        if (isLeftMouseButtonPressed && _pressedHitModel != null)
        {
            var distance = Vector2.Subtract(mousePosition, _pressedMouseLocation).Length();

            if (distance >= _dragMouseDistance && isMouseDraggingEnabled)
            {
                ShowMessage($"{_pressedHitModel.Name} DRAGGING STARTED");
                OnModelBeginMouseDrag(_pressedHitModel, mousePosition);

                _isMouseDragging = true;
                return;
            }
        }

        var hitModel = GetHitModel(mousePosition);

        if (hitModel == _lastHitModel)
            return;

        if (_lastHitModel != null && _lastHitModel.Material == _selectedMaterial) // do not change _clickedMaterial 
        {
            ShowMessage($"{_lastHitModel.Name} MOUSE LEAVE");
            OnModelMouseLeave(_lastHitModel, mousePosition);
        }

        if (hitModel != null)
        {
            ShowMessage($"{hitModel.Name} MOUSE ENTER");
            OnModelMouseEnter(hitModel, mousePosition);
        }

        _lastHitModel = hitModel;
    }
    #endregion

    #region 3D Model mouse events
    private void OnModelMouseEnter(ModelNode modelNode, Vector2 mousePoint)
    {
        if (modelNode.Material == _normalMaterial) // do not change _clickedMaterial 
            modelNode.Material = _selectedMaterial;
    }

    private void OnModelMouseLeave(ModelNode modelNode, Vector2 mousePoint)
    {
        modelNode.Material = _normalMaterial;
    }

    private void OnModelMouseClick(ModelNode modelNode, Vector2 mousePoint)
    {
        if (_clickedModels.Contains(modelNode))
        {
            // Remove model from the clicked models
            modelNode.Material = _selectedMaterial; // mouse is still over the element
            _clickedModels.Remove(modelNode);
        }
        else
        {
            // Add model to clicked models
            modelNode.Material = _clickedMaterial;
            _clickedModels.Add(modelNode);
        }
    }

    private void OnModelBeginMouseDrag(ModelNode modelNode, Vector2 mousePoint)
    {
        modelNode.Material = _draggedMaterial;

        // Gets the mouse position on the XZ plane - this is a start 3D position for dragging
        _dragStartPosition = GetPositionOnHorizontalPlane(mousePoint);

        if (_dragStartPosition != null)
        {
            _modelTranslateTransform = modelNode.Transform as TranslateTransform;
            if (_modelTranslateTransform == null)
            {
                _modelTranslateTransform = new TranslateTransform();
                modelNode.Transform = _modelTranslateTransform;
            }

            _startModelOffset = new Vector3(_modelTranslateTransform.X, _modelTranslateTransform.Y, _modelTranslateTransform.Z);
        }
    }

    private void OnModelEndMouseDrag(ModelNode modelNode, Vector2 mousePoint)
    {
        if (_clickedModels.Contains(modelNode))
            modelNode.Material = _clickedMaterial;
        else
            modelNode.Material = _normalMaterial;

        _dragStartPosition = null;
        _modelTranslateTransform = null;
    }

    private void OnModelMouseDrag(ModelNode modelNode, Vector2 mousePoint)
    {
        if (_dragStartPosition == null || _modelTranslateTransform == null)
            return;

        // Gets the mouse position on the XZ plane - this is a start 3D position for dragging
        var currentDragPosition = GetPositionOnHorizontalPlane(mousePoint);

        if (currentDragPosition != null)
        {
            var draggedVector = currentDragPosition.Value - _dragStartPosition.Value;

            var savedX = _modelTranslateTransform.X;
            var savedZ = _modelTranslateTransform.Z;

            _modelTranslateTransform.X = _startModelOffset.X + draggedVector.X;
            _modelTranslateTransform.Z = _startModelOffset.Z + draggedVector.Z;

            if (_pressedHitModel != null)
            {
                List<ModelNode>? collidedModels;

                if (isCollisionDetectionEnabled)
                    collidedModels = GetCollidedModels(_pressedHitModel, _boxesGroupNode);
                else
                    collidedModels = null;

                if (collidedModels != null)
                {
                    _modelTranslateTransform.X = savedX;
                    _modelTranslateTransform.Z = savedZ;

                    if (!_isCollided)
                    {
                        _pressedHitModel.Material = _collidedMaterial;
                        _isCollided = true;
                    }

                    ShowMessage("COLLIDED with " + string.Join(", ", collidedModels.Select(m => m.Name)));
                }
                else
                {
                    if (_isCollided)
                    {
                        _pressedHitModel.Material = _draggedMaterial;
                        _isCollided = false;
                    }

                    ShowMessage($"DRAGGED for {draggedVector.X:F1} {draggedVector.Z:F1}");
                }
            }
        }
    }
    #endregion

    #region Collision detection
    // Gets models from groupNode that collide with modelNode
    // The method only checks 2D top-down collisions
    private List<ModelNode>? GetCollidedModels(ModelNode modelNode, GroupNode? groupNode)
    {
        // First get bounding box
        // Because all the tested object have the same GroupNode parent, 
        // we can use local BoundingBox. Otherwise WorldBoundingBox should be used.
        var boundingBox = modelNode.GetLocalBoundingBox(updateIfDirty: true);

        if (boundingBox.IsEmpty || groupNode == null || groupNode.Count == 0)
            return null;

        // The collision detection will be done in 2D (top down),
        // so convert 3D bounding box to 2D rect

        var rect1Min = new Vector2(boundingBox.Minimum.X, boundingBox.Minimum.Z);
        var rect1Max = new Vector2(boundingBox.Maximum.X, boundingBox.Maximum.Z);


        // Now go through all child nodes in groupNode and check for intersections
        List<ModelNode>? collidedModels = null;

        foreach (var childNode in groupNode.OfType<ModelNode>())
        {
            if (ReferenceEquals(modelNode, childNode))
                continue;

            boundingBox = childNode.GetLocalBoundingBox(updateIfDirty: true);

            if (boundingBox.IsEmpty)
                continue;

            var rect2Min = new Vector2(boundingBox.Minimum.X, boundingBox.Minimum.Z);
            var rect2Max = new Vector2(boundingBox.Maximum.X, boundingBox.Maximum.Z);

            if (rect2Min.X <= rect1Max.X && rect2Max.X >= rect1Min.X &&
                rect2Min.Y <= rect1Max.Y && rect2Max.Y >= rect1Min.Y)
            {
                collidedModels ??= new List<ModelNode>();
                collidedModels.Add(childNode);
            }
        }

        return collidedModels;
    }
    #endregion

    #region Helper methods
    private ModelNode? GetHitModel(Vector2 mousePosition)
    {
        if (SceneView == null)
            return null;

        var rayHitTestResult = SceneView.GetClosestHitObject(mousePosition.X, mousePosition.Y);

        if (rayHitTestResult == null)
            return null;

        return rayHitTestResult.HitSceneNode as ModelNode;
    }

    private Vector3? GetPositionOnHorizontalPlane(Vector2 mousePosition)
    {
        if (SceneView == null)
            return null;

        var rayFromCamera = SceneView.GetRayFromCamera(mousePosition.X, mousePosition.Y);

        // Get intersection of ray created from mouse position and the horizontal plane (position: 0,0,0; normal: 0,1,0)
        bool hasIntersection = Ab4d.SharpEngine.Utilities.MathUtils.RayPlaneIntersection(rayOrigin: rayFromCamera.Position,
                                                                                         rayDirection: rayFromCamera.Direction,
                                                                                         pointOnPlane: _dragPlanePoint,
                                                                                         planeNormal: _dragPlaneNormal,
                                                                                         intersectionPoint: out var intersectionPoint);

        if (!hasIntersection)
            return null;

        return intersectionPoint;
    }

    #endregion

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        CreateCustomUI(ui);

        if (context.CurrentSharpEngineSceneView != null)
            SubscribeMouseEvents(context.CurrentSharpEngineSceneView);
    }
}
