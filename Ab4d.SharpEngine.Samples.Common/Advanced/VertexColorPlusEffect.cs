using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Effects;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.Vulkan;
using System.Runtime.InteropServices;
using System.Text;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

sealed unsafe class VertexColorPlusEffect : Effect
{
    [StructLayout(LayoutKind.Explicit, Size = SizeInBytes)]
    struct Mat
    {
        [FieldOffset(0)]
        public Color4 DiffuseColor;
        [FieldOffset(16)]
        public Color3 SpecularColor;
        [FieldOffset(28)]
        public float SpecularPower;
        [FieldOffset(32)]
        public Color3 EmissiveColor;
        [FieldOffset(44)]
        public float AlphaClipThreshold;
        [FieldOffset(48)]
        public bool IsTwoSided;
        [FieldOffset(52)]
        public bool IsSolidColor;
        [FieldOffset(56)]
        public bool BlendVertexColors;
        [FieldOffset(60)]
        public float VertexColorsOpacity;

        public const int SizeInBytes = 64;
    }

    readonly struct TexturedDescriptorSetsKey(int materialMemBlockIndex, long gpuImageId, long gpuSamplerId)
        : IEquatable<TexturedDescriptorSetsKey>
    {
        readonly int _materialMemBlockIndex = materialMemBlockIndex;
        readonly long _gpuImageId = gpuImageId;
        readonly long _gpuSamplerId = gpuSamplerId;

        public bool Equals(TexturedDescriptorSetsKey other) =>
            _materialMemBlockIndex == other._materialMemBlockIndex && _gpuImageId == other._gpuImageId && _gpuSamplerId == other._gpuSamplerId;

        public override bool Equals(object? obj) =>
            obj is TexturedDescriptorSetsKey other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(_materialMemBlockIndex, _gpuImageId, _gpuSamplerId);
    }

    static readonly string _logArea = typeof(VertexColorEffect).FullName!;
    static readonly byte* _shaderEntryPoint;
    static VertexColorPlusEffect()
    {
        _shaderEntryPoint = (byte*)NativeMemory.Alloc(5);
        var entryPt = Encoding.ASCII.GetBytes("main" + char.MinValue);
        Marshal.Copy(entryPt, 0, (IntPtr)_shaderEntryPoint, entryPt.Length);
    }

    DescriptorSetLayout _materialsDescriptorSetLayout;

    VulkanDescriptorSetFactory? _materialsDescriptorSetFactory;
    DisposeToken _materialsDescriptorSetFactoryDisposeToken;
    GpuDynamicMemoryBlockPool<Mat>? _materialsMemBlockPool;
    DisposeToken _materialsMemBlockPoolDisposeToken;

    readonly Dictionary<TexturedDescriptorSetsKey, (DescriptorSet[] DescriptorSets, int PoolIndex)> _texturedDescriptorSets = new();
    DynamicDescriptorSetFactory? _texturedMaterialsDescriptorSetFactory;
    DisposeToken _texturedMaterialsDescriptorSetFactoryDisposeToken;

    // Standard, backface `,
    // Use diffuse texture, backface `,
    // Use vertex colors, backface `,
    // Use diffuse texture and vertex colors, backface `
    VertexColorPlusEffectTechnique[]? _effectTechniques;

    int _swapChainImageIndex;
    int _swapChainImageCount;

    VertexColorPlusEffect(Scene scene, string? name)
        : base(scene, name)
    {
    }

    public static VertexColorPlusEffect GetDefault(Scene scene)
    {
        if (scene.GpuDevice is null || scene.EffectsManager is null)
            throw new InvalidOperationException("Scene is not yet initialized (Scene was not created with VulkanDevice or Initialized method was not yet called)");
        return scene.EffectsManager.GetDefault<VertexColorPlusEffect>();
    }

    public static (VertexColorPlusEffect effect, DisposeToken disposeToken) CreateNew(Scene scene, string uniqueEffectName)
    {
        var newEffect = new VertexColorPlusEffect(scene, uniqueEffectName);
        scene.EffectsManager.RegisterEffect(newEffect);
        var disposeToken = new DisposeToken(() => newEffect.CheckAndDispose(true));
        return (newEffect, disposeToken);
    }

