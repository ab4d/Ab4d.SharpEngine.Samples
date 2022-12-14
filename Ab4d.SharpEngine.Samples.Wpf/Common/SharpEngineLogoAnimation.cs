using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using static Ab4d.SharpEngine.Meshes.MeshFactory;

namespace Ab4d.SharpEngine.Samples.Wpf.Common;

public class SharpEngineLogoAnimation
{
    private IBitmapIO _bitmapIO;
    private VulkanDevice _gpuDevice;

    private MeshModelNode _hashModel;
    private StandardTransform _hashModelTransform;

    private Vector3Track _rotationTrack;
    private Vector3Track _moveTrack;
    private FloatTrack _showLogoTrack;

    private DateTime _animationStartTime;
    private double _animationDurationIsSeconds = 1.2;
    private double _lastRotateFrameNumber;
    private double _startColorAnimationFrameNumber;
    private double _lastColorFrameNumber;

    private StandardMaterial _logoTextureMaterial;
    private StandardMaterial _hashModelMaterial;
    private PlaneModelNode _logoPlaneModel;

    public string LogoImageResourceName = @"Resources\sharp-engine-logo.png";

    public float HashModelSize = 100;
    public float HashModelBarThickness = 16;
    public float HashModelBarOffset = 20;

    public MeshModelNode HashModel => _hashModel;
    public PlaneModelNode LogoPlaneModel => _logoPlaneModel;

    public event EventHandler? AnimationCompleted;

    public SharpEngineLogoAnimation(IBitmapIO bitmapIO, VulkanDevice gpuDevice)
    {
        _bitmapIO = bitmapIO;
        _gpuDevice = gpuDevice;


        // Create PlaneModelNode that will show the logo bitmap
        _logoTextureMaterial = TextureLoader.CreateTextureMaterial(LogoImageResourceName, _bitmapIO, _gpuDevice);
        _logoTextureMaterial.Alpha = 0; // hidden at start

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

        _hashModelTransform = new StandardTransform();

        _hashModelMaterial = new StandardMaterial(Color3.FromByteRgb(255, 197, 0));

        _hashModel = new Ab4d.SharpEngine.SceneNodes.MeshModelNode(hashSymbolMesh, "HashSymbolModel")
        {
            Material = _hashModelMaterial,
            Transform = _hashModelTransform
        };


        // SetupAnimation
        double endInitialRotationFrameNumber = 30;

        _lastRotateFrameNumber = 65;
        _startColorAnimationFrameNumber = 80;
        _lastColorFrameNumber = 100;


        _rotationTrack = new Vector3Track();
        _rotationTrack.Keys.Add(new Vector3KeyFrame(0, new Vector3(0, 101, 0)));
        _rotationTrack.Keys.Add(new Vector3KeyFrame(endInitialRotationFrameNumber, new Vector3(0, 65, 0)));
        _rotationTrack.Keys.Add(new Vector3KeyFrame(_lastRotateFrameNumber, new Vector3(73, 24, 55)));
        _rotationTrack.Keys.Add(new Vector3KeyFrame(_startColorAnimationFrameNumber, new Vector3(73, 24, 55)));
        _rotationTrack.Keys.Add(new Vector3KeyFrame(_lastColorFrameNumber, new Vector3(73, 24, 55)));

        _moveTrack = new Vector3Track();
        _moveTrack.Keys.Add(new Vector3KeyFrame(0, new Vector3(0, 0, 350)));
        _moveTrack.Keys.Add(new Vector3KeyFrame(endInitialRotationFrameNumber, new Vector3(0, 0, 300)));
        _moveTrack.Keys.Add(new Vector3KeyFrame(_lastRotateFrameNumber, new Vector3(-6, 31, -6)));
        _moveTrack.Keys.Add(new Vector3KeyFrame(_startColorAnimationFrameNumber, new Vector3(-6, 31, -6)));
        _moveTrack.Keys.Add(new Vector3KeyFrame(_lastColorFrameNumber, new Vector3(-6, 31, 300)));

        _showLogoTrack = new FloatTrack();
        _showLogoTrack.Keys.Add(new FloatKeyFrame(0, 0));
        _showLogoTrack.Keys.Add(new FloatKeyFrame(_startColorAnimationFrameNumber, 0));
        _showLogoTrack.Keys.Add(new FloatKeyFrame(_lastColorFrameNumber, 1));
    }

    public void StartAnimation()
    {
        _animationStartTime = DateTime.Now;
    }

    public void StopAnimation()
    {
        _animationStartTime = DateTime.MinValue;
    }

