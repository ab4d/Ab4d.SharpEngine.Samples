using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using Ab4d.Vulkan;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

public class AsyncUploadSample : CommonSample
{
    public override string Title => "Async (background) texture loading and buffer data upload";
 
    private PlaneModelNode? _planeModelNode1;
    private PlaneModelNode? _planeModelNode2;
    private MeshModelNode? _meshModelNode1;
    private MeshModelNode? _meshModelNode2;

    private readonly string _textureFileName;

    private bool _isUploadDelayed = true;

    public AsyncUploadSample(ICommonSamplesContext context)
        : base(context)
    {
        _textureFileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources/Textures/sharp-engine-logo.png");
    }

    protected override void OnCreateScene(Scene scene)
    {
        InitializeTestScene(scene);

        // Start camera rotation to see any UI thread delay
        targetPositionCamera?.StartRotation(50);
    }

    private void InitializeTestScene(Scene scene)
    {
        scene.RootNode.Clear();

        // Initially create a few models with gray material.
        // The material will change after texture is loaded or when new vertex and index buffers are uploaded
        _planeModelNode1 = new PlaneModelNode()
        {
            Position = new Vector3(0, 0, 150),
            Size = new Vector2(200, 200),
        };
        scene.RootNode.Add(_planeModelNode1);
        
        _planeModelNode2 = new PlaneModelNode()
        {
            Position = new Vector3(0, 0, -150),
            Size = new Vector2(200, 200),
        };
        scene.RootNode.Add(_planeModelNode2);


        var originalMesh1 = Meshes.MeshFactory.CreateBoxMesh(new Vector3(0, 0, 0), new Vector3(100, 100, 100), 1, 1, 1);

        _meshModelNode1 = new MeshModelNode(originalMesh1, "LazyLoadedMeshModelNode1") { Transform = new TranslateTransform(200, 50, 150) };
        scene.RootNode.Add(_meshModelNode1);
        
        
        var originalMesh2 = Meshes.MeshFactory.CreateBoxMesh(new Vector3(0, 0, 0), new Vector3(100, 100, 100), 1, 1, 1);

        _meshModelNode2 = new MeshModelNode(originalMesh2, "LazyLoadedMeshModelNode2") { Transform = new TranslateTransform(200, 50, -150) };
        scene.RootNode.Add(_meshModelNode2);

        ResetMaterials();
    }

    private void ResetMaterials()
    {
        var grayMaterial = StandardMaterials.Gray;

        if (_planeModelNode1 != null)
            _planeModelNode1.Material = grayMaterial;

        if (_planeModelNode2 != null)
            _planeModelNode2.Material = grayMaterial;

        if (_meshModelNode1 != null)
            _meshModelNode1.Material = grayMaterial;

        if (_meshModelNode2 != null)
            _meshModelNode2.Material = grayMaterial;
    }

