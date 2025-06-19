using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GridStateData
{
    public int gridSize;
    public List<CellData> cells = new List<CellData>();
    public DateTime savedAt;

    public GridStateData(int gridSize)
    {
        this.gridSize = gridSize;
        this.savedAt = DateTime.Now;
    }
}

[Serializable]
public class CellData
{
    public int x;
    public int y;
    public string pixelType; // Using string to handle null values in JSON

    public CellData(int x, int y, PixelType? pixelType)
    {
        this.x = x;
        this.y = y;
        this.pixelType = pixelType?.ToString();
    }
} 