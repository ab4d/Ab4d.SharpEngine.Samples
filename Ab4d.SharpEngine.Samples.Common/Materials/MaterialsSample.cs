using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.Materials;

public class MaterialsSample : CommonSample
{
    public override string Title => "Materials";
    public override string Subtitle => "See code behind to see different ways to assign each material type";

    private GroupNode? _testModelsGroup;
    private StandardMesh? _sphereMesh;
    private StandardMesh? _boxMesh;
    private MeshModelNode? _vertexColorModelNode;


    public MaterialsSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        int sphereRadius = 30;

        _sphereMesh = MeshFactory.CreateSphereMesh(new Vector3(0, 0, 0), radius: sphereRadius);
        _boxMesh    = MeshFactory.CreateBoxMesh(new Vector3(0, 0, 0), new Vector3(60, 25, 50));

        // Set VertexColors to meshes so that we can use VertexColorMaterial to render those two meshes
        
        // Generate colors for each vertex (position).
        // Then set positionColors to VertexColors data channel on the mesh
        // VertexColors data channel can be set to an array of Color3 or Color4 items.
        //
        // If we later change the values in the positionColors array, then we need to call
        // boxMesh.UpdateDataChannel(MeshDataChannelTypes.VertexColors);
        // after changing the data for the changes to take effect.

        var vertexColors = GetSphereVertexColors(_sphereMesh, sphereRadius);
        _sphereMesh.SetDataChannel(MeshDataChannelTypes.VertexColors, vertexColors);

        vertexColors = GetBoxVertexColors(_boxMesh);
        _boxMesh.SetDataChannel(MeshDataChannelTypes.VertexColors, vertexColors);


        var textBlockFactory = context.GetTextBlockFactory();
        textBlockFactory.BackgroundColor = Colors.LightYellow;
        textBlockFactory.BorderThickness = 1;
        textBlockFactory.BorderColor = Colors.DimGray;
        textBlockFactory.FontSize = 9;

        var boxModelNode = new BoxModelNode(centerPosition: new Vector3(0, -40, 0), size: new Vector3(700, 10, 300))
        {
            Material = StandardMaterials.Silver
        };

        scene.RootNode.Add(boxModelNode);


        // Test models will be added to TestModelsGroup
        _testModelsGroup = new GroupNode("TestModelsGroup");
        scene.RootNode.Add(_testModelsGroup);


        //
        // 1) Material with Diffuse color
        //

        // The most simple way to define a material is to use StandardMaterials that define StandardMaterial objects with DiffuseColor set to all common colors:
        var material1 = StandardMaterials.Orange;

        // We could also define the same material with the following:
        //material1 = new StandardMaterial(Colors.Orange, name: "Orange material"); // name is optional
        //material1 = new StandardMaterial()
        //{
        //    DiffuseColor = Colors.Orange
        //};

        //material1 = new StandardMaterial(new Color3(red: 1, green: 0.647058845f, blue: 0));
        //material1 = new StandardMaterial(Color3.FromByteRgb(red: 255, green: 165, blue: 0));


        var modelNode1 = new MeshModelNode(_sphereMesh, material1, "DiffuseMaterialModel")
        {
            Transform = new TranslateTransform(-250, 0, 0)
        };

        _testModelsGroup.Add(modelNode1);

        var textNode1 = textBlockFactory.CreateTextBlock("DiffuseMaterial", new Vector3(-250, -20, 50), textAttitude: 30);
        scene.RootNode.Add(textNode1);



        //
        // 2) Material with Diffuse color and Specular color and Specular power
        //

        var material2 = StandardMaterials.Orange.SetSpecular(specularPower: 32);

        // Other options to define the same material:
        //material2 = StandardMaterials.Orange.SetSpecular(specularColor: Colors.White, specularPower: 32, name: "Orange specular material"); // name is optional
        //material2 = new StandardMaterial(Colors.Orange, opacity: 1, specularPower: 32);
        //
        //material2 = new StandardMaterial(Colors.Orange)
        //{
        //    SpecularColor = Colors.White,
        //    SpecularPower = 32
        //};

        //material2 = new StandardMaterial()
        //{
        //    DiffuseColor = Colors.Orange,
        //    SpecularColor = Colors.White,
        //    SpecularPower = 32
        //};

        var modelNode2 = new MeshModelNode(_sphereMesh, material2, "SpecularMaterialModel")
        {
            Transform = new TranslateTransform(-150, 0, 0)
        };

        _testModelsGroup.Add(modelNode2);

        var textNode2 = textBlockFactory.CreateTextBlock("SpecularMaterial", new Vector3(-150, -20, 50), textAttitude: 30);
        scene.RootNode.Add(textNode2);


        //
        // 3) Material with Diffuse texture
        //

