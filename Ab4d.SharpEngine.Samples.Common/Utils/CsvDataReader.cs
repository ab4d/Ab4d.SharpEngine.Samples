using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using Ab4d.SharpEngine.Common;

namespace Ab4d.SharpEngine.Samples.Common.Utils;

/// <summary>
/// CsvDataReader can read float data from a csv file that defines the column names in the first row.
/// By default it is set up to read data from ParaView application.
/// </summary>
public class CsvDataReader
{
    public int DataCount { get; private set; }
    public int ObjectsCount { get; private set; }

    public string[]? ColumnNames { get; private set; }
    public float[,]? DataArray { get; private set; }

    // IndividualObjectIndexes is an array of indexes in DataArray at which individual objects (for example individual streamlines) start.
    public int[]? IndividualObjectIndexes { get; private set; }

    public string PositionXColumnName = "Points:0";
    public string PositionYColumnName = "Points:1";
    public string PositionZColumnName = "Points:2";


    public void ReadFile(string fileName)
    {
        var fileLines = System.IO.File.ReadAllLines(fileName);

        var columnSeparators = new char[] { ',', ';' };

        var columnNames = fileLines[0].Split(columnSeparators, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < columnNames.Length; i++)
            columnNames[i] = columnNames[i].Trim(' ', '"', '\''); // Trim leading spaces or quotes

        var data = new float[fileLines.Length - 1, columnNames.Length];

        for (var i = 1; i < fileLines.Length; i++)
        {
            var oneLine = fileLines[i].Split(columnSeparators, StringSplitOptions.RemoveEmptyEntries);
            for (var j = 0; j < oneLine.Length; j++)
                data[i - 1, j] = float.Parse(oneLine[j], NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
        }

        this.ColumnNames = columnNames;
        this.DataArray = data;

        this.DataCount = fileLines.Length - 1;

        CollectIndividualObjectIndexes();

        if (IndividualObjectIndexes != null)
            this.ObjectsCount = IndividualObjectIndexes.Length;
    }

    public virtual void CollectIndividualObjectIndexes()
    {
        CheckIfFileRead();


        // IntegrationTime always increases (or always decreases) in one line; when this direction is changed, then a new line is started.
        // This method returns indexes at positions when a new line is started.

        int integrationTimeIndex = Array.IndexOf(ColumnNames, "IntegrationTime");
        if (integrationTimeIndex == -1)
        {
            return;
            //throw new Exception("No IntegrationTime in the data. Please override the CollectIndividualObjectIndexes method to provide your own way to split the data between objects");
        }


        var indexes = new List<int>();
        indexes.Add(0); // Add start of the first line

        var data = this.DataArray;
        int positionsCount = data.GetLength(0);

        float i0 = data[0, integrationTimeIndex];
        float i1 = data[1, integrationTimeIndex];

        var globalDirectionSign = Math.Sign(i1 - i0);

        i0 = i1;

        for (int i = 2; i < positionsCount; i++)
        {
            i1 = data[i, integrationTimeIndex];

            var oneStepDirectionSign = Math.Sign(i1 - i0);
            i0 = i1;

            if (oneStepDirectionSign != globalDirectionSign)
                indexes.Add(i);
        }

        IndividualObjectIndexes = indexes.ToArray();
    }

    public float[] GetValues(string columnName)
    {
        return GetValues(columnName, out _, out _);
    }

    public float[] GetValues(string columnName, out float minValue, out float maxValue)
    {
        return GetValues(columnName, 0, this.DataCount, out minValue, out maxValue);
    }

    public float[] GetValues(string columnName, int startIndex, int valuesCount)
    {
        return GetValues(columnName, startIndex, valuesCount, out _, out _);
    }

    public float[] GetValues(string columnName, int startIndex, int valuesCount, out float minValue, out float maxValue)
    {
        CheckIfFileRead();

        int index = Array.IndexOf(ColumnNames, columnName);
        if (index == -1)
            throw new Exception("No column with name " + columnName);

        var data = this.DataArray;

        minValue = data[startIndex, index];
        maxValue = minValue;

        
        var values = new float[valuesCount];
        values[0] = minValue;

        int endPositionIndex = startIndex + valuesCount;
        int positionsIndex   = 1;

        for (int i = startIndex + 1; i < endPositionIndex; i++)
        {
            float oneValue = data[i, index];

            values[positionsIndex] = oneValue;
            positionsIndex++;

            if (minValue > oneValue) minValue = oneValue;
            if (maxValue < oneValue) maxValue = oneValue;
        }

        return values;
    }

    public void GetValuesRange(string columnName, out float minValue, out float maxValue)
    {
        CheckIfFileRead();

        int index = Array.IndexOf(ColumnNames, columnName);
        if (index == -1)
            throw new Exception("No column with name " + columnName);

        var data = this.DataArray;

        minValue = data[0, index];
        maxValue = minValue;

        int dataCount = data.GetLength(0);

        for (int i = 1; i < dataCount; i++)
        {
            float oneValue = data[i, index];

            if (minValue > oneValue) minValue = oneValue;
            if (maxValue < oneValue) maxValue = oneValue;
        }
    }
    
    public Vector3[] GetPositions()
    {
        BoundingBox bounds;
        return GetPositions(out bounds);
    }

    public Vector3[] GetPositions(out BoundingBox bounds)
    {
        CheckIfFileRead();

        int positionsCount = DataArray.GetLength(0);
        return GetPositions(0, positionsCount, out bounds);
    }

    public Vector3[] GetPositions(int objectIndex, out BoundingBox bounds)
    {
        CheckIfFileRead();

        int positionsCount = DataArray.GetLength(0);
        return GetPositions(0, positionsCount, out bounds);
    }

    public Vector3[] GetPositions(int startIndex, int positionsCount, out BoundingBox bounds)
    {
        CheckIfFileRead();

        var positions = new Vector3[positionsCount];

        // Get indexes
        int ix = Array.IndexOf(ColumnNames, PositionXColumnName);
        int iy = Array.IndexOf(ColumnNames, PositionYColumnName);
        int iz = Array.IndexOf(ColumnNames, PositionZColumnName);

        if (ix == -1 || iy == -1 || iz == -1)
            throw new FormatException("Cannot find position data columns");

        var data = this.DataArray;

        float x = data[startIndex, ix];
        float y = data[startIndex, iy];
        float z = data[startIndex, iz];

        positions[0] = new Vector3(x, y, z);

        float minX, maxX, minY, maxY, minZ, maxZ;

        minX = maxX = x;
        minY = maxY = y;
        minZ = maxZ = z;


        int endPositionIndex = startIndex + positionsCount;
        int positionsIndex = 1;

        for (int i = startIndex + 1; i < endPositionIndex; i++)
        {
            x = data[i, ix];
            y = data[i, iy];
            z = data[i, iz];

            positions[positionsIndex] = new Vector3(x, y, z);
            positionsIndex++;


            if (minX > x) minX = x;
            if (maxX < x) maxX = x;

            if (minY > y) minY = y;
            if (maxY < y) maxY = y;

            if (minZ > z) minZ = z;
            if (maxZ < z) maxZ = z;
        }

        bounds = new BoundingBox(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
        return positions;
    }

    [MemberNotNull(nameof(ColumnNames))]
    [MemberNotNull(nameof(DataArray))]
    private void CheckIfFileRead()
    {
        if (ColumnNames == null || DataArray == null)
            throw new Exception("File not yet read");
    }
}
