using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.Text;

public class BackgroundVectorTextCreationSample : CommonSample
{
    public override string Title => "Creating many vector texts in the background thread";

    private string _fontName = "Roboto-Black"; // This requires Roboto-Regular.ttf in the Resources/TrueTypeFonts folder


    private int _xCount = 10;
    private int _yCount = 10;
    private int _zCount = 50;
    private float _fontSize = 10;

    private volatile int _totalCharactersCount;
    private volatile int _totalTrianglesCount;
    private ICommonSampleUIElement? _charsCountLabel;
    
    private MeshModelNode? _allTextsNode;
    private GpuBuffer? _textsVertexBuffer;
    private GpuBuffer? _textsIndexBuffer;


    public BackgroundVectorTextCreationSample(ICommonSamplesContext context)
        : base(context)
    {
        ShowCameraAxisPanel = true;
    }

    protected override void OnCreateScene(Scene scene)
    {
        var boxModelNode = new BoxModelNode(new Vector3(0, -1100, 0), new Vector3(2000, 100, 10000), StandardMaterials.Green, "BaseGreenBox");
        scene.RootNode.Add(boxModelNode);

        RecreateText(scene);

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = -50;
            targetPositionCamera.Attitude = -15;
            targetPositionCamera.Distance = 4000;

            targetPositionCamera.StartRotation(headingChangeInSecond: 20);
        }
    }

    protected override void OnDisposed()
    {
        DisposeCurrentTextsGpuBuffers();

        base.OnDisposed();
    }

    private void DisposeCurrentTextsGpuBuffers()
    {
        // Because we have manually created GpuBuffers for vertex and index buffers, we also need to manually dispose them.
        if (_textsVertexBuffer != null)
        {
            _textsVertexBuffer.Dispose();
            _textsVertexBuffer = null;
        }
        
        if (_textsIndexBuffer != null)
        {
            _textsIndexBuffer.Dispose();
            _textsIndexBuffer = null;
        }
    }

    private void RecreateText(Scene scene)
    {
        if (_allTextsNode != null)
        {
            scene.RootNode.Remove(_allTextsNode);
            _allTextsNode = null;

            DisposeCurrentTextsGpuBuffers();
        }

        // Call async method from sync context
        _ = CreateVectorTextAsync(scene); 
    }

    private async Task CreateVectorTextAsync(Scene scene)
    {
        var gpuDevice = scene.GpuDevice;

        if (gpuDevice == null)
            return;

        // Check if font is already loaded
        if (!TrueTypeFontLoader.Instance.IsFontLoaded(_fontName))
        {
            try
            {
                // if not, load it in the background thread
                var fontFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources/TrueTypeFonts/", _fontName + ".ttf");
                await TrueTypeFontLoader.Instance.LoadFontFileAsync(fontFileName, _fontName);
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error loading font:\n" + ex.Message);
                return;
            }
        }


        StandardMesh textMesh = await Task.Run(() =>
        {
            // Run in background thread
            // We need to create a new VectorFontFactory because the existing one is used in the main thread
            var vectorFontFactory = new VectorFontFactory("Roboto-Black");

            var allTextMeshes = CreateInstanceTextMeshes(vectorFontFactory,
                                                         centerPosition: new Vector3(0, 0, 0),
                                                         size: new Vector3(2000, 2000, 10000),
                                                         xCount: _xCount, yCount: _yCount, zCount: _zCount,
                                                         _fontSize);

            // Combine all meshes from List<StandardMesh> into a single StandardMesh
            var combinedMeshes = Ab4d.SharpEngine.Utilities.MeshUtils.CombineMeshes(allTextMeshes);

            return combinedMeshes;
        });


        if (textMesh.Vertices == null || textMesh.TriangleIndices == null)
            return;



        // Create an empty mesh that will hold all text. 
        var textsMesh = new StandardMesh("AllVectorTextsMesh");


        // Create new GpuBuffers for vertex and index buffers in the BACKGROUND thread

        // IMPORTANT:
        // We will need to manually dispose these buffers when they are no longer needed (see DisposeCurrentTextsGpuBuffers)

        _textsVertexBuffer = await gpuDevice.CreateBufferAsync(textMesh.Vertices,        Ab4d.Vulkan.BufferUsageFlags.VertexBuffer, name: "VectorTextVertexBuffer");
        _textsIndexBuffer  = await gpuDevice.CreateBufferAsync(textMesh.TriangleIndices, Ab4d.Vulkan.BufferUsageFlags.IndexBuffer,  name: "VectorTextIndexBuffer");

        // Assign the created buffers to the textsMesh in the MAIN thread
        textsMesh.SetCustomVertexBuffer(textMesh.Vertices, _textsVertexBuffer, textMesh.BoundingBox);
        textsMesh.SetCustomIndexBuffer(textMesh.TriangleIndices, _textsIndexBuffer);


        Material usedMaterial = new SolidColorMaterial(Colors.Orange) { IsTwoSided = true };

        // Create MeshModelNode that will show all texts
        _allTextsNode = new MeshModelNode(textsMesh, usedMaterial, name: "AllVectorTextsNode");

        scene.RootNode.Add(_allTextsNode);

        _totalTrianglesCount = textMesh.TriangleIndices.Length / 3;

        UpdateTotalCharsCount();
    }

    private List<StandardMesh> CreateInstanceTextMeshes(VectorFontFactory vectorFontFactory, Vector3 centerPosition, Vector3 size, int xCount, int yCount, int zCount, float fontSize)
    {
        float xStep = xCount <= 1 ? 0 : (float)(size.X / (xCount - 1));
        float yStep = yCount <= 1 ? 0 : (float)(size.Y / (yCount - 1));
        float zStep = zCount <= 1 ? 0 : (float)(size.Z / (zCount - 1));
        

        var allTextMeshes = new List<StandardMesh>(xCount * yCount * zCount);
        int totalCharactersCount = 0;

        for (int z = 0; z < zCount; z++)
        {
            float zPos = (float)(centerPosition.Z - (size.Z / 2.0) + (z * zStep));

            for (int y = 0; y < yCount; y++)
            {
                float yPos = (float)(centerPosition.Y - (size.Y / 2.0) + (y * yStep));

                for (int x = 0; x < xCount; x++)
                {
                    float xPos = (float)(centerPosition.X - (size.X / 2.0) + (x * xStep));

                    string infoText = $"({xPos:0} {yPos:0} {zPos:0})";

                    var textMesh = vectorFontFactory.CreateTextMesh(infoText,
                                                                    textPosition: new Vector3(xPos, yPos, zPos),
                                                                    positionType: TextPositionTypes.Baseline, // NOTE that this takes TextPositionTypes that also defines the Baseline value
                                                                    textDirection: new Vector3(1, 0, 0),
                                                                    upDirection: new Vector3(0, 1, 0),
                                                                    fontSize: _fontSize,
                                                                    textAlignment: TextAlignment.Left);

                    totalCharactersCount += infoText.Length;

                    if (textMesh != null)
                        allTextMeshes.Add(textMesh);
                }
            }
        }

        _totalCharactersCount = totalCharactersCount;

        return allTextMeshes;
    }   

    private void UpdateTotalCharsCount()
    {
        if (_charsCountLabel == null)
            return;
        
        _charsCountLabel.SetText($"Total chars count: {_totalCharactersCount:N0}\nTotal triangles count: {_totalTrianglesCount:N0}");
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("Select texts count:", isHeader: true);

        ui.CreateRadioButtons(new string[] { "125  (5 x 5 x 5)", "5,000  (10 x 10 x 50)", "10,000  (10 x 10 x 100)", "40,000  (10 x 20 x 200)" },
            (selectedIndex, selectedText) =>
            {
                switch (selectedIndex)
                {
                    case 0: // "125":
                        _xCount = 5;
                        _yCount = 5;
                        _zCount = 5;
                        _fontSize = 50;
                        break;

                    case 1: //  "5,000":
                        _xCount = 10;
                        _yCount = 10;
                        _zCount = 50;
                        _fontSize = 20;
                        break;
                    
                    case 2: // "10,000":
                        _xCount = 10;
                        _yCount = 10;
                        _zCount = 100;
                        _fontSize = 10;
                        break;
                    
                    case 3: // "400,000":
                        _xCount = 10;
                        _yCount = 20;
                        _zCount = 200;
                        _fontSize = 10;
                        break;
                }
                
                RecreateText(this.Scene!);
            },
            selectedItemIndex: 1);

        ui.AddSeparator();

        ui.CreateLabel("Each text shows its X, Y and Z coordinate in brackets.", width: 200).SetStyle("italic");
        
        ui.AddSeparator();
        
        _charsCountLabel = ui.CreateLabel("Total chars count: ").SetStyle("bold");
        UpdateTotalCharsCount();
        
        ui.AddSeparator();

        ui.CreateButton("Stop camera rotation", () => targetPositionCamera?.StopRotation());
    }
}