        // Because Ab4d.SharpEngine is cross-platform and can work with different UI frameworks, 
        // it cannot use an OS or UI framework dependent bitmap readers.
        // Therefore, there is a IBitmapIO interface that provides an abstraction for bitmap IO operations (reading and saving images).
        // See online help for IBitmapIO: https://www.ab4d.com/help/SharpEngine/html/T_Ab4d_SharpEngine_Common_IBitmapIO.htm
        //
        // Ab4d.SharpEngine provides the PngBitmapIO that implements IBitmapIO and as its name suggests it reads and writes png files.
        // The PngBitmapIO is also set to the GpuDevice.DefaultBitmapIO and is used as a fallback IBitmapIO provider.
        // So if you need to read or write png files, then you do not need any additional third-party dependencies to read bitmap files.
        //
        // But if you want to read jpg or other file types, or would like to use OS or other native file readers and writers,
        // then you can implement your own class that implements IBitmapIO interface or use existing implementation of IBitmapIO.
        //
        // The following are existing implementation of IBitmapIO:
        // 
        // | class name:           | assembly:                   | comments:
        // |--------------------------------------------------------------------------------------
        // | PngBitmapIO           | Ab4d.SharpEngine            | reads and writes only png files; by default set to GpuDevice.DefaultBitmapIO
        // | SkiaSharpBitmapIO     | Ab4d.SharpEngine.Avalonia   |
        // | WpfBitmapIO           | Ab4d.SharpEngine.WPF        |
        // | SystemDrawingBitmapIO | Ab4d.SharpEngine.WinForms   |
        // | WinUIBitmapIO         | Ab4d.SharpEngine.WinUI      |
        //
        // 
        // There are multiple ways to create a texture from a bitmap image.
        // You can use define the bitmap file name in the constructor of StandardMaterial or SolidColorMaterial.
        // You can also use TextureLoader to get the GpuImage and then set that to DiffuseTexture property in the material.
        // It is also possible to load the texture in the background.
        // See samples and code comments below.
        //
        // If not otherwise specified, then the IBitmapIO that is defined in GpuDevice.DefaultBitmapIO is used.
        // That property is by default set to an instance of PngBitmapIO.
        

        string textureFileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources/Textures/10x10-texture.png"); // NOTE: SharpEngine will automatically adjust directory separator ('\' or '/' based on the used OS).

        // The most easy way to define a texture material is to set the file name in the StandardMaterial constructor.
        // We also need to set the BitmapIO provider that will read the bitmap (this is automatically set by the sample; it can be WpfBitmapIO, SkiaSharpBitmapIO (from Ab4d.SharpEngine.Avalonia), WinUIBitmapIO or ImageMagickBitmapIO)
        // This will save the texture name and when the GpuDevice is available, then the texture file will be loaded and a GpuImage object will be created on the graphics card.
        var material3 = new StandardMaterial(textureFileName, this.BitmapIO, name: "10x10-texture.png"); // name is optional

        //// When the GpuDevice is available, then we can also create the GpuImage manually and set it to the DiffuseTexture property:
        //var gpuImage = TextureLoader.CreateTexture(textureFileName, this.BitmapIO, GpuDevice, generateMipMaps: true, isDeviceLocal: true, cacheGpuTexture: true);
        //material3 = new StandardMaterial()
        //{
        //    DiffuseTexture = gpuImage
        //};

        //// We can also change the sample type from the default Wrap to some other sampler type
        //material3.DiffuseTextureSamplerType = CommonSamplerTypes.Mirror;

        //// Instead of setting DiffuseTextureSamplerType, we can also provide a custom Vulkan Sampler:
        ////material3.DiffuseTextureSampler = customVulkanSampler;

        //// When StandardMaterial is showing a diffuse texture, then the colors from the texture are multiplied by the color set in the DiffuseColor property (color mask).
        //// Because of this, the DiffuseColor is automatically set to white color (from Black color).
        //// If you want to multiply all the colors by some color (set the color mask), you can change the DiffuseColor:
        //material3.DiffuseColor = Colors.Red;


        //
        // Background / async texture loading:
        //

        // We can also load the texture in the background (set loadInBackground to true).
        // The initialDiffuseColor parameter sets the initial color that is replaced with the texture when the texture is loaded.
        //var material3 = new StandardMaterial(textureFileName, this.BitmapIO, initialDiffuseColor: Colors.Gray, loadInBackground: true, name: "10x10-texture.png"); 

        // We can also await by using TextureLoader.CreateTextureAsync:
        //var gpuImage = await TextureLoader.CreateTextureAsync(textureFileName, scene, this.BitmapIO);
        //var material3 = new StandardMaterial(gpuImage);

        // We can also use a callback action to set the loaded texture to the created material
        //var material3 = new StandardMaterial(Colors.Gray); // Initially set the color to Gray
        //TextureLoader.CreateTextureAsync(textureFileName, scene, gpuImage =>
        //{
        //    material3.DiffuseTexture = gpuImage;   // Set the texture
        //    material3.DiffuseColor = Colors.White; // Set the color mask to white
        //}, this.BitmapIO);


