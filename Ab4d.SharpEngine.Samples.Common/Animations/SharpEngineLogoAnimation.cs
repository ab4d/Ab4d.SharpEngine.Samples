using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using static Ab4d.SharpEngine.Meshes.MeshFactory;
using System.Numerics;
using System;
using Ab4d.SharpEngine.Animation;

namespace Ab4d.SharpEngine.Samples.Common.Animations;

public class SharpEngineLogoAnimation
{
    public float AnimationDurationIsSeconds = 1.2f;
    
    public string LogoImageResourceName = @"Resources\Textures\sharp-engine-logo.png";

    public float HashModelSize = 100;
    public float HashModelBarThickness = 16;
    public float HashModelBarOffset = 20;

    public MeshModelNode HashModel => _hashModel;
    public PlaneModelNode LogoPlaneModel => _logoPlaneModel;

    public bool IsStarted => _transformationAnimation != null && _transformationAnimation.IsInitialized;

    public event EventHandler? AnimationCompleted;

    #if DEBUG
    public TransformationAnimation? TransformationAnimation => _transformationAnimation;
    public MaterialAnimation? MaterialAnimation => _materialAnimation;
#endif

    private MeshModelNode _hashModel;
    
    private StandardMaterial _logoTextureMaterial;
    private StandardMaterial _hashModelMaterial;
    private PlaneModelNode _logoPlaneModel;

    private TransformationAnimation? _transformationAnimation;
    private MaterialAnimation? _materialAnimation;
    

    public SharpEngineLogoAnimation(Scene scene, IBitmapIO bitmapIO)
    {
        // Create PlaneModelNode that will show the logo bitmap
        _logoTextureMaterial = new StandardMaterial(LogoImageResourceName, bitmapIO);
        _logoTextureMaterial.Opacity = 0; // hidden at start

        _logoPlaneModel = new PlaneModelNode()
        {
            Position = new Vector3(0, 0, -100),
            Normal = new Vector3(0, 0, 1),
            HeightDirection = new Vector3(0, 1, 0),
            Size = new Vector2(220, 220), // ration: w : h = 0.85 : 1.00,
            Material = _logoTextureMaterial,
            BackMaterial = StandardMaterials.Black,
        };


        // Create hash symbol
        var hashSymbolMesh = CreateHashSymbolMesh(centerPosition: new Vector3(0, HashModelBarThickness * -0.5f, 0),
                                                  shapeYVector: new Vector3(0, 0, 1),
                                                  extrudeVector: new Vector3(0, HashModelBarThickness, 0),
                                                  size: HashModelSize,
                                                  barThickness: HashModelBarThickness,
                                                  barOffset: HashModelBarOffset,
                                                  name: "HashSymbolMesh");

        _hashModelMaterial = new StandardMaterial(Color3.FromByteRgb(255, 197, 0));

        _hashModel = new Ab4d.SharpEngine.SceneNodes.MeshModelNode(hashSymbolMesh, "HashSymbolModel")
        {
            Material = _hashModelMaterial,
            Transform = new StandardTransform()
        };


        // SetupAnimation
        float endInitialRotationFrameNumber = 0.30f;
        float lastRotateFrameNumber = 0.65f;
        float startColorAnimationFrameNumber = 0.80f;
        float lastColorFrameNumber = 1;


        _transformationAnimation = AnimationBuilder.CreateTransformationAnimation(scene, "SharpEngineLogo-TransformationAnimation");

        _transformationAnimation.AddTarget(_hashModel);

        _transformationAnimation.AddRotateKeyframe(0,                               new Vector3(0, 101, 0));
        _transformationAnimation.AddRotateKeyframe(endInitialRotationFrameNumber,   new Vector3(0, 65, 0));
        _transformationAnimation.AddRotateKeyframe(lastRotateFrameNumber,          new Vector3(73, 24, 55));
        _transformationAnimation.AddRotateKeyframe(startColorAnimationFrameNumber, new Vector3(73, 24, 55));
        _transformationAnimation.AddRotateKeyframe(lastColorFrameNumber,           new Vector3(73, 24, 55));

        _transformationAnimation.AddTranslateKeyframe(0,                               new Vector3(0, 0, 350));
        _transformationAnimation.AddTranslateKeyframe(endInitialRotationFrameNumber,   new Vector3(0, 0, 300));
        _transformationAnimation.AddTranslateKeyframe(lastRotateFrameNumber,          new Vector3(-6, 31, -6));
        _transformationAnimation.AddTranslateKeyframe(startColorAnimationFrameNumber, new Vector3(-6, 31, -6));
        _transformationAnimation.AddTranslateKeyframe(lastColorFrameNumber,           new Vector3(-6, 31, 300));

        _transformationAnimation.Completed += (_, _) => AnimationCompleted?.Invoke(this, EventArgs.Empty);

        _transformationAnimation.SetDuration(AnimationDurationIsSeconds * 1000); // Duration of this animation is only 65% (_lastRotateFrameNumber) of the whole animation length


        _materialAnimation = AnimationBuilder.CreateMaterialsAnimation(scene, "SharpEngineLogo-MaterialAnimation");
        _materialAnimation.AddTarget(_hashModelMaterial);

        _materialAnimation.SetOpacity(0, duration: lastColorFrameNumber - startColorAnimationFrameNumber, delay: startColorAnimationFrameNumber);

        _materialAnimation.SetDuration(AnimationDurationIsSeconds * 1000);
    }

