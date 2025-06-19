using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Grid))]
public class PowerBitCharacterRenderer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float bitSize = 0.25f;
    [SerializeField] private bool showDebugInfo = true;

    // Public property to access bit size
    public float BitSize => bitSize;

    [Header("Bit Sprites")]
    [SerializeField] private Sprite rarePowerBitSprite;
    [SerializeField] private Sprite epicPowerBitSprite;
    [SerializeField] private Sprite legendaryPowerBitSprite;

    // Component references
    private Grid grid;
    private Tilemap tilemap;
    private TilemapRenderer tilemapRenderer;
    private Dictionary<Vector2Int, SmithCellData> activeBits = new Dictionary<Vector2Int, SmithCellData>();
    private Dictionary<Vector2Int, bool> isOuterBit = new Dictionary<Vector2Int, bool>();
    private int gridSize;

    // Sprite cache
    private Dictionary<Rarity, Sprite> bitSprites = new Dictionary<Rarity, Sprite>();
    private Dictionary<Rarity, Tile> bitTiles = new Dictionary<Rarity, Tile>();

    private void Awake()
    {
        // Get required components
        grid = GetComponent<Grid>();
        tilemap = GetComponentInChildren<Tilemap>();
        tilemapRenderer = tilemap.GetComponent<TilemapRenderer>();

        if (tilemap == null)
        {
            Debug.LogError($"No Tilemap found in children of {gameObject.name}! Please add a Tilemap component.");
            return;
        }

        // Load bit sprites
        LoadBitSprites();

        // Set up the grid
        grid.cellSize = new Vector3(bitSize, bitSize, 0);
        grid.cellGap = Vector3.zero;
        grid.cellLayout = GridLayout.CellLayout.Rectangle;
        grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;

        // Set up the tilemap
        tilemap.ClearAllTiles();
        tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0); // Center the tiles

        // Ensure the tilemap renderer is properly configured
        tilemapRenderer.sortingOrder = 1;
        tilemapRenderer.mode = TilemapRenderer.Mode.Chunk;

        if (showDebugInfo)
        {
            Debug.Log($"PowerBitCharacterRenderer initialized on {gameObject.name}");
            Debug.Log($"Grid cell size set to: {grid.cellSize}");
            Debug.Log($"Tilemap found on: {tilemap.gameObject.name}");
        }
    }

    private void LoadBitSprites()
    {
        // Load sprites from inspector assignments
        if (rarePowerBitSprite != null)
        {
            bitSprites[Rarity.Rare] = rarePowerBitSprite;
            CreateTileForRarity(Rarity.Rare);
        }
        
        if (epicPowerBitSprite != null)
        {
            bitSprites[Rarity.Epic] = epicPowerBitSprite;
            CreateTileForRarity(Rarity.Epic);
        }
        
        if (legendaryPowerBitSprite != null)
        {
            bitSprites[Rarity.Legendary] = legendaryPowerBitSprite;
            CreateTileForRarity(Rarity.Legendary);
        }

        if (showDebugInfo)
        {
            Debug.Log($"Loaded {bitSprites.Count} bit sprites");
        }
    }

    private void CreateTileForRarity(Rarity rarity)
    {
        if (bitSprites.TryGetValue(rarity, out Sprite sprite))
        {
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            bitTiles[rarity] = tile;
        }
    }

    public void LoadCharacterFromSmithBuild(SmithGridStateData gridState)
    {
        if (gridState == null)
        {
            Debug.LogError("Cannot load null Smith grid state!");
            return;
        }

        if (grid == null)
        {
            Debug.LogError("Grid component is null!");
            return;
        }

        if (tilemap == null)
        {
            Debug.LogError("Tilemap component is null!");
            return;
        }

        if (showDebugInfo)
        {
            Debug.Log($"Loading Smith build with size {gridState.gridSize}");
            Debug.Log($"Number of Power Bits: {gridState.cells?.Count ?? 0}");
        }

        // Clear existing data
        activeBits.Clear();
        isOuterBit.Clear();
        tilemap.ClearAllTiles();
        gridSize = gridState.gridSize;

        // Reset the tilemap position to align with the parent's pivot.
        tilemap.transform.localPosition = Vector3.zero;

        if (showDebugInfo)
        {
            Debug.Log($"Tilemap position offset: ({tilemap.transform.localPosition.x}, {tilemap.transform.localPosition.y})");
        }

        // Load active Power Bits
        if (gridState.cells != null)
        {
            foreach (var cell in gridState.cells)
            {
                if (cell == null) continue;

                // Invert the Y coordinate because UI grid Y=0 is top, but tilemap Y=0 is bottom.
                int invertedY = (gridSize - 1) - cell.y;
                Vector2Int bitPos = new Vector2Int(cell.x, invertedY);

                SmithCellData correctedCell = cell;
                correctedCell.y = invertedY;

                activeBits[bitPos] = correctedCell;
                isOuterBit[bitPos] = false; // Initialize as non-outer, will be updated later
                
                // Place the tile
                Tile tile = GetTileForRarity(cell.rarity);
                if (tile != null)
                {
                    Vector3Int tilemapPos = new Vector3Int(bitPos.x, bitPos.y, 0);
                    tilemap.SetTile(tilemapPos, tile);
                }
                else
                {
                    Debug.LogWarning($"No tile found for rarity: {cell.rarity}");
                }
                
                if (showDebugInfo)
                {
                    Debug.Log($"Loaded Power Bit at grid({cell.x},{cell.y}) -> renderer({bitPos.x},{bitPos.y}): {cell.bitName} ({cell.rarity})");
                }
            }
        }

        // Identify outer bits
        UpdateOuterBits();

        if (showDebugInfo)
        {
            Debug.Log($"Character loaded with {activeBits.Count} Power Bits");
            Debug.Log($"Character bounds: {GetCharacterBounds()}");
        }
    }

    private Tile GetTileForRarity(Rarity rarity)
    {
        if (bitTiles.TryGetValue(rarity, out Tile tile))
        {
            return tile;
        }
        
        Debug.LogWarning($"No tile found for rarity: {rarity}");
        return null;
    }

    private void UpdateOuterBits()
    {
        isOuterBit.Clear();
        foreach (var bit in activeBits)
        {
            Vector2Int pos = bit.Key;
            bool isOuter = false;

            // Check all four adjacent positions
            Vector2Int[] adjacentPositions = new Vector2Int[]
            {
                new Vector2Int(pos.x + 1, pos.y), // right
                new Vector2Int(pos.x - 1, pos.y), // left
                new Vector2Int(pos.x, pos.y + 1), // up
                new Vector2Int(pos.x, pos.y - 1)  // down
            };

            // A bit is outer if any of its adjacent positions is empty
            foreach (var adjacentPos in adjacentPositions)
            {
                if (!activeBits.ContainsKey(adjacentPos))
                {
                    isOuter = true;
                    break;
                }
            }

            isOuterBit[pos] = isOuter;
        }

        if (showDebugInfo)
        {
            Debug.Log($"Updated outer bits. Total outer bits: {isOuterBit.Count(p => p.Value)}");
        }
    }

    public void RemoveBit(Vector2Int position)
    {
        if (!activeBits.ContainsKey(position)) return;
        
        // Remove the bit
        activeBits.Remove(position);
        
        // Update outer status of neighboring bits
        UpdateNeighborsOuterStatus(position);
        
        // Update visuals
        UpdateVisuals();
        
        if (showDebugInfo)
        {
            Debug.Log($"Removed Power Bit at {position}");
            Debug.Log($"Remaining bits: {activeBits.Count}");
            Debug.Log($"Outer bits: {activeBits.Count(p => isOuterBit[p.Key])}");
        }
    }

    private void UpdateNeighborsOuterStatus(Vector2Int removedPosition)
    {
        // Check all neighbors of the removed bit
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                
                Vector2Int neighborPos = removedPosition + new Vector2Int(dx, dy);
                if (activeBits.ContainsKey(neighborPos))
                {
                    // Recalculate if this neighbor is now an outer bit
                    bool isOuter = false;
                    for (int nx = -1; nx <= 1; nx++)
                    {
                        for (int ny = -1; ny <= 1; ny++)
                        {
                            if (nx == 0 && ny == 0) continue;
                            
                            Vector2Int checkPos = neighborPos + new Vector2Int(nx, ny);
                            if (!activeBits.ContainsKey(checkPos))
                            {
                                isOuter = true;
                                break;
                            }
                        }
                        if (isOuter) break;
                    }
                    isOuterBit[neighborPos] = isOuter;
                }
            }
        }
    }

    private void UpdateVisuals()
    {
        // Clear the tilemap
        tilemap.ClearAllTiles();
        
        // Render all active bits
        foreach (var bit in activeBits)
        {
            Tile tile = GetTileForRarity(bit.Value.rarity);
            if (tile != null)
            {
                Vector3Int tilePos = new Vector3Int(bit.Key.x, bit.Key.y, 0);
                tilemap.SetTile(tilePos, tile);
            }
        }
    }

    // Public methods for gameplay
    public bool IsBitOuter(Vector2Int position)
    {
        return activeBits.ContainsKey(position) && isOuterBit[position];
    }

    public List<Vector2Int> GetOuterBits()
    {
        return activeBits.Where(p => isOuterBit[p.Key])
                        .Select(p => p.Key)
                        .ToList();
    }

    public List<Vector2Int> GetActiveBits()
    {
        return activeBits.Keys.ToList();
    }

    public bool HasBitAt(Vector2Int position)
    {
        return activeBits.ContainsKey(position);
    }

    public SmithCellData GetBitAt(Vector2Int position)
    {
        return activeBits.TryGetValue(position, out SmithCellData bitData) ? bitData : null;
    }

    public Bounds GetCharacterBounds()
    {
        if (gridSize <= 0)
        {
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        float fullSize = gridSize * bitSize;
        Vector3 size = new Vector3(fullSize, fullSize, 0);
        // The center of the grid area, assuming the grid's origin is at (0,0)
        Vector3 center = new Vector3(fullSize * 0.5f, fullSize * 0.5f, 0);

        Bounds bounds = new Bounds(center, size);

        if (showDebugInfo)
        {
            Debug.Log($"Fixed Grid Bounds - Size: {size}, Center: {center}");
        }

        return bounds;
    }

    // Get total damage from all Power Bits
    public int GetTotalDamage()
    {
        return activeBits.Values.Sum(bit => bit.damage);
    }

    // Get average shooting probability from all Power Bits
    public float GetAverageShootingProbability()
    {
        if (activeBits.Count == 0) return 0f;
        return activeBits.Values.Average(bit => bit.shootingProbability);
    }
} 