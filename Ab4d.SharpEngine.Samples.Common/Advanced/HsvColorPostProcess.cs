using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.PostProcessing;
using Ab4d.Vulkan;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

// HsvColorPostProcess is derived from SimpleFragmentShaderPostProcess.
// This means that it only needs to provide the name of the fragment shader file in the constructor
// and then provide the GetFragmentPushConstantsSize and CustomBindAction where the push constants are set.
//
// To load the shader file, the shader provider must be registered in the ShadersManager.
// In the CustomPostProcessSample, this is done by using the AssemblyShaderBytecodeProvider - see the code in the constructor of the CustomPostProcessSample.

/// <summary>
/// HsvColorPostProcess is a post process that convert each pixel to HSV color space and then adjust the color by the specified <see cref="HueOffset"/>, <see cref="SaturationFactor"/> and <see cref="BrightnessFactor"/>.
/// </summary>
public class HsvColorPostProcess : SimpleFragmentShaderPostProcess
{
    private float _hueOffset = 0f;

    /// <summary>
    /// HueOffset is added to the hue HSV color component. The value is in range from 0 to 360. Default value is 0 (no hue change).
    /// </summary>
    public float HueOffset
    {
        get => _hueOffset;
        set
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_hueOffset == value)
                return;
            
            _hueOffset = value;
            
            SceneView?.NotifyChange(SceneViewDirtyFlags.PostProcessChanged);
        }
    }
    
    
    private float _saturationFactor = 1f;

    /// <summary>
    /// SaturationFactor value is multiplied by the saturation HSV color component. Default value is 1 (no saturation change).
    /// </summary>
    public float SaturationFactor
    {
        get => _saturationFactor;
        set
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_saturationFactor == value)
                return;
            
            _saturationFactor = value;
            
            SceneView?.NotifyChange(SceneViewDirtyFlags.PostProcessChanged);
        }
    }
    
    
    private float _brightnessFactor = 1f;

    /// <summary>
    /// BrightnessFactor value is multiplied by the value or brightness HSV color component. Default value is 1 (no brightness change).
    /// </summary>
    public float BrightnessFactor
    {
        get => _brightnessFactor;
        set
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_brightnessFactor == value)
                return;
            
            _brightnessFactor = value;
            
            SceneView?.NotifyChange(SceneViewDirtyFlags.PostProcessChanged);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HsvColorPostProcess"/> class.
    /// </summary>
    public HsvColorPostProcess()
        : base(fragmentShaderResourceName: "HsvColorPostProcessShader.frag.spv", name: "HsvColorPostProcess")
    {
    }

    /// <inheritdoc />
    protected override int GetFragmentPushConstantsSize() => 12; // 12 bytes: 3 * float values
    
    /// <inheritdoc />
    protected override unsafe void CustomBindAction(RenderingContext renderingContext, CommandBuffer commandBuffer, PipelineLayout pipelineLayout)
    {
        var parameters = stackalloc float[3];
        parameters[0] = _hueOffset / 360.0f; // convert range from 0...360 to 0...1 that is used in the shader
        parameters[1] = _saturationFactor;
        parameters[2] = _brightnessFactor;
        
        renderingContext.GpuDevice.Vk.CmdPushConstants(commandBuffer, pipelineLayout, ShaderStageFlags.Fragment, 0, 12, parameters);
    }
}