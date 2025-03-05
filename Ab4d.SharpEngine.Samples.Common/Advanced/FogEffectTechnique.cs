using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Effects;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.Vulkan;
using System.Runtime.CompilerServices;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

public class FogEffectTechnique : EffectTechnique
{
    public PipelineShaderStageCreateInfo[]? ShaderStages;

    public IntPtr VertexInputStatePtr;

    public PipelineInputAssemblyStateCreateInfo InputAssemblyState;
    public PipelineRasterizationStateCreateInfo RasterizationState;
    public PipelineColorBlendAttachmentState ColorBlendAttachmentState;
    public PipelineDepthStencilStateCreateInfo DepthStencilState;
    public PipelineTessellationStateCreateInfo TessellationState;

    public List<DynamicState> AdditionalDynamicStates { get; set; }

    public PipelineLayout PipelineLayout;

    public Pipeline Pipeline { get; private set; }

        public ShaderStageFlags MatrixPushConstantShaderStages = ShaderStageFlags.Vertex;

    // For ThickLineEffect this is set to ShaderStageGeometryBit | ShaderStageFragmentBit
    public ShaderStageFlags MaterialPushConstantShaderStages = ShaderStageFlags.Fragment;

    // If we want to correctly calculate the lights for back-face materials, we need to multiply normal by -1.
    // In StandardEffect this is signaled to fragment shader by setting materialIndex to a negative value.
    // This is enabled by setting UseNegativeMaterialIndexForBackFaceMaterial field to true.
    public bool UseNegativeMaterialIndexForBackFaceMaterial = false;


    private int _sceneDescriptorSetBindingIndex = 0;

    /// <summary>
    /// Gets or sets the binding index of the SceneDescriptorSet that is used when calling CmdBindDescriptorSets method. Default value is 0.
    /// </summary>
    public int SceneDescriptorSetBindingIndex
    {
        get => _sceneDescriptorSetBindingIndex;
        set
        {
            if (value < 0 || value > RenderingContext.MaxBoundDescriptorSetIndex)
                throw new ArgumentOutOfRangeException(nameof(SceneDescriptorSetBindingIndex));

            _sceneDescriptorSetBindingIndex = value;
        }
    }


    private int _allLightsDescriptorSetBindingIndex = 1;

    /// <summary>
    /// Gets or sets the binding index of the AllLightsDescriptorSet that is used when calling CmdBindDescriptorSets method. Default value is 1.
    /// </summary>
    public int AllLightsDescriptorSetBindingIndex
    {
        get => _allLightsDescriptorSetBindingIndex;
        set
        {
            if (value < 0 || value > RenderingContext.MaxBoundDescriptorSetIndex)
                throw new ArgumentOutOfRangeException(nameof(AllLightsDescriptorSetBindingIndex));

            _allLightsDescriptorSetBindingIndex = value;
        }
    }
    
    
    private int _matricesDescriptorSetBindingIndex = 2;

    /// <summary>
    /// Gets or sets the binding index of the MatricesDescriptorSet that is used when calling CmdBindDescriptorSets method. Default value is 2.
    /// </summary>
    public int MatricesDescriptorSetBindingIndex
    {
        get => _matricesDescriptorSetBindingIndex;
        set
        {
            if (value < 0 || value > RenderingContext.MaxBoundDescriptorSetIndex)
                throw new ArgumentOutOfRangeException(nameof(MatricesDescriptorSetBindingIndex));

            _matricesDescriptorSetBindingIndex = value;
        }
    }
    
    
    private int _materialDescriptorSetBindingIndex = 3;

    /// <summary>
    /// Gets or sets the binding index of the MaterialDescriptorSet that is used when calling CmdBindDescriptorSets method. Default value is 3.
    /// </summary>
    public int MaterialDescriptorSetBindingIndex
    {
        get => _materialDescriptorSetBindingIndex;
        set
        {
            if (value < 0 || value > RenderingContext.MaxBoundDescriptorSetIndex)
                throw new ArgumentOutOfRangeException(nameof(MaterialDescriptorSetBindingIndex));

            _materialDescriptorSetBindingIndex = value;
        }
    }

