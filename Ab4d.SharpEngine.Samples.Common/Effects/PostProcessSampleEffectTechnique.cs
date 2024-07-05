using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Effects;
using Ab4d.Vulkan;

namespace Ab4d.SharpEngine.Samples.Common.Effects;

sealed class PostProcessSampleEffectTechnique : EffectTechnique
{
    public PostProcessSampleEffectTechnique(Scene scene, string? name = null)
        : base(scene, name)
    {
    }

    public override void Render(CommandBuffer commandBuffer, RenderingItem renderingItem, RenderingContext renderingContext)
    {
        throw new NotImplementedException();
    }

    protected override void Dispose(bool disposing)
    {
        throw new NotImplementedException();
    }
}
