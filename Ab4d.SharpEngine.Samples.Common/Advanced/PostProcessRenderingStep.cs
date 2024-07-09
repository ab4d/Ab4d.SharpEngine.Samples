using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.RenderingSteps;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.Vulkan;
using System.Runtime.CompilerServices;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

// references:
// https://github.com/SaschaWillems/Vulkan/blob/master/examples/bloom/bloom.cpp
// https://vulkan-tutorial.com
// FogEffectTechnique/FogEffect

sealed class PostProcessRenderingStep : RenderingStep
{
    static readonly string _logArea = typeof(PostProcessRenderingStep).FullName!;

    static readonly uint _samplerDescriptorSetIndex = 0;

    //static readonly uint _matrixIndexPushConstantSize = sizeof(int);

    //static readonly SamplerCreateInfo _samplerCreateInfo = new()
    //{
    //    MagFilter = Filter.Linear,
    //    MinFilter = Filter.Linear,
    //    MipmapMode = SamplerMipmapMode.Linear,
    //    AddressModeU = SamplerAddressMode.ClampToEdge,
    //    AddressModeV = SamplerAddressMode.ClampToEdge,
    //    AddressModeW = SamplerAddressMode.ClampToEdge,
    //    MipLodBias = 0,
    //    MaxAnisotropy = 1,
    //    MinLod = 0,
    //    MaxLod = 1,
    //    BorderColor = BorderColor.FloatOpaqueWhite,
    //};
    //readonly Sampler _sampler;

    readonly DescriptorSet[] _samplerDescriptorSets;

    readonly PipelineLayout _pipelineLayout;
    readonly PipelineShaderStageCreateInfo[] _shaderStageCreateInfo;
    static readonly PipelineDepthStencilStateCreateInfo _depthStencilStateCreateInfo = CommonStatesManager.DepthReadWrite;
    static readonly PipelineRasterizationStateCreateInfo _rasterizationStateCreateInfo = CommonStatesManager.CullCounterClockwise;
    static readonly PipelineTessellationStateCreateInfo _tessellationStateCreateInfo = new()
    {
        SType = StructureType.PipelineTessellationStateCreateInfo,
        Flags = 0,
        PatchControlPoints = 0,
    };
    static readonly PipelineInputAssemblyStateCreateInfo _inputAssemblyStateCreateInfo = CommonStatesManager.TriangleListInputAssemblyState;
    static readonly PipelineColorBlendAttachmentState _colorBlendAttachmentState = CommonStatesManager.OpaqueAttachmentState;

    readonly VertexBufferDescription _vertexBufferDescription;

    Pipeline _pipeline;

    public unsafe PostProcessRenderingStep(
        string vertexShaderName, string fragmentShaderName,
        SceneView sceneView, string? name = null, string? description = null)
        : base(sceneView, name, description)
    {
        Log.IsLoggingToDebugOutput = true;
        Log.AddLogListener((levels, msg) =>
        {
            if (levels.HasFlag(SharpEngine.Common.LogLevels.Error) ||
            levels.HasFlag(SharpEngine.Common.LogLevels.Fatal))
            {

            }
        });

        var gpuDevice = sceneView.GpuDevice!;
        var scene = sceneView.Scene;

        // descriptor sets

        var descriptorSetLayouts = new DescriptorSetLayout[] {
            gpuDevice.CreateDescriptorSetLayout(DescriptorType.CombinedImageSampler, ShaderStageFlags.Fragment)
        };

        var swapChainImagesCount = sceneView.SwapChainImagesCount;
        var samplerDescriptorPool = gpuDevice.CreateDescriptorPool(DescriptorType.CombinedImageSampler, swapChainImagesCount);
        _samplerDescriptorSets = gpuDevice.CreateDescriptorSets(descriptorSetLayouts[0], samplerDescriptorPool, swapChainImagesCount);

        //(_samplerDescriptorSetFactory, _samplerDescriptorSetFactoryDisposeToken) = VulkanDescriptorSetFactory.Create(
        //    gpuDevice,
        //    DescriptorType.CombinedImageSampler,
        //    descriptorSetLayouts[0],
        //    Math.Max(16, 8 * scene.SwapChainImagesCount));
        //(_samplerMemoryBlockPool, _samplerMemoryBlockPoolDisposeToken) = GpuDynamicMemoryBlockPool<DescriptorImageInfo>.Create(
        //    scene,
        //    sizeof(DescriptorImageInfo),
        //    scene.SwapChainImagesCount);
        //_samplerMemoryBlockPool.CreateDescriptorSetsAction = gpuBuffers => _samplerDescriptorSetFactory?.CreateDescriptorSets(gpuBuffers);
        //(_samplerMemoryBlockIndex, _samplerMemoryIndex) = _samplerMemoryBlockPool.GetNextFreeIndex();

        //var samplerCreateInfo = _samplerCreateInfo;
        //Sampler sampler;
        //gpuDevice.Vk.CreateSampler(gpuDevice.Device, &samplerCreateInfo, null, &sampler);
        //_sampler = sampler;

        // pipeline create info

        _pipelineLayout = gpuDevice.CreatePipelineLayout(descriptorSetLayouts, 0, 0);
        _shaderStageCreateInfo = [
            gpuDevice.ShadersManager.GetOrCreatePipelineShaderStage(vertexShaderName, ShaderStageFlags.Vertex, "main"),
            gpuDevice.ShadersManager.GetOrCreatePipelineShaderStage(fragmentShaderName, ShaderStageFlags.Fragment, "main"),
        ];

        // vertex buffer description

        _vertexBufferDescription = gpuDevice.VertexBufferDescriptionsManager.GetPositionNormalTextureVertexBufferDescription();
    }

    unsafe Pipeline _createPipeline(
        float width, float height,
        SampleCountFlags multisampleCount,
        RenderPass renderPass,
        Pipeline parentPipeline,
        PipelineCreateFlags flags,
        string? name)
    {
        // Vulkan by default uses right coordinate system with y axis down (?!). To enable y axis up, we need to define negative height.
        var viewport = height < 0 ?
            new Viewport(0, -height, width, height, 0, 1) :
            new Viewport(0, 0, width, height, 0, 1);
        var scissor = new Rect2D(new Offset2D(0, 0), new Extent2D((uint)width, (uint)Math.Abs(height)));
        var viewportCreateInfo = new PipelineViewportStateCreateInfo
        {
            SType = StructureType.PipelineViewportStateCreateInfo,
            ViewportCount = 1,
            PViewports = &viewport,
            ScissorCount = 1,
            PScissors = &scissor,
        };

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
            fixed (DynamicState* stackallocDynamicStates = dynamicStates)
            {
                dynamicStateCreateInfo = new()
                {
                    SType = StructureType.PipelineDynamicStateCreateInfo,
                    Flags = 0,
                    DynamicStateCount = (uint)dynamicStates.Length,
                    PDynamicStates = stackallocDynamicStates
                };
            }
        }

        var depthStencilStateCreateInfoCreateInfo = _depthStencilStateCreateInfo;
        var rasterizationStateCreateInfo = _rasterizationStateCreateInfo;
        var tessellationStateCreateInfo = _tessellationStateCreateInfo;
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
        var vkPipelineMultisampleStateCreateInfo = new PipelineMultisampleStateCreateInfo
        {
            SType = StructureType.PipelineMultisampleStateCreateInfo,
            RasterizationSamples = multisampleCount
        };

        GraphicsPipelineCreateInfo pipelineCreateInfo;
        fixed (PipelineShaderStageCreateInfo* shaderStageCreateInfo = _shaderStageCreateInfo)
        {
            pipelineCreateInfo = new()
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                Layout = _pipelineLayout,
                PViewportState = &viewportCreateInfo,
                StageCount = (uint)_shaderStageCreateInfo.Length,
                PStages = shaderStageCreateInfo,
                PDynamicState = &dynamicStateCreateInfo,
                PDepthStencilState = &depthStencilStateCreateInfoCreateInfo,
                PRasterizationState = &rasterizationStateCreateInfo,
                PTessellationState = &tessellationStateCreateInfo,
                PInputAssemblyState = &inputAssemblyStateCreateInfo,
                PColorBlendState = &colorBlendStateCreateInfo,
                PMultisampleState = &vkPipelineMultisampleStateCreateInfo,
                PVertexInputState = (PipelineVertexInputStateCreateInfo*)_vertexBufferDescription.PipelineVertexInputStateCreateInfoPtr,
                RenderPass = renderPass,
                BasePipelineHandle = parentPipeline,
                BasePipelineIndex = -1,
                Subpass = 0,
                Flags = flags,
            };
        }

        Pipeline pipeline;
        var gpuDevice = SceneView.GpuDevice!;
        gpuDevice.Vk.CreateGraphicsPipelines(gpuDevice.Device, gpuDevice.GetPipelineCache(), 1, &pipelineCreateInfo, null, &pipeline);
        if (name is not null)
            pipeline.SetName(gpuDevice, name);

        _pipeline = pipeline;
        return pipeline;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected override unsafe bool OnRun(RenderingContext renderingContext)
    {
        var descriptorSetChangesCount = 0;

        var gpuDevice = renderingContext.GpuDevice;
        var vk = gpuDevice.Vk;
        var cmdBuf = gpuDevice.BeginGraphicsCommands();

        // ensure the pipeline exists

        if (_pipeline.IsNull())
        {
            _pipeline = _createPipeline(
                renderingContext.Width, renderingContext.Height,
                renderingContext.MultisampleCount,
                renderingContext.RenderPass,
                Pipeline.Null,
                PipelineCreateFlags.AllowDerivatives,
                null);
        }

        // update descriptor sets

        var boundDescriptorSets = renderingContext.CurrentBoundDescriptorSets;
        var swapChainImageIndex = renderingContext.CurrentSwapChainImageIndex;

        var samplerDescriptorSet = _samplerDescriptorSets[swapChainImageIndex];

        // note to Andrej:
        // instead of more images here
        // it'd be great if we could just set ImageUsage on the swap chain to allow the sampler to see it
        //var imageCreateInfo = new ImageCreateInfo
        //{
        //    ArrayLayers = 1,
        //    Extent = new((int)renderingContext.Width, (int)renderingContext.Height, 1),
        //    Format = renderingContext.SwapChain.SurfaceFormat.Format,
        //    ImageType = ImageType.ImageType2D,
        //    MipLevels = 1,
        //    Tiling = ImageTiling.Optimal,
        //    InitialLayout = ImageLayout.Undefined,
        //    Usage = ImageUsageFlags.TransferDst | ImageUsageFlags.Sampled,
        //    Samples = SampleCountFlags.SampleCount1,
        //    SharingMode = SharingMode.Exclusive,
        //    SType = StructureType.ImageCreateInfo,
        //};
        ////gpuDevice.CreateSwapChainImages()
        //Image image;
        //vk.CreateImage(gpuDevice.Device, &imageCreateInfo, null, &image);
        //MemoryRequirements imageMemoryRequirements;
        //vk.GetImageMemoryRequirements(gpuDevice.Device, image, &imageMemoryRequirements);
        //var imageMemoryAllocateInfo = new MemoryAllocateInfo
        //{
        //    AllocationSize = imageMemoryRequirements.Size,
        //    MemoryTypeIndex = (uint)gpuDevice.PhysicalDeviceDetails.FindMemoryType(imageMemoryRequirements.MemoryTypeBits, MemoryPropertyFlags.DeviceLocal),
        //    SType = StructureType.MemoryAllocateInfo,
        //};
        //DeviceMemory imageMemory;
        //vk.AllocateMemory(gpuDevice.Device, &imageMemoryAllocateInfo, null, &imageMemory);
        //vk.BindImageMemory(gpuDevice.Device, image, imageMemory, 0);
        //renderingContext.SwapChain.CopyToImage(swapChainImageIndex, image);

        var imageViewSubresourceRange = new ImageSubresourceRange
        {
            AspectMask = ImageAspectFlags.Color,
            BaseMipLevel = 0,
            LevelCount = 1,
            BaseArrayLayer = 0,
            LayerCount = 1,
        };
        var imageViewCreateInfo = new ImageViewCreateInfo
        {
            Components = new ComponentMapping(ComponentSwizzle.R, ComponentSwizzle.G, ComponentSwizzle.B, ComponentSwizzle.A),
            Format = renderingContext.SwapChain.SurfaceFormat.Format,
            // not sure where to get the image from
            Image = renderingContext.StagingGpuImage.Image,
            //Image = _saveOutputRenderingStep.Images[swapChainImageIndex],
            //Image = image,
            //Image = renderingContext.CurrentSwapChainImage,
            SType = StructureType.ImageViewCreateInfo,
            SubresourceRange = imageViewSubresourceRange,
            ViewType = ImageViewType.ImageViewType2D,
        };
        ImageView imageView;
        vk.CreateImageView(gpuDevice.Device, &imageViewCreateInfo, null, &imageView);
        var descriptorImageInfo = new DescriptorImageInfo
        {
            ImageLayout = ImageLayout.ReadOnlyOptimal,
            ImageView = imageView,
            //ImageView = renderingContext.CurrentSwapChainImageView,
            Sampler = gpuDevice.SamplerFactory.MirrorSampler.Sampler,
        };
        var writeDescriptorSet = new WriteDescriptorSet
        {
            DescriptorType = DescriptorType.CombinedImageSampler,
            DstArrayElement = 0,
            DescriptorCount = 1,
            DstBinding = _samplerDescriptorSetIndex,
            DstSet = samplerDescriptorSet,
            PImageInfo = &descriptorImageInfo,
            SType = StructureType.WriteDescriptorSet,
        };
        vk.UpdateDescriptorSets(gpuDevice.Device, 1, &writeDescriptorSet, 0, null);

        // bind descriptor sets

        if (samplerDescriptorSet != boundDescriptorSets[_samplerDescriptorSetIndex])
        {
            vk.CmdBindDescriptorSets(cmdBuf, PipelineBindPoint.Graphics, _pipelineLayout, _samplerDescriptorSetIndex, 1, &samplerDescriptorSet, 0, null);
            boundDescriptorSets[_samplerDescriptorSetIndex] = samplerDescriptorSet;
            descriptorSetChangesCount++;
        }

        // bind the pipeline

        vk.CmdBindPipeline(cmdBuf, PipelineBindPoint.Graphics, _pipeline);

        // ende

        gpuDevice.EndGraphicsCommands();

        return true;
    }
}
