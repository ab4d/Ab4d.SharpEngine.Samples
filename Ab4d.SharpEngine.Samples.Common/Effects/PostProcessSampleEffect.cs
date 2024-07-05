using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Effects;
using Ab4d.SharpEngine.Materials;

namespace Ab4d.SharpEngine.Samples.Common.Effects;

sealed class PostProcessSampleEffect : Effect
{
    public PostProcessSampleEffect(Scene scene, string? name = null)
        : base(scene, name)
    {
    }

    public override void ApplyRenderingItemMaterial(RenderingItem renderingItem, Material material, RenderingContext renderingContext)
    {
        throw new NotImplementedException();
    }

    public override void Cleanup(bool increaseFrameNumber, bool freeEmptyMemoryBlocks)
    {
        throw new NotImplementedException();
    }

    public override void DisposeMaterial(Material material)
    {
        throw new NotImplementedException();
    }

    public override void InitializeMaterial(Material material)
    {
        throw new NotImplementedException();
    }

    public override void OnBeginUpdate(RenderingContext renderingContext)
    {
        throw new NotImplementedException();
    }

    public override void OnEndUpdate()
    {
        throw new NotImplementedException();
    }

    public override void ResetPipelines()
    {
        throw new NotImplementedException();
    }

    public override void UpdateMaterial(Material material)
    {
        throw new NotImplementedException();
    }
}
