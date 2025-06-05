using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using Ab4d.Vulkan;
using System.Numerics;
using Ab4d.SharpEngine.Core;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class PixelsRenderingSample : CommonSample
{
    public override string Title => "Rendering 3D positions as pixels";
    
    public override string Subtitle => "See also 'Importers / Point-cloud importer' sample";

    private float _pixelSize = 2;
    private bool _useTexture = false;

    private PixelsNode? _pixelsNode;
    private GpuImage? _treeGpuImage;
    private Color4 _savedPixelsColor;
    
    private ICommonSampleUIElement? _pixelSizeComboBox;
    

    public PixelsRenderingSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        ChangeShownPositions(selectedIndex: 2); // Show Dragon model
        
                
        // IMPORTANT:
        // With PixelNode and PixelMaterial, the texture is always rendered to a square area (width == height)
        // so it is recommended that the texture is also square otherwise it will be stretched.
        //
        // In this sample we use a special TreeTexture-square.png that is the same as TreeTexture.png but
        // has added transparent pixels on the left and right so that the final image is squared.
        //_treeGpuImage = TextureLoader.CreateTexture(@"Resources\Textures\TreeTexture-square.png", scene);
        _treeGpuImage = TextureLoader.CreateTexture(@"Resources\Textures\TreeTexture-square.png", scene);

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 30;
            targetPositionCamera.Distance = 600;
        }
    }

    private void ShowMesh(StandardMesh mesh, Color4 pixelColor)
    {
        Vector3[]? positions = mesh.GetDataChannelArray<Vector3>(MeshDataChannelTypes.Positions);

        if (positions == null)
            return;

        ShowPositionsArray(positions, _pixelSize, pixelColor, mesh.BoundingBox);
    }

    private void ShowPositionsArray(Vector3[] positions, float pixelsSize, Color4 pixelColor, Vector3 centerPosition, Vector3 size)
    {
        var positionBounds = new BoundingBox(new Vector3(centerPosition.X - size.X * 0.5f, centerPosition.Y - size.Y * 0.5f, centerPosition.Z - size.Z * 0.5f),
                                             new Vector3(centerPosition.X + size.X * 0.5f, centerPosition.Y + size.Y * 0.5f, centerPosition.Z + size.Z * 0.5f));

        ShowPositionsArray(positions, pixelsSize, pixelColor, positionBounds);
    }
    
    private void ShowPositionsArray(Vector3[] positions, float pixelSize, Color4 pixelColor, BoundingBox positionsBounds)
    {
        if (Scene == null)
            return;

        // Create PixelsNode that will show the positions.
        // We can also pass the positionBounds that define the BoundingBox of the positions.
        // If this is not done, the BoundingBox is calculated by the SharpEngine by checking all the positions.
        _pixelsNode = new PixelsNode(positions, positionsBounds, pixelColor, pixelSize, "PixelsNode");

        UpdatePixelsTexture();

        Scene.RootNode.Add(_pixelsNode);
    }
    
    private void UpdatePixelsTexture()
    {
        if (_pixelsNode == null || _treeGpuImage == null)
            return;
        
        if (_useTexture)
        {
            if (!_pixelsNode.HasTexture)
            {
                // Save the current pixel color, because calling SetTexture will set the PixelColor to white (no color mask)
                _savedPixelsColor = _pixelsNode.PixelColor; 

                _pixelsNode.SetTexture(_treeGpuImage);

                // SetTexture has some additional overrides:
                //pixelsNode.SetTexture(_treeGpuImage, colorMask: Colors.Red, alphaClipThreshold: 0.1f);

                // We could also set the texture directly to the PixelMaterial of the PixelsNode
                //var pixelMaterial = pixelsNode.GetMaterial();
                //pixelMaterial.DiffuseTexture = _treeGpuImage;
                //pixelMaterial.PixelColor = Color4.White; // No color mask
            }
        }
        else
        {
            if (_pixelsNode.HasTexture)
            {
                _pixelsNode.RemoveTexture();
                _pixelsNode.PixelColor = _savedPixelsColor; // Restore the saved pixel color
            }
        }
    }
    
    private void ShowMillionBlocks(int xCount, int zCount, Vector3 blockSize, Color4 pixelColor)
    {
        float totalSizeX = xCount * blockSize.X * 1.5f; // multiply by 1.5 to add half blockSize margin between blocks
        float totalSizeZ = zCount * blockSize.Z * 1.5f;

        float x = -(totalSizeX - blockSize.X) / 2;

        for (int ix = 0; ix < xCount; ix++)
        {
            float z = -(totalSizeZ - blockSize.Z) / 2;

            for (int iz = 0; iz < zCount; iz++)
            {
                var positionsArray = CreatePositionsArray(new Vector3(x, 0, z), blockSize, 100, 100, 100);
                ShowPositionsArray(positionsArray, _pixelSize, pixelColor, new Vector3(x, 0, z), blockSize);

                z += 1.5f * blockSize.Z;
            }

            x += 1.5f * blockSize.X;
        }
    }

    public static Vector3[] CreatePositionsArray(Vector3 center, Vector3 size, int xCount, int yCount, int zCount)
    {
        var positionsArray = new Vector3[xCount * yCount * zCount];

        float xStep = xCount <= 1 ? 0 : (float)(size.X / (xCount - 1));
        float yStep = yCount <= 1 ? 0 : (float)(size.Y / (yCount - 1));
        float zStep = zCount <= 1 ? 0 : (float)(size.Z / (zCount - 1));

        float xStart = (float)center.X - ((float)size.X / 2.0f);
        float yStart = (float)center.Y - ((float)size.Y / 2.0f);
        float zStart = (float)center.Z - ((float)size.Z / 2.0f);

        int i = 0;
        for (int z = 0; z < zCount; z++)
        {
            float zPos = zStart + (z * zStep);

            for (int y = 0; y < yCount; y++)
            {
                float yPos = yStart + (y * yStep);

                for (int x = 0; x < xCount; x++)
                {
                    float xPos = xStart + (x * xStep);

                    positionsArray[i] = new Vector3(xPos, yPos, zPos);
                    i++;
                }
            }
        }

        return positionsArray;
    }
    
    private void ChangeShownPositions(int selectedIndex)
    {
        if (Scene == null)
            return;

        // Dispose existing positions
        Scene.RootNode.DisposeAllChildren(disposeMaterials: false, disposeMeshes: true);

        switch (selectedIndex)
        {
                case 0: // Box
                    var boxMesh = MeshFactory.CreateBoxMesh(centerPosition: new Vector3(0, 0, 0), size: new Vector3(150, 150, 150), xSegments: 10, ySegments: 10, zSegments: 10);
                    ShowMesh(boxMesh, Colors.Green);
                    break;

                case 1: // Sphere
                    var sphereMesh = MeshFactory.CreateSphereMesh(centerPosition: new Vector3(0, 0, 0), radius: 100, segments: 50);
                    ShowMesh(sphereMesh, Colors.DeepSkyBlue);
                    break;

                case 2: // Dragon model
                    var dragonMesh = TestScenes.GetTestMesh(TestScenes.StandardTestScenes.Dragon, finalSize: new Vector3(300, 300, 300));
                    ShowMesh(dragonMesh, Colors.Gold);
                    break;

                case 3: // 10,000 pixels (100 x 1 x 100)
                    var positionsArray = CreatePositionsArray(new Vector3(0, 0, 0), new Vector3(300, 1, 300), 100, 1, 100);
                    ShowPositionsArray(positionsArray, _pixelSize, Colors.Red, centerPosition: new Vector3(0, 0, 0), size: new Vector3(300, 1, 300));
                    break;

                case 4: // 1 million pixels (100 x 100 x 100)
                    var positionsArray2 = CreatePositionsArray(new Vector3(0, 0, 0), new Vector3(220, 220, 220), 100, 100, 100);
                    ShowPositionsArray(positionsArray2, _pixelSize, Colors.Red, centerPosition: new Vector3(0, 0, 0), size: new Vector3(220, 220, 220));
                    break;

                case 5: // 9 million pixels (9 x 1M)
                    ShowMillionBlocks(3, 3, new Vector3(80, 80, 80), Colors.Red);
                    break;

                case 6: // 25 million pixels (5 x 5 x 1M)
                    ShowMillionBlocks(5, 5, new Vector3(60, 60, 60), Colors.Red);
                    break;

                case 7: // 100 million pixels (10 x 10 x 1M)
                    ShowMillionBlocks(10, 10, new Vector3(30, 30, 30), Colors.Red);
                    break;
        }
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);


        ui.CreateLabel("Positions:");

        var items = new string[] {
        "Box (726)",
        "Sphere (2,601)",
        "Dragon model (143,382)",
        "10,000 pixels (100 x 1 x 100)",
        "1 million pixels (100 x 100 x 100)",
        "9 million pixels (9 x 1M)",
        "25 million pixels (5 x 5 x 1M)",
        "100 million pixels (10 x 10 x 1M)" };

        ui.CreateComboBox(items, (selectedIndex, selectedText) => ChangeShownPositions(selectedIndex), selectedItemIndex: 2, width: 240);


        ui.AddSeparator();
        ui.CreateLabel("PixelSize:");

        var pixelSizes = new float[] { 0.1f, 0.5f, 1, 2, 4, 8, 16, 32 };

        _pixelSizeComboBox = ui.CreateComboBox(
            pixelSizes.Select(s => s.ToString()).ToArray(),
            (selectedIndex, selectedText) =>
            {
                _pixelSize = pixelSizes[selectedIndex];

                if (_pixelsNode != null)
                    _pixelsNode.PixelSize = _pixelSize;
            },
            selectedItemIndex: 3);


        ui.AddSeparator();
        ui.AddSeparator();

        ui.CreateCheckBox("Use texture (billboard)", _useTexture, (isChecked) =>
        {
            _useTexture = isChecked;
            
            if (_pixelSize < 16)
                _pixelSizeComboBox.SetValue("32"); // Increase pixel size to 32 so that texture is visible

            UpdatePixelsTexture();
        });
    }
}