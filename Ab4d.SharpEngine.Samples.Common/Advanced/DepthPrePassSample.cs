using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

public class DepthPrePassSample : CommonSample
{
    public override string Title => "Depth pre-pass rendering";
    public override string Subtitle => "This sample uses DepthOnlyMaterial that writes the depth values for the 3D scene before the main rendering occurs. This is known as depth pre-pass (or z pre-pass). After that rendering pass, the final depth values for each screen pixel are known and because of this all the fragment shaders executions that are now known to be behind some other object (have bigger depth values) are skipped. This can significantly improve the rendering performance.\n\nIn this sample some screen pixels would require more than 50 fragment shader executions before the final pixel color is determined. By using the depth pre-pass, only a single fragment shader is executed for each screen pixel.";
 
    public DepthPrePassSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        // Create many sphere meshes and store individual meshes so they can be combined later.
        int spheresCount = 500;
        int radius = 20;
        int sphereSegments = 20;
        
        var allMeshes = new List<StandardMesh>(spheresCount);

        for (int i = 0; i < spheresCount; i++)
        {
            var randomColor = GetRandomHsvColor3();
            var randomMaterial = new StandardMaterial(randomColor) { SpecularPower = 128, SpecularColor = Color3.White };
            var randomPosition = GetRandomPosition(centerPosition: new Vector3(0, -50, 0), areaSize: new Vector3(200, 200, 200));

            var oneSphereMesh = Meshes.MeshFactory.CreateSphereMesh(centerPosition: randomPosition, radius, sphereSegments);

            var sphereModel = new MeshModelNode(oneSphereMesh, randomMaterial);
            scene.RootNode.Add(sphereModel);

            allMeshes.Add(oneSphereMesh);
        }
        
        // Because rendering depth pre-pass does not require any colors or other special per-sphere settings,
        // we can improve the rendering performance by combining all the sphere meshes into one mesh.
        // This way the whole depth pre-pass can be rendered by one draw call.
        // Because only vertex shaders are executed in this pass (no fragment shaders), this is executed very quickly
        // and skips executing many much more expensive fragment shaders that would be otherwise executed when rendering MeshModelNode objects.
        var combinedSphereMesh = Ab4d.SharpEngine.Utilities.MeshUtils.CombineMeshes(allMeshes);
        
        // To render the depth pre-pass we use the DepthOnlyMaterial.
        // This material has only one property: IsTwoSided - when true the triangles will be rendered from both sides.
        var depthOnlyMaterial = new DepthOnlyMaterial();
        depthOnlyMaterial.IsTwoSided = false;

        // We make use of the depth pre-pass we also need to make sure that it is rendered before other objects.
        // The easies way to achieve that is to set CustomRenderingLayer to BackgroundRenderingLayer.
        var depthOnlyModelNode = new MeshModelNode(combinedSphereMesh, depthOnlyMaterial)
        {
            CustomRenderingLayer = scene.BackgroundRenderingLayer
        };
        
        scene.RootNode.Add(depthOnlyModelNode);
        
        

        // Add 16 random lights to make the fragment shaders more expensive to execute
        for (int i = 0; i < 32; i++)
        {
            //var randomQuaternion = Quaternion.CreateFromYawPitchRoll(yaw: GetRandomFloat() * 2 * MathF.PI, pitch: GetRandomFloat() * 2 * MathF.PI, roll: GetRandomFloat() * 2 * MathF.PI);
            //var randomLightPosition = Vector3.Transform(new Vector3(1000, 0, 0), randomQuaternion);

            var randomLightPosition = GetRandomPosition(centerPosition: new Vector3(0, 0, 0), areaSize: new Vector3(1000, 2000, 1000));
            var pointLight = new PointLight(randomLightPosition);
            scene.Lights.Add(pointLight);
        }

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 30;
            targetPositionCamera.Attitude = -15;
            targetPositionCamera.Distance= 800;
            
            targetPositionCamera.StartRotation(headingChangeInSecond: 30);
        }
    }
}