        var modelNode3 = new MeshModelNode(_sphereMesh, material3, "TextureMaterialModel")
        {
            Transform = new TranslateTransform(-50, 0, 0)
        };

        _testModelsGroup.Add(modelNode3);

        var textNode3 = textBlockFactory.CreateTextBlock("TextureMaterial", new Vector3(-50, -20, 50), textAttitude: 30);
        scene.RootNode.Add(textNode3);


        //
        // 4) Semi-transparent material
        //

        var material4 = StandardMaterials.Orange.SetOpacity(0.5f);

        //material4 = new StandardMaterial("SemiTransparentMaterial") // name is optional
        //{
        //    DiffuseColor = Colors.Orange,
        //    Opacity = 0.5f,
        //};

        //material4 = new StandardMaterial(new Color3(red: 0, Orange: 1, blue: 0), opacity: 0.5f);
        //material4 = new StandardMaterial(new Color4(red: 0, Orange: 1, blue: 0, alpha: 0.5f));

        // We can also adjust the transparency of the texture:
        //material4 = new StandardMaterial(textureFileName, this.BitmapIO)
        //{
        //    Opacity = 0.5f
        //};

        var modelNode4 = new MeshModelNode(_sphereMesh, material4, "SemiTransparentModel")
        {
            BackMaterial = material4, // Because we can see inside the Model, we also set the BackMaterial that will render the back sides of the triangles
            Transform = new TranslateTransform(50, 0, 0)
        };

        _testModelsGroup.Add(modelNode4);

        var textNode4 = textBlockFactory.CreateTextBlock("SemiTransparent\r\nMaterial", new Vector3(50, -15, 50), textAttitude: 30);
        scene.RootNode.Add(textNode4);


        //
        // 5) VertexColor material (specify different color for each vertex)
        //

        // The easiest option to render VertexColor material is to set an array of Color3 or Color4 data
        // to the mesh's VertexColors data channel. See GetSphereVertexColorData or GetBoxVertexColorData methods below.
        //
        // Another option is to set the PositionColors array in the VertexColorMaterial object.
        // This will also overwrite the data in the VertexColors data channel.
        //
        // It is recommended to use VertexColors data channel instead of PositionColors in VertexColorMaterial
        // because in this case the color is just another property of each position in the mesh
        // and therefore that data belongs to the mesh.
        //
        // In case of using PositionColors in VertexColorMaterial we need new VertexColorMaterial for each mesh.
        //
        // The following code shows how to set up PositionColors for VertexColorMaterial:
        //
        //var positionColors = GetSphereVertexColors(_sphereMesh, sphereRadius);
        //var vertexColorMaterial = new VertexColorMaterial(positionColors, "SphereVertexColorMaterial");

        // When using transparent colors (alpha < 1) for VertexColorMaterial,
        // we need to convert the colors to alpha pre-multiplied values.
        // This is done by multiplying all the color components by alpha value, for example:
        // float alpha = 0.3f;
        // positionColors[i] = new Color4(red * alpha, green * alpha, blue * alpha, alpha);

        var vertexColorMaterial = new VertexColorMaterial("VertexColorMaterial");

        _vertexColorModelNode = new MeshModelNode(_sphereMesh, vertexColorMaterial, "VertexColorModel")
        {
            Transform = new TranslateTransform(150, 0, 0)
        };

        _testModelsGroup.Add(_vertexColorModelNode);

        var textNode5 = textBlockFactory.CreateTextBlock("VertexColor\r\nMaterial", new Vector3(150, -15, 50), textAttitude: 30);
        scene.RootNode.Add(textNode5);


        //
        // 6) Solid-color material (material where no light-shading is applied)
        //

        //// We can also use texture:
        //var solidColorMaterial = new SolidColorMaterial(textureFileName, this.BitmapIO);
        
        // We could also render solid color by using StandardMaterial and then setting Effect to SolidColorEffect:
        //var solidColorMaterial = StandardMaterials.Orange;
        //var solidColorMaterial = new StandardMaterial(Colors.Orange, "SolidColorMaterial");
        //
        //// ... and then change the default effect that is used to render that material to a SolidColorEffect
        //solidColorMaterial.Effect = scene.EffectsManager.GetDefault<SolidColorEffect>();

        var solidColorMaterial = new SolidColorMaterial(Colors.Orange, "SolidColorMaterial");

        // NOTE:
        // SolidColorMaterial also supports IsTwoSided property.
        // When it is true, the front and back triangles are rendered with one draw call.
        //solidColorMaterial.IsTwoSided = true;