    public void GotoFirstFrame()
    {
        UpdateAnimation(0);
    }

    public void GotoLastFrame()
    {
        UpdateAnimation(_lastColorFrameNumber);
    }

    public void SetAnimationDuration(float durationInSeconds)
    {
        _animationDurationIsSeconds = durationInSeconds;
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

        scene.Lights.Add(new AmbientLight(intensity: 0.25f));          // 25% ambient light
        scene.Lights.Add(new DirectionalLight(new Vector3(0, -0.2f, -1))); // light into the screen
    }
    
    public void UpdateAnimation()
    {
        if (_animationStartTime == DateTime.MinValue)
            return;

        var elapsedSeconds = (DateTime.Now - _animationStartTime).TotalSeconds;

        double frameNumber = (100 * elapsedSeconds) / _animationDurationIsSeconds; // last frame is 100

        UpdateAnimation(frameNumber);


        if (frameNumber > _lastColorFrameNumber)
        {
            StopAnimation();
            AnimationCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    public void UpdateAnimation(double frameNumber)
    {
        var movementValue = _moveTrack.GetVectorForFrame(frameNumber);
        var rotationValue = _rotationTrack.GetVectorForFrame(frameNumber);

        _hashModelTransform.TranslateX = movementValue.X;
        _hashModelTransform.TranslateY = movementValue.Y;
        _hashModelTransform.TranslateZ = movementValue.Z;

        _hashModelTransform.RotateX = rotationValue.X;
        _hashModelTransform.RotateY = rotationValue.Y;
        _hashModelTransform.RotateZ = rotationValue.Z;


        var showLogoAmount = _showLogoTrack.GetFloatValueForFrame(frameNumber);

        _hashModelMaterial.Alpha = 1 - showLogoAmount;
        _hashModelMaterial.EmissiveColor = new Color3(showLogoAmount, showLogoAmount, showLogoAmount);

        _logoTextureMaterial.Alpha = showLogoAmount;
    }

    private void SetEasingFunction(Func<double, double> easingFunction)
    {
        _rotationTrack.EasingFunction = easingFunction;
        _moveTrack.EasingFunction = easingFunction;
        _showLogoTrack.EasingFunction = easingFunction;
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

    #region Animation (from Ab3d.PowerToys)

    // The following classes were copied from Ab3d.PowerToys.
    // They were slightly adjusted to work with float instead of double and Vector3 instead of Vector3D.
    // The classes are not yet full ported (for example frameNumber is still double)
    // The classes are not included into the SharpEngine because it will get a new animation engine.

    /// <summary>
    /// KeyFrameBase is an abstract class that is a base for all classes that define data for one key frame.
    /// </summary>
    public abstract class KeyFrameBase
    {
        /// <summary>
        /// FrameNumber as double
        /// </summary>
        public readonly double FrameNumber;

        /// <summary>
        /// EasingFunction can be specified to provide custom interpolation between key frames. In case this value is not set (is null), then linear interpolation is used.
        /// When the function is defined it gets a double parameter value between 0 and 1 and should return a value between 0 and 1. 
        /// Standard easing function are defined in <see cref="EasingFunctions"/> class.
        /// </summary>
        public Func<double, double>? EasingFunction;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="frameNumber">frame number</param>
        protected KeyFrameBase(double frameNumber)
        {
            FrameNumber = frameNumber;
        }
    }

    /// <summary>
    /// FloatKeyFrame class defines one float value for the specified FrameNumber.
    /// </summary>
    public sealed class FloatKeyFrame : KeyFrameBase
    {
        /// <summary>
        /// Value as float
        /// </summary>
        public float FloatValue;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="frameNumber">frame number</param>
        public FloatKeyFrame(double frameNumber)
            : base(frameNumber)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="frameNumber">frame number</param>
        /// <param name="floatValue">float value</param>
        public FloatKeyFrame(double frameNumber, float floatValue)
            : base(frameNumber)
        {
            FloatValue = floatValue;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "DoubleKeyFrame: FrameNumber = {0}; FloatValue = {1}",
                FrameNumber, FloatValue);
        }
    }

    /// <summary>
    /// Vector3KeyFrame class defines the values as Vector3 for the specified FrameNumber.
    /// </summary>
    public sealed class Vector3KeyFrame : KeyFrameBase
    {
        /// <summary>
        /// Vector as Vector3
        /// </summary>
        public Vector3 Vector;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="frameNumber">frame number</param>
        public Vector3KeyFrame(double frameNumber)
            : base(frameNumber)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="frameNumber">frame number</param>
        /// <param name="vector">Vector as Vector3D</param>
        public Vector3KeyFrame(double frameNumber, Vector3 vector)
            : base(frameNumber)
        {
            Vector = vector;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "Vector3DKeyFrame: FrameNumber = {0}; Vector = {1}",
                FrameNumber, Vector);
        }
    }

    /// <summary>
    /// KeyFramesTrackBase is a base class for all key frame tracks that define animation data stored in a <see cref="Keys"/> list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// KeyFramesTrackBase is a base class for all key frame tracks that define animation data stored in a <see cref="Keys"/> list.
    /// </para>
    /// <para>
    /// NOTE:<br/>
    /// All derived class need to call <see cref="InterpolateFrameNumber"/> method to adjust the frame number based on the TrackInterpolation property value
    /// before the data for the frame are calculated. 
    /// </para>
    /// </remarks>
    /// <typeparam name="T">KeyFrameBase</typeparam>
    public abstract class KeyFramesTrackBase<T> where T : KeyFrameBase
    {
        /// <summary>
        /// Gets a list of key frames.
        /// </summary>
        public List<T> Keys { get; private set; }

        /// <summary>
        /// Gets count of key frames.
        /// </summary>
        public int KeysCount { get { return Keys.Count; } }

        /// <summary>
        /// Gets first frame for this track
        /// </summary>
        public double FirstFrame
        {
            get
            {
                if (Keys == null || Keys.Count == 0)
                    return 0;

                return Keys[0].FrameNumber;
            }
        }

        /// <summary>
        /// Gets last frame for this track
        /// </summary>
        public double LastFrame
        {
            get
            {
                if (Keys == null || Keys.Count == 0)
                    return 0;

                return Keys[Keys.Count - 1].FrameNumber;
            }
        }

        /// <summary>
        /// Gets or sets a Func that gets a double and returns a double and can be specified to provide custom interpolation between first and last key frame. 
        /// This defines the speed of the animation. When null (by default), linear interpolation is used.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Gets or sets a Func that gets a double and returns a double and can be specified to provide custom interpolation between first and last key frame. 
        /// This defines the speed of the animation. When null (by default), linear interpolation is used.
        /// </para>
        /// <para>
        /// The function gets a double parameter value between 0 and 1 and should return a value between 0 and 1. 
        /// </para>
        /// <para>
        /// Standard easing function are defined in <see cref="EasingFunctions"/> class.
        /// </para>
        /// <para>
        /// If linear interpolation is used (when set to null - by default), then animation speed is constant.
        /// If Ease in is used, then animation starts slowly and then accelerates and finishes with constant speed.
        /// </para>
        /// <para>
        /// This property is different from the <see cref="KeyFrameBase.EasingFunction"/>.
        /// This property affects the animation speed of the whole Track, where the <see cref="KeyFrameBase.EasingFunction"/> changes only animation around the specified Key.
        /// </para>
        /// <para>
        /// NOTE:<br/>
        /// All derived class need to call <see cref="InterpolateFrameNumber"/> method to adjust the frame number based on the TrackInterpolation property value
        /// before the data for the frame are calculated. 
        /// </para>
        /// </remarks>
        public Func<double, double>? EasingFunction { get; set; }


        /// <summary>
        /// Constructor
        /// </summary>
        protected KeyFramesTrackBase()
        {
            Keys = new List<T>();
        }


        /// <summary>
        /// SetInterpolationToAllKeys methods sets the specified interpolation to all keys in the key frames track.
        /// </summary>
        /// <param name="easingFunction">easingFunction gets a double parameter value between 0 and 1 and should return a value between 0 and 1.</param>
        public void SetEasingFunctionToAllKeys(Func<double, double> easingFunction)
        {
            foreach (var key in Keys)
                key.EasingFunction = easingFunction;
        }

        /// <summary>
        /// InterpolateFrameNumber method interpolates the specified frame number based on the specified <see cref="EasingFunction"/>.
        /// This method must be called from all derived classes to calculate the final frame number.
        /// </summary>
        /// <param name="frameNumber"></param>
        /// <returns>interpolated frame number</returns>
        protected double InterpolateFrameNumber(double frameNumber)
        {
            if (EasingFunction == null)
                return frameNumber;

            double lastFrame = (double)LastFrame;
            double relativeFrameNumber = frameNumber / lastFrame;
            relativeFrameNumber = EasingFunction(relativeFrameNumber);

            return relativeFrameNumber * lastFrame;
        }
    }

    /// <summary>
    /// FloatTrack defines key frames that contain different values (as float). The key frames are defined in the <see cref="KeyFramesTrackBase{T}.Keys"/> list.
    /// </summary>
    public class FloatTrack : KeyFramesTrackBase<FloatKeyFrame>
    {
        /// <summary>
        /// Gets interpolated double value for the specified frame (you can also specify fractions between frames - for example targetFrame = 1.245)
        /// </summary>
        /// <param name="frameNumber">frame number as double (allow sub-frame animations)</param>
        /// <returns>double value</returns>
        public float GetFloatValueForFrame(double frameNumber)
        {
            if (Keys == null || Keys.Count == 0)
                throw new Exception("Failed to call GetDoubleValueForFrame because no keys are set for DoubleTrack");

            var lastFrame = Keys[Keys.Count - 1].FrameNumber;

            float frameFloatValue;

            if (frameNumber <= 0 || Keys.Count == 1)
            {
                frameFloatValue = Keys[0].FloatValue;
            }
            else if (frameNumber >= lastFrame)
            {
                int lastFrameIndex = Keys.Count - 1;
                frameFloatValue = Keys[lastFrameIndex].FloatValue;
            }
            else
            {
                // Apply TrackInterpolation if set
                frameNumber = base.InterpolateFrameNumber(frameNumber);

                int keyIndex;

                for (keyIndex = 1; keyIndex < Keys.Count; keyIndex++)
                {
                    if (Keys[keyIndex].FrameNumber > frameNumber)
                        break;
                }

                // interpolateLevel={0...1} - 0 = as first position, 1 = as last position, between values mean the position is between first and last
                var interpolateLevel = (frameNumber - Keys[keyIndex - 1].FrameNumber) / (Keys[keyIndex].FrameNumber - Keys[keyIndex - 1].FrameNumber);

                var easingFunction = Keys[keyIndex].EasingFunction;
                if (easingFunction != null)
                    interpolateLevel = easingFunction(interpolateLevel);

                float v1 = Keys[keyIndex - 1].FloatValue;
                float v2 = Keys[keyIndex].FloatValue;

                frameFloatValue = v1 + (v2 - v1) * (float)interpolateLevel;
            }

            return frameFloatValue;
        }
    }

    /// <summary>
    /// Vector3Track defines key frames that contain different 3D vectors (as Vector3). The key frames are defined in the <see cref="KeyFramesTrackBase{T}.Keys"/> list.
    /// </summary>
    public class Vector3Track : KeyFramesTrackBase<Vector3KeyFrame>
    {
        /// <summary>
        /// Gets interpolated Vector3 for the specified frame (you can also specify fractions between frames - for example targetFrame = 1.245)
        /// </summary>
        /// <param name="frameNumber">frame number as double (allow sub-frame animations)</param>
        /// <returns>vector as Vector3</returns>
        public Vector3 GetVectorForFrame(double frameNumber)
        {
            if (Keys == null || Keys.Count == 0)
                throw new Exception("Failed to call GetPositionForFrame because no keys are set for Vector3DTrack");

            Vector3 vector;

            var lastFrame = Keys[Keys.Count - 1].FrameNumber;

            if (frameNumber <= 0 || Keys.Count == 1)
            {
                vector = Keys[0].Vector;
            }
            else if (frameNumber >= lastFrame)
            {
                int lastFrameIndex = Keys.Count - 1;
                vector = Keys[lastFrameIndex].Vector;
            }
            else
            {
                // Apply TrackInterpolation if set
                frameNumber = base.InterpolateFrameNumber(frameNumber);

                int keyIndex;

                for (keyIndex = 1; keyIndex < Keys.Count; keyIndex++)
                {
                    if (Keys[keyIndex].FrameNumber > frameNumber)
                        break;
                }

                // interpolateLevel={0...1} - 0 = as first position, 1 = as last position, between values mean the position is between first and last
                var interpolateLevel = (frameNumber - Keys[keyIndex - 1].FrameNumber) / (Keys[keyIndex].FrameNumber - Keys[keyIndex - 1].FrameNumber);

                var easingFunction = Keys[keyIndex].EasingFunction;
                if (easingFunction != null)
                    interpolateLevel = easingFunction(interpolateLevel);

                Vector3 v1 = Keys[keyIndex - 1].Vector;
                Vector3 v2 = Keys[keyIndex].Vector;

                vector = new Vector3(v1.X + (v2.X - v1.X) * (float)interpolateLevel,
                                     v1.Y + (v2.Y - v1.Y) * (float)interpolateLevel,
                                     v1.Z + (v2.Z - v1.Z) * (float)interpolateLevel);
            }

            return vector;
        }
    }

    // Based on http://gizma.com/easing/#cub2

    /// <summary>
    /// EasingFunctions static class defines the standard easing functions that can ease a value iin range from 0 to 1.
    /// </summary>
    [Obfuscation(Feature = "control flow", Exclude = true, ApplyToMembers = true)]
    public static class EasingFunctions
    {
        /// <summary>
        /// QuadraticEaseInFunction
        /// </summary>
        /// <param name="t">input value in range from 0 to 1</param>
        /// <returns>returned eased value in range from 0 to 1</returns>
        public static double QuadraticEaseInFunction(double t)
        {
            return t * t;
        }

        /// <summary>
        /// QuadraticEaseOutFunction
        /// </summary>
        /// <param name="t">input value in range from 0 to 1</param>
        /// <returns>returned eased value in range from 0 to 1</returns>
        public static double QuadraticEaseOutFunction(double t)
        {
            return -1 * t * (t - 2);
        }

        /// <summary>
        /// QuadraticEaseInOutFunction
        /// </summary>
        /// <param name="t">input value in range from 0 to 1</param>
        /// <returns>returned eased value in range from 0 to 1</returns>
        public static double QuadraticEaseInOutFunction(double t)
        {
            t *= 2;

            if (t < 1)
                return t * t * 0.5;

            return -0.5 * ((t - 1) * (t - 3) - 1);
        }




        /// <summary>
        /// CubicEaseInFunction
        /// </summary>
        /// <param name="t">input value in range from 0 to 1</param>
        /// <returns>returned eased value in range from 0 to 1</returns>
        public static double CubicEaseInFunction(double t)
        {
            return t * t * t;
        }

        /// <summary>
        /// CubicEaseOutFunction
        /// </summary>
        /// <param name="t">input value in range from 0 to 1</param>
        /// <returns>returned eased value in range from 0 to 1</returns>
        public static double CubicEaseOutFunction(double t)
        {
            t -= 1;
            return t * t * t + 1;
        }

        /// <summary>
        /// CubicEaseInOutFunction
        /// </summary>
        /// <param name="t">input value in range from 0 to 1</param>
        /// <returns>returned eased value in range from 0 to 1</returns>
        public static double CubicEaseInOutFunction(double t)
        {
            t *= 2;

            if (t < 1)
                return t * t * t * 0.5;

            t -= 2;
            return 0.5 * (t * t * t + 2);
        }




        /// <summary>
        /// ExponentialEaseInFunction
        /// </summary>
        /// <param name="t">input value in range from 0 to 1</param>
        /// <returns>returned eased value in range from 0 to 1</returns>
        public static double ExponentialEaseInFunction(double t)
        {
            return Math.Pow(2, 10 * (t - 1));
        }

        /// <summary>
        /// ExponentialEaseOutFunction
        /// </summary>
        /// <param name="t">input value in range from 0 to 1</param>
        /// <returns>returned eased value in range from 0 to 1</returns>
        public static double ExponentialEaseOutFunction(double t)
        {
            return 1 - Math.Pow(2, -10 * t);
        }

        /// <summary>
        /// ExponentialEaseInOutFunction
        /// </summary>
        /// <param name="t">input value in range from 0 to 1</param>
        /// <returns>returned eased value in range from 0 to 1</returns>
        public static double ExponentialEaseInOutFunction(double t)
        {
            t *= 2;

            if (t < 1)
                return 0.5 * Math.Pow(2, 10 * (t - 1));

            return 1 - 0.5 * Math.Pow(2, -10 * (t - 1));
        }




        /// <summary>
        /// SinusoidalEaseInFunction
        /// </summary>
        /// <param name="t">input value in range from 0 to 1</param>
        /// <returns>returned eased value in range from 0 to 1</returns>
        public static double SinusoidalEaseInFunction(double t)
        {
            return -Math.Cos(t * Math.PI * 0.5) + 1;
        }

        /// <summary>
        /// SinusoidalEaseOutFunction
        /// </summary>
        /// <param name="t">input value in range from 0 to 1</param>
        /// <returns>returned eased value in range from 0 to 1</returns>
        public static double SinusoidalEaseOutFunction(double t)
        {
            return Math.Sin(t * Math.PI * 0.5);
        }

        /// <summary>
        /// SinusoidalEaseInOutFunction
        /// </summary>
        /// <param name="t">input value in range from 0 to 1</param>
        /// <returns>returned eased value in range from 0 to 1</returns>
        public static double SinusoidalEaseInOutFunction(double t)
        {
            return -0.5 * (Math.Cos(t * Math.PI) - 1);
        }
    }

    #endregion
}