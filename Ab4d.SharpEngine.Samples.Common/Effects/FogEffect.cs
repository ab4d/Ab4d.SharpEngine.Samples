using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Effects;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.Common.Materials;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.Vulkan;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ab4d.SharpEngine.Samples.Common.Effects;

public class FogEffect : Effect
{
    private static readonly string LogArea = typeof(FogEffect).FullName!;

    private FogEffectTechnique? _standardTechnique;

    // All possible techniques for this effect: normal, transparent, back-face, back-face transparent
    private readonly FogEffectTechnique?[] _effectTechniques = new FogEffectTechnique[4];

    private PipelineShaderStageCreateInfo[]? _pipelineShaderStages;
    private PipelineLayout _pipelineLayout;
    private VertexBufferDescription? _vertexBufferDescription;

    private VulkanDescriptorSetFactory? _standardDescriptorPoolFactory;
    private DisposeToken _standardDescriptorPoolFactoryDisposeToken;

    private GpuDynamicMemoryBlockPool<FogMaterialUniformBuffer>? _materialsDataBlockPool;
    private DisposeToken _materialsDataBlockPoolDisposeToken;
    private DescriptorSetLayout _fogMaterialsDescriptorSetLayout;
    private int _swapChainImageIndex;


    // See Resources/Shaders/txt/FogShader.frag.json to see the FieldOffset values
    [StructLayout(LayoutKind.Explicit, Size = SizeInBytes)]
    internal struct FogMaterialUniformBuffer
    {
        [FieldOffset(0)]
        public Color4 DiffuseColor;

        [FieldOffset(16)]
        public float FogStart;

        [FieldOffset(20)]
        public float FogFullColorStart;

        [FieldOffset(32)]
        public Color3 FogColor;

        [FieldOffset(44)]
        private float _dummy; // added to align the size to 16 bytes

        public const int SizeInBytes = 48;
    }