    public FogEffectTechnique(Scene scene, string? name = null)
        : base(scene, name)
    {
        // Set default values
        InputAssemblyState        = CommonStatesManager.TriangleListInputAssemblyState;
        RasterizationState        = CommonStatesManager.CullCounterClockwise;
        ColorBlendAttachmentState = CommonStatesManager.OpaqueAttachmentState;
        DepthStencilState         = CommonStatesManager.DepthReadWrite;

        TessellationState = new PipelineTessellationStateCreateInfo()
        {
            SType              = StructureType.PipelineTessellationStateCreateInfo,
            Flags              = 0,
            PatchControlPoints = 0
        };

        AdditionalDynamicStates = new List<DynamicState>();
    }

    public Pipeline CreatePipeline(RenderPass renderPass,
                               float width,
                               float height, // height can be negative when SceneView.IsYAxisUp is true
                               SampleCountFlags multisampleCount,
                               PipelineCreateFlags flags,
                               string? pipelineName)
    {
        return CreatePipeline(renderPass, width, height, multisampleCount, flags, Pipeline.Null, pipelineName);
    }

    public Pipeline CreatePipeline(RenderingContext renderingContext,
                                   PipelineCreateFlags flags,
                                   string? pipelineName)
    {
        return CreatePipeline(renderingContext.CurrentRenderPass,
                              renderingContext.Width,
                              renderingContext.Height,
                              renderingContext.SceneView.UsedMultiSampleCountFlags,
                              flags,
                              Pipeline.Null,
                              pipelineName);
    }

    private Pipeline CreateDerivativePipeline(RenderPass renderPass,
                                              float width,
                                              float height, // height can be negative when SceneView.IsYAxisUp is true
                                              SampleCountFlags multisampleCount,
                                              PipelineCreateFlags flags,
                                              Pipeline parentPipeline,
                                              string? pipelineName)
    {
        if (parentPipeline.IsNull())
            throw new SharpEngineException("CreateDerivativePipeline called without parentPipeline set");

        return CreatePipeline(renderPass, width, height, multisampleCount, flags, parentPipeline, pipelineName);
    }

    public Pipeline CreateDerivativePipeline(RenderingContext renderingContext,
                                             PipelineCreateFlags flags,
                                             Pipeline parentPipeline,
                                             string? pipelineName)
    {
        if (parentPipeline.IsNull())
            throw new SharpEngineException("CreateDerivativePipeline called without parentPipeline set");

        return CreatePipeline(renderingContext.CurrentRenderPass,
                              renderingContext.Width,
                              renderingContext.Height,
                              renderingContext.SceneView.UsedMultiSampleCountFlags,
                              flags,
                              parentPipeline,
                              pipelineName);
    }

