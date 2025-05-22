using Ab4d.SharpEngine.Samples.Common.Utils;
using System.Drawing;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class StreamlinesSample : CommonSample
{
    public override string Title => "Streamlines";
    public override string Subtitle => "Streamlines with density encoded into color: blue: low density, red: high density\nStreamlines data are read from a csv file";

    private StandardMaterial? _gradientMaterial;

    public StreamlinesSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        if (scene.GpuDevice == null)
            return;

        // Create the gradient that will represent the density (blue: low density, red: high density)
        var gradient = new GradientStop[]
        {
            new GradientStop(Colors.Blue, 0.0f),
            new GradientStop(Colors.Aqua, 0.25f),
            new GradientStop(Colors.LightGreen, 0.5f),
            new GradientStop(Colors.Yellow, 0.75f),
            new GradientStop(Colors.Red, 1.0f),
        };

        var gradientTexture = TextureFactory.CreateGradientTexture(scene.GpuDevice, gradient, textureSize: 256);

        _gradientMaterial = new StandardMaterial(gradientTexture);


        // Sample data was created by using ParaView application and exporting the streamlines into csv file.
        string sampleDataFileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources/streamlines.csv");
        sampleDataFileName = FileUtils.FixDirectorySeparator(sampleDataFileName);

        ReadStreamlinesData(scene, sampleDataFileName);
    }

    protected override void OnDisposed()
    {
        if (_gradientMaterial != null)
        {
            _gradientMaterial.DisposeWithTexture();
            _gradientMaterial = null;
        }

        base.OnDisposed();
    }

    private void ReadStreamlinesData(Scene scene, string sampleDataFileName)
    {
        // We will set the tube path position color based on the Density
        string colorColumnName  = "Density"; // "AngularVelocity"
        bool   invertColorValue = false;     // This needs to be set to true when using AngularVelocity


        // Create csv file reader that can read data from a csv file
        var csvDataReader = new CsvDataReader();
        csvDataReader.ReadFile(sampleDataFileName);


        csvDataReader.GetValuesRange(colorColumnName, out var minValue, out var maxValue);

        float dataRange = maxValue - minValue;


        var streamlineIndexes = csvDataReader.IndividualObjectIndexes;

        if (streamlineIndexes == null)
            return;

        // Create the streamlines
        var allStreamlineBounds = new BoundingBox();

        for (var i = 0; i < streamlineIndexes.Length - 1; i++)
        {
            int startIndex = streamlineIndexes[i];
            int endIndex = streamlineIndexes[i + 1] - 1;

            int dataCount = endIndex - startIndex;

            if (dataCount < 2) // Skip streamlines without any positions or with less than 2 positions
                continue;

            var positions = csvDataReader.GetPositions(startIndex, dataCount, out var bounds);

            allStreamlineBounds.Add(bounds);

            float[] dataValues = csvDataReader.GetValues(colorColumnName, startIndex, dataCount);

            // Generate texture coordinates for each path position
            // Because our texture is one dimensional gradient image (size 256 x 1)
            // we set the x coordinate in range from 0 to 1 (0 = first gradient color; 1 = last gradient color).
            
            var positionsCount     = positions.Length;
            var textureCoordinates = new Vector2[positionsCount];

            for (int j = 0; j < dataCount; j++)
            {
                float relativeDataValue = (dataValues[j] - minValue) / dataRange;

                if (invertColorValue)
                    relativeDataValue = 1.0f - relativeDataValue;

                textureCoordinates[j] = new Vector2(relativeDataValue, 0.5f);
            }


            var tubePathMesh = MeshFactory.CreateTubeMeshAlongPath(pathPositions: positions,
                                                                   pathPositionTextureCoordinates: textureCoordinates,
                                                                   radius: 0.03f,
                                                                   isTubeClosed: true,
                                                                   isPathClosed: false,
                                                                   segments: 8);

            var meshModelNode = new MeshModelNode(tubePathMesh, _gradientMaterial);

            scene.RootNode.Add(meshModelNode);
        }

        if (targetPositionCamera != null)
        {
            targetPositionCamera.TargetPosition = allStreamlineBounds.GetCenterPosition();
            targetPositionCamera.Distance = allStreamlineBounds.GetDiagonalLength();
            targetPositionCamera.Heading = 30;
            targetPositionCamera.Attitude = -20;
        }
    }
}