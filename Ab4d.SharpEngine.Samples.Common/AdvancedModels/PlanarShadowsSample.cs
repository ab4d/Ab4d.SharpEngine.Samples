using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class PlanarShadowsSample : CommonSample
{
    public override string Title => "Planar Shadows";

    private const float HalfAnimationHeight = 40f;
    private const float AnimationMinHeight = 15f;

    private bool _isTransparentMaterial = true;

    private PointLight? _shadowPointLight;
    private DirectionalLight? _shadowDirectionalLight;
    private AmbientLight? _ambientLight;

    private Light? _currentShadowLight;

    private float _lightVerticalAngle;
    private float _lightHorizontalAngle;
    private float _lightDistance;

    private DateTime _lastAnimationTime;

    private GroupNode? _sampleObjectsGroupNode;
    private PlaneModelNode? _planeModelNode;

    private PlanarShadowMeshCreator? _planarShadowMeshCreator;

    private StandardMaterial _solidShadowMaterial;
    private StandardMaterial _transparentShadowMaterial;

    private MeshModelNode? _shadowModel;
    
    public PlanarShadowsSample(ICommonSamplesContext context)
        : base(context)
    {
        _solidShadowMaterial = StandardMaterials.LightGray;
        _transparentShadowMaterial = new StandardMaterial(Color3.Black.ToColor4(alpha: 0.5f));
    }

    protected override void OnCreateScene(Scene scene)
    {
        _planeModelNode = new PlaneModelNode()
        {
            Position = new Vector3(0, 0, 0),
            Size = new Vector2(400, 400),
            Normal = new Vector3(0, 1, 0),
            HeightDirection = new Vector3(0, 0, -1),
            Material = new StandardMaterial(@"Resources\Textures\10x10-texture.png", BitmapIO)
        };

        scene.RootNode.Add(_planeModelNode);


        _sampleObjectsGroupNode = new GroupNode();
        scene.RootNode.Add(_sampleObjectsGroupNode);


        // Create 10 spheres, prevent creating two spheres on the same XZ coordinates
        CreateTestSpheres();


        // Create PlanarShadowMeshCreator
        _planarShadowMeshCreator = new PlanarShadowMeshCreator(_sampleObjectsGroupNode);
        _planarShadowMeshCreator.SetPlane(_planeModelNode.GetCenterPosition(), _planeModelNode.Normal, _planeModelNode.HeightDirection, _planeModelNode.Size);
        _planarShadowMeshCreator.ClipToPlane = true;


        _lightHorizontalAngle = -60;
        _lightVerticalAngle = 60;
        _lightDistance = 500;

        _ambientLight = new AmbientLight(0.2f);

        _shadowPointLight = new PointLight();
        _shadowDirectionalLight = new DirectionalLight();

        SetShadowLight(isDirectionalLight: true);

        UpdateLights();

        UpdateShadowModel();


        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 10;
            targetPositionCamera.Distance = 800;
            targetPositionCamera.ShowCameraLight = ShowCameraLightType.Never; // prevent adding camera's light
        }
    }

    protected override void OnDisposed()
    {
        StopAnimation();

        base.OnDisposed();
    }

    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        StartAnimation();

        base.OnSceneViewInitialized(sceneView);
    }

    private void CreateTestSpheres()
    {
        if (_sampleObjectsGroupNode == null || _planeModelNode == null)
            return;

        _sampleObjectsGroupNode.Clear();

        var spherePositions = new List<Vector3>();

        var rnd = new Random();

        float planeCellSize = _planeModelNode.Size.X / 10;

        while (spherePositions.Count < 10)
        {
            int cellXIndex = rnd.Next(10);
            int cellYIndex = rnd.Next(10);

            var spherePosition = new Vector3((cellXIndex - 5) * planeCellSize + planeCellSize / 2,
                                             0,
                                             (cellYIndex - 5) * planeCellSize + planeCellSize / 2);

            // Check if this position was already taken
            if (spherePositions.Any(p => MathUtils.IsSame(p.X, spherePosition.X) && MathUtils.IsSame(p.Z, spherePosition.Z)))
                continue;

            spherePositions.Add(spherePosition);


            // t defines an animation time between 0 and 1.
            float t = rnd.NextSingle();

            var sphereVisual3D = new SphereModelNode()
            {
                CenterPosition = spherePosition,
                Radius = planeCellSize * 0.25f,
                Material = new StandardMaterial(GetRandomColor3()),
                Tag = t,
            };

            sphereVisual3D.Transform = new TranslateTransform(0, GetAnimatedHeight(t), 0);

            _sampleObjectsGroupNode.Add(sphereVisual3D);
        }
    }

    private void StartAnimation()
    {
        if (SceneView != null)
            SceneView.SceneUpdating += SceneViewOnSceneUpdating;
    }

    private void StopAnimation()
    {
        if (SceneView != null)
            SceneView.SceneUpdating -= SceneViewOnSceneUpdating;
    }

    private void UpdateShadowModel()
    {
        if (_planarShadowMeshCreator == null || Scene == null)
            return;

        // PlanarShadowMeshCreator generates a MeshGeometry3D that represents a shadow that is flattened to the plane.
        if (_currentShadowLight == _shadowDirectionalLight)
        {
            if (_shadowDirectionalLight != null)
                _planarShadowMeshCreator.ApplyDirectionalLight(_shadowDirectionalLight.Direction);
        }
        else
        {
            if (_shadowPointLight != null)
                _planarShadowMeshCreator.ApplyPointLight(_shadowPointLight.Position);
        }

        if (_planarShadowMeshCreator.ShadowMesh == null)
        {
            if (_shadowModel != null)
                _shadowModel.Mesh = null;

            return;
        }

        if (_shadowModel == null)
        {
            var shadowMaterial = _isTransparentMaterial ? _transparentShadowMaterial : _solidShadowMaterial;

            _shadowModel = new MeshModelNode(_planarShadowMeshCreator.ShadowMesh, shadowMaterial);
            _shadowModel.Transform = new TranslateTransform(0, 0.05f, 0); // Lift the shadow 3D model slightly above the ground

            Scene.RootNode.Add(_shadowModel);
        }
        else
        {
            _shadowModel.Mesh = _planarShadowMeshCreator.ShadowMesh;
        }
    }

    private void SceneViewOnSceneUpdating(object? sender, EventArgs e)
    {
        var now = DateTime.Now;

        if (_lastAnimationTime != DateTime.MinValue)
        {
            double elapsedSeconds = (now - _lastAnimationTime).TotalSeconds;
            AnimateAllObjects((float)elapsedSeconds * 0.5f); // take 2 seconds for one animation
        }

        _lastAnimationTime = now;
    }

    private float GetAnimatedHeight(float t)
    {
        return MathF.Sin(t * MathF.PI * 2f) // make new sin cycle on each whole value of t
               * HalfAnimationHeight        // adjust result to be between -HalfAnimationHeight to +HalfAnimationHeight
               + HalfAnimationHeight        // make the result positive: between 0 and 2 * HalfAnimationHeight
               + AnimationMinHeight;        // add min height
    }

    private void AnimateAllObjects(float dt)
    {
        if (_sampleObjectsGroupNode == null)
            return;

        foreach (var oneSphereModelNode in _sampleObjectsGroupNode.GetAllChildren<SphereModelNode>())
        {
            if (oneSphereModelNode.Tag is not float || oneSphereModelNode.Transform is not TranslateTransform translateTransform)
                continue;

            var t = (float)oneSphereModelNode.Tag;

            t += dt;

            oneSphereModelNode.Tag = t;
            translateTransform.Y = GetAnimatedHeight(t);
        }

        if (_planarShadowMeshCreator != null && _shadowModel != null)
        {
            _planarShadowMeshCreator.UpdateGroupNode();
            UpdateShadowModel();
        }
    }

    private void UpdateLights()
    {
        var position = CalculateLightPosition();

        // Create direction from position - target position = (0,0,0)
        var lightDirection = new Vector3(-position.X, -position.Y, -position.Z);
        lightDirection = Vector3.Normalize(lightDirection);

        if (_shadowPointLight != null)
            _shadowPointLight.Position = position;

        if (_shadowDirectionalLight != null)
            _shadowDirectionalLight.Direction = lightDirection;
    }

    private Vector3 CalculateLightPosition()
    {
        float xRad = _lightHorizontalAngle * MathF.PI / 180.0f;
        float yRad = _lightVerticalAngle * MathF.PI / 180.0f;

        float x = (MathF.Sin(xRad) * MathF.Cos(yRad)) * _lightDistance;
        float y = MathF.Sin(yRad) * _lightDistance;
        float z = (MathF.Cos(xRad) * MathF.Cos(yRad)) * _lightDistance;

        return new Vector3(x, y, z);
    }

    private void SetShadowLight(bool isDirectionalLight)
    {
        if (Scene == null)
            return;

        if (isDirectionalLight)
        {
            if (_currentShadowLight == _shadowDirectionalLight)
                return;

            _currentShadowLight = _shadowDirectionalLight;
        }
        else
        {
            if (_currentShadowLight == _shadowPointLight)
                return;

            _currentShadowLight = _shadowPointLight;
        }


        if (_ambientLight != null && !Scene.Lights.Contains(_ambientLight))
            Scene.Lights.Add(_ambientLight);

        if (_currentShadowLight != null && !Scene.Lights.Contains(_currentShadowLight))
            Scene.Lights.Add(_currentShadowLight);
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateCheckBox("Show PlanarShadow", isInitiallyChecked: true, isChecked =>
        {
            if (_shadowModel != null)
                _shadowModel.Visibility = isChecked ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
        });
        
        ui.CreateCheckBox("Clip shadow to plane (?): Clipping shadow to plane is a complex operations, so if possible it is better that this is disabled.", isInitiallyChecked: true, isChecked =>
        {
            if (_planarShadowMeshCreator != null)
                _planarShadowMeshCreator.ClipToPlane = isChecked;
        });
    }
}