    private async Task LoadTextureAsync(ModelNode? modelNode)
    {
        if (Scene == null || Scene.GpuDevice == null || modelNode == null)
            return;

        try
        {
            StandardMaterial textureMaterial;

            if (!_isUploadDelayed)
            {
                //
                // Detailed comments about texture loading can be found in the Materials/MaterialsSample.cs file - see "3) Material with Diffuse texture" section
                //

                // The easiest way to load texture in the background thread is to use the StandardMaterial,
                // set the file name and loadInBackground to true.
                // We can also set the initialDiffuseColor - this will show the material as gray until the texture is loaded.
                textureMaterial = new StandardMaterial(_textureFileName, 
                                                       initialDiffuseColor: Colors.Gray, 
                                                       loadInBackground: true, 
                                                       name: "LazyLoadedGpuImageMaterial");

                // We could also manually load the texture by using TextureLoader
                // var gpuImage = await TextureLoader.CreateTextureAsync(_textureFileName, Scene, useSceneCache: false);
                // textureMaterial = new StandardMaterial(gpuImage, name: "LazyLoadedGpuImageMaterial");

                // If you want to continue executing the method and provide only a simple code that uses the created GpuImage, use the following:
                //TextureLoader.CreateTextureAsync(_textureFileName, 
                //                                 Scene, 
                //                                 textureCreatedCallback: createdGpuImage => modelNode.Material = new StandardMaterial(createdGpuImage, name: "LazyLoadedGpuImageMaterial"), 
                //                                 useSceneCache: false,
                //                                 textureCreationFailedCallback: exception => modelNode.Material = StandardMaterials.Red);
            }
            else
            {
                // Use SlowStream that adds delay to reading the stream
                await using var fileStream = System.IO.File.OpenRead(_textureFileName);
                await using var slowStream = new SlowStream(fileStream, delayMilliseconds: 2); // 2 ms delay is added to each stream.Read call

                // We cannot use stream in the StandardMaterial constructor, because in this case the reading
                // of the stream would happen after the stream is already closed.
                //textureMaterial = new StandardMaterial(slowStream,
                //                                       _textureFileName,
                //                                       initialDiffuseColor: Colors.Gray,
                //                                       loadInBackground: true,
                //                                       name: "LazyLoadedGpuImageMaterial");

                // So we need to manually load the texture by using TextureLoader
                var gpuImage = await TextureLoader.CreateTextureAsync(slowStream, _textureFileName, Scene, useSceneCache: false);

                textureMaterial = new StandardMaterial(gpuImage, name: "LazyLoadedGpuImageMaterial");
            }

            modelNode.Material = textureMaterial;
        }
        catch
        {
            modelNode.Material = StandardMaterials.Red;
        }
    }        

    private void LoadTexture(ModelNode? modelNode)
    {
        if (Scene == null || Scene.GpuDevice == null || modelNode == null)
            return;

        try
        {
            if (!_isUploadDelayed)
            {
                var textureMaterial = new StandardMaterial(_textureFileName, name: "GpuImageMaterial");
                
                // We could also manually load the texture by using TextureLoader
                // var gpuImage = TextureLoader.CreateTexture(_textureFileName, Scene, useSceneCache: false);
                // textureMaterial = new StandardMaterial(gpuImage, name: "GpuImageMaterial");

                modelNode.Material = textureMaterial;
            }
            else
            {
                // Use SlowStream that adds delay to reading the stream
                using var fileStream = System.IO.File.OpenRead(_textureFileName);
                using var slowStream = new SlowStream(fileStream, delayMilliseconds: 2); // 2 ms delay is added to each stream.Read call

                var textureMaterial = new StandardMaterial(slowStream, _textureFileName, name: "GpuImageMaterial");
                
                // We must set the textureMaterial to modelNode inside this else block.
                // In the setter, the OnInitializeSceneResources method is called and there the loading of the stream happens.
                // If we would set the Material outside this block, then the stream would be already closed.
                // In this case we can use the TextureLoader.CreateTexture as shown in the commented code below.
                modelNode.Material = textureMaterial;

                // We could also manually load the texture by using TextureLoader
                // var gpuImage = TextureLoader.CreateTexture(slowStream, _textureFileName, Scene, useSceneCache: false);
                // modelNode.Material = new StandardMaterial(gpuImage, name: "GpuImageMaterial");
            }
        }
        catch
        {
            modelNode.Material = StandardMaterials.Red;
        }
    }


