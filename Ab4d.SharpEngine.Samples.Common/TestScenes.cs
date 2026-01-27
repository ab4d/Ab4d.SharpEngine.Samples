using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using System.Numerics;
using Ab4d.SharpEngine.Transformations;

namespace Ab4d.SharpEngine.Samples.Common;

public static class TestScenes
{
    public enum StandardTestScenes
    {
        Teapot = 0,
        TeapotLowResolution,
        HouseWithTrees,
        Dragon
    }

    private static string[] _standardTestScenesFileNames = new string[]
    {
        "teapot-hires.obj",
        "Teapot.obj",
        "house with trees.obj",
        "dragon_vrip_res3.obj"
    };

    private static Dictionary<StandardTestScenes, GroupNode> _cachedTestScenes = new();

    public static async Task<GroupNode> GetTestSceneAsync(Scene scene, StandardTestScenes testScene, bool cacheSceneNode = true)
    {
        if (cacheSceneNode && _cachedTestScenes.TryGetValue(testScene, out var cachedGroupNode))
        {
            if (!cachedGroupNode.IsDisposed)
                return cachedGroupNode;
        }


        var testSceneFileName = _standardTestScenesFileNames[(int)testScene];
        string fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\Models", testSceneFileName);

        var objImporter = new ObjImporter(scene);
        var readGroupNode = await objImporter.ImportAsync(fileName);

        if (cacheSceneNode)
            _cachedTestScenes[testScene] = readGroupNode;
        
        return readGroupNode;
    }
    
    public static async Task<GroupNode> GetTestSceneAsync(Scene scene, StandardTestScenes testScene, Vector3 finalSize, bool cacheSceneNode = true)
    {
        return await GetTestSceneAsync(scene, testScene, Vector3.Zero, PositionTypes.Center, finalSize, cacheSceneNode: cacheSceneNode);
    }
    