        var modelNode6 = new MeshModelNode(_sphereMesh, solidColorMaterial, "SolidColorModel")
        {
            Transform = new TranslateTransform(250, 0, 0)
        };

        _testModelsGroup.Add(modelNode6);

        var textNode6 = textBlockFactory.CreateTextBlock("SolidColor\r\nMaterial", new Vector3(250, -15, 50), textAttitude: 30);
        scene.RootNode.Add(textNode6);


        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 25;
            targetPositionCamera.Attitude = -15;
            targetPositionCamera.Distance = 800;
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

    
    private Color4[]? GetSphereVertexColors(StandardMesh sphereMesh, float sphereRadius)
    {
        if (sphereMesh.Vertices == null)
            return null;

        var positionsCount = sphereMesh.VertexCount;
        var positionColors = new Color4[positionsCount];

        var sphereDiameter = sphereRadius * 2;

        for (int i = 0; i < positionsCount; i++)
        {
            var position = sphereMesh.Vertices[i].Position;

            // Get colors based on the relative position inside the Sphere
            float red = (position.X + sphereRadius) / sphereDiameter;
            float green = (position.Y + sphereRadius) / sphereDiameter;
            float blue = (position.Z + sphereRadius) / sphereDiameter;

            // Set Color this position
            positionColors[i] = new Color4(red, green, blue, alpha: 1.0f);

            // When using transparent colors (alpha < 1) for VertexColorMaterial,
            // we need to convert the colors to alpha pre-multiplied values.
            // This is done by multiplying all the color components by alpha value, for example:
            // float alpha = 0.3f;
            // positionColors[i] = new Color4(red * alpha, green * alpha, blue * alpha, alpha);
        }

        return positionColors;
    }
    
    private Color4[]? GetBoxVertexColors(StandardMesh boxMesh)
    {
        if (boxMesh.Vertices == null)
            return null;

        var positionsCount = boxMesh.VertexCount;
        var positionColors = new Color4[positionsCount];

        var boxBounds = boxMesh.BoundingBox;

        for (int i = 0; i < positionsCount; i++)
        {
            var position = boxMesh.Vertices[i].Position;

            // Get colors based on the relative position inside the Bounds - in range from (0, 0, 0) to (1, 1, 1)
            float red = (position.X - boxBounds.Minimum.X) / boxBounds.SizeX;
            float green = (position.Y - boxBounds.Minimum.Y) / boxBounds.SizeY;
            float blue = (position.Z - boxBounds.Minimum.Z) / boxBounds.SizeZ;

            // Set Color this position
            positionColors[i] = new Color4(red, green, blue, alpha: 1.0f);

            // When using transparent colors (alpha < 1) for VertexColorMaterial,
            // we need to convert the colors to alpha pre-multiplied values.
            // This is done by multiplying all the color components by alpha value, for example:
            // float alpha = 0.3f;
            // positionColors[i] = new Color4(red * alpha, green * alpha, blue * alpha, alpha);
        }

        return positionColors;
    }

    private void UpdateMesh(bool isSphereMesh)
    {
        if (_testModelsGroup == null || _sphereMesh == null || _boxMesh == null)
            return;

        foreach (var sceneNode in _testModelsGroup)
        {
            if (sceneNode is MeshModelNode meshModelNode)
                meshModelNode.Mesh = isSphereMesh ? _sphereMesh : _boxMesh;
        }
    }
    
    private void UpdateFrontBackMaterial(bool isFrontMaterial)
    {
        if (_testModelsGroup == null)
            return;

        foreach (var sceneNode in _testModelsGroup)
        {
            if (sceneNode is ModelNode modelNode)
            {
                var material = modelNode.Material ?? modelNode.BackMaterial;

                if (isFrontMaterial)
                {
                    modelNode.Material     = material;
                    modelNode.BackMaterial = null;

                }
                else
                {
                    modelNode.Material     = null;
                    modelNode.BackMaterial = material;
                }
            }
        }
        
        // NOTE:
        // SolidColorMaterial supports IsTwoSided property.
        // When it is true, the front and back triangles are rendered with one draw call.
        // This is the same as Material and BackMaterial are set to the same material, but it renders twice as fast.
        //solidColorMaterial.IsTwoSided = true;
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateRadioButtons(new string[] { "Sphere", "Box" }, 
            (selectedIndex, selectedText) => UpdateMesh(isSphereMesh: selectedIndex == 0), 
            selectedItemIndex: 0);

        ui.AddSeparator();

        ui.CreateRadioButtons(new string[]
            {
                "Set front material (?):Sets the material to the Material property that shows the material on the front side of the triangles.", 
                "Set back material (?):Sets the material to the BackMaterial property that shows the material on the back side of the triangles."
            }, 
            (selectedIndex, selectedText) => UpdateFrontBackMaterial(isFrontMaterial: selectedIndex == 0), 
            selectedItemIndex: 0);
    }
}