using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Grid))]
public class CharacterRenderer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float pixelSize = 0.25f;
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showGroundCheckDebug = false; // New flag for ground check debug

    [Header("Pixel Types")]
    [SerializeField] private Sprite emptyPixelSprite; // Reference to a transparent sprite for empty cells

    // Component references
    private Grid grid;
    private Tilemap tilemap;
    private TilemapRenderer tilemapRenderer;
    private Dictionary<Vector2Int, string> activePixels = new Dictionary<Vector2Int, string>();
    private Dictionary<Vector2Int, bool> isOuterPixel = new Dictionary<Vector2Int, bool>();
    private int gridSize;

    // Sprite cache
    private Dictionary<string, Sprite> pixelSprites = new Dictionary<string, Sprite>();
    private Dictionary<string, Tile> pixelTiles = new Dictionary<string, Tile>(); // Keep for backward compatibility

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

        // Load pixel sprites from Resources
        LoadPixelSprites();

        // Set up the grid
        grid.cellSize = new Vector3(pixelSize, pixelSize, 0);
        grid.cellGap = Vector3.zero;
        grid.cellLayout = GridLayout.CellLayout.Rectangle;
        grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;

        // Set up the tilemap
        tilemap.ClearAllTiles();
        tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0); // Center the tiles

        // Ensure the tilemap renderer is properly configured
        tilemapRenderer.sortingOrder = 1; // Make sure it renders above the ground
        tilemapRenderer.mode = TilemapRenderer.Mode.Chunk;

        if (showDebugInfo)
        {
            Debug.Log($"CharacterRenderer initialized on {gameObject.name}");
            Debug.Log($"Grid cell size set to: {grid.cellSize}");
            Debug.Log($"Tilemap found on: {tilemap.gameObject.name}");
            Debug.Log($"Loaded {pixelSprites.Count} pixel sprites:");
            foreach (var sprite in pixelSprites)
            {
                Debug.Log($"- {sprite.Key}: {(sprite.Value != null ? "Loaded" : "Failed to load")}");
            }
            if (emptyPixelSprite != null)
            {
                Debug.Log("Empty pixel sprite is assigned");
            }
            else
            {
                Debug.LogWarning("Empty pixel sprite is not assigned! Empty cells will be invisible.");
            }
        }
    }

    private void LoadPixelSprites()
    {
        // Define the pixel types and their corresponding sprite paths
        var pixelTypes = new Dictionary<string, string>
        {
            { "armor", "PixelTypes/Armor/Armor" },
            { "critical", "PixelTypes/Critical/Critical" },
            { "damage", "PixelTypes/Damage/Damage" },
            { "health", "PixelTypes/Health/Health" },
            { "lifesteal", "PixelTypes/Lifesteal/Lifesteal" },
            { "luck", "PixelTypes/Luck/Luck" }
        };

        if (showDebugInfo)
        {
            Debug.Log("Starting to load pixel sprites...");
        }

        // Load sprites from their specific paths
        foreach (var pixelType in pixelTypes)
        {
            if (showDebugInfo)
            {
                Debug.Log($"Attempting to load sprite for {pixelType.Key} from path: {pixelType.Value}");
            }

            // First try to load as a Sprite
            Sprite sprite = Resources.Load<Sprite>(pixelType.Value);
            
            // If that fails, try loading as a Texture2D and converting to Sprite
            if (sprite == null)
            {
                Texture2D texture = Resources.Load<Texture2D>(pixelType.Value);
                if (texture != null)
                {
                    Debug.LogWarning($"Found texture but not sprite for {pixelType.Key}. Attempting to create sprite...");
                    sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                }
            }

            if (sprite != null)
            {
                pixelSprites[pixelType.Key] = sprite;
                
                // Create a tile for the sprite
                Tile tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                pixelTiles[pixelType.Key] = tile;

                if (showDebugInfo)
                {
                    Debug.Log($"Successfully loaded sprite for {pixelType.Key}:");
                    Debug.Log($"- Sprite size: {sprite.rect.width}x{sprite.rect.height}");
                    Debug.Log($"- Pixels per unit: {sprite.pixelsPerUnit}");
                    Debug.Log($"- Pivot: {sprite.pivot}");
                }
            }
            else
            {
                Debug.LogError($"Failed to load sprite for {pixelType.Key} from path: {pixelType.Value}");
                Debug.LogError("Please check:");
                Debug.LogError($"1. Does the file exist at 'Assets/Resources/{pixelType.Value}.png'?");
                Debug.LogError("2. Is the file set as a Sprite (2D and UI) in its import settings?");
                Debug.LogError("3. Is the file in a Resources folder?");
                Debug.LogError("4. Does the file name match exactly (case-sensitive)?");
            }
        }

        if (showDebugInfo)
        {
            Debug.Log($"Finished loading pixel sprites. Successfully loaded {pixelSprites.Count} out of {pixelTypes.Count} sprites:");
            foreach (var sprite in pixelSprites)
            {
                Debug.Log($"- {sprite.Key}: {(sprite.Value != null ? "Loaded" : "Failed to load")}");
            }
            foreach (var pixelType in pixelTypes.Keys.Except(pixelSprites.Keys))
            {
                Debug.LogError($"- {pixelType}: Failed to load (missing from pixelSprites)");
            }
        }

        if (pixelSprites.Count == 0)
        {
            Debug.LogError("No pixel sprites were loaded! Please check:");
            Debug.LogError("1. Are your sprites in the correct folders under Resources/PixelTypes?");
            Debug.LogError("2. Do your sprite names match the paths defined in the code?");
            Debug.LogError("3. Are your sprites set as 'Sprite (2D and UI)' in their import settings?");
            Debug.LogError("4. Are your sprite files named exactly as specified (e.g., 'Armor.png' for the Armor sprite)?");
        }
    }

    private Tile GetTileForPixelType(string pixelType)
    {
        if (string.IsNullOrEmpty(pixelType))
        {
            Debug.LogWarning("Received null or empty pixel type");
            return null;
        }

        // Convert to lowercase for case-insensitive matching
        string pixelTypeLower = pixelType.ToLower();

        // First try to get from sprite cache and create a tile
        if (pixelSprites.TryGetValue(pixelTypeLower, out Sprite sprite))
        {
            if (!pixelTiles.ContainsKey(pixelTypeLower))
            {
                Tile tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                pixelTiles[pixelTypeLower] = tile;
            }
            return pixelTiles[pixelTypeLower];
        }

        // Fallback to old tile cache for backward compatibility
        if (pixelTiles.TryGetValue(pixelTypeLower, out Tile existingTile))
        {
            return existingTile;
        }

        Debug.LogWarning($"No sprite found for pixel type: {pixelType} (tried lowercase: {pixelTypeLower})");
        Debug.LogWarning("Available pixel types: " + string.Join(", ", pixelSprites.Keys));
        return null;
    }

    public void LoadCharacterFromGridState(GridStateData gridState)
    {
        if (gridState == null)
        {
            Debug.LogError("Cannot load null grid state!");
            return;
        }

        if (grid == null)
        {
            Debug.LogError("Grid component is null! Make sure it's attached to the GameObject.");
            return;
        }

        if (tilemap == null)
        {
            Debug.LogError("Tilemap component is null! Make sure it's attached to a child GameObject.");
            return;
        }

        if (showDebugInfo)
        {
            Debug.Log($"Loading grid state with size {gridState.gridSize}");
            Debug.Log($"Number of cells in grid state: {gridState.cells?.Count ?? 0}");
            if (gridState.cells != null)
            {
                Debug.Log("Cell types in grid state (original coordinates):");
                foreach (var cell in gridState.cells)
                {
                    Debug.Log($"- Cell at ({cell.x}, {cell.y}): {cell.pixelType}");
                }
            }
            else
            {
                Debug.LogError("Grid state cells collection is null!");
            }
        }

        // Clear existing data
        activePixels.Clear();
        isOuterPixel.Clear();
        tilemap.ClearAllTiles();
        gridSize = gridState.gridSize;

        // Calculate the offset to center the character
        float offsetX = -(gridSize * pixelSize) / 2f;
        float offsetY = -(gridSize * pixelSize) / 2f;
        transform.localPosition = new Vector3(offsetX, offsetY, 0);

        if (showDebugInfo)
        {
            Debug.Log($"Character position offset: ({offsetX}, {offsetY})");
        }

        // Create empty pixel tile if sprite is assigned
        Tile emptyTile = null;
        if (emptyPixelSprite != null)
        {
            emptyTile = ScriptableObject.CreateInstance<Tile>();
            emptyTile.sprite = emptyPixelSprite;
        }

        // First pass: initialize all cells with empty tiles
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                // Flip y-coordinate so (0,3) is at bottom left
                Vector3Int tilePos = new Vector3Int(x, gridSize - 1 - y, 0);
                if (emptyTile != null)
                {
                    tilemap.SetTile(tilePos, emptyTile);
                }
            }
        }

        // Second pass: load active pixels
        if (gridState.cells != null)
        {
            foreach (var cell in gridState.cells)
            {
                if (cell == null)
                {
                    Debug.LogWarning("Found null cell in grid state!");
                    continue;
                }

                // Store the original coordinates in activePixels
                Vector2Int originalPos = new Vector2Int(cell.x, cell.y);
                // Use flipped coordinates for tile placement
                Vector2Int tilePos = new Vector2Int(cell.x, gridSize - 1 - cell.y);
                Vector3Int tilemapPos = new Vector3Int(tilePos.x, tilePos.y, 0);

                if (!string.IsNullOrEmpty(cell.pixelType))
                {
                    activePixels[originalPos] = cell.pixelType;
                    isOuterPixel[originalPos] = false; // Initialize as non-outer, will be updated in second pass
                    
                    // Place the tile
                    Tile tile = GetTileForPixelType(cell.pixelType);
                    if (tile != null)
                    {
                        tilemap.SetTile(tilemapPos, tile);
                    }
                    else
                    {
                        Debug.LogWarning($"No tile found for pixel type: {cell.pixelType}");
                    }
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"Loaded pixel at original position ({originalPos.x}, {originalPos.y}): {cell.pixelType}");
                    }
                }
            }
        }

        // Third pass: identify outer pixels
        UpdateOuterPixels();

        if (showDebugInfo)
        {
            Debug.Log($"Character loaded with {activePixels.Count} pixels");
            Debug.Log($"Character bounds: {GetCharacterBounds()}");
        }
    }

    public void RemovePixel(Vector2Int position)
    {
        if (!activePixels.ContainsKey(position)) return;
        
        // Remove the pixel
        activePixels.Remove(position);
        
        // Update outer status of neighboring pixels
        UpdateNeighborsOuterStatus(position);
        
        // Update visuals
        UpdateVisuals();
        
        if (showDebugInfo)
        {
            Debug.Log($"Removed pixel at {position}");
            Debug.Log($"Remaining pixels: {activePixels.Count}");
            Debug.Log($"Outer pixels: {activePixels.Count(p => isOuterPixel[p.Key])}");
        }
    }

    private void UpdateNeighborsOuterStatus(Vector2Int removedPosition)
    {
        // Check all neighbors of the removed pixel
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                
                Vector2Int neighborPos = removedPosition + new Vector2Int(dx, dy);
                if (activePixels.ContainsKey(neighborPos))
                {
                    // Recalculate if this neighbor is now an outer pixel
                    bool isOuter = false;
                    for (int nx = -1; nx <= 1; nx++)
                    {
                        for (int ny = -1; ny <= 1; ny++)
                        {
                            if (nx == 0 && ny == 0) continue;
                            
                            Vector2Int checkPos = neighborPos + new Vector2Int(nx, ny);
                            if (!activePixels.ContainsKey(checkPos))
                            {
                                isOuter = true;
                                break;
                            }
                        }
                        if (isOuter) break;
                    }
                    isOuterPixel[neighborPos] = isOuter;
                }
            }
        }
    }

    private void UpdateVisuals()
    {
        // Clear the tilemap
        tilemap.ClearAllTiles();
        
        // Render all active pixels
        foreach (var pixel in activePixels)
        {
            Tile tile = GetTileForPixelType(pixel.Value);
            if (tile != null)
            {
                Vector3Int tilePos = new Vector3Int(pixel.Key.x, pixel.Key.y, 0);
                tilemap.SetTile(tilePos, tile);
            }
        }
    }

    private void RenderPixels()
    {
        if (tilemap == null)
        {
            Debug.LogError("Cannot render pixels: Tilemap is null!");
            return;
        }

        // Clear existing tiles
        tilemap.ClearAllTiles();

        int renderedTiles = 0;
        // Render all active pixels
        foreach (var pixel in activePixels)
        {
            Tile tile = GetTileForPixelType(pixel.Value);
            if (tile != null)
            {
                Vector3Int tilePos = new Vector3Int(pixel.Key.x, pixel.Key.y, 0);
                tilemap.SetTile(tilePos, tile);
                renderedTiles++;

                if (showDebugInfo)
                {
                    Debug.Log($"Rendered tile at ({tilePos.x}, {tilePos.y}) using tile for {pixel.Value}");
                }
            }
            else
            {
                Debug.LogWarning($"No tile found for pixel type: {pixel.Value}");
            }
        }

        if (showDebugInfo)
        {
            Debug.Log($"Rendered {renderedTiles} tiles out of {activePixels.Count} pixels");
        }
    }

    private void ClearCharacter()
    {
        activePixels.Clear();
        tilemap.ClearAllTiles();
    }

    // Helper class to store pixel data
    private class PixelData
    {
        public Vector2Int position;
        public string type;
        public bool isOuter;
    }

    // Public methods for gameplay
    public bool IsPixelOuter(Vector2Int position)
    {
        return activePixels.ContainsKey(position) && isOuterPixel[position];
    }

    public List<Vector2Int> GetOuterPixels()
    {
        return activePixels.Where(p => isOuterPixel[p.Key])
                          .Select(p => p.Key)
                          .ToList();
    }

    public List<Vector2Int> GetActivePixels()
    {
        return activePixels.Keys.ToList();
    }

    public bool HasPixelAt(Vector2Int position)
    {
        return activePixels.ContainsKey(position);
    }

    public string GetPixelTypeAt(Vector2Int position)
    {
        return activePixels.TryGetValue(position, out string pixelType) ? pixelType : null;
    }

    public Bounds GetCharacterBounds()
    {
        if (gridSize <= 0)
        {
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        // Calculate the offset used for centering
        float offsetX = -(gridSize * pixelSize) / 2f;
        float offsetY = -(gridSize * pixelSize) / 2f;

        // The full grid covers from (0,0) to (gridSize, gridSize) in local space
        float minX = 0 * pixelSize + offsetX;
        float minY = 0 * pixelSize + offsetY;
        float maxX = gridSize * pixelSize + offsetX;
        float maxY = gridSize * pixelSize + offsetY;

        Vector3 size = new Vector3(
            maxX - minX,
            maxY - minY,
            0
        );
        Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0);

        if (showDebugInfo)
        {
            Debug.Log($"Full grid bounds - Size: {size}, Center: {center}");
            Debug.Log($"Min: ({minX}, {minY}), Max: ({maxX}, {maxY})");
        }

        return new Bounds(center, size);
    }

    private void UpdateOuterPixels()
    {
        isOuterPixel.Clear();
        foreach (var pixel in activePixels)
        {
            Vector2Int pos = pixel.Key;
            bool isOuter = false;

            // Check all four adjacent positions
            Vector2Int[] adjacentPositions = new Vector2Int[]
            {
                new Vector2Int(pos.x + 1, pos.y), // right
                new Vector2Int(pos.x - 1, pos.y), // left
                new Vector2Int(pos.x, pos.y + 1), // up
                new Vector2Int(pos.x, pos.y - 1)  // down
            };

            // A pixel is outer if any of its adjacent positions is empty
            foreach (var adjacentPos in adjacentPositions)
            {
                if (!activePixels.ContainsKey(adjacentPos))
                {
                    isOuter = true;
                    break;
                }
            }

            isOuterPixel[pos] = isOuter;
        }

        if (showDebugInfo)
        {
            Debug.Log($"Updated outer pixels. Total outer pixels: {isOuterPixel.Count(p => p.Value)}");
        }
    }
} 