    public static async Task<GroupNode> GetTestSceneAsync(Scene scene, 
                                                          StandardTestScenes testScene,
                                                          Vector3 position,
                                                          PositionTypes positionType,
                                                          Vector3 finalSize,
                                                          bool cacheSceneNode = true,
                                                          bool preserveAspectRatio = true,
                                                          bool preserveCurrentTransformation = true)
    {
        var importedGroupNode = await GetTestSceneAsync(scene, testScene, cacheSceneNode);

        // When we have custom position and size, we add the loaded scene to a new GroupNode.
        // But first disconnect from any previous GroupNode, if any.
        if (importedGroupNode.Parent != null)
            importedGroupNode.Parent.Remove(importedGroupNode);

        var finalGroupNode = new GroupNode(testScene.ToString());
        finalGroupNode.Add(importedGroupNode);

        ModelUtils.PositionAndScaleSceneNode(finalGroupNode,
                                             position,
                                             positionType,
                                             finalSize,
                                             preserveAspectRatio,
                                             preserveCurrentTransformation);

        return finalGroupNode;
    }

    
    public static void GetTestScene(Scene scene, StandardTestScenes testScene, Action<GroupNode> sceneCreatedCallback, bool cacheSceneNode = true)
    {
        if (cacheSceneNode && _cachedTestScenes.TryGetValue(testScene, out var cachedGroupNode))
        {
            if (!cachedGroupNode.IsDisposed)
            {
                sceneCreatedCallback(cachedGroupNode);
                return;
            }
        }


        var testSceneFileName = _standardTestScenesFileNames[(int)testScene];
        string fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\Models", testSceneFileName);

        var objImporter = new ObjImporter(scene);
        
        _ = objImporter.ImportAsync(fileName)
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                    throw task.Exception;
                 
                var readSceneNode = task.Result;

                if (cacheSceneNode)
                    _cachedTestScenes[testScene] = readSceneNode;

                sceneCreatedCallback(readSceneNode);
            }, TaskScheduler.FromCurrentSynchronizationContext()); // Continue on UI thread
    }

    public static void GetTestScene(Scene scene, StandardTestScenes testScene, Vector3 finalSize, Action<GroupNode> sceneCreatedCallback, bool cacheSceneNode = true)
    {
        GetTestScene(scene, testScene, Vector3.Zero, PositionTypes.Center, finalSize, preserveAspectRatio: true, preserveCurrentTransformation: true, sceneCreatedCallback, cacheSceneNode: cacheSceneNode);
    }

    public static void GetTestScene(Scene scene,
                                    StandardTestScenes testScene,
                                    Vector3 position,
                                    PositionTypes positionType,
                                    Vector3 finalSize,
                                    Action<GroupNode> sceneCreatedCallback, 
                                    bool cacheSceneNode = true)
    {
        GetTestScene(scene, testScene, position, positionType, finalSize, preserveAspectRatio: true, preserveCurrentTransformation: true, sceneCreatedCallback, cacheSceneNode: cacheSceneNode);
    }

    public static void GetTestScene(Scene scene, 
                                    StandardTestScenes testScene,
                                    Vector3 position,
                                    PositionTypes positionType,
                                    Vector3 finalSize,
                                    bool preserveAspectRatio,
                                    bool preserveCurrentTransformation, 
                                    Action<GroupNode> sceneCreatedCallback,
                                    bool cacheSceneNode = true)
    {
        GetTestScene(scene, testScene, (importedGroupNode) =>
        {
            // When we have custom position and size, we add the loaded scene to a new GroupNode.
            // But first disconnect from any previous GroupNode, if any.
            if (importedGroupNode.Parent != null)
                importedGroupNode.Parent.Remove(importedGroupNode);

            var finalGroupNode = new GroupNode(testScene.ToString());
            finalGroupNode.Add(importedGroupNode);

            ModelUtils.PositionAndScaleSceneNode(finalGroupNode,
                                                 position,
                                                 positionType,
                                                 finalSize,
                                                 preserveAspectRatio,
                                                 preserveCurrentTransformation);

            sceneCreatedCallback(finalGroupNode);
        }, cacheSceneNode: cacheSceneNode);
    }

    [Obsolete("Use GetTestScene with Scene and callback or async version")]
    public static GroupNode GetTestScene(StandardTestScenes testScene)
    {
        var testSceneFileName = _standardTestScenesFileNames[(int)testScene];

        string fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\Models", testSceneFileName);

#if VULKAN
        var objImporter = new ObjImporter();
        var readGroupNode = objImporter.Import(fileName);
#else
        var readGroupNode = new GroupNode();
        throw new NotSupportedException();
#endif

        return readGroupNode;
    }

    [Obsolete("Use GetTestScene with Scene and callback or async version")]
    public static GroupNode GetTestScene(StandardTestScenes testScene, Vector3 finalSize)
    {
        return GetTestScene(testScene, Vector3.Zero, PositionTypes.Center, finalSize);
    }
    
    [Obsolete("Use GetTestScene with Scene and callback or async version")]
    public static GroupNode GetTestScene(StandardTestScenes testScene,
                                         Vector3 position,
                                         PositionTypes positionType,
                                         Vector3 finalSize,
                                         bool preserveAspectRatio = true,
                                         bool preserveCurrentTransformation = true)
    {
        var readGroupNode = GetTestScene(testScene);

        ModelUtils.PositionAndScaleSceneNode(readGroupNode,
                                             position,
                                             positionType,
                                             finalSize,
                                             preserveAspectRatio,
                                             preserveCurrentTransformation);

        return readGroupNode;
    }


    public static void GetTestMesh(Scene scene, StandardTestScenes testScene, Vector3 finalSize, Action<StandardMesh> meshCreatedCallback, bool cacheSceneNode = true)
    {
        GetTestMesh(scene, testScene, Vector3.Zero, PositionTypes.Center, finalSize, meshCreatedCallback, cacheSceneNode: cacheSceneNode, preserveAspectRatio: true);
    }

    public static void GetTestMesh(Scene scene,
                                   StandardTestScenes testScene,
                                   Vector3 position,
                                   PositionTypes positionType,
                                   Vector3 finalSize,
                                   Action<StandardMesh> meshCreatedCallback,
                                   bool cacheSceneNode = true,
                                   bool preserveAspectRatio = true)
    {
        GetTestScene(scene, testScene, sceneCreatedCallback: readGroupNode =>
        {
            if (readGroupNode.Count > 0 && readGroupNode[0] is MeshModelNode singeMeshModelNode)
            {
                if (singeMeshModelNode.Mesh is StandardMesh standardMesh)
                {
                    var (translateVector, scaleVector) = ModelUtils.GetPositionAndScaleTransform(standardMesh.BoundingBox,
                                                                                                 position,
                                                                                                 positionType,
                                                                                                 finalSize,
                                                                                                 preserveAspectRatio);

                    var transformGroup = new TransformGroup();
                    transformGroup.Add(new ScaleTransform(scaleVector));
                    transformGroup.Add(new TranslateTransform(translateVector));

                    var transformedMesh = MeshUtils.TransformMesh(standardMesh, transformGroup);
                    meshCreatedCallback(transformedMesh);
                }
            }

            throw new Exception("Cannot get single mesh from " + testScene.ToString());
        }, cacheSceneNode: cacheSceneNode);
    }

    
    public static async Task<StandardMesh> GetTestMeshAsync(Scene scene, StandardTestScenes testScene, Vector3 finalSize, bool cacheSceneNode = true)
    {
        return await GetTestMeshAsync(scene, testScene, Vector3.Zero, PositionTypes.Center, finalSize, cacheSceneNode: cacheSceneNode, preserveAspectRatio: true);
    }

    public static async Task<StandardMesh> GetTestMeshAsync(Scene scene,
                                                            StandardTestScenes testScene,
                                                            Vector3 position,
                                                            PositionTypes positionType,
                                                            Vector3 finalSize,
                                                            bool cacheSceneNode = true,
                                                            bool preserveAspectRatio = true)
    {
        var readGroupNode = await GetTestSceneAsync(scene, testScene, cacheSceneNode: cacheSceneNode);

        if (readGroupNode.Count > 0 && readGroupNode[0] is MeshModelNode singeMeshModelNode)
        {
            if (singeMeshModelNode.Mesh is StandardMesh standardMesh)
            {
                var (translateVector, scaleVector) = ModelUtils.GetPositionAndScaleTransform(standardMesh.BoundingBox,
                                                                                             position,
                                                                                             positionType,
                                                                                             finalSize,
                                                                                             preserveAspectRatio);

                var transformGroup = new TransformGroup();
                transformGroup.Add(new ScaleTransform(scaleVector));
                transformGroup.Add(new TranslateTransform(translateVector));

                var transformedMesh = MeshUtils.TransformMesh(standardMesh, transformGroup);
                return transformedMesh;
            }
        }

        throw new Exception("Cannot get single mesh from " + testScene.ToString());
    }


    [Obsolete("Use GetTestMesh with Scene and callback or async version")]
    public static StandardMesh GetTestMesh(StandardTestScenes testScene, Vector3 finalSize)
    {
        return GetTestMesh(testScene, Vector3.Zero, PositionTypes.Center, finalSize);
    }

    [Obsolete("Use GetTestMesh with Scene and callback or async version")]
    public static StandardMesh GetTestMesh(StandardTestScenes testScene,
                                           Vector3 position,
                                           PositionTypes positionType,
                                           Vector3 finalSize,
                                           bool preserveAspectRatio = true,
                                           bool preserveCurrentTransformation = true)
    {
        var readGroupNode = GetTestScene(testScene);

        if (readGroupNode.Count > 0 && readGroupNode[0] is MeshModelNode singeMeshModelNode)
        {
            if (singeMeshModelNode.Mesh is StandardMesh standardMesh)
            {
                ModelUtils.PositionAndScaleSceneNode(singeMeshModelNode,
                                                     position,
                                                     positionType,
                                                     finalSize,
                                                     preserveAspectRatio,
                                                     preserveCurrentTransformation);

                Ab4d.SharpEngine.Utilities.ModelUtils.PositionAndScaleSceneNode(singeMeshModelNode, position, positionType, finalSize);
                standardMesh = Ab4d.SharpEngine.Utilities.MeshUtils.TransformMesh(standardMesh, singeMeshModelNode.Transform);

                return standardMesh;
            }
        }

        throw new Exception("Cannot get single mesh from " + testScene.ToString());
    }
}