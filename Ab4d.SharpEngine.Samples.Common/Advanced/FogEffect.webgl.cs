using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Effects;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.WebGL;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

public class FogEffect : Effect, IEffectConstructor<FogEffect>
{
    private FogEffectTechnique? _standardEffectTechnique;
    private FogEffectTechnique? _texturedEffectTechnique;
    
    private uint _vertexShaderId;

    private FogEffect(Scene scene, string? name) 
        : base(scene, name)
    {

    }

    public static FogEffect GetDefault(Scene scene)
    {
        if (scene == null) throw new ArgumentNullException(nameof(scene));

        if (scene.GpuDevice == null || scene.EffectsManager == null)
            throw new InvalidOperationException("Scene is not yet initialized (Scene was not created with VulkanDevice or Initialized method was not yet called)");
        
        return scene.EffectsManager.GetDefault<FogEffect>();
    }

    public static (FogEffect effect, DisposeToken disposeToken) CreateNew(Scene scene, string uniqueEffectName)
    {
        if (scene == null) throw new ArgumentNullException(nameof(scene));

        var newEffect = new FogEffect(scene, uniqueEffectName);

        scene.EffectsManager.RegisterEffect(newEffect);

        var disposeToken = new DisposeToken(newEffect.DisposeAction);

        return (newEffect, disposeToken);
    }

    private void DisposeAction()
    {
        this.CheckAndDispose(disposing: true);
    }


    /// <inheritdoc />
    public override void ApplyRenderingItemMaterial(RenderingItem renderingItem, Material material, RenderingContext renderingContext)
    {
        renderingItem.EffectTechnique = GetEffectTechnique(renderingItem, material, renderingContext);
    }

    private EffectTechnique? GetEffectTechnique(RenderingItem renderingItem, Material material, RenderingContext renderingContext)
    {
        CheckIfInitialized();
        var gpuDevice = Scene.GpuDevice!;
        var scene = renderingContext.Scene;

        bool hasTexture = material is IDiffuseTextureMaterial textureMaterial && textureMaterial.DiffuseTexture != null;


        if (hasTexture)
        {
            if (_texturedEffectTechnique != null)
                return _texturedEffectTechnique;
        }
        else
        {
            if (_standardEffectTechnique != null)
                return _standardEffectTechnique;
        }


        // The same vertex shader is needed for both standard and textures technique
        uint vertexShaderId = _vertexShaderId;
        
        if (vertexShaderId == 0) // Do we already have the vertex shader?
        {
            var vertexGlsl = GetVertexShaderText();

            vertexShaderId = gpuDevice.CompileShader(vertexGlsl, ShaderType.VertexShader);

            if (vertexShaderId <= 0)
                return null;

            _vertexShaderId = vertexShaderId;
        }

        
        // Fragment shader is different for standard and textures techniques
        var fragmentGlsl = GetFragmentShaderText(hasTexture: hasTexture);

        var fragmentShaderId = gpuDevice.CompileShader(fragmentGlsl, ShaderType.FragmentShader);

        uint shaderProgramId;

        if (fragmentShaderId > 0)
            shaderProgramId = gpuDevice.LinkShaderProgram(vertexShaderId, fragmentShaderId);
        else
            shaderProgramId = 0;

        if (fragmentShaderId <= 0 || shaderProgramId <= 0)
            return null;


        var fogEffectUniformBinding = new FogEffectUniformBinding();

        // Get uniform locations
        fogEffectUniformBinding.ViewProjection    = gpuDevice.GetUniformLocation(shaderProgramId, "viewProjection"u8);
        fogEffectUniformBinding.WorldMatrix       = gpuDevice.GetUniformLocation(shaderProgramId, "worldMatrix"u8);
        fogEffectUniformBinding.DiffuseColor      = gpuDevice.GetUniformLocation(shaderProgramId, "diffuseColor"u8);
        fogEffectUniformBinding.AmbientLightColor = gpuDevice.GetUniformLocation(shaderProgramId, "ambientLightColor"u8);
        fogEffectUniformBinding.EyePosition       = gpuDevice.GetUniformLocation(shaderProgramId, "eyePosition"u8);
        
        fogEffectUniformBinding.FogStart          = gpuDevice.GetUniformLocation(shaderProgramId, "fogStart"u8);
        fogEffectUniformBinding.FogFullColorStart = gpuDevice.GetUniformLocation(shaderProgramId, "fogFullColorStart"u8);
        fogEffectUniformBinding.FogColor          = gpuDevice.GetUniformLocation(shaderProgramId, "fogColor"u8);

        if (hasTexture)
            fogEffectUniformBinding.DiffuseSampler = gpuDevice.GetUniformLocation(shaderProgramId, "textureSampler"u8);


        // Here we support only one directional light
        var directionalLightBindings = new DirectionalLightUniformBinding[1];
        directionalLightBindings[0].Direction = gpuDevice.GetUniformLocation(shaderProgramId, "directionalLightDirection"u8);
        directionalLightBindings[0].DiffuseColor = gpuDevice.GetUniformLocation(shaderProgramId, "directionalLightDiffuseColor"u8);

        fogEffectUniformBinding.DirectionalLights = directionalLightBindings;


        var techniqueName = hasTexture ? "FogTextureTechnique" : "FogTechnique";
        var effectTechnique = new FogEffectTechnique(gpuDevice, vertexShaderId, fragmentShaderId, shaderProgramId, fogEffectUniformBinding, name: techniqueName);

        if (hasTexture)
            _texturedEffectTechnique = effectTechnique;
        else
            _standardEffectTechnique = effectTechnique;
        
        return effectTechnique;
    }


