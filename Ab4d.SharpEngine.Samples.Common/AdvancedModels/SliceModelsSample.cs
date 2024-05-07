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
        // This method is defined in the class below and will be part of Ab4d.SharpEngine v1.1
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


    public static class ModelUtils
    {
        // The Ab4d.ShareEngine v1.0 supports only slicing of StandardMesh.
        // The following methods add support for slicing other SceneNodes.
        // Those methods will be added to the Ab4d.SharpEngine.Utilities.ModelUtils in v1.1:

        public static (SceneNode? frontSceneNodes, SceneNode? backSceneNodes) SliceSceneNode(Plane plane, SceneNode sceneNode)
        {
            return SliceSceneNode(plane, sceneNode, parentTransform: null);
        }

        public static (SceneNode? frontSceneNodes, SceneNode? backSceneNodes) SliceSceneNode(Plane plane, SceneNode sceneNode, Transform? parentTransform)
        {
            if (sceneNode == null) 
                throw new ArgumentNullException(nameof(sceneNode));

            SceneNode? frontSceneNodes, backSceneNodes;

            if (sceneNode is GroupNode groupNode)
            {
                (frontSceneNodes, backSceneNodes) = SliceGroupNode(plane, groupNode, parentTransform);
            }
            else if (sceneNode is ModelNode modelNode)
            {
                (frontSceneNodes, backSceneNodes) = SliceModelNode(plane, modelNode, parentTransform);
            }
            else
            {
                // Unsupported SceneNode type
                frontSceneNodes = null;
                backSceneNodes = null;
            }

            return (frontSceneNodes, backSceneNodes);
        }

        public static (GroupNode? frontGroupNode, GroupNode? backGroupNode) SliceGroupNode(Plane plane, GroupNode groupNode)
        {
            return SliceGroupNode(plane, groupNode, parentTransform: null);
        }

        public static (GroupNode? frontGroupNode, GroupNode? backGroupNode) SliceGroupNode(Plane plane, GroupNode groupNode, Transform? parentTransform)
        {
            if (groupNode == null) 
                throw new ArgumentNullException(nameof(groupNode));


            var thisTransform = groupNode.Transform;

            var frontGroupNode = new GroupNode();
            frontGroupNode.Transform = thisTransform;

            var backGroupNode = new GroupNode();
            backGroupNode.Transform = thisTransform;

#if DEBUG
            if (!string.IsNullOrEmpty(groupNode.Name))
            {
                frontGroupNode.Name = groupNode.Name + "_front";
                backGroupNode.Name = groupNode.Name + "_back";
            }
#endif


            // Combine parentTransform and groupNode.Transform
            Transform? transform = parentTransform;

            if (thisTransform != null && !thisTransform.Value.IsIdentity)
            {
                // Combine parentTransform with transform defined on this GroupNode
                if (transform == null || transform.Value.IsIdentity)
                    transform = thisTransform;
                else
                    transform = new MatrixTransform(thisTransform.Value * transform.Value);
            }


            foreach (var childNode in groupNode)
            {
                (var frontSceneNode, var backSceneNode) = SliceSceneNode(plane, childNode, transform);

                if (frontSceneNode != null)
                    frontGroupNode.Add(frontSceneNode);

                if (backGroupNode != null && backSceneNode != null)
                    backGroupNode.Add(backSceneNode);
            }

            if (frontGroupNode.Count == 0)
                frontGroupNode = null;

            if (backGroupNode != null && backGroupNode.Count == 0)
                backGroupNode = null;

            return (frontGroupNode, backGroupNode);
        }

        public static (MeshModelNode? frontModelNode, MeshModelNode? backModelNode) SliceModelNode(Plane plane, ModelNode modelNode)
        {
            return SliceModelNode(plane, modelNode, parentTransform: null);
        }

        public static (MeshModelNode? frontModelNode, MeshModelNode? backModelNode) SliceModelNode(Plane plane, ModelNode modelNode, Transform? parentTransform)
        {
            if (modelNode == null) 
                throw new ArgumentNullException(nameof(modelNode));


            var standardMesh = modelNode.GetMesh() as StandardMesh; // Only StandardMesh is supported

            if (standardMesh == null)
                return (null, null);


            // Combine parentTransform and groupNode.Transform
            Transform? transform = parentTransform;

            var meshTransform = modelNode.GetMeshTransform();
            if (meshTransform != null)
            {
                // Combine parentTransform with transform defined on this ModelNode
                if (transform == null || transform.Value.IsIdentity)
                    transform = meshTransform;
                else
                    transform = new MatrixTransform(meshTransform.Value * transform.Value);
            }

            var thisTransform = modelNode.Transform;

            if (thisTransform != null && !thisTransform.Value.IsIdentity)
            {
                // Combine parentTransform with transform defined on this ModelNode
                if (transform == null || transform.Value.IsIdentity)
                    transform = thisTransform;
                else
                    transform = new MatrixTransform(thisTransform.Value * transform.Value);

                // Add meshTransform to thisTransform
                if (meshTransform != null)
                    thisTransform = new MatrixTransform(meshTransform.Value * thisTransform.Value);
            }
            else
            {
                if (meshTransform != null)
                    thisTransform = meshTransform; // Add meshTransform to thisTransform
            }


            // SliceMesh does not correctly use the plane.D, so we need to multiply its value by -1 here.
            // This will be fixed in v1.1
            var fixedPlane = new Plane(plane.Normal, -plane.D);


            // Slice the standardMesh by the plane into frontMesh and backMesh
            (var frontMesh, var backMesh) = Ab4d.SharpEngine.Utilities.MeshUtils.SliceMesh(fixedPlane, standardMesh, transform);


            MeshModelNode? frontModelNode, backModelNode;

            if (frontMesh != null)
            {
                // In v1.0, the SliceMesh does not correctly calculate normals and texture coordinates.
                // A workaround is to manually calculate Normals here. This will be also fixed in v1.1
                if (frontMesh.Vertices != null && frontMesh.TriangleIndices != null)
                    MeshUtils.CalculateNormals(frontMesh.Vertices, frontMesh.TriangleIndices);

                frontModelNode = new MeshModelNode(frontMesh, modelNode.Material);
                if (frontModelNode.BackMaterial != null)
                    frontModelNode.BackMaterial = modelNode.BackMaterial;

                frontModelNode.Transform = thisTransform;

#if DEBUG
                if (!string.IsNullOrEmpty(modelNode.Name))
                    frontModelNode.Name = modelNode.Name + "_front";
#endif
            }
            else
            {
                frontModelNode = null;
            }

            if (backMesh != null)
            {
                // In v1.0, the SliceMesh does not correctly calculate normals and texture coordinates.
                // A workaround is to manually calculate Normals here. This will be also fixed in v1.1
                if (backMesh.Vertices != null && backMesh.TriangleIndices != null)
                    MeshUtils.CalculateNormals(backMesh.Vertices, backMesh.TriangleIndices);

                backModelNode = new MeshModelNode(backMesh, modelNode.Material);
                if (modelNode.BackMaterial != null)
                    backModelNode.BackMaterial = modelNode.BackMaterial;

                backModelNode.Transform = thisTransform;

#if DEBUG
                if (!string.IsNullOrEmpty(modelNode.Name))
                    backModelNode.Name = modelNode.Name + "_back";
#endif
            }
            else
            {
                backModelNode = null;
            }
            
            return (frontModelNode, backModelNode);
        }
    }
}