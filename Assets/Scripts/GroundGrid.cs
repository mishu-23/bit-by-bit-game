using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
[RequireComponent(typeof(BoxCollider2D))]
public class GroundGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask playerLayer;

    private Tilemap tilemap;
    private BoxCollider2D groundCollider;

    private void Awake()
    {
        // Get required components
        tilemap = GetComponent<Tilemap>();
        groundCollider = GetComponent<BoxCollider2D>();
        
        // Configure the ground collider to match the tilemap bounds
        Bounds tilemapBounds = tilemap.localBounds;
        groundCollider.size = new Vector2(tilemapBounds.size.x, tilemapBounds.size.y);
        groundCollider.offset = new Vector2(tilemapBounds.center.x, tilemapBounds.center.y);
        
        // Set the layer for ground detection
        int groundLayerIndex = Mathf.RoundToInt(Mathf.Log(groundLayer.value, 2));
        gameObject.layer = groundLayerIndex;

        // Make sure the ground is static
        gameObject.isStatic = true;

        // Ensure layers can collide
        int playerLayerIndex = Mathf.RoundToInt(Mathf.Log(playerLayer.value, 2));
        Physics2D.IgnoreLayerCollision(playerLayerIndex, groundLayerIndex, false);

        // Debug information
        Debug.Log($"Ground Layer: {gameObject.layer} (Layer {groundLayerIndex})");
        Debug.Log($"Player Layer: {playerLayerIndex}");
        Debug.Log($"Ground Collider Size: {groundCollider.size}");
        Debug.Log($"Ground Collider Offset: {groundCollider.offset}");
        Debug.Log($"Ground Position: {transform.position}");
        Debug.Log($"Ground Bounds: {tilemapBounds}");
        Debug.Log($"Layers can collide: {!Physics2D.GetIgnoreLayerCollision(playerLayerIndex, groundLayerIndex)}");
    }

    private void OnDrawGizmos()
    {
        // Draw the ground collider in the editor
        if (groundCollider != null)
        {
            // Draw the collider
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position + (Vector3)groundCollider.offset, groundCollider.size);

            // Draw the actual bounds of the tilemap
            if (tilemap != null)
            {
                Gizmos.color = Color.yellow;
                Bounds bounds = tilemap.localBounds;
                Gizmos.DrawWireCube(transform.position + bounds.center, bounds.size);
            }
        }
    }

    // Helper method to check if a tile is solid
    public bool IsTileSolid(Vector3Int tilePosition)
    {
        TileBase tile = tilemap.GetTile(tilePosition);
        return tile != null;
    }
} 