    protected override void OnInitializeDeviceResources(VulkanDevice gpuDevice)
    {
        _swapChainImageCount = Scene.SwapChainImagesCount;

        var descriptorTypes = new[] { DescriptorType.StorageBuffer, DescriptorType.CombinedImageSampler };
        _materialsDescriptorSetLayout = gpuDevice.CreateDescriptorSetLayout(descriptorTypes, ShaderStageFlags.Fragment);

        var poolCapacity = Math.Max(16, 8 * Scene.SwapChainImagesCount);

        (_materialsDescriptorSetFactory, _materialsDescriptorSetFactoryDisposeToken) = VulkanDescriptorSetFactory.Create(
            gpuDevice, DescriptorType.StorageBuffer, _materialsDescriptorSetLayout, poolCapacity);
        (_materialsMemBlockPool, _materialsMemBlockPoolDisposeToken) = GpuDynamicMemoryBlockPool<Mat>.Create(
            Scene, Mat.SizeInBytes, Scene.SwapChainImagesCount);
        _materialsMemBlockPool.CreateDescriptorSetsAction = gpuBuffers => _materialsDescriptorSetFactory!.CreateDescriptorSets(gpuBuffers);
        _materialsMemBlockPool.PreventZeroBlockIndex = true;

        (_texturedMaterialsDescriptorSetFactory, _texturedMaterialsDescriptorSetFactoryDisposeToken) = DynamicDescriptorSetFactory.Create(
            gpuDevice, descriptorTypes, _materialsDescriptorSetLayout, poolCapacity);

        // Effect techniques

        
        _effectTechniques = new VertexColorPlusEffectTechnique[8];
        var rasterizationStateCreateInfo = Scene.IsRightHandedCoordinateSystem ? CommonStatesManager.CullCounterClockwise : CommonStatesManager.CullClockwise;
        var rasterizationStateCreateInfoBackface = Scene.IsRightHandedCoordinateSystem ? CommonStatesManager.CullClockwise : CommonStatesManager.CullCounterClockwise;
        // Standard
        var shadersManager = gpuDevice.ShadersManager;
        var shaderStageCreateInfo = createShaderStageCreateInfo(null);
        _effectTechniques[0] = new(Scene, Name + "-Technique")
        {
            MaterialsDescriptorSetLayout = _materialsDescriptorSetLayout,
            ShaderStageCreateInfo = shaderStageCreateInfo,
            RasterizationStateCreateInfo = rasterizationStateCreateInfo,
        };
        _effectTechniques[1] = new(Scene, Name + "-BackfaceTechnique")
        {
            MaterialsDescriptorSetLayout = _materialsDescriptorSetLayout,
            ShaderStageCreateInfo = shaderStageCreateInfo,
            RasterizationStateCreateInfo = rasterizationStateCreateInfoBackface,
        };
        // Use diffuse texture
        var specializationInfo = createSpecializationInfo(true, false);
        shaderStageCreateInfo = createShaderStageCreateInfo(specializationInfo);
        _effectTechniques[2] = new(Scene, Name + "-UseDiffuseTextureTechnique")
        {
            MaterialsDescriptorSetLayout = _materialsDescriptorSetLayout,
            ShaderStageCreateInfo = shaderStageCreateInfo,
            RasterizationStateCreateInfo = rasterizationStateCreateInfo,
        };
        _effectTechniques[3] = new(Scene, Name + "-UseDiffuseTextureBackfaceTechnique")
        {
            MaterialsDescriptorSetLayout = _materialsDescriptorSetLayout,
            ShaderStageCreateInfo = shaderStageCreateInfo,
            RasterizationStateCreateInfo = rasterizationStateCreateInfoBackface,
        };
        // Use vertex colors
        specializationInfo = createSpecializationInfo(false, true);
        shaderStageCreateInfo = createShaderStageCreateInfo(specializationInfo);
        _effectTechniques[4] = new(Scene, Name + "-UseVertexColorsTechnique")
        {
            MaterialsDescriptorSetLayout = _materialsDescriptorSetLayout,
            ShaderStageCreateInfo = shaderStageCreateInfo,
            RasterizationStateCreateInfo = rasterizationStateCreateInfo,
        };
        _effectTechniques[5] = new(Scene, Name + "-UseVertexColorsBackfaceTechnique")
        {
            MaterialsDescriptorSetLayout = _materialsDescriptorSetLayout,
            ShaderStageCreateInfo = shaderStageCreateInfo,
            RasterizationStateCreateInfo = rasterizationStateCreateInfoBackface,
        };
        // Use diffuse texture and vertex colors
        specializationInfo = createSpecializationInfo(true, true);
        shaderStageCreateInfo = createShaderStageCreateInfo(specializationInfo);
        _effectTechniques[6] = new(Scene, Name + "-UseDiffuseTextureAndVertexColorsTechnique")
        {
            MaterialsDescriptorSetLayout = _materialsDescriptorSetLayout,
            ShaderStageCreateInfo = shaderStageCreateInfo,
            RasterizationStateCreateInfo = rasterizationStateCreateInfo,
        };
        _effectTechniques[7] = new(Scene, Name + "-UseDiffuseTextureAndVertexColorsBackfaceTechnique")
        {
            MaterialsDescriptorSetLayout = _materialsDescriptorSetLayout,
            ShaderStageCreateInfo = shaderStageCreateInfo,
            RasterizationStateCreateInfo = rasterizationStateCreateInfoBackface,
        };

        base.OnInitializeDeviceResources(gpuDevice);

        SpecializationInfo* createSpecializationInfo(bool useDiffuseTexture, bool useVertexColors)
        {
            var mapEntries = (SpecializationMapEntry*)NativeMemory.Alloc((nuint)sizeof(SpecializationMapEntry) * 2);
            mapEntries[0] = new SpecializationMapEntry { Offset = 0, Size = 4, ConstantID = 0 };
            mapEntries[1] = new SpecializationMapEntry { Offset = 4, Size = 4, ConstantID = 1 };

            var dataSize = (nuint)8;

            var data = (int*)NativeMemory.Alloc(dataSize);
            data[0] = useDiffuseTexture ? 1 : 0;
            data[1] = useVertexColors ? 1 : 0;

            var specializationInfo = (SpecializationInfo*)NativeMemory.Alloc((nuint)sizeof(SpecializationInfo));
            *specializationInfo = new SpecializationInfo
            {
                MapEntryCount = 2,
                PMapEntries = mapEntries,
                DataSize = dataSize,
                PData = data,
            };
            return specializationInfo;
        }

        PipelineShaderStageCreateInfo[] createShaderStageCreateInfo(SpecializationInfo* specializationInfo)
        {
            return [
                new()
                {
                    SType = StructureType.PipelineShaderStageCreateInfo,
                    PNext = null,
                    Flags = PipelineShaderStageCreateFlags.None,
                    Stage = ShaderStageFlags.Vertex,
                    Module = shadersManager.GetOrCreateShaderModule("VertexColorPlus.vert.spv"),
                    PName = _shaderEntryPoint,
                    PSpecializationInfo = specializationInfo,
                },
                new()
                {
                    SType = StructureType.PipelineShaderStageCreateInfo,
                    PNext = null,
                    Flags = PipelineShaderStageCreateFlags.None,
                    Stage = ShaderStageFlags.Fragment,
                    Module = shadersManager.GetOrCreateShaderModule("VertexColorPlus.frag.spv"),
                    PName = _shaderEntryPoint,
                    PSpecializationInfo = specializationInfo,
                },
            ];
        }
    }

