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

    private GroupNode? _importedModel;
    private StandardMaterial _redBackMaterial;
    
    private GroupNode? _frontGroupNode;
    private GroupNode? _backGroupNode;
    
    private TranslateTransform? _frontGroupTranslateTransform;
    private TranslateTransform? _backGroupTranslateTransform;

    private Plane[] _allPlanes;

    private int _selectedPlaneIndex = 0;

    private float _slicePositionPercent = 70;
    private float _planeRotationAngle = 0;

    private EdgeLinesFactory? _edgeLinesFactory;
    private MultiLineNode? _frontEdgeLinesNode;
    private MultiLineNode? _backEdgeLinesNode;

    private bool _showFront = true;
    private bool _showBack = true;
    private bool _showEdgeLines = true;


    public SliceModelsSample(ICommonSamplesContext context)
        : base(context)
    {
        _redBackMaterial = StandardMaterials.Red; // Use a single instance for back material

        // Plane is created by defining plane's normal vector (first 3 numbers) and an offset d (forth number):
        _allPlanes = new Plane[]
        {
            new Plane(1, 0, 0, 0),
            new Plane(0, 1, 0, 0),
            new Plane(0, 0, 1, 0),
        };

        ShowCameraAxisPanel = true;
    }

    protected override async Task OnCreateSceneAsync(Scene scene)
    {
        // We add additional front and back GroupNodes that will provide separation from the front and back models.
        // Here we only create TranslateTransform objects. The actual transformation is set in UpdateSlicedModel method.
        _frontGroupNode = new GroupNode("FrontGroup");
        _frontGroupTranslateTransform = new TranslateTransform();
        _frontGroupNode.Transform = _frontGroupTranslateTransform;
        scene.RootNode.Add(_frontGroupNode);

        _backGroupNode = new GroupNode("BackGroup");
        _backGroupTranslateTransform = new TranslateTransform();
        _backGroupNode.Transform = _backGroupTranslateTransform;
        scene.RootNode.Add(_backGroupNode);


        // Create MultiLineNodes that will show edge lines
        var edgeLineMaterial = new LineMaterial(Colors.Black)
        {
            LineThickness = 1,
            DepthBias = 0.005f // rise the lines above the 3D object
        };

        _frontEdgeLinesNode = new MultiLineNode(isLineStrip: false, edgeLineMaterial);
        scene.RootNode.Add(_frontEdgeLinesNode);

        _backEdgeLinesNode = new MultiLineNode(isLineStrip: false, edgeLineMaterial);
        scene.RootNode.Add(_backEdgeLinesNode);



        // Read model from obj file
        //string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\Models\\robotarm.obj");

        //var objImporter = new ObjImporter();
        //_importedModel = objImporter.Import(fileName);

        _importedModel = await base.GetCommonSceneAsync(scene, CommonScenes.RobotArm, cacheSceneNode: false);


        // Uncomment to read model from some other file format:
        //var assimpImporter = Importers.AssimpImporterSample.InitAssimpLibrary(scene.GpuDevice, this.BitmapIO, "assimp-lib", showErrorMessageAction: null);

        //if (assimpImporter == null)
        //    return;

        //string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\Models\\planetary-gear.FBX");
        //fileName = Ab4d.SharpEngine.Utilities.FileUtils.FixDirectorySeparator(fileName);

        //_importedModel = assimpImporter.Import(fileName);


        // Test on fixed box models:

        //var box1 = new BoxModelNode("Box1")
        //{
        //    Position = new Vector3(-100, 0, 0),
        //    Size = new Vector3(100, 60, 80),
        //    Material = StandardMaterials.Green,
        //    UseSharedBoxMesh = false
        //};

        //var box2 = new BoxModelNode("Box2")
        //{
        //    Position = new Vector3(0, 0, 0),
        //    Size = new Vector3(100, 60, 80),
        //    Material = StandardMaterials.Blue,
        //    UseSharedBoxMesh = false,
        //    Transform = new TranslateTransform(100, 0, 0)
        //};

        //var box3 = new BoxModelNode("Box3")
        //{
        //    Position = new Vector3(0, -40, 0),
        //    Size = new Vector3(300, 5, 100),
        //    Material = StandardMaterials.Gray,
        //    UseSharedBoxMesh = false,
        //    Transform = new AxisAngleRotateTransform(new Vector3(0, 1, 0), 30)
        //};

        //_importedModel = new GroupNode("RootGroup");
        //_importedModel.Transform = new TranslateTransform(20, 0, 50);
        //_importedModel.Transform = new AxisAngleRotateTransform(new Vector3(0, 0, 1), 30);
        //_importedModel.Add(box1);
        //_importedModel.Add(box2);
        //_importedModel.Add(box3);



        if (_importedModel == null)
            return;

        if (_importedModel.WorldBoundingBox.IsUndefined)
            _importedModel.Update();

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 25;
            targetPositionCamera.Attitude = -15;

            if (!_importedModel.WorldBoundingBox.IsUndefined)
            {
                targetPositionCamera.TargetPosition = _importedModel.WorldBoundingBox.GetCenterPosition();
                targetPositionCamera.Distance = _importedModel.WorldBoundingBox.GetDiagonalLength() * 1.6f;
            }
        }

        UpdateSlicedModel();
    }

    private void UpdateSlicedModel()
    {
        if (_importedModel == null || Scene == null)
            return;


        // Get slice plane
        var plane = _allPlanes[_selectedPlaneIndex];

        // We will update the plane's D value by the slider's _slicePositionPercent value
        
        // First get the size and position of the model in the plane's normal direction
        float modelSizeInNormalDirection = Vector3.Dot(_importedModel.WorldBoundingBox.GetSize(), plane.Normal);
        float modelOffsetInNormalDirection = Vector3.Dot(_importedModel.WorldBoundingBox.GetCenterPosition(), plane.Normal);

        float positionFactor = _slicePositionPercent / 100;
        float actualSlicePosition = modelOffsetInNormalDirection - modelSizeInNormalDirection / 2 + modelSizeInNormalDirection * positionFactor;
        
        plane.D = actualSlicePosition;


        if (_planeRotationAngle != 0)
        {
            var transform = new AxisAngleRotateTransform(new Vector3(plane.Normal.Y, plane.Normal.Z, plane.Normal.X), _planeRotationAngle); // Rotate around rotated plane's normal
            plane = Plane.Transform(plane, transform.Value);
        }

        // ModelUtils.SliceSceneNode slices the _importedModel by the plane.
        // It returns frontSceneNodes and backSceneNodes.
        // We can also call SliceGroupNode or SliceModelNode.
        var(frontSceneNode, backSceneNode) = ModelUtils.SliceSceneNode(plane, _importedModel, parentTransform: null); // we can also remove the parentTransform parameter because its default value is null

        // To slice a mesh, use:
        //(var frontMesh, var backMesh) = MeshUtils.SliceMesh(plane, mesh, parentTransform);


        // Define the separation (gap) between the models
        float slicedModelsSeparation = modelSizeInNormalDirection * 0.1f;

        // Set back material to red so we will easily see the inner parts of the model
        if (frontSceneNode != null && _frontGroupNode != null && _frontGroupTranslateTransform != null)
        {
            // Change back material to red, so we can more easily see inside the model
            Utilities.ModelUtils.ChangeBackMaterial(frontSceneNode, _redBackMaterial);

            // Create a new GroupNode because we need to apply separation transformation (and we do not want to overwrite any existing transform).
            // We could also use Ab4d.SharpEngine.Utilities.TransformationUtils.CombineTransformations, but creating a new GroupNode is cleaner.
            
            _frontGroupNode.Clear();
            _frontGroupNode.Add(frontSceneNode);

            // Move front model for the 10% of the object size in the direction of plane's normal
            _frontGroupTranslateTransform.SetTranslate(plane.Normal.X * slicedModelsSeparation, plane.Normal.Y * slicedModelsSeparation, plane.Normal.Z * slicedModelsSeparation);
        }

        if (backSceneNode != null && _backGroupNode != null && _backGroupTranslateTransform != null)
        {
            Utilities.ModelUtils.ChangeBackMaterial(backSceneNode, _redBackMaterial);
            
            _backGroupNode.Clear();
            _backGroupNode.Add(backSceneNode);

            // Move front model for the 10% of the object size in the opposite direction of plane's normal
            _backGroupTranslateTransform.SetTranslate(plane.Normal.X * -slicedModelsSeparation, plane.Normal.Y * -slicedModelsSeparation, plane.Normal.Z * -slicedModelsSeparation);
        }

        UpdateEdgeLines();

        UpdateVisibility();
    }

    private void UpdateEdgeLines()
    {
        if (Scene == null || !_showEdgeLines) // No need to update edge lines when they are not visible
            return;

        _edgeLinesFactory ??= new EdgeLinesFactory(); // Reuse the EdgeLinesFactory object. This reuses the internal collections.

        if (_frontGroupNode != null && _frontEdgeLinesNode != null)
        {
            // Generate edge lines from the _frontGroupNode
            var edgeLines = _edgeLinesFactory.CreateEdgeLines(_frontGroupNode, edgeStartAngleInDegrees: 25);
            
            // And update the line positions in the MultiLineNode
            _frontEdgeLinesNode.Positions = edgeLines.ToArray();
            //_frontEdgeLinesNode.Transform = _frontGroupNode.Transform; // _frontGroupNode is already transformed; but if we would create edge lines from frontSceneNode, then we would need to apply the transformation from _frontGroupNode
        }

        if (_backGroupNode != null && _backEdgeLinesNode != null)
        {
            // Generate edge lines from the _backGroupNode
            var edgeLines = _edgeLinesFactory.CreateEdgeLines(_backGroupNode, edgeStartAngleInDegrees: 25);
            
            // And update the line positions in the MultiLineNode
            _backEdgeLinesNode.Positions = edgeLines.ToArray();
        }
    }
    
    private void UpdateVisibility()
    {
        if (_frontGroupNode != null)
            _frontGroupNode.Visibility = _showFront ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;

        if (_backGroupNode != null)
            _backGroupNode.Visibility = _showBack ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;

        if (_frontEdgeLinesNode != null)
            _frontEdgeLinesNode.Visibility = (_showFront && _showEdgeLines) ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;

        if (_backEdgeLinesNode != null)
            _backEdgeLinesNode.Visibility = (_showBack && _showEdgeLines) ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);


        ui.CreateLabel("Plane for slicing:");

        var planeDescriptions = _allPlanes.Select(p => $"Normal: ({p.Normal.X}, {p.Normal.Y}, {p.Normal.Z})").ToArray();
        ui.CreateRadioButtons(planeDescriptions, 
                              (selectedIndex, selectedText) =>
                              {
                                  _selectedPlaneIndex = selectedIndex;
                                  UpdateSlicedModel();
                              }, 
                              _selectedPlaneIndex);


        ui.AddSeparator();
        ui.CreateLabel("Slice position:\n(sets plane's D value)");
        ui.CreateSlider(0, 100,
                        () => _slicePositionPercent,
                        newValue =>
                        {
                            if ((int)_slicePositionPercent != (int)newValue)
                            {
                                _slicePositionPercent = newValue;
                                UpdateSlicedModel();
                            }
                        },
                        width: 120,
                        formatShownValueFunc: sliderValue => sliderValue.ToString("F0"));
        
        
        ui.AddSeparator();
        ui.CreateLabel("Plane rotation:");
        ui.CreateSlider(-90, 90,
                        () => _planeRotationAngle,
                        newValue =>
                        {
                            if ((int)_planeRotationAngle != (int)newValue)
                            {
                                _planeRotationAngle = newValue;
                                UpdateSlicedModel();
                            }
                        },
                        width: 120,
                        formatShownValueFunc: sliderValue => sliderValue.ToString("F0"));

        ui.AddSeparator();
        ui.CreateCheckBox("Show front", _showFront, isChecked =>
        {
            _showFront = isChecked;
            UpdateVisibility();
        });
        
        ui.CreateCheckBox("Show back", _showBack, isChecked =>
        {
            _showBack = isChecked;
            UpdateVisibility();
        });

        ui.CreateCheckBox("Show edge lines", _showEdgeLines, isChecked =>
        {
            _showEdgeLines = isChecked;

            if (isChecked)
                UpdateEdgeLines(); // Regenerate edge lines

            UpdateVisibility();
        });
    }
}