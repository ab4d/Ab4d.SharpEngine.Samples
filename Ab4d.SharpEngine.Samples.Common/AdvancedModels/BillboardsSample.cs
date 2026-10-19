using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class BillboardsSample : CommonSample
{
    public override string Title => "Billboards (rendering many textures that face the camera)";
    
    private float[,]? _heightData;
    private int _xDataDimension, _yDataDimension;
    private float[]? _savedPixelSizes;
    private HeightMapSurfaceNode? _heightMapNode;
    private PixelsNode? _pixelsNode;
    private GpuImage? _treeGpuImage;


    public BillboardsSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override async Task OnCreateSceneAsync(Scene scene)
    {
        if (scene.GpuDevice == null)
            return;

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = -5;
            targetPositionCamera.Attitude = -20;
            targetPositionCamera.Distance = 120;
        }

        _treeGpuImage = await this.GetCommonTextureAsync(scene, CommonTextures.Tree);

#if VULKAN
        var heightDateFileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources/HeightMaps/simpleHeightMap.png");
        var heightImageData = BitmapIO.LoadBitmap(heightDateFileName);
#else
        string heightDateFileName = GetCommonTexturePath("Resources/HeightMaps/simpleHeightMap.png");
        var heightImageData = await scene.GpuDevice.CanvasInterop.LoadImageBytesAsync(heightDateFileName);
#endif


        // Create height data from bitmap
        // Returns value in range from 0 to 1
        _heightData = HeightMapSurfaceNode.CreateHeightDataFromImageData(heightImageData);

        _xDataDimension = _heightData.GetUpperBound(0);
        _yDataDimension = _heightData.GetUpperBound(1);


        var gradientStops = new GradientStop[]
        {
            new GradientStop(Colors.White, 1),
            new GradientStop(Colors.Gray, 0.9f),
            new GradientStop(Colors.LightGreen, 0.80f),
            new GradientStop(Colors.LightGreen, 0.7f),
            new GradientStop(Colors.SandyBrown, 0.65f),
            new GradientStop(Colors.SandyBrown, 0),
        };


        // Create 1D texture; "linear" texture that requires special texture coordinates
        var texture1 = TextureFactory.CreateGradientTexture(scene.GpuDevice, gradientStops, textureSize: 256);
        var heightMapMaterial = new StandardMaterial(texture1, name: "HeightMapTexture");


        // Create height map surface
        _heightMapNode = new HeightMapSurfaceNode(centerPosition: new Vector3(0, 0, 0),
                                                  size: new Vector3(100, 30, 100),
                                                  heightData: _heightData,
                                                  useHeightValuesAsTextureCoordinates: true,
                                                  name: "HeightMapSurface")
        {
            Material     = heightMapMaterial,
            BackMaterial = StandardMaterials.DimGray,
        };

        scene.RootNode.Add(_heightMapNode);


        AddRandomTrees(scene, 15000);
    }


    private void AddRandomTrees(Scene scene, int tresCount)
    {
        if (_heightMapNode == null || _heightData == null)
            return;

        var rnd = new Random();

        var positions = new Vector3[tresCount];
        var pixelSizes = new float[tresCount];

        float xFactor = (float)_heightMapNode.Size.X;
        float yFactor = (float)_heightMapNode.Size.Y;
        float zFactor = (float)_heightMapNode.Size.Z;

        float xOffset = xFactor * -0.5f;
        float yOffset = yFactor * -0.5f;
        float zOffset = zFactor * -0.5f;

        float maxTreeHeight = 0.85f;
        float forestHeight = 0.5f;

        float pixelSize = 2;
        //float pixelSize = 0.5f;

        for (int i = 0; i < tresCount; i++)
        {
            var x = rnd.NextSingle();
            var z = rnd.NextSingle();
            var y = GetHeight(x, z);

            if (y > maxTreeHeight)
            {
                // Skip adding tree above the maxTreeHeight
                i--;
                continue;
            }


            float treeSize;

            if (y > forestHeight)
            {
                // Trees between forestHeight and maxTreeHeight are less common as the height increases
                // Higher the tree, less probable it is to show it.
                // Also the size is smaller as the height increases.
                float percent = (y - forestHeight) / (maxTreeHeight - forestHeight);
                if (rnd.NextSingle() < percent)
                {
                    // skip this tree
                    i--;
                    continue;
                }

                treeSize = Math.Max(0.3f, 1 - percent); // smaller tree higher the y
            }
            else
            {
                treeSize = rnd.NextSingle() * 1.0f + 0.5f; // random between 0.5 to 1.5
            }

            positions[i] = new Vector3(x * xFactor + xOffset,
                                       y * yFactor + yOffset + (treeSize * 0.5f * pixelSize),
                                       z * zFactor + zOffset);

            pixelSizes[i] = treeSize;
        }


        if (_pixelsNode == null)
        {
            _pixelsNode = new PixelsNode(positions)
            {
                PixelSize = pixelSize,
                IsWorldSize = true,
                PixelSizes = pixelSizes
            };

            if (_treeGpuImage != null)
                _pixelsNode.SetTexture(_treeGpuImage);

            scene.RootNode.Add(_pixelsNode);
        }
        else
        {
            // Notify existing PixelsNode that the 
            _pixelsNode.Positions = positions;
            _pixelsNode.PixelSizes = pixelSizes;
        }

        _savedPixelSizes = null;
    }

    private float GetHeight(float x, float y)
    {
        if (x < 0 || y < 0 || x > 1 || y > 1 || _heightData == null)
            return 0;

        float xPos = x * _xDataDimension;
        float yPos = y * _yDataDimension;

        float xSubPosition = xPos - MathF.Floor(xPos);
        int xIndex1 = (int)xPos;
        
        float ySubPosition = yPos - MathF.Floor(yPos);
        int yIndex1 = (int)yPos;

        int xIndex2 = (xIndex1 >= _xDataDimension - 1) ? _xDataDimension - 1 : xIndex1 + 1;
        int yIndex2 = (yIndex1 >= _yDataDimension - 1) ? _yDataDimension - 1 : yIndex1 + 1;

        float v00 = _heightData[xIndex1, yIndex1];
        float v10 = _heightData[xIndex2, yIndex1];
        float v11 = _heightData[xIndex2, yIndex2];
        float v01 = _heightData[xIndex1, yIndex2];

        // bilinear interpolation
        // https://math.stackexchange.com/questions/3230376/interpolate-between-4-points-on-a-2d-plane

        float value = (1 - xSubPosition) * (1 - ySubPosition) * v00 + 
                       xSubPosition * (1 - ySubPosition) * v10 +
                       (1 - xSubPosition) * ySubPosition * v01 + 
                       xSubPosition * ySubPosition * v11;

        return value;
    }
    
    private void HideHigherTrees()
    {
        if (_pixelsNode == null || _pixelsNode.PixelSizes == null || _pixelsNode.Positions == null || _heightMapNode == null)
            return;

        var pixelSizes = _pixelsNode.PixelSizes;
        var positions = _pixelsNode.Positions;
        int count = pixelSizes.Length;

        // Copy existing sizes to _savedPixelSizes
        _savedPixelSizes = new float[count];
        Array.Copy(pixelSizes, _savedPixelSizes, count);

        // Set the maximum height above which trees will be hidden
        // Pixel positions are in range from -0.5 * heightMapSize to 0.5 * heightMapSize.
        // To hide upper half of the pixels, we can set the maxHeight to 0 (which is the y center of the height map).
        float maxHeight = 0; 

        for (int i = 0; i < count; i++)
        {
            if (positions[i].Y > maxHeight)
                pixelSizes[i] = 0;  // Set size to 0 to hide the tree
        }

        _pixelsNode.UpdatePixelSizes();
    }

    private void ShowHigherTrees()
    {
        if (_pixelsNode == null || _savedPixelSizes == null)
            return;

        _pixelsNode.PixelSizes = _savedPixelSizes;
        _savedPixelSizes = null;
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        var treesCounts = new int[] { 1000, 5000, 10000, 15000, 20000, 50000, 100000, 500000 };
        ui.CreateComboBox(treesCounts.Select(c => c.ToString("N0")).ToArray(), (selectedIndex, selectedText) =>
            {
                AddRandomTrees(Scene!, treesCounts[selectedIndex]);
            },
            selectedItemIndex: 3,
            keyText: "Trees count: ");

        
        ui.AddSeparator();

        ui.CreateCheckBox("Show trees at higher altitude", true, isChecked =>
        {
            if (isChecked)
                ShowHigherTrees();
            else
                HideHigherTrees();
        });
    }
}