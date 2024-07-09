using Ab4d.SharpEngine.Effects;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.RenderingSteps;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

public class PostProcessSample : CommonSample
{
    public override string Title => "Post processing";
    public override string? Subtitle => string.Empty;

    public PostProcessSample(ICommonSamplesContext context)
        : base(context)
    {
        var resourceAssembly = GetType().Assembly;
        ShadersManager.RegisterShaderResourceStatic(
            new AssemblyShaderBytecodeProvider(
                resourceAssembly,
                resourceAssembly.GetName().Name + ".Resources.Shaders.spv."));
    }

    protected override void OnCreateScene(Scene scene)
    {
        scene.RootNode.Add(new BoxModelNode(Vector3.Zero, Vector3.One * 100, StandardMaterials.Gold));
    }

    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        // todo:
        // pass in the proper sampler and image view (PostProcessRenderingStep l.293)
        // dispose things and free memory where possible in PostProcessRenderingStep

        var beginRenderPassRenderingStep = new BeginRenderPassRenderingStep(sceneView, "PostProcessSample-BeginRenderingPass");
        var postProcessRenderingStep = new PostProcessRenderingStep("PostProcessSampleShader.vert.spv", "PostProcessSampleShader.frag.spv", sceneView);
        var completeRenderingStep = new CompleteRenderingStep(sceneView, "PostProcessSample-CompleteRenderPass");
        sceneView.RenderingSteps.Add(
            beginRenderPassRenderingStep,
            postProcessRenderingStep,
            completeRenderingStep);

        base.OnSceneViewInitialized(sceneView);
    }
}