    private unsafe Pipeline CreatePipeline(RenderPass renderPass,
                                           float width,
                                           float height, // height can be negative when SceneView.IsYAxisUp is true
                                           SampleCountFlags multisampleCount,
                                           PipelineCreateFlags flags,
                                           Pipeline parentPipeline,
                                           string? pipelineName)
    {
        if (Scene == null)
            throw new SharpEngineException("Effect not initialized in CreatePipeline");

        if (ShaderStages == null)
            throw new SharpEngineException("ShaderStages must not be null in CreatePipeline");

        if (VertexInputStatePtr == IntPtr.Zero)
            throw new SharpEngineException("VertexInputStatePtr must be set in CreatePipeline");


        if (!parentPipeline.IsNull())
            flags |= PipelineCreateFlags.Derivative;


        //Log.Info?.Write(LogArea, "CreatePipeline('{0}': {1} x {2} (multisample: {3}; flags: {4})", pipelineName ?? "", width, height, multisampleCount, flags);


        if (!IsInitialized)
            InitializeDeviceResources(); // This will also check if Scene is initialized

        var vulkanDevice = this.Scene.GpuDevice;

        if (vulkanDevice == null)
            throw new SharpEngineException("Scene.GpuDevice not initialized in CreatePipeline");


        // We use dynamic viewport and scissor, but anyway we set them here.

        // Viewport specifies how the normalized device coordinates are transformed into the pixel coordinates of the framebuffer
        // Scissor is the area where you can render, this is similar to viewport in that regard but changing the scissor rectangle doesn't affect the coordinates.

        Viewport viewport;

        // Vulkan by default uses right coordinate system with y axis down (?!). 
        // To enable y axis up, we need to define negative height.
        // See comments in EffectsManager.IsYAxisUpInClipSpace
        if (height < 0)
            viewport = new Viewport(0, -height, width, height, 0, 1);
        else
            viewport = new Viewport(0, 0, width, height, 0, 1);

        var scissor = new Rect2D(new Offset2D(0, 0), new Extent2D((uint)width, (uint)Math.Abs(height)));

        var viewportCreateInfo = new PipelineViewportStateCreateInfo
        {
            SType = StructureType.PipelineViewportStateCreateInfo,
            ViewportCount = 1,
            PViewports = &viewport,
            ScissorCount = 1,
            PScissors = &scissor,
        };


        // With using dynamic Viewport and Scissor we do not need to recreate pipeline objects where resizing
        // https://www.khronos.org/registry/vulkan/specs/1.2-extensions/man/html/DynamicState.html

        PipelineDynamicStateCreateInfo vkPipelineDynamicStateCreateInfo;

        // Start with global dynamic states
        var dynamicStates = VulkanDevice.GlobalDynamicStates;

        // Add additional dynamic states if defined
        if (AdditionalDynamicStates.Count > 0)
        {
            if (dynamicStates != null)
            {
                int startIndex = dynamicStates.Length;

                Array.Resize(ref dynamicStates, dynamicStates.Length + AdditionalDynamicStates.Count);

                for (int i = 0; i < AdditionalDynamicStates.Count; i++)
                    dynamicStates[startIndex + i] = AdditionalDynamicStates[i];
            }
            else
            {
                dynamicStates = AdditionalDynamicStates.ToArray();
            }
        }


        if (dynamicStates == null || dynamicStates.Length == 0)
        {
            vkPipelineDynamicStateCreateInfo = new PipelineDynamicStateCreateInfo()
            {
                SType = StructureType.PipelineDynamicStateCreateInfo,
                Flags = 0,
                DynamicStateCount = 0,
                PDynamicStates = null
            };
        }
        else
        {
            var dynamicState = stackalloc DynamicState[dynamicStates.Length];
            for (int i = 0; i < dynamicStates.Length; i++)
                dynamicState[i] = dynamicStates[i];

            vkPipelineDynamicStateCreateInfo = new PipelineDynamicStateCreateInfo()
            {
                SType = StructureType.PipelineDynamicStateCreateInfo,
                Flags = 0,
                DynamicStateCount = (uint)dynamicStates.Length,
                PDynamicStates = dynamicState
            };
        }


        var localShaderStages = stackalloc PipelineShaderStageCreateInfo[ShaderStages.Length];
        for (int i = 0; i < ShaderStages.Length; i++)
            localShaderStages[i] = ShaderStages[i];


        var vkPipelineMultisampleStateCreateInfo = new PipelineMultisampleStateCreateInfo
        {
            SType = StructureType.PipelineMultisampleStateCreateInfo,
            RasterizationSamples = multisampleCount
        };


        // Copy to local struct on the stack so we can get the address
        var localRasterizationState = RasterizationState;
        var localTessellationState = TessellationState;
        var localInputAssemblyState = InputAssemblyState;
        var localDepthStencilStateCreateInfo = DepthStencilState;

        // We cannot store PipelineColorBlendStateCreateInfo because using native memory because it uses address of localColorBlendAttachmentState.
        // Therefore we store only ColorBlendAttachmentState and now create the PipelineColorBlendStateCreateInfo so that address of localColorBlendAttachmentState will be still valid when creating the pipeline.
        var localColorBlendAttachmentState = ColorBlendAttachmentState;
        var localColorBlendState = new PipelineColorBlendStateCreateInfo
        {
            SType = StructureType.PipelineColorBlendStateCreateInfo,
            LogicOpEnable = false,
            LogicOp = LogicOp.Copy,
            AttachmentCount = 1,
            PAttachments = &localColorBlendAttachmentState,
        };

        var pipelineCreateInfo = new GraphicsPipelineCreateInfo
        {
            SType = StructureType.GraphicsPipelineCreateInfo,
            Layout = PipelineLayout,
            PViewportState = &viewportCreateInfo,
            StageCount = (uint)ShaderStages.Length,
            PStages = localShaderStages,
            PDepthStencilState = &localDepthStencilStateCreateInfo,
            PDynamicState = &vkPipelineDynamicStateCreateInfo,
            PMultisampleState = &vkPipelineMultisampleStateCreateInfo,
            PColorBlendState = &localColorBlendState,
            PRasterizationState = &localRasterizationState,
            PTessellationState = &localTessellationState,
            PInputAssemblyState = &localInputAssemblyState,
            PVertexInputState = (PipelineVertexInputStateCreateInfo*)VertexInputStatePtr,
            RenderPass = renderPass,
            BasePipelineHandle = parentPipeline,
            BasePipelineIndex = -1,
            Subpass = 0,
            Flags = flags
        };


        Pipeline pipeline;
        vulkanDevice.Vk.CreateGraphicsPipelines(vulkanDevice.Device, vulkanDevice.GetPipelineCache(), 1, &pipelineCreateInfo, null, &pipeline);//.LogAndCheckResult(LogArea, "vkCreateGraphicsPipeline for " + this.Name);

        if (pipelineName != null)
        {
            if (!pipelineName.Contains("Pipeline"))
                pipelineName += "-Pipeline";

            pipeline.SetName(vulkanDevice, pipelineName);
        }

        this.Pipeline = pipeline;

        return pipeline;
    }

