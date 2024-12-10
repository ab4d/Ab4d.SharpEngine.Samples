using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.RenderingSteps;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.Vulkan;

namespace Ab4d.SharpEngine.Samples.ImGui;

public class ImGuiRenderingStep : RenderingStep
{
    // Data for font texture
    private readonly RawImageData _fontTextureData;
    private const IntPtr FontTextureId = 1; // ID we want ImGui to use for the font texture

    private GpuImage? _fontTextureImage;
    private DescriptorSetLayout _fontTextureDescriptorSetLayout;
    private DescriptorPool _fontTextureDescriptorPool;
    private DescriptorSet[]? _fontTextureDescriptorSets;

    private Vector2 _lastDisplaySize;

    private nint _imGuiCtx;
    private ImGuiNET.ImGuiIOPtr _imGuiIo;

    private GpuBuffer[]? _matricesBuffers;
    private DescriptorSetLayout _matricesDescriptorSetLayout;
    private DescriptorPool _matricesDescriptorPool;
    private DescriptorSet[]? _matricesDescriptorSets;

    private Matrix4x4 _mvpMatrix;
    private bool[]? _isMatrixBufferDirty;

    // Vertex and index data
    private VertexBufferDescription? _vertexBufferDescription;
    private DisposeToken _vertexBufferDescriptionDisposeToken;

    private ushort[]? _indices;
    private byte[]? _vertices; // Raw vertex data (mixture of float and uint32 values)

    private GpuBuffer? _indexBuffer;
    private GpuBuffer? _vertexBuffer;

    // Pipeline
    private PipelineLayout _pipelineLayout;
    private Pipeline _pipeline;

    /// <inheritdoc />
    public ImGuiRenderingStep(SceneView sceneView, nint imGuiCtx, string? name, string? description = null)
        : base(sceneView, name, description)
    {
        _imGuiCtx = imGuiCtx;

        ImGuiNET.ImGui.SetCurrentContext(_imGuiCtx); // Ensure context is active
        _imGuiIo = ImGuiNET.ImGui.GetIO();

        // Get data for font texture
        _imGuiIo.Fonts.GetTexDataAsRGBA32(out IntPtr textureDataPtr, out var textureWidth, out var textureHeight, out var textureBytesPerPixel);
        _imGuiIo.Fonts.SetTexID(FontTextureId);

        var textureData = new byte[textureWidth * textureHeight * textureBytesPerPixel];
        System.Runtime.InteropServices.Marshal.Copy(textureDataPtr, textureData, 0, textureData.Length);

        _fontTextureData = new RawImageData(textureWidth, textureHeight, textureWidth * textureBytesPerPixel, Format.R8G8B8A8Unorm, textureData, checkTransparency: false)
        {
            HasTransparentPixels = true,
            IsPreMultipliedAlpha = false,
        };
    }