    private async Task CreateMeshAsync(MeshModelNode? meshModelNode)
    {
        if (Scene == null || Scene.GpuDevice == null || meshModelNode == null)
            return;

        // Create a complex sphere mesh in the background thread
        StandardMesh? newSphereMesh = null;
        await Task.Run(() =>
        {
            if (_isUploadDelayed)
                System.Threading.Thread.Sleep(500);

            // Create sphere mesh with 300 segments: 90,601 positions and 59,800 triangles
            newSphereMesh = MeshFactory.CreateSphereMesh(new Vector3(0, 0, 0), radius: 50, segments: 300, "NewSphereMesh");
        });

        if (newSphereMesh == null)
            return;

        var gpuDevice = Scene.GpuDevice;
        var vertices = newSphereMesh.Vertices!;
        var triangleIndices = newSphereMesh.TriangleIndices!;

        var newVertexBuffer = await gpuDevice.CreateBufferAsync(vertices,        BufferUsageFlags.VertexBuffer, name: "AsyncCreatedVertexBuffer");
        var newIndexBuffer  = await gpuDevice.CreateBufferAsync(triangleIndices, BufferUsageFlags.IndexBuffer,  name: "AsyncCreatedIndexBuffer");

        // We could also create a new GpuBuffer manually by using the GpuBuffer constructor and then call:
        //await newVertexBuffer.WriteToBufferAsync(vertices); // NOTE: The size of GpuBuffer must be big enough for the vertices


        // Set custom vertex and index buffer to MeshModelNode:
        meshModelNode.SetCustomVertexBuffer(vertices, newVertexBuffer, newSphereMesh.BoundingBox);
        meshModelNode.SetCustomIndexBuffer(triangleIndices, newIndexBuffer);
        

        // We could also update the mesh directly by the following code:
        // originalMesh.SetCustomVertexBuffer(newVertexBuffer, newSphereMesh.BoundingBox);
        // originalMesh.SetCustomIndexBuffer(newIndexBuffer);
        //
        // // After that updating the mesh, we also need to inform the originalMeshModelNode about this change:
        // originalMeshModelNode.NotifyChange(SceneNodeDirtyFlags.MeshChanged);
        //
        //
        // The problem with this code is that the hit-testing code is still working on the old data from vertices and triangleIndices.
        // In this case this is not a problem because the shape of mesh is the same, but if the mesh would be different,
        // then we would need to also update the vertices and triangleIndices (as done in this sample).
        // Another option is to call SetCustomVertexBuffer and SetCustomIndexBuffer on SimpleMesh or TriangleMesh<PositionNormalTextureVertex>
        // and pass new vertices and triangleIndices:
        //if (meshModelNode.Mesh is StandardMesh standardMesh)
        //{
        //    standardMesh.SetCustomVertexBuffer(vertices, newVertexBuffer, newSphereMesh.BoundingBox);
        //    standardMesh.SetCustomIndexBuffer(triangleIndices, newIndexBuffer);
        //    originalMeshModelNode.NotifyChange(SceneNodeDirtyFlags.MeshChanged);
        //}


        // Set material to green
        meshModelNode.Material = StandardMaterials.Green;
    }

    private void CreateMesh(MeshModelNode? meshModelNode)
    {
        if (Scene == null || Scene.GpuDevice == null || meshModelNode == null)
            return;

        
        if (_isUploadDelayed)
            System.Threading.Thread.Sleep(500);

        // Create sphere mesh with 300 segments: 90,601 positions and 59,800 triangles
        var newSphereMesh = MeshFactory.CreateSphereMesh(new Vector3(0, 0, 0), radius: 50, segments: 300, "NewSphereMesh");

        var gpuDevice = Scene.GpuDevice;
        var vertices = newSphereMesh.Vertices!;
        var triangleIndices = newSphereMesh.TriangleIndices!;

        var newVertexBuffer = gpuDevice.CreateBuffer(vertices,        usage: BufferUsageFlags.VertexBuffer, name: "VertexBuffer");
        var newIndexBuffer  = gpuDevice.CreateBuffer(triangleIndices, usage: BufferUsageFlags.IndexBuffer,  name: "IndexBuffer");

        // Set custom vertex and index buffer to MeshModelNode
        // See additional comments in CreateMeshAsync
        meshModelNode.SetCustomVertexBuffer(vertices, newVertexBuffer, newSphereMesh.BoundingBox);
        meshModelNode.SetCustomIndexBuffer(triangleIndices, newIndexBuffer);

        // Set material to green
        meshModelNode.Material = StandardMaterials.Green;
    }

