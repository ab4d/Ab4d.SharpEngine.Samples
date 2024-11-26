using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.Vulkan;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

sealed class VertexColorPlusMaterial : StandardMaterial
{
    public Color4[]? VertexColors { get; set; }

    bool _isSolidColor;
    public bool IsSolidColor
    {
        get => _isSolidColor;
        set
        {
            if (value != _isSolidColor)
            {
                _isSolidColor = value;
                NotifyMaterialChange();
            }
        }
    }

    bool _blendVertexColors = true;
    public bool BlendVertexColors
    {
        get => _blendVertexColors;
        set
        {
            if (value != _blendVertexColors)
            {
                _blendVertexColors = value;
                NotifyMaterialChange();
            }
        }
    }

    float _vertexColorsOpacity = 1;
    public float VertexColorsOpacity
    {
        get => _vertexColorsOpacity;
        set
        {
            if (value != _vertexColorsOpacity)
            {
                _vertexColorsOpacity = value;
                NotifyMaterialChange();
            }
        }
    }

    GpuBuffer? _vertexColorsBuffer;
    public GpuBuffer? VertexColorsBuffer => _vertexColorsBuffer;

    public VertexColorPlusMaterial(string? name = null) : base(name)
    {
    }

    public VertexColorPlusMaterial(Color4[] positionColors, string? name = null) : base(name)
    {
        VertexColors = positionColors;
    }

    protected override void Dispose(bool disposing)
    {
        _vertexColorsBuffer?.Dispose();
        _vertexColorsBuffer = null;
        base.Dispose(disposing);
    }

    protected override void OnInitializeSceneResources(Scene scene, VulkanDevice gpuDevice)
    {
        Effect ??= VertexColorPlusEffect.GetDefault(scene);
        base.OnInitializeSceneResources(scene, gpuDevice);
        UpdateVertexColors();
    }

    public void UpdateVertexColors()
    {
        _vertexColorsBuffer?.Dispose();
        _vertexColorsBuffer = null;
        if (VertexColors is not null)
            _vertexColorsBuffer = Scene?.GpuDevice?.CreateBuffer(VertexColors, BufferUsageFlags.VertexBuffer, true);
        NotifyMaterialChange();
    }
}
