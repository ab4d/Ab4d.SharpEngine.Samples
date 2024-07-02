using System.Numerics;
using System.Text;
using Ab4d.SharpEngine.Common;

namespace Ab4d.SharpEngine.Samples.Common.Utils
{
    public static class PlyPointCloudWriter
    {
        public static void ExportPointCloud(string fileName, Vector3[] positions, Color4[] positionColors, bool isBinaryFileFormat)
        {
            if (positions == null || positions.Length == 0) 
                return;

            bool hasColors = positionColors != null && positionColors.Length > 0;
            int positionsCount = positions.Length;


            using (var stream = System.IO.File.Create(fileName))
            {
                string formatText = isBinaryFileFormat ? "binary_little_endian" : "ascii";
                string colorsHeader = hasColors ? @"
property uchar red
property uchar green
property uchar blue" : "";

                string headerText = $@"ply
format {formatText} 1.0
element vertex {positionsCount}
property float x
property float y
property float z{colorsHeader}
end_header
";

                byte[] headerBytes = Encoding.ASCII.GetBytes(headerText);

                stream.Write(headerBytes, 0, headerBytes.Length);


                if (isBinaryFileFormat)
                {
                    using (var writer = new BinaryWriter(stream))
                    {
                        for (int i = 0; i < positionsCount; i++)
                        {
                            var onePosition = positions[i];
                            writer.Write(onePosition.X);
                            writer.Write(onePosition.Y);
                            writer.Write(onePosition.Z);

                            if (hasColors)
                            {
                                var oneColor = positionColors[i];
                                int red = (int)(oneColor.Red * 255);
                                int green = (int)(oneColor.Green * 255);
                                int blue = (int)(oneColor.Blue * 255);

                                writer.Write((byte)red);
                                writer.Write((byte)green);
                                writer.Write((byte)blue);
                            }
                        }
                    }
                }
                else
                {
                    using (var writer = new StreamWriter(stream, Encoding.ASCII))
                    {
                        for (int i = 0; i < positionsCount; i++)
                        {
                            var onePosition = positions[i];
                            writer.Write(onePosition.X.ToString(System.Globalization.CultureInfo.InvariantCulture));
                            writer.Write(' ');
                            writer.Write(onePosition.Y.ToString(System.Globalization.CultureInfo.InvariantCulture));
                            writer.Write(' ');
                            writer.Write(onePosition.Z.ToString(System.Globalization.CultureInfo.InvariantCulture));

                            if (hasColors)
                            {
                                var oneColor = positionColors[i];
                                int red = (int)(oneColor.Red * 255);
                                int green = (int)(oneColor.Green * 255);
                                int blue = (int)(oneColor.Blue * 255);

                                writer.Write(' ');
                                writer.Write(red);
                                writer.Write(' ');
                                writer.Write(green);
                                writer.Write(' ');
                                writer.Write(blue);
                            }

                            writer.Write(Environment.NewLine);
                        }
                    }
                }
            }
        }
    }
}