using System.Drawing;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.HitTesting;

// See also ManualInputEventsSample:
// ray to plane intersection is used to move the objects on a horizontal plane - see GetPositionOnHorizontalPlane method

public abstract class RayPlaneHitTestingSample : CommonSample
{
    public override string Title => "Ray - Plane hit testing";

    private PlaneModelNode? _planeModelNode;

    private WireCrossNode? _hitWireCrossNode;
    
    private string _lastMousePositionText = "";
    private string _lastPositionOnPlaneText = "";

    private ICommonSampleUIElement? _mousePositionLabel;
    private ICommonSampleUIElement? _positionOnPlaneLabel;


    public RayPlaneHitTestingSample(ICommonSamplesContext context)
        : base(context)
    {
    }


    // The following method need to be implemented in a derived class:
    protected abstract void SubscribeMouseEvents(ISharpEngineSceneView sharpEngineSceneView);

    protected override void OnCreateScene(Scene scene)
    {
        _planeModelNode = new PlaneModelNode()
        {
            Position = new Vector3(0, 0, 0),
            PositionType = PositionTypes.Center,
            Normal = new Vector3(0, 1, 0),
            HeightDirection = new Vector3(0, 0, 1),
            Size = new Vector2(1000, 1000),
            Material = StandardMaterials.Green,
            BackMaterial = StandardMaterials.Green
        };

        SetPlane(planeIndex: 0);

        scene.RootNode.Add(_planeModelNode);


        _hitWireCrossNode = new WireCrossNode()
        {
            Position = new Vector3(0, 0, 0),
            LineColor = Colors.Red,
            LineThickness = 3,
            LinesLength = 50
        };

        scene.RootNode.Add(_hitWireCrossNode);


        var axisLineNode = new AxisLineNode();
        scene.RootNode.Add(axisLineNode);

        ShowCameraAxisPanel = true;
    }

    protected void ProcessMouseMove(Vector2 mousePosition)
    {
        if (SceneView == null || _planeModelNode == null || _hitWireCrossNode == null)
            return;

        //Point mousePosition = e.GetPosition(MainViewport);
        //MousePositionValueTextBlock.Text = string.Format("{0:0}", mousePosition);

        // Calculate the 3D ray that goes from the mouse position into the 3D scene
        var ray = SceneView.GetRayFromCamera(mousePosition.X, mousePosition.Y);

        // Get intersection of ray created from mouse position and the current plane
        var pointOnPlane = _planeModelNode.Position;
        var planeNormal = _planeModelNode.Normal;

        var hasIntersection = MathUtils.RayPlaneIntersection(ray.Position, ray.Direction, pointOnPlane, planeNormal, out Vector3 intersectionPoint);

        // The GetMousePositionOnPlane uses the CreateMouseRay3D that creates a ray from a current camera and mouse position.
        // You can also use that method file the following code:
        //Point3D rayOrigin;
        //Vector3D rayDirection;

        //// Calculate the 3D ray that goes from the mouse position into the 3D scene
        //bool success = Camera1.CreateMouseRay3D(mousePosition, out rayOrigin, out rayDirection);


        if (hasIntersection)
        {
            float planeLimits = _planeModelNode.Size.X / 2;

            // We limit the area where we can position the sphere to the area defined by PlaneVisual
            if (Math.Abs(intersectionPoint.Z) > planeLimits || Math.Abs(intersectionPoint.X) > planeLimits)
            {
                _lastMousePositionText  = "(out of bounds)";
                _lastPositionOnPlaneText = "";

                _hitWireCrossNode.Visibility = SceneNodeVisibility.Hidden;
            }
            else
            {
                _hitWireCrossNode.Position = intersectionPoint;
                _hitWireCrossNode.Visibility = SceneNodeVisibility.Visible;

                _lastMousePositionText = $"{mousePosition.X:F0} {mousePosition.Y:F0}";
                _lastPositionOnPlaneText = $"{intersectionPoint.X:F0} {intersectionPoint.Y:F0} {intersectionPoint.Z:F0}";
            }
        }
        else
        {
            _hitWireCrossNode.Visibility = SceneNodeVisibility.Hidden;

            _lastMousePositionText  = "(no intersection)";
            _lastPositionOnPlaneText = "";
        }

        _mousePositionLabel?.UpdateValue();
        _positionOnPlaneLabel?.UpdateValue();
    }

    private void SetPlane(int planeIndex)
    {
        if (_planeModelNode == null)
            return;

        _planeModelNode.Position        = _pointsOnPlanes[planeIndex];
        _planeModelNode.Normal          = _normals[planeIndex];
        _planeModelNode.HeightDirection = _heigthDirections[planeIndex];

        _lastMousePositionText = "";
        _lastPositionOnPlaneText = "";

        _mousePositionLabel?.UpdateValue();
        _positionOnPlaneLabel?.UpdateValue();

        if (_hitWireCrossNode != null)
            _hitWireCrossNode.Visibility = SceneNodeVisibility.Hidden;
    }
    
    private void SetPlaneSize(int planeSizeIndex)
    {
        if (_planeModelNode == null)
            return;

        _planeModelNode.Size = new Vector2(_planeSizes[planeSizeIndex], _planeSizes[planeSizeIndex]);
    }

    private Vector3[] _pointsOnPlanes   = new Vector3[] { new Vector3(0, 0, 0), new Vector3(0, 30, 0), new Vector3(0, -100, 0), new Vector3(0, 30, 0),         new Vector3(0, 0, 0)};
    private Vector3[] _normals          = new Vector3[] { new Vector3(0, 1, 0), new Vector3(0, 1, 0),  new Vector3(0, 1, 0),    new Vector3(0, 0.71f, 0.71f),  new Vector3(0, 0, 1)};
    private Vector3[] _heigthDirections = new Vector3[] { new Vector3(0, 0, 1), new Vector3(0, 0, 1),  new Vector3(0, 0, 1),    new Vector3(0, 0.71f, -0.71f), new Vector3(0, 1, 0)};

    private float[] _planeSizes = new float[] { 500, 1000, 10000 };

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);
        
        ui.CreateLabel("Selected plane:", isHeader: true);
        ui.CreateRadioButtons(new string[]
            {
                "P = (0, 0, 0); N = (0, 1, 0)",
                "P = (0, 30, 0); N = (0, 1, 0)",
                "P = (0, -100, 0); N = (0, 1, 0)",
                "P = (0, 30, 0); N = (0, 0.71, 0.71)  ",
                "P = (0, 0, 0); N = (0, 0, 1)"
            }, 
            (selectedIndex, selectedText) => SetPlane(selectedIndex), 
            selectedItemIndex: 0);

        ui.CreateLabel("P: point on the plane");
        ui.CreateLabel("N: plane's normal vector");

        ui.AddSeparator();


        ui.CreateLabel("Plane size:", isHeader: true);
        ui.CreateRadioButtons(new string[]
            {
                "500 x 500",
                "1000 x 1000",
                "10000 x 10000"
            }, 
            (selectedIndex, selectedText) => SetPlaneSize(selectedIndex), 
            selectedItemIndex: 1);

        ui.AddSeparator();


        _mousePositionLabel = ui.CreateKeyValueLabel("Mouse position: ", () => _lastMousePositionText);
        _positionOnPlaneLabel = ui.CreateKeyValueLabel("Position on plane: ", () => _lastPositionOnPlaneText);

        if (context.CurrentSharpEngineSceneView != null)
            SubscribeMouseEvents(context.CurrentSharpEngineSceneView);
    }
}