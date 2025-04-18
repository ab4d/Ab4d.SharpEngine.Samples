using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using System.Numerics;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Common;
using System.Threading;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class BooleanMeshSample : CommonSample
{
    public override string Title => "BooleanMesh sample with background thread operations";
    public override string Subtitle => "BooleanMesh can be used when multiple Boolean operations are preformed on a mesh";

    private Vector3[]? _sphereCenters;
    private BooleanMesh[]? _sphereBooleanMeshes;
    private BooleanMesh? _initialBooleanMesh;
    private StandardMesh? _lastShownMesh;

    public BooleanMeshSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        // When working with more complex meshes, the boolean operations can take some time.
        // To prevent blocking UI thread, we can do the boolean operations in the background thread.
        //
        // The following code is using spheres with 30 segments to demonstrate a longer boolean operation.
        // 

        // First define the original box mesh
        var initialBoxMesh = MeshFactory.CreateBoxMesh(centerPosition: new Vector3(0, 25, 0), size: new Vector3(100, 50, 100));

        ShowNewOriginalMesh(initialBoxMesh);


        if (targetPositionCamera != null)
        {
            targetPositionCamera.Distance = 400;
            targetPositionCamera.StartRotation(50, 0);
        }


        _initialBooleanMesh = new BooleanMesh(initialBoxMesh);

        _sphereCenters = new Vector3[]
        {
            new Vector3(-50, 50, 50),
            new Vector3(50, 50, 50),
            new Vector3(50, 50, -50),
            new Vector3(-50, 50, -50),
            new Vector3(-50, 0, 50),
            new Vector3(50, 0, 50),
            new Vector3(50, 0, -50),
            new Vector3(-50, 0, -50)
        };

        _sphereBooleanMeshes = new BooleanMesh[_sphereCenters.Length];

        // We can create the sphere mesh and prepare the BooleanMesh in parallel - each can be created in its own thread
        Parallel.For(0, _sphereCenters.Length, (i, state) =>
        {
            var sphereMesh = MeshFactory.CreateSphereMesh(_sphereCenters[i], radius: 15, segments: 30);
            _sphereBooleanMeshes[i] = new BooleanMesh(sphereMesh);
        });


        // Save the SynchronizationContext from the current (UI) thread so we can call the method on this thread from the background thread
        // We could also use Dispatcher or some other UI framework specific class, but because this is a cross-plaform sample, we use SynchronizationContext
        var synchronizationContext = SynchronizationContext.Current;

        if (synchronizationContext == null)
            return;

        // After that we can subtract each of the sphere meshes from the initialBooleanMesh.
        // This can be done in background thread (we use Dispatcher.Invoke to "send" the mesh to the UI thread)
        Task.Factory.StartNew(() =>
        {
            // Note the order in which the sphereBooleanMesh are created is not determined (as the BooleanMeshes are created, they get available to the foreach)
            foreach (var sphereBooleanMesh in _sphereBooleanMeshes)
            {
                if (IsDisposed)
                    return; // stop processing when the sample is unloaded


                // Subtract the sphereBooleanMesh from the initialBooleanMesh
                _initialBooleanMesh.Subtract(sphereBooleanMesh);

                // Get the Mesh after the last Subtract
                var mesh = _initialBooleanMesh.GetMesh();

                synchronizationContext!.Post(generatedMeshObject =>
                {
                    ShowNewOriginalMesh((StandardMesh)generatedMeshObject!);
                }, state: mesh);
            }
        })
        // After all the boolean operations are completed, we generate texture coordinates
        // so we will be able to use texture image as material (meshes that are created with boolean operations do not have texture coordinates defined)
        //.ContinueWith(_ => GenerateTextureCoordinates(), TaskScheduler.FromCurrentSynchronizationContext()); // The last one is run on UI thread
        .ContinueWith(_ =>
        {
            GenerateTextureCoordinates();
            //ShowNewOriginalMesh(_finalMesh);
        }, TaskScheduler.FromCurrentSynchronizationContext()); // The last one is run on UI thread
    }

    private void ShowNewOriginalMesh(StandardMesh mesh)
    {
        // NOTE: This code must be executed in the UI thread

        if (Scene == null || IsDisposed) // if user already switch to another sample, then we will just return
            return;

        var meshModelNode = new MeshModelNode(mesh)
        {
            Material = StandardMaterials.Gold
        };

        Scene.RootNode.Clear();
        Scene.RootNode.Add(meshModelNode);

        _lastShownMesh = mesh;
    }


    private void GenerateTextureCoordinates()
    {
        if (_lastShownMesh == null || Scene == null || IsDisposed)
            return;

        // Boolean operation do not generate TextureCoordinates.
        // This means that you cannot show texture image as material on such object.
        //
        // But it is possible to generate TextureCoordinates with methods from MeshUtils class.
        // In this sample we will use GenerateCubicTextureCoordinates method that will generate TextureCoordinates based on the cubic projection.

        Ab4d.SharpEngine.Utilities.MeshUtils.GenerateCubicTextureCoordinates(_lastShownMesh);

        var textureImage = GetCommonTexture(this.GpuDevice, "10x10-texture.png");

        if (textureImage != null)
        {
            var meshModelNode = new MeshModelNode(_lastShownMesh)
            {
                Material = new StandardMaterial(textureImage)
            };

            Scene.RootNode.Clear();
            Scene.RootNode.Add(meshModelNode);
        }
    }
}