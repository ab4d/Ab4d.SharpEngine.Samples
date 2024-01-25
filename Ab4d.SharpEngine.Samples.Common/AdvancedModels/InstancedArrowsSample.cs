using System.Diagnostics;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class InstancedArrowsSample : CommonSample
{
    public override string Title => "Animated instanced arrows";
    public override string Subtitle => "Arrows are pointing towards an animated sphere and their color is changed based on the distance to the sphere.\nThis sample is using object instancing to achieve super fast rendering and animation.";
    
    private bool _isAnimatingArrows = true;
    private bool _useOptimizedCode = true;
    private int _selectedArrowsCount = 100;

    private WorldColorInstanceData[]? _instanceData;
    private InstancedMeshNode? _instancedMeshNode;

    private int _xCount;
    private int _yCount;
    private float _xSize;
    private float _ySize;
    private float _arrowsLength;

    private float _minDistance;
    private float _maxDistance;

    private Vector3 _sphereStartPosition;
    private Vector3 _spherePosition;

    private Color4[]? _gradientColors;

    private DateTime _startTime;
    private TranslateTransform? _sphereTranslate;

    private int _cameraIndex;

    private int _lastSecond;
    
    private Stopwatch _stopwatch = new Stopwatch();

    private double _totalUpdateDataTime;
    private int _updateDataSamplesCount;

    private double _totalRenderingTime;
    private int _renderingTimeSamplesCount;

    private SceneView? _subscribedSceneView;

    private string? _totalPositionText;
    private string? _performanceText;
    private StandardMesh? _arrowMesh;
    private ICommonSampleUIElement? _totalPositionLabel;
    private ICommonSampleUIElement? _performanceLabel;

    public InstancedArrowsSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        _xSize = 2000;
        _ySize = 2000;

        _arrowsLength = 30;

        _sphereStartPosition = new Vector3(0, 200, 0);


        // Min and max distance will be used to get the color from the current arrow distance
        _minDistance = _sphereStartPosition.Y;

        float dx = MathF.Abs(_sphereStartPosition.X) + (_xSize / 2f);
        float dz = MathF.Abs(_sphereStartPosition.Z) + (_ySize / 2f);

        _maxDistance = MathF.Sqrt(dx * dx + _sphereStartPosition.Y * _sphereStartPosition.Y + dz * dz);


        _gradientColors = CreateDistanceColors();

        var sphereModelNode = new SphereModelNode()
        {
            CenterPosition = new Vector3(0, 0, 0),
            Radius         = 10,
            Material       = StandardMaterials.Gold
        };

        _sphereTranslate = new TranslateTransform(_sphereStartPosition);
        sphereModelNode.Transform = _sphereTranslate;

        scene.RootNode.Add(sphereModelNode);


        
        _arrowMesh = MeshFactory.CreateArrowMesh(startPosition: new Vector3(0, 0, 0), 
                                                 endPosition: new Vector3(1, 0, 0), 
                                                 radius: 1.0f / 15.0f, 
                                                 arrowRadius: 2.0f / 15.0f, 
                                                 arrowAngle: 45f, 
                                                 maxArrowLength: 0.5f, 
                                                 segments: 10, 
                                                 generateTextureCoordinates: false, 
                                                 name: "ArrowMesh");

        _instancedMeshNode = new InstancedMeshNode(_arrowMesh)
        {
            IsUpdatingBounds = false // Prevent updating BoundingBox
        };

        UpdateInstanceData();


        scene.RootNode.Add(_instancedMeshNode);

        if (targetPositionCamera != null)
        {
            targetPositionCamera.TargetPosition = new Vector3(0, _sphereStartPosition.Y * 0.3f, 0); // target y = 1/3 of the sphere start height
            targetPositionCamera.Heading = 30;
            targetPositionCamera.Attitude = -20;
            targetPositionCamera.Distance = 2500;
        }

        _startTime = DateTime.Now;
    }

    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        sceneView.IsCollectingStatistics = true;

        sceneView.SceneUpdating += OnSceneViewOnSceneUpdating;
        sceneView.SceneRendered += SceneViewOnSceneRendered;
        _subscribedSceneView = sceneView;
    }

    private void SceneViewOnSceneRendered(object? sender, EventArgs e)
    {
        if (SceneView != null && SceneView.Statistics != null)
        {
            _totalRenderingTime += SceneView.Statistics.TotalRenderTimeMs;
            _renderingTimeSamplesCount++;
        }
    }

    private void OnSceneViewOnSceneUpdating(object? sender, EventArgs args)
    {
        UpdateAnimatedArrows();
    }

    private void UpdateAnimatedArrows()
    {
        float elapsedSeconds = (float)(DateTime.Now - _startTime).TotalSeconds;

        // Update statistics only once per second
        if (DateTime.Now.Second != _lastSecond)
        {
            double averageUpdateTime = _updateDataSamplesCount > 0 ? _totalUpdateDataTime / (double) _updateDataSamplesCount : 0;
            double averageRenderTime = _renderingTimeSamplesCount > 0 ? _totalRenderingTime / (double)_renderingTimeSamplesCount : 0;

            _performanceText = $"Performance:\nUpdate InstanceData: {averageUpdateTime:#,##0.00} ms\nRender time: {averageRenderTime:#,##0.00} ms";

            _performanceLabel?.UpdateValue();

            _totalUpdateDataTime = 0;
            _updateDataSamplesCount = 0;
            _totalRenderingTime = 0;
            _renderingTimeSamplesCount = 0;

            _lastSecond = DateTime.Now.Second;
        }

        if (!_isAnimatingArrows)
            return;

        float x = _sphereStartPosition.X;
        float y = _sphereStartPosition.Y;
        float z = _sphereStartPosition.Z;

        // Rotate on xz plane
        x += MathF.Sin(elapsedSeconds * 3f) * _xSize * 0.1f;
        z += MathF.Cos(elapsedSeconds * 3f) * _ySize * 0.1f;

        // Rotate on xy plane
        x += MathF.Sin(elapsedSeconds) * _xSize * 0.2f;
        y += MathF.Cos(elapsedSeconds) * 90f;
        
        // Rotate on yz plane
        y += MathF.Sin(elapsedSeconds * 5f) * 50f;
        z += MathF.Cos(elapsedSeconds * 0.3f) * _ySize * 0.2f;

        
        if (_sphereTranslate != null)
            _sphereTranslate.SetTranslate(x, y, z);

        _spherePosition = new Vector3(x, y, z);

        // After we have a new sphere position, we can update the instances data
        //if (OptimizedCheckBox.IsChecked ?? false)
            UpdateInstanceData();
        //else
        //    UpdateInstanceData_Unoptimized();

        //_instancedMeshNode?.UpdateInstancesData(updateBoundingBox: false);
    }

    private void UpdateInstanceData()
    {
        if (_selectedArrowsCount == 0 || _gradientColors == null)
            return;

        _xCount = _selectedArrowsCount;
        _yCount = _selectedArrowsCount;

        if (_instanceData != null && _instanceData.Length != _xCount * _yCount)
            _instanceData = null;

        bool isNewInstanceData;

        if (_instanceData == null)
        {
            _instanceData = new WorldColorInstanceData[_xCount * _yCount];
            isNewInstanceData = true;
        }
        else
        {
            isNewInstanceData = false;
        }

        _stopwatch.Restart();


        if (_useOptimizedCode)
            UpdateInstanceData_Optimized();
        else
            UpdateInstanceData_Unoptimized();

        
        if (isNewInstanceData)
        {
            // Set instance data and also provide the bounding box.
            // It is recommended to specify the BoundingBox of the instances manually.
            // Otherwise, the actual bounding box will be measured by transforming the mesh's BoundingBox by each instance transform
            // and this can take a long time.
            // Here we provide a slightly bigger bounding box so that it will fit for all possible transformations in this sample.

            var boundingBox = new BoundingBox(minimum: new Vector3(-_xSize - _arrowsLength, -_arrowsLength * 0.1f, -_ySize - _arrowsLength), 
                                              maximum: new Vector3(_xSize + _arrowsLength, _arrowsLength, _ySize + _arrowsLength));

            _instancedMeshNode?.SetInstancesData(_instanceData, boundingBox);
        }
        else
        {
            // When we only updated the instances data, then we need to call UpdateInstancesData
            // Do not update bounding box because we have already provided a bounding box that fits for all transformations.
            _instancedMeshNode?.UpdateInstancesData(updateBoundingBox: false);
        }

        _stopwatch.Stop();
        _totalUpdateDataTime += _stopwatch.Elapsed.TotalMilliseconds;
        _updateDataSamplesCount++;

        if (isNewInstanceData && _arrowMesh != null)
        {
            int trianglesCount = (int)(_xCount * _yCount * _arrowMesh.IndexCount / 3);
            _totalPositionText = $"Total triangles: {trianglesCount:#,###}";
            _totalPositionLabel?.UpdateValue();
        }
    }

    private void UpdateInstanceData_Optimized()
    {
        var instancedData = _instanceData;

        if (instancedData == null || _xCount == 0 || _yCount == 0 || _gradientColors == null)
            return;
        
        float xStep = _xSize / _xCount;
        float yStep = _ySize / _yCount;

        
        // The following is the initial (unoptimized) code to update the matrices:
        
        var spherePosition = _spherePosition;
        var arrowsLength = _arrowsLength;
        var gradientColors = _gradientColors;

        //for (int xi = 0; xi < _xCount; xi++)
        Parallel.For((int)0, _xCount, xi =>
        {
            float x = xi * xStep -(_xSize / 2);
            float y = -(_ySize / 2);

            int instanceIndex = xi * _yCount;

            for (int yi = 0; yi < _yCount; yi++)
            {
                //var arrowDirection = GetArrowDirection(x, y);
                //var arrowDirection = new Vector3(spherePosition.X - x, spherePosition.Y, spherePosition.Z - y);
                //arrowDirection.Normalize();

                float dx = spherePosition.X - x;
                float dy = spherePosition.Y;
                float dz = spherePosition.Z - y;

                float sphereDistance = MathF.Sqrt(dx * dx + dy * dy + dz * dz);

                if (sphereDistance > 1E-10f)
                {
                    float denum = 1.0f / sphereDistance;
                    dx *= denum;
                    dy *= denum;
                    dz *= denum;
                }


                //var arrowStartPosition = new Vector3(x, 0, y);

                //instancedData[instanceIndex].World = GetMatrixFromDirection(arrowDirection, arrowStartPosition, arrowScale);

                //var xAxis = normalizedDirectionVector;
                //var yAxis = CalculateUpDirection(arrowDirection);

                //var horizontalVector = SharpDX.Vector3.Cross(SharpDX.Vector3.Up, arrowDirection);

                //var horizontalVector = new Vector3(
                //    1 * arrowDirection.Z - 0 * arrowDirection.Y,
                //    0 * arrowDirection.X - 0 * arrowDirection.Z,
                //    0 * arrowDirection.Y - 1 * arrowDirection.X);

                //var horizontalVector = new Vector3(
                //    1 * dz,
                //    0,
                //    -1 * dx);

                float hx = dz;
                //float hy = 0;
                float hz = -dx;

                float length = MathF.Sqrt(hx * hx + hz * hz);

                if (length > 0.00000001f)
                {
                    float denum = 1.0f / length;
                    hx *= denum;
                    hz *= denum;
                }
                else
                {
                    hx = 0;
                    //hy = 0;
                    hz = 1;
                }


                // First we need to check for edge case - the look direction is in the UpVector direction - the length of horizontalVector is 0 (or almost zero)

                //if (horizontalVector.LengthSquared() < 0.0001) // we can use LengthSquared to avoid costly sqrt
                //    horizontalVector = SharpDX.Vector3.UnitZ; // Any vector on xz plane could be used

                //yAxis = SharpDX.Vector3.Cross(arrowDirection, horizontalVector);

                //yAxis = new Vector3(
                //    arrowDirection.Y * horizontalVector.Z - arrowDirection.Z * horizontalVector.Y, 
                //    arrowDirection.Z * horizontalVector.X - arrowDirection.X * horizontalVector.Z,
                //    arrowDirection.X * horizontalVector.Y - arrowDirection.Y * horizontalVector.X);

                // horizontalVector.Y is always 0
                var yAxis = new Vector3(dy * hz, 
                                        dz * hx - dx * hz,
                                        -dy * hx);

                //var zAxis = SharpDX.Vector3.Cross(arrowDirection, yAxis);

                var zAxis = new Vector3(dy * yAxis.Z - dz * yAxis.Y,
                                        dz * yAxis.X - dx * yAxis.Z,
                                        dx * yAxis.Y - dy * yAxis.X);


                // For more info see comments in GetRotationMatrixFromDirection
                // NOTE: The following math works only for uniform scale (scale factor for x, y and z is the same - arrowsLength in our case)
                instancedData[instanceIndex].World = new Matrix4x4(dx * arrowsLength,               dy * arrowsLength,               dz * arrowsLength,      0,
                                                                   yAxis.X * arrowsLength,          yAxis.Y * arrowsLength,          yAxis.Z * arrowsLength, 0,
                                                                   zAxis.X * arrowsLength,          zAxis.Y * arrowsLength,          zAxis.Z * arrowsLength, 0,
                                                                   x,                               0,                               y,                      1);



                //double distance = GetDistance(x, y);
                //float dx = spherePosition.X - x;
                //float dz = spherePosition.Z - y;
                //float distance = (float)Math.Sqrt(dx * dx + spherePosition.Y * spherePosition.Y + dz * dz);

                //instancedData[instanceIndex].DiffuseColor = GetColorForDistance(distance);

                int materialIndex;

                if (sphereDistance <= _minDistance)
                    materialIndex = 0;
                else if (sphereDistance >= _maxDistance)
                    materialIndex = gradientColors.Length - 1;
                else
                    materialIndex = (int)((sphereDistance - _minDistance) * (gradientColors.Length - 1) / (_maxDistance - _minDistance));

                instancedData[instanceIndex].DiffuseColor = gradientColors[materialIndex];


                y += yStep;
                instanceIndex++;
            }
        } );
    }

    #region Unoptimized version
    private void UpdateInstanceData_Unoptimized()
    {
        var instancedData = _instanceData;

        if (instancedData == null || _xCount == 0 || _yCount == 0)
            return;

        float xStep = _xSize / _xCount;
        float yStep = _ySize / _yCount;


        // The following is the initial (unoptimized) code to update the matrices:
        
        var arrowScale = new Vector3(_arrowsLength, _arrowsLength, _arrowsLength);

        float x = -(_xSize / 2);
        for (int xi = 0; xi < _xCount; xi++)
        {
            float y = -(_ySize / 2);

            int instanceIndex = xi * _yCount;

            for (int yi = 0; yi < _yCount; yi++)
            {
                var arrowDirection = GetArrowDirection(x, y);
                var arrowStartPosition = new Vector3(x, 0, y);

                instancedData[instanceIndex].World = GetMatrixFromDirection(arrowDirection, arrowStartPosition, arrowScale);

                double distance = GetDistance(x, y);
                instancedData[instanceIndex].DiffuseColor = GetColorForDistance(distance);

                y += yStep;
                instanceIndex++;
            }

            x += xStep;
        }
    }

    private static Matrix4x4 GetMatrixFromDirection(Vector3 normalizedDirectionVector, Vector3 position, Vector3 scale)
    {
        GetMatrixFromDirection(normalizedDirectionVector, position, scale, out var orientationMatrix);
        return orientationMatrix;
    }

    private static void GetMatrixFromDirection(Vector3 normalizedDirectionVector, Vector3 position, Vector3 scale, out Matrix4x4 orientationMatrix)
    {
        //var xAxis = normalizedDirectionVector;
        var yAxis = CalculateUpDirection(normalizedDirectionVector);
        var zAxis = Vector3.Cross(normalizedDirectionVector, yAxis);

        orientationMatrix = new Matrix4x4(normalizedDirectionVector.X * scale.X, normalizedDirectionVector.Y * scale.Y, normalizedDirectionVector.Z * scale.Z, 0,
                                          yAxis.X * scale.X, yAxis.Y * scale.Y, yAxis.Z * scale.Z, 0,
                                          zAxis.X * scale.X, zAxis.Y * scale.Y, zAxis.Z * scale.Z, 0,
                                          position.X, position.Y, position.Z, 1);
    }

    private static Vector3 CalculateUpDirection(Vector3 lookDirection)
    {
        // To get the up direction we need to find a vector that lies on the xz plane (horizontal plane) and is perpendicular to Up vector and lookDirection.
        // Than we just create a perpendicular vector to lookDirection and the found vector on xz plane.

        var horizontalVector = Vector3.Cross(new Vector3(0, 1, 0), lookDirection);

        // First we need to check for edge case - the look direction is in the UpVector direction - the length of horizontalVector is 0 (or almost zero)

        if (horizontalVector.LengthSquared() < 0.0001) // we can use LengthSquared to avoid costly sqrt
            return Vector3.UnitZ;              // Any vector on xz plane could be used


        var upDirection = Vector3.Cross(lookDirection, horizontalVector);
        upDirection = Vector3.Normalize(upDirection);

        return upDirection;
    }

    private Vector3 GetArrowDirection(float x, float y)
    {
        var direction = new Vector3(_spherePosition.X - x, _spherePosition.Y, _spherePosition.Z - y);
        direction = Vector3.Normalize(direction);

        return direction;
    }

    private double GetDistance(double x, double y)
    {
        double dx = _spherePosition.X - x;
        double dz = _spherePosition.Z - y;

        double distance = Math.Sqrt(dx * dx + _spherePosition.Y * _spherePosition.Y + dz * dz);

        return distance;
    }

    private Color4 GetColorForDistance(double distance)
    {
        if (_gradientColors == null)
            _gradientColors = CreateDistanceColors();

        int materialIndex;

        if (distance <= _minDistance)
            materialIndex = 0;
        else if (distance >= _maxDistance)
            materialIndex = _gradientColors.Length - 1;
        else
            materialIndex = Convert.ToInt32((distance - _minDistance) * (_gradientColors.Length - 1) / (_maxDistance - _minDistance));

        return _gradientColors[materialIndex];
    }
    #endregion

    private Color4[] CreateDistanceColors()
    {
        // Here we prepare list of materials that will be used to for arrows on different distances from the gold sphere

        var gradient = new GradientStop[]
        {
            new GradientStop(Colors.Red, 0.0f),
            new GradientStop(Colors.Orange, 0.2f),
            new GradientStop(Colors.Yellow, 0.4f),
            new GradientStop(Colors.Green, 0.6f),
            new GradientStop(Colors.DarkBlue, 0.8f),
            new GradientStop(Colors.DodgerBlue, 1.0f),
        };
        
        var gradientColorsCount = 128;

        var gradientColors = new Color4[gradientColorsCount];

        for (int i = 0; i < gradientColorsCount; i++)
            gradientColors[i] = TextureFactory.GetGradientColor(((float)i / ((float)gradientColorsCount - 1)), gradient);

        return gradientColors;
    }
    
    private void SelectedArrowNumberChanged(string? selectedText)
    {
        if (selectedText == null || _arrowMesh == null)
            return;

        var parts = selectedText.Split(' ');
        _selectedArrowsCount = int.Parse(parts[0]);

        UpdateInstanceData();
    }
    
    private void ChangeCamera()
    {
        if (targetPositionCamera == null)
            return;

        _cameraIndex++;

        switch (_cameraIndex)
        {
            case 1:
                targetPositionCamera.Heading = 47f;
                targetPositionCamera.Attitude = -8.6f;
                targetPositionCamera.Distance = 1200;
                targetPositionCamera.TargetPosition = new Vector3(-46, -227 + 60, 66);
                break;

            case 2:
                targetPositionCamera.Heading = -1.4f;
                targetPositionCamera.Attitude = -4f;
                targetPositionCamera.Distance = 1776;
                targetPositionCamera.TargetPosition = new Vector3(-16, -109 + 60, 37);
                break;

            case 3:
                targetPositionCamera.Heading = 0f;
                targetPositionCamera.Attitude = -31f;
                targetPositionCamera.Distance = 1325;
                targetPositionCamera.TargetPosition = new Vector3(10, -134 + 60, -130);
                break;

            case 4:
                targetPositionCamera.Heading        = -0.57f;
                targetPositionCamera.Attitude       = -89f;
                targetPositionCamera.Distance       = 4275;
                targetPositionCamera.TargetPosition = new Vector3(-16, -109 + 60, 37);
                break;

            default:
                targetPositionCamera.Heading = 30;
                targetPositionCamera.Attitude = -20;
                targetPositionCamera.Distance = 2500;
                targetPositionCamera.TargetPosition = new Vector3(0, 60, 0);

                _cameraIndex = 0;
                break;
        }
    }
    
    protected override void OnDisposed()
    {
        if (_subscribedSceneView != null)
        {
            _subscribedSceneView.SceneUpdating -= OnSceneViewOnSceneUpdating;
            _subscribedSceneView.SceneRendered -= SceneViewOnSceneRendered;
            _subscribedSceneView = null;
        }
        
        base.OnDisposed();
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateCheckBox("Animate arrows", _isAnimatingArrows, isChecked => _isAnimatingArrows = isChecked);
        ui.CreateCheckBox("Use optimized code", _useOptimizedCode, isChecked => _useOptimizedCode = isChecked);

        ui.AddSeparator();

        ui.CreateLabel("Number of arrows:");
        var arrowNumberOptions = new string[]
        {
            "10 x 10 (100)",
            "100 x 100 (10.000)",
            "300 x 300 (90.000)",
            "1000 x 1000 (1.000.000)",
        };
        ui.CreateComboBox(arrowNumberOptions, (selectedIndex, selectedText) => SelectedArrowNumberChanged(selectedText), selectedItemIndex: 1);

        ui.AddSeparator();

        _totalPositionLabel = ui.CreateKeyValueLabel(null, () => _totalPositionText ?? "");

        ui.AddSeparator();

        _performanceLabel = ui.CreateKeyValueLabel(null, () => _performanceText ?? "");

        ui.AddSeparator();

        ui.CreateButton("Change camera", () => ChangeCamera(), width: 200);
    }
}