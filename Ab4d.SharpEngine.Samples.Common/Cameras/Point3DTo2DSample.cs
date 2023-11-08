using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;

namespace Ab4d.SharpEngine.Samples.Common.Cameras;

public abstract class Point3DTo2DSample : CommonSample
{
    public override string Title => "Point3DTo2D sample";
    public override string Subtitle => "Convert 3D position to 2D screen position and show UI elements on top of 3D scene";

    private SphereModelNode? _sphereModelNode;

    public Point3DTo2DSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        var planeModelNode = new PlaneModelNode()
        {
            Size = new Vector2(700, 700),
            Material = StandardMaterials.LightGray,
            BackMaterial = StandardMaterials.DimGray,
        };

        scene.RootNode.Add(planeModelNode);


        _sphereModelNode = new SphereModelNode()
        {
            CenterPosition = new Vector3(200, 50, 0),
            Radius = 40,
            Material = StandardMaterials.Gold.SetSpecular(16)
        };

        scene.RootNode.Add(_sphereModelNode);


        if (targetPositionCamera != null)
        {
            targetPositionCamera.CameraChanged += OnCameraChanged;

            targetPositionCamera.StartRotation(headingChangeInSecond: 50);
        }

        ShowCameraAxisPanel = true;
    }

    private void OnCameraChanged(object? sender, EventArgs e)
    {
        if (SceneView == null || _sphereModelNode == null)
            return;

        // Point3DTo2D method converts 3D position to 2D screen position
        var screenPosition = SceneView.Point3DTo2D(_sphereModelNode.CenterPosition);

        // The current version of SharpEngine returns the screenPosition on the view that is defined by SceneView
        // In case of DPI scale this view is larger than the size of the final UI element that will show the control.
        // Therefore we need to scale down the position.
        // In the next version we could simply use the adjustByDpiScale parameter:
        // Point3DTo2D(_sphereModelNode.CenterPosition, adjustByDpiScale: true)
        screenPosition = new Vector2(screenPosition.X / SceneView.DpiScaleX, screenPosition.Y / SceneView.DpiScaleY);


        // When converting multiple positions, you can also an optimized version that can convert positions in parallel for:
        //SceneView.Points3DTo2D(points3D, points2D, transform, useParallelFor);

        // To convert 3D line to screen position with clipping the line to screen edges, you can use:
        //(Vector2 startPositionScreen, Vector2 endPositionScreen) = SceneView.Line3DTo2D(startPosition3D, endPosition3D);

        // To get 2D bounding box, use:
        //(Vector2 minimumScreen, Vector2 maximumScreen) = SceneView.BoundingBox3DTo2D(boundingBox3D);

        // You can also convert 3D positions to 2D positions by using a custom camera and custom view size:
        //var screenPosition = SceneView.Point3DTo2D(point3D, camera, viewWidth, viewHeight);
        //SceneView.Points3DTo2D(points3D, points2D, camera, viewWidth, viewHeight, transform, useParallelFor);


        // Update UI
        OnSphereScreenPositionChanged(screenPosition);
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        CreateCustomUI(ui);
    }

    protected abstract void OnSphereScreenPositionChanged(Vector2 screenPosition);

    protected abstract void CreateCustomUI(ICommonSampleUIProvider ui);
}