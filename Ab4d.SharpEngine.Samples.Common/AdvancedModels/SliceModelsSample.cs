using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class SliceModelsSample : CommonSample
{
    public override string Title => "Slice 3D model with a plane";

        
    private readonly Slicer _slicer;
    
    private enum SceneTypes
    {
        Box,
        Sphere,
        Teapot,
        MultipleModels,
        Robotarm,
    }

    private SceneTypes _currentSceneType = SceneTypes.MultipleModels;
    
    private int _selectedPlaneIndex = 2;

    private float _transformSliderValue = 50;

    private bool _isTranslateTransform = true;
    
    private float _transformationAmount = 0.5f;
    
    private GroupNode _frontGroupNode;
    private GroupNode _backGroupNode;
    private GroupNode _slicesOnPlaneGroup;
    private MultiLineNode _wireframeLineNode;
    
    private TranslateTransform? _frontGroupTranslateTransform;
    private TranslateTransform? _backGroupTranslateTransform;

    private Plane[] _allPlanes;
    
    private BoundingBox _modelBounds;
    
    private bool _showEdgeLines = true;
    private bool _showWireframe = false;
    private bool _showFront = true;
    private bool _showBack = true;
    private bool _showSliceMesh = true;
    private bool _closeMesh = false;
    
    private StandardMesh? _originalMesh;
    private GroupNode? _originalGroupNode;
    private StandardMesh? _teapotMesh;
    private GroupNode? _robotArmModel;

    private EdgeLinesFactory? _edgeLinesFactory;
    
    private float _rootModelSize;
    
    private ICommonSampleUIElement? _transformAmountLabel;
    private ICommonSampleUIElement? _showWireframeCheckBox;
    private ICommonSampleUIElement? _showEdgeLinesCheckBox;


    public SliceModelsSample(ICommonSamplesContext context)
        : base(context)
    {
        // Create the Slicer helper class that can slice 3D models
        _slicer = new Slicer()
        {
            // When we use Slicer only to slice 3D models and we do not need the 2D slice shape or to close the mesh, 
            // then we can set the CollectIntersectionPoints to false.
            //CollectIntersectionPoints = false
            
            // When creating closed meshes or shape polylines, then Slicer needs to combine duplicate positions
            // (positions that lie at the same position in 3D space). But because of limited float precision (32 bits)
            // this leads to floating point imprecision. To solve that the actual positions are converted into 
            // normalized positions with a fixed precision (deleting the least significant bits).
            // The number of used bits is defined by the DuplicatePositionsPrecisionBitsCount.
            // 
            // By default, it is set to 18 bits. If you still find that some closed meshes are not generated,
            // you can decrease this number. But be aware that too low precision can lead to wrong results,
            // especially for meshes with positions that are very close to each other.
            DuplicatePositionsPrecisionBitsCount = 18
        };


        // Plane is created by defining plane's normal vector (first 3 numbers) and an offset d (forth number):
        _allPlanes = new Plane[]
        {
            new Plane(1, 0, 0, 0),
            new Plane(0, 1, 0, 0),
            new Plane(0, 0, 1, 0),
        };

        _frontGroupNode = new GroupNode("FrontGroup");
        _frontGroupTranslateTransform = new TranslateTransform();
        _frontGroupNode.Transform = _frontGroupTranslateTransform;

        _backGroupNode = new GroupNode("BackGroup");
        _backGroupTranslateTransform = new TranslateTransform();
        _backGroupNode.Transform = _backGroupTranslateTransform;

        _slicesOnPlaneGroup = new GroupNode("SlicesOnPlaneGroup");

        _wireframeLineNode = new MultiLineNode(lineColor: Colors.Black, lineThickness: 1, "WireframeLinesNode")
        {
            DepthBias = 0.005f
        };

        ShowCameraAxisPanel = true;
    }

    protected override async Task OnCreateSceneAsync(Scene scene)
    {
        // We add additional front and back GroupNodes that will provide separation from the front and back models.
        // Here we only create TranslateTransform objects. The actual transformation is set in UpdateSlicedModel method.

        scene.RootNode.Add(_frontGroupNode);
        scene.RootNode.Add(_backGroupNode);
        scene.RootNode.Add(_slicesOnPlaneGroup);
        scene.RootNode.Add(_wireframeLineNode);

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading  = 50;
            targetPositionCamera.Attitude = -20;
            targetPositionCamera.Distance = 500;
        }

        
        UpdateScene();
        
        UpdateSlicedModel();


        // Load teapot and robot arm models so if user changes the scene type, they will be already loaded
        _teapotMesh = await base.GetCommonMeshAsync(scene, CommonMeshes.Teapot);
        _robotArmModel = await base.GetCommonSceneAsync(scene, CommonScenes.RobotArm);

        // If initially the _currentSceneType was set to Teapot or Robotarm, then update the scene after the resources are loaded.
        if (_currentSceneType == SceneTypes.Teapot || _currentSceneType == SceneTypes.Robotarm)
        {
            UpdateScene();
            UpdateSlicedModel();
        }
    }

    private void UpdateScene()
    {
        _originalMesh = null;
        _originalGroupNode = null;
        _modelBounds = BoundingBox.Undefined;
        
        switch (_currentSceneType)
        {
            case SceneTypes.Box:
                _originalMesh = MeshFactory.CreateBoxMesh(centerPosition: new Vector3(0, 0, 0), size: new Vector3(120, 80, 100));
                _modelBounds = _originalMesh.BoundingBox;
                break;
            
            case SceneTypes.Sphere:
                _originalMesh = MeshFactory.CreateSphereMesh(centerPosition: new Vector3(0, 0, 0), radius: 50, segments: 20);
                _modelBounds = _originalMesh.BoundingBox;
                break;
            
            case SceneTypes.Teapot:
                if (_teapotMesh != null)
                {
                    _originalMesh = _teapotMesh;
                    _modelBounds = _teapotMesh.BoundingBox;
                }
                break;
            
            case SceneTypes.MultipleModels:
                _originalGroupNode = new GroupNode("MultipleModelsGroup");
                
                _originalGroupNode.Add(new PyramidModelNode(bottomCenterPosition: new Vector3(-360, -60, 0), size: new Vector3(120, 120, 120), StandardMaterials.Green));
                
                _originalGroupNode.Add(new BoxModelNode(centerPosition: new Vector3(-180, 0, 0), size: new Vector3(120, 120, 120), StandardMaterials.Green)
                {
                    // We must not use shared mesh because in this case the mesh is generated only after the BoxModelNode is added to the Scene (to get a shared box mesh).
                    // But this BoxModelNode is never added to the Scene because it is only used to generate the sliced models.
                    // When UseSharedBoxMesh is false, then mesh is created only for that instance of BoxModelNode.
                    UseSharedBoxMesh = false 
                });
                
                _originalGroupNode.Add(new TorusKnotModelNode(centerPosition: new Vector3(0, 0, 0), radius1: 40, radius2: 20, radius3: 10, StandardMaterials.Green) { P = 3, Q = 4 });
                
                _originalGroupNode.Add(new TubeModelNode(bottomCenterPosition: new Vector3(160, -60, 0), outerRadius: 60, innerRadius: 40, height: 120, segments: 4, StandardMaterials.Green)
                {
                    Transform = new StandardTransform(rotateY: 90) { PivotPoint = new Vector3(160, -60, 0) }
                });
                
                _originalGroupNode.Add(new TubeModelNode(bottomCenterPosition: new Vector3(320, -60, 0), outerRadius: 60, innerRadius: 40, height: 120, segments: 30, StandardMaterials.Green));

                _modelBounds = _originalGroupNode.GetLocalBoundingBox(updateIfDirty: true);
                
                break;
            
            case SceneTypes.Robotarm:
                if (_robotArmModel != null)
                {
                    _originalGroupNode = _robotArmModel;
                    _modelBounds = _robotArmModel.GetLocalBoundingBox();
                }

                break;
        }

        _rootModelSize = MathF.Sqrt(_modelBounds.SizeX * _modelBounds.SizeX + _modelBounds.SizeY * _modelBounds.SizeY + _modelBounds.SizeZ * _modelBounds.SizeZ);
        
        if (targetPositionCamera != null)
            targetPositionCamera.Distance = 2 * _rootModelSize;
    }

    private void UpdateSlicedModel()
    {
        _frontGroupNode.Clear();
        _backGroupNode.Clear();
        _slicesOnPlaneGroup.Clear();

        _wireframeLineNode.Positions = null;
        
        
        // Get the selected 3D plane
        var plane = GetSelectedPlane();

        // Transform the plane that will be used to slice the model
        var planeTransform = GetSelectedPlaneTransform(updateTransformationAmount: true);
        plane = Plane.Transform(plane, planeTransform.Value);
        

        // Update Slicer        
        _slicer.Plane = plane;
        _slicer.CloseSlicedMeshes = _closeMesh;

        
        SceneNode? frontSceneNode = null;
        SceneNode? backSceneNode = null;

        if (_originalMesh != null)
        {
            _slicer.SliceMesh(_originalMesh, transform: null, out var frontMesh, out var backMesh);
            
            // To get only front mesh, we can use the following method:
            // frontMesh = _slicer.SliceMesh(_originalMesh); // optionally, we can set the transform parameter
            // backMesh = null;

            if (frontMesh != null)
                frontSceneNode = new MeshModelNode(frontMesh, StandardMaterials.Gold, "FrontMeshNode");

            if (backMesh != null)
                backSceneNode = new MeshModelNode(backMesh, StandardMaterials.Gold, "BackMeshNode");
        }
        else if (_originalGroupNode != null)
        {
            _slicer.SliceGroupNode(_originalGroupNode, parentTransform: null, out var frontGroupNode, out var backGroupNode);
            frontSceneNode = frontGroupNode;
            backSceneNode = backGroupNode;
            
            // To get only front group node, we can use the following method:
            // frontGroupNode = _slicer.SliceGroupNode(_originalGroupNode); // optionally, we can set the parentTransform parameter
            // backGroupNode = null;

            // You can also use:
            // _slicer.SliceModelNode(modelNode);
            // _slicer.SliceSceneNode(sceneNode);
        }
       
        
        ShowSlice(frontSceneNode, isFront: true);
        ShowSlice(backSceneNode, isFront: false);
        
        
        if (_showSliceMesh)
        {
            if (_originalMesh != null)
            {
                ShowSliceMesh(_originalMesh);
            }
            else if (_originalGroupNode != null)
            {
                _originalGroupNode.ForEachChild<ModelNode>(modelNode => ShowSliceMesh(modelNode.GetMesh()));
            }
        }

        
        if (this.Scene != null)
        {
            if (_showEdgeLines)
            {
                _edgeLinesFactory ??= new EdgeLinesFactory(); // Reuse the EdgeLinesFactory object. This reuses the internal collections.
                var edgeLines = _edgeLinesFactory.CreateEdgeLines(this.Scene.RootNode, edgeStartAngleInDegrees: 25);

                _wireframeLineNode.Positions = edgeLines.ToArray();
            }
            else if (_showWireframe)
            {
                _wireframeLineNode.Positions = LineUtils.GetWireframeLinePositions(this.Scene.RootNode);
            }
        }
    }
    
    private void ShowSlice(SceneNode? sceneNode, bool isFront)
    {
        if (sceneNode == null || (isFront && !_showFront) || (!isFront && !_showBack))
            return;


        // Set BackMaterial to red color to show the inner parts of the models
        Utilities.ModelUtils.ChangeBackMaterial(sceneNode, StandardMaterials.Red);

            
        // Define separationTransform that separates the front and back models
        var plane = _slicer.Plane;
        float slicedModelsSeparation = _rootModelSize * 0.2f;

        if (!isFront)
            slicedModelsSeparation *= -1;

        var separationTransform = new TranslateTransform(plane.Normal.X * slicedModelsSeparation, 
                                                         plane.Normal.Y * slicedModelsSeparation, 
                                                         plane.Normal.Z * slicedModelsSeparation);

        sceneNode.Transform = separationTransform;


        // Add to scene
        if (isFront)
            _frontGroupNode.Add(sceneNode);
        else
            _backGroupNode.Add(sceneNode);
    }

    private void ShowSliceMesh(Mesh? mesh)
    {
        if (mesh is not StandardMesh standardMesh)
            return;
        
        var closedSliceMesh = _slicer.GetClosedSliceMesh(standardMesh, useMeshTransform: true);

        if (closedSliceMesh != null)
        {
            var meshModelNode = new MeshModelNode(closedSliceMesh, StandardMaterials.Silver, "SliceMeshModel")
            {
                BackMaterial = StandardMaterials.Silver
            };
            
            _slicesOnPlaneGroup.Add(meshModelNode);
        }
    }
    
    private Plane GetSelectedPlane() => _allPlanes[_selectedPlaneIndex];
    
    private Transform GetSelectedPlaneTransform(bool updateTransformationAmount)
    {
        var plane = GetSelectedPlane();

        Transform transform;
        if (_isTranslateTransform)
        {
            float offset = _transformSliderValue / 100;
            offset = (plane.Normal.X * _modelBounds.Minimum.X + plane.Normal.Y * _modelBounds.Minimum.Y + plane.Normal.Z * _modelBounds.Minimum.Z) +
                     (offset * (plane.Normal.X * _modelBounds.SizeX + plane.Normal.Y * _modelBounds.SizeY + plane.Normal.Z * _modelBounds.SizeZ));

            transform = new TranslateTransform(plane.Normal.X * offset, plane.Normal.Y * offset, plane.Normal.Z * offset);

            _transformationAmount = offset;
        }
        else
        {
            float angle = ((_transformSliderValue / 100) - 0.5f) * 360.0f;
            var perpendicularNormal = new Vector3(plane.Normal.Y, plane.Normal.Z, plane.Normal.X);
            transform = new AxisAngleRotateTransform(axis: perpendicularNormal, angle); 

            _transformationAmount = angle;
        }
        
        if (updateTransformationAmount) // Update only if needed (updating TextBlock is slow)
            _transformAmountLabel?.UpdateValue();

        return transform;
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("View:", isHeader: true);
        
        ui.CreateCheckBox("Show front", isInitiallyChecked: _showFront, isChecked =>
        {
            _showFront = isChecked;
            UpdateSlicedModel();
        });
        
        ui.CreateCheckBox("Show back", isInitiallyChecked: _showBack, isChecked =>
        {
            _showBack = isChecked;
            UpdateSlicedModel();
        });
        
        ui.CreateCheckBox("Show slice mesh on plane", isInitiallyChecked: _showSliceMesh, isChecked =>
        {
            _showSliceMesh = isChecked;
            UpdateSlicedModel();
        });
        
        ui.CreateCheckBox("Close meshes", isInitiallyChecked: _closeMesh, isChecked =>
        {
            _closeMesh = isChecked;
            UpdateSlicedModel();
        });

        ui.AddSeparator();
        
        
        _showEdgeLinesCheckBox = ui.CreateCheckBox("Show edge lines", isInitiallyChecked: _showEdgeLines, isChecked =>
        {
            _showEdgeLines = isChecked;
            
            if (isChecked && _showWireframe)
                _showWireframeCheckBox!.SetValue(false); // show only edge lines, not wireframe
            else
                UpdateSlicedModel();
        });
        
        _showWireframeCheckBox = ui.CreateCheckBox("Show wireframe", isInitiallyChecked: _showWireframe, isChecked =>
        {
            _showWireframe = isChecked;
            
            if (isChecked && _showEdgeLines)
                _showEdgeLinesCheckBox!.SetValue(false); // show only wireframe, not edge lines
            else
                UpdateSlicedModel();
        });

        // Showing 2D slices shape is not yet implemented
        // See sample for Ab3d.PowerToys library to see how to show 2D slice polylines.
        // ui.CreateCheckBox("Show 2D slice", isInitiallyChecked: _show2DSlice, isChecked =>
        // {
        //     _show2DSlice = isChecked;
        //     UpdateSlicedModel();
        // });

        
        ui.CreateLabel("3D model:", isHeader: true);
        ui.CreateRadioButtons(new string[]
            {
                "Box", 
                "Sphere", 
                "Teapot (?):Note that the the middle part of the teapot model cannot be closed\nbecause the model is not fully connected.", 
                "Multiple models",
                "RobotArm scene (?):Note that the teapot model cannot be closed because the model is not fully connected.\nCheck the 'Teapot' RadioButton for closer investigation of that problem."
            },
            (selectedIndex, selectedText) =>
            {
                _currentSceneType = (SceneTypes)selectedIndex;
                UpdateScene();
                UpdateSlicedModel();
            },
            (int)_currentSceneType);
        
        
        ui.CreateLabel("Plane for slicing:", isHeader: true);
                                                                                                                                                            
        var planeDescriptions = _allPlanes.Select(p => $"Normal: ({p.Normal.X}, {p.Normal.Y}, {p.Normal.Z})").ToArray();
        ui.CreateRadioButtons(planeDescriptions, 
                              (selectedIndex, selectedText) =>
                              {
                                  _selectedPlaneIndex = selectedIndex;
                                  UpdateSlicedModel();
                              }, 
                              _selectedPlaneIndex);


        ui.CreateLabel("Transform:", isHeader: true);

        _transformAmountLabel = ui.CreateKeyValueLabel("Transform amount:", () => _transformationAmount.ToString("N1"));
        
        ui.CreateSlider(1, 99,
                        () => _transformSliderValue,
                        newValue =>
                        {
                            if ((int)_transformSliderValue != (int)newValue)
                            {
                                _transformSliderValue = newValue;
                                UpdateSlicedModel();
                            }
                        });
        
        ui.CreateRadioButtons(new string[] { "Translate", "Rotate" }, (selectedIndex, selectedText) =>
        {
            _isTranslateTransform = selectedIndex == 0;
            UpdateSlicedModel();
        }, selectedItemIndex: 0);
    }
}