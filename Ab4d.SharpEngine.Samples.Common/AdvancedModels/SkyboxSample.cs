using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class SkyboxSample : CommonSample
{
    public override string Title => "Skybox created with MultiMaterialModelNode";

    private ManualPointerCameraController? _pointerCameraController;
    private TranslateTransform? _skyboxTransform;

    public SkyboxSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override async Task OnCreateSceneAsync(Scene scene)
    {
        SetupSkybox(scene);

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 35;
            targetPositionCamera.Attitude = -14;
            targetPositionCamera.Distance = 200;
        }

        await base.ShowCommonSceneAsync(scene, CommonScenes.Teapot, finalSize: new Vector3(100, 100, 100));
    }

    private void SetupSkybox(Scene scene)
    {
        // To create a skybox effect, we create a MultiMaterialModelNode with a special box mesh
        // and assign correct skybox texture to each of the box sides.

        // CreateSkyBoxMesh creates a box mesh with inverted normals, adjusted texture coordinates and flipped triangle order to correctly show the sky box
        var skyBoxMesh = MeshFactory.CreateSkyBoxMesh(size: 5000);

        var skyBoxNode = new MultiMaterialModelNode(skyBoxMesh, "SkyboxMultiMaterialModelNode")
        {
            IsHitTestVisible = false // IMPORTANT: Disable hit testing to prevent rotating around positions on the skybox and zooming to skybox
        };

        // The textures for the sky box are correctly visible only when the camera is positioned in the center of the sky box.
        // But because we render all the objects in the scene with the same camera, we cannot move the camera only for the sky box.
        // To solve this, we update the position of the sky box node in each frame so it is always centered on the camera position.
        // This way we achieve the same effect as we would move the camera to the center of the sky box, but without actually moving the camera.
        // The _skyboxTransform is updated in the OnCameraChanged event handler that is called each time the camera moves.
        _skyboxTransform = new TranslateTransform(targetPositionCamera?.GetCameraPosition() ?? new Vector3(0, 0, 0));
        skyBoxNode.Transform = _skyboxTransform;

        if (scene.BackgroundRenderingLayer != null)
        {
            // Set the CustomRenderingLayer of the skybox node to the BackgroundRenderingLayer so it is rendered before other 3D objects.
            skyBoxNode.CustomRenderingLayer = scene.BackgroundRenderingLayer;

            // Clear the depth buffer after rendering the skybox so the rest of the scene would render correctly in front of the skybox
            scene.BackgroundRenderingLayer.ClearDepthStencilBufferAfterRendering = true; 
        }

        // Now set the sky box textures for each of the 6 sides of the box mesh.

        // See Materials/PhysicallyBasedMaterialSample to see how to create sky box from a cube map GpuImage.

        // If we do not have a cube map GpuImage, then we can manually create the materials for each of the sky box sides
        // by loading the textures from files and creating GpuImages for each of them.

        // The first parameter to AddSubMesh is startIndexLocation and the next is indexCount
        // We leave the material parameter with null value and only set backMaterial.
        // This can be done with the following code:

        skyBoxNode.AddSubMesh(0, 6,  material: GetSkyboxFaceMaterial("_posy"));
        skyBoxNode.AddSubMesh(6, 6,  material: GetSkyboxFaceMaterial("_posz"));
        skyBoxNode.AddSubMesh(12, 6, material: GetSkyboxFaceMaterial("_negx"));
        skyBoxNode.AddSubMesh(18, 6, material: GetSkyboxFaceMaterial("_posx"));
        skyBoxNode.AddSubMesh(24, 6, material: GetSkyboxFaceMaterial("_negz"));
        skyBoxNode.AddSubMesh(30, 6, material: GetSkyboxFaceMaterial("_negy"));

        scene.RootNode.Add(skyBoxNode);


        // Subscribe to camera changes to update the _skyboxTransform
        if (targetPositionCamera != null)
            targetPositionCamera.CameraChanged += OnCameraChanged;

        // IMPORTANT:
        // Do not forget to unsubscribe from CameraChanged
    }

    public override void InitializePointerCameraController(ManualPointerCameraController pointerCameraController)
    {
        // Limit camera distance so the user does not go farther away as the size of the SkyBox (500)
        pointerCameraController.MaxCameraDistance = 490;

        _pointerCameraController = pointerCameraController;

        base.InitializePointerCameraController(pointerCameraController);
    }

    private Material? GetSkyboxFaceMaterial(string sideName)
    {
        if (Scene == null || Scene.GpuDevice == null)
            return null;

        var fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Resources/Textures/CloudyLightRays/CloudyLightRays{sideName}.png");

        // We need to create a GpuImage here
        // Creating a lazy-initialized SolidColorMaterial that would get just a file name does not work (yet) with MultiMaterialModelNode
        var texture = TextureLoader.CreateTexture(fileName, Scene.GpuDevice);

        // Use SolidColorMaterial so there is no shading based on lights
        return new SolidColorMaterial(texture);
    }

    private void OnCameraChanged(object? sender, EventArgs e)
    {
        if (targetPositionCamera == null || _skyboxTransform == null)
            return;

        // Update the position of the sky box so it is always centered on the camera position.
        // See comment in the OnCreateSceneAsync for more info.
        var cameraPosition = targetPositionCamera.GetCameraPosition();

        // Only update the transformation when the camera position is changed.
        
        // ReSharper disable CompareOfFloatsByEqualityOperator
        if (cameraPosition.X != _skyboxTransform.X || cameraPosition.Y != _skyboxTransform.Y || cameraPosition.Z != _skyboxTransform.Z)
            _skyboxTransform.SetTranslate(cameraPosition);
        // ReSharper restore CompareOfFloatsByEqualityOperator
    }

    /// <inheritdoc />
    protected override void OnDisposed()
    {
        if (_pointerCameraController != null)
            _pointerCameraController.MaxCameraDistance = float.NaN; // Reset the MaxCameraDistance

        // Unsubscribe from CameraChanged - there we updated the position of the sky box to always be centered on the camera position.
        if (targetPositionCamera != null)
            targetPositionCamera.CameraChanged -= OnCameraChanged;

        base.OnDisposed();
    }
}

