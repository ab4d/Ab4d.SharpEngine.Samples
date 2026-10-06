using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.Materials;

public class VertexColorsMaterialSample : CommonSample
{
    public override string Title => "Per-vertex colors";
    public override string Subtitle => "Demonstration of vertex-color change through mesh data channel.";

    private StandardMesh? _boxMesh;
    private VertexColorMaterial? _vertexColorMaterial;

    public VertexColorsMaterialSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override async Task OnCreateSceneAsync(Scene scene)
    {
        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 25;
            targetPositionCamera.Attitude = -15;
            targetPositionCamera.Distance = 200;

            targetPositionCamera.StartRotation(headingChangeInSecond: 50);
        }

        _boxMesh = MeshFactory.CreateBoxMesh(new Vector3(0, 0, 0), new Vector3(60, 25, 50));

        var colors = GetBoxVertexColors(_boxMesh);
        _boxMesh.SetDataChannel(MeshDataChannelTypes.VertexColors, colors);

        _vertexColorMaterial = new VertexColorMaterial("VertexColorMaterial")
        {
            //HasTransparency = true,     // Set HasTransparency to true when vertex colors have alpha < 1
            //IsSolidColor = false,       // Set to false to enable light shading
            //SpecularPower = 12,         // Set SpecularPower > 0 to enable specular highlights. IsSolidColor must also be set to false.
            //Opacity = 0.7f,             // Opacity is multiplied by alpha values of the per-vertex colors (by default, set to 1 to preserve the per-vertex alpha values)
            //DiffuseColor = Colors.Blue, // DiffuseColor is multiplied by the per-vertex colors (by default, set to White to preserve the per-vertex colors)
        };

        var modelNode = new MeshModelNode(_boxMesh, _vertexColorMaterial, "VertexColorModel");
        scene.RootNode.Add(modelNode);

        ShowCameraAxisPanel = true;
    }

    protected override void OnCreateLights(Scene scene)
    {
        var directionalLight = new DirectionalLight(new Vector3(-0.3f, -1f, 0));
        scene.Lights.Add(directionalLight);

        var pointLight = new PointLight(new Vector3(-500, 100, 200));
        scene.Lights.Add(pointLight);

        scene.SetAmbientLight(intensity: 0.3f);

        base.OnCreateLights(scene);
    }


    private Color4[]? GetBoxVertexColors(StandardMesh boxMesh)
    {
        if (boxMesh.Vertices == null)
            return null;

        var positionsCount = boxMesh.VertexCount;
        var positionColors = new Color4[positionsCount];

        var boxBounds = boxMesh.BoundingBox;

        for (var i = 0; i < positionsCount; i++)
        {
            var position = boxMesh.Vertices[i].Position;

            var red = (position.X - boxBounds.Minimum.X) / boxBounds.SizeX;
            var green = (position.Y - boxBounds.Minimum.Y) / boxBounds.SizeY;
            var blue = (position.Z - boxBounds.Minimum.Z) / boxBounds.SizeZ;

            positionColors[i] = new Color4(red, green, blue, alpha: 1.0f);
        }

        return positionColors;
    }

    private Color4[]? GetBoxRandomVertexColors(StandardMesh boxMesh)
    {
        if (boxMesh.Vertices == null)
            return null;

        var positionsCount = boxMesh.VertexCount;
        var positionColors = new Color4[positionsCount];

        var rand = new Random();
        for (var i = 0; i < positionsCount; i++)
        {
            positionColors[i] = new Color4(rand.NextSingle(), rand.NextSingle(), rand.NextSingle(), alpha: 1.0f);
        }
        return positionColors;
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateButton("Random colors",
        () =>
        {
            var colors = GetBoxRandomVertexColors(_boxMesh!);
            _boxMesh?.SetDataChannel(MeshDataChannelTypes.VertexColors, colors);
        });
        ui.CreateButton("Original colors",
            () =>
            {
                var colors = GetBoxVertexColors(_boxMesh!);
                _boxMesh?.SetDataChannel(MeshDataChannelTypes.VertexColors, colors);
            });
        ui.AddSeparator();
        ui.CreateCheckBox("Transparent colors",false,
            (isChecked) =>
            {
                if (isChecked)
                {
                    _vertexColorMaterial?.HasTransparency = true;
                    _vertexColorMaterial?.Opacity = 0.5f;
                }
                else
                {
                    _vertexColorMaterial?.HasTransparency = false;
                    _vertexColorMaterial?.Opacity = 1.0f;
                }
            });
    }
}
