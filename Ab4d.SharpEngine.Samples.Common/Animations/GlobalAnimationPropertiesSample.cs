using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using System;
using System.Numerics;
using Ab4d.SharpEngine.Animation;

namespace Ab4d.SharpEngine.Samples.Common.Animations;

public class GlobalAnimationPropertiesSample : CommonSample
{
    public override string Title => "Global Animation properties";
    public override string? Subtitle => null;

    private ICommonSampleUIProvider? _uiProvider;

    private TargetPositionCamera? _targetPositionCamera;

    private GroupNode? _boxesGroupNode;
    private TransformationAnimation? _animation;

    private int _usedColumnsCount, _usedRowsCount;

    private PlanarShadowMeshCreator? _planarShadowMeshCreator;
    private PlaneModelNode? _planeModelNode;
    private MeshModelNode? _shadowModel;
    private DirectionalLight? _shadowDirectionalLight;

    private enum SampleDelayTypes
    {
        None = 0,
        Fixed1000ms,
        ByAbsoluteNodeIndex,
        ByRelativeNodeIndex,
        BySinNodeIndex,
        StaggeringFromCenter,
        GridStaggerFromFirst,
        GridStaggerFromCenter,
        GridStaggerReverseXAxis
    }

    private enum SampleDurationTypes
    {
        Undefined = 0,
        Fixed1000ms,
        Fixed2000ms,
        StaggeringFromCenter,
    }

    private int _selectedRowsCount = 5;
    private int _selectedColumnCount = 5;

    private SampleDelayTypes _currentDelay = SampleDelayTypes.GridStaggerFromCenter;
    private SampleDurationTypes _currentDuration = SampleDurationTypes.Undefined;
    private float _currentEndDelay = 0;
    private AnimationDirections _currentAnimationDirection = AnimationDirections.Alternate;



    public GlobalAnimationPropertiesSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        _planeModelNode = new PlaneModelNode("BasePlane")
        {
            Position = new Vector3(0, -0.5f, 0),
            PositionType = PositionTypes.Center,
            Normal = new Vector3(0, 1, 0),
            HeightDirection = new Vector3(0, 0, 1),
            Size = new Vector2(600, 400),
            Material = StandardMaterials.Gray,
            BackMaterial = StandardMaterials.Black
        };

        scene.RootNode.Add(_planeModelNode);


        _boxesGroupNode = new GroupNode("AnimatedBoxesGroup");
        scene.RootNode.Add(_boxesGroupNode);


        UpdateSceneObjectsIfNeeded();