    /// <summary>
    /// ResetPipeline delay disposes the Pipeline and set it to null
    /// </summary>
    public override void ResetPipeline()
    {
        if (this.Scene.GpuDevice == null)
            return;

        var pipeline = Pipeline;
        if (!pipeline.IsNull())
        {
            // The pipeline may still be used so we need to dispose it after rendering is complete
            this.Scene.GpuDevice.DisposeVulkanResourceOnMainThreadAfterFrameRendered(pipeline.Handle, typeof(Pipeline));

            Pipeline = Pipeline.Null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public unsafe override void Render(CommandBuffer commandBuffer, RenderingItem renderingItem, RenderingContext renderingContext)
    {
        int vertexBufferChangesCount = 0;
        int indexBufferChangesCount = 0;
        int descriptorSetChangesCount = 0;
        int pushConstantsChangesCount = 0;

        var vk = renderingContext.GpuDevice.Vk;

        var currentBoundDescriptorSets = renderingContext.CurrentBoundDescriptorSets;
        var renderingItemFlags = renderingItem.Flags;

        bool isPipelineChanged = renderingContext.CurrentPipeline != Pipeline;

        if (isPipelineChanged)
        {
            if (Pipeline.IsNull())
            {
                //Log.Warn?.Write(LogArea, Id, $"Skipping rendering RenderingItem '{renderingItem.ToString()}' because its StandardEffectTechnique.Pipeline is null.");
                return;
            }
            
            if (PipelineLayout.IsNull())
            {
                //Log.Warn?.Write(LogArea, Id, $"Skipping rendering RenderingItem '{renderingItem.ToString()}' because its StandardEffectTechnique.PipelineLayout is null.");
                return;
            }

            vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, Pipeline);

            renderingContext.CurrentPipeline = Pipeline;
            renderingContext.PipelineChangesCount++;
        }


        // Changing Pipeline will reset all bound descriptor set, so we will need to bind them again
        // This is used in ThickLineEffect (see GeometryShaderLineRasterizer.ApplyRenderingItemMaterial method)
        // We also need to bind again when a standard PipelineLayout is used after non-standard one
        bool isNonStandardPipelineLayout = renderingItemFlags.HasFlag(RenderingItemFlags.NonStandardPipelineLayout);
            
        if (isPipelineChanged || isNonStandardPipelineLayout != renderingContext.IsNonStandardPipelineLayout)
        {
            for (var i = 0; i < currentBoundDescriptorSets.Length; i++)
                currentBoundDescriptorSets[i] = DescriptorSet.Null;

            renderingContext.IsNonStandardPipelineLayout = isNonStandardPipelineLayout;
        }


        // Bind SceneDescriptorSet as set 0
        var descriptorSet = renderingContext.SceneDescriptorSet;
        if (descriptorSet != currentBoundDescriptorSets[_sceneDescriptorSetBindingIndex])
        {
            vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, PipelineLayout, (uint)_sceneDescriptorSetBindingIndex, 1, &descriptorSet, 0, null);
            currentBoundDescriptorSets[_sceneDescriptorSetBindingIndex] = descriptorSet;
        }

        // Bind LightsDescriptorSet as set 1
        descriptorSet = renderingContext.AllLightsDescriptorSet;
        if (descriptorSet != currentBoundDescriptorSets[_allLightsDescriptorSetBindingIndex])
        {
            vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, PipelineLayout, (uint)_allLightsDescriptorSetBindingIndex, 1, &descriptorSet, 0, null);
            currentBoundDescriptorSets[_allLightsDescriptorSetBindingIndex] = descriptorSet;
        }


        var buffer = renderingItem.VertexBuffer;
        var additionalVertexBuffers = renderingItem.AdditionalVertexBuffers;

