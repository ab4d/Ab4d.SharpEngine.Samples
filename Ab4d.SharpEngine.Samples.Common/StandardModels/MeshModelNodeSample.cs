using System.Numerics;
using System.Text;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.Samples.Common.Utils;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.StandardModels;

public class MeshModelNodeSample : StandardModelsSampleBase
{
    public override string Title => "MeshModelNode";
    public override string Subtitle => "MeshModelNode defines a 3D model from Positions, Normals, TextureCoordinates and TriangleIndices.\nNote that the order of triangles is important: Material shows front side; BackMaterial shows back side (red in this sample; see bottom side of the plane).";

    private StandardMesh? _mesh;

    public MeshModelNodeSample(ICommonSamplesContext context)
        : base(context)
    {
        propertiesTitleText = "Mesh data:"; // Replace "Properties" text with "Mesh data:"

        // Change default CheckBox values:
        isSemiTransparentCheckBoxShown = false; 
        isSemiTransparentMaterialChecked = false;
        isTextureMaterialChecked = true;

        normalsLength = 10;
        normalsLineThickness = 2;
    }

    protected override ModelNode CreateModelNode()
    {
        // To create a MeshModelNode we need a mesh.

        // We can create a StandardMesh that requires an array of PositionNormalTextureVertex and TriangleIndices
        // or a GeometryMesh that require an array of Positions, Normals, TextureCoordinates and TriangleIndices (see commented code below):
        // In this case the Positions, Normals, TextureCoordinates are converted to an array of PositionNormalTextureVertex,
        // so using StandardMesh is faster.

        // Each PositionNormalTextureVertex vertex is defined by:
        // - position: defines the position in 3D space
        // - normal: defines the normal vector at the positions (for flat shade mesh this is the normal of the triangle) - this is used for lighting calculations (it is possible to calculate normals - see commented code below)
        // - textureCoordinate: required only for showing textures - they map 2D texture coordinates in range from (0, 0) to (1, 1) to each 3D position (it is possible to calculate texture coordinates - see commented code below)

        var vertices = new PositionNormalTextureVertex[4]
        {
            new PositionNormalTextureVertex(position: new Vector3(10, 0, 5),    normal: new Vector3(0, 1, 0), textureCoordinate: new Vector2(0, 0)),
            new PositionNormalTextureVertex(position: new Vector3(100, 0, 5),  normal: new Vector3(0, 1, 0), textureCoordinate: new Vector2(1, 0)),
            new PositionNormalTextureVertex(position: new Vector3(100, 0, 50), normal: new Vector3(0, 1, 0), textureCoordinate: new Vector2(1, 1)),
            new PositionNormalTextureVertex(position: new Vector3(10, 0, 50),   normal: new Vector3(0, 1, 0), textureCoordinate: new Vector2(0, 1)),
        };

        // triangleIndices connect 3 vertices into triangles (each vertex is defined by and index in the vertices array)
        var triangleIndices = new int[]
        {
            0, 2, 1, // first triangle from positions at indices 0, 2, 1
            3, 2, 0  // second triangle
        };

        // It is also possible to calculate the Normals from positions and triangle indices (see the following or some other overload of CalculateNormals):
        //Ab4d.SharpEngine.Utilities.MeshUtils.CalculateNormals(vertices, triangleIndices);

        // It is also possible to calculate the TextureCoordinates by using different projections:
        //Ab4d.SharpEngine.Utilities.MeshUtils.GenerateCubicTextureCoordinates(vertices, triangleIndices);
        //Ab4d.SharpEngine.Utilities.MeshUtils.GeneratePlanarTextureCoordinates(vertices, triangleIndices, ...);
        //Ab4d.SharpEngine.Utilities.MeshUtils.GenerateCylindricalTextureCoordinates(vertices, triangleIndices, ...);

        _mesh = new StandardMesh(vertices, triangleIndices, name: "StandardMesh");


        // It is also possible to create a GeometryMesh:
        //var positions = new Vector3[]
        //{
        //    new Vector3(10, 0, 5),
        //    new Vector3(100, 0, 5),
        //    new Vector3(100, 0, 50),
        //    new Vector3(10, 0, 50),
        //};

        //var normals = new Vector3[]
        //{
        //    new Vector3(0, 1, 0),
        //    new Vector3(0, 1, 0),
        //    new Vector3(0, 1, 0),
        //    new Vector3(0, 1, 0),
        //};

        //var textureCoordinates = new Vector2[]
        //{
        //    new Vector2(0, 0),
        //    new Vector2(1, 0),
        //    new Vector2(1, 1),
        //    new Vector2(0, 1),
        //};

        //_mesh = new GeometryMesh(positions, normals, textureCoordinates, triangleIndices, name: "GeometryMesh");



        var meshModelNode = new MeshModelNode(_mesh, StandardMaterials.Orange, name: "SampleMeshModel");
        meshModelNode.BackMaterial = StandardMaterials.Red;

        return meshModelNode;
    }