        _animation?.Start();
    }

    protected override Camera OnCreateCamera()
    {
        _targetPositionCamera = new TargetPositionCamera()
        {
            Heading = -15,
            Attitude = -30,
            Distance = 850,
            TargetPosition = new Vector3(0, 40, 0),
            ShowCameraLight = ShowCameraLightType.Never
        };

        return _targetPositionCamera;
    }

    protected override void OnCreateLights(Scene scene)
    {
        // Add ambient light and one directions lights that will be also used for planar shadow
        scene.SetAmbientLight(intensity: 0.3f);

        _shadowDirectionalLight = new DirectionalLight(new Vector3(-0.4f, -1, -0.2f));
        scene.Lights.Add(_shadowDirectionalLight);

        SetupPlanarShadow();

        //base.OnCreateLights(scene);
    }

    private void UpdateSceneObjectsIfNeeded()
    {
        if (_usedColumnsCount != _selectedColumnCount || _usedRowsCount != _selectedRowsCount)
        {
            CreateAnimatedObjects(_selectedColumnCount, _selectedRowsCount);
            CreateOrUpdateAnimation();

            if (_planarShadowMeshCreator == null)
                SetupPlanarShadow();
            else
                UpdatePlanarShadow();
        }
    }

    private void CreateAnimatedObjects(int columnsCount, int rowsCount)
    {
        if (_boxesGroupNode == null)
            return;

        _boxesGroupNode.Clear();

        for (int y = 0; y < rowsCount; y++)
        {
            for (int x = 0; x < columnsCount; x++)
            {
                var boxModelNode = new BoxModelNode(name: $"AnimatedBox_{x}_{y}")
                {
                    Position = new Vector3(x * 50 - 200, 0, y * 50 - 100),
                    PositionType = PositionTypes.Bottom | PositionTypes.Center,
                    Size = new Vector3(30, 30, 30),
                    Material = StandardMaterials.Orange
                };

                _boxesGroupNode.Add(boxModelNode);
            }
        }

        _usedColumnsCount = columnsCount;
        _usedRowsCount = rowsCount;
    }

    private void SetupPlanarShadow()
    {
        if (_boxesGroupNode == null || _planeModelNode == null || _shadowDirectionalLight == null || Scene == null)
            return;

        // Create PlanarShadowMeshCreator
        _planarShadowMeshCreator = new PlanarShadowMeshCreator(_boxesGroupNode);
        _planarShadowMeshCreator.SetPlane(_planeModelNode.GetCenterPosition(), _planeModelNode.Normal, _planeModelNode.HeightDirection, _planeModelNode.Size);
        _planarShadowMeshCreator.ClipToPlane = false; // No need to clip shadow to plane because plane is big enough (when having smaller plane, turn this on - this creates a lot of additional objects on GC)

        _planarShadowMeshCreator.ApplyDirectionalLight(_shadowDirectionalLight.Direction);

        if (_planarShadowMeshCreator.ShadowMesh != null)
        {
            _shadowModel = new MeshModelNode(_planarShadowMeshCreator.ShadowMesh, StandardMaterials.DimGray, "PlanarShadowModel");
            _shadowModel.Transform = new Ab4d.SharpEngine.Transformations.TranslateTransform(0, 0.05f, 0); // Lift the shadow 3D model slightly above the ground

            Scene.RootNode.Add(_shadowModel);
        }
    }

    private void UpdatePlanarShadow()
    {
        if (_planarShadowMeshCreator != null && _shadowModel != null && _shadowDirectionalLight != null)
        {
            _planarShadowMeshCreator.UpdateGroupNode();
            _planarShadowMeshCreator.ApplyDirectionalLight(_shadowDirectionalLight.Direction);

            _shadowModel.Mesh = _planarShadowMeshCreator.ShadowMesh;
        }
    }

    private void UpdateAnimation()
    {
        if (_animation == null || _boxesGroupNode == null)
            return;

        bool isAnimationStarted = _animation.IsRunning;

        // When the animation is running, we cannot change the animation settings. So we need to stop the animation first.
        _animation.Stop();

        // Before starting a new and changed animation, we reset all the changes by clearing the Transformation on all boxes
        foreach (var sceneNode in _boxesGroupNode)
            sceneNode.Transform = null;

        UpdateSceneObjectsIfNeeded();
        UpdateAnimationSettings();

        if (isAnimationStarted)
            _animation.Start();
    }

    private void CreateOrUpdateAnimation()
    {
        if (Scene == null)
            return;

        if (_animation == null)
        {
            _animation = AnimationBuilder.CreateTransformationAnimation(Scene, "Animation1");

            _animation.Set(TransformationAnimatedProperties.TranslateY, propertyValue: 100);
            _animation.Loop = true;

            _animation.Updated += delegate(object? sender, EventArgs args)
            {
                UpdatePlanarShadow();
            };
        }
        else
        {
            // If animation already existed, then just update the target objects
            _animation.RemoveAllTargets();
        }

        // Add target objects to all SceneNodes that has name that begins with "AnimatedBox_"
        _animation.AddTargets("AnimatedBox_*");

        UpdateAnimationSettings();
    }

    private void UpdateAnimationSettings()
    {
        if (_animation == null)
            return;

        switch (_currentDelay)
        {
            case SampleDelayTypes.None:
                _animation.SetDelay(0);
                break;
            case SampleDelayTypes.Fixed1000ms:
                _animation.SetDelay(1000); // 1 second
                break;
            case SampleDelayTypes.ByAbsoluteNodeIndex:
                // increase start delay by 50 ms for each node 
                _animation.SetDelay((sceneNode, nodeIndex, nodesCount) => nodeIndex * 50);
                break;
            case SampleDelayTypes.ByRelativeNodeIndex:
                // Max delay is 500 ms: set scene node delay based on index 
                _animation.SetDelay((sceneNode, nodeIndex, nodesCount) => ((float)(nodeIndex + 1) / (float)nodesCount) * 500);
                break;
            case SampleDelayTypes.BySinNodeIndex:
                // Use sinus function to distribute delay (max delay is 500 ms)
                _animation.SetDelay((sceneNode, nodeIndex, nodesCount) => 500 - MathF.Sin((float)(nodeIndex + 1) / (float)nodesCount * MathF.PI) * 500);
                break;
            case SampleDelayTypes.StaggeringFromCenter:
                // Use GetStaggeringFunction to define delay.
                // The delay will be increased by 200 starting at the center item.
                // GetStaggeringFunction is usually used for objects in a row, but can be also used for objects in a grid formation.
                _animation.SetDelay(GetStaggeringFunction(valueIncrease: 200, StaggeringStartingPositions.Center));
                break;
            case SampleDelayTypes.GridStaggerFromFirst:
                // For objects in a grid formation, we can use GetGridStaggeringFunction.
                // 200 ms delay will be multiplied by the distance from the first item in the grid.
                _animation.SetDelay(GetGridStaggeringFunction(itemsInRow: _usedColumnsCount, valueIncrease: 200));
                break;
            case SampleDelayTypes.GridStaggerFromCenter:
                // For objects in a grid formation, we can use GetGridStaggeringFunction.
                // 200 ms delay will be multiplied by the distance from the center item in the grid.
                _animation.SetDelay(GetGridStaggeringFunction(itemsInRow: _usedColumnsCount, valueIncrease: 200, StaggeringStartingPositions.Center));
                break;
            case SampleDelayTypes.GridStaggerReverseXAxis:
                // For objects in a grid formation, we can use GetGridStaggeringFunction.
                // 200 ms delay will be multiplied by the distance last row (last row is has max Y value).
                _animation.SetDelay(GetGridStaggeringFunction(itemsInRow: _usedColumnsCount, valueIncrease: 200, StaggeringStartingPositions.Last, useXAxis: false));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        switch (_currentDuration)
        {
            case SampleDurationTypes.Undefined:
                _animation.SetDuration(0); // Set animation duration to 0 to unset it so the actual duration of an animation will be calculated from the keyframes
                break;
            case SampleDurationTypes.Fixed1000ms:
                _animation.SetDuration(1000); // 1 second
                break;
            case SampleDurationTypes.Fixed2000ms:
                _animation.SetDuration(2000); // 2 seconds
                break;
            case SampleDurationTypes.StaggeringFromCenter:
                // Use GetStaggeringFunction to define duration.
                // The duration's start value will be 1000 ms and will be increased by 200 starting at the center item.
                _animation.SetDuration(GetGridStaggeringFunction(itemsInRow: _usedColumnsCount, valueIncrease: 200, StaggeringStartingPositions.Center, startValue: 1000));

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        _animation.SetEndDelay(_currentEndDelay);
        _animation.Direction = _currentAnimationDirection;
    }

    private void UpdateSelectedBoxesCount(string? selectedBoxesCountText)
    {
        if (selectedBoxesCountText == null)
        {
            _selectedColumnCount = 6;
            _selectedRowsCount = 5;
            return;
        }

        var subItemTexts = selectedBoxesCountText.Split('x', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        _selectedColumnCount = Int32.Parse(subItemTexts[0]);
        _selectedRowsCount = Int32.Parse(subItemTexts[1]);

        UpdateAnimation();
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        _uiProvider = ui;

        double valuesWidth = 180;

        ui.SetSettingFloat("HeaderTopMargin", 8);
        ui.SetSettingFloat("HeaderBottomMarin", 0);
        ui.SetSettingFloat("FontSize", 12);

        ui.CreateStackPanel(PositionTypes.BottomRight);

        ui.CreateLabel("Boxes count:", isHeader: true);
        ui.CreateComboBox(new string[] { "5 x 1", "1 x 5", "3 x 5", "5 x 5", "6 x 4", "6 x 5", "7 x 5" },
            (index, selectedText) => UpdateSelectedBoxesCount(selectedText),
            selectedItemIndex: 5);

        ui.CreateLabel("Delay:", isHeader: true);
        ui.CreateRadioButtons(new string[] {
                    "None",
                    "Fixed: 1000ms",
                    "By absolute node index (?):increase start delay by 50 ms for each node:\n_animation.SetDelay((sceneNode, nodeIndex, nodesCount) => nodeIndex * 20);",
                    "By relative node index (?):Max delay is 500 ms: set scene node delay based on index:\n_animation.SetDelay((sceneNode, nodeIndex, nodesCount) => ((float)(nodeIndex + 1) / (float)nodesCount) * 500);",
                    "By Sin node index (?):Use sinus function to distribute delay (max delay is 500 ms):\n_animation.SetDelay((sceneNode, nodeIndex, nodesCount) => 500 - MathF.Sin((float)(nodeIndex + 1) / (float)nodesCount * MathF.PI) * 500);",
                    "Stagger from center (?):Use GetStaggeringFunction to define delay.\nThe delay will be increased by 200 starting at the center item.\nGetStaggeringFunction is usually used for objects in a row, but can be also used for objects in a grid formation.\n_animation.SetDelay(GetStaggeringFunction(valueIncrease: 200, StaggeringStartingPositions.Center));",
                    $"Grid stagger from first (?):For objects in a grid formation, we can use GetGridStaggeringFunction.\n200 ms delay will be multiplied by the distance from the first item in the grid.\n_animation.SetDelay(GetGridStaggeringFunction(itemsInRow: {_usedColumnsCount}, valueIncrease: 200));",
                    $"Grid stagger from center (?):For objects in a grid formation, we can use GetGridStaggeringFunction.\n200 ms delay will be multiplied by the distance from the center item in the grid.\n_animation.SetDelay(GetGridStaggeringFunction(itemsInRow: {_usedColumnsCount}, valueIncrease: 200, StaggeringStartingPositions.Center));",
                    $"Grid stagger reverse X axis (?):For objects in a grid formation, we can use GetGridStaggeringFunction.\n200 ms delay will be multiplied by the distance last row (last row is has max Y value).\n_animation.SetDelay(GetGridStaggeringFunction(itemsInRow: {_usedColumnsCount}, valueIncrease: 200, StaggeringStartingPositions.Last, useXAxis: false));"
                },
                (selectedIndex, selectedText) =>
                {
                    _currentDelay = (SampleDelayTypes)selectedIndex;
                    UpdateAnimation();
                },
                selectedItemIndex: 7);

        ui.CreateLabel("Duration:", isHeader: true);
        ui.CreateRadioButtons(new string[] {
                "Undefined (?):When animation's duration is not set, then it is calculated from the animation keyframes",
                "Fixed: 1000ms",
                "Fixed: 2000ms",
                $"Grid stagger from center (?):Use GetStaggeringFunction to define duration.\nThe duration's start value will be 1000 ms and will be increased by 200 starting at the center item.\n_animation.SetDuration(GetGridStaggeringFunction(itemsInRow: {_usedColumnsCount}, valueIncrease: 200, StaggeringStartingPositions.Center, startValue: 1000));"
            },
            (selectedIndex, selectedText) =>
            {
                _currentDuration = (SampleDurationTypes)selectedIndex;
                UpdateAnimation();
            },
            selectedItemIndex: 1);

        ui.CreateLabel("EndDelay (?):EndDelay specifies the delay after the last keyframe.\nIt can be also set to function as demonstrated with Delay and Duration but for simplicity that is not used here.", isHeader: true);
        ui.CreateComboBox(new string[] {
                "None",
                "500ms",
                "1000ms",
                "1500ms"
            },
            (selectedIndex, selectedText) =>
            {
                _currentEndDelay = selectedIndex * 500;
                UpdateAnimation();
            },
            selectedItemIndex: 0);

        ui.CreateLabel("Direction:", isHeader: true);
        ui.CreateComboBox(new string[] {
                "Normal",
                "Reverse",
                "Alternate"
            },
            (selectedIndex, selectedText) =>
            {
                _currentAnimationDirection = (AnimationDirections)selectedIndex;
                UpdateAnimation();
            },
            selectedItemIndex: 2,
            width: valuesWidth);
    }


    public enum StaggeringStartingPositions
    {
        First,
        Last,
        Center
    }

    public static Func<SceneNode, int, int, float> GetStaggeringFunction(float valueIncrease, StaggeringStartingPositions startingPosition = StaggeringStartingPositions.First, float startValue = 0)
    {
        return GetGridStaggeringFunction(itemsInRow: 1, valueIncrease, startingPosition, startValue, useXAxis: false, useYAxis: true);
    }

    public static Func<SceneNode, int, int, float> GetGridStaggeringFunction(
        int itemsInRow,
        float valueIncrease,
        StaggeringStartingPositions startingPosition = StaggeringStartingPositions.First,
        float startValue = 0,
        bool useXAxis = true,
        bool useYAxis = true)
    {
        if (itemsInRow <= 0) throw new ArgumentOutOfRangeException(nameof(itemsInRow));

        return new Func<SceneNode, int, int, float>((sceneNode, nodeIndex, nodesCount) =>
        {
            int x = nodeIndex % itemsInRow;
            int y = (int)(nodeIndex / itemsInRow);

            int rowsCount = (int)(MathF.Ceiling((float)nodesCount / (float)itemsInRow));

            int maxX = itemsInRow - 1;
            int maxY = rowsCount - 1;


            float distance;

            if (useXAxis && useYAxis)
            {
                float xPos = startingPosition switch
                {
                    StaggeringStartingPositions.First => x,
                    StaggeringStartingPositions.Last => maxX - x,
                    StaggeringStartingPositions.Center => (x - maxX * 0.5f),
                    _ => 0,
                };

                float yPos = startingPosition switch
                {
                    StaggeringStartingPositions.First => y,
                    StaggeringStartingPositions.Last => maxY - y,
                    StaggeringStartingPositions.Center => (y - maxY * 0.5f),
                    _ => 0,
                };

                distance = MathF.Sqrt(xPos * xPos + yPos * yPos);
            }
            else
            {
                if (useXAxis)
                {
                    distance = startingPosition switch
                    {
                        StaggeringStartingPositions.First => x,
                        StaggeringStartingPositions.Last => maxX - x,
                        StaggeringStartingPositions.Center => Math.Abs(x - maxX * 0.5f),
                        _ => 0,
                    };
                }
                else if (useYAxis)
                {
                    distance = startingPosition switch
                    {
                        StaggeringStartingPositions.First => y,
                        StaggeringStartingPositions.Last => maxY - y,
                        StaggeringStartingPositions.Center => Math.Abs(y - maxY * 0.5f),
                        _ => 0,
                    };
                }
                else
                {
                    // useXAxis == false && useYAxis == false
                    distance = 0;
                }
            }

            return startValue + distance * valueIncrease;
        });
    }

    protected override void OnDisposed()
    {
        if (_animation != null)
            _animation.Stop();

        base.OnDisposed();
    }
}