//#define ADVANCED_TIME_MEASUREMENT

using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Effects;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.Samples.Common.Utils;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.Vulkan;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Security.Cryptography;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

public class MultiSceneNodesSample : CommonSample
{
    public override string Title => "Multiple SceneNodes";
    public override string? Subtitle => "";

    private bool _isAnimatingScene = true;

    private PyramidModelNode? _silverPyramidModel;
    private StandardMaterial? _silverPyramidMaterial;

    private ModelNode? _animatedModel1;

    private long _initialTimestamp;

    private MatrixTransform? _silverPyramidTransform;
    private MatrixTransform? _redPyramidTransform;
    private TranslateTransform? _specialMaterialGroupTransform;

    private SphereModelNode? _sphereGeometryModel;
    private Transform? _sphereTransform;
    private GroupNode? _specialMaterialGroup;

    private VertexColorMaterial? _vertexColorMaterial;
    private MeshModelNode? _vertexColorModel;

    private StandardMaterial? _thickLineOverrideMaterial;
    private LineMaterial? _lineMaterial1;
    private MeshModelNode? _thickLineModel1;
    private MeshModelNode? _thickLineOverrideModel;

    private GroupNode? _lightsGroup;
    private GroupNode? _additionalObjectsGroup;

    private DisposeList _disposables;

#if ADVANCED_TIME_MEASUREMENT
    private DateTime _startTime;
    private static double _loadBitmapFontsTime;
#endif

    public MultiSceneNodesSample(ICommonSamplesContext context)
        : base(context)
    {
        _disposables = new DisposeList();
    }

    protected override void OnDisposed()
    {
        if (SceneView != null)
        {
            SceneView.SceneUpdating -= UpdateModelWorldMatrices;

#if ADVANCED_TIME_MEASUREMENT
            SceneView.SceneRendered -= SceneViewOnSceneRendered;
#endif
        }

        _disposables.Dispose();

        base.OnDisposed();
    }

    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        // Subscribe to SceneUpdating to update animations
        sceneView.SceneUpdating += UpdateModelWorldMatrices;

#if ADVANCED_TIME_MEASUREMENT
        sceneView.SceneRendered += SceneViewOnSceneRendered;
#endif

        base.OnSceneViewInitialized(sceneView);
    }


#if ADVANCED_TIME_MEASUREMENT
    private void SceneViewOnSceneRendered(object? sender, EventArgs e)
    {
        if (SceneView == null)
            return;

        SceneView.SceneRendered -= SceneViewOnSceneRendered;

        System.Diagnostics.Debug.WriteLine($"TIME TO FIRST FRAME: {(DateTime.Now - _startTime).TotalMilliseconds} ms");
        System.Diagnostics.Debug.WriteLine($"BufferStagingTicks: {SceneView.GpuDevice.BufferStagingTimeMs} ms");
        System.Diagnostics.Debug.WriteLine($"ImageStagingTime: {SceneView.GpuDevice.ImageStagingTimeMs} ms");
        System.Diagnostics.Debug.WriteLine($"TextureLoader.LoadBitmapTimeMs: {TextureLoader.LoadBitmapTimeMs} ms");
        System.Diagnostics.Debug.WriteLine($"TextureLoader.CreateGpuImageTimeMs: {TextureLoader.CreateGpuImageTimeMs} ms");
        System.Diagnostics.Debug.WriteLine($"loadBitmapFontsTime: {_loadBitmapFontsTime} ms");
    }
