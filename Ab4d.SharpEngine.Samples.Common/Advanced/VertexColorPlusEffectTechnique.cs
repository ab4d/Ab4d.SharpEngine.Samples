using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Effects;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.Vulkan;
using System.Runtime.CompilerServices;
using Ab4d.SharpEngine.Common;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

sealed class VertexColorPlusEffectTechnique : EffectTechnique
{
    public required DescriptorSetLayout MaterialsDescriptorSetLayout;
    public required PipelineShaderStageCreateInfo[] ShaderStageCreateInfo;
    static readonly PipelineDepthStencilStateCreateInfo _depthStencilStateCreateInfo = CommonStatesManager.DepthReadWrite;
    public required PipelineRasterizationStateCreateInfo RasterizationStateCreateInfo;
    static readonly PipelineTessellationStateCreateInfo _tessellationStateCreateInfo = new() { SType = StructureType.PipelineTessellationStateCreateInfo };
    static readonly PipelineInputAssemblyStateCreateInfo _inputAssemblyStateCreateInfo = CommonStatesManager.TriangleListInputAssemblyState;
    static readonly PipelineColorBlendAttachmentState _colorBlendAttachmentState = CommonStatesManager.PremultipliedAlphaBlendAttachmentState;

    Pipeline _pipeline;
    public Pipeline Pipeline => _pipeline;

    PipelineLayout _pipelineLayout;
    nint _vertexInputStateCreateInfoPtr;

    const int _sceneDescriptorSetBindingIndex = 0;
    const int _lightsDescriptorSetBindingIndex = 1;
    const int _matricesDescriptorSetBindingIndex = 2;
    const int _materialsDescriptorSetBindingIndex = 3;

    public VertexColorPlusEffectTechnique(Scene scene, string? name)
        : base(scene, name)
    {
    }

    protected override void OnInitializeDeviceResources(VulkanDevice gpuDevice)
    {
        _pipelineLayout = gpuDevice.CreatePipelineLayout(
            [
                Scene.SceneDescriptorSetLayout,
                Scene.AllLightsDescriptorSetLayout,
                Scene.AllWorldMatricesDescriptorSetLayout,
                MaterialsDescriptorSetLayout
            ],
            [
                new() { StageFlags = ShaderStageFlags.Vertex, Offset = 0, Size = 4 },
                new() { StageFlags = ShaderStageFlags.Fragment, Offset = 4, Size = 4 }
            ]);

        _vertexInputStateCreateInfoPtr = gpuDevice.VertexBufferDescriptionsManager
            .GetPositionNormalTexture0Color1VertexBufferDescription()
            .PipelineVertexInputStateCreateInfoPtr;
    }

