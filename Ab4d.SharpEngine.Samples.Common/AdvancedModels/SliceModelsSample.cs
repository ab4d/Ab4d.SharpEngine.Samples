using System.Numerics;
using Ab4d.SharpEngine.Assimp;
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

    private float _slicePositionPercent = 70;

    public SliceModelsSample(ICommonSamplesContext context)
        : base(context)
    {
        _redBackMaterial = StandardMaterials.Red;

        ShowCameraAxisPanel = true;
    }

    protected override void OnCreateScene(Scene scene)
    {
        // Read model from obj file
        string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\Models\\robotarm.obj");

        var readerObj = new ReaderObj();
        _importedModel = readerObj.ReadSceneNodes(fileName);


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


        // Test on sphere model

        //var sphere = new SphereModelNode()
        //{
        //    Radius = 100,
        //    Material = StandardMaterials.Blue,
        //    //UseSharedSphereMesh = false
        //};

        //_importedModel = new GroupNode("RootGroup");
        //_importedModel.Add(sphere);


        if (_importedModel == null)
            return;

        scene.RootNode.Add(_importedModel);


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

        Scene.RootNode.Clear();

        // Define the Plane that will be used to slice the model
        var modelWidth = _importedModel.WorldBoundingBox.SizeX;
        var actualSlicePosition = modelWidth * _slicePositionPercent / 100 - modelWidth / 2 + _importedModel.WorldBoundingBox.GetCenterPosition().X;

        var plane = new Plane(normal: new Vector3(1, 0, 0), actualSlicePosition);

        // ModelUtils.SliceSceneNode slices the _importedModel by the plane.
        // It returns frontSceneNodes and backSceneNodes.
        (var frontSceneNodes, var backSceneNodes) = ModelUtils.SliceSceneNode(plane, _importedModel);


        // Define the separation (gap) between the models
        float slicedModelsSeparation = modelWidth * 0.1f;

        // Set back material to red so we will easily see the inner parts of the model
        if (frontSceneNodes != null)
        {
            // Change back material to red, so we can more easily see inside the model
            Utilities.ModelUtils.ChangeBackMaterial(frontSceneNodes, _redBackMaterial);

            // Create a new GroupNode because we need to apply separation transformation (and we do not want to overwrite any existing transform).
            // We could also use Ab4d.SharpEngine.Utilities.TransformationUtils.CombineTransformations, but creating a new GroupNode is cleaner.
            
            var frontGroupNode = new GroupNode("SeparatedFrontGroup");
            frontGroupNode.Transform = new TranslateTransform(plane.Normal.X * slicedModelsSeparation, plane.Normal.Y * slicedModelsSeparation, plane.Normal.Z * slicedModelsSeparation);

            frontGroupNode.Add(frontSceneNodes);

            Scene.RootNode.Add(frontGroupNode);
        }

        if (backSceneNodes != null)
        {
            Utilities.ModelUtils.ChangeBackMaterial(backSceneNodes, _redBackMaterial);
            
            var backGroupNode = new GroupNode("SeparatedBackGroup");
            backGroupNode.Transform = new TranslateTransform(plane.Normal.X * -slicedModelsSeparation, plane.Normal.Y * -slicedModelsSeparation, plane.Normal.Z * -slicedModelsSeparation);

            backGroupNode.Add(backSceneNodes);

            Scene.RootNode.Add(backGroupNode);
        }
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

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
                        keyText: "Slice position:",
                        keyTextWidth: 100,
                        formatShownValueFunc: sliderValue => sliderValue.ToString("F0"));
    }
}