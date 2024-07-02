using System.Globalization;
using System.Text;
using System.Numerics;
using Ab4d.SharpEngine.Common;

namespace Ab4d.SharpEngine.Samples.Common.Utils
{
    public class PlyPointCloudReader
    {
        // See ply specification:
        // https://people.math.sc.edu/Burkardt/data/ply/ply.txt

        public string? FileFormat { get; private set; }
        public int VerticesCount { get; private set; }
        public int FacesCount { get; private set; }

        public Color4[]? PixelColors { get; private set; }

        /// <summary>
        /// Gets or sets a Boolean that specified if Y and Z coordinates are swapped when reading the data.
        /// By default, this property is true, because usually Z coordinates defines the up value in the point cloud.
        /// </summary>
        public bool SwapYZCoordinates { get; set; }

        private List<string> _vertexElementTypes = new List<string>();
        private List<string> _vertexElementNames = new List<string>();

        private StringBuilder _sb = new StringBuilder();
        private List<string> _lineParts = new List<string>();

        public PlyPointCloudReader()
        {
            SwapYZCoordinates = true;
        }

        public Vector3[] ReadPointCloud(string fileName)
        {
            ResetFile();

            Vector3[]? vertices = null;
            Color4[]? pixelColors = null;

            using (var fs = System.IO.File.OpenRead(fileName))
            {
                using (var reader = new BinaryReader(fs))
                {
                    ReadHeader(reader);

                    if (VerticesCount == 0)
                        throw new FormatException("No vertices defined");

                    if (!HasStandardPosition())
                        throw new FormatException("Cannot read ply file because vertex positions are not defined in a standard way (x, y, z)");
                    
                    vertices = new Vector3[VerticesCount];

                    if (FileFormat == "ascii")
                    {
                        bool swapRedAndBlue;
                        int colorIndex = GetColorIndex(requireUCharColor: false, swapRedAndBlue: out swapRedAndBlue);
                        int redOffset = swapRedAndBlue ? 0 : 2;
                        int blueOffset = swapRedAndBlue ? 2 : 1;

                        if (colorIndex > 0)
                        {
                            pixelColors = new Color4[VerticesCount];

                            for (int i = 0; i < VerticesCount; i++)
                            {
                                ReadLineParts(reader, maxLength: 100);

                                float x = float.Parse(_lineParts[0], NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
                                float y = float.Parse(_lineParts[1], NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
                                float z = float.Parse(_lineParts[2], NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);

                                vertices[i] = new Vector3(x, z, y); // swap z and y; we assume that SwapYZCoordinates is true (if not, we fix that later - see code below)


                                int red   = Int32.Parse(_lineParts[colorIndex + redOffset]);
                                int green = Int32.Parse(_lineParts[colorIndex + 1]);
                                int blue  = Int32.Parse(_lineParts[colorIndex + blueOffset]);

                                pixelColors[i] = new Color4((float)red / 255f, (float)green / 255f, (float)blue / 255f, 1);
                            }
                        }
                        else
                        {
                            // Read only positions
                            for (int i = 0; i < VerticesCount; i++)
                            {
                                ReadLineParts(reader, maxLength: 100);

                                float x = float.Parse(_lineParts[0], NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
                                float y = float.Parse(_lineParts[1], NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
                                float z = float.Parse(_lineParts[2], NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);

                                vertices[i] = new Vector3(x, z, y); // swap z and y; we assume that SwapYZCoordinates is true (if not, we fix that later - see code below)
                            }
                        }
                    }
                    else if (FileFormat == "binary_little_endian")
                    {
                        var oneElementSize = GetSizeOfElements(_vertexElementTypes);

                        if (!HasFloatPositionFormat())
                            throw new FormatException("Binary ply file cannot be read because positions are not defined as float values");


                        bool swapRedAndBlue;
                        int colorIndex = GetColorIndex(requireUCharColor: true, swapRedAndBlue: out swapRedAndBlue);

                        if (colorIndex > 0)
                        {
                            var colorOffset = GetElementOffset(colorIndex);
                            var skippedSizeAfterPosition = colorOffset - 3 * 4;               // how many bytes we need to skip to read the color after reading the position (position is 3 * 4 bytes)
                            var skippedSizeAfterColor = oneElementSize - colorOffset - 3; // we read 3 float32, then skip colorOffset and read 3 uchars, other bytes need to be skipped

                            pixelColors = new Color4[VerticesCount];

                            for (int i = 0; i < VerticesCount; i++)
                            {
                                float x = reader.ReadSingle();
                                float y = reader.ReadSingle();
                                float z = reader.ReadSingle();

                                vertices[i] = new Vector3(x, z, y); // swap z and y; we assume that SwapYZCoordinates is true (if not, we fix that later - see code below)

                                if (skippedSizeAfterPosition > 0)
                                    reader.BaseStream.Seek(skippedSizeAfterPosition, SeekOrigin.Current);

                                byte red   = reader.ReadByte();
                                byte green = reader.ReadByte();
                                byte blue  = reader.ReadByte();

                                if (swapRedAndBlue)
                                {
                                    byte temp = red;
                                    red = blue;
                                    blue = temp;
                                }

                                pixelColors[i] = new Color4((float)red / 255f, (float)green / 255f, (float)blue / 255f, 1);

                                if (skippedSizeAfterColor > 0)
                                    reader.BaseStream.Seek(skippedSizeAfterColor, SeekOrigin.Current);
                            }
                        }
                        else
                        {
                            // Read only positions
                            var skippedSize = oneElementSize - 3 * 4; // we read 3 float32, other bytes need to be skipped

                            for (int i = 0; i < VerticesCount; i++)
                            {
                                float x = reader.ReadSingle();
                                float y = reader.ReadSingle();
                                float z = reader.ReadSingle();

                                vertices[i] = new Vector3(x, z, y); // swap z and y; we assume that SwapYZCoordinates is true (if not, we fix that later - see code below)

                                if (skippedSize > 0)
                                    reader.BaseStream.Seek(skippedSize, SeekOrigin.Current);
                            }
                        }
                    }
                    //else if (FileFormat == "binary_big_endian")
                    //{
                    //    // TODO:
                    //    // See https://stackoverflow.com/questions/8620885/c-sharp-binary-reader-in-big-endian
                    //}
                    else
                    {
                        throw new FormatException("Unsupported file format: " + FileFormat);
                    }
                }
            }

            PixelColors = pixelColors;


            if (!SwapYZCoordinates)
            {
                // Usually the SwapYZCoordinates is true because usually the point-cloud data are defined by z-up axis.
                // To make the code simpler, the code above always swaps y and z. Here we can fix that in case SwapYZCoordinates is false.
                // If you have more files where y is up, you should add the code sections that read the data without swapping y and z.
                for (int i = 0; i < VerticesCount; i++)
                {
                    var onePosition = vertices[i];
                    vertices[i] = new Vector3(onePosition.X, onePosition.Z, onePosition.Y);
                }
            }

            return vertices;
        }

        private bool HasStandardPosition()
        {
            return _vertexElementNames.Count >= 3 &&
                   _vertexElementNames[0] == "x" &&
                   _vertexElementNames[1] == "y" &&
                   _vertexElementNames[2] == "z";
        }
        
        private bool HasFloatPositionFormat()
        {
            return (_vertexElementTypes[0] == "float" || _vertexElementTypes[0] == "float32") &&
                   (_vertexElementTypes[1] == "float" || _vertexElementTypes[1] == "float32") &&
                   (_vertexElementTypes[2] == "float" || _vertexElementTypes[2] == "float32");
        }

        private int GetElementNameIndex(string name, bool exactMatch)
        {
            for (var i = 0; i < _vertexElementNames.Count; i++)
            {
                string oneName = _vertexElementNames[i];

                if ((exactMatch && oneName.Equals(name, StringComparison.OrdinalIgnoreCase)) ||
                    (!exactMatch && oneName.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1))
                {
                    return i;
                }
            }

            return -1;
        }

        private int GetElementOffset(int elementIndex)
        {
            int offset = 0;
            for (int i = 0; i < elementIndex; i++)
                offset += GetSizeOfElement(_vertexElementTypes[i]);

            return offset;
        }

        private int GetColorIndex(bool requireUCharColor, out bool swapRedAndBlue)
        {
            swapRedAndBlue = false;

            int redIndex = GetElementNameIndex("red", exactMatch: false); // this also matches "diffuse_red", etc.

            if (redIndex == -1)
                return -1;

            int greenIndex = GetElementNameIndex("green", exactMatch: false);
            int blueIndex = GetElementNameIndex("blue", exactMatch: false);

            if (greenIndex == -1 || blueIndex == -1)
                return -1;


            // Check if color is specified in the supported format (uchar)
            // This is required when reading from binary format where we need to know the color type
            // When reading from ascii, we are reading color as text and there the type is not that important
            if (requireUCharColor &&
                 (!_vertexElementTypes[redIndex].Equals("uchar", StringComparison.OrdinalIgnoreCase) ||
                 !_vertexElementTypes[greenIndex].Equals("uchar", StringComparison.OrdinalIgnoreCase) ||
                 !_vertexElementTypes[blueIndex].Equals("uchar", StringComparison.OrdinalIgnoreCase)))
            {
                return -1;
            }


            // check if we have any of the supported color orders
            if (greenIndex == redIndex + 1 && blueIndex == redIndex + 2)
                return redIndex; // RGB
            
            if (greenIndex == blueIndex + 1 && redIndex == blueIndex + 2)
            {
                swapRedAndBlue = true;
                return blueIndex; // BGR
            }

            return -1; // unsupported colors order
        }

        private void ResetFile()
        {
            FileFormat  = null;
            VerticesCount = 0;
            FacesCount  = 0;

            _vertexElementTypes.Clear();
            _vertexElementNames.Clear();
        }

        public void ReadHeader(BinaryReader reader)
        {
            ResetFile();

            var fileLine = ReadLine(reader, maxLength: 10);

            if (fileLine != "ply")
                throw new FormatException("Invalid file format. The ply file should start with ply text");

            
            string? currentElementType = null;
            int lineIndex = 0;

            while (true)
            {
                try
                {
                    ReadLineParts(reader, maxLength: 1000);

                    string lineKeyword = _lineParts[0];

                    if (lineKeyword == "end_header")
                        break;

                    if (lineKeyword == "format")
                    {
                        FileFormat = _lineParts[1];
                    }
                    else if (lineKeyword == "element")
                    {
                        currentElementType = _lineParts[1];

                        if (currentElementType == "vertex")
                            VerticesCount = Int32.Parse(_lineParts[2]);
                        else if (currentElementType == "face")
                            FacesCount = Int32.Parse(_lineParts[2]);
                    }
                    else if (lineKeyword.Equals("property", StringComparison.Ordinal))
                    {
                        if (currentElementType == "vertex")
                        {
                            _vertexElementTypes.Add(_lineParts[1]);
                            _vertexElementNames.Add(_lineParts[2]);
                        }
                    }

                    lineIndex++;
                }
                catch (Exception ex)
                {
                    throw new FormatException(string.Format("Error reading ply file header in line {0}: {1}", lineIndex, ex.Message), ex);
                }
            }
        }

        private void ReadLineParts(BinaryReader reader, int maxLength)
        {
            int lineLength = 0;

            // Reuse StringBuilder and List for each header line
            var sb = _sb;
            var lineParts = _lineParts;
            lineParts.Clear();

            while (lineLength <= maxLength)
            {
                char ch = reader.ReadChar();

                if (ch == ' ' || ch == '\n')
                {
                    if (sb.Length > 0)
                    {
                        lineParts.Add(sb.ToString());
                        sb.Clear();
                    }

                    if (ch == '\n')
                        break;
                }
                else if (ch >= ' ') // Do not add '\r' or other control chars
                {
                    sb.Append(ch);
                }

                lineLength++;
            }
        }
        
        private static string ReadLine(BinaryReader reader, int maxLength)
        {
            string line = "";

            while (line.Length <= maxLength)
            {
                char ch = reader.ReadChar();

                if (ch == '\n')
                    break;

                if (ch >= ' ')
                    line += ch; // Do not add '\r' or other control chars
            }
        
            return line;
        }

        private static int GetSizeOfElements(List<string> elementTypesList)
        {
            int size = 0;

            foreach (var elementType in elementTypesList)
                size += GetSizeOfElement(elementType);

            return size;
        }

        private static int GetSizeOfElement(string elementType)
        {
            switch (elementType)
            {
                case "float":
                case "float32":
                case "int":
                case "uint":
                case "int32":
                case "uint32":
                    return 4;

                case "int16":
                case "uint16":
                case "short":
                case "ushort":
                    return 2;

                case "int8":
                case "uint8":
                case "uchar":
                case "char":
                    return 1;

                case "float64":
                case "double":
                    return 8;
            }

            return 0; // unknown
        }
    }
}