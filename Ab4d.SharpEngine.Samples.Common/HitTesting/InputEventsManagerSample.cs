using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.HitTesting;

public class InputEventsManagerSample : CommonSample
{
    public override string Title => "InputEventsManager helps use pointer or mouse events on 3D objects";
    
    public override string Subtitle => "The following pointer / mouse events are demonstrated here:\n- PointerEnter\n- PointerLeave\n- PointerMove\n- PointerClick\n- PointerDoubleClick\n- MouseWheel";

    private readonly Material _normalMaterial        = StandardMaterials.Silver;
    private readonly Material _selectedMaterial      = StandardMaterials.Orange;
    private readonly Material _clickedMaterial       = StandardMaterials.Red;
    private readonly Material _doubleClickedMaterial = StandardMaterials.Magenta;

    private bool _isGlassBoxExcluded;
    private ManualInputEventsManager? _inputEventsManager;

    private BoxModelNode? _baseBoxModelNode;
    private BoxModelNode? _glassBoxModelNode;
    private WireCrossNode? _wireCrossNode;
    
    private ModelNode? _selectedModelNode;
    

    public InputEventsManagerSample(ICommonSamplesContext context)
        : base(context)
    {
        RotateCameraConditions = MouseAndKeyboardConditions.RightMouseButtonPressed;
        MoveCameraConditions = MouseAndKeyboardConditions.RightMouseButtonPressed | MouseAndKeyboardConditions.ControlKey;
        QuickZoomConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed | MouseAndKeyboardConditions.RightMouseButtonPressed;
        IsMouseWheelZoomEnabled = false; // Disable wheel zoom because here we demonstrate mouse wheel handling and this prevents using wheel two times

        ShowCameraAxisPanel = true;
    }

