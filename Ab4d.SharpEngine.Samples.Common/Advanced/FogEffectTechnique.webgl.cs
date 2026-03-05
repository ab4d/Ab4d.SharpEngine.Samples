using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Effects;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.WebGL;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

public class FogEffectTechnique : EffectTechnique
{
    private WebGLDevice _gpuDevice;

    //public int VertexPositionIndex { get; private set; }
    //public int VertexNormalIndex { get; private set; }
    //public int VertexTextureCoordinatesIndex { get; private set; }

    public uint ShaderProgramId { get; }
    public uint VertexShaderId { get; }
    public uint FragmentShaderId { get;  }
    
    public FogEffectUniformBinding UniformBinding { get;  }


    private Material? _lastMaterial;
    private bool _isLastWorldMatrixIdentity;

    private bool _isWebGLTransposeSupported;


    /// <inheritdoc />
    public FogEffectTechnique(WebGLDevice gpuDevice,
        uint vertexShaderId,
        uint fragmentShaderId,
        uint shaderProgramId,
        FogEffectUniformBinding standardEffectUniformBinding,
        string name = "")
        : base(name)
    {
        _gpuDevice = gpuDevice;

        VertexShaderId = vertexShaderId;
        FragmentShaderId = fragmentShaderId;
        ShaderProgramId = shaderProgramId;

        UniformBinding = standardEffectUniformBinding;

        _isWebGLTransposeSupported = gpuDevice.CanvasInterop.IsWebGL2; // WebGL 1 does not support transpose in glUniformMatrix4fv
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override void ApplyPerFrameSettings(RenderingContext renderingContext)
    {
       GL.UseProgram(programName: this.Name ?? "", ShaderProgramId);

        var usedCamera = renderingContext.UsedCamera;

        var uniforms = UniformBinding;

        if (uniforms.ViewProjection >= 0)
        {
            Matrix4x4 viewProjectionMatrix;
            if (usedCamera != null)
                viewProjectionMatrix = usedCamera.GetViewProjectionMatrix();
            else
                viewProjectionMatrix = Matrix4x4.Identity;

            bool transpose = _isWebGLTransposeSupported;
            if (!transpose)
                viewProjectionMatrix = Matrix4x4.Transpose(viewProjectionMatrix); // WebGL 1 does not support transpose in glUniformMatrix4fv

            GL.UniformMatrix4("viewProjection", uniforms.ViewProjection, transpose, ref viewProjectionMatrix);
        }


        var scene = renderingContext.Scene;

        var ambientLightColor = scene.GetAmbientLightColor();

        if (uniforms.AmbientLightColor >= 0)
            GL.Uniform3("ambientLightColor", uniforms.AmbientLightColor, ambientLightColor);

        if (uniforms.EyePosition >= 0 && usedCamera != null)
            GL.Uniform3("eyePosition", uniforms.EyePosition, usedCamera.GetCameraPosition());


        var firstDirectionalLight = scene.Lights.OfType<IDirectionalLight>().FirstOrDefault(); // This will also get the CameraLight

        // Here we support only one directional light
        if (firstDirectionalLight != null && uniforms.DirectionalLights != null)
        {
            GL.Uniform3("DirectionalLightDirection", uniforms.DirectionalLights[0].Direction, firstDirectionalLight.GetNormalizedDirection());
            GL.Uniform3("DirectionalLightDiffuseColor", uniforms.DirectionalLights[0].DiffuseColor, firstDirectionalLight.Color);
        }

        _lastMaterial = null;
        _isLastWorldMatrixIdentity = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override void ApplyPerObjectSettings(RenderingContext renderingContext, RenderingItem renderingItem)
    {
        var uniforms = UniformBinding;

        // Optimization: When this and the previous world matrix was identity, then just skip setting the matrix
        var isWorldMatrixIdentity = renderingItem.IsWorldMatrixIdentity;
        if (!_isLastWorldMatrixIdentity || isWorldMatrixIdentity != _isLastWorldMatrixIdentity)
        {
            if (uniforms.WorldMatrix >= 0)
            {
                Matrix4x4 worldMatrix;

                if (isWorldMatrixIdentity)
                {
                    worldMatrix = Matrix4x4.Identity;
                    GL.UniformMatrix4("worldMatrix", uniforms.WorldMatrix, transpose: false, ref worldMatrix); // No need to transpose Identity
                }
                else
                {
                    worldMatrix = renderingItem.WorldMatrix;

                    bool transpose = _isWebGLTransposeSupported;
                    if (!transpose)
                        worldMatrix = Matrix4x4.Transpose(worldMatrix); // WebGL 1 does not support transpose in glUniformMatrix4fv

                    GL.UniformMatrix4("worldMatrix", uniforms.WorldMatrix, transpose, ref worldMatrix);
                }
            }

            _isLastWorldMatrixIdentity = isWorldMatrixIdentity;
        }

        var material = renderingItem.Material;


        // First set the culling.
        // Do this before we can skip setting up other properties when the material is the same as last time.

        FrontFaceDirection newFrontFace;
        if (material is ITwoSidedMaterial twoSidedMaterial && twoSidedMaterial.IsTwoSided)
        {
            newFrontFace = (FrontFaceDirection)0; // 0 is not a valid TriangleFace value, but we use it to indicate that culling is disabled
        }
        else
        {
            bool isFrontClockwise = (renderingItem.Flags & RenderingItemFlags.IsBackFaceMaterial) != 0;
            if (!renderingContext.Scene.IsRightHandedCoordinateSystem)
                isFrontClockwise = !isFrontClockwise;

            newFrontFace = isFrontClockwise ? FrontFaceDirection.CW : FrontFaceDirection.Ccw;
        }

        var lastCullFace = renderingContext.LastFrontFace;
        if (newFrontFace != lastCullFace)
        {
            if (newFrontFace == (FrontFaceDirection)0)
            {
                GL.Disable(EnableCap.CullFace);
            }
            else
            {
                if (lastCullFace == 0)
                {
                    GL.Enable(EnableCap.CullFace);
                    GL.CullFace(TriangleFace.Back);
                }

                GL.FrontFace(newFrontFace);
            }

            renderingContext.LastFrontFace = newFrontFace;
        }


        // Skip setting the material if it is the same as previous material
        if (ReferenceEquals(material, _lastMaterial))
            return;


        Color4 diffuseColor;
        
        if (material is StandardMaterialBase standardMaterial)
        {
            diffuseColor = new Color4(standardMaterial.DiffuseColor, standardMaterial.Opacity);

            //if (standardMaterial.DiffuseTexture != null && uniforms.DiffuseSampler > 0)
            //{
            //    // Activate texture unit and bind
            //    GL.ActiveTexture(TextureUnit.Texture0); // Use texture unit 0
            //    GL.BindTexture(TextureTarget.Texture2D, standardMaterial.DiffuseTexture.Image);

            //    // Pass texture unit index to shader
            //    GL.Uniform1("TEXTURE0", uniforms.DiffuseSampler, 0); // 0 corresponds to TEXTURE0


            //    if (standardMaterial.DiffuseTexture.HasNoMipMaps)
            //    {
            //        // If texture has no mipmaps (for example when using WebGL 1), then set different wrapping and filtering
            //        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            //        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            //        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            //    }
            //    else if (standardMaterial.DiffuseTextureSampler != null)
            //    {
            //        standardMaterial.DiffuseTextureSampler.ApplySampler(renderingContext);
            //    }

            //    alphaClipThreshold = standardMaterial.AlphaClipThreshold;
            //    isTexture = true;
            //}
        }
        else
        {
            if (material is IDiffuseMaterial diffuseMaterial)
                diffuseColor = new Color4(diffuseMaterial.DiffuseColor, diffuseMaterial.Opacity);
            else
                diffuseColor = Color4.Black;

            //if (material is IDiffuseTextureMaterial diffuseTextureMaterial)
            //{
            //    alphaClipThreshold = diffuseTextureMaterial.AlphaClipThreshold;
            //    isTexture = diffuseTextureMaterial.DiffuseTexture != null;
            //}
        }

        // Here we do not support transparent materials
        GL.Disable(EnableCap.Blend);

        // Set color after blend type because we may need to alpha-premultiply the diffuseColor
        if (uniforms.DiffuseColor >= 0)
            GL.Uniform4("diffuseColor", uniforms.DiffuseColor, diffuseColor);



        if (material is FogMaterial fogMaterial)
        {
            if (uniforms.FogStart >= 0)
                GL.Uniform1("fogStart", uniforms.FogStart, fogMaterial.FogStart);

            if (uniforms.FogFullColorStart >= 0)
                GL.Uniform1("fogFullColorStart", uniforms.FogFullColorStart, fogMaterial.FogFullColorStart);

            if (uniforms.FogColor >= 0)
                GL.Uniform3("fogColor", uniforms.FogColor, fogMaterial.FogColor);
        }


        _lastMaterial = material;
    }
}