    protected override void Dispose(bool disposing)
    {
        var gpuDevice = Scene.GpuDevice;
        if (gpuDevice is not null)
        {
            if (_materialsDescriptorSetLayout.IsNull() is false)
            {
                gpuDevice.DisposeVulkanResourceOnMainThreadAfterFrameRendered(_materialsDescriptorSetLayout.Handle, typeof(DescriptorSetLayout));
                _materialsDescriptorSetLayout = DescriptorSetLayout.Null;
            }
        }

        if (_materialsDescriptorSetFactory is not null)
        {
            _materialsDescriptorSetFactoryDisposeToken.Dispose();
            _materialsDescriptorSetFactory = null;
        }
        if (_materialsMemBlockPool is not null)
        {
            _materialsMemBlockPoolDisposeToken.Dispose();
            _materialsMemBlockPool = null;
        }

        foreach (var (descriptorSets, index) in _texturedDescriptorSets.Values)
            _texturedMaterialsDescriptorSetFactory?.DestroyDescriptorSets(descriptorSets, index);
        _texturedDescriptorSets.Clear();
        if (_texturedMaterialsDescriptorSetFactory is not null)
        {
            _texturedMaterialsDescriptorSetFactoryDisposeToken.Dispose();
            _texturedMaterialsDescriptorSetFactory = null;
        }

        if (_effectTechniques is not null)
        {
            foreach (var effectTechnique in _effectTechniques)
            {
                foreach (var specializationInfo in effectTechnique.ShaderStageCreateInfo)
                {
                    NativeMemory.Free(specializationInfo.PSpecializationInfo->PMapEntries);
                    NativeMemory.Free(specializationInfo.PSpecializationInfo->PData);
                    NativeMemory.Free(specializationInfo.PSpecializationInfo);
                }
                effectTechnique.Dispose();
            }
            _effectTechniques = null;
        }

            // if (_useDiffuseTextureShaderStageCreateInfo is not null)
            // {
            //     foreach (var shaderStageCreateInfo in _useDiffuseTextureShaderStageCreateInfo)
            //     {
            //         var specializationInfo = (IntPtr)shaderStageCreateInfo.PSpecializationInfo;
            //         if (specializationInfo != IntPtr.Zero)
            //             Scene.GpuDevice?.ReleaseShaderSpecializationInfo(specializationInfo);
            //     }
            // }
            // _useDiffuseTextureShaderStageCreateInfo = [];

        base.Dispose(disposing);
    }