    public void StartAnimation()
    {
        _transformationAnimation?.Start();
        _materialAnimation?.Start();
    }

    public void StopAnimation()
    {
        _transformationAnimation?.Stop();
        _materialAnimation?.Stop();
    }

    public void GotoFirstFrame()
    {
        _transformationAnimation?.Seek(0);
        _materialAnimation?.Seek(0);
    }

    public void GotoLastFrame()
    {
        _transformationAnimation?.Seek(_transformationAnimation.Duration);
        _materialAnimation?.Seek(_materialAnimation.Duration);
    }

    public void SetAnimationDuration(float durationInSeconds)
    {
        _transformationAnimation?.SetDuration(durationInSeconds * 1000);
        _materialAnimation?.SetDuration(durationInSeconds * 1000);
    }

    public void SetHashModelVisibility(bool isVisible)
    {
        _hashModel.Visibility = isVisible ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
    }

    public void SetLogoPlaneVisibility(bool isVisible)
    {
        _logoPlaneModel.Visibility = isVisible ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
    }

    public void ResetCamera(TargetPositionCamera targetPositionCamera)
    {
        targetPositionCamera.TargetPosition = new Vector3(0, 0, 0);
        targetPositionCamera.Heading = 0;
        targetPositionCamera.Attitude = 0;
        targetPositionCamera.Distance = 400;
    }

    public void SetupLights(Scene scene)
    {
        scene.Lights.Clear();

        scene.Lights.Add(new AmbientLight(intensity: 0.25f));              // 25% ambient light
        scene.Lights.Add(new DirectionalLight(new Vector3(0, -0.2f, -1))); // light into the screen
    }

    public void UpdateAnimation()
    {
        _transformationAnimation?.Update();
        _materialAnimation?.Update();

        // Alpha of the background logo image is inverted alpha of the 3D hash model
        var showLogoModelAmount = _hashModelMaterial.Opacity;
        var showLogoImageAmount = 1 - showLogoModelAmount;

        _logoTextureMaterial.Opacity = showLogoImageAmount;
        
        // When increasing transparency of the logo model, we also increase the EmissiveColor amount to prevent color bleeding
        _hashModelMaterial.EmissiveColor = new Color3(showLogoImageAmount, showLogoImageAmount, showLogoImageAmount);
    }

