﻿using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Effects;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.ObjFile;
using Ab4d.SharpEngine.Samples.Common.Utils;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.Materials;

public class MaterialsSample : CommonSample
{
    public override string Title => "Materials";
    public override string Subtitle => "See code behind to see different ways to assign each material type";

    public MaterialsSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        var sphereRadius = 30;
        var sphereMesh = MeshFactory.CreateSphereMesh(new Vector3(0, 0, 0), radius: sphereRadius);


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


        var modelNode1 = new MeshModelNode(sphereMesh, material1, "DiffuseMaterialModel")
        {
            Transform = new TranslateTransform(-250, 0, 0)
        };

        scene.RootNode.Add(modelNode1);

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

        var modelNode2 = new MeshModelNode(sphereMesh, material2, "SpecularMaterialModel")
        {
            Transform = new TranslateTransform(-150, 0, 0)
        };

        scene.RootNode.Add(modelNode2);

        var textNode2 = textBlockFactory.CreateTextBlock("SpecularMaterial", new Vector3(-150, -20, 50), textAttitude: 30);
        scene.RootNode.Add(textNode2);


        //
        // 3) Material with Diffuse texture
        //

        string textureFileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources/Textures/10x10-texture.png"); // NOTE: SharpEngine will automatically adjust directory separator ('\' or '/' based on the used OS).

        // The most easy way to define a texture material is to set the file name in the StandardMaterial constructor.
        // We also need to set the BitmapIO provider that will read the bitmap (this is automatically set by the sample; it can be WpfBitmapIO, AvaloniaBitmapIO, WinUIBitmapIO, SkiaSharpBitmapIO or ImageMagickBitmapIO)
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

        var modelNode3 = new MeshModelNode(sphereMesh, material3, "TextureMaterialModel")
        {
            Transform = new TranslateTransform(-50, 0, 0)
        };

        scene.RootNode.Add(modelNode3);

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

        var modelNode4 = new MeshModelNode(sphereMesh, material4, "SemiTransparentModel")
        {
            BackMaterial = material4, // Because we can see inside the Model, we also set the BackMaterial that will render the back sides of the triangles
            Transform = new TranslateTransform(50, 0, 0)
        };

        scene.RootNode.Add(modelNode4);

        var textNode4 = textBlockFactory.CreateTextBlock("SemiTransparent\r\nMaterial", new Vector3(50, -15, 50), textAttitude: 30);
        scene.RootNode.Add(textNode4);


        //
        // 5) VertexColor material (specify different color for each vertex)
        //

        if (sphereMesh.Vertices != null)
        {
            var positionsCount = sphereMesh.VertexCount;
            var positionColors = new Color4[positionsCount];

            var sphereDiameter = sphereRadius * 2;

            for (int i = 0; i < positionsCount; i++)
            {
                var position = sphereMesh.Vertices[i].Position;

                // Get colors based on the relative position inside the Sphere
                float red   = (position.X + sphereRadius) / sphereDiameter;
                float Orange = (position.Y + sphereRadius) / sphereDiameter;
                float blue  = (position.Z + sphereRadius) / sphereDiameter;

                // Set Color this position
                positionColors[i] = new Color4(red, Orange, blue, alpha: 1.0f);
            }

            var vertexColorMaterial = new VertexColorMaterial(positionColors, "VertexColorMaterial");

            vertexColorMaterial.HasTransparency = false; // When we also have transparent colors, we need to set HasTransparency to true

            // If later the positions colors are changed, we also need to call UpdatePositionColors:
            //vertexColorMaterial.UpdatePositionColors();

            var modelNode5 = new MeshModelNode(sphereMesh, vertexColorMaterial, "VertexColorModel")
            {
                Transform = new TranslateTransform(150, 0, 0)
            };

            scene.RootNode.Add(modelNode5);

            var textNode5 = textBlockFactory.CreateTextBlock("VertexColor\r\nMaterial", new Vector3(150, -15, 50), textAttitude: 30);
            scene.RootNode.Add(textNode5);
        }


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

        var modelNode6 = new MeshModelNode(sphereMesh, solidColorMaterial, "SolidColorModel")
        {
            Transform = new TranslateTransform(250, 0, 0)
        };

        scene.RootNode.Add(modelNode6);

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
}