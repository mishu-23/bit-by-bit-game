using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class BitCollectionManager : MonoBehaviour
{
    public static BitCollectionManager Instance { get; private set; }
    
    [Header("References")]
    public PowerBitPlayerController playerController;
    public GameObject bitDropPrefab; // Assign the BitDrop prefab here
    
    private string buildFilePath;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            buildFilePath = Path.Combine(Application.persistentDataPath, "smith_build.json");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        if (playerController == null)
        {
            playerController = FindObjectOfType<PowerBitPlayerController>();
        }
        
        // Assign the BitDrop prefab to the static reference
        if (bitDropPrefab != null)
        {
            BitDrop.BitDropPrefab = bitDropPrefab;
            Debug.Log("BitDrop prefab assigned successfully!");
        }
        else
        {
            Debug.LogWarning("BitDrop prefab not assigned in BitCollectionManager! Please assign it in the inspector.");
        }
    }
    
    public bool CollectBit(Bit bitData)
    {
        if (bitData == null)
        {
            Debug.LogError("Cannot collect null bit!");
            return false;
        }

        // Load current build
        SmithGridStateData currentBuild = LoadCurrentBuild();
        if (currentBuild == null)
        {
            Debug.LogError("Failed to load current build!");
            return false;
        }

        // Check if build is full
        int maxBits = currentBuild.gridSize * currentBuild.gridSize;
        if (currentBuild.cells.Count >= maxBits)
        {
            Debug.Log($"Build is full! Cannot add {bitData.bitName}. Build has {currentBuild.cells.Count}/{maxBits} bits.");
            return false;
        }

        // Find a random empty position
        Vector2Int emptyPosition = FindRandomEmptyPosition(currentBuild);
        if (emptyPosition.x == -1)
        {
            Debug.LogError("No empty position found in build!");
            return false;
        }

        // Create new cell data
        SmithCellData newCell = new SmithCellData(emptyPosition.x, emptyPosition.y, bitData);

        // Add to build
        currentBuild.cells.Add(newCell);

        // Save updated build
        SaveBuild(currentBuild);

        // Load build into player
        if (playerController != null)
        {
            playerController.LoadSmithBuild(currentBuild);
        }

        Debug.Log($"Added {bitData.bitName} to build at position ({emptyPosition.x}, {emptyPosition.y})");
        return true;
    }
    
    private SmithGridStateData LoadCurrentBuild()
    {
        if (File.Exists(buildFilePath))
        {
            try
            {
                string json = File.ReadAllText(buildFilePath);
                SmithGridStateData build = JsonUtility.FromJson<SmithGridStateData>(json);
                return build;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error loading build: {e.Message}");
            }
        }
        
        // Create new build if file doesn't exist or is corrupted
        // Check current grid size from SmithBuildManager
        SmithBuildManager smithManager = FindObjectOfType<SmithBuildManager>();
        int currentGridSize = smithManager != null ? smithManager.gridSize : 2; // Default to 2x2 if no manager found
        return new SmithGridStateData(currentGridSize);
    }
    
    private Vector2Int FindRandomEmptyPosition(SmithGridStateData build)
    {
        List<Vector2Int> emptyPositions = new List<Vector2Int>();
        
        // Find all empty positions
        for (int y = 0; y < build.gridSize; y++)
        {
            for (int x = 0; x < build.gridSize; x++)
            {
                bool isOccupied = false;
                
                // Check if this position is already occupied
                foreach (var cell in build.cells)
                {
                    if (cell.x == x && cell.y == y)
                    {
                        isOccupied = true;
                        break;
                    }
                }
                
                if (!isOccupied)
                {
                    emptyPositions.Add(new Vector2Int(x, y));
                }
            }
        }
        
        // Return random empty position or (-1, -1) if no space available
        if (emptyPositions.Count > 0)
        {
            int randomIndex = Random.Range(0, emptyPositions.Count);
            return emptyPositions[randomIndex];
        }
        
        return new Vector2Int(-1, -1); // No space available
    }
    
    private void AddBitToBuild(SmithGridStateData build, Vector2Int position, Bit bitData)
    {
        SmithCellData newCell = new SmithCellData(position.x, position.y, bitData);
        build.cells.Add(newCell);
    }
    
    private void SaveBuild(SmithGridStateData build)
    {
        try
        {
            string json = JsonUtility.ToJson(build, true);
            File.WriteAllText(buildFilePath, json);
            Debug.Log($"Build saved successfully with {build.cells.Count} bits");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving build: {e.Message}");
        }
    }
    
    // Public method to check if build has space
    public bool HasEmptySpace()
    {
        SmithGridStateData build = LoadCurrentBuild();
        Vector2Int emptyPos = FindRandomEmptyPosition(build);
        return emptyPos.x >= 0 && emptyPos.y >= 0;
    }
    
    // Public method to get current build info
    public string GetBuildInfo()
    {
        SmithGridStateData build = LoadCurrentBuild();
        int totalSlots = build.gridSize * build.gridSize;
        int occupiedSlots = build.cells.Count;
        int emptySlots = totalSlots - occupiedSlots;
        
        return $"Build: {occupiedSlots}/{totalSlots} slots occupied ({emptySlots} empty)";
    }
} 