    private void AddManySceneNodes(int sceneNodesCount)
    {
        if (Scene == null || Scene.GpuDevice == null)
            return;

        float radius = 10;
        Vector3 xOffset = new Vector3(radius * 1.2f, 0, 0);
        Vector3 yOffset = new Vector3(0, radius * 1.2f, 0);
        Vector3 offset = new Vector3(300, 0, -150);

        int rowsCount = 10;

        var greenMaterial = StandardMaterials.Green; // Use the same instance of StandardMaterial for all MeshModelNode; the StandardMaterials.Green getter creates a new instance on each call.

        var groupNode = new GroupNode("AllSpheres");

        for (int i = 0; i < sceneNodesCount; i++)
        {
            int x = i / rowsCount;
            int y = i % rowsCount;

            var oneSphereMesh = MeshFactory.CreateSphereMesh(offset + x * xOffset + y * yOffset, radius: 5, segments: 30);

            // NOTE:
            // Creating many sphere meshed with the same segments count just to have different position is very inefficient.
            // Instead, curate a single CreateSphereMesh and the then use transformation to change the position or size of the sphere.
            // But for this sample, we simulate a scenario where we would create many different meshes.

            var oneMeshModelNode = new MeshModelNode(oneSphereMesh, greenMaterial);

            groupNode.Add(oneMeshModelNode);
        }

        Scene.RootNode.Add(groupNode);

        if (_isUploadDelayed)
            System.Threading.Thread.Sleep(500);
    }
    
    private async Task AddManySceneNodesAsync(int sceneNodesCount)
    {
        if (Scene == null || Scene.GpuDevice == null)
            return;

        // Create a complex sphere mesh in the background thread

        var triangleIndicesCounts = new int[sceneNodesCount];
        var boundingBoxes = new BoundingBox[sceneNodesCount];

        StandardMesh? combinedMesh = null;

        await Task.Run(() =>
        {
            //
            // The following code is run on background thread:
            //

            var allMeshes = new List<StandardMesh>(sceneNodesCount);

            float radius = 10;
            Vector3 xOffset = new Vector3(radius * 1.2f, 0, 0);
            Vector3 yOffset = new Vector3(0, radius * 1.2f, 0);
            Vector3 offset = new Vector3(300, 0, 150);

            int rowsCount = 10;

            for (int i = 0; i < sceneNodesCount; i++)
            {
                int x = i / rowsCount;
                int y = i % rowsCount;

                var oneSphereMesh = MeshFactory.CreateSphereMesh(offset + x * xOffset + y * yOffset, radius: 5, segments: 30);
                allMeshes.Add(oneSphereMesh);

                // Save how many triangle indices is needed for this mesh so we can later get the offsets and indices count for each MeshModelNode
                // Here we could use a single indices count because all sphere meshes have the same count of segments, but in reality we may use different meshes
                triangleIndicesCounts[i] = (int)oneSphereMesh.IndexCount;

                // We also need to save the individual bounding boxes
                boundingBoxes[i] = oneSphereMesh.BoundingBox;
            }

            // Combine all meshes into a single mesh
            combinedMesh = Ab4d.SharpEngine.Utilities.MeshUtils.CombineMeshes(allMeshes);

            if (_isUploadDelayed)
                System.Threading.Thread.Sleep(500);
        });


        if (combinedMesh == null)
            return;

        var gpuDevice = Scene.GpuDevice;
        var combinedVertices = combinedMesh.Vertices!;
        var combinedTriangleIndices = combinedMesh.TriangleIndices!;

        //
        // The data for vertex and index buffers are uploaded in the background thread
        //
        var combinedVertexBuffer = await gpuDevice.CreateBufferAsync(combinedVertices,        BufferUsageFlags.VertexBuffer, name: "AsyncCreatedCombinedVertexBuffer");
        var combinedIndexBuffer  = await gpuDevice.CreateBufferAsync(combinedTriangleIndices, BufferUsageFlags.IndexBuffer,  name: "AsyncCreatedCombinedIndexBuffer");

        // We could also create a new GpuBuffer manually by using the GpuBuffer constructor and then call:
        //await newVertexBuffer.WriteToBufferAsync(vertices); // NOTE: The size of GpuBuffer must be big enough for the vertices

        //
        // The following code is running on UI thread (we must not create SceneNodes and Materials on background thread).
        // NOTE: If this code still takes too long to run, then split adding new SceneNode objects into multiple frames,
        //       for example, instead of adding 20,000 MeshModelNode in one frame, add 2000 per frame,
        //       slightly reducing the frame rate for 10 frames. This can look much nicer for the user.
        //
        int indexOffset = 0;

        var vertexBufferDescription = gpuDevice.VertexBufferDescriptionsManager.GetPositionNormalTextureVertexBufferDescription();
        var greenMaterial = StandardMaterials.Green; // Use the same instance of StandardMaterial for all MeshModelNode; the StandardMaterials.Green getter creates a new instance on each call.

        var groupNode = new GroupNode("AllAsyncSpheres");


        for (int i = 0; i < sceneNodesCount; i++)
        {
            // First create a new StandardMesh with combined vertices and triangle indices - those are needed for hit-testing
            var oneMesh = new StandardMesh(combinedVertices, combinedTriangleIndices, boundingBoxes[i], $"Mesh_{i}");

            // The set custom vertex and index buffers and specify indexOffset and customIndexCount to use only part of the triangles
            oneMesh.SetCustomVertexBuffer(combinedVertices, combinedVertexBuffer, boundingBoxes[i], vertexBufferDescription);
            oneMesh.SetCustomIndexBuffer(combinedTriangleIndices, combinedIndexBuffer, indexOffset: indexOffset, customIndexCount: triangleIndicesCounts[i]);

            // Then we can create a new MeshModelNode from the mesh
            var oneMeshModelNode = new MeshModelNode(oneMesh, greenMaterial, name: $"MeshModelNode_{i}");

            groupNode.Add(oneMeshModelNode);

            indexOffset += triangleIndicesCounts[i];
        }

        Scene.RootNode.Add(groupNode);
    }

