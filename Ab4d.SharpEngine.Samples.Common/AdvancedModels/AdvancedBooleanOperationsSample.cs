using System.Data.SqlTypes;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.Samples.Common.Utils;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class AdvancedBooleanOperationsSample : CommonSample
{
    public override string Title => "Advanced Boolean operations processing options";

    private GroupNode? _wireframeGroup;

    private bool _generateInnerTriangles = true;

    public AdvancedBooleanOperationsSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        CreateTestScene(scene);

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 20;
            targetPositionCamera.Attitude = -35;
            targetPositionCamera.Distance = 700;
        }
    }

    private void CreateTestScene(Scene scene)
    { 
        _wireframeGroup = new GroupNode("WireframeGroup");
        scene.RootNode.Add(_wireframeGroup);


        var boxMesh = MeshFactory.CreateBoxMesh(centerPosition: new Vector3(0, 0, 0), size: new Vector3(200, 10, 200), xSegments: 10, ySegments: 1, zSegments: 10);

        // NOTE:
        // When doing the boolean operations it is very good to keep the meshes as simple as possible.
        // Here we use quite complex meshes because segments parameter is set to 30. This better demonstrates the use of processOnlyIntersectingTriangles parameter.
        // For a real world scenarios I would recommend to use lower segment values.
        var cylinder1Mesh = MeshFactory.CreateCylinderMesh(new Vector3(-50, -50, 90), radius: 8, height: 100, segments: 30, isSmooth: true);
        var cylinder2Mesh = MeshFactory.CreateCylinderMesh(new Vector3(-20, -50, 90), radius: 8, height: 100, segments: 30, isSmooth: true);

        // It is recommended to combine the cylinder meshes and then do one subtraction.
        var combinedMesh = Utilities.MeshUtils.CombineMeshes(cylinder1Mesh, cylinder2Mesh);


        // Process mesh subtraction only on triangles that intersect the combinedMesh.
        // Because in our case the combinedMesh is much smaller then boxMesh, this produces significantly less triangles.
        //
        // But when you know that most of the triangles in the meshes would intersect, then
        // it is worth setting processOnlyIntersectingTriangles to false to skip getting intersecting triangles.

        var subtractedMesh1 = Ab4d.SharpEngine.Utilities.MeshBooleanOperations.Subtract(boxMesh, combinedMesh, processOnlyIntersectingTriangles: true, _generateInnerTriangles);
        ShowMesh(scene, subtractedMesh1, -120);

        var subtractedMesh2 = Ab4d.SharpEngine.Utilities.MeshBooleanOperations.Subtract(boxMesh, combinedMesh, processOnlyIntersectingTriangles: false, _generateInnerTriangles);
        ShowMesh(scene, subtractedMesh2, 120);


        var textBlockFactory = context.GetTextBlockFactory();

        textBlockFactory.FontSize = 10;
        textBlockFactory.BackgroundColor = Colors.LightYellow;
        textBlockFactory.BorderThickness = 1;
        textBlockFactory.BorderColor = Colors.DimGray;

        var textNode = textBlockFactory.CreateTextBlock(new Vector3(-120, 0, 150),
                                                        $"processOnlyIntersectingTriangles: true\r\nFinal triangles count: {subtractedMesh1!.TriangleIndices!.Length / 3}",
                                                        textAttitude: 30);
        scene.RootNode.Add(textNode);

        textNode = textBlockFactory.CreateTextBlock(new Vector3(120, 0, 150),
                                                    $"processOnlyIntersectingTriangles: false\r\nFinal triangles count: {subtractedMesh2!.TriangleIndices!.Length / 3}",
                                                    textAttitude: 30);
        scene.RootNode.Add(textNode);


        // Deep dive:
        // When processOnlyIntersectingTriangles is set to true, then behind the scenes the 
        // MeshUtils.GetIntersectingTriangles, MeshUtils.SplitMeshByIndexesOfTriangles and MeshUtils.CombineMeshes methods
        // are used to split the original mesh into two meshes.
        // 
        // If you want to do that manually, here is the code from Subtract method:
        // (instead of SubtractInt method you can call Subtract method with processOnlyIntersectingTriangles set to false)
        //
        //public static MeshGeometry3D Subtract(MeshGeometry3D mesh1, MeshGeometry3D mesh2, bool processOnlyIntersectingTriangles = true)
        //{
        //    if (mesh1 == null)
        //        return null;

        //    if (mesh2 == null)
        //        return mesh1;


        //    MeshGeometry3D resultMesh;

        //    if (processOnlyIntersectingTriangles)
        //    {
        //        int originalTrianglesCount = mesh1.TriangleIndices.Count / 3;

        //        var intersectingTriangles = MeshUtils.GetIntersectingTriangles(mesh2.Bounds, mesh1, meshTransform: null);

        //        if (intersectingTriangles == null || intersectingTriangles.Count == 0)
        //            return mesh1; // No intersection - preserve the mesh1


        //        if (intersectingTriangles.Count > originalTrianglesCount * 0.8) // When more than 80 percent of triangles is inside the mesh2, then just use the original mesh1
        //        {
        //            resultMesh = SubtractInt(mesh1, mesh2);
        //        }
        //        else
        //        {
        //            MeshGeometry3D splitMesh1, splitMesh2;
        //            MeshUtils.SplitMeshByIndexesOfTriangles(mesh1, intersectingTriangles, false, out splitMesh1, out splitMesh2);

        //            var processedMesh = SubtractInt(splitMesh1, mesh2);

        //            if (processedMesh == null)
        //            {
        //                resultMesh = splitMesh2;
        //            }
        //            else
        //            {
        //                // Combine triangles that were not processed (not intersecting with mesh2) with the result of subtraction
        //                resultMesh = Ab3d.Utilities.MeshUtils.CombineMeshes(splitMesh2, processedMesh);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        if (!mesh2.Bounds.IntersectsWith(mesh1.Bounds))
        //        {
        //            // In case there is no intersection, then just return the original mesh1
        //            resultMesh = mesh1;
        //        }
        //        else
        //        {
        //            // Process whole meshes
        //            resultMesh = SubtractInt(mesh1, mesh2);
        //        }
        //    }

        //    return resultMesh;
        //}



        var wireGridNode = new WireGridNode()
        {
            CenterPosition = new Vector3(0, -15, 0),
            Size = new Vector2(600, 280),
            WidthCellsCount = 42,
            HeightCellsCount = 24,
            MajorLineColor = Colors.DimGray,
            MajorLineThickness = 1
        };

        scene.RootNode.Add(wireGridNode);


        var planeModelNode = new PlaneModelNode()
        {
            Position = new Vector3(0, -15.1f, 0),
            PositionType = PositionTypes.Center,
            Size = new Vector2(600, 280),
            Material = StandardMaterials.LightGray,
            BackMaterial = StandardMaterials.LightGray,
        };

        scene.RootNode.Add(planeModelNode);
    }

    private void ShowMesh(Scene scene, StandardMesh? mesh, float xOffset)
    {
        if (mesh == null)
            return;

        var meshModelNode = new MeshModelNode(mesh)
        {
            Material = StandardMaterials.Gold,
            BackMaterial = StandardMaterials.Black,
            Transform = new TranslateTransform(x: xOffset)
        };

        scene.RootNode.Add(meshModelNode);


        if (_wireframeGroup != null)
        {
            var wireframePositions = LineUtils.GetWireframeLinePositions(mesh, removedDuplicateLines: false); // do not remove duplicate lines because this may take long time when the mesh complex

            var lineMaterial = new LineMaterial(Color3.Black, lineThickness: 0.7f)
            {
                DepthBias = 0.002f
            };

            var wireframeLineNode = new MultiLineNode(wireframePositions, isLineStrip: false, lineMaterial, "WireframeLines")
            {
                Transform = meshModelNode.Transform,
            };

            _wireframeGroup.Add(wireframeLineNode);
        }
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateCheckBox("GenerateInnerTriangles", true, isChecked =>
            {
                _generateInnerTriangles = isChecked;

                if (Scene != null)
                {
                    Scene.RootNode.Clear();
                    CreateTestScene(Scene);
                }
            }).SetToolTip("When checked, then subtraction also generates the inner triangles. This closes the hole that is created by subtraction.");
    }
}