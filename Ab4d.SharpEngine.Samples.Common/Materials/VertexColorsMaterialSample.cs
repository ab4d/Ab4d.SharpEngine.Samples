using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.Materials;

public class VertexColorsMaterialSample : CommonSample
{
    public override string Title => "VertexColorMaterial";
    public override string Subtitle => "VertexColorMaterial can be used to render 3D objects by specifying color for each position.";

    private VertexColorMaterial? _vertexColorMaterial;
    private MeshModelNode? _sampleModelNode;
    private StandardMesh[] _meshes = new StandardMesh[2];

    private ICommonSampleUIElement? _specularPowerCheckBox;

    public VertexColorsMaterialSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        sceneView.BackgroundColor = Colors.White;

        base.OnSceneViewInitialized(sceneView);
    }

    protected override void OnCreateScene(Scene scene)
    {
        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 25;
            targetPositionCamera.Attitude = -15;
            targetPositionCamera.Distance = 200;
            targetPositionCamera.ShowCameraLight = ShowCameraLightType.Never;

            targetPositionCamera.StartRotation(headingChangeInSecond: 50);
        }


        var boxMesh = MeshFactory.CreateBoxMesh(new Vector3(0, 0, 0), new Vector3(60, 25, 50));
        var boxVertexColors = MaterialsSample.GetBoxVertexColors(boxMesh);

        // The recommended way to store vertex colors is to use VertexColors data channel on the mesh.
        // This way the colors are part of the mesh.
        //
        // Another possibility is to create VertexColorMaterial object with vertexColors array or
        // set PositionColor property to the vertexColors array.
        // This decouples the colors from the mesh.
        // Also, this does not work for WebGL.
        //var vertexColorMaterial = new VertexColorMaterial(boxVertexColors!);
        boxMesh.SetDataChannel(MeshDataChannelTypes.VertexColors, boxVertexColors);
        

        var sphereMesh = MeshFactory.CreateSphereMesh(new Vector3(0, 0, 0), radius: 30);
        var sphereVertexColors = MaterialsSample.GetSphereVertexColors(sphereMesh, sphereRadius: 30);
        sphereMesh.SetDataChannel(MeshDataChannelTypes.VertexColors, sphereVertexColors);

        _meshes[0] = boxMesh;
        _meshes[1] = sphereMesh;


        _vertexColorMaterial = new VertexColorMaterial("VertexColorMaterial")
        {
            //HasTransparency = true,     // Set HasTransparency to true when vertex colors have alpha < 1
            //IsSolidColor = false,       // Set to false to enable light shading
            //SpecularPower = 32,         // Set SpecularPower > 0 to enable specular highlights. IsSolidColor must also be set to false.
            //Opacity = 0.7f,             // Opacity is multiplied by alpha values of the per-vertex colors (by default, set to 1 to preserve the per-vertex alpha values)
            //DiffuseColor = Colors.Blue, // DiffuseColor is multiplied by the per-vertex colors (by default, it is set to White to preserve the per-vertex colors)
        };

        _sampleModelNode = new MeshModelNode(boxMesh, _vertexColorMaterial, "VertexColorModel")
        {
            BackMaterial = _vertexColorMaterial // also set BackMaterial so inner triangles are visible when using transparent vertex colors
        };
        scene.RootNode.Add(_sampleModelNode);

        ShowCameraAxisPanel = true;
    }

    private void SetStandardVertexColors()
    {
        var boxMesh = _meshes[0];
        var sphereMesh = _meshes[1];

        // Just replace the existing colors array with new one.
        // This will also notify the mesh about the change.
        boxMesh.SetDataChannel(MeshDataChannelTypes.VertexColors, MaterialsSample.GetBoxVertexColors(boxMesh));
        sphereMesh.SetDataChannel(MeshDataChannelTypes.VertexColors, MaterialsSample.GetSphereVertexColors(sphereMesh, sphereRadius: 30));
    }

    private void SetRandomVertexColors(bool hasTransparentColors)
    {
        foreach (var mesh in _meshes)
        {
            var vertexColors = mesh.GetDataChannelArray<Color4>(MeshDataChannelTypes.VertexColors);

            if (vertexColors == null)
                continue;

            for (var i = 0; i < vertexColors.Length; i++)
                vertexColors[i] = new Color4(GetRandomColor3(), alpha: hasTransparentColors ? GetRandomFloat() : 1.0f);

            // Notify the Mesh about the change in vertexColors array
            mesh.UpdateDataChannel(MeshDataChannelTypes.VertexColors); 
        }
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

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateRadioButtons(new string[] { "Box", "Sphere" }, 
            (selectedIndex, selectedText) => _sampleModelNode!.Mesh = _meshes[selectedIndex], 
            selectedItemIndex: 0);

        ui.AddSeparator();
        

        ui.CreateButton("Random colors", () =>
            {
                SetRandomVertexColors(hasTransparentColors: false);
                _vertexColorMaterial!.HasTransparency = false;
            });
        
        ui.CreateButton("Random transparent colors", () =>
            {
                SetRandomVertexColors(hasTransparentColors: true);
                _vertexColorMaterial!.HasTransparency = true;
            });

        ui.CreateButton("Original colors", () =>
            {
                SetStandardVertexColors();
                _vertexColorMaterial!.HasTransparency = false;
            });


        ui.AddSeparator();

        ui.CreateCheckBox("Use blue color mask", false, (isChecked) =>
        {
            // DiffuseColor and Opacity properties on VertexColorMaterial are used as color mask: each color in _vertexColors is multiplied with that.
            // Therefore, the initial values of DiffuseColor is White.
            _vertexColorMaterial!.DiffuseColor = isChecked ? Colors.Blue : Colors.White;
        });

        ui.CreateCheckBox("IsSolidColor (?):When unchecked, then triangles are shaded by the angle to the light.\nIn this sample, when unchecked, the botton and -Z part of the object will be darker.", true, (isChecked) =>
        {
            _vertexColorMaterial!.IsSolidColor = isChecked;

            // Specular effect can be rendered only when IsSolidColor is set to false
            _specularPowerCheckBox?.SetIsVisible(!isChecked);
        });

        _specularPowerCheckBox = ui.CreateCheckBox("Use Specular effect (?):When checked, then specular effect is rendered on the model. This can be used only when IsSolidColor is set to false. Use Sphere mesh to better see this effect.", false, 
            (isChecked) => _vertexColorMaterial!.SpecularPower = isChecked ? 32 : 0 ).SetIsVisible(false);
    }
}