        if (buffer.IsNull())
        {
            //if (!renderingItemFlags.HasFlag(RenderingItemFlags.NoVertexBuffer))
            //    Log.Warn?.Write(LogArea, Id, $"Skipping rendering RenderingItem '{renderingItem.ToString()}' because its VertexBuffer is null. If this is intended, then set the NoVertexBuffer to RenderingItem.Flags.");
            
            return;
        }

        if (buffer != renderingContext.CurrentVertexBuffer)
        {
            ulong offsets = 0;
            vk.CmdBindVertexBuffers(commandBuffer, 0, 1, &buffer, &offsets);
            vertexBufferChangesCount++;

            renderingContext.CurrentVertexBuffer = buffer;
        }

        // Check if some additional vertex buffers are provided
        // NOTE: Vulkan does not require to unbind unused vertex buffer (actually binding to null would produce an error; except when nullDescriptor feature is enabled - https://www.khronos.org/registry/vulkan/specs/1.2-extensions/html/vkspec.html#features-nullDescriptor)
        if (additionalVertexBuffers != null)
        {
            for (var j = 0; j < additionalVertexBuffers.Length; j++)
            {
                buffer = additionalVertexBuffers[j];

                if (!buffer.IsNull())
                {
                    ulong offsets = 0;
                    vk.CmdBindVertexBuffers(commandBuffer, (uint)(1 + j), 1, &buffer, &offsets);
                    vertexBufferChangesCount++;
                }
            }
        }


        var indexBuffer = renderingItem.IndexBuffer;
        if (indexBuffer != renderingContext.CurrentIndexBuffer)
        {
            renderingContext.CurrentIndexBuffer = indexBuffer;

            if (!indexBuffer.IsNull())
            {
                vk.CmdBindIndexBuffer(commandBuffer, indexBuffer, 0, renderingItem.IndexBufferType);
                indexBufferChangesCount = 1;
            }
        }



        int swapChainImageIndex = renderingContext.CurrentSwapChainImageIndex;

        var pipelineLayout = PipelineLayout;

        // Bind model matrix descriptor set and set the dynamic offset
        var descriptorSets = renderingItem.MatricesDescriptorSets;

        if (descriptorSets != null &&
            swapChainImageIndex < descriptorSets.Length)
        {
            descriptorSet = descriptorSets[swapChainImageIndex];
            if (descriptorSet != currentBoundDescriptorSets[2])
            {
                vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, pipelineLayout, firstSet: (uint)_matricesDescriptorSetBindingIndex, descriptorSetCount: 1, pDescriptorSets: (DescriptorSet*)&descriptorSet, dynamicOffsetCount: 0, pDynamicOffsets: null);

                currentBoundDescriptorSets[_matricesDescriptorSetBindingIndex] = descriptorSet;
                descriptorSetChangesCount++;
            }

            // Set matrix index with using PushConstants
            int matrixIndex = renderingItem.MatrixIndex;