    public override void InitializeMaterial(Material material)
    {
        if (material is not VertexColorPlusMaterial materialAs)
            throw new ArgumentException($"{nameof(VertexColorPlusEffect)} must be used with {nameof(VertexColorMaterial)}", nameof(material));

        CheckIfInitialized();

        if (materialAs.IsInitialized is false)
            materialAs.InitializeSceneResources(Scene);

        var (blockIndex, index) = _materialsMemBlockPool!.GetNextFreeIndex();
        if (index < 0)
            throw new Exception(nameof(_materialsMemBlockPool) + " is full");
        SetMaterialBlock(materialAs, blockIndex, index);

        _updateMaterialData(materialAs);
    }

    public override void DisposeMaterial(Material material)
    {
        // TODO
    }

    public override void UpdateMaterial(Material material)
    {
        if (material is not VertexColorPlusMaterial materialAs)
            throw new ArgumentException($"{nameof(VertexColorPlusEffect)} must be used with {nameof(VertexColorPlusMaterial)}", nameof(material));

        CheckIfInitialized();

        _updateMaterialData(materialAs);
    }

    void _updateMaterialData(VertexColorPlusMaterial material)
    {
        var blockIndex = material.MaterialBlockIndex;
        var index = material.MaterialIndex;

        if (blockIndex < 0 || index < 0)
        {
            Log.Warn?.Write(_logArea, Id, $"Trying to update material '{material.Name}' ({material.Id}) but blockIndex ({blockIndex}) or index ({index}) is not valid.");
            return;
        }

        var memBlock = _materialsMemBlockPool!.GetMemoryBlock(blockIndex, index);
        var memBlockData = memBlock.MemoryBlock.Data;

        memBlockData[index].DiffuseColor = new Color4(material.DiffuseColor, material.Opacity);
        memBlockData[index].SpecularColor = material.SpecularColor;
        memBlockData[index].SpecularPower = material.SpecularPower;
        memBlockData[index].EmissiveColor = material.EmissiveColor;
        memBlockData[index].AlphaClipThreshold = material.AlphaClipThreshold;
        memBlockData[index].IsTwoSided = false;
        memBlockData[index].IsSolidColor = material.IsSolidColor;
        memBlockData[index].BlendVertexColors = material.BlendVertexColors;
        memBlockData[index].VertexColorsOpacity = material.VertexColorsOpacity;
        memBlock.MarkDataChanged();

        var diffuseTexture = material.DiffuseTexture;
        if (diffuseTexture is not null)
        {
            var sampler = material.DiffuseTextureSampler ?? material.Scene?.GpuDevice?.SamplerFactory.MirrorSampler;
            if (sampler is not null)
                _getTexturedDescriptorSets(diffuseTexture, sampler, memBlock, blockIndex);
        }
    }