    // Each Effect should have a private constructor.
    // Then an instance of the effect is created by calling a static CreateNew method that 
    // returns an instance of FogEffect and also a DisposeToken that can be used to dispose the effect.
    // This way the effect can be disposed only by the constructor of the effect and
    // not by any user of the effect (there is no public Dispose method).

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
        CheckAndDispose(disposing: true);
    }


    /// <inheritdoc />
    protected override void OnInitializeDeviceResources(VulkanDevice gpuDevice)
    {
        // Use ShadersManager to read and cache the shaders
        // Note that for ShadersManager to be able to read the shaders from assemblie's EmbeddedResource,
        // we need to setup a custom AssemblyShaderBytecodeProvider. See code in the CustomFogEffectSample's constructor.
        var shadersManager = gpuDevice.ShadersManager;
        var vertexShaderStageInfo = shadersManager.GetOrCreatePipelineShaderStage("FogShader.vert.spv", ShaderStageFlags.Vertex, "main");
        var fragmentShaderStageInfo = shadersManager.GetOrCreatePipelineShaderStage("FogShader.frag.spv", ShaderStageFlags.Fragment, "main");

        _pipelineShaderStages = new PipelineShaderStageCreateInfo[] {
            vertexShaderStageInfo,
            fragmentShaderStageInfo
        };

        var fogMaterialsDescriptorSetLayout = gpuDevice.CreateDescriptorSetLayout(DescriptorType.StorageBuffer, ShaderStageFlags.Fragment, "FogEffect-FogMaterialsDescriptorSetLayout");
        _fogMaterialsDescriptorSetLayout = fogMaterialsDescriptorSetLayout;

        _pipelineLayout = gpuDevice.CreatePipelineLayout(
            descriptorSetLayout: new[] { Scene.SceneDescriptorSetLayout,
                                         Scene.AllLightsDescriptorSetLayout,
                                         Scene.AllWorldMatricesDescriptorSetLayout,
                                         fogMaterialsDescriptorSetLayout },
            vertexPushConstantsSize: Scene.StandardVertexShaderPushConstantsSize, // = 4
            fragmentPushConstantsSize: Scene.StandardFragmentShaderPushConstantsSize, // = 4
            name: "FogEffect-PipelineLayout");


        _vertexBufferDescription = gpuDevice.VertexBufferDescriptionsManager.GetPositionNormalTextureVertexBufferDescription();


        int poolCapacity = Math.Max(16, 8 * Scene.SwapChainImagesCount); // Make pool size multiple of swapChainImagesCount because we will allocate in chunks of swapChainImagesCount (this also prevents error that is described in VulkanDescriptorSetFactory.CreateDescriptorSets)
        (_standardDescriptorPoolFactory, _standardDescriptorPoolFactoryDisposeToken) = VulkanDescriptorSetFactory.Create(gpuDevice, DescriptorType.StorageBuffer, fogMaterialsDescriptorSetLayout, poolCapacity, name: "FogEffect-DescriptorPool");


        (_materialsDataBlockPool, _materialsDataBlockPoolDisposeToken) = GpuDynamicMemoryBlockPool<FogMaterialUniformBuffer>.Create(Scene, FogMaterialUniformBuffer.SizeInBytes, Scene.SwapChainImagesCount, "MaterialMemoryBlocks");

        // Assign DescriptorSets to each created memory block
        _materialsDataBlockPool.CreateDescriptorSetsAction = gpuBuffers => _standardDescriptorPoolFactory?.CreateDescriptorSets(gpuBuffers);

        // IMPORTANT:
        // Because material index is also used to tell if we need to multiply normal by -1,
        // the material index must not be 0. So we need to set PreventZeroBlockIndex so the first material index start with 1.
        _materialsDataBlockPool.PreventZeroBlockIndex = true;


        base.OnInitializeDeviceResources(gpuDevice);
    }


    public override void InitializeMaterial(Material material)
    {
        if (material is not FogMaterial fogMaterial)
        {
            // Only FogMaterial can be used with FogEffect => Write log warning
            return;
        }

        CheckIfInitialized();

        Log.Trace?.Write(LogArea, Id, "Initialize material id: {0} (name: {1})", material.Id, material.Name ?? "");

        if (!material.IsInitialized)
            material.InitializeSceneResources(Scene);

        // First set MaterialBlockIndex and MaterialIndex
        EnsureMaterialsDataBlockPool();

        (int dataBlockIndex, int dataIndex) = _materialsDataBlockPool.GetNextFreeIndex();

        if (dataIndex < 0)
            throw new Exception("_materialsData is full");

        //Debug.Assert(dataIndex != 0, "materialDataIndex should not be zero because this prevents rendering backsided materials");

        SetMaterialBlock(material, dataBlockIndex, dataIndex);

        // Now update data
        UpdateMaterialData(fogMaterial);
    }

    public override void UpdateMaterial(Material material)
    {
        CheckIfInitialized();

        Log.Trace?.Write(LogArea, Id, "Update material id: {0} (name: {1})", material.Id, material.Name ?? "");

        if (material is FogMaterial fogMaterial)
            UpdateMaterialData(fogMaterial);
    }

    public override void DisposeMaterial(Material material)
    {
        // TODO;
        //lock (this) // lock access because this may be modified from another thread when DisposeMaterial is called from destructor
        //{
        //    var materialBlockIndex = material.MaterialBlockIndex;
        //    var materialIndex = material.MaterialIndex;

        //    EnsureMaterialsDataBlockPool(); // this makes sure that _materialsDataBlockPool is set
        //    var materialsData = _materialsDataBlockPool.GetMemoryBlockOrDefault(material.MaterialBlockIndex, material.MaterialIndex);

        //    if (materialIndex >= 0 && materialsData != null) 
        //    {
        //        // Free memory block in main thread and when the current frame is rendered
        //        if (Scene.GpuDevice != null)
        //        {
        //            Scene.GpuDevice.FreeMemoryBlockOnMainThreadAfterFrameRendered(materialsData.MemoryBlock, materialIndex);
        //            Scene.NotifyChange(SceneDirtyFlags.MaterialDisposed);
        //        }
        //        else
        //        {
        //            materialsData.MemoryBlock.FreeIndex(materialIndex);
        //        }

        //        material.SetMaterialBlock(-1, -1); // Set MaterialBlockIndex and MaterialIndex to -1
        //    }
        //}

        //Log.Trace?.Write(LogArea, Id, "Material disposed id: {0} (name: {1})", material.Id, material.Name ?? "");
    }

    public override void ApplyRenderingItemMaterial(RenderingItem renderingItem, Material material, RenderingContext renderingContext)
    {
        if (material is not FogMaterial fogMaterial)
        {
            // Only FogMaterial can be used with FogEffect => Write log warning
            return;
        }

        CheckIfDisposed();

        bool hasTransparency = false;

        bool isFrontCounterClockwise = (renderingItem.Flags & RenderingItemFlags.IsBackFaceMaterial) != 0;
        if (!renderingContext.Scene.IsRightHandedCoordinateSystem)
            isFrontCounterClockwise = !isFrontCounterClockwise;

        // Different Vulkan Pipeline objects are required for different values of hasTransparency, isFrontCounterClockwise
        // The GetEffectTechnique gets or creates the required EffectTechnique.
        var effectTechnique = GetEffectTechnique(hasTransparency, isFrontCounterClockwise, renderingContext, out var techniqueIndex);

        if (effectTechnique != null && effectTechnique.Pipeline.IsNull())
            effectTechnique.CreatePipeline(renderingContext, PipelineCreateFlags.AllowDerivatives, effectTechnique.Name);

        renderingItem.EffectTechnique = effectTechnique;


        DescriptorSet[]? descriptorSets;

        int dataBlockIndex = material.MaterialBlockIndex;
        int materialDataIndex = material.MaterialIndex;

        EnsureMaterialsDataBlockPool(); // this makes sure that _materialsDataBlockPool is set

        var materialDataBlock = _materialsDataBlockPool.GetMemoryBlockOrDefault(dataBlockIndex, materialDataIndex);

        if (materialDataBlock != null)
            descriptorSets = materialDataBlock.DescriptorSets;
        else
            descriptorSets = null;

        renderingItem.MaterialDescriptorSets = descriptorSets;


        // When no DescriptorSets is used, then set NoMaterialDescriptorSets Flags:
        //renderingItem.Flags |= RenderingItemFlags.NoMaterialDescriptorSets; // this allows setting MaterialDescriptorSets to null
        //renderingItem.MaterialDescriptorSets =  null;


        // Set StateSortValue that can be used to group same techniques together and reduce state (pipeline changes
        uint stateSortValue;
        unchecked
        {
            stateSortValue = (uint)(Id << 8);
        }

        if (isFrontCounterClockwise) // is back material
            stateSortValue -= (uint)techniqueIndex; // make stateSortValue for back material smaller so back materials are rendered before front materials
        else
            stateSortValue += (uint)techniqueIndex;

        renderingItem.StateSortValue = stateSortValue;
    }

    public override void ResetPipelines()
    {
        for (var i = 0; i < _effectTechniques.Length; i++)
            _effectTechniques[i]?.ResetPipeline();
    }

    /// <inheritdoc />
    public override void OnBeginUpdate(RenderingContext renderingContext)
    {
        _swapChainImageIndex = renderingContext.CurrentSwapChainImageIndex;
    }

    /// <inheritdoc />
    public override void OnEndUpdate()
    {
        EnsureMaterialsDataBlockPool(); // this makes sure that _materialsDataBlockPool is set

        _materialsDataBlockPool.UpdateDataBlocks(_swapChainImageIndex);
    }

    public override void Cleanup(bool increaseFrameNumber, bool freeEmptyMemoryBlocks)
    {
        EnsureMaterialsDataBlockPool(); // this makes sure that _materialsDataBlockPool is set

        // Dispose empty memory blocks
        _materialsDataBlockPool.Cleanup(increaseFrameNumber, freeEmptyMemoryBlocks, memoryBlockDisposedCallback: null);
    }

    private void EnsureStandardTechniqueWithPipeline(RenderingContext renderingContext)
    {
        if (_standardTechnique != null && !_standardTechnique.Pipeline.IsNull() || _vertexBufferDescription == null)
            return; // Exist early if _standardTechnique is already created

        if (_standardTechnique == null)
        {
            _standardTechnique = new FogEffectTechnique(Scene, "FogEffect-StandardTechnique")
            {
                VertexInputStatePtr                         = _vertexBufferDescription.PipelineVertexInputStateCreateInfoPtr,
                ShaderStages                                = _pipelineShaderStages,
                PipelineLayout                              = _pipelineLayout,
                ColorBlendAttachmentState                   = CommonStatesManager.OpaqueAttachmentState,
                RasterizationState                          = CommonStatesManager.CullCounterClockwise,
                UseNegativeMaterialIndexForBackFaceMaterial = true
            };

            _effectTechniques[0] = _standardTechnique; // Also set _standardTechnique as first technique in _effectTechniques
        }

        // NOTE: When RenderPass is changed (for example adding new rendering stages for shadows, post-processes, etc.), then ResetPipelines must be called to clear the existing Pipelines
        _standardTechnique.CreatePipeline(renderingContext, PipelineCreateFlags.AllowDerivatives, _standardTechnique.Name);
        _standardTechnique.PipelineLayout = _pipelineLayout;
    }

    private FogEffectTechnique? GetEffectTechnique(bool hasTransparency, bool isFrontCounterClockwise, RenderingContext renderingContext, out int techniqueIndex)
    {
        if (_vertexBufferDescription == null)
        {
            if (_vertexBufferDescription == null)
                Log.Warn?.Write(LogArea, Id, "Trying to get EffectTechnique but _vertexBufferDescription is null");

            techniqueIndex = 0;
            return null;
        }

        // In any case we need the _standardTechnique
        // If we are creating some other non-standard technique then we need it to create DerivativePipeline
        EnsureStandardTechniqueWithPipeline(renderingContext);


        techniqueIndex = GetEffectTechniqueIndex(hasTransparency, isFrontCounterClockwise);
        var effectTechnique = _effectTechniques[techniqueIndex];

        if (effectTechnique == null)
        {
            if (techniqueIndex == 0)
            {
                effectTechnique = _standardTechnique!;
            }
            else
            {
                string techniqueName = (hasTransparency ? "Transparent" : "") +
                                       (isFrontCounterClockwise ? "FrontCounterClockwise" : "") +
                                       "Technique";

                Log.Trace?.Write(LogArea, Id, "Creating EffectTechnique: FogEffect-{0} (index: {1})", techniqueName, techniqueIndex);

                effectTechnique = new FogEffectTechnique(Scene, "FogEffect-" + techniqueName)
                {
                    VertexInputStatePtr                         = _vertexBufferDescription.PipelineVertexInputStateCreateInfoPtr,
                    ShaderStages                                = _pipelineShaderStages,
                    PipelineLayout                              = _pipelineLayout,
                    UseNegativeMaterialIndexForBackFaceMaterial = true
                };

                if (hasTransparency)
                {
                    //if (isPreMultipliedAlpha)
                    effectTechnique.ColorBlendAttachmentState = CommonStatesManager.PremultipliedAlphaBlendAttachmentState;
                    //else
                    //    effectTechnique.ColorBlendAttachmentState = CommonStatesManager.NonPremultipliedAlphaBlendAttachmentState;
                }

                if (isFrontCounterClockwise)
                    effectTechnique.RasterizationState = CommonStatesManager.CullClockwise;

                _effectTechniques[techniqueIndex] = effectTechnique;
            }
        }

        if (effectTechnique.Pipeline.IsNull())
        {
            // NOTE: When RenderPass is changed (for example adding new rendering stages for shadows, post-processes, etc.), then ResetPipelines must be called to clear the existing Pipelines
            effectTechnique.CreateDerivativePipeline(renderingContext,
                                                     PipelineCreateFlags.Derivative,
                                                     _standardTechnique!.Pipeline,
                                                     effectTechnique.Name);
        }

        return effectTechnique;
    }

    private int GetEffectTechniqueIndex(bool hasTransparency, bool isFrontCounterClockwise)
    {
        // 0 is _standardTechnique
        return (hasTransparency ? 1 : 0) +
               (isFrontCounterClockwise ? 2 : 0); // is back material
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [MemberNotNull(nameof(_materialsDataBlockPool))]
    private void EnsureMaterialsDataBlockPool()
    {
        if (_materialsDataBlockPool != null)
            return;

        if (Scene == null)
            throw new SharpEngineException("Cannot create GpuDynamicMemoryBlockPool because Scene is null");

        (_materialsDataBlockPool, _materialsDataBlockPoolDisposeToken) = GpuDynamicMemoryBlockPool<FogMaterialUniformBuffer>.Create(Scene, FogMaterialUniformBuffer.SizeInBytes, Scene.SwapChainImagesCount, "FogMaterialMemoryBlocks");

        // Assign DescriptorSets to each created memory block
        _materialsDataBlockPool.CreateDescriptorSetsAction = gpuBuffers => _standardDescriptorPoolFactory?.CreateDescriptorSets(gpuBuffers);

        // IMPORTANT:
        // Because material index is also used to tell if we need to multiply normal by -1,
        // the material index must not be 0. So we need to set PreventZeroBlockIndex so the first material index start with 1.
        _materialsDataBlockPool.PreventZeroBlockIndex = true;

        // MinBlockSize and MaxBlockSize are by default set to:
        //_materialsDataBlockPool.MinBlockSize = 4096;  // 4 Kb
        //_materialsDataBlockPool.MaxBlockSize = 65536; // 64 Kb
    }

    private void UpdateMaterialData(FogMaterial fogMaterial)
    {
        int dataBlockIndex = fogMaterial.MaterialBlockIndex;
        int materialDataIndex = fogMaterial.MaterialIndex;

        if (dataBlockIndex < 0 || materialDataIndex < 0)
        {
            Log.Warn?.Write(LogArea, Id, $"Trying to update material '{fogMaterial.Name ?? ""}' ({fogMaterial.Id}) but dataBlockIndex ({dataBlockIndex}) or materialDataIndex ({materialDataIndex}) is not valid.");
            return;
        }

        EnsureMaterialsDataBlockPool(); // this makes sure that _materialsDataBlockPool is set

        var materialDataBlock = _materialsDataBlockPool.GetMemoryBlock(dataBlockIndex, materialDataIndex);


        // We need to pre-multiply alpha
        var alpha = fogMaterial.Opacity;
        var blockData = materialDataBlock.MemoryBlock.Data;

        if (fogMaterial.IsPreMultipliedAlphaColor)
        {
            // Already in pre-multiplied alpha
            blockData[materialDataIndex].DiffuseColor = new Color4(fogMaterial.DiffuseColor.Red,
                                                                   fogMaterial.DiffuseColor.Green,
                                                                   fogMaterial.DiffuseColor.Blue,
                                                                   alpha);
        }
        else
        {
            // Convert to pre-multiplied alpha
            blockData[materialDataIndex].DiffuseColor = new Color4(fogMaterial.DiffuseColor.Red * alpha,
                                                                   fogMaterial.DiffuseColor.Green * alpha,
                                                                   fogMaterial.DiffuseColor.Blue * alpha,
                                                                   alpha);
        }

        blockData[materialDataIndex].FogStart           = fogMaterial.FogStart;
        blockData[materialDataIndex].FogFullColorStart  = fogMaterial.FogFullColorStart;
        blockData[materialDataIndex].FogColor           = fogMaterial.FogColor;

        // Mark that we need to copy the _materialsData to the _materialUniformBuffers
        materialDataBlock.MarkDataChanged();
    }

    /// <inheritdoc />
    protected override unsafe void Dispose(bool disposing)
    {
        ResetPipelines(); // This will dispose created pipelines (after current rendering is complete)

        // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract - in case of call from finalizer

        if (Scene.GpuDevice != null)
        {
            if (disposing && _materialsDataBlockPool != null)
            {
                _materialsDataBlockPoolDisposeToken.Dispose();
                _materialsDataBlockPool = null;
            }

            _vertexBufferDescription = null; // Do not dispose this here

            // We can immediately dispose ShaderSpecializationInfo because this is used for creation of new Pipelines and we will not do that anymore
            if (_pipelineShaderStages != null)
            {
                for (var i = 0; i < _pipelineShaderStages.Length; i++)
                {
                    var ptr = (IntPtr)_pipelineShaderStages[i].PSpecializationInfo;
                    if (ptr != IntPtr.Zero)
                        Scene.GpuDevice.ReleaseShaderSpecializationInfo(ptr);
                }

                _pipelineShaderStages = null;
            }

            if (!_pipelineLayout.IsNull())
            {
                // We must not immediately dispose the resources, because they may be currently used if frames are rendered in the background.
                // To solve that call the DisposeVulkanResourceOnMainThreadAfterFrameRendered that will wait until the current frame is rendered,
                // or dispose the resources immediately  when no frame is currently being rendered.
                Scene.GpuDevice.DisposeVulkanResourceOnMainThreadAfterFrameRendered(_pipelineLayout.Handle, typeof(PipelineLayout));
                _pipelineLayout = PipelineLayout.Null;
            }

            if (!_fogMaterialsDescriptorSetLayout.IsNull())
            {
                Scene.GpuDevice.DisposeVulkanResourceOnMainThreadAfterFrameRendered(_fogMaterialsDescriptorSetLayout.Handle, typeof(DescriptorSetLayout));
                _fogMaterialsDescriptorSetLayout = DescriptorSetLayout.Null;
            }

            if (_materialsDataBlockPool != null)
            {
                _materialsDataBlockPoolDisposeToken.Dispose();
                _materialsDataBlockPool = null;
            }

            if (disposing)
            {
                if (_standardDescriptorPoolFactory != null)
                {
                    _standardDescriptorPoolFactoryDisposeToken.Dispose();
                    _standardDescriptorPoolFactory = null;
                }
            }
        }

        if (_effectTechniques != null)
        {
            for (var i = 0; i < _effectTechniques.Length; i++)
                _effectTechniques[i] = null;

            _standardTechnique = null;
        }

        base.Dispose(disposing);

        // ReSharper restore ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
    }
}