            if (matrixIndex >= 0 && matrixIndex != renderingContext.CurrentMatrixIndex)
            {
                vk.CmdPushConstants(commandBuffer, pipelineLayout, MatrixPushConstantShaderStages, offset: 0, size: 4, pValues: &matrixIndex);
                renderingContext.CurrentMatrixIndex = matrixIndex;
                pushConstantsChangesCount++;
            }
        }
        else
        {
            if (descriptorSets == null)
            {
                //if (!renderingItemFlags.HasFlag(RenderingItemFlags.NoMatricesDescriptorSets))
                //    Log.Warn?.Write(LogArea, Id, $"Skipping rendering RenderingItem '{renderingItem.ToString()}' because its MatricesDescriptorSets is null. If this is intended, then set the NoMatricesDescriptorSets to RenderingItem.Flags.");
             
                return;
            }

            if (swapChainImageIndex >= descriptorSets.Length)
            {
                //Log.Warn?.Write(LogArea, Id, $"Skipping rendering RenderingItem '{renderingItem.ToString()}' because swapChainImageIndex ({swapChainImageIndex}) >= renderingItem.MatricesDescriptorSets.Length ({descriptorSets.Length}). Make sure that the number of MatricesDescriptorSets is the same as created SwapChain images.");
                return;
            }
        }


        // Bind materials descriptor set
        descriptorSets = renderingItem.MaterialDescriptorSets;

        if (descriptorSets != null &&
            swapChainImageIndex < descriptorSets.Length)
        {
            descriptorSet = descriptorSets[swapChainImageIndex];
            if (descriptorSet != currentBoundDescriptorSets[3])
            {
                vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, pipelineLayout, firstSet: (uint)_materialDescriptorSetBindingIndex, descriptorSetCount: 1, pDescriptorSets: (DescriptorSet*)&descriptorSet, dynamicOffsetCount: 0, pDynamicOffsets: null);

                currentBoundDescriptorSets[_materialDescriptorSetBindingIndex] = descriptorSet;
                descriptorSetChangesCount++;
            }

            // When materialIndex is bigger or equal then 0, then PushConstant defines the index in the bound uniform buffer that contains multiple materials.
            // If MaterialIndex is smaller then zero, then no PushConstant is defied - assuming each renderingItem defines its own descriptorSet with all the required material data.
            int materialIndex = renderingItem.MaterialIndex;

            if (materialIndex >= 0)
            {
                // When we are rendering back face material we need to multiply normal by -1.
                // This is signaled to fragment shader by setting materialIndex to negative value.
                if ((renderingItemFlags & RenderingItemFlags.IsBackFaceMaterial) != 0)
                    materialIndex = -materialIndex;

                if (materialIndex != renderingContext.CurrentMaterialIndex)
                {
                    vk.CmdPushConstants(commandBuffer, pipelineLayout, MaterialPushConstantShaderStages, offset: 4, size: 4, pValues: &materialIndex);
                    renderingContext.CurrentMaterialIndex = materialIndex;
                    pushConstantsChangesCount++;
                }
            }
        }
        else
        {
            if (descriptorSets == null)
            {
                if (!renderingItemFlags.HasFlag(RenderingItemFlags.NoMaterialDescriptorSets))
                {
                    //Log.Warn?.Write(LogArea, Id, $"Skipping rendering RenderingItem '{renderingItem.ToString()}' because its MaterialDescriptorSets is null. If this is intended, then set the NoMaterialDescriptorSets to RenderingItem.Flags.");
                    return;
                }
            }
            else if (swapChainImageIndex >= descriptorSets.Length)
            {
                //Log.Warn?.Write(LogArea, Id, $"Skipping rendering RenderingItem '{renderingItem.ToString()}' because swapChainImageIndex ({swapChainImageIndex}) >= renderingItem.MaterialDescriptorSets.Length ({descriptorSets.Length}). Make sure that the number of MaterialDescriptorSets is the same as created SwapChain images.");
                return;
            }
        }

        
        var vertexCount = renderingItem.VertexCount;
        var indexCount = renderingItem.IndexCount;
        var instanceCount = renderingItem.InstanceCount;

        //if (instanceCount > 1 && !this.Flags.HasFlag(EffectTechniqueFlags.InstancingSupported))
        //    Log.Warn?.Write(LogArea, Id, $"RenderingItem '{renderingItem.ToString()}' has InstanceCount greater than 1 ({instanceCount}) but its EffectTechnique ({this.Name}) does not have InstancingSupported flag set.");


        // Draw
        if (renderingItem.CustomRenderAction != null)
            renderingItem.CustomRenderAction(renderingContext, commandBuffer, renderingItem);
        else if (!indexBuffer.IsNull())
            vk.CmdDrawIndexed(commandBuffer, indexCount, instanceCount, renderingItem.FirstIndex, renderingItem.VertexOffset, renderingItem.FirstInstance);
        else
            vk.CmdDraw(commandBuffer, vertexCount, instanceCount, (uint)renderingItem.VertexOffset, renderingItem.FirstInstance);


        var renderingStatistics = renderingContext.RenderingStatistics;

        if (renderingStatistics != null)
        {
            renderingContext.DrawCallsCount++;
            renderingContext.DrawnIndicesCount         += (int)(indexCount * instanceCount);
            renderingContext.DrawnVerticesCount        += (int)(vertexCount * instanceCount);
            renderingContext.VertexBuffersChangesCount += vertexBufferChangesCount;
            renderingContext.IndexBuffersChangesCount  += indexBufferChangesCount;
            renderingContext.DescriptorSetChangesCount += descriptorSetChangesCount;
            renderingContext.PushConstantsChangesCount += pushConstantsChangesCount;
        }
    }

    protected override void Dispose(bool disposing)
    {
        ResetPipeline();

        // Because PipelineLayout were not created here, we do not dispose it here (also, PipelineLayout can be shared by multiple Pipelines - see StandardEffect)
        PipelineLayout = PipelineLayout.Null;
    }
}