    public override void ApplyRenderingItemMaterial(RenderingItem renderingItem, Material material, RenderingContext renderingContext)
    {
        if (material is not VertexColorPlusMaterial materialAs)
            throw new ArgumentException($"{nameof(VertexColorPlusEffect)} must be used with {nameof(VertexColorMaterial)}", nameof(material));

        CheckIfDisposed();

        if (renderingItem.ParentSceneNode is ModelNode modelNode)
        {
            var mesh = modelNode.GetMesh();
            if (mesh is not null)
            {
                var vertexColorsBuffer = materialAs.VertexColorsBuffer;
                mesh.SetDataChannelGpuBuffer(MeshDataChannelTypes.VertexColors, vertexColorsBuffer);
                if (vertexColorsBuffer is not null)
                {
                    if (vertexColorsBuffer.ItemsCount < renderingItem.VertexCount)
                        Log.Warn?.Write(_logArea, $"VertexColorPlusEffect warning: vertexColorsBuffer has fewer colors defined ({vertexColorsBuffer.ItemsCount}) then the number of vertices ({renderingItem.VertexCount}) in the RenderingItem (ParentSceneNode.Id: {renderingItem.ParentSceneNode?.Id.ToString() ?? "<null>"})");
                    renderingItem.AdditionalVertexBuffers = [vertexColorsBuffer.Buffer];
                }
            }
        }

        var diffuseTexture = materialAs.DiffuseTexture;
        var sampler = materialAs.DiffuseTextureSampler ?? materialAs.Scene?.GpuDevice?.SamplerFactory.MirrorSampler;
        var useDiffuseTexture = diffuseTexture is not null && sampler is not null;
        var useVertexColors = materialAs.VertexColorsBuffer is not null;

        var effectTechniqueIndex = renderingItem.Flags.HasFlag(RenderingItemFlags.IsBackFaceMaterial) ? 1 : 0;
        if (useDiffuseTexture && useVertexColors)
            effectTechniqueIndex += 6;
        else if (useVertexColors)
            effectTechniqueIndex += 4;
        else if (useDiffuseTexture)
            effectTechniqueIndex += 2;

        var standardEffectTechnique = _effectTechniques![0];
        var effectTechnique = _effectTechniques[effectTechniqueIndex];
        if (standardEffectTechnique.Pipeline.IsNull())
            standardEffectTechnique.CreatePipeline(renderingContext.Width, renderingContext.Height, renderingContext.RenderPass, renderingContext.MultisampleCount, Pipeline.Null);
        if (effectTechnique != standardEffectTechnique && effectTechnique.Pipeline.IsNull())
            effectTechnique.CreatePipeline(renderingContext.Width, renderingContext.Height, renderingContext.RenderPass, renderingContext.MultisampleCount, standardEffectTechnique.Pipeline);
        renderingItem.EffectTechnique = effectTechnique;

        var materialBlockIndex = materialAs.MaterialBlockIndex;
        var materialIndex = materialAs.MaterialIndex;
        var materialMemBlock = _materialsMemBlockPool!.GetMemoryBlockOrDefault(materialBlockIndex, materialIndex);
        renderingItem.MaterialDescriptorSets = useDiffuseTexture ?
            _getTexturedDescriptorSets(diffuseTexture!, sampler!, materialMemBlock, materialBlockIndex) :
            materialMemBlock?.DescriptorSets;
    }

