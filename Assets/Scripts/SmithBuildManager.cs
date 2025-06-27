using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using TMPro;

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
    public GameObject inventoryBitSlotPrefab; // Assign the InventoryBitSlot prefab

    [Header("Character Integration")]
    public PowerBitPlayerController playerController; // Assign in inspector
    
    [Header("Grid Prefabs")]
    public GameObject buildGridCellPrefab; // Assign the BuildGridCell prefab for creating new cells

    [Header("UI References")]
    public TextMeshProUGUI gridSizeIndicator; // Assign a text component to show current grid size

    private BuildGridCellUI[,] gridCells;
    private SmithGridStateData originalBuildState; // Store the original state when opening menu
    private List<InventoryBitData> originalInventoryState; // Store original inventory state
    private List<InventoryBitData> addedBitsDuringSession; // Track bits added during this session
    private UnityEngine.UI.GridLayoutGroup gridLayoutGroup;

    // Class to track inventory bit data
    [System.Serializable]
    public class InventoryBitData
    {
        public SerializableBitData bitData;
        public Vector2 originalPosition;
        public Vector3 originalScale;
        public Vector2 originalAnchors;
        public Vector2 originalPivot;

        public InventoryBitData(InventoryBitSlotUI bitSlot)
        {
            this.bitData = new SerializableBitData(bitSlot.bitData);
            this.originalPosition = bitSlot.GetComponent<RectTransform>().anchoredPosition;
            this.originalScale = bitSlot.transform.localScale;
            this.originalAnchors = new Vector2(
                bitSlot.GetComponent<RectTransform>().anchorMin.x,
                bitSlot.GetComponent<RectTransform>().anchorMin.y
            );
            this.originalPivot = bitSlot.GetComponent<RectTransform>().pivot;
        }
        
        // Constructor for loading from saved data
        public InventoryBitData(SerializableBitData bitData, Vector2 position, Vector3 scale, Vector2 anchors, Vector2 pivot)
        {
            this.bitData = bitData;
            this.originalPosition = position;
            this.originalScale = scale;
            this.originalAnchors = anchors;
            this.originalPivot = pivot;
        }
    }
    
    // Serializable version of Bit data
    [System.Serializable]
    public class SerializableBitData
    {
        public string bitName;
        public BitType bitType;
        public Rarity rarity;
        public int damage;
        public float shootingProbability;
        
        public SerializableBitData(Bit bit)
        {
            if (bit != null)
            {
                this.bitName = bit.bitName;
                this.bitType = bit.bitType;
                this.rarity = bit.rarity;
                this.damage = bit.damage;
                this.shootingProbability = bit.shootingProbability;
            }
        }
        
        // Convert back to a Bit object
        public Bit ToBit()
        {
            Bit bit = ScriptableObject.CreateInstance<Bit>();
            bit.bitName = this.bitName;
            bit.bitType = this.bitType;
            bit.rarity = this.rarity;
            bit.damage = this.damage;
            bit.shootingProbability = this.shootingProbability;
            return bit;
        }
    }

    void Start()
    {
        // Debug the buildGridPanel assignment
        Debug.Log($"SmithBuildManager Start: buildGridPanel = {(buildGridPanel != null ? buildGridPanel.name : "NULL")}");
        
        // Get the GridLayoutGroup component
        if (buildGridPanel != null)
        {
            gridLayoutGroup = buildGridPanel.GetComponent<UnityEngine.UI.GridLayoutGroup>();
            if (gridLayoutGroup == null)
            {
                Debug.LogError("BuildGridPanel does not have a GridLayoutGroup component!");
                Debug.LogError($"BuildGridPanel components: {string.Join(", ", buildGridPanel.GetComponents<Component>().Select(c => c.GetType().Name))}");
            }
            else
            {
                Debug.Log($"GridLayoutGroup found! Current constraintCount: {gridLayoutGroup.constraintCount}");
            }
        }
        else
        {
            Debug.LogError("BuildGridPanel is not assigned in SmithBuildManager!");
        }
        
        InitializeGridCells();
        UpdateGridSizeIndicator();
    }
    
    void Update()
    {
        // Only process manual input when game is paused and Smith menu is open
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            GameObject smithCanvas = GameObject.Find("SmithCanvas");
            if (smithCanvas != null && smithCanvas.activeInHierarchy)
            {
                // Only process grid size changes when Smith menu is open
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    ChangeGridSize(2);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    ChangeGridSize(3);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    ChangeGridSize(4);
                }
            }
        }
    }
    
    public void ChangeGridSize(int newSize)
    {
        Debug.Log($"ChangeGridSize called with newSize={newSize}, current gridSize={gridSize}");
        
        if (newSize != 2 && newSize != 3 && newSize != 4)
        {
            Debug.LogWarning($"Invalid grid size: {newSize}. Only 2x2, 3x3, and 4x4 are supported.");
            return;
        }
        
        if (newSize == gridSize)
        {
            Debug.Log($"Grid is already {newSize}x{newSize}");
            return;
        }
        
        Debug.Log($"Changing grid size from {gridSize}x{gridSize} to {newSize}x{newSize}");
        
        // Save current build state
        Debug.Log("Saving current grid state...");
        SaveCurrentGridState();
        
        // Update grid size
        Debug.Log($"Updating gridSize from {gridSize} to {newSize}");
        gridSize = newSize;
        
        // Update GridLayoutGroup constraint count
        if (gridLayoutGroup != null)
        {
            Debug.Log($"Updating GridLayoutGroup constraintCount from {gridLayoutGroup.constraintCount} to {newSize}");
            gridLayoutGroup.constraintCount = newSize;
            Debug.Log($"GridLayoutGroup constraintCount is now: {gridLayoutGroup.constraintCount}");
        }
        else
        {
            Debug.LogError("GridLayoutGroup is null! Cannot update constraint count.");
        }
        
        // Recreate the grid with new size
        Debug.Log("Creating new grid cells...");
        CreateGridCells();
        
        // Try to restore build state to new grid
        Debug.Log("Restoring grid state...");
        RestoreGridState();
        
        // Update UI indicator
        Debug.Log("Updating grid size indicator...");
        UpdateGridSizeIndicator();
        
        Debug.Log($"Grid size change completed! gridSize={gridSize}");
    }
    
    // Debug method to test grid size change
    [ContextMenu("Test Grid Size Change to 3x3")]
    public void TestGridSizeChange()
    {
        Debug.Log("Manual test: Changing grid size to 3x3");
        ChangeGridSize(3);
    }
    
    private void CreateGridCells()
    {
        if (buildGridPanel == null)
        {
            Debug.LogError("BuildGridPanel not assigned!");
            return;
        }
        
        if (buildGridCellPrefab == null)
        {
            Debug.LogError("BuildGridCell prefab not assigned!");
            return;
        }
        
        // Update GridLayoutGroup constraint count first
        if (gridLayoutGroup != null)
        {
            Debug.Log($"Updating GridLayoutGroup constraintCount from {gridLayoutGroup.constraintCount} to {gridSize}");
            gridLayoutGroup.constraintCount = gridSize;
        }
        else
        {
            Debug.LogError("GridLayoutGroup is null! Cannot update constraint count.");
        }
        
        // Clear existing cells
        for (int i = buildGridPanel.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(buildGridPanel.GetChild(i).gameObject);
        }
        
        // Create new grid array
        gridCells = new BuildGridCellUI[gridSize, gridSize];
        
        // Create the required number of cells
        for (int i = 0; i < gridSize * gridSize; i++)
        {
            GameObject cellObj = Instantiate(buildGridCellPrefab, buildGridPanel);
            BuildGridCellUI cellUI = cellObj.GetComponent<BuildGridCellUI>();
            
            if (cellUI != null)
            {
                int x = i % gridSize;
                int y = i / gridSize;
                gridCells[x, y] = cellUI;
                cellObj.name = $"BuildGridCell ({x},{y})";
            }
            else
            {
                Debug.LogError("BuildGridCell prefab does not have BuildGridCellUI component!");
            }
        }
        
        Debug.Log($"Created {gridSize * gridSize} grid cells with GridLayoutGroup constraintCount={gridLayoutGroup?.constraintCount}");
    }
    
    private SmithGridStateData currentGridState;
    
    private void SaveCurrentGridState()
    {
        if (gridCells == null) return;
        
        currentGridState = new SmithGridStateData(gridSize);
        
        // Get actual array dimensions to avoid IndexOutOfRangeException
        int actualWidth = gridCells.GetLength(0);
        int actualHeight = gridCells.GetLength(1);
        
        // Collect all cell data
        for (int y = 0; y < actualHeight; y++)
        {
            for (int x = 0; x < actualWidth; x++)
            {
                BuildGridCellUI cell = gridCells[x, y];
                if (cell != null)
                {
                    InventoryBitSlotUI[] bitSlots = cell.GetComponentsInChildren<InventoryBitSlotUI>();
                    
                    // Take the first bit slot if any exist
                    if (bitSlots.Length > 0 && bitSlots[0].bitData != null)
                    {
                        SmithCellData cellData = new SmithCellData(x, y, bitSlots[0].bitData);
                        currentGridState.cells.Add(cellData);
                    }
                }
            }
        }
        Debug.Log($"Saved grid state with {currentGridState.cells.Count} cells (actual dimensions: {actualWidth}x{actualHeight})");
    }
    
    private void RestoreGridState()
    {
        if (currentGridState == null || gridCells == null) return;
        
        // Clear the new grid first
        ClearGrid();
        
        // Restore bits that fit in the new grid size
        foreach (var cell in currentGridState.cells)
        {
            if (cell.x >= 0 && cell.x < gridSize && cell.y >= 0 && cell.y < gridSize)
            {
                BuildGridCellUI gridCell = gridCells[cell.x, cell.y];
                
                // Create a new InventoryBitSlotUI from prefab
                if (inventoryBitSlotPrefab != null && gridCell != null)
                {
                    GameObject bitSlotObj = Instantiate(inventoryBitSlotPrefab, gridCell.transform);
                    InventoryBitSlotUI bitSlot = bitSlotObj.GetComponent<InventoryBitSlotUI>();
                    
                    if (bitSlot != null)
                    {
                        // Create a Bit object from the cell data
                        Bit bitData = CreateBitFromCellData(cell);
                        
                        // Set the bit data and sprite directly
                        bitSlot.bitData = bitData;
                        if (bitSlot.iconImage != null)
                        {
                            bitSlot.iconImage.sprite = bitData.GetSprite();
                        }
                        
                        // Position the slot in the center of the grid cell
                        RectTransform slotRect = bitSlot.GetComponent<RectTransform>();
                        slotRect.anchorMin = new Vector2(0.5f, 0.5f);
                        slotRect.anchorMax = new Vector2(0.5f, 0.5f);
                        slotRect.pivot = new Vector2(0.5f, 0.5f);
                        slotRect.anchoredPosition = Vector2.zero;
                        
                        // Tell the grid cell it's occupied
                        gridCell.SetCurrentBitSlot(bitSlot);
                    }
                }
            }
        }
    }
    
    private void UpdateGridSizeIndicator()
    {
        if (gridSizeIndicator != null)
        {
            gridSizeIndicator.text = $"Grid: {gridSize}x{gridSize} (Press 1 for 2x2, 2 for 3x3, 3 for 4x4)";
        }
    }

    private void InitializeGridCells()
    {
        // Use the new CreateGridCells method for initial setup
        CreateGridCells();
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
                InventoryBitSlotUI[] bitSlots = cell.GetComponentsInChildren<InventoryBitSlotUI>();
                
                // Take the first bit slot if any exist
                if (bitSlots.Length > 0 && bitSlots[0].bitData != null)
                {
                    SmithCellData cellData = new SmithCellData(x, y, bitSlots[0].bitData);
                    gridState.cells.Add(cellData);
                    Debug.Log($"Cell [{x},{y}]: {bitSlots[0].bitData.bitName} ({bitSlots[0].bitData.rarity})");
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
        
        // Save the current inventory state to preserve bits that were dragged out
        SaveCurrentInventoryState();
        
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
        Debug.Log("=== CLOSING SMITH MENU - REVERTING CHANGES ===");
        
        // Revert to the original build state
        if (originalBuildState != null)
        {
            RevertToOriginalState();
        }
        
        // Revert inventory to original state
        RevertInventoryToOriginalState();
        
        // Find and hide the SmithCanvas
        GameObject smithCanvas = GameObject.Find("SmithCanvas");
        if (smithCanvas != null)
        {
            smithCanvas.SetActive(false);
        }
        
        // Resume the game when closing smith menu
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.ResumeGame();
        }
        
        Debug.Log("Smith menu closed - changes reverted and game resumed");
    }
    
    public void LoadCurrentBuild()
    {
        Debug.Log("=== LOADING CURRENT BUILD INTO SMITH UI ===");
        
        // Ensure the grid is properly sized for the current gridSize
        if (gridCells == null || gridCells.GetLength(0) != gridSize || gridCells.GetLength(1) != gridSize)
        {
            Debug.Log($"Grid size mismatch detected. Current gridSize: {gridSize}, actual grid dimensions: {(gridCells != null ? $"{gridCells.GetLength(0)}x{gridCells.GetLength(1)}" : "null")}");
            Debug.Log("Recreating grid to match current gridSize...");
            CreateGridCells();
        }
        
        // Save the original state before making any changes
        SaveOriginalBuildState();
        SaveOriginalInventoryState();
        
        // Load the saved build data
        string buildFilePath = System.IO.Path.Combine(Application.persistentDataPath, "smith_build.json");
        if (!System.IO.File.Exists(buildFilePath))
        {
            Debug.Log("No saved build found. Starting with empty grid.");
            ClearGrid();
            return;
        }

        try
        {
            string json = System.IO.File.ReadAllText(buildFilePath);
            SmithGridStateData gridState = JsonUtility.FromJson<SmithGridStateData>(json);
            
            if (gridState != null)
            {
                LoadBuildIntoGrid(gridState);
                Debug.Log($"Loaded build with {gridState.cells.Count} bits into Smith UI");
            }
            else
            {
                Debug.LogWarning("Failed to parse saved build data. Starting with empty grid.");
                ClearGrid();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading build: {e.Message}. Starting with empty grid.");
            ClearGrid();
        }
        
        // Load the saved inventory state
        LoadSavedInventoryState();
    }
    
    // Save the original build state when opening the menu
    private void SaveOriginalBuildState()
    {
        if (gridCells == null)
        {
            Debug.LogWarning("gridCells is null in SaveOriginalBuildState()");
            originalBuildState = new SmithGridStateData(gridSize);
            return;
        }
        
        originalBuildState = new SmithGridStateData(gridSize);
        
        // Get actual array dimensions to avoid IndexOutOfRangeException
        int actualWidth = gridCells.GetLength(0);
        int actualHeight = gridCells.GetLength(1);
        
        // Collect current build state
        for (int y = 0; y < actualHeight; y++)
        {
            for (int x = 0; x < actualWidth; x++)
            {
                BuildGridCellUI cell = gridCells[x, y];
                if (cell != null)
                {
                    InventoryBitSlotUI[] bitSlots = cell.GetComponentsInChildren<InventoryBitSlotUI>();
                    
                    if (bitSlots.Length > 0 && bitSlots[0].bitData != null)
                    {
                        SmithCellData cellData = new SmithCellData(x, y, bitSlots[0].bitData);
                        originalBuildState.cells.Add(cellData);
                    }
                }
            }
        }
        
        Debug.Log($"Saved original build state with {originalBuildState.cells.Count} bits (actual dimensions: {actualWidth}x{actualHeight})");
    }
    
    // Save the original inventory state when opening the menu
    private void SaveOriginalInventoryState()
    {
        originalInventoryState = new List<InventoryBitData>();
        addedBitsDuringSession = new List<InventoryBitData>();
        
        // Find all inventory bit slots
        Transform inventoryContent = FindInventoryContent();
        if (inventoryContent != null)
        {
            InventoryBitSlotUI[] inventorySlots = inventoryContent.GetComponentsInChildren<InventoryBitSlotUI>();
            
            foreach (var slot in inventorySlots)
            {
                if (slot.bitData != null)
                {
                    InventoryBitData bitData = new InventoryBitData(slot);
                    originalInventoryState.Add(bitData);
                }
            }
        }
        
        Debug.Log($"Saved original inventory state with {originalInventoryState.Count} bits");
    }
    
    // Revert to the original build state
    private void RevertToOriginalState()
    {
        Debug.Log("Reverting to original build state...");
        
        // Clear current grid
        ClearGrid();
        
        // Load the original state back into the grid
        if (originalBuildState != null)
        {
            LoadBuildIntoGrid(originalBuildState);
            Debug.Log($"Reverted to original build with {originalBuildState.cells.Count} bits");
        }
        
        // Clear the original state reference
        originalBuildState = null;
    }
    
    // Revert inventory to original state
    private void RevertInventoryToOriginalState()
    {
        Debug.Log("Reverting inventory to original state...");
        
        Transform inventoryContent = FindInventoryContent();
        if (inventoryContent == null)
        {
            Debug.LogWarning("Could not find inventory content for reversion!");
            return;
        }
        
        // Remove all bits that were added during this session
        InventoryBitSlotUI[] currentInventorySlots = inventoryContent.GetComponentsInChildren<InventoryBitSlotUI>();
        foreach (var slot in currentInventorySlots)
        {
            if (slot.bitData != null)
            {
                // Check if this bit was in the original inventory
                bool wasInOriginal = originalInventoryState.Any(original => 
                    original.bitData.bitName == slot.bitData.bitName &&
                    original.bitData.rarity == slot.bitData.rarity);
                
                if (!wasInOriginal)
                {
                    // This bit was added during the session, remove it
                    Debug.Log($"Removing added bit: {slot.bitData.bitName}");
                    Destroy(slot.gameObject);
                }
            }
        }
        
        // Restore original positions and properties for remaining bits
        foreach (var originalBit in originalInventoryState)
        {
            // Find the corresponding slot in current inventory
            InventoryBitSlotUI[] currentSlots = inventoryContent.GetComponentsInChildren<InventoryBitSlotUI>();
            foreach (var currentSlot in currentSlots)
            {
                if (currentSlot.bitData != null &&
                    currentSlot.bitData.bitName == originalBit.bitData.bitName &&
                    currentSlot.bitData.rarity == originalBit.bitData.rarity)
                {
                    // Restore original position and properties
                    RectTransform rectTransform = currentSlot.GetComponent<RectTransform>();
                    rectTransform.anchoredPosition = originalBit.originalPosition;
                    currentSlot.transform.localScale = originalBit.originalScale;
                    rectTransform.anchorMin = originalBit.originalAnchors;
                    rectTransform.anchorMax = originalBit.originalAnchors;
                    rectTransform.pivot = originalBit.originalPivot;
                    
                    break;
                }
            }
        }
        
        Debug.Log($"Reverted inventory to original state with {originalInventoryState.Count} bits");
        
        // Clear the state references
        originalInventoryState = null;
        addedBitsDuringSession = null;
    }
    
    // Find the inventory content area
    private Transform FindInventoryContent()
    {
        // Look for the inventory content in the scene
        Transform inventoryContent = GameObject.Find("InventoryContent")?.transform;
        if (inventoryContent == null)
        {
            // Alternative: look for SmithInventoryTestPopulator and get its inventoryContent
            SmithInventoryTestPopulator populator = FindObjectOfType<SmithInventoryTestPopulator>();
            if (populator != null)
            {
                inventoryContent = populator.inventoryContent;
            }
        }
        return inventoryContent;
    }
    
    private void LoadBuildIntoGrid(SmithGridStateData gridState)
    {
        // Clear the grid first
        ClearGrid();
        
        if (gridCells == null)
        {
            Debug.LogWarning("gridCells is null in LoadBuildIntoGrid()");
            return;
        }
        
        int actualWidth = gridCells.GetLength(0);
        int actualHeight = gridCells.GetLength(1);
        
        // Load each bit into the grid
        foreach (var cell in gridState.cells)
        {
            // Check bounds against both gridSize and actual array dimensions
            if (cell.x >= 0 && cell.x < gridSize && cell.y >= 0 && cell.y < gridSize &&
                cell.x < actualWidth && cell.y < actualHeight)
            {
                BuildGridCellUI gridCell = gridCells[cell.x, cell.y];
                
                if (gridCell != null)
                {
                    // Create a new InventoryBitSlotUI from prefab
                    if (inventoryBitSlotPrefab != null)
                    {
                        GameObject bitSlotObj = Instantiate(inventoryBitSlotPrefab, gridCell.transform);
                        InventoryBitSlotUI bitSlot = bitSlotObj.GetComponent<InventoryBitSlotUI>();
                        
                        if (bitSlot != null)
                        {
                            // Create a Bit object from the cell data
                            Bit bitData = CreateBitFromCellData(cell);
                            
                            // Set the bit data and sprite directly
                            bitSlot.bitData = bitData;
                            if (bitSlot.iconImage != null)
                            {
                                bitSlot.iconImage.sprite = bitData.GetSprite();
                            }
                            
                            // Position the slot in the center of the grid cell
                            RectTransform slotRect = bitSlot.GetComponent<RectTransform>();
                            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
                            slotRect.anchorMax = new Vector2(0.5f, 0.5f);
                            slotRect.pivot = new Vector2(0.5f, 0.5f);
                            slotRect.anchoredPosition = Vector2.zero;
                            
                            // Tell the grid cell it's occupied
                            gridCell.SetCurrentBitSlot(bitSlot);
                            
                            Debug.Log($"Loaded {cell.bitName} into grid position [{cell.x},{cell.y}]");
                        }
                    }
                    else
                    {
                        Debug.LogError("InventoryBitSlot prefab not assigned!");
                    }
                }
            }
        }
        Debug.Log($"LoadBuildIntoGrid completed (grid dimensions: {actualWidth}x{actualHeight})");
    }
    
    private Bit CreateBitFromCellData(SmithCellData cell)
    {
        // Create a temporary Bit object from the saved data
        // This is a simplified approach - you might want to use BitManager to get the actual Bit assets
        Bit bit = ScriptableObject.CreateInstance<Bit>();
        bit.bitName = cell.bitName;
        bit.bitType = cell.bitType;
        bit.rarity = cell.rarity;
        bit.damage = cell.damage;
        bit.shootingProbability = cell.shootingProbability;
        
        return bit;
    }
    
    private void ClearGrid()
    {
        // Clear all grid cells by destroying any InventoryBitSlotUI children
        if (gridCells == null)
        {
            Debug.LogWarning("gridCells is null in ClearGrid()");
            return;
        }
        
        int actualWidth = gridCells.GetLength(0);
        int actualHeight = gridCells.GetLength(1);
        
        for (int y = 0; y < actualHeight; y++)
        {
            for (int x = 0; x < actualWidth; x++)
            {
                BuildGridCellUI gridCell = gridCells[x, y];
                if (gridCell != null)
                {
                    InventoryBitSlotUI[] bitSlots = gridCell.GetComponentsInChildren<InventoryBitSlotUI>();
                    
                    foreach (var bitSlot in bitSlots)
                    {
                        Destroy(bitSlot.gameObject);
                    }
                    
                    // Clear the grid cell's tracking
                    gridCell.SetCurrentBitSlot(null);
                }
            }
        }
        Debug.Log($"Grid cleared (actual dimensions: {actualWidth}x{actualHeight})");
    }

    // Save the current inventory state to preserve bits that were dragged out
    private void SaveCurrentInventoryState()
    {
        Debug.Log("=== SAVING CURRENT INVENTORY STATE ===");
        
        List<InventoryBitData> currentInventoryState = new List<InventoryBitData>();
        
        // Find all inventory bit slots
        Transform inventoryContent = FindInventoryContent();
        if (inventoryContent != null)
        {
            InventoryBitSlotUI[] inventorySlots = inventoryContent.GetComponentsInChildren<InventoryBitSlotUI>();
            
            foreach (var slot in inventorySlots)
            {
                if (slot.bitData != null)
                {
                    InventoryBitData bitData = new InventoryBitData(slot);
                    currentInventoryState.Add(bitData);
                    Debug.Log($"Saving inventory bit: {slot.bitData.bitName} (Type: {slot.bitData.bitType}, Rarity: {slot.bitData.rarity})");
                }
            }
        }
        
        // Convert to JSON and save
        string json = JsonUtility.ToJson(new InventorySaveData(currentInventoryState), true);
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, "smith_inventory.json");
        System.IO.File.WriteAllText(filePath, json);
        
        Debug.Log($"Inventory saved to: {filePath}");
        Debug.Log($"Total inventory bits: {currentInventoryState.Count}");
        Debug.Log($"JSON content: {json}");
    }
    
    // Helper class for saving inventory data
    [System.Serializable]
    public class InventorySaveData
    {
        public List<InventoryBitData> bits = new List<InventoryBitData>();
        
        public InventorySaveData(List<InventoryBitData> bits)
        {
            this.bits = bits;
        }
    }

    // Load the saved inventory state
    private void LoadSavedInventoryState()
    {
        Debug.Log("=== LOADING SAVED INVENTORY STATE ===");
        
        string inventoryFilePath = System.IO.Path.Combine(Application.persistentDataPath, "smith_inventory.json");
        if (!System.IO.File.Exists(inventoryFilePath))
        {
            Debug.Log("No saved inventory found.");
            return;
        }

        try
        {
            string json = System.IO.File.ReadAllText(inventoryFilePath);
            Debug.Log($"Loading inventory JSON: {json}");
            
            InventorySaveData inventoryData = JsonUtility.FromJson<InventorySaveData>(json);
            
            if (inventoryData != null && inventoryData.bits.Count > 0)
            {
                // Clear current inventory first
                Transform inventoryContent = FindInventoryContent();
                if (inventoryContent != null)
                {
                    InventoryBitSlotUI[] currentSlots = inventoryContent.GetComponentsInChildren<InventoryBitSlotUI>();
                    foreach (var slot in currentSlots)
                    {
                        Destroy(slot.gameObject);
                    }
                }
                
                // Restore saved inventory bits
                foreach (var savedBit in inventoryData.bits)
                {
                    RestoreInventoryBit(savedBit);
                }
                
                Debug.Log($"Loaded inventory with {inventoryData.bits.Count} bits");
            }
            else
            {
                Debug.LogWarning("Inventory data is null or empty!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading inventory: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
        }
    }
    
    // Restore a single inventory bit
    private void RestoreInventoryBit(InventoryBitData savedBit)
    {
        Transform inventoryContent = FindInventoryContent();
        if (inventoryContent == null || inventoryBitSlotPrefab == null)
        {
            Debug.LogWarning("Cannot restore inventory bit - missing inventory content or prefab");
            return;
        }
        
        if (savedBit.bitData == null)
        {
            Debug.LogWarning("Cannot restore inventory bit - saved bit data is null");
            return;
        }
        
        // Create new inventory slot
        GameObject bitSlotObj = Instantiate(inventoryBitSlotPrefab, inventoryContent);
        InventoryBitSlotUI bitSlot = bitSlotObj.GetComponent<InventoryBitSlotUI>();
        
        if (bitSlot != null)
        {
            // Convert SerializableBitData back to Bit object
            Bit restoredBit = savedBit.bitData.ToBit();
            
            // Set the bit data
            bitSlot.bitData = restoredBit;
            if (bitSlot.iconImage != null)
            {
                bitSlot.iconImage.sprite = restoredBit.GetSprite();
            }
            
            // Restore position and properties
            RectTransform rectTransform = bitSlot.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = savedBit.originalPosition;
            bitSlot.transform.localScale = savedBit.originalScale;
            rectTransform.anchorMin = savedBit.originalAnchors;
            rectTransform.anchorMax = savedBit.originalAnchors;
            rectTransform.pivot = savedBit.originalPivot;
            
            Debug.Log($"Restored inventory bit: {restoredBit.bitName} (Type: {restoredBit.bitType}, Rarity: {restoredBit.rarity})");
        }
        else
        {
            Debug.LogError("Failed to get InventoryBitSlotUI component from instantiated prefab!");
        }
    }

    // Special method for Core upgrades - upgrades grid size while preserving existing build
    public void UpgradeGridSizeAndSaveBuild(int newSize)
    {
        Debug.Log($"=== CORE UPGRADE: Upgrading grid size to {newSize}x{newSize} ===");
        
        // Load the current saved build first
        string buildFilePath = System.IO.Path.Combine(Application.persistentDataPath, "smith_build.json");
        SmithGridStateData existingBuild = null;
        
        if (System.IO.File.Exists(buildFilePath))
        {
            try
            {
                string json = System.IO.File.ReadAllText(buildFilePath);
                existingBuild = JsonUtility.FromJson<SmithGridStateData>(json);
                Debug.Log($"Loaded existing build with {existingBuild?.cells?.Count ?? 0} bits");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error loading existing build: {e.Message}");
            }
        }
        
        // Update the grid size
        gridSize = newSize;
        
        // Create the new build data with upgraded grid size
        SmithGridStateData upgradedBuild = new SmithGridStateData(newSize);
        
        // Preserve existing bits that fit in the new grid
        if (existingBuild != null && existingBuild.cells != null)
        {
            foreach (var cell in existingBuild.cells)
            {
                // Only keep bits that fit within the new grid size
                if (cell.x >= 0 && cell.x < newSize && cell.y >= 0 && cell.y < newSize)
                {
                    upgradedBuild.cells.Add(cell);
                    Debug.Log($"Preserved bit: {cell.bitName} at [{cell.x},{cell.y}]");
                }
                else
                {
                    Debug.Log($"Removed bit {cell.bitName} - position [{cell.x},{cell.y}] doesn't fit in {newSize}x{newSize} grid");
                }
            }
        }
        
        // Save the upgraded build
        string upgradedJson = JsonUtility.ToJson(upgradedBuild, true);
        System.IO.File.WriteAllText(buildFilePath, upgradedJson);
        
        Debug.Log($"Grid upgrade complete! Saved build with {upgradedBuild.cells.Count} bits to {newSize}x{newSize} grid");
        
        // Apply the build to the player character
        if (playerController != null)
        {
            playerController.LoadSmithBuild(upgradedBuild);
            Debug.Log("Upgraded build applied to player character");
        }
        else
        {
            Debug.LogWarning("PlayerController not assigned! Cannot apply upgraded build to character.");
        }
    }
} 