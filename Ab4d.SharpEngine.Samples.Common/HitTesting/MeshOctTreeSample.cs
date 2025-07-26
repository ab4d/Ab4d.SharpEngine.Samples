using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.HitTesting;

// OctTree is a data structure that organizes the triangles in 3D space into multiple levels of
// OctTreeNode objects so that the search of a triangle or a check for triangle ray intersection is very efficient.
// Each OctTreeNodes divide its space into 8 child OctTreeNodes.
// See also: https://en.wikipedia.org/wiki/Octree
//
// OctTrees are used in SharpEngine for very efficient hit testing of complex meshes.
//
// OctTree generation for hit testing (when calling GetClosestHitObject or GetAllHitObjects methods) 
// is controlled by HitTestOptions.MeshPositionsCountForOctTreeGeneration property.
// This property gets or sets an integer value that specifies number of positions in a mesh
// at which an OctTree is generated to speed up hit testing
// (e.g. if mesh has more positions then a value specified with this property,
// then OctTree will be generated for the mesh). Default value is 512.
//
// This sample shows how to manually create an OctTree (this can be also done in background thread).

public class MeshOctTreeSample : CommonSample
{
    public override string Title => "Mesh OctTree sample";

    private const int MaxNodeLevels = 4; // This should be determined by the number of triangles (more triangles bigger max level)

    private GroupNode _octTreeLinesGroupNode;
    private GroupNode _hitLinesGroupNode;

    private MeshOctTree? _meshOctTree;
    private StandardMesh? _teapotMesh;

    private bool _expandChildBoundingBoxes = true;
    private bool _showActualBoundingBox = true;

    public MeshOctTreeSample(ICommonSamplesContext context)
        : base(context)
    {
        _octTreeLinesGroupNode = new GroupNode("OctTeeLines");
        _hitLinesGroupNode     = new GroupNode("HitLines");
    }

    protected override void OnCreateScene(Scene scene)
    {
        scene.RootNode.Add(_octTreeLinesGroupNode);

        scene.RootNode.Add(_hitLinesGroupNode);

        _teapotMesh = TestScenes.GetTestMesh(TestScenes.StandardTestScenes.Teapot, finalSize: new Vector3(100, 100, 100));

        RecreateOctTree();

        var meshModelNode = new MeshModelNode(_teapotMesh, StandardMaterials.Silver);
        scene.RootNode.Add(meshModelNode);

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading  = 30;
            targetPositionCamera.Attitude = -20;
            targetPositionCamera.Distance = 200;
        }
    }

    private void RecreateOctTree()
    {
        if (_teapotMesh == null)
            return;

        float expandChildBoundingBoxes = _expandChildBoundingBoxes ? 0.2f : 0f;

        _meshOctTree = _teapotMesh.CreateOctTree(MaxNodeLevels, expandChildBoundingBoxes);

        ShowBoundingBoxes();

        var nodeStatistics = _meshOctTree.GetNodeStatistics();

        //ResultTextBox.Text = "";
        //AddMessage("MeshOctTree nodes statistics:\r\n" + nodeStatistics);
    }

    private void ShowBoundingBoxes()
    {
        if (_meshOctTree == null || Scene == null)
            return;

        _octTreeLinesGroupNode.Clear();

        var colors = new Color4[] { Colors.Red, Colors.Green, Colors.Blue, Colors.Black };

        int startNodeLevel = 2;
        for (int i = startNodeLevel; i <= MaxNodeLevels; i++)
        {
            var boundingBoxes = _meshOctTree.CollectBoundingBoxesInLevel(i, _showActualBoundingBox);

            foreach (var boundingBox in boundingBoxes)
            {
                var wireBoxNode = new WireBoxNode()
                {
                    Position      = boundingBox.GetCenterPosition(),
                    Size          = boundingBox.GetSize(),
                    LineColor     = colors[(i - startNodeLevel) % (colors.Length)],
                    LineThickness = 2
                };

                Scene.RootNode.Add(wireBoxNode);
            }

        }
    }
}