    public class SlowStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly int _delayMilliseconds;

        public SlowStream(Stream baseStream, int delayMilliseconds)
        {
            _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _delayMilliseconds = delayMilliseconds >= 0 ? delayMilliseconds : throw new ArgumentOutOfRangeException(nameof(delayMilliseconds));
        }

        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => _baseStream.CanSeek;
        public override bool CanWrite => _baseStream.CanWrite;
        public override long Length => _baseStream.Length;

        public override long Position
        {
            get => _baseStream.Position;
            set => _baseStream.Position = value;
        }

        public override void Flush()
        {
            _baseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            System.Threading.Thread.Sleep(_delayMilliseconds);
            return _baseStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _baseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _baseStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _baseStream.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _baseStream?.Dispose();
            
            base.Dispose(disposing);
        }
    }


    /// <inheritdoc />
    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateButton("Load texture sync", () => LoadTexture(_planeModelNode2));
        ui.CreateButton("Load texture async", () => _ = LoadTextureAsync(_planeModelNode1)); // Note: calling async method from sync context. Discarding the result Task to prevent warning (adding "_ = ...")
        ui.AddSeparator();
        
        ui.CreateButton("Update mesh buffers sync", () => CreateMesh(_meshModelNode2));
        ui.CreateButton("Update mesh buffers async", () => _ = CreateMeshAsync(_meshModelNode1)); // Note: calling async method from sync context. Discarding the result Task to prevent warning (adding "_ = ...")
        ui.AddSeparator();
        
        ui.CreateButton("Add many SceneNodes sync", () => AddManySceneNodes(sceneNodesCount: 1000));
        ui.CreateButton("Add many SceneNodes async", () => _ = AddManySceneNodesAsync(sceneNodesCount: 1000)); // Note: calling async method from sync context. Discarding the result Task to prevent warning (adding "_ = ...")
        ui.AddSeparator();

        ui.CreateButton("Reset materials to gray", ResetMaterials);
        ui.AddSeparator();
        ui.CreateCheckBox("Simulate long upload (?):When checked then a delay is added when creating the mesh to simulate a complex mesh upload.", _isUploadDelayed, isChecked => _isUploadDelayed = isChecked);

        if (Scene != null && Scene.GpuDevice != null && 
            (!Scene.GpuDevice.IsBackgroundImageUploadSupported ||
             !Scene.GpuDevice.IsBackgroundBufferUploadSupported))
        {
            ui.CreateLabel("Async upload is not supported!").SetColor(Colors.Red);
        }
    }
}