    /// <inheritdoc />
    protected override unsafe bool OnRun(RenderingContext renderingContext)
    {
        var swapChainImageIndex = renderingContext.CurrentSwapChainImageIndex;

        if (_matricesDescriptorSets != null && _matricesDescriptorSets.Length != renderingContext.SwapChainImagesCount)
            DisposeMatricesResources(renderingContext.GpuDevice);

        if (_matricesDescriptorSets == null)
            InitializeMatricesResources(renderingContext);

        if (_fontTextureDescriptorSets == null)
            InitializeFontTextureResources(renderingContext);

        // Create pipeline, if necessary
        if (_pipeline.IsNull())
            CreatePipeline(renderingContext);

        // Retrieve Vulkan command buffer
        if (!renderingContext.IsCurrentCommandBufferDirty)
        {
            return true; // Skip filling command buffer again as it is still valid; return true to continue rendering
        }

        var commandBuffer = renderingContext.CurrentCommandBuffer;
        var vk = renderingContext.GpuDevice.Vk;

        // Fetch ImGui data
        ImGuiNET.ImGui.SetCurrentContext(_imGuiCtx);
        var drawData = ImGuiNET.ImGui.GetDrawData();

        // Nothing to render. This might happen on first draw with UI that contains only windows that were not explicitly
        // sized; in such cases, ImGui does not show anything on the first frame.
        if (drawData.CmdLists.Size == 0)
        {
            return true;
        }

        // Check if display size changed
        // Create/update orthographic projection matrix. The matrix can be (re)computed on viewport changes.
        if (_lastDisplaySize != _imGuiIo.DisplaySize)
        {
            _mvpMatrix = Matrix4x4.CreateOrthographicOffCenter(
                0f,
                SceneView.Width,
                SceneView.Height,
                0.0f,
                -1.0f,
                1.0f);

            // Mark all GpuBuffers that store MVP (_matricesBuffers) as dirty - this will update them by calling WriteToBuffer (see below)
            MarkMatrixBufferDirty();

            _lastDisplaySize = _imGuiIo.DisplaySize;
        }

        drawData.ScaleClipRects(_imGuiIo.DisplayFramebufferScale * renderingContext.SceneView.DpiScaleX * renderingContext.SupersamplingFactor);

        // If GpuBuffer for the current frame is marked as dirty, then update it
        if (_isMatrixBufferDirty != null && _isMatrixBufferDirty[swapChainImageIndex] && _matricesBuffers != null)
        {
            _matricesBuffers[swapChainImageIndex].WriteToBuffer(ref _mvpMatrix); // write from CPU's _mvpMatrix to the GpuBuffer
            _isMatrixBufferDirty[swapChainImageIndex] = false;
        }

        // Collect all index and vertex data
        const int bytesPerVertex = 20; // four 32-bit floats + one 32-bit integer
        var indicesChanged = _indices == null || _indices.Length != drawData.TotalIdxCount;
        var verticesChanged = _vertices == null || _vertices.Length != drawData.TotalVtxCount * bytesPerVertex;

        if (indicesChanged)
            _indices = new ushort[drawData.TotalIdxCount];

        if (verticesChanged)
            _vertices = new byte[drawData.TotalVtxCount * bytesPerVertex];

        var verticesOffset = 0;
        var indicesOffset = 0;
        for (var i = 0; i < drawData.CmdListsCount; i++)
        {
            var cmdList = drawData.CmdLists[i];

            // If vertex/index buffer has not been (re-)allocated, check if the chunk of data that corresponds to the
            // given command list has changed or not, and only copy it if it has. After first change, do not keep
            // checking anymore, and assume that all subsequent data has changed.

            // Copy indices (16-bit unsigned shorts).
            var spanNewIdxData = new ReadOnlySpan<ushort>((void*)cmdList.IdxBuffer.Data, cmdList.IdxBuffer.Size);
            var spanOldIdxData = new Span<ushort>(_indices, indicesOffset, cmdList.IdxBuffer.Size);

            if (!indicesChanged)
                indicesChanged |= !spanOldIdxData.SequenceEqual(spanNewIdxData);

            if (indicesChanged)
                spanNewIdxData.CopyTo(spanOldIdxData);

            indicesOffset += cmdList.IdxBuffer.Size;

            // Copy vertex data, without trying to re-interpret it (i.e., copy raw bytes). Our vertex buffer
            // description is set to match the format used by ImGui (2 floats for position, 2 floats for texture,
            // 1 32-bit integer / 4 bytes for RGBA color).
            var spanNewVtxData = new ReadOnlySpan<byte>((void*)cmdList.VtxBuffer.Data, cmdList.VtxBuffer.Size * bytesPerVertex);
            var spanOldVxtData = new Span<byte>(_vertices, verticesOffset * bytesPerVertex, cmdList.VtxBuffer.Size * bytesPerVertex);

            if (!verticesChanged)
                verticesChanged |= !spanOldVxtData.SequenceEqual(spanNewVtxData);

            if (verticesChanged)
                spanNewVtxData.CopyTo(spanOldVxtData);

            verticesOffset += cmdList.VtxBuffer.Size;
        }

        if (indicesChanged)
        {
            // Dispose old index buffer. Note that the actual Vulkan buffer will be kept alive until the current "in-flight" frames are fully rendered.
            _indexBuffer?.Dispose();

            // Create a new index buffer.
            // isDeviceLocal is false because this buffer is frequently changed so it is better that it stays on the CPU's side.
            _indexBuffer = renderingContext.GpuDevice.CreateBuffer(_indices!, BufferUsageFlags.IndexBuffer, isDeviceLocal: false, name: $"ImGuiIndexBuffer");
        }

        if (verticesChanged)
        {
            // Dispose old vertex buffer. Note that the actual Vulkan buffer will be kept alive until the current "in-flight" frames are fully rendered.
            _vertexBuffer?.Dispose();

            // Create a new index buffer.
            // isDeviceLocal is false because this buffer is frequently changed so it is better that it stays on the CPU's side.
            _vertexBuffer = renderingContext.GpuDevice.CreateBuffer(_vertices!, BufferUsageFlags.VertexBuffer, isDeviceLocal: false, name: $"ImGuiVertexBuffer");
        }

        // Bind pipeline
        var currentBoundDescriptorSets = renderingContext.CurrentBoundDescriptorSets;
        if (renderingContext.CurrentPipeline != _pipeline)
        {
            vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, _pipeline);
            renderingContext.CurrentPipeline = _pipeline;
            renderingContext.PipelineChangesCount++;

            // Changing pipeline resets bound descriptor sets, so we will need to re-bind them again.
            for (var i = 0; i < currentBoundDescriptorSets.Length; i++)
                currentBoundDescriptorSets[i] = DescriptorSet.Null;

            renderingContext.IsNonStandardPipelineLayout = false; // We are using standard pipeline

            renderingContext.CurrentMatrixIndex = -1; // Set the push constants again
        }