    // The following methods are not needed here, but can be implemented when needed:

    ///// <summary>
    ///// OnBeginUpdate method needs to be implemented by the effect class.
    ///// The method is called from the <see cref="Effect.BeginUpdate"/> method which is called from the Render method in SceneView before rendering of a next frame is started (before the Scene.Update method is called).
    ///// It can be used to read some data from the RenderingContext.
    ///// It is used with the <see cref="OnEndUpdate"/> method to prepare all the data for the next rendered frame.
    ///// </summary>
    ///// <param name="renderingContext">RenderingContext</param>
    //public override void OnBeginUpdate(RenderingContext renderingContext)
    //{
        
    //}

    ///// <summary>
    ///// OnEndUpdate method needs to be implemented by the effect class.
    ///// The method is called from the <see cref="Effect.EndUpdate"/> method method which is called after the Scene.Update method is called.
    ///// This method can update the material's buffers in case any of the material was changed.
    ///// </summary>
    //public override void OnEndUpdate()
    //{
        
    //}

    ///// <summary>
    ///// Cleanup method is called from <see cref="Scene.Cleanup"/> method and can be used to remove some temporary resources in the effect.
    ///// </summary>
    //public override void Cleanup()
    //{
        
    //}

    private string GetVertexShaderText()
    {
        return 
@"#version 300 es // 330 is not supported

precision highp float;

layout(location = 0) in highp vec3 in_pos;
layout(location = 1) in highp vec3 in_normal;
layout(location = 2) in highp vec2 in_uv;

// GLSL uses the reverse order to a System.Numerics.Matrix4x4
uniform mat4 worldMatrix;
uniform mat4 viewProjection;

out vec3 vertex_worldPos;
out vec3 vertex_normal;
out vec2 vertex_uv;

void main()
{
    vec4 posWorld4 = vec4(in_pos, 1.0) * worldMatrix;

    gl_Position = posWorld4 * viewProjection; 

    vertex_worldPos = posWorld4.xyz;
    vertex_normal = (vec4(in_normal, 0.0) * worldMatrix).xyz;
    vertex_uv = in_uv;
}
";
    }

