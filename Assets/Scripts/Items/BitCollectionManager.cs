using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BitByBit.Core;
namespace BitByBit.Items
{
    public interface IBuildPersistenceService
    {
        SmithGridStateData LoadBuild(string fileName);
        bool SaveBuild(SmithGridStateData build, string fileName);
        bool BuildFileExists(string fileName);
    }
    public class FileBuildPersistenceService : IBuildPersistenceService
    {
        private readonly string basePath;
        public FileBuildPersistenceService()
        {
            basePath = Application.persistentDataPath;
        }
        public SmithGridStateData LoadBuild(string fileName)
        {
            string filePath = Path.Combine(basePath, fileName);
            if (!File.Exists(filePath))
                return null;
            try
            {
                string json = File.ReadAllText(filePath);
                return JsonUtility.FromJson<SmithGridStateData>(json);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load build from {fileName}: {ex.Message}");
                return null;
            }
        }
        public bool SaveBuild(SmithGridStateData build, string fileName)
        {
            string filePath = Path.Combine(basePath, fileName);
            try
            {
                string json = JsonUtility.ToJson(build, true);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save build to {fileName}: {ex.Message}");
                return false;
            }
        }
        public bool BuildFileExists(string fileName)
        {
            string filePath = Path.Combine(basePath, fileName);
            return File.Exists(filePath);
        }
    }
public class BitCollectionManager : MonoBehaviour
{
    [Header("References")]
        [SerializeField] private PowerBitPlayerController playerController;
        [SerializeField] private GameObject bitDropPrefab;
        [Header("Settings")]
        [SerializeField] private string buildFileName = "smith_build.json";
        [SerializeField] private int defaultGridSize = 2;
        private IBuildPersistenceService persistenceService;
        private HashSet<Vector2Int> occupiedPositions;
        private SmithGridStateData cachedBuild;
        private bool buildCacheValid;
        private bool isUpdatingPlayer;
        public static BitCollectionManager Instance { get; private set; }
    private void Awake()
    {
            InitializeSingleton();
            InitializeServices();
            InitializeCache();
        }
        private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
            InitializeComponents();
            ConfigureBitDropPrefab();
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C) && Input.GetKey(KeyCode.LeftControl))
            {
                Debug.Log("=== MANUAL BUILD CLEANUP TRIGGERED ===");
                CleanupBuildData();
            }
            if (Input.GetKeyDown(KeyCode.T) && Input.GetKey(KeyCode.LeftControl))
            {
                Debug.Log("=== COORDINATE SYSTEM TEST ===");
                TestCoordinateSystem();
            }
        }
        private void InitializeServices()
        {
            persistenceService = new FileBuildPersistenceService();
        }
        private void InitializeCache()
        {
            occupiedPositions = new HashSet<Vector2Int>();
            buildCacheValid = false;
        }
        private void InitializeComponents()
        {
            if (playerController == null)
            {
                if (GameReferences.Instance != null && GameReferences.Instance.PlayerController != null)
                {
                    playerController = GameReferences.Instance.PlayerController;
                    Debug.Log("BitCollectionManager: Found PowerBitPlayerController via GameReferences");
                }
                else
                {
                    playerController = FindObjectOfType<PowerBitPlayerController>();
                    if (playerController != null)
                    {
                        Debug.LogWarning("BitCollectionManager: Found PowerBitPlayerController via fallback method. Please ensure GameReferences is properly configured.");
                    }
                    else
                    {
                        Debug.LogWarning("BitCollectionManager: PowerBitPlayerController not found!");
                    }
                }
            }
        }
        private void ConfigureBitDropPrefab()
        {
            if (bitDropPrefab != null)
            {
                BitDrop.PrefabReference = bitDropPrefab;
                Debug.Log("BitCollectionManager: BitDrop prefab configured successfully");
            }
            else
            {
                Debug.LogWarning("BitCollectionManager: BitDrop prefab not assigned! Please assign it in the inspector.");
            }
        }
        public bool CollectBit(Bit bitData)
        {
            if (!ValidateBitData(bitData))
                return false;
            var currentBuild = GetCurrentBuild();
            if (currentBuild == null)
            {
                Debug.LogError("BitCollectionManager: Failed to load current build!");
                return false;
            }
            if (!HasAvailableSpace(currentBuild))
            {
                Debug.Log($"BitCollectionManager: Build is full! Cannot add {bitData.BitName}");
                return false;
            }
            var emptyPosition = FindOptimalEmptyPosition(currentBuild);
            if (!emptyPosition.HasValue)
            {
                Debug.LogError("BitCollectionManager: No empty position found!");
            return false;
            }
            return ExecuteCollection(currentBuild, emptyPosition.Value, bitData);
        }
        public bool HasEmptySpace()
        {
            var build = GetCurrentBuild();
            return build != null && HasAvailableSpace(build);
        }
        public BuildInfo GetBuildInfo()
        {
            var build = GetCurrentBuild();
            if (build == null)
                return new BuildInfo(0, 0, 0);
            int totalSlots = build.gridSize * build.gridSize;
            int occupiedSlots = build.cells.Count;
            int emptySlots = totalSlots - occupiedSlots;
            return new BuildInfo(totalSlots, occupiedSlots, emptySlots);
        }
        public void InvalidateCache()
        {
            buildCacheValid = false;
            occupiedPositions.Clear();
        }
        public bool CleanupBuildData()
        {
            Debug.Log("BitCollectionManager: Starting comprehensive build data cleanup...");
            var build = persistenceService.LoadBuild(buildFileName);
            if (build?.cells == null)
            {
                Debug.Log("BitCollectionManager: No build data to clean up");
        return true;
    }
            Debug.Log($"BitCollectionManager: Before cleanup - {build.cells.Count} total bits");
            var positionGroups = build.cells
                .GroupBy(cell => new Vector2Int(cell.x, cell.y))
                .ToList();
            var cleanedCells = new List<SmithCellData>();
            int duplicatesRemoved = 0;
            foreach (var group in positionGroups)
            {
                var position = group.Key;
                var cellsAtPosition = group.ToList();
                if (cellsAtPosition.Count == 1)
                {
                    cleanedCells.Add(cellsAtPosition[0]);
                }
                else
                {
                    Debug.LogWarning($"BitCollectionManager: Found {cellsAtPosition.Count} bits at position ({position.x}, {position.y})");
                    var resolvedCell = cellsAtPosition
                        .OrderByDescending(cell => cell.bitType == BitType.PowerBit ? 1 : 0)
                        .ThenByDescending(cell => (int)cell.rarity)
                        .First();
                    cleanedCells.Add(resolvedCell);
                    duplicatesRemoved += cellsAtPosition.Count - 1;
                    Debug.Log($"BitCollectionManager: Kept {resolvedCell.bitName} ({resolvedCell.rarity}) at ({position.x}, {position.y}), removed {cellsAtPosition.Count - 1} duplicates");
                    foreach (var removedCell in cellsAtPosition.Skip(1))
                    {
                        Debug.Log($"  - Removed: {removedCell.bitName} ({removedCell.rarity})");
                    }
                }
            }
            Debug.Log($"BitCollectionManager: After cleanup - {cleanedCells.Count} bits, removed {duplicatesRemoved} duplicates");
            build.cells = cleanedCells;
            bool saveSuccess = persistenceService.SaveBuild(build, buildFileName);
            if (saveSuccess)
            {
                InvalidateCache();
                if (playerController != null)
                {
                    Debug.Log("BitCollectionManager: Updating player with cleaned build data");
                    UpdatePlayerBuild(build);
                }
                Debug.Log("BitCollectionManager: Build data cleanup completed successfully");
                return true;
            }
            else
            {
                Debug.LogError("BitCollectionManager: Failed to save cleaned build data!");
                return false;
            }
        }
        private bool ValidateBitData(Bit bitData)
        {
            if (bitData == null)
            {
                Debug.LogError("BitCollectionManager: Cannot collect null bit!");
                return false;
            }
            return true;
        }
        private SmithGridStateData GetCurrentBuild()
        {
            if (buildCacheValid && cachedBuild != null)
                return cachedBuild;
            cachedBuild = LoadOrCreateBuild();
            if (cachedBuild != null)
            {
                RefreshOccupiedPositions();
                buildCacheValid = true;
            }
            return cachedBuild;
        }
        private SmithGridStateData LoadOrCreateBuild()
        {
            var build = persistenceService.LoadBuild(buildFileName);
            if (build != null)
                return build;
            int gridSize = GetCurrentGridSize();
            Debug.Log($"BitCollectionManager: Creating new build with {gridSize}x{gridSize} grid");
            return new SmithGridStateData(gridSize);
        }
        private int GetCurrentGridSize()
        {
            if (GameReferences.Instance != null && GameReferences.Instance.SmithBuildManager != null)
            {
                return GameReferences.Instance.SmithBuildManager.gridSize;
            }
            var smithManager = FindObjectOfType<SmithBuildManager>();
            if (smithManager != null)
            {
                Debug.LogWarning("BitCollectionManager: Found SmithBuildManager via fallback method. Please ensure GameReferences is properly configured.");
                return smithManager.gridSize;
            }
            return defaultGridSize;
        }
        private bool HasAvailableSpace(SmithGridStateData build)
        {
            int maxBits = build.gridSize * build.gridSize;
            return build.cells.Count < maxBits;
        }
        private Vector2Int? FindOptimalEmptyPosition(SmithGridStateData build)
        {
            var emptyPositions = GetAllEmptyPositions(build);
            if (emptyPositions.Count == 0)
                return null;
            int randomIndex = Random.Range(0, emptyPositions.Count);
            return emptyPositions[randomIndex];
        }
        private List<Vector2Int> GetAllEmptyPositions(SmithGridStateData build)
        {
            var emptyPositions = new List<Vector2Int>();
        for (int y = 0; y < build.gridSize; y++)
        {
            for (int x = 0; x < build.gridSize; x++)
            {
                    var position = new Vector2Int(x, y);
                    if (!occupiedPositions.Contains(position))
                    {
                        emptyPositions.Add(position);
                    }
                }
            }
            return emptyPositions;
        }
        private void RefreshOccupiedPositions()
        {
            occupiedPositions.Clear();
            if (cachedBuild?.cells != null)
            {
                var duplicatePositions = new HashSet<Vector2Int>();
                var cleanedCells = new List<SmithCellData>();
                foreach (var cell in cachedBuild.cells)
                {
                    var position = new Vector2Int(cell.x, cell.y);
                    if (!duplicatePositions.Contains(position))
                    {
                        duplicatePositions.Add(position);
                        occupiedPositions.Add(position);
                        cleanedCells.Add(cell);
                    }
                    else
                    {
                        Debug.LogWarning($"BitCollectionManager: Duplicate position ({cell.x}, {cell.y}) found in build data! Skipping duplicate: {cell.bitName}");
                    }
                }
                if (cleanedCells.Count != cachedBuild.cells.Count)
                {
                    Debug.Log($"BitCollectionManager: Cleaned {cachedBuild.cells.Count - cleanedCells.Count} duplicate positions from build data");
                    cachedBuild.cells = cleanedCells;
                    if (!persistenceService.SaveBuild(cachedBuild, buildFileName))
                    {
                        Debug.LogError("BitCollectionManager: Failed to save cleaned build data!");
                    }
                    else
                    {
                        Debug.Log("BitCollectionManager: Saved cleaned build data without duplicates");
                    }
                }
            }
        }
        private bool ExecuteCollection(SmithGridStateData build, Vector2Int position, Bit bitData)
        {
            Debug.Log($"BitCollectionManager: Adding {bitData.BitName} at position ({position.x}, {position.y})");
            Debug.Log($"Build before addition: {build.cells.Count} bits");
            var newCell = new SmithCellData(position.x, position.y, bitData);
            build.cells.Add(newCell);
            occupiedPositions.Add(position);
            Debug.Log($"Build after addition: {build.cells.Count} bits");
            if (!persistenceService.SaveBuild(build, buildFileName))
            {
                Debug.LogError("BitCollectionManager: Failed to save build!");
                build.cells.RemoveAt(build.cells.Count - 1);
                occupiedPositions.Remove(position);
                return false;
            }
            Debug.Log("BitCollectionManager: Build saved successfully, updating player incrementally...");
            AddBitToPlayer(position, newCell);
            Debug.Log($"BitCollectionManager: Successfully collected {bitData.BitName} at ({position.x}, {position.y})");
            return true;
        }
        private void UpdatePlayerBuild(SmithGridStateData build)
        {
            if (isUpdatingPlayer)
            {
                Debug.LogWarning("BitCollectionManager: Already updating player, skipping to prevent recursion");
                return;
            }
            if (playerController != null)
            {
                isUpdatingPlayer = true;
                try
                {
                    playerController.LoadSmithBuild(build);
                    Debug.Log("BitCollectionManager: Player build updated successfully");
                }
                finally
                {
                    isUpdatingPlayer = false;
                }
            }
            else
            {
                Debug.LogWarning("BitCollectionManager: PlayerController is null! Build saved but not loaded into player.");
            }
        }
        private void AddBitToPlayer(Vector2Int position, SmithCellData cellData)
        {
            if (isUpdatingPlayer)
            {
                Debug.LogWarning("BitCollectionManager: Already updating player, skipping incremental update to prevent recursion");
                return;
            }
            if (playerController != null)
            {
                isUpdatingPlayer = true;
                try
                {
                    playerController.AddBitToBuild(position, cellData);
                    Debug.Log($"BitCollectionManager: Incrementally added {cellData.bitName} to player at ({position.x}, {position.y})");
                }
                finally
                {
                    isUpdatingPlayer = false;
                }
            }
            else
            {
                Debug.LogWarning("BitCollectionManager: PlayerController is null! Bit saved but not added to player.");
            }
        }
        private void TestCoordinateSystem()
        {
            var build = GetCurrentBuild();
            if (build?.cells == null)
            {
                Debug.Log("No build data to test coordinates");
                return;
            }
            Debug.Log($"Testing {build.cells.Count} bits in {build.gridSize}x{build.gridSize} grid:");
            Debug.Log("Unity Coordinate System: Bottom-left = (0,0), Top-right = (gridSize-1,gridSize-1)");
            foreach (var cell in build.cells)
            {
                string position = $"({cell.x},{cell.y})";
                string location = "";
                if (cell.x == 0 && cell.y == 0) location = " [BOTTOM-LEFT]";
                else if (cell.x == build.gridSize-1 && cell.y == 0) location = " [BOTTOM-RIGHT]";
                else if (cell.x == 0 && cell.y == build.gridSize-1) location = " [TOP-LEFT]";
                else if (cell.x == build.gridSize-1 && cell.y == build.gridSize-1) location = " [TOP-RIGHT]";
                Debug.Log($"  {cell.bitName} at {position}{location}");
            }
        }
        public PowerBitPlayerController PlayerController => playerController;
        public GameObject BitDropPrefab => bitDropPrefab;
        public string BuildFileName => buildFileName;
    }
    [System.Serializable]
    public struct BuildInfo
    {
        public int totalSlots;
        public int occupiedSlots;
        public int emptySlots;
        public BuildInfo(int total, int occupied, int empty)
        {
            totalSlots = total;
            occupiedSlots = occupied;
            emptySlots = empty;
        }
        public float OccupancyPercentage => totalSlots > 0 ? (float)occupiedSlots / totalSlots * 100f : 0f;
        public override string ToString()
        {
            return $"Build: {occupiedSlots}/{totalSlots} slots occupied ({emptySlots} empty) - {OccupancyPercentage:F1}%";
        }
    }
}