    protected override void OnCreateScene(Scene scene)
    {
        base.OnCreateScene(scene);

        // Show position index as 3D text
        var textBlockFactory = context.GetTextBlockFactory();
        textBlockFactory.FontSize = 6;
        textBlockFactory.BackgroundColor = Colors.Transparent; // no background
        textBlockFactory.BorderThickness = 0;

        if (_mesh != null)
        {
            var positions = _mesh.GetDataChannelArray<Vector3>(MeshDataChannelTypes.Positions);
            if (positions != null)
            {
                for (var i = 0; i < positions.Length; i++)
                {
                    var textNode1 = textBlockFactory.CreateTextBlock(i.ToString(), positions[i] + new Vector3(0, 0.5f, 0)); // Add position index text slightly above the model
                    scene.RootNode.Add(textNode1);
                }
            }
        }

        // Add WireGrid
        var wireGridNode = new WireGridNode()
        {
            CenterPosition = new Vector3(55, -0.1f, 30),
            Size = new Vector2(110, 60),
            WidthCellsCount = 11,
            HeightCellsCount = 6,
            MinorLineColor = Colors.DimGray
        };

        scene.RootNode.Add(wireGridNode);


        // Add 3 axis
        scene.RootNode.Add(new ArrowModelNode(startPosition: new Vector3(0, 0, 0), endPosition: new Vector3(30, 0, 0), radius: 0.5f) { Material = StandardMaterials.Red });
        scene.RootNode.Add(new ArrowModelNode(startPosition: new Vector3(0, 0, 0), endPosition: new Vector3(0, 30, 0), radius: 0.5f) { Material = StandardMaterials.Green });
        scene.RootNode.Add(new ArrowModelNode(startPosition: new Vector3(0, 0, 0), endPosition: new Vector3(0, 0, 30), radius: 0.5f) { Material = StandardMaterials.Blue });

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 20;
            targetPositionCamera.Attitude = -40;
            targetPositionCamera.TargetPosition = new Vector3(65, 0, 0);
        }
    }

    protected override void UpdateMaterial()
    {
        if (modelNode == null)
            return;

        modelNode.Material = GetMaterial();
        modelNode.BackMaterial = StandardMaterials.Red; // We always show Red material as BackMaterial
    }

    protected override void UpdateNormals(StandardMesh? mesh, Transform? modelTransform)
    {
        if (Scene == null || _mesh == null)
            return;

        if (normalsLineNode != null)
            Scene.RootNode.Remove(normalsLineNode);

        if (mesh == null)
            return;


        var normalsGroupNode = new GroupNode("NormalsGroup");

        var positions = _mesh.GetDataChannelArray<Vector3>(MeshDataChannelTypes.Positions);
        if (positions != null)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                var arrowModelNode = new ArrowModelNode("NormalArrowLine")
                {
                    StartPosition = positions[i],
                    EndPosition = positions[i] + new Vector3(0, 10, 0),
                    Radius = 0.3f,
                    Material = StandardMaterials.Orange
                };

                normalsGroupNode.Add(arrowModelNode);
            }
        }

        Scene.RootNode.Add(normalsGroupNode);

        normalsLineNode = normalsGroupNode;
    }

    protected override void OnCreatePropertiesUI(ICommonSampleUIProvider ui)
    {
        if (_mesh == null)
            return;

        var sb = new StringBuilder();

        var positions = _mesh.GetDataChannelArray<Vector3>(MeshDataChannelTypes.Positions);
        if (positions != null)
        {
            for (int i = 0; i < positions.Length; i++)
                sb.AppendFormat("\r\n{0}: ({1} {2} {3})", i, positions[i].X, positions[i].Y, positions[i].Z);

            ui.CreateLabel("Positions (black numbers):");
            ui.CreateTextBox(width: 160, height: 0, sb.ToString().Substring(2)); // Substring(2) is used to strip off initial new line "\r\n"
        }
        
        var normals = _mesh.GetDataChannelArray<Vector3>(MeshDataChannelTypes.Normals);
        if (normals != null)
        {
            sb.Clear();

            for (int i = 0; i < normals.Length; i++)
                sb.AppendFormat("\r\n{0}: ({1} {2} {3})", i, normals[i].X, normals[i].Y, normals[i].Z);

            ui.AddSeparator();
            ui.CreateLabel("Normals (orange arrows):");
            ui.CreateTextBox(width: 160, height: 0, sb.ToString().Substring(2)); // Substring(2) is used to strip off initial new line "\r\n"
        }
        
        var textureCoordinates = _mesh.GetDataChannelArray<Vector2>(MeshDataChannelTypes.TextureCoordinates);
        if (textureCoordinates != null)
        {
            sb.Clear();

            for (int i = 0; i < textureCoordinates.Length; i++)
                sb.AppendFormat("\r\n{0}: ({1} {2})", i, textureCoordinates[i].X, textureCoordinates[i].Y);

            ui.AddSeparator();
            ui.CreateLabel("TextureCoordinates:");
            ui.CreateTextBox(width: 160, height: 0, sb.ToString().Substring(2)); // Substring(2) is used to strip off initial new line "\r\n"
        }
        
        
        var triangleIndices = _mesh.GetDataChannelArray<int>(MeshDataChannelTypes.TriangleIndices);
        if (triangleIndices != null)
        {
            sb.Clear();

            for (int i = 0; i < triangleIndices.Length; i += 3)
                sb.AppendFormat("\r\n{0}, {1}, {2}", triangleIndices[i], triangleIndices[i + 1], triangleIndices[i + 2]);

            ui.AddSeparator();
            ui.CreateLabel("TriangleIndices:");
            ui.CreateTextBox(width: 160, height: 0, sb.ToString().Substring(2)); // Substring(2) is used to strip off initial new line "\r\n"
        }
    }
}