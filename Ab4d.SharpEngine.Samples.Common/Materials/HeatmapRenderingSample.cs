using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;

namespace Ab4d.SharpEngine.Samples.Common.Materials;

// This sample shows how to render a heatmap on a 3D object.
//
// In this sample the heat value is defined by the distance of an animated hit position to each teapot position
// (positions that are closer to the hit positions are shows with hotter color than positions that are farther away).
//
// This is done with first creating a 256 x 1 texture from the selected gradient (see SetGradientTexture method).
// This texture is then set as a Material to the 3D object.
// To specify which color to show for each position, we set the TextureCoordinates of the mesh.
// Because we have a one dimensional texture, the Y TextureCoordinate value is always set to 0.5.
// The X value in the TextureCoordinate defines the heat value, e.g. the color from the gradient: 0 is the first gradient color; 1 is the last gradient color.
//
// See UpdateTextureCoordinatesForDistance method.
// After this method is called, each mesh position gets its own color.
// Because we use a gradient texture, the color interpolation between positions uses the gradient,
// for example, when using the 3rd gradient when we interpolate from heat value 0.5 to heat value 1, 
// the color is interpolated by going from LightGreen, to Yellow and then to Red.
// 
// If we would use VertexColorMaterial, then the interpolation would always go directly from one color to another,
// for then interpolate from heat value 0.5 to heat value 1 would go directly from LightGreen to Red.
// Anyway, for simple interpolations with only two colors, VertexColorMaterial can be used.
//
// When using the third gradient with multiple colors it is possible to see the mesh of the teapot.
// This can be prevented by defining a mesh that have more equal distances between positions or
// by dividing each triangle into smaller triangles.

public class HeatmapRenderingSample : CommonSample
{
    public override string Title => "Heatmap";
    public override string Subtitle => "Showing a heatmap with a gradient texture and adjusting TextureCoordinates";

    private GroupNode? _testObjectsGroup;
    private MeshModelNode? _teapotMeshModelNode;
    private GpuImage? _gradientTexture;

    private WireCrossNode? _hitWireCrossNode;

    private SceneView? _subscribedSceneView;

    private DateTime _animationStartTime;

    private static readonly GradientStop[] _gradient1 = new GradientStop[]
    {
        new GradientStop(Colors.DodgerBlue, 0.0f),
        new GradientStop(Colors.Red, 1.0f),
    };
    
    private static readonly GradientStop[] _gradient2 = new GradientStop[]
    {
        new GradientStop(Colors.Yellow, 0.0f),
        new GradientStop(Colors.Red, 1.0f),
    };
    
    private static readonly GradientStop[] _gradient3 = new GradientStop[]
    {
        new GradientStop(Colors.Blue, 0.0f),
        new GradientStop(Colors.Aqua, 0.25f),
        new GradientStop(Colors.LightGreen, 0.5f),
        new GradientStop(Colors.Yellow, 0.75f),
        new GradientStop(Colors.Red, 1.0f),
    };

    private static readonly GradientStop[][] _allGradientStops = new GradientStop[][] {_gradient1, _gradient2, _gradient3 };


    public HeatmapRenderingSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        _testObjectsGroup = new GroupNode();
        scene.RootNode.Add(_testObjectsGroup);

        _hitWireCrossNode = new WireCrossNode(new Vector3(0, 0, 0), lineColor: Colors.Red, lineThickness: 2, lineLength: 20);
        _hitWireCrossNode.Visibility = SceneNodeVisibility.Hidden;
        scene.RootNode.Add(_hitWireCrossNode);

        ShowTeapot();

        if (scene.GpuDevice != null)
            SetGradientTexture(gradientIndex: 0);

