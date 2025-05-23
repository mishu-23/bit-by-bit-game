using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class BuildGridPanel : MonoBehaviour
{
    public static BuildGridPanel Instance { get; private set; }

    private const int MIN_GRID_SIZE = 2;
    private const int MAX_GRID_SIZE = 8;
    private const float MIN_CELL_SIZE = 50f;
    private const float MAX_CELL_SIZE = 200f;

    [Header("Grid Settings")]
    [SerializeField] private int gridSize = 4;
    [SerializeField] private float cellSize = 100f;
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private MessagePanel messagePanel;

    private GridCell[,] gridCells;
    private PlayerStats playerStats;

    private Vector2Int requiredFirstPosition = new Vector2Int(0, 3);
    private bool firstPixelPlaced = false;
    private Dictionary<Vector2Int, PixelCell> placedPixels = new Dictionary<Vector2Int, PixelCell>();
    private Dictionary<PixelType, int> pixelCounts = new Dictionary<PixelType, int>();

    public event Action<Vector2Int, PixelCell> OnPixelPlaced;
    public event Action<Vector2Int, PixelCell> OnPixelRemoved;
    public event Action OnGridInitialized;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        ValidateSettings();
        InitializeComponents();
    }

    private void ValidateSettings()
    {
        if (gridSize < MIN_GRID_SIZE || gridSize > MAX_GRID_SIZE)
        {
            Debug.LogWarning($"[{nameof(BuildGridPanel)}] Invalid grid size: {gridSize}. Must be between {MIN_GRID_SIZE} and {MAX_GRID_SIZE}");
            gridSize = Mathf.Clamp(gridSize, MIN_GRID_SIZE, MAX_GRID_SIZE);
        }

        if (cellSize < MIN_CELL_SIZE || cellSize > MAX_CELL_SIZE)
        {
            Debug.LogWarning($"[{nameof(BuildGridPanel)}] Invalid cell size: {cellSize}. Must be between {MIN_CELL_SIZE} and {MAX_CELL_SIZE}");
            cellSize = Mathf.Clamp(cellSize, MIN_CELL_SIZE, MAX_CELL_SIZE);
        }

        if (cellPrefab == null)
        {
            Debug.LogError($"[{nameof(BuildGridPanel)}] Cell prefab is not assigned!");
        }

        if (messagePanel == null)
        {
            Debug.LogWarning($"[{nameof(BuildGridPanel)}] MessagePanel reference is missing!");
        }
    }

    private void InitializeComponents()
    {
        playerStats = FindObjectOfType<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogWarning($"[{nameof(BuildGridPanel)}] PlayerStats not found in scene!");
        }
        CreateGrid();
    }

    private void CreateGrid()
    {
        gridCells = new GridCell[gridSize, gridSize];

        // Clear existing cells
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Create new grid
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                CreateGridCell(x, y);
            }
        }

        OnGridInitialized?.Invoke();
    }

    private void CreateGridCell(int x, int y)
    {
        GameObject cell = Instantiate(cellPrefab, transform);
        RectTransform cellRect = cell.GetComponent<RectTransform>();

        cellRect.anchoredPosition = new Vector2(
            x * cellSize + cellSize / 2,
            -y * cellSize - cellSize / 2
        );

        GridCell gridCell = cell.GetComponent<GridCell>();
        if (gridCell == null)
        {
            gridCell = cell.AddComponent<GridCell>();
        }
        gridCell.Initialize(this, x, y);
        gridCells[x, y] = gridCell;
    }

    public bool TryPlacePixel(Vector2Int gridPos, PixelCell newPixel)
    {
        if (!ValidatePlacement(gridPos, newPixel))
        {
            return false;
        }

        GridCell targetCell = gridCells[gridPos.x, gridPos.y];
        RemoveExistingPixel(gridPos, targetCell);
        PlaceNewPixel(gridPos, targetCell, newPixel);

        return true;
    }

    private bool ValidatePlacement(Vector2Int gridPos, PixelCell newPixel)
    {
        if (!IsPositionValid(gridPos))
        {
            messagePanel?.ShowWarningMessage("Invalid Position", "Cannot place pixel outside the grid!");
            return false;
        }

        if (!firstPixelPlaced)
        {
            if (gridPos != requiredFirstPosition)
            {
                messagePanel?.ShowWarningMessage("Invalid First Pixel", "The first pixel must be placed at the bottom-left corner!");
                return false;
            }
            firstPixelPlaced = true;
        }
        else
        {
            bool isReplacing = placedPixels.ContainsKey(gridPos);
            if (!isReplacing && !HasAdjacentPixel(gridPos))
            {
                messagePanel?.ShowWarningMessage("Invalid Placement", "Pixels must be placed adjacent to existing pixels!");
                return false;
            }
        }

        return true;
    }

    private void RemoveExistingPixel(Vector2Int gridPos, GridCell targetCell)
    {
        if (targetCell.currentPixel != null)
        {
            if (placedPixels.ContainsKey(gridPos))
            {
                placedPixels.Remove(gridPos);
            }

            PixelType oldType = targetCell.currentPixel.pixelType;
            if (pixelCounts.ContainsKey(oldType))
            {
                pixelCounts[oldType]--;
                if (pixelCounts[oldType] <= 0)
                {
                    pixelCounts.Remove(oldType);
                }
            }

            OnPixelRemoved?.Invoke(gridPos, targetCell.currentPixel);
            targetCell.currentPixel.ReturnToInventory(true);
            targetCell.currentPixel = null;
        }
    }

    private void PlaceNewPixel(Vector2Int gridPos, GridCell targetCell, PixelCell newPixel)
    {
        targetCell.currentPixel = newPixel;
        placedPixels.Add(gridPos, newPixel);
        newPixel.PlaceInCell(targetCell.transform);

        PixelType newType = newPixel.pixelType;
        if (!pixelCounts.ContainsKey(newType))
        {
            pixelCounts[newType] = 0;
        }
        pixelCounts[newType]++;

        OnPixelPlaced?.Invoke(gridPos, newPixel);
        UpdateStats();

        string pixelTypeName = newType.ToString();
        messagePanel?.ShowSuccessMessage("Pixel Placed", $"Successfully placed a {pixelTypeName} pixel!");

        LogAllPixels();
    }

    private bool IsPositionValid(Vector2Int position)
    {
        return position.x >= 0 && position.x < gridSize &&
               position.y >= 0 && position.y < gridSize;
    }

    private bool HasAdjacentPixel(Vector2Int position)
    {
        Vector2Int[] directions = {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (var dir in directions)
        {
            Vector2Int neighborPos = position + dir;
            if (placedPixels.ContainsKey(neighborPos))
                return true;
        }
        return false;
    }

    private void UpdateStats()
    {
        if (playerStats == null) return;

        foreach (var kvp in pixelCounts)
        {
            playerStats.UpdateStatsFromPixels(kvp.Key, kvp.Value);
        }
    }

    private void LogAllPixels()
    {
        if (placedPixels.Count == 0)
        {
            Debug.Log("No pixels placed on grid");
            return;
        }

        string logMessage = "=== PLACED PIXELS LOG ===\n";
        logMessage += $"Total pixels: {placedPixels.Count}\n";

        foreach (KeyValuePair<Vector2Int, PixelCell> entry in placedPixels)
        {
            Image pixelImage = entry.Value.GetComponent<Image>();
            Color pixelColor = pixelImage != null ? pixelImage.color : Color.white;

            logMessage += $"[{entry.Key.x},{entry.Key.y}] - " +
                          $"Color: {ColorUtility.ToHtmlStringRGB(pixelColor)} - " +
                          $"GameObject: {entry.Value.name}\n";
        }

        Debug.Log(logMessage);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

