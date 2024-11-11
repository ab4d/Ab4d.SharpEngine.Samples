using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class SkyboxSample : CommonSample
{
    public override string Title => "Skybox created with MultiMaterialModelNode";

    private ManualPointerCameraController? _pointerCameraController;

    public SkyboxSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        // To create a skybox effect, we create a MultiMaterialModelNode with box mesh 
        // and assign correct skybox texture to each of the box sides.
        // We use BackMaterial so the texture is visible inside the box.

        var boxMesh = MeshFactory.CreateBoxMesh(new Vector3(0, 0, 0), new Vector3(500, 500, 500), 1, 1, 1);

        var multiMaterialModelNode = new MultiMaterialModelNode(boxMesh, "SkyboxMultiMaterialModelNode");

        // The first parameter to AddSubMesh is startIndexLocation and the next is indexCount
        // We leave the material parameter with null value
        multiMaterialModelNode.AddSubMesh(0,  6, backMaterial: GetSkyboxMaterial("Up"));
        multiMaterialModelNode.AddSubMesh(6,  6, backMaterial: GetSkyboxMaterial("Front"));
        multiMaterialModelNode.AddSubMesh(12, 6, backMaterial: GetSkyboxMaterial("Left"));
        multiMaterialModelNode.AddSubMesh(18, 6, backMaterial: GetSkyboxMaterial("Right"));
        multiMaterialModelNode.AddSubMesh(24, 6, backMaterial: GetSkyboxMaterial("Back"));
        multiMaterialModelNode.AddSubMesh(30, 6, backMaterial: GetSkyboxMaterial("Down"));

        scene.RootNode.Add(multiMaterialModelNode);


        var teapotNode = TestScenes.GetTestScene(TestScenes.StandardTestScenes.Teapot, new Vector3(100, 100, 100));

        scene.RootNode.Add(teapotNode);


        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 35;
            targetPositionCamera.Attitude = -14;
            targetPositionCamera.Distance = 200;
        }
    }

    public override void InitializePointerCameraController(ManualPointerCameraController pointerCameraController)
    {
        // Limit camera distance so the user does not go farther away as the size of the SkyBox (500)
        pointerCameraController.MaxCameraDistance = 490;

        _pointerCameraController = pointerCameraController;

        base.InitializePointerCameraController(pointerCameraController);
    }

    private Material? GetSkyboxMaterial(string sideName)
    {
        if (Scene == null || Scene.GpuDevice == null)
            return null;

        var fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Resources/SkyboxTextures/CloudyLightRays{sideName}512.png");

        // We need to create a GpuImage here
        // Creating a lazy-initialized SolidColorMaterial that would get just a file name does not work (yet) with MultiMaterialModelNode
        var texture = TextureLoader.CreateTexture(fileName, Scene.GpuDevice);

        // Use SolidColorMaterial so there is no shading based on lights
        return new SolidColorMaterial(texture);
    }

    /// <inheritdoc />
    protected override void OnDisposed()
    {
        if (_pointerCameraController != null)
            _pointerCameraController.MaxCameraDistance = float.NaN; // Reset the MaxCameraDistance

        base.OnDisposed();
    }
}