    DescriptorSet[] _getTexturedDescriptorSets(GpuImage diffuseTexture, GpuSampler sampler, GpuDynamicMemoryBlock<Mat>? memBlock, int materialBlockIndex)
    {
        var key = new TexturedDescriptorSetsKey(materialBlockIndex, diffuseTexture.Id, sampler.Id);
        if (_texturedDescriptorSets.TryGetValue(key, out var tuple) is false)
        {
            tuple = _texturedMaterialsDescriptorSetFactory!.CreateDescriptorSets(_swapChainImageCount);
            _texturedDescriptorSets.Add(key, tuple);
        }
        _updateTexturedMaterial(tuple.DescriptorSets, memBlock?.GpuBuffers, diffuseTexture, sampler);
        return tuple.DescriptorSets;
    }

    void _updateTexturedMaterial(DescriptorSet[] descriptorSets, GpuBuffer[]? gpuBuffers, GpuImage diffuseTexture, GpuSampler sampler)
    {
        if (gpuBuffers is null)
            return;

        var gpuDevice = diffuseTexture.GpuDevice;
        var descriptorWriteCount = descriptorSets.Length * 2;
        var descriptorWrites = stackalloc WriteDescriptorSet[descriptorWriteCount];
        diffuseTexture.GetCurrentImageLayoutAndFlags(out _, out var imageLayout, out _);
        var imageInfo = new DescriptorImageInfo
        {
            ImageLayout = imageLayout,
            ImageView = diffuseTexture.ImageView,
            Sampler = sampler.Sampler,
        };

        for (var i = 0; i < descriptorSets.Length; i++)
        {
            var j = i * 2;

            // Material
            var bufferInfo = new DescriptorBufferInfo
            {
                Buffer = gpuBuffers[i].Buffer,
                Offset = 0,
                Range = ulong.MaxValue,
            };
            descriptorWrites[j] = new()
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = descriptorSets[i],
                DstBinding = 0,
                DescriptorType = DescriptorType.StorageBuffer,
                DescriptorCount = 1,
                PBufferInfo = &bufferInfo,
            };

            // Texture
            descriptorWrites[j + 1] = new()
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = descriptorSets[i],
                DstBinding = 1,
                DescriptorType = DescriptorType.CombinedImageSampler,
                DescriptorCount = 1,
                PImageInfo = &imageInfo,
            };
        }

        gpuDevice.Vk.UpdateDescriptorSets(gpuDevice.Device, (uint)descriptorWriteCount, descriptorWrites, 0, null);
    }

    public override void ResetPipelines()
    {
        if (_effectTechniques is not null)
        {
            foreach (var effectTechnique in _effectTechniques)
                effectTechnique.ResetPipeline();
        }
    }

    public override void Cleanup(bool increaseFrameNumber, bool freeEmptyMemoryBlocks)
    {
        _materialsMemBlockPool?.Cleanup(increaseFrameNumber, freeEmptyMemoryBlocks);
    }

    public override void OnBeginUpdate(RenderingContext renderingContext)
    {
        _swapChainImageIndex = renderingContext.CurrentSwapChainImageIndex;
        _swapChainImageCount = renderingContext.SwapChainImagesCount;
    }

    protected override void OnSwapChainImagesCountChanged(int newSwapChainImagesCount)
    {
        _swapChainImageCount = newSwapChainImagesCount;
        base.OnSwapChainImagesCountChanged(newSwapChainImagesCount);
    }

    public override void OnEndUpdate()
    {
        _materialsMemBlockPool?.UpdateDataBlocks(_swapChainImageIndex);
    }
}