#endif

    protected override void OnCreateScene(Scene scene)
    {
#if ADVANCED_TIME_MEASUREMENT
        _startTime = DateTime.Now;
#endif

        var gpuDevice = scene.GpuDevice;
        
        var planeModel = new PlaneModelNode(centerPosition: new Vector3(0, -50, 0),
                                            size: new Vector2(800, 1000),
                                            normal: new Vector3(0, 1, 0),
                                            heightDirection: new Vector3(1, 0, 0),
                                            name: "Gray plane")
        {
            Material = StandardMaterials.Gray,
            BackMaterial = StandardMaterials.Black
        };

        scene.RootNode.Add(planeModel);


        var axisLineNode = new AxisLineNode(length: 50, "AxisGroup");
        scene.RootNode.Add(axisLineNode);



        //_silverPyramidMaterial = new StandardMaterial("SpecularGrayMaterial1") { DiffuseColor = Colors.White, SpecularColor = Color3.White, SpecularPower = 16 };
        _silverPyramidMaterial = StandardMaterials.LightGray.SetSpecular(Color3.White, specularPower: 16);
        _silverPyramidTransform = new MatrixTransform(Matrix4x4.CreateTranslation(0, 0, -120));

        _silverPyramidModel = new PyramidModelNode(bottomCenterPosition: new Vector3(0, 0, 0), size: new Vector3(80, 50, 80), name: "Silver pyramid")
        {
            Material = _silverPyramidMaterial,
            Transform = _silverPyramidTransform
        };

        scene.RootNode.Add(_silverPyramidModel);
        _animatedModel1 = _silverPyramidModel;


        _redPyramidTransform = new MatrixTransform(Matrix4x4.CreateTranslation(0, 60, -120));

        var redPyramidModel = new PyramidModelNode("Red pyramid")
        {
            Size = new Vector3(80, 50, 80),
            Transform = _redPyramidTransform,
            //Material = new StandardMaterial() { DiffuseColor = new Color3(0.8f, 0.0f, 0.0f) }
            //Material = new StandardMaterial(Colors.Red)
            //Material = new StandardMaterial() { DiffuseColor = Colors.Red }
            Material = StandardMaterials.Red
        };

        scene.RootNode.Add(redPyramidModel);


        _sphereTransform = new TranslateTransform(0, 0, 150);
        _sphereGeometryModel = new SphereModelNode("Silver sphere")
        {
            Radius = 20,
            Material = _silverPyramidMaterial,
            Transform = _sphereTransform
        };

        scene.RootNode.Add(_sphereGeometryModel);


        _specialMaterialGroup = new GroupNode("SpecialMaterialsGroup");
        _specialMaterialGroupTransform = new TranslateTransform(300, 0, -100);
        _specialMaterialGroup.Transform = _specialMaterialGroupTransform;

        scene.RootNode.Add(_specialMaterialGroup);

        var geometryModel3 = new BoxModelNode(centerPosition: new Vector3(0, 0, -200), size: new Vector3(80, 80, 40), "Green transparent box")
        {
            Material = new StandardMaterial() { DiffuseColor = Colors.Green.ToColor3(), Opacity = 0.7f, HasTransparency = true },
            UseSharedBoxMesh = true // this is also a default value (a shared 1x1x1 mesh is by default used to for all boxes; then this mesh is transformed to be positioned and scaled based on centerPosition and Size
        };

        _specialMaterialGroup.Add(geometryModel3);


        // Using properties to define CenterPosition and Size
        var geometryModel4 = new BoxModelNode("Blue transparent box")
        {
            Position = new Vector3(0, 0, -100),
            PositionType = PositionTypes.Center,
            Size = new Vector3(80, 80, 40),
            Material = new StandardMaterial() { DiffuseColor = Colors.Blue.ToColor3(), Opacity = 0.7f, HasTransparency = true },
        };

        _specialMaterialGroup.Add(geometryModel4);



        // Create box model with first creating box mesh and then using MeshModelNode:
        var redBoxMesh = MeshFactory.CreateBoxMesh(centerPosition: new Vector3(0, 0, 0), size: new Vector3(80, 80, 40));
        var geometryModel5 = new MeshModelNode(redBoxMesh, "Gray transparent box")
        {
            Material = new StandardMaterial() { DiffuseColor = Colors.Gray.ToColor3(), Opacity = 0.7f, HasTransparency = true },
        };

        _specialMaterialGroup.Add(geometryModel5);


        // Again create a box mesh but this time scale and position it to its final position and size
        var standardBoxMesh = MeshFactory.CreateBoxMesh(centerPosition: new Vector3(0, 0, 0), size: new Vector3(1, 1, 1));

        var geometryModel6 = new MeshModelNode(standardBoxMesh, "Red BackMaterial box")
        {
            BackMaterial = new StandardMaterial(Colors.Red),
            Transform = new StandardTransform(0, 0, 100, scale: 80) { ScaleZ = 40 }
        };

        _specialMaterialGroup.Add(geometryModel6);


        var texturedMaterialGroup = new GroupNode("TexturedMaterials");
        texturedMaterialGroup.Transform = new TranslateTransform(450, 0, -300);

        scene.RootNode.Add(texturedMaterialGroup);


        // Add textured sample when we support reading texture files
        if (BitmapIO.IsFileFormatImportSupported("jpg") && BitmapIO.IsFileFormatImportSupported("png"))
        {
            if (gpuDevice != null)
            {
                // Manually load texture with TextureLoader (this way we can set generateMipMaps to false)
                var textureWithoutMipMaps = TextureLoader.CreateTexture(@"Resources\Textures\10x10-texture.png", gpuDevice, BitmapIO, generateMipMaps: false, useGpuDeviceCache: false); // do not cache this special no-mips texture (this would prevent caching 10x10-texture.png that is loaded in a standard way)
                _disposables.Add(textureWithoutMipMaps);

                var geometryModel7 = new BoxModelNode(centerPosition: new Vector3(0, 0, 0), size: new Vector3(80, 80, 40), "Textured box 1 (nomips)")
                {
                    Material = new StandardMaterial(textureWithoutMipMaps)
                };

                texturedMaterialGroup.Add(geometryModel7);
            }

            // The most simple way to create texture is to set file name to StandardMaterial constructor (this can also lazy load the texture if currently GpuDevice is not yet set)
            var textureMaterial = new StandardMaterial(@"Resources\Textures\uvchecker2.jpg", BitmapIO);
            var geometryModel8 = new BoxModelNode(centerPosition: new Vector3(0, 0, 100), size: new Vector3(80, 80, 40), "Textured box 2")
            {
                Material = textureMaterial,
            };

            texturedMaterialGroup.Add(geometryModel8);


            var geometryModel9 = new BoxModelNode(centerPosition: new Vector3(0, 0, 200), size: new Vector3(80, 80, 40), "Textured box 3 (reused 2 material with green filter)")
            {
                Material = new StandardMaterial(new Color3(0.0f, 1f, 0.0f)) // Set color filter to green color only
                {
                    DiffuseTexture = textureMaterial.DiffuseTexture
                },
            };

            texturedMaterialGroup.Add(geometryModel9);


            var geometryModel10 = new BoxModelNode(centerPosition: new Vector3(0, 0, 300), size: new Vector3(80, 80, 40), "Textured box 4")
            {
                Material = new StandardMaterial(@"Resources\Textures\uvchecker.png", BitmapIO),
            };

            texturedMaterialGroup.Add(geometryModel10);
        }

        // TEST crating custom texture
        if (gpuDevice != null)
        {
            var customTexture = CreateCustomTexture(gpuDevice, 256, 128, alphaValue: 1);
            _disposables.Add(customTexture);

            var customTextureMaterial = new StandardMaterial(customTexture, CommonSamplerTypes.Clamp, "CustomTextureMaterial");

            var planeModelNode = new PlaneModelNode("TestCustomTexturePlane")
            {
                Position = new Vector3(0, 100, 0),
                Size = new Vector2(100, 50),
                Normal = new Vector3(0, 0, 1),
                HeightDirection = new Vector3(0, 1, 0),
                Material = customTextureMaterial,
                BackMaterial = StandardMaterials.Black,
            };

            texturedMaterialGroup.Add(planeModelNode);
        }


        var treePlaneMaterial = new StandardMaterial(@"Resources\Textures\TreeTexture.png", BitmapIO);

        for (int i = 0; i < 5; i++)
        {
            StandardMaterial usedMaterial;

            if (i < 2)
            {
                usedMaterial = treePlaneMaterial;
            }
            else
            {
                usedMaterial = (StandardMaterial)treePlaneMaterial.Clone($"TreeTexture_{i}");
                usedMaterial.AlphaClipThreshold = 0.2f * (i - 1);
            }

            var treePlane = new PlaneModelNode($"TreePlane_{i}")
            {
                Position = new Vector3(-400 + i * 50, 20, 350),
                Size = new Vector2(100, 150),
                Normal = new Vector3(1, 0, 0),
                HeightDirection = new Vector3(0, 1, 0),
                Material = usedMaterial
            };

            treePlane.BackMaterial = treePlane.Material;

            scene.RootNode.Add(treePlane);
        }


        //// Test code that created a texture material that is never used and will be disposed in finalizer:
        //var imageFileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\Textures\uvchecker.png");
        //var imageData = _bitmapIO.LoadBitmap(imageFileName);
        //var gpuImage = new GpuImage(_vulkanDevice, imageData, generateMipMaps: true, isDeviceLocal: true, imageSource: imageFileName);
        //var gpuTexture = new GpuTexture(gpuImage);
        //var materialToBeDisposedInDestructor = new StandardMaterial() { DiffuseTexture = gpuTexture };

        // 
        // Test VertexColorMaterial
        //
        var boxMesh = MeshFactory.CreateBoxMesh(centerPosition: new Vector3(0, 0, 0), size: new Vector3(1, 1, 1));

        var positionsCount = boxMesh.Vertices!.Length;
        var positionColors = new Color4[positionsCount];

        var boundsPosition = boxMesh.BoundingBox.Minimum;
        var boundsSize = boxMesh.BoundingBox.Maximum - boxMesh.BoundingBox.Minimum;

        for (int i = 0; i < positionsCount; i++)
        {
            var position = boxMesh.Vertices[i].Position;

            // Get colors based on the relative position inside the Bounds - in range from (0, 0, 0) to (1, 1, 1)
            float red = (position.X - boundsPosition.X) / boundsSize.X;
            float green = (position.Y - boundsPosition.Y) / boundsSize.Y;
            float blue = (position.Z - boundsPosition.Z) / boundsSize.Z;

            // Set Color this position
            positionColors[i] = new Color4(red, green, blue, alpha: 1.0f);
        }

        _vertexColorMaterial = new VertexColorMaterial(positionColors, "TestVertexColorMaterial");

        _vertexColorModel = new MeshModelNode(boxMesh, "VertexColorBox")
        {
            Material = _vertexColorMaterial,
            Transform = new StandardTransform(0, 0, -360, scale: 80) { ScaleZ = 60 }
        };

        scene.RootNode.Add(_vertexColorModel);

        // 
        // VertexColorMaterial with transparency
        //
        var transparentPositionColors = positionColors.ToArray();
        for (int i = 0; i < positionsCount; i++)
            transparentPositionColors[i].Alpha = 0.3f;

        var vertexColorMaterial2 = new VertexColorMaterial(transparentPositionColors, "VertexColorMaterial-transparent");

        var vertexColorModel2 = new BoxModelNode(centerPosition: new Vector3(0, 0, -280), size: new Vector3(80, 80, 60), "VertexColorBox-transparent")
        {
            Material = vertexColorMaterial2,
        };

        scene.RootNode.Add(vertexColorModel2);


        // 
        // SolidColorMaterial
        //
        var solidColorMaterial = new SolidColorMaterial(new Color3(0.3f, 0.9f, 0.3f));

        // We could also render solid color by using StandardMaterial and then setting Effect to SolidColorEffect:
        //var solidColorMaterial = StandardMaterials.Orange;
        //var solidColorMaterial = new StandardMaterial(Colors.Orange, "SolidColorMaterial");
        //
        //// ... and then change the default effect that is used to render that material to a SolidColorEffect
        //solidColorMaterial.Effect = scene.EffectsManager.GetDefault<SolidColorEffect>();

        var solidColorModel = new BoxModelNode(centerPosition: new Vector3(120, 0, -360), size: new Vector3(80, 80, 60), "SolidColorModel")
        {
            Material = solidColorMaterial,
        };

        scene.RootNode.Add(solidColorModel);


        // 
        // SolidColorMaterial with texture
        //
        var solidColorMaterial2 = new SolidColorMaterial(@"Resources\Textures\10x10-texture.png", BitmapIO);
        var solidColorModel2 = new BoxModelNode(centerPosition: new Vector3(120, 0, -280), size: new Vector3(80, 80, 60), "SolidColorModel-withTexture")
        {
            Material = solidColorMaterial2,
        };

        scene.RootNode.Add(solidColorModel2);


        // 
        // Custom SolidColorEffect with texture
        //

        // First check if this custom effect was already created before
        SolidColorEffect? customSolidColorEffect = scene.EffectsManager.Get<SolidColorEffect>("CustomSolidColorEffect");

        if (customSolidColorEffect == null)
        {
            // ... if not, then create the effect now
            (customSolidColorEffect, var customSolidColorEffectDisposeToken) = scene.EffectsManager.CreateNew<SolidColorEffect>("CustomSolidColorEffect");

            // Because we have manually created a new instance of an Effect, we are the owners of that effect and therefore we are responsible
            // for the lifecylce of that object. The created Effect does not have a Dispose method, so another "user" of the Effect cannot
            // dispose it (and ruin our part of the code). So to dispose the Effect we also get a DisposeToken that can be used to dispose the Effect.
            // We store that DisposeToken to our _disposables list
            _disposables.Add(customSolidColorEffectDisposeToken);
        }


        customSolidColorEffect.OverrideColor = new Color4(0.3f, 1f, 0.3f, 0.5f);

        var solidColorMaterial3 = new StandardMaterial(@"Resources\Textures\10x10-texture.png", BitmapIO);
        solidColorMaterial3.Effect = customSolidColorEffect;

        var solidColorModel3 = new BoxModelNode(centerPosition: new Vector3(120, 0, -200), size: new Vector3(80, 80, 60), "SolidColorModel-withTexture")
        {
            Material = solidColorMaterial3,
            CustomRenderingLayer = scene.TransparentRenderingLayer // when setting OverrideAlpha to a value less than 1 you may also need to set CustomRenderingLayer TransparentRenderingLayer for the objects to be used in transparency sorting.
        };

        scene.RootNode.Add(solidColorModel3);


        // BoxModel3D
        for (int i = 1; i <= 5; i++)
        {
            var boxModel3D = new BoxModelNode($"BoxModel3D_{i}")
            {
                Position = new Vector3(-100, 200 - i * 40, -280),
                Size = new Vector3(i * 10, 20, i * 10),
                Material = new StandardMaterial(new Color3(i * 0.15f, 1f, i * 0.15f)),
                UseSharedBoxMesh = i % 2 == 0
            };

            scene.RootNode.Add(boxModel3D);
        }


        // SphereModel3D
        for (int i = 1; i <= 5; i++)
        {
            var sphereModel3D = new SphereModelNode($"SphereModel3D_{i}")
            {
                CenterPosition = new Vector3(-100, 200 - i * 40, -360),
                Radius = (i + 1) * 3,
                Material = new StandardMaterial(new Color3(i * 0.15f, 1f, i * 0.15f)),
                UseSharedSphereMesh = i % 2 == 0
            };

            scene.RootNode.Add(sphereModel3D);
        }


        // ThickLineEffect

        var simpleSphereMesh = MeshFactory.CreateSphereMesh(new Vector3(0, 0, 0), 40, 6);

        _thickLineOverrideMaterial = new StandardMaterial(new Color3(0.3f, 0.3f, 0.9f), "StandardMaterial-with-ThickLineEffect");
        _thickLineOverrideMaterial.Effect = scene.EffectsManager.GetDefault<ThickLineEffect>();

        _thickLineOverrideModel = new MeshModelNode(simpleSphereMesh, "ThickLineModel")
        {
            Material = _thickLineOverrideMaterial,
            Transform = new StandardTransform(-200, 0, -200, scale: 1)
        };

        scene.RootNode.Add(_thickLineOverrideModel);


        // Show wireframe positions
        var sphereWireframePositions = LineUtils.GetWireframeLinePositions(simpleSphereMesh, removedDuplicateLines: true); // remove duplicate lines at the edges of triangles

        var wireframeLineNode = new MultiLineNode(sphereWireframePositions, isLineStrip: false, Color3.Black, 0.4f, "WireframeLine")
        {
            Transform = new TranslateTransform(-200, 0, -100)
        };

        scene.RootNode.Add(wireframeLineNode);


        // First check if this custom effect was already created before
        ThickLineEffect? customThickLineEffect = scene.EffectsManager.Get<ThickLineEffect>("CustomThickLineEffect");

        if (customThickLineEffect == null)
        {
            // ... if not, then create the effect now
            (customThickLineEffect, var customThickLineEffectDisposeToken) = scene.EffectsManager.CreateNew<ThickLineEffect>("CustomThickLineEffect");

            // Because we have manually created a new instance of an Effect, we are the owners of that effect and therefore we are responsible
            // for the lifecylce of that object. The created Effect does not have a Dispose method, so another "user" of the Effect cannot
            // dispose it (and ruin our part of the code). So to dispose the Effect we also get a DisposeToken that can be used to dispose the Effect.
            // We store that DisposeToken to our _disposables list
            _disposables.Add(customThickLineEffectDisposeToken);
        }

        // Because we have a custom effect, we can override the material properties - those will be used to render all objects with that effect.
        customThickLineEffect.OverrideLineThickness = 2f;
        customThickLineEffect.OverrideLineColor = Colors.Gold;
        customThickLineEffect.OverrideLineStipplePattern = 0xff00;

        _thickLineOverrideMaterial = new StandardMaterial(new Color3(0.3f, 0.3f, 0.9f), "StandardMaterial-with-CustomThickLineEffect");
        _thickLineOverrideMaterial.Effect = customThickLineEffect;

        _thickLineOverrideModel = new MeshModelNode(simpleSphereMesh, "CustomThickLineModel")
        {
            Material = _thickLineOverrideMaterial,
            Transform = new StandardTransform(-200, 0, -280, scale: 1)
        };

        scene.RootNode.Add(_thickLineOverrideModel);


        _lineMaterial1 = new LineMaterial("LineMaterial1")
        {
            LineColor = new Color4(0.3f, 0.9f, 0.3f, 1.0f),
            LineThickness = 4
        };

        _thickLineModel1 = new MeshModelNode(simpleSphereMesh, "ThickLineModel3")
        {
            Material = _lineMaterial1,
            Transform = new TranslateTransform(-200, 0, -360)
        };

        scene.RootNode.Add(_thickLineModel1);


        // Create a new material because _lineMaterial1 is disposed / recreated in ChangeMaterial2
        var lineMaterial2 = new LineMaterial("LineMaterial2")
        {
            LineColor = new Color4(0.3f, 0.9f, 0.3f, 1.0f),
            LineThickness = 4
        };

        var linePositions = new Vector3[] { new Vector3(0, 0, 0),
                                            new Vector3(0, 30, -30),
                                            new Vector3(0, 30, 0),
                                            new Vector3(0, 60, -30),
                                            new Vector3(0, 60, 0),
                                            new Vector3(0, 90, -30) };

        var lineListMesh1 = new PositionsMesh(linePositions, PrimitiveTopology.LineList, "LineListPositionsMesh1");
        var lineStripMesh1 = new PositionsMesh(linePositions, PrimitiveTopology.LineStrip, "LineStripPositionsMesh1");

        var lineListNode1 = new MeshModelNode(lineListMesh1, "LineListNode1")
        {
            Material = lineMaterial2,
            Transform = new TranslateTransform(-250, 0, -280)
        };

        scene.RootNode.Add(lineListNode1);

        var lineStripNode1 = new MeshModelNode(lineStripMesh1, "LineStripNode1")
        {
            Material = lineMaterial2,
            Transform = new TranslateTransform(-280, 0, -280)
        };

        scene.RootNode.Add(lineStripNode1);


        var line1 = new MultiLineNode(linePositions, isLineStrip: false, lineMaterial2, "Line3D-LineList")
        {
            Transform = new TranslateTransform(-250, 120, -280)
        };

        scene.RootNode.Add(line1);


        var line2 = new MultiLineNode(linePositions, isLineStrip: true, lineMaterial2, "Line3D-LineStrip")
        {
            Transform = new TranslateTransform(-280, 120, -280)
        };

        scene.RootNode.Add(line2);


        var stipplePatterns = new ushort[]
        {
                0b0101010101010101,
                0b0011001100110011,
                0b0000111100001111,
                0b0000000000000001,
                0b0000000000001111,
        };

        for (int i = 0; i < stipplePatterns.Length * 3; i++)
        {
            var lineMaterial = new LineMaterial(new Color3(0.2f, 0.2f, 0.8f), lineThickness: 3);
            lineMaterial.LinePattern = stipplePatterns[i % stipplePatterns.Length];
            lineMaterial.LinePatternScale = 1 + i / stipplePatterns.Length;

            var line = new LineNode(startPosition: new Vector3(0, 0, 0), endPosition: new Vector3(0, 0, -150), lineMaterial, $"Line3D-Stipple_{i}")
            {
                Transform = new TranslateTransform(-320, i * 10, -220)
            };

            scene.RootNode.Add(line);
        }


        float lineThickness = 0.25f;
        for (int i = 0; i < 10; i++)
        {
            var line = new LineNode(startPosition: new Vector3(0, 0, 0), endPosition: new Vector3(0, 0, -150), new Color3(0.2f, 0.8f, 1f), lineThickness, $"Line3D-Thickness_{lineThickness:F2}")
            {
                Transform = new TranslateTransform(-360, 180 - i * 20, -220)
            };

            if (lineThickness < 1)
                lineThickness *= 2;
            else
                lineThickness++;

            scene.RootNode.Add(line);
        }


        
        var text1Position = new Vector3(-400, 150, 100);

        var wireCross3D = new WireCrossNode(text1Position, Colors.Red);
        scene.RootNode.Add(wireCross3D);
        
        
        //
        // Show text
        //

        // Text in SharpEngine is rendered by using bitmap fonts.
        // Bitmap font is defined by one or more textures with rendered characters and font data that define where on the texture the character is.

        // GetDefaultBitmapTextCreator gets the BitmapTextCreator with the default bitmap font that is build into the Ab4d.SharpEngine.
        // The bitmap font is created from Roboto font (Google's Open Font) with size of character set to 64 pixels (see roboto_64 in the Resources\BitmapFonts)
        var normalBitmapTextCreator = BitmapTextCreator.GetDefaultBitmapTextCreator(scene);


        // Create text node
        // Usually this is a MeshModelNode, but in case when bitmap font uses
        // multiple pages (textures) to define font, then a GroupNode is returned 
        // with multiple MeshModelNodes (each with its own texture)
        var textNode1 = normalBitmapTextCreator.CreateTextNode(text: "Text node\r\nwith default font",
                                                               position: text1Position,
                                                               positionType: PositionTypes.BottomLeft,
                                                               textDirection: new Vector3(1, 0, 0),
                                                               upDirection: new Vector3(0, 1, 0),
                                                               fontSize: 50,
                                                               textColor: Colors.Blue,
                                                               isSolidColorMaterial: false);

        scene.RootNode.Add(textNode1);

        // Font is render with "Latin", "Latin-1 Supplement" and "Latin Extended-A" characters and defines many special characters.
        string textWithSpecialChars = "with special chars:\r\n@*?${}@\r\nüůűščžŠČŽ";
        var textNode2 = normalBitmapTextCreator.CreateTextNode(text: textWithSpecialChars,
                                                               position: text1Position,
                                                               positionType: PositionTypes.TopLeft,
                                                               textDirection: new Vector3(0, 0, -1),
                                                               upDirection: new Vector3(0, 1, 0),
                                                               fontSize: 30,
                                                               textColor: Colors.SkyBlue,
                                                               isSolidColorMaterial: false);

        scene.RootNode.Add(textNode2);

        // Measure text size
        var textSize = normalBitmapTextCreator.GetTextSize(textWithSpecialChars, fontSize: 30);


        // Calculate the end position of the text
        // This is done by moving the start position (text1Position) for textDirection from textNode2 multiplied by textSize.X and upDirection multiplied by textSize.Y
        var endTextPosition = text1Position + new Vector3(0, 0, -1) * textSize.X // textNode2.textDirection * textSize.X
                                            - new Vector3(0, 1, 0) * textSize.Y; // textNode2.upDirection * textSize.Y

        var wireCross2 = new WireCrossNode(endTextPosition, Colors.Green, lineLength: 40, lineThickness: 2); // lineLength is 50 by default; lineThickness is 1 by default
        scene.RootNode.Add(wireCross2);


        // Use a custom font (ArialBlack or RobotoBlack)
        //
        // Bitmap fonts in this sample are generated by "Bitmap font generator" from https://www.angelcode.com/products/bmfont/
        // See remarks for BitmapTextCreator.cs file for more info: https://www.ab4d.com/help/SharpEngine/html/T_Ab4d_SharpEngine_Utilities_BitmapTextCreator.htm
        //
        // fnt and png files are copied to output folder into "Resources\BitmapFonts\" folder.
        //
        // Note that custom fonts use characters that are rendered to 128 pixels and are better quality than the build-in font that use 64 pixels.
        // This can be see if you zoom to text.
        string fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\BitmapFonts\");

        // On Windows we are allowed to use Arial font; on other systems use use Roboto font (Google's Open Font)
        bool useArialFont = OperatingSystem.IsWindows();

        string fontFileName = fontPath + (useArialFont ? "arial_black_128.fnt" : "roboto_black_128.fnt");

        //var blackBitmapFont = CreateBitmapFont(fontFileName, BitmapIO);
        var blackBitmapFont = CreateBitmapFont(fontFileName, scene.GpuDevice.DefaultBitmapIO);

        if (blackBitmapFont != null)
        {
            var blackBitmapTextCreator = new BitmapTextCreator(scene, blackBitmapFont, scene.GpuDevice.DefaultBitmapIO)
            {
                CacheFontGpuImages = false // Do not cache the font bitmaps for black font version (this will also dispose the font bitmaps when this sample is not shown any more)
            };

            _disposables.Add(blackBitmapTextCreator);

            var textNode3 = blackBitmapTextCreator.CreateTextNode(text: "Bolder text node",
                                                                  position: text1Position - new Vector3(0, textSize.Y + 20, 0),
                                                                  positionType: PositionTypes.TopLeft,
                                                                  textDirection: new Vector3(1, 0, 0),
                                                                  upDirection: new Vector3(0, 1, 0),
                                                                  fontSize: 50,
                                                                  textColor: Colors.Orange,
                                                                  isSolidColorMaterial: false);

            scene.RootNode.Add(textNode3);
        }


        // It is possible to generate bitmap font with outline (using "Outlined text with alpha" preset in Bitmap font generator)
        // Note that outline is always black; we can set the color of the font.
        // Also set isSolidColorMaterial to true. This render the font with the specified color regardless of the lights and text angle.
        // To see the difference in color compare this text with the ArialBlack text (defined above)
        fontFileName = fontPath + (useArialFont ? "arial_black_with_outline_128.fnt" : "roboto_black_with_outline_128.fnt");

        var blackWithOutlineBitmapFont = CreateBitmapFont(fontFileName, BitmapIO);

        if (blackWithOutlineBitmapFont != null)
        {
            var blackWithOutlineBitmapTextCreator = new BitmapTextCreator(scene, blackWithOutlineBitmapFont, scene.GpuDevice.DefaultBitmapIO)
            {
                AdditionalLineSpace = -20, // Decrease default line space
                CacheFontGpuImages = false // Do not cache the font bitmaps for black_with_outline font version (this will also dispose the font bitmaps when this sample is not shown any more)
            };

            _disposables.Add(blackWithOutlineBitmapTextCreator);

            var textNode4 = blackWithOutlineBitmapTextCreator.CreateTextNode(text: "Text with outline and\r\nSolidColorMaterial",
                                                                             position: text1Position - new Vector3(0, textSize.Y - 20, 0),
                                                                             positionType: PositionTypes.TopRight,
                                                                             textDirection: new Vector3(1, 0, 0),
                                                                             upDirection: new Vector3(0, 1, 0),
                                                                             fontSize: 40,
                                                                             textColor: Colors.Orange,
                                                                             isSolidColorMaterial: true); // Using solid color material

            scene.RootNode.Add(textNode4);
        }


        // Add InstancedMeshNode that can show many instances of the same mesh (sphere in this sample).
        // The color, position, size and orientation of each instance is defined by instancesData.
        var sphereMesh = MeshFactory.CreateSphereMesh(centerPosition: new Vector3(0, 0, 0), radius: 2, segments: 30);

        var instancedMeshNode = new InstancedMeshNode("InstancedMeshNode");
        instancedMeshNode.Mesh = sphereMesh;

        var instancesData = CreateInstancesData(center: new Vector3(150, 0, 100),
                                                                    size: new Vector3(100, 40, 100),
                                                                    modelScaleFactor: 1,
                                                                    xCount: 10, yCount: 4, zCount: 10,
                                                                    useTransparency: false);

        instancedMeshNode.SetInstancesData(instancesData);

        scene.RootNode.Add(instancedMeshNode);



        // ********** Setup lights group that shows models for lights **********
        _lightsGroup = new GroupNode("LightGroup");
        scene.RootNode.Add(_lightsGroup);

        _additionalObjectsGroup = new GroupNode("AdditionalObjectsGroup");
        scene.RootNode.Add(_additionalObjectsGroup);

#if ADVANCED_TIME_MEASUREMENT
        System.Diagnostics.Debug.WriteLine($"OnCreateScene METHOD TIME: {(DateTime.Now - _startTime).TotalMilliseconds} ms");
#endif
    }

    protected override void OnCreateLights(Scene scene)
    {
        scene.Lights.Clear();

        // Add lights
        scene.SetAmbientLight(intensity: 0.3f);

        var directionalLight = new DirectionalLight(new Vector3(-1, -0.3f, 0));
        scene.Lights.Add(directionalLight);

        scene.Lights.Add(new PointLight(new Vector3(100, 0, 100), range: 10000) { Attenuation = new Vector3(1, 0, 0) });

        //float att = 1f / (200 * 200);
        //_scene.Lights.Add(new PointLight(new Vector3(-200, 0, 300), 200) { Attenuation = new Vector3(0, 0, att)});
        //_scene.Lights.Add(new PointLight(new Vector3(-200, 0, 300), 200) { Attenuation = new Vector3(0, 0.05f, 0)});
        scene.Lights.Add(new SpotLight(new Vector3(300, 0, 300), new Vector3(-1, -0.3f, 0)) { Color = new Color3(0.4f, 0.4f, 0.4f) });
    }

    private void UpdateModelWorldMatrices(object? sender, EventArgs e)
    {
        if (!_isAnimatingScene)
            return;

        if (_animatedModel1 == null)
            return;


        if (_initialTimestamp == 0)
            _initialTimestamp = Stopwatch.GetTimestamp();

        long currentTimestamp = Stopwatch.GetTimestamp();
        float totalTime = (currentTimestamp - _initialTimestamp) / (float)Stopwatch.Frequency;

        var world = Matrix4x4.CreateRotationY(MathF.Sin(totalTime) * MathF.PI);
        world.M43 = -120f;

        if (_silverPyramidTransform != null)
            _silverPyramidTransform.SetMatrix(ref world);

        if (_redPyramidTransform != null)
        {
            world = Matrix4x4.CreateRotationY(MathF.Sin(totalTime + 1) * MathF.PI);
            world.M42 = 60f;
            world.M43 = -120f;

            _redPyramidTransform.SetMatrix(ref world);
        }

        if (_specialMaterialGroupTransform != null)
            _specialMaterialGroupTransform.Y = MathF.Sin(totalTime * 2) * 20 + 15;
    }

    private GpuImage CreateCustomTexture(VulkanDevice gpuDevice, int width, int height, float alphaValue)
    {
        int imageStride = width * 4;
        var imageBytes = new byte[imageStride * height];

        float widthFactor = 255.0f / width;
        float heightFactor = 255.0f / height;

        byte red, green, blue;
        green = 0;

        byte alpha = (byte)(255 * alphaValue);

        for (int y = 0; y < height; y++)
        {
            int pos = y * imageStride;

            red = (byte)(y * heightFactor);

            // Duplicate for loop to remove multiplying by alphaValue in non-transparent code path
            if (alphaValue < 1)
            {
                for (int x = 0; x < width; x++)
                {
                    blue = (byte)(x * widthFactor);

                    // we have B8G8R8A8 format and memory layout
                    // In case of using alpha we need to convert to pre-multiplied alpha value
                    imageBytes[pos] = (byte)(blue * alphaValue);
                    imageBytes[pos + 1] = (byte)(green * alphaValue);
                    imageBytes[pos + 2] = (byte)(red * alphaValue);
                    imageBytes[pos + 3] = alpha; // alpha

                    pos += 4;
                }
            }
            else
            {
                for (int x = 0; x < width; x++)
                {
                    blue = (byte)(x * widthFactor);

                    // we have B8G8R8A8 format and memory layout
                    imageBytes[pos] = blue;
                    imageBytes[pos + 1] = green;
                    imageBytes[pos + 2] = red;
                    imageBytes[pos + 3] = alpha; // alpha

                    pos += 4;
                }
            }
        }

        var rawImageData = new RawImageData(width, height, imageStride, Format.B8G8R8A8Unorm, imageBytes, checkTransparency: false);

        var gpuImage = new GpuImage(gpuDevice, rawImageData, generateMipMaps: true, imageSource: "CustomTexture")
        {
            IsPreMultipliedAlpha = true,
            HasTransparentPixels = alphaValue < 1,
        };

        return gpuImage;
    }

    private static BitmapFont? CreateBitmapFont(string fontFileName, IBitmapIO bitmapIO)
    {
        string? usedFileName = null;
        Stream? fileStream = null;

#if ADVANCED_TIME_MEASUREMENT
        var startTime = DateTime.Now;
#endif

        fontFileName = FileUtils.FixDirectorySeparator(fontFileName); // use slash or backslash as folder separator depanding on the OS

        if (File.Exists(fontFileName))
        {
            usedFileName = fontFileName;
        }
        else
        {
            if (bitmapIO.FileStreamResolver != null) // if stream resolved exists, then try to get the stream from the file name (note that this is the only way to resolve files on Android)
                fileStream = bitmapIO.FileStreamResolver(fontFileName);

            if (fileStream == null && bitmapIO.FileNotFoundResolver != null)
            {
                usedFileName = bitmapIO.FileNotFoundResolver(fontFileName); // try to resolve the path to the file

                if (usedFileName != null && !File.Exists(fontFileName))
                    usedFileName = null;
            }
        }

        BitmapFont? arialBitmapFont = null;
        if (usedFileName != null || fileStream != null)
        {
            if (fileStream != null)
            {
                arialBitmapFont = new BitmapFont(fileStream); // load from stream
                fileStream.Close();
            }
            else if (usedFileName != null)
            {
                arialBitmapFont = new BitmapFont(usedFileName);
            }
        }

#if ADVANCED_TIME_MEASUREMENT
        _loadBitmapFontsTime += (DateTime.Now - startTime).TotalMilliseconds;
#endif

        return arialBitmapFont;
    }

    public static WorldColorInstanceData[] CreateInstancesData(Vector3 center, Vector3 size, float modelScaleFactor, int xCount, int yCount, int zCount, bool useTransparency)
    {
        var instancedData = new WorldColorInstanceData[xCount * yCount * zCount];

        float xStep = xCount <= 1 ? 0 : (float)(size.X / (xCount - 1));
        float yStep = yCount <= 1 ? 0 : (float)(size.Y / (yCount - 1));
        float zStep = zCount <= 1 ? 0 : (float)(size.Z / (zCount - 1));

        int i = 0;
        for (int z = 0; z < zCount; z++)
        {
            float zPos = (float)(center.Z - size.Z / 2.0 + z * zStep);
            float zPercent = z / (float)zCount;

            for (int y = 0; y < yCount; y++)
            {
                float yPos = (float)(center.Y - size.Y / 2.0 + y * yStep);
                float yPercent = y / (float)yCount;

                for (int x = 0; x < xCount; x++)
                {
                    float xPos = (float)(center.X - size.X / 2.0 + x * xStep);

                    instancedData[i].World = new Matrix4x4(modelScaleFactor, 0, 0, 0,
                                                           0, modelScaleFactor, 0, 0,
                                                           0, 0, modelScaleFactor, 0,
                                                           xPos, yPos, zPos, 1);

                    if (useTransparency)
                    {
                        // When we use transparency, we set alpha color to 0.2 (we also need to set InstancedMeshGeometryVisual3D.UseAlphaBlend to true)
                        instancedData[i].DiffuseColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f - yPercent); // White with variable transparency - top objects fully transparent, bottom objects solid
                    }
                    else
                    {
                        // Start with yellow and move to white (multiplied by 1.4 so that white color appear before the top)
                        //instancedData[i].DiffuseColor = new Color4(red: 1.0f,
                        //                                           green: 1.0f,
                        //                                           blue: yPercent * 1.4f,
                        //                                           alpha: 1.0f);

                        instancedData[i].DiffuseColor = new Color4(red: 0.3f + x / (float)xCount * 0.7f,
                                                                   green: 0.3f + yPercent * 0.7f,
                                                                   blue: 0.3f + zPercent * 0.7f,
                                                                   alpha: 1.0f);

                        //if (yPercent > 0.4f && yPercent < 0.7f)
                        //    instancedData[i].DiffuseColor = new Color4(instancedData[i].DiffuseColor.Red, instancedData[i].DiffuseColor.Green, instancedData[i].DiffuseColor.Blue, 0);
                    }

                    i++;
                }
            }
        }

        return instancedData;
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateCheckBox("Is animating", true, isChecked => _isAnimatingScene = isChecked);
    }
}