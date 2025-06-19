using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class SmithGridStateData
{
    public int gridSize;
    public List<SmithCellData> cells = new List<SmithCellData>();
    public DateTime savedAt;

    public SmithGridStateData(int gridSize)
    {
        this.gridSize = gridSize;
        this.savedAt = DateTime.Now;
    }
}

[Serializable]
public class SmithCellData
{
    public int x;
    public int y;
    public string bitName;
    public BitType bitType;
    public Rarity rarity;
    public int damage;
    public float shootingProbability;

    public SmithCellData(int x, int y, Bit bitData)
    {
        this.x = x;
        this.y = y;
        if (bitData != null)
        {
            this.bitName = bitData.bitName;
            this.bitType = bitData.bitType;
            this.rarity = bitData.rarity;
            this.damage = bitData.damage;
            this.shootingProbability = bitData.shootingProbability;
        }
    }
}

public class SmithBuildManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public Transform buildGridPanel; // Assign the BuildGridPanel
    public int gridSize = 2; // Current grid size (will be expandable)

    [Header("Character Integration")]
    public PowerBitPlayerController playerController; // Assign in inspector

    private BuildGridCellUI[,] gridCells;

    void Start()
    {
        InitializeGridCells();
    }

    private void InitializeGridCells()
    {
        if (buildGridPanel == null)
        {
            Debug.LogError("BuildGridPanel not assigned!");
            return;
        }

        gridCells = new BuildGridCellUI[gridSize, gridSize];
        BuildGridCellUI[] cells = buildGridPanel.GetComponentsInChildren<BuildGridCellUI>();
        
        if (cells.Length != gridSize * gridSize)
        {
            Debug.LogError($"Expected {gridSize * gridSize} grid cells, but found {cells.Length}");
            return;
        }

        int index = 0;
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                gridCells[x, y] = cells[index];
                index++;
            }
        }
    }

    public void SaveBuild()
    {
        Debug.Log("=== SAVING SMITH BUILD ===");
        
        SmithGridStateData gridState = new SmithGridStateData(gridSize);
        
        // Collect all cell data
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                BuildGridCellUI cell = gridCells[x, y];
                InventoryBitSlotUI bitSlot = cell.GetComponentInChildren<InventoryBitSlotUI>();
                
                if (bitSlot != null && bitSlot.bitData != null)
                {
                    SmithCellData cellData = new SmithCellData(x, y, bitSlot.bitData);
                    gridState.cells.Add(cellData);
                    Debug.Log($"Cell [{x},{y}]: {bitSlot.bitData.bitName} ({bitSlot.bitData.rarity})");
                }
                else
                {
                    Debug.Log($"Cell [{x},{y}]: Empty");
                }
            }
        }

        // Convert to JSON and save
        string json = JsonUtility.ToJson(gridState, true);
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, "smith_build.json");
        System.IO.File.WriteAllText(filePath, json);
        
        Debug.Log($"Build saved to: {filePath}");
        Debug.Log($"Total Power Bits: {gridState.cells.Count}");
        Debug.Log($"Save time: {gridState.savedAt}");
        
        // Load the build into the character
        if (playerController != null)
        {
            playerController.LoadSmithBuild(gridState);
        }
        else
        {
            Debug.LogWarning("PlayerController not assigned! Cannot load build into character.");
        }
    }

    public void CloseSmithMenu()
    {
        // Find and hide the SmithCanvas
        GameObject smithCanvas = GameObject.Find("SmithCanvas");
        if (smithCanvas != null)
        {
            smithCanvas.SetActive(false);
        }
    }
} 