    private string GetFragmentShaderText(bool hasTexture)
    {
        string textureDeclarationsGlsl, textureReadGlsl;

        if (hasTexture)
        {
            textureDeclarationsGlsl = @"
in vec2 vertex_uv;
uniform sampler2D textureSampler;
";

            textureReadGlsl = @"
vec4 usedDiffuseColor = texture(textureSampler, vertex_uv) * diffuseColor;
";
        }
        else
        {
            textureDeclarationsGlsl = "";
            textureReadGlsl = @"
vec4 usedDiffuseColor = diffuseColor;
";
        }

        return 
@"#version 300 es
precision highp float;

in vec3 vertex_worldPos;
in vec3 vertex_normal;
out vec4 finalColor;

uniform vec3 ambientLightColor;
uniform vec3 eyePosition;
uniform vec4 diffuseColor;

uniform vec3 directionalLightDirection;
uniform vec3 directionalLightDiffuseColor;
uniform vec3 directionalLightSpecularColor;

uniform float fogStart;
uniform float fogFullColorStart;
uniform vec3 fogColor;

" + textureDeclarationsGlsl + @"

void main()
{
    vec3 normal = normalize(vertex_normal);
" + textureReadGlsl + @"

    // Directional light: 
    float diffuseFactor = dot(-directionalLightDirection, normal);
    diffuseFactor = max(diffuseFactor, 0.0);
    vec3 finalDiffuseColor = diffuseFactor * directionalLightDiffuseColor;

	// Add ambient color and multiply by the material's color
	finalDiffuseColor = clamp(finalDiffuseColor + ambientLightColor, vec3(0), vec3(1)) * usedDiffuseColor.rgb;


	// Add fog

	// Get distance from the current point to camera (in world coordinates)
	float distanceW = length(eyePosition - vertex_worldPos);

	// Fog settings (in world coordinates)
	//float fogStart = 150.0; // fog starts when objects are 150 units from camera
	//float fogEndW = 220.0;   // full color fog start at 220 units
	//float3 fogColor = float3(1, 1, 1); // fog color in RGB

	// interpolate from 0 to 1: 0 starting at fogStart and 1 at fogEnd 
	// saturate clamps the specified value within the range of 0 to 1.
	float fogFactor = clamp((distanceW - fogStart) / (fogFullColorStart - fogStart), 0.0, 1.0);

	// lerp lineary interpolates the color
	finalDiffuseColor = mix(finalDiffuseColor, fogColor, fogFactor);


	// Add alpha
	finalColor = clamp(vec4(finalDiffuseColor, usedDiffuseColor.a), vec4(0), vec4(1));
}
";
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (_standardEffectTechnique != null)
        {
            if (_standardEffectTechnique.ShaderProgramId != 0)
                GL.DeleteProgram(_standardEffectTechnique.Name, _standardEffectTechnique.ShaderProgramId);

            if (_standardEffectTechnique.FragmentShaderId != 0)
                GL.DeleteShader(_standardEffectTechnique.FragmentShaderId);

            // Vertex shader is disposed below

            _standardEffectTechnique = null;
        }
        
        if (_texturedEffectTechnique != null)
        {
            if (_texturedEffectTechnique.ShaderProgramId != 0)
                GL.DeleteProgram(_texturedEffectTechnique.Name, _texturedEffectTechnique.ShaderProgramId);

            if (_texturedEffectTechnique.FragmentShaderId != 0)
                GL.DeleteShader(_texturedEffectTechnique.FragmentShaderId);

            // Vertex shader is disposed below

            _texturedEffectTechnique = null;
        }

        // We have a common vertex shader that is used by both _standardEffectTechnique and _texturedEffectTechnique
        if (_vertexShaderId != 0)
        {
            GL.DeleteShader(_vertexShaderId);
            _vertexShaderId = 0;
        }
    }
}