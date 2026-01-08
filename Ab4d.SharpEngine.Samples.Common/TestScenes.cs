using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using System.Numerics;
using Ab4d.SharpEngine.Meshes;

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

    public static async Task<GroupNode> GetTestSceneAsync(Scene scene, StandardTestScenes testScene)
    {
        var testSceneFileName = _standardTestScenesFileNames[(int)testScene];

        string fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\Models", testSceneFileName);

#if VULKAN
        var objImporter = new ObjImporter();
        var readGroupNode = await Task.Run(() => objImporter.Import(fileName));
#else
        var objImporter = new ObjImporter(scene);
        var readGroupNode = await objImporter.ImportAsync(fileName);
#endif

        return readGroupNode;
    }
    
    public static async Task<GroupNode> GetTestSceneAsync(Scene scene, StandardTestScenes testScene, Vector3 finalSize)
    {
        return await GetTestSceneAsync(scene, testScene, Vector3.Zero, PositionTypes.Center, finalSize);
    }
    
    public static async Task<GroupNode> GetTestSceneAsync(Scene scene, 
                                                          StandardTestScenes testScene,
                                                          Vector3 position,
                                                          PositionTypes positionType,
                                                          Vector3 finalSize,
                                                          bool preserveAspectRatio = true,
                                                          bool preserveCurrentTransformation = true)
    {
        var readGroupNode = await GetTestSceneAsync(scene, testScene);

        ModelUtils.PositionAndScaleSceneNode(readGroupNode,
                                             position,
                                             positionType,
                                             finalSize,
                                             preserveAspectRatio,
                                             preserveCurrentTransformation);

        return readGroupNode;
    }

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

    public static GroupNode GetTestScene(StandardTestScenes testScene, Vector3 finalSize)
    {
        return GetTestScene(testScene, Vector3.Zero, PositionTypes.Center, finalSize);
    }
    
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

    public static StandardMesh GetTestMesh(StandardTestScenes testScene, Vector3 finalSize)
    {
        return GetTestMesh(testScene, Vector3.Zero, PositionTypes.Center, finalSize);
    }


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
            if (singeMeshModelNode.Mesh is StandardMesh teapotMesh)
            {
                ModelUtils.PositionAndScaleSceneNode(singeMeshModelNode,
                                                     position,
                                                     positionType,
                                                     finalSize,
                                                     preserveAspectRatio,
                                                     preserveCurrentTransformation);

                Ab4d.SharpEngine.Utilities.ModelUtils.PositionAndScaleSceneNode(singeMeshModelNode, position, positionType, finalSize);
                teapotMesh = Ab4d.SharpEngine.Utilities.MeshUtils.TransformMesh(teapotMesh, singeMeshModelNode.Transform);

                return teapotMesh;
            }
        }

        throw new Exception("Cannot get single mesh from " + testScene.ToString());
    }
}