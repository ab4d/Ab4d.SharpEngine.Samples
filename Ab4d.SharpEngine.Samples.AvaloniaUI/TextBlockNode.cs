//using Ab4d.SharpEngine.Common;
//using Ab4d.SharpEngine.Meshes;
//using Ab4d.SharpEngine.Utilities;
//using Ab4d.SharpEngine.Vulkan;
//using System.Numerics;
//using Ab4d.SharpEngine.Transformations;

//namespace Ab4d.SharpEngine.SceneNodes;

//public class TextBlockNode : ModelNode
//{
//    public string? Text { get; set; }

//    /// <summary>
//    /// Constructor
//    /// </summary>
//    public TextBlockNode()
//        : this(null)
//    {
//    }

//    /// <summary>
//    /// Constructor
//    /// </summary>
//    /// <param name="name">optional name</param>
//    public TextBlockNode(string? name)
//        : base(name)
//    {
//    }

//    /// <inheritdoc />
//    protected override void OnInitializeSceneResources(Scene scene, VulkanDevice gpuDevice)
//    {
//        var mesh = GetMesh();
//        if (mesh == null)
//            UpdateMesh(); // This is needed in case UseShared1x1x1BoxMesh because in this case the mesh cannot be created before Scene is set

//        base.OnInitializeSceneResources(scene, gpuDevice);
//    }

//    /// <inheritdoc />
//    protected override void UpdateMesh()
//    {
//        // Set null for mesh in case of NaN in _centerPosition or _size
//        if (!MathUtils.IsFinite(_position) ||
//            !MathUtils.IsFinite(_size))
//        {
//            ClearMesh();
//            return;
//        }


//        // Get center position from Position and PositionType
//        Vector3 centerPosition;
//        if (_positionType == PositionTypes.Center)
//            centerPosition = _position;
//        else
//            centerPosition = Utilities.MathUtils.GetCenterPosition(_position, _positionType, _size, sizeXDirection: new Vector3(1, 0, 0), sizeYDirection: new Vector3(0, 1, 0));


//        if (UseSharedBoxMesh && _xSegmentsCount == 1 && _ySegmentsCount == 1 && _zSegmentsCount == 1)
//        {
//            if (Scene == null)
//                return; // We cannot get a shared mesh before Scene is set; UpdateMesh is will be called from OnInitializeSceneResources


//            // Because shared box mesh has center at (0, 0, 0) and size (1, 1, 1)
//            // we need to set matrixTransform to transform that mesh to its final position and size.
//            var meshTransformMatrix = new Matrix4x4(_size.X, 0, 0, 0,
//                                                    0, _size.Y, 0, 0,
//                                                    0, 0, _size.Z, 0,
//                                                    centerPosition.X, centerPosition.Y, centerPosition.Z, 1);


//            var currentMesh = GetMesh();
//            var sharedMesh = MeshFactory.GetSharedBoxMesh(Scene);

//            if (ReferenceEquals(currentMesh, sharedMesh))
//            {
//                // Shared mesh already used, just update meshTransformMatrix
//                SetMeshTransform(meshTransformMatrix);
//                ClearMeshDirtyFlag();
//            }
//            else
//            {
//                // Set mesh to shared mesh and use meshTransformMatrix
//                var newMeshTransform = new MatrixTransform(ref meshTransformMatrix);
//                SetMesh(sharedMesh, isMeshCreatedHere: false, newMeshTransform, notifyChange: true, clearMeshDirtyFlag: true); // set isMeshCreatedHere to false to prevent disposing a shared mesh
//            }
//        }
//        else
//        {
//            string? meshName = string.IsNullOrEmpty(this.Name) ? null : this.Name + "-mesh";
//            var newMesh = MeshFactory.CreateBoxMesh(centerPosition, _size, _xSegmentsCount, _ySegmentsCount, _zSegmentsCount, name: meshName);

//            // No mesh transform is needed because the mesh positions are already set for the specified box position and size
//            SetMesh(newMesh, isMeshCreatedHere: true, meshTransform: null, notifyChange: true, clearMeshDirtyFlag: true);
//        }
//    }
//}