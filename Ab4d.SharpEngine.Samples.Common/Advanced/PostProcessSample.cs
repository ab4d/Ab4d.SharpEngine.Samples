using Ab4d.SharpEngine.Samples.Common.Effects;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

public class PostProcessSample : CommonSample
{
    public override string Title => "Post processing";
    public override string? Subtitle => string.Empty;

    public PostProcessSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        var gpuDevice = scene.GpuDevice;

        scene.RootNode.Add(new BoxModelNode());
    }

    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        sceneView.RenderingSteps.AddAfter(
            sceneView.DefaultRenderObjectsRenderingStep!,
            new PostProcessRenderingStep(new PostProcessSampleEffect(Scene!), sceneView));

        base.OnSceneViewInitialized(sceneView);
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
    }
}