        if (targetPositionCamera != null)
        {
            targetPositionCamera.TargetPosition = new Vector3(0, 20, 0);
            targetPositionCamera.Distance = 150;
        }
    }

    protected override void OnDisposed()
    {
        if (_gradientTexture != null)
        {
            _gradientTexture.Dispose();
            _gradientTexture = null;
        }

        if (_subscribedSceneView != null)
        {
            _subscribedSceneView.SceneUpdating -= SceneViewOnSceneUpdating;
            _subscribedSceneView = null;
        }

        base.OnDisposed();
    }

    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        sceneView.SceneUpdating += SceneViewOnSceneUpdating;
        _subscribedSceneView = sceneView;

        base.OnSceneViewInitialized(sceneView);
    }

    private void ShowTeapot()
    {
        if (_testObjectsGroup == null)
            return;

        _testObjectsGroup.Clear();


        string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\Models\teapot-hires.obj");

        var objImporter = new ObjImporter();
        var teapotModelNode = objImporter.Import(fileName);

        // teapotModelNode.GetLocalBoundingBox()
        // {Minimum:<-45.6221, 0, -29.923> Maximum:<52.8465, 48.2153, 31.3027>}
        //     IsUndefined: false
        //     IsZeroSize: false
        //     Maximum: {<52.8465, 48.2153, 31.3027>}
        //     Minimum: {<-45.6221, 0, -29.923>}
        //     SizeX: 98.4686
        //     SizeY: 48.2153
        //     SizeZ: 61.2257

        // Get the MeshModelNode so we will be able to update the texture coordinates
        _teapotMeshModelNode = objImporter.NamedObjects["Teapot001"] as MeshModelNode;

        _testObjectsGroup.Add(teapotModelNode);
    }

    private void SetGradientTexture(int gradientIndex)
    {
        if (_teapotMeshModelNode == null || Scene == null || Scene.GpuDevice == null)
            return;


        if (_gradientTexture != null)
            _gradientTexture.Dispose();

        _gradientTexture = TextureFactory.CreateGradientTexture(Scene.GpuDevice, _allGradientStops[gradientIndex], textureWidth: 256);

        _teapotMeshModelNode.Material = new StandardMaterial(_gradientTexture);
    }

    private void UpdateTextureCoordinatesForDistance(Vector3 targetPosition, float maxDistance)
    {
        if (_teapotMeshModelNode == null)
            return;

        var geometryMesh = _teapotMeshModelNode.Mesh as GeometryMesh;

        if (geometryMesh == null)
            return;

        var positions = geometryMesh.Positions;
        var textureCoordinates = geometryMesh.TextureCoordinates;

        if (positions == null || textureCoordinates == null)
            return;


        var positionsCount = positions.Length;

        for (int i = 0; i < positionsCount; i++)
        {
            var onePosition = positions[i];

            // Get distance of this position from the targetPosition
            float length = (onePosition - targetPosition).Length();
            if (length > maxDistance)
                length = maxDistance;

            // set the relative color index that is a number from 0 to 1 where 0 is the first color in the gradient and 1 is the last color in the gradient
            float relativeColorIndex = length / maxDistance;

            // Set X texture coordinate to the color in the _gradientColorsArray. 
            // We also invert the color so the colors starts from the bottom up.
            textureCoordinates[i] = new Vector2(1.0f - relativeColorIndex, 0.5f);
        }

        // After changing the TextureCoordinates we need to update the underlying buffer (we can preserve the BoundingBox because no position was changed)
        geometryMesh.UpdateMesh(geometryMesh.BoundingBox);
    }
    
    private void SceneViewOnSceneUpdating(object? sender, EventArgs e)
    {
        if (SceneView == null)
            return;

        if (_animationStartTime == DateTime.MinValue)
        {
            _animationStartTime = DateTime.Now;
            return;
        }

        float elapsedSeconds = (float)(DateTime.Now - _animationStartTime).TotalSeconds;


        // Position around center of the OverlayCanvas and with radius = 100 and in clockwise direction (negate the elapsedSeconds)
        float xPos = MathF.Sin(-elapsedSeconds * 2) * 100 + (float)SceneView.Width  / (2 * SceneView.DpiScaleX);
        float yPos = MathF.Cos(-elapsedSeconds * 2) * 100 + (float)SceneView.Height / (2 * SceneView.DpiScaleY);

        // Simulate mouse hit at the specified positions
        ProcessMouseHit(new Vector2(xPos, yPos));
    }

    private void ProcessMouseHit(Vector2 mousePosition)
    {
        if (SceneView == null)
            return;

        var rayHitTestResult = SceneView.GetClosestHitObject(mousePosition.X, mousePosition.Y);

        if (rayHitTestResult != null)
        {
            UpdateTextureCoordinatesForDistance(rayHitTestResult.HitPosition, 50);

            if (_hitWireCrossNode != null)
            {
                _hitWireCrossNode.Position = rayHitTestResult.HitPosition;
                _hitWireCrossNode.Visibility = SceneNodeVisibility.Visible;
            }
        }
        else
        {
            // use some distant position for hit point so that we get the whole model in the lowest color
            UpdateTextureCoordinatesForDistance(new Vector3(10000, 0, 0), 50);

            if (_hitWireCrossNode != null)
                _hitWireCrossNode.Visibility = SceneNodeVisibility.Hidden;
        }
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateRadioButtons(new string[] { "Gradient 1", "Gradient 2", "Gradient 3" }, (selectedIndex, selectedText) => SetGradientTexture(gradientIndex: selectedIndex), selectedItemIndex: 0);
    }
}