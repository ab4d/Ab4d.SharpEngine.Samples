using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Vulkan;
using System.Text;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

public class FogMaterial : Material, IDiffuseMaterial, IDiffuseTextureMaterial, ITransparentMaterial
{
    private Color3 _diffuseColor;
    private bool _isDiffuseColorSet;

    /// <summary>
    /// Gets or sets a Color3 that specifies the diffuse color of this material.
    /// Default value is Black.
    /// </summary>
    public Color3 DiffuseColor
    {
        get => _diffuseColor;
        set
        {
            _isDiffuseColorSet = true; // Mark that user has set the DiffuseColor. This prevents automatic changing of DiffuseColor to White when DiffuseTexture is set.

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
                _hasTransparency = newTransparencyValue || (DiffuseTexture != null && DiffuseTexture.HasTransparentPixels);

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


    private bool _isPreMultipliedAlphaColor;

    /// <summary>
    /// When IsPreMultipliedAlphaColor is true (false by default) and Opacity is less then 1, then the color components of the <see cref="DiffuseColor"/> are already multiplied with alpha value to produce pre-multiplied colors.
    /// When false, then non-premultiplied color is converted in pre-multiplied color when this is required by the shader. Default value is false.
    /// </summary>
    public bool IsPreMultipliedAlphaColor
    {
        get => _isPreMultipliedAlphaColor;
        set
        {
            if (_isPreMultipliedAlphaColor == value)
                return;

            _isPreMultipliedAlphaColor = value;
            NotifyMaterialBufferChange();
        }
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


    #region IDiffuseTextureMaterial
    /// <summary>
    /// Gets a source string that can contain file name or other string that defines the source of this GpuImage (TextureSource is read from GpuImage object and can be null even when DiffuseTexture is set).
    /// </summary>
    public string? TextureSource
    {
        get
        {
            if (_diffuseTexture != null)
                return _diffuseTexture.Source;

            return null;
        }
    }


    private GpuImage? _diffuseTexture;

    /// <summary>
    /// Gets or sets a GpuImage objects that define the texture.
    /// When rendering texture, the colors from texture are multiplied by the <see cref="DiffuseColor"/> (White color is used when a texture is set and DiffuseColor is not changed by the user).
    /// When user does not set the DiffuseColor but sets the DiffuseTexture, then the DiffuseColor is automatically set to White color to preserve the colors in the texture.
    /// The GpuImage is not disposed when disposing this Material (except when calling <see cref="DisposeWithTexture"/> method).
    /// </summary>
    public GpuImage? DiffuseTexture
    {
        get => _diffuseTexture;
        set
        {
            if (ReferenceEquals(_diffuseTexture, value))
                return;

            DisconnectDiffuseTexture(dispose: false); // Do not automatically dispose the texture that was previously assigned

            _diffuseTexture = value;

            if (value != null)
            {
                if (value.IsDisposed)
                {
                    _diffuseTexture = null; // Prevent setting disposed GpuImage to DiffuseTexture
                    throw new ObjectDisposedException($"Cannot assign a disposed GpuImage ({value}).");
                }

                if (this.Scene != null && this.Scene.GpuDevice != null)
                {
                    if (!ReferenceEquals(this.Scene.GpuDevice, value.GpuDevice))
                    {
                        _diffuseTexture = null;
                        throw new ArgumentException("Cannot use GpuImage because it was created by a different GpuDevice that is used by this Material.");
                    }

                    this.Scene.GpuDevice.CheckIsOnMainThread("DiffuseTexture must not be assigned from the background thread. Use Async methods in the TextureLoader to load the texture in the background thread and then assign the loaded GpuImage in the UI thread.");
                }

                value.Disposing += OnDiffuseTextureDisposing;
                
                _hasTransparency |= value.HasTransparentPixels; // If HasTransparency was true before, then preserve that value even if texture is not transparent
            }

            if (!_isDiffuseColorSet)
                _diffuseColor = Color3.White; // Automatically set DiffuseColor to White to preserve the texture colors

            NotifyMaterialComplexChange(); // we need to regenerate RenderingItems
        }
    }

    /// <summary>
    /// Gets the GpuSampler that defines how the diffuse texture is read by the graphics card.
    /// The sampler can be set by <see cref="DiffuseTextureSamplerType"/> or by calling the <see cref="SetDiffuseTextureSampler"/> method.
    /// </summary>
    public GpuSampler? DiffuseTextureSampler { get; private set; }


    private CommonSamplerTypes _diffuseTextureSamplerType = CommonSamplerTypes.Mirror; // using mirror sample by default (see comments in SamplerFactory why this is used instead of Warp)

    /// <summary>
    /// Gets or sets the sampler type for the diffuse texture.
    /// Sampler type defines how the texture is read by the graphics card.
    /// Default value is <see cref="CommonSamplerTypes.Mirror"/>.
    /// Setting this property sets the <see cref="DiffuseTextureSampler"/> with the actual GpuSampler when the material is initialized.
    /// It is also possible to set the DiffuseTextureSampler by calling the <see cref="SetDiffuseTextureSampler"/> method.
    /// </summary>
    public CommonSamplerTypes DiffuseTextureSamplerType
    {
        get => _diffuseTextureSamplerType;
        set
        {
            if (_diffuseTextureSamplerType == value)
                return;

            _diffuseTextureSamplerType = value;

            if (Scene != null && Scene.GpuDevice != null && value != CommonSamplerTypes.Other)
            {
                DiffuseTextureSampler = Scene.GpuDevice.SamplerFactory.GetSampler(value);
                NotifyMaterialComplexChange(); // we need to regenerate RenderingItems
            }
        }
    }

    /// <summary>
    /// SetDiffuseTextureSampler method sets the <see cref="DiffuseTextureSampler"/> to the specified sampler.
    /// It also sets the <see cref="DiffuseTextureSamplerType"/> to <see cref="CommonSamplerTypes.Other"/>.
    /// It is also possible to set the DiffuseTextureSampler to a common sampler by setting the <see cref="DiffuseTextureSamplerType"/>.
    /// </summary>
    /// <param name="gpuSampler"></param>
    public void SetDiffuseTextureSampler(GpuSampler? gpuSampler)
    {
        if (gpuSampler == null)
        {
            _diffuseTextureSamplerType = CommonSamplerTypes.Mirror; // Set back to default value. Using mirror sample by default (see comments in SamplerFactory why this is used instead of Warp)
        }
        else
        {
            if (this.Scene != null && this.Scene.GpuDevice != null && !ReferenceEquals(this.Scene.GpuDevice, gpuSampler.GpuDevice))
                throw new ArgumentException("Cannot use the specified gpuSampler because it was created by a different GpuDevice that is used by this Material.", nameof(gpuSampler));

            _diffuseTextureSamplerType = CommonSamplerTypes.Other;
        }

        DiffuseTextureSampler = gpuSampler;
    }

    private void DisconnectDiffuseTexture(bool dispose)
    {
        var diffuseTexture = _diffuseTexture;

        if (diffuseTexture != null)
        {
            diffuseTexture.Disposing -= OnDiffuseTextureDisposing;

            if (dispose && !diffuseTexture.IsDisposed && !diffuseTexture.IsDisposing)
                diffuseTexture.Dispose();

            // We also need to call DisposeDescriptorSetForTexture to remove the disposed ImageView from the _textureDescriptorSets dictionary.
            // If this is not done, then the new ImageView with the same handle can use a descriptor set that is not valid anymore.
            // This happened in AnimatedTexturesSample when using WPF with OverlayTexture or in Avalonia in Release build.
            if (this.DiffuseTextureSampler != null)
            {
                if (this.Effect is FogEffect standardEffect)
                    standardEffect.DisposeDescriptorSetForTexture(this.MaterialBlockIndex, diffuseTexture, this.DiffuseTextureSampler);
            }
            
            _diffuseTexture = null;
        }
    }

    /// <summary>
    /// OnDiffuseTextureDisposing
    /// </summary>
    /// <param name="sender">sender</param>
    /// <param name="disposing">disposing</param>
    protected virtual void OnDiffuseTextureDisposing(object? sender, bool disposing)
    {
        if (sender is GpuImage gpuImage && ReferenceEquals(gpuImage, _diffuseTexture))
        {
            DisconnectDiffuseTexture(dispose: false); // Unassign the texture to prevent using a disposed object
            
            if (disposing)
                NotifyMaterialComplexChange(); // we need to regenerate RenderingItems
        }
    }


    private float _alphaClipThreshold;

    /// <summary>
    /// Pixels with alpha color values below this value will be clipped (not rendered and their depth will not be written to depth buffer).
    /// Expected values are between 0 and 1.
    /// When 0 (by default) then alpha clipping is disabled - this means that also pixels with alpha value 0 are fully processed (they are not visible but its depth value is still written so objects that are rendered afterwards and are behind the pixel will not be visible).
    /// </summary>
    public float AlphaClipThreshold
    {
        get => _alphaClipThreshold;
        set
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (value == _alphaClipThreshold)
                return;

            _alphaClipThreshold = value;
            NotifyMaterialBufferChange();
        }
    }

    #endregion




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