    protected override void OnCreateScene(Scene scene)
    {
        _baseBoxModelNode = new BoxModelNode(centerPosition: new Vector3(0, -10, 0), size: new Vector3(500, 20, 400), material: StandardMaterials.Green, "Base box");
        scene.RootNode.Add(_baseBoxModelNode);


        _wireCrossNode = new WireCrossNode(new Vector3(0, 0, 0), Colors.Red, lineLength: 30, lineThickness: 3)
        {
            Visibility = SceneNodeVisibility.Hidden
        };
        scene.RootNode.Add(_wireCrossNode);


        // Glass box is used to demonstrate the RegisterExcludedSceneNode method (3D models that do not block hit-testing)
        _glassBoxModelNode = new BoxModelNode(centerPosition: new Vector3(0, 90, 200), size: new Vector3(200, 220, 10), material: StandardMaterials.LightBlue.SetOpacity(0.3f), "Glass box");
        scene.RootNode.Add(_glassBoxModelNode);

        
        // 10 BoxModelNode will be created in the OnInputEventsManagerInitialized
        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = -30;
            targetPositionCamera.Attitude = -25;
            targetPositionCamera.Distance = 900;
        }
    }

    /// <summary>
    /// OnInputEventsManagerInitialized can be overridden to initialize the InputEventsManager.
    /// </summary>
    /// <param name="inputEventsManager">ManualInputEventsManager</param>
    protected override void OnInputEventsManagerInitialized(ManualInputEventsManager inputEventsManager)
    {
        // NOTE:
        // This method gets inputEventsManager that is ManualInputEventsManager and not InputEventsManager.
        // ManualInputEventsManager is a genetic platform-independent object that is defined in Ab4d.SharpEngine.
        // The InputEventsManager is platform specific and a different class is defined for WPF, Avalonia, and other platforms.

        _inputEventsManager = inputEventsManager;

        var scene = inputEventsManager.SceneView.Scene;


        // By default, exclude _glassBox from hit testing so objects behind it can be selected and clicked
        SetupExcludedGlassBox(isExcluded: true);


        //
        // When user will move the pointer or mouse over the green box
        // we will show a red wire-cross lines at the hit position
        //
        if (_baseBoxModelNode != null)
        {
            // Because we are using just a single ModelNode we can create an instance of
            // ModelNodeEventsSource and then register it on the inputEventsManager.
            var baseBoxEventsSource = new ModelNodeEventsSource(_baseBoxModelNode);
            inputEventsManager.RegisterEventsSource(baseBoxEventsSource);

            // After that we can subscribe to many pointer events:
            
            baseBoxEventsSource.PointerEntered += (sender, args) =>
            {
                if (_wireCrossNode != null)
                {
                    _wireCrossNode.Position = args.RayHitResult.HitPosition;
                    _wireCrossNode.Visibility = SceneNodeVisibility.Visible;
                }
            };

            baseBoxEventsSource.PointerExited += (sender, args) =>
            {
                if (_wireCrossNode != null)
                    _wireCrossNode.Visibility = SceneNodeVisibility.Hidden;
            };

            baseBoxEventsSource.PointerMoved += (sender, args) =>
            {
                if (_wireCrossNode != null)
                    _wireCrossNode.Position = args.RayHitResult.HitPosition;
            };
        }


        //
        // Create 9 pyramid model nodes
        // For each of the pyramid, we will create a new ModelNodeEventsSource and subscribe to pointer events
        //
        
        for (int i = 0; i <= 8; i++)
        {
            //var oneBox = new BoxModelNode(new Vector3(i * 50 - 200, 20, 100), new Vector3(40, 40, 40), _normalMaterial, $"Box_{i}");
            var onePyramid = new PyramidModelNode(new Vector3(i * 50 - 200, 0, 100), new Vector3(40, 40, 40), _normalMaterial, $"Pyramid_{i}");
            scene.RootNode.Add(onePyramid);

            // Create ModelNodeEventsSource with the created BoxModelNode ...
            var modelNodeEventsSource = new ModelNodeEventsSource(onePyramid);
            inputEventsManager.RegisterEventsSource(modelNodeEventsSource);

            // ... and then we can subscribe to various pointer / mouse events on that 3D objects.
            modelNodeEventsSource.PointerEntered += (sender, args) =>
            {
                ProcessSelect(onePyramid);
            };

            modelNodeEventsSource.PointerExited += (sender, args) =>
            {
                ProcessDeSelect();
            };
            
            modelNodeEventsSource.PointerClicked += (sender, args) =>
            {
                ProcessClick(onePyramid);  
            };

            modelNodeEventsSource.PointerDoubleClicked += (sender, args) =>
            {
                ProcessDoubleClick(onePyramid);  
            };
            
            modelNodeEventsSource.PointerWheelChanged += (sender, args) =>
            {
                //ProcessScale(oneBox, args.MouseWheelDelta);  
                ProcessRotate(onePyramid, args.MouseWheelDelta);  
            };

            // Dragging is demonstrated in InputEventsManagerWithSurfaceSample sample
            // (this also requires registering a drag surface, so just uncommenting the code below will not work yet)

            //modelNodeEventsSource.BeginPointerDrag += (sender, args) =>
            //{
            //    System.Diagnostics.Debug.WriteLine("BeginPointerDrag");
            //};

            //modelNodeEventsSource.EndPointerDrag += (sender, args) =>
            //{
            //    System.Diagnostics.Debug.WriteLine("EndPointerDrag");
            //};

            //modelNodeEventsSource.PointerDrag += (sender, args) =>
            //{
            //    System.Diagnostics.Debug.WriteLine(args.SurfaceHitPointDiff);
            //};
        }


        //
        // Create 9 sphere model nodes and add them to the GroupNode
        // Create a single MultiModelNodesEventsSource from the GroupNode and subscribe to pointer events
        //

        var spheresGroupNode = new GroupNode("SpheresGroup");
        scene.RootNode.Add(spheresGroupNode);

        for (int i = 0; i <= 8; i++)
        {
            var oneSphere = new SphereModelNode(new Vector3(i * 50 - 200, 20, 20), radius: 20, _normalMaterial, $"Sphere_{i}");
            spheresGroupNode.Add(oneSphere);
        }

        var multiModelNodesEventsSource = new MultiModelNodesEventsSource(spheresGroupNode);
        
        // Other options to create a MultiModelNodesEventsSource:
        //var multiModelNodesEventsSource = new MultiModelNodesEventsSource(sphere1, sphere2, sphere3);
        //var multiModelNodesEventsSource = new MultiModelNodesEventsSource(spheresList);
        //var multiModelNodesEventsSource = new MultiModelNodesEventsSource();
        //multiModelNodesEventsSource.ModelNodes.Add(sphere1);
        //multiModelNodesEventsSource.ModelNodes.Add(sphere2);

        inputEventsManager.RegisterEventsSource(multiModelNodesEventsSource);


        multiModelNodesEventsSource.PointerEntered += (sender, args) =>
        {
            if (args.RayHitResult.HitSceneNode is ModelNode modelNode)
                ProcessSelect(modelNode);
        };

        multiModelNodesEventsSource.PointerExited += (sender, args) =>
        {
            ProcessDeSelect();
        };
            
        multiModelNodesEventsSource.PointerClicked += (sender, args) =>
        {
            if (args.RayHitResult.HitSceneNode is ModelNode modelNode)
                ProcessClick(modelNode);  
        };

        multiModelNodesEventsSource.PointerDoubleClicked += (sender, args) =>
        {
            if (args.RayHitResult.HitSceneNode is ModelNode modelNode)
                ProcessDoubleClick(modelNode);  
        };

        multiModelNodesEventsSource.PointerWheelChanged += (sender, args) =>
        {
            if (args.RayHitResult.HitSceneNode is ModelNode modelNode)
            {
                ProcessScale(modelNode, args.MouseWheelDelta);  
                //ProcessRotate(modelNode, args.MouseWheelDelta);
            }
        };
        

        //
        // Create 9 box model nodes and add them to the RootNode
        // Create a single NamedModelNodesEventsSource with wildcard name pattern that matches all boxes and subscribe to pointer events
        //
        
        for (int i = 0; i <= 8; i++)
        {
            var oneBox = new BoxModelNode(new Vector3(i * 50 - 200, 20, -100), new Vector3(40, 40, 40), _normalMaterial, $"Box_{i}");
            scene.RootNode.Add(oneBox);
        }

        // NamedModelNodesEventsSource is used to register ModelNode objects by their name.
        // It is also possible to use wildcard name that is defined when the name starts with * or end with *.
        // For example, "Box*" matches all ModelNodes whose name start with "Box"; "*_blue" matches all ModelNodes whose name ends with "_blue".
        
        var namedModelNodesEventsSource = new NamedModelNodesEventsSource("Box_*");
        inputEventsManager.RegisterEventsSource(namedModelNodesEventsSource);

        namedModelNodesEventsSource.PointerEntered += (sender, args) =>
        {
            if (args.RayHitResult.HitSceneNode is ModelNode modelNode)
                ProcessSelect(modelNode);
        };

        namedModelNodesEventsSource.PointerExited += (sender, args) =>
        {
            ProcessDeSelect();
        };
            
        namedModelNodesEventsSource.PointerClicked += (sender, args) =>
        {
            if (args.RayHitResult.HitSceneNode is ModelNode modelNode)
                ProcessClick(modelNode);  
        };

        namedModelNodesEventsSource.PointerDoubleClicked += (sender, args) =>
        {
            if (args.RayHitResult.HitSceneNode is ModelNode modelNode)
                ProcessDoubleClick(modelNode);  
        };

        namedModelNodesEventsSource.PointerWheelChanged += (sender, args) =>
        {
            if (args.RayHitResult.HitSceneNode is ModelNode modelNode)
            {
                //ProcessScale(modelNode, args.MouseWheelDelta);  
                ProcessRotate(modelNode, args.MouseWheelDelta);
            }
        };
    }


    private void ProcessSelect(ModelNode modelNode)
    {
        if (modelNode.Material == _normalMaterial)
            modelNode.Material = _selectedMaterial;

        _selectedModelNode = modelNode;
    }
    
    private void ProcessDeSelect()
    {
        if (_selectedModelNode != null)
        {
            if (_selectedModelNode.Material == _selectedMaterial) // Preserve clicked and double-clicked materials
                _selectedModelNode.Material = _normalMaterial;

            _selectedModelNode = null;
        }
    }
    
    private void ProcessClick(ModelNode modelNode)
    {
        if (modelNode.Material != _clickedMaterial)
            modelNode.Material = _clickedMaterial;
        else
            modelNode.Material = _selectedMaterial;   
    }
    
    private void ProcessDoubleClick(ModelNode modelNode)
    {
        if (modelNode.Material != _doubleClickedMaterial)
            modelNode.Material = _doubleClickedMaterial;
        else
            modelNode.Material = _selectedMaterial;   
    }
    
    private void ProcessRotate(ModelNode modelNode, float mouseWheelDelta)
    {
        if (modelNode.Transform is not AxisAngleRotateTransform axisAngleRotateTransform)
        {
            // Create a new AxisAngleRotateTransform and set pivotPoint to the model's center position.
            // If we would not set the center, then the rotation would happen around (0, 0, 0) and this would also move the object.
            axisAngleRotateTransform = new AxisAngleRotateTransform(axis: new Vector3(0, 1, 0), angle: 0, pivotPoint: modelNode.GetCenterPosition());
            modelNode.Transform = axisAngleRotateTransform;
        }

        axisAngleRotateTransform.Angle  += mouseWheelDelta < 0 ? -5 : +5;
    }
    
    private void ProcessScale(ModelNode modelNode, float mouseWheelDelta)
    {
        if (modelNode.Transform is not ScaleTransform scaleTransform)
        {
            // Create a new ScaleTransform and set pivotPoint to the model's center position.
            // If we would not set the center, then the scale would happen from (0, 0, 0) and this would also move the object.
            scaleTransform = new ScaleTransform(scaleFactors: new Vector3(1, 1, 1), pivotPoint: modelNode.GetCenterPosition());
            modelNode.Transform = scaleTransform;
        }

        float scaleFactor = mouseWheelDelta < 0 ? 1.05f : 0.95f; // increase or decrease the size
        var newScaleFactor = scaleTransform.GetAverageScale() * scaleFactor;

        scaleTransform.SetScale(newScaleFactor);
    }

    private void SetupExcludedGlassBox(bool isExcluded)
    {
        if (_inputEventsManager == null || _glassBoxModelNode == null || isExcluded == _isGlassBoxExcluded)
            return;

        if (isExcluded)
        {
            // RegisterExcludedSceneNode method registers a SceneNode that will be excluded from hit testing
            // (this means that we can hit 3D objects behind the excluded object).
            // This is usually used for transparent objects.
            _inputEventsManager.RegisterExcludedSceneNode(_glassBoxModelNode);
        }
        else
        {
            _inputEventsManager.RemoveExcludedSceneNode(_glassBoxModelNode);
        }

        _isGlassBoxExcluded = isExcluded;
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("Special settings:", isHeader: true);

        ui.CreateCheckBox("Exclude hit testing on glass box (?):When checked then the RegisterExcludedSceneNode is called on the _glassBoxModelNode and this excludes that object from hit testing so objects behind it can be hit.", 
            _isGlassBoxExcluded, 
            isChecked => SetupExcludedGlassBox(isChecked));
        
        ui.CreateCheckBox("TriggerEnterLeaveEventsOnEachSceneNodeChange (?):When checked then Enter and Leave events are triggered on each change of the SceneNode.\nWhen unchecked, then Enter and Leave events are triggered only when InputEventSource object is changed\n(in this example this can be seen by unchecking this CheckBox and then moving pointer from one box to another).", 
            true, 
            isChecked =>
            {
                if (_inputEventsManager != null)
                    _inputEventsManager.TriggerEnterLeaveEventsOnEachSceneNodeChange = isChecked;
            });
    }
}