    public unsafe void CreatePipeline(
        float width, float height,
        RenderPass renderPass,
        SampleCountFlags multisampleCount,
        Pipeline parentPipeline,
        string? name = null)
    {
        var gpuDevice = Scene.GpuDevice;
        if (gpuDevice is null)
            return;

        if (!IsInitialized)
            InitializeDeviceResources();

        PipelineDynamicStateCreateInfo dynamicStateCreateInfo;
        var dynamicStates = VulkanDevice.GlobalDynamicStates;
        if (dynamicStates is null || dynamicStates.Length == 0)
        {
            dynamicStateCreateInfo = new()
            {
                SType = StructureType.PipelineDynamicStateCreateInfo,
                Flags = 0,
                DynamicStateCount = 0,
                PDynamicStates = null
            };
        }
        else
        {
            fixed (DynamicState* dynamicStatesPtr = dynamicStates)
            {
                dynamicStateCreateInfo = new()
                {
                    SType = StructureType.PipelineDynamicStateCreateInfo,
                    Flags = 0,
                    DynamicStateCount = (uint)dynamicStates.Length,
                    PDynamicStates = dynamicStatesPtr
                };
            }
        }
        var depthStencilStateCreateInfoCreateInfo = _depthStencilStateCreateInfo;
        var rasterizationStateCreateInfo = RasterizationStateCreateInfo;
        var tessellationStateCreateInfo = _tessellationStateCreateInfo;
        Viewport viewport;
        Rect2D scissor;
        if (height < 0)
        {
            viewport = new Viewport(0, -height, width, height, 0, 1);
            scissor = new Rect2D(new Offset2D(0, 0), new Extent2D((uint)width, (uint)-height));
        }
        else
        {
            viewport = new Viewport(0, 0, width, height, 0, 1);
            scissor = new Rect2D(new Offset2D(0, 0), new Extent2D((uint)width, (uint)height));
        }
        var viewportStateCreateInfo = new PipelineViewportStateCreateInfo
        {
            SType = StructureType.PipelineViewportStateCreateInfo,
            ViewportCount = 1,
            PViewports = &viewport,
            ScissorCount = 1,
            PScissors = &scissor,
        };
        var inputAssemblyStateCreateInfo = _inputAssemblyStateCreateInfo;
        var colorBlendAttachmentState = _colorBlendAttachmentState;
        var colorBlendStateCreateInfo = new PipelineColorBlendStateCreateInfo
        {
            SType = StructureType.PipelineColorBlendStateCreateInfo,
            LogicOpEnable = false,
            LogicOp = LogicOp.Copy,
            AttachmentCount = 1,
            PAttachments = &colorBlendAttachmentState,
        };
        var multisampleStateCreateInfo = new PipelineMultisampleStateCreateInfo
        {
            SType = StructureType.PipelineMultisampleStateCreateInfo,
            RasterizationSamples = multisampleCount,
        };

        fixed (PipelineShaderStageCreateInfo* shaderStageCreateInfoPtr = ShaderStageCreateInfo)
        {
            var pipelineCreateInfo = new GraphicsPipelineCreateInfo()
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                Layout = _pipelineLayout,
                StageCount = (uint)ShaderStageCreateInfo.Length,
                PStages = shaderStageCreateInfoPtr,
                PDynamicState = &dynamicStateCreateInfo,
                PDepthStencilState = &depthStencilStateCreateInfoCreateInfo,
                PRasterizationState = &rasterizationStateCreateInfo,
                PTessellationState = &tessellationStateCreateInfo,
                PViewportState = &viewportStateCreateInfo,
                PInputAssemblyState = &inputAssemblyStateCreateInfo,
                PColorBlendState = &colorBlendStateCreateInfo,
                PMultisampleState = &multisampleStateCreateInfo,
                PVertexInputState = (PipelineVertexInputStateCreateInfo*)_vertexInputStateCreateInfoPtr,
                RenderPass = renderPass,
                BasePipelineHandle = parentPipeline,
                BasePipelineIndex = -1,
                Subpass = 0,
                Flags = parentPipeline.IsNull() ? PipelineCreateFlags.AllowDerivatives : PipelineCreateFlags.Derivative,
            };

            Pipeline pipeline;
            gpuDevice.Vk.CreateGraphicsPipelines(gpuDevice.Device, gpuDevice.GetPipelineCache(), 1, &pipelineCreateInfo, null, &pipeline);
            if (name is not null)
                pipeline.SetName(gpuDevice, name);
            _pipeline = pipeline;
        }
    }

    protected override void Dispose(bool disposing)
    {
        var gpuDevice = Scene.GpuDevice;
        if (gpuDevice is not null)
        {
            if (_pipelineLayout.IsNull() is false)
            {
                gpuDevice.DisposeVulkanResourceOnMainThreadAfterFrameRendered(_pipelineLayout.Handle, typeof(PipelineLayout));
                _pipelineLayout = PipelineLayout.Null;
            }
        }

        ResetPipeline();
    }

    public override void ResetPipeline()
    {
        var gpuDevice = Scene.GpuDevice;
        if (gpuDevice is not null && _pipeline.IsNull() is false)
        {
            gpuDevice.DisposeVulkanResourceOnMainThreadAfterFrameRendered(_pipeline.Handle, typeof(Pipeline));
            _pipeline = Pipeline.Null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override unsafe void Render(CommandBuffer commandBuffer, RenderingItem renderingItem, RenderingContext renderingContext)
    {
        var descriptorSetChangesCount = 0;
        var pushConstantsChangesCount = 0;
        var vertexBufferChangesCount = 0;

        var i = 0;

        var vk = renderingContext.GpuDevice.Vk;

        // Bind the pipeline

        if (renderingContext.CurrentPipeline != _pipeline)
        {
            vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, _pipeline);
            renderingContext.CurrentPipeline = _pipeline;
            renderingContext.PipelineChangesCount++;
        }

        // Bind descriptor sets and set push constants

        var currentBoundDescriptorSets = renderingContext.CurrentBoundDescriptorSets;
        var swapChainImageIndex = renderingContext.CurrentSwapChainImageIndex;

        // Scene
        var descriptorSet = renderingContext.SceneDescriptorSet;
        if (descriptorSet != currentBoundDescriptorSets[_sceneDescriptorSetBindingIndex])
        {
            vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, _pipelineLayout, _sceneDescriptorSetBindingIndex, 1, &descriptorSet, 0, null);
            currentBoundDescriptorSets[_sceneDescriptorSetBindingIndex] = descriptorSet;
            descriptorSetChangesCount = 1;
        }

        // Lights
        descriptorSet = renderingContext.AllLightsDescriptorSet;
        if (descriptorSet != currentBoundDescriptorSets[_lightsDescriptorSetBindingIndex])
        {
            vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, _pipelineLayout, (uint)_lightsDescriptorSetBindingIndex, 1, &descriptorSet, 0, null);
            currentBoundDescriptorSets[_lightsDescriptorSetBindingIndex] = descriptorSet;
            descriptorSetChangesCount++;
        }

        // Matrices
        var descriptorSets = renderingItem.MatricesDescriptorSets;
        if (descriptorSets is not null &&
            swapChainImageIndex < descriptorSets.Length)
        {
            descriptorSet = descriptorSets[swapChainImageIndex];
            if (descriptorSet != currentBoundDescriptorSets[_matricesDescriptorSetBindingIndex])
            {
                vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, _pipelineLayout, _matricesDescriptorSetBindingIndex, 1, &descriptorSet, 0, null);
                currentBoundDescriptorSets[_matricesDescriptorSetBindingIndex] = descriptorSet;
                descriptorSetChangesCount++;
            }

            // Matrix index push const
            i = renderingItem.MatrixIndex;
            if (i != renderingContext.CurrentMatrixIndex &&
                i >= 0)
            {
                vk.CmdPushConstants(commandBuffer, _pipelineLayout, ShaderStageFlags.Vertex, 0, 4, &i);
                renderingContext.CurrentMatrixIndex = i;
                pushConstantsChangesCount = 1;
            }
        }

        // Materials
        descriptorSets = renderingItem.MaterialDescriptorSets;
        if (descriptorSets is not null &&
            swapChainImageIndex < descriptorSets.Length)
        {
            descriptorSet = descriptorSets[swapChainImageIndex];
            if (descriptorSet != currentBoundDescriptorSets[_materialsDescriptorSetBindingIndex])
            {
                vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, _pipelineLayout, _materialsDescriptorSetBindingIndex, 1, &descriptorSet, 0, null);
                currentBoundDescriptorSets[_materialsDescriptorSetBindingIndex] = descriptorSet;
                descriptorSetChangesCount++;
            }

            // Material index push const
            i = renderingItem.MaterialIndex;
            if (i >= 0)
            {
                if (renderingItem.Flags.HasFlag(RenderingItemFlags.IsBackFaceMaterial))
                    i = -i;

                if (i != renderingContext.CurrentMaterialIndex)
                {
                    vk.CmdPushConstants(commandBuffer, _pipelineLayout, ShaderStageFlags.Fragment, 4, 4, &i);
                    renderingContext.CurrentMaterialIndex = i;
                    pushConstantsChangesCount++;
                }
            }
        }

        // Bind vertex and index buffers

        var buffer = renderingItem.VertexBuffer;
        var additionalVertexBuffers = renderingItem.AdditionalVertexBuffers;
        if (buffer != renderingContext.CurrentVertexBuffer)
        {
            var offset = 0ul;
            vk.CmdBindVertexBuffers(commandBuffer, 0, 1, &buffer, &offset);
            renderingContext.CurrentVertexBuffer = buffer;
            vertexBufferChangesCount = 1;
        }
        if (additionalVertexBuffers is not null)
        {
            var length = additionalVertexBuffers.Length;
            var offsets = stackalloc ulong[length];
            fixed (Ab4d.Vulkan.Buffer* stackallocAdditionalVertexBuffers = additionalVertexBuffers)
                vk.CmdBindVertexBuffers(commandBuffer, 1, (uint)length, stackallocAdditionalVertexBuffers, offsets);
            vertexBufferChangesCount += length;
        }

        buffer = renderingItem.IndexBuffer;
        if (buffer != renderingContext.CurrentIndexBuffer)
        {
            vk.CmdBindIndexBuffer(commandBuffer, buffer, 0, renderingItem.IndexBufferType);
            renderingContext.CurrentIndexBuffer = buffer;
            renderingContext.IndexBuffersChangesCount++;
        }

        // Draw

        var vertexCount = renderingItem.VertexCount;
        var indexCount = renderingItem.IndexCount;
        var instanceCount = renderingItem.InstanceCount;

        if (renderingItem.CustomRenderAction != null)
            renderingItem.CustomRenderAction(renderingContext, commandBuffer, renderingItem);
        else
            vk.CmdDrawIndexed(commandBuffer, indexCount, instanceCount, renderingItem.FirstIndex, renderingItem.VertexOffset, renderingItem.FirstInstance);

        // Increment render stats

        if (renderingContext.RenderingStatistics is not null)
        {
            renderingContext.DrawCallsCount++;
            renderingContext.DrawnIndicesCount += (int)(indexCount * instanceCount);
            renderingContext.DrawnVerticesCount += (int)(vertexCount * instanceCount);
            renderingContext.VertexBuffersChangesCount += vertexBufferChangesCount;
            renderingContext.PushConstantsChangesCount += pushConstantsChangesCount;
            renderingContext.DescriptorSetChangesCount += descriptorSetChangesCount;
        }
    }
}
