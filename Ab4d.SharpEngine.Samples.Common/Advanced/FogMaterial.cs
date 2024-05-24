using System.Text;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Vulkan;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

public class FogMaterial : Material, ITransparentMaterial
{
    private Color3 _diffuseColor;

    /// <summary>
    /// Gets or sets a Color3 that specifies the diffuse color of this material.
    /// Default value is Black.
    /// </summary>
    public Color3 DiffuseColor
    {
        get => _diffuseColor;
        set
        {
            if (value == _diffuseColor)
                return;

            _diffuseColor = value;
            NotifyMaterialBufferChange();
        }
    }


    private float _opacity;

    /// <summary>
    /// Gets or sets a float value that defines the opacity for the material, for example 1 means fully opaque and 0 means fully transparent.
    /// Default value is 1;
    /// </summary>
    public float Opacity
    {
        get => _opacity;
        set
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_opacity == value)
                return;

            _opacity = value;

            bool newTransparencyValue = value < 1;

            if (newTransparencyValue != _hasTransparency)
            {
                // Set private _hasTransparency field so that the _isTransparencyManuallySet is not set to true (this happens when the HasTransparency property is changed)
                _hasTransparency = newTransparencyValue;

                NotifyMaterialComplexChange(); // Changing HasTransparency requires a full render pass (using new Pipeline objects and recording the command buffers again)
            }
            else
            {
                // When HasTransparency is not changed, then we can only update the material buffer and render the scene again - no need to record command buffer again
                NotifyMaterialBufferChange();
            }
        }
    }

    private bool _hasTransparency;

    /// <summary>
    /// Gets a boolean that specifies if this material is semi-transparent and needs to be alpha blended with the scene.
    /// This value is automatically set to true when <see cref="Opacity"/> is set to a value smaller than 1.
    /// </summary>
    public bool HasTransparency
    {
        get => _hasTransparency;
    }


    private float _fogStart = 0;

    public float FogStart
    {
        get => _fogStart;
        set
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (value == _fogStart)
                return;

            _fogStart = value;
            NotifyMaterialBufferChange();
        }
    }


    private float _fogFullColorStart = 100;

    public float FogFullColorStart
    {
        get => _fogFullColorStart;
        set
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (value == _fogFullColorStart)
                return;

            _fogFullColorStart = value;
            NotifyMaterialBufferChange();
        }
    }


    private Color3 _fogColor = Color3.White;

    public Color3 FogColor
    {
        get => _fogColor;
        set
        {
            if (value == _fogColor)
                return;

            _fogColor = value;
            NotifyMaterialBufferChange();
        }
    }



    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="name">optional name</param>
    public FogMaterial(string? name = null) 
        : base(name)
    {

    }

    /// <inheritdoc />
    protected override void OnInitializeSceneResources(Scene scene, VulkanDevice gpuDevice)
    {
        // Get the default FogEffect (one instance of FogEffect that is created on first call to EffectsManager.GetDefault<FogEffect>.)
        if (Effect == null)
            Effect = scene.EffectsManager.GetDefault<FogEffect>();

        base.OnInitializeSceneResources(scene, gpuDevice);
    }

    /// <inheritdoc />
    public override void GetDetailsText(StringBuilder sb, bool showDirtyFlags = true, bool showVersion = true, bool showGpuHandles = false)
    {
        if (!string.IsNullOrEmpty(this.Name))
            sb.AppendFormat("FogMaterial ({0}) '{1}'", Id, this.Name);
        else
            sb.AppendFormat("FogMaterial ({0})", Id);

        if (IsDisposed)
            sb.Append("(DISPOSED) ");
    }

    // If we would create some Vulkan resources here, then we need to dispose them in the Dispose method.

    ///// <inheritdoc />
    //protected override void Dispose(bool disposing)
    //{
    //    base.Dispose(disposing);
    //}
}