    // Centered around (0,0)
    private static Vector2[][] CreateHashSymbolPolygons(float size,
                                                        float barThickness,
                                                        float barOffset)
    {
        var outerPoints = new Vector2[28];

        float barInnerDistance = size - 2 * (barOffset + barThickness);

        // Start to the right and down:
        Vector2 xDirection = new Vector2(1, 0);
        Vector2 yDirection = new Vector2(0, 1);

        // start positions
        Vector2 position = new Vector2(barOffset - size * 0.5f, size * -0.5f);

        int i = 0; // index in outerPoints

        outerPoints[i++] = position;

        // create positions for outer polygon in 4 steps
        for (int segment = 0; segment < 4; segment++)
        {
            position += xDirection * barThickness;
            outerPoints[i++] = position;

            position += yDirection * barOffset;
            outerPoints[i++] = position;

            position += xDirection * barInnerDistance;
            outerPoints[i++] = position;

            position -= yDirection * barOffset;
            outerPoints[i++] = position;

            position += xDirection * barThickness;
            outerPoints[i++] = position;

            position += yDirection * barOffset;
            outerPoints[i++] = position;

            if (i < 28) // skip last position - it is connected to the first position
            {
                position += xDirection * barOffset;
                outerPoints[i++] = position;
            }

            // Rotate by 90 degrees:
            xDirection = yDirection;
            yDirection = new Vector2(-yDirection.Y, yDirection.X);
        }

        // add points for inner hole - those points need to be oriented in the opposite (counter-clockwise) direction
        var innerPoints = new Vector2[4];

        float innerPolygonStart = barOffset + barThickness - size * 0.5f;
        innerPoints[0] = new Vector2(innerPolygonStart, innerPolygonStart);
        innerPoints[1] = new Vector2(innerPolygonStart, innerPolygonStart + barInnerDistance);
        innerPoints[2] = new Vector2(innerPolygonStart + barInnerDistance, innerPolygonStart + barInnerDistance);
        innerPoints[3] = new Vector2(innerPolygonStart + barInnerDistance, innerPolygonStart);

        return new Vector2[][] { outerPoints, innerPoints };
    }

    /// <summary>
    /// CreateHashSymbolMesh creates a mesh for hash symbol from the specified parameters.
    /// </summary>
    /// <param name="centerPosition">center position as Vector3</param>
    /// <param name="shapeYVector">Y axis vector of the shape as Vector3</param>
    /// <param name="extrudeVector">extrude vector as Vector3</param>
    /// <param name="size">size of the hash symbol</param>
    /// <param name="barThickness">thickness of the horizontal and vertical bar</param>
    /// <param name="barOffset">offset of the horizontal and vertical bar</param>
    /// <param name="name">optional name for the mesh</param>
    /// <returns>StandardMesh with filled vertices array and index array</returns>        
    public static StandardMesh CreateHashSymbolMesh(Vector3 centerPosition,
                                                    Vector3 shapeYVector,
                                                    Vector3 extrudeVector,
                                                    float size,
                                                    float barThickness,
                                                    float barOffset,
                                                    string? name = null)
    {
        var hashSymbolPolygons = CreateHashSymbolPolygons(size, barThickness, barOffset);

        var triangulator = new Triangulator(hashSymbolPolygons, isYAxisUp: false);
        triangulator.Triangulate(out var positions, out var triangulatedIndices);

        var triangleIndices = triangulatedIndices.ToArray();

        MeshUtils.InverseTriangleIndicesOrientation(triangleIndices);

#if DEBUG
        if (name == null)
            name = "HashSymbolMesh";
#endif

        var extrudedMesh = MeshFactory.CreateExtrudedMesh(positions.ToArray(),
                                                          triangleIndices,
                                                          isSmooth: false,
                                                          flipNormals: false,
                                                          modelOffset: centerPosition,
                                                          extrudeVector: extrudeVector,
                                                          shapeYVector: shapeYVector,
                                                          textureCoordinatesGenerationType: ExtrudeTextureCoordinatesGenerationType.Cylindrical,
                                                          closeBottom: true,
                                                          closeTop: true,
                                                          name: name);
        return extrudedMesh;
    }
}