        // Bind SceneDescriptorSet as set 0
        var descriptorSet = renderingContext.SceneDescriptorSet;
        const int sceneDescriptorSetBindingIndex = 0;
        if (descriptorSet != currentBoundDescriptorSets[sceneDescriptorSetBindingIndex])
        {
            vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, _pipelineLayout, sceneDescriptorSetBindingIndex, 1, &descriptorSet, 0, null);
            currentBoundDescriptorSets[sceneDescriptorSetBindingIndex] = descriptorSet;
        }

        // Bind LightsDescriptorSet as set 1
        const int allLightsDescriptorSetBindingIndex = 1;
        descriptorSet = renderingContext.AllLightsDescriptorSet;
        if (descriptorSet != currentBoundDescriptorSets[allLightsDescriptorSetBindingIndex])
        {
            vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, _pipelineLayout, allLightsDescriptorSetBindingIndex, 1, &descriptorSet, 0, null);
            currentBoundDescriptorSets[allLightsDescriptorSetBindingIndex] = descriptorSet;
        }

        // Bind MatricesDescriptorSets as set 2
        const int matricesDescriptorSetBindingIndex = 2;
        descriptorSet = _matricesDescriptorSets![swapChainImageIndex];
        if (descriptorSet != currentBoundDescriptorSets[matricesDescriptorSetBindingIndex])
        {
            vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, _pipelineLayout, matricesDescriptorSetBindingIndex, descriptorSetCount: 1, &descriptorSet, 0, null);
            currentBoundDescriptorSets[matricesDescriptorSetBindingIndex] = descriptorSet;
            renderingContext.DescriptorSetChangesCount++;
        }

        // Set matrix index with using PushConstants
        int matrixIndex = 0;
        if (matrixIndex != renderingContext.CurrentMatrixIndex)
        {
            vk.CmdPushConstants(commandBuffer, _pipelineLayout, ShaderStageFlags.Vertex, offset: 0, size: 4, pValues: &matrixIndex);
            renderingContext.CurrentMatrixIndex = matrixIndex;
            renderingContext.PushConstantsChangesCount++;
        }

        // NOTE: MaterialDescriptorSets is bound inside the command processing loop, because technically, the texture might change.

        // Bind vertex and index buffer
        if (_vertexBuffer!.Buffer != renderingContext.CurrentVertexBuffer)
        {
            ulong offsets = 0;
            var buffer = _vertexBuffer.Buffer;
            vk.CmdBindVertexBuffers(commandBuffer, 0, 1, &buffer, &offsets);
            renderingContext.VertexBuffersChangesCount++;
            renderingContext.CurrentVertexBuffer = _vertexBuffer.Buffer;
        }

        if (_indexBuffer!.Buffer != renderingContext.CurrentIndexBuffer)
        {
            vk.CmdBindIndexBuffer(commandBuffer, _indexBuffer.Buffer, 0, IndexType.Uint16);
            renderingContext.IndexBuffersChangesCount++;
            renderingContext.CurrentIndexBuffer = _indexBuffer.Buffer;
        }

        // Process all draw commands to draw (sub)elements of the UI
        verticesOffset = 0;
        indicesOffset = 0;

        for (var i = 0; i < drawData.CmdListsCount; i++)
        {
            var cmdList = drawData.CmdLists[i];

            for (var c = 0; c < cmdList.CmdBuffer.Size; c++)
            {
                var cmd = cmdList.CmdBuffer[c];

                // TODO: add support for other ImGui-supplied textures
                var descriptorSets = cmd.TextureId == FontTextureId ? _fontTextureDescriptorSets : null;

                const int materialDescriptorSetBindingIndex = 3;
                if (descriptorSets != null && swapChainImageIndex < descriptorSets.Length)
                {
                    descriptorSet = descriptorSets[swapChainImageIndex];
                    if (descriptorSet != currentBoundDescriptorSets[materialDescriptorSetBindingIndex])
                    {
                        vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, _pipelineLayout,
                            firstSet: materialDescriptorSetBindingIndex, descriptorSetCount: 1,
                            pDescriptorSets: &descriptorSet, dynamicOffsetCount: 0,
                            pDynamicOffsets: null);

                        currentBoundDescriptorSets[materialDescriptorSetBindingIndex] = descriptorSet;
                        renderingContext.DescriptorSetChangesCount++;
                    }
                }

                // Set clipping rect
                var scissor = new Rect2D(
                    new Offset2D((int)cmd.ClipRect.X, (int)cmd.ClipRect.Y),
                    new Extent2D((int)(cmd.ClipRect.Z - cmd.ClipRect.X), (int)(cmd.ClipRect.W - cmd.ClipRect.Y)));
                vk.CmdSetScissor(commandBuffer, 0, 1, &scissor);

                // Draw
                vk.CmdDrawIndexed(commandBuffer, cmd.ElemCount, 1, (uint)indicesOffset + cmd.IdxOffset, verticesOffset + (int)cmd.VtxOffset, 0);
            }

            verticesOffset += cmdList.VtxBuffer.Size;
            indicesOffset += cmdList.IdxBuffer.Size;
        }

        return true;
    }

    private unsafe void CreatePipeline(RenderingContext renderingContext)
    {
        var gpuDevice = renderingContext.GpuDevice;


        // We can reuse the Sprites shaders from SharpEngine.
        // The ShadersManager will get the shaders from the core assembly if shaders with the specified shaderResourceName exist.
        var shadersManager = gpuDevice.ShadersManager;
        var vertexShaderStageInfo = shadersManager.GetOrCreatePipelineShaderStage(shaderResourceName: "Sprites.spv.SpritesShader.vert.spv", ShaderStageFlags.Vertex, "main");
        var fragmentShaderStageInfo = shadersManager.GetOrCreatePipelineShaderStage(shaderResourceName: "Sprites.spv.SpritesShader.frag.spv", ShaderStageFlags.Fragment, "main");

        var pipelineShaderStages = stackalloc PipelineShaderStageCreateInfo[]
        {
            vertexShaderStageInfo,
            fragmentShaderStageInfo
        };


        // To reuse the shaders, we need to use the same pipeline layout as for Sprites shaders.
        var scene = renderingContext.Scene;
        _pipelineLayout = gpuDevice.CreatePipelineLayout(
            descriptorSetLayout: new[]
            {
                scene.SceneDescriptorSetLayout,
                scene.AllLightsDescriptorSetLayout,
                scene.AllWorldMatricesDescriptorSetLayout,
                _fontTextureDescriptorSetLayout
            },
            vertexPushConstantsSize: Scene.StandardVertexShaderPushConstantsSize, // = 4
            fragmentPushConstantsSize: Scene.StandardFragmentShaderPushConstantsSize, // = 4
            name: "ImGuiPipelineLayout");

        // Create custom vertex buffer description that matches native ImGui format
        (_vertexBufferDescription, _vertexBufferDescriptionDisposeToken) =
            VertexBufferDescription.Create(
                gpuDevice,
                new VertexInputBindingDescription[]
                {
                    new()
                    {
                        Binding = 0, Stride = 5 * 4 /* 5 32-bit values (4 floats and 1 uint32) */,
                        InputRate = VertexInputRate.Vertex
                    }
                },
                new VertexInputAttributeDescription[]
                {
                    new()
                    {
                        Location = 0, Binding = 0, Offset = 0, Format = Format.R32G32Sfloat
                    }, // Position: 2 floats (x, y)
                    new()
                    {
                        Location = 1, Binding = 0, Offset = 8, Format = Format.R32G32Sfloat
                    }, // TextureCoordinate: 2 floats (u, v)
                    new()
                    {
                        Location = 2, Binding = 0, Offset = 16, Format = Format.R8G8B8A8Unorm
                    } // Color: single 32-bit RGBA integer (= 4 bytes, one for each component)
                },
                "ImGuiVertexBufferDescription");


        float width = renderingContext.Width;
        float height = renderingContext.Height;

        var viewport = new Viewport(0, 0, width, height, 0, 1);
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
        var dynamicStates = stackalloc DynamicState[2] { DynamicState.Viewport, DynamicState.Scissor };
        var vkPipelineDynamicStateCreateInfo = new PipelineDynamicStateCreateInfo()
        {
            SType = StructureType.PipelineDynamicStateCreateInfo,
            Flags = 0,
            DynamicStateCount = 2,
            PDynamicStates = dynamicStates
        };


        var vkPipelineMultisampleStateCreateInfo = new PipelineMultisampleStateCreateInfo
        {
            SType = StructureType.PipelineMultisampleStateCreateInfo,
            RasterizationSamples = renderingContext.SceneView.MultisampleCountFlags
        };


        // Copy to local struct on the stack, so we can get the address.
        var localRasterizationState = CommonStatesManager.CullNone;
        var localInputAssemblyState = CommonStatesManager.TriangleListInputAssemblyState;
        var localDepthStencilStateCreateInfo = CommonStatesManager.DepthNone;

        // In v2.1+ the following can be used:
        //var localTessellationState = CommonStatesManager.NoTessellation;
        var localTessellationState = new PipelineTessellationStateCreateInfo()
        {
            SType = StructureType.PipelineTessellationStateCreateInfo,
            Flags = 0,
            PatchControlPoints = 0
        };

        // We cannot store PipelineColorBlendStateCreateInfo using native memory because it uses address of localColorBlendAttachmentState.
        // Therefore, we store only ColorBlendAttachmentState and now create the PipelineColorBlendStateCreateInfo so that address
        // of localColorBlendAttachmentState will be still valid when creating the pipeline.
        var localColorBlendAttachmentState = CommonStatesManager.NonPremultipliedAlphaBlendAttachmentState;
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
            Layout = _pipelineLayout,
            PViewportState = &viewportCreateInfo,
            StageCount = 2,
            PStages = pipelineShaderStages,
            PDepthStencilState = &localDepthStencilStateCreateInfo,
            PDynamicState = &vkPipelineDynamicStateCreateInfo,
            PMultisampleState = &vkPipelineMultisampleStateCreateInfo,
            PColorBlendState = &localColorBlendState,
            PRasterizationState = &localRasterizationState,
            PTessellationState = &localTessellationState,
            PInputAssemblyState = &localInputAssemblyState,
            PVertexInputState = (PipelineVertexInputStateCreateInfo*)_vertexBufferDescription.PipelineVertexInputStateCreateInfoPtr,
            RenderPass = renderingContext.SceneView.MainRenderPass!.RenderPass,
            BasePipelineHandle = Pipeline.Null,
            BasePipelineIndex = -1,
            Subpass = 0,
            Flags = PipelineCreateFlags.None
        };

        Pipeline pipeline;
        gpuDevice.Vk.CreateGraphicsPipelines(gpuDevice.Device, gpuDevice.GetPipelineCache(), 1, &pipelineCreateInfo, null, &pipeline).CheckResult();
        pipeline.SetName(gpuDevice, "ImGuiPipeline");

        _pipeline = pipeline;
    }

    private void InitializeMatricesResources(RenderingContext renderingContext)
    {
        var gpuDevice = renderingContext.GpuDevice;
        int swapChainImagesCount = renderingContext.SwapChainImagesCount;

        // Because we can have multiple frames "in-flight"
        // (e.g. some frames are being rendered by the GPU in the background while we are updating the data for the next frame)
        // we need to have multiple GpuBuffers and DescriptorSets.
        // The number of possible in-flight frames is defined by the SwapChainImagesCount.

        // Create GpuBuffers that will store the world-view-projection matrices on the GPU:
        int bufferSize = 16 * 4; // 16 floats
        _matricesBuffers = gpuDevice.CreateBuffers(bufferSize, swapChainImagesCount, BufferUsageFlags.StorageBuffer, typeof(Matrix4x4), itemsCount: 1, name: "ImGuiMatricesUniformBuffer");

        // Create DescriptorSets
        _matricesDescriptorSetLayout = gpuDevice.CreateDescriptorSetLayout(DescriptorType.StorageBuffer, ShaderStageFlags.Vertex, "ImGuiMatricesDescriptorSetLayout");

        _matricesDescriptorPool = gpuDevice.CreateDescriptorPool(DescriptorType.StorageBuffer, swapChainImagesCount, name: "ImGuiMatricesDescriptorPool");

        _matricesDescriptorSets = gpuDevice.CreateDescriptorSets(_matricesDescriptorSetLayout, _matricesDescriptorPool, swapChainImagesCount, "ImGuiMatricesDescriptorSets");
        gpuDevice.UpdateDescriptorSets(_matricesDescriptorSets, _matricesBuffers, DescriptorType.StorageBuffer);

        // Mark all GpuBuffers and DescriptorSets as dirty
        _isMatrixBufferDirty = new bool[swapChainImagesCount];
        MarkMatrixBufferDirty();
    }

    private unsafe void InitializeFontTextureResources(RenderingContext renderingContext)
    {
        var gpuDevice = renderingContext.GpuDevice;
        var swapChainImagesCount = renderingContext.SwapChainImagesCount;

        // NOTE: we need to store reference to created GpuImage, otherwise it will end up garbage-collected.
        _fontTextureImage = new GpuImage(gpuDevice, _fontTextureData, true, null);
        var sampler = renderingContext.GpuDevice.SamplerFactory.MirrorSampler;

        // Create descriptor sets
        var texturedDescriptorTypes = new[] { DescriptorType.CombinedImageSampler };
        _fontTextureDescriptorSetLayout = gpuDevice.CreateDescriptorSetLayout(texturedDescriptorTypes, ShaderStageFlags.Fragment, name: "ImGuiFontTextureDescriptorSetLayout");
        _fontTextureDescriptorPool = gpuDevice.CreateDescriptorPool(DescriptorType.StorageBuffer, swapChainImagesCount, name: "ImGuiFontTextureDescriptorPool");
        _fontTextureDescriptorSets = gpuDevice.CreateDescriptorSets(_fontTextureDescriptorSetLayout, _fontTextureDescriptorPool, swapChainImagesCount, "ImGuiFontTextureDescriptorSets");

        for (int i = 0; i < swapChainImagesCount; i++)
        {
            var descriptorImageInfo = new DescriptorImageInfo
            {
                ImageView = _fontTextureImage.ImageView,
                Sampler = sampler.Sampler,
                ImageLayout = ImageLayout.ShaderReadOnlyOptimal
            };

            var writeDescriptorSets = new WriteDescriptorSet
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = _fontTextureDescriptorSets[i],
                DescriptorType = DescriptorType.CombinedImageSampler,
                DescriptorCount = 1,
                DstBinding = 0,
                PImageInfo = &descriptorImageInfo
            };

            gpuDevice.Vk.UpdateDescriptorSets(gpuDevice.Device, descriptorWriteCount: 1,
                                              pDescriptorWrites: &writeDescriptorSets, descriptorCopyCount: 0,
                                              pDescriptorCopies: (CopyDescriptorSet*)0);
        }
    }

    private void MarkMatrixBufferDirty()
    {
        if (_isMatrixBufferDirty == null)
            return;

        for (int i = 0; i < _isMatrixBufferDirty.Length; i++)
            _isMatrixBufferDirty[i] = true;
    }

    private unsafe void DisposeMatricesResources(VulkanDevice gpuDevice)
    {
        var device = gpuDevice.Device;

        if (_matricesBuffers != null)
        {
            foreach (var buffer in _matricesBuffers)
                buffer.Dispose();

            _matricesBuffers = null;
        }

        if (!_matricesDescriptorPool.IsNull())
        {
            gpuDevice.Vk.DestroyDescriptorPool(device, _matricesDescriptorPool, null);
            _matricesDescriptorPool = DescriptorPool.Null;
        }

        _matricesDescriptorSets = null;

        if (!_matricesDescriptorSetLayout.IsNull())
        {
            gpuDevice.Vk.DestroyDescriptorSetLayout(device, _matricesDescriptorSetLayout, null);
            _matricesDescriptorSetLayout = DescriptorSetLayout.Null;
        }
    }

    private unsafe void DisposeFontTextureResources(VulkanDevice gpuDevice)
    {
        var device = gpuDevice.Device;

        if (_fontTextureImage != null)
        {
            _fontTextureImage.Dispose();
            _fontTextureImage = null;
        }

        if (!_fontTextureDescriptorPool.IsNull())
        {
            gpuDevice.Vk.DestroyDescriptorPool(device, _fontTextureDescriptorPool, null);
            _fontTextureDescriptorPool = DescriptorPool.Null;
        }

        _fontTextureDescriptorSets = null;

        if (!_fontTextureDescriptorSetLayout.IsNull())
        {
            gpuDevice.Vk.DestroyDescriptorSetLayout(device, _fontTextureDescriptorSetLayout, null);
            _fontTextureDescriptorSetLayout = DescriptorSetLayout.Null;
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            var gpuDevice = SceneView.GpuDevice;

            if (gpuDevice != null)
            {
                if (!_pipelineLayout.IsNull())
                {
                    gpuDevice.DisposeVulkanResourceOnMainThreadAfterFrameRendered(_pipelineLayout.Handle, typeof(PipelineLayout));
                    _pipelineLayout = PipelineLayout.Null;
                }

                if (!_pipeline.IsNull())
                {
                    gpuDevice.DisposeVulkanResourceOnMainThreadAfterFrameRendered(_pipeline.Handle, typeof(Pipeline));
                    _pipeline = Pipeline.Null;
                }

                DisposeMatricesResources(gpuDevice);
                DisposeFontTextureResources(gpuDevice);
            }

            if (_indexBuffer != null)
            {
                _indexBuffer.Dispose();
                _indexBuffer = null;
            }

            if (_vertexBuffer != null)
            {
                _vertexBuffer.Dispose();
                _vertexBuffer = null;
            }

            if (_vertexBufferDescription != null)
            {
                _vertexBufferDescriptionDisposeToken.Dispose();
                _vertexBufferDescription = null;
            }


            // NOTE: we do not own the ImGui context nor its I/O structure; so just reset the pointers.
            _imGuiIo = IntPtr.Zero;
            _imGuiCtx = 0;
        }

        base.Dispose(disposing);
    }
}