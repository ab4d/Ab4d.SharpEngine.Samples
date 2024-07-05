using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Effects;
using Ab4d.SharpEngine.RenderingSteps;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

sealed class PostProcessRenderingStep : RenderingStep
{
    public PostProcessRenderingStep(Effect effect, SceneView sceneView, string? name = null, string? description = null)
        : base(sceneView, name, description)
    {
    }

    protected override bool OnRun(RenderingContext renderingContext)
    {
        var x = SceneView.FrameBuffers;

        //renderingContext.
        return true;
    }
}
