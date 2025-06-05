using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Grid))]
public class GroundGrid : MonoBehaviour
{
    [Header("Layer Settings")]
    [SerializeField] private LayerMask groundLayer = 256; // Layer 8 (2^8 = 256)
    [SerializeField] private LayerMask playerLayer = 8;   // Layer 3 (2^3 = 8)

    private Grid grid;
    private Tilemap tilemap;
    private BoxCollider2D groundCollider;

    private void Awake()
    {
        // Get required components
        grid = GetComponent<Grid>();
        tilemap = GetComponentInChildren<Tilemap>();
        groundCollider = GetComponent<BoxCollider2D>();

        // Validate components
        if (grid == null)
        {
            Debug.LogError($"GroundGrid on {gameObject.name} requires a Grid component!");
            enabled = false;
            return;
        }

        if (tilemap == null)
        {
            Debug.LogError($"GroundGrid on {gameObject.name} requires a Tilemap component on a child GameObject!");
            Debug.LogError("Please create a child GameObject with a Tilemap component.");
            enabled = false;
            return;
        }

        if (groundCollider == null)
        {
            Debug.LogError($"GroundGrid on {gameObject.name} requires a BoxCollider2D component!");
            enabled = false;
            return;
        }

        // Set the ground object to the ground layer (Layer 8)
        gameObject.layer = 8;

        // Ensure the layers can collide
        Physics2D.IgnoreLayerCollision(8, 3, false);

        if (Debug.isDebugBuild)
        {
            Debug.Log($"GroundGrid initialized on {gameObject.name}");
            Debug.Log("Layer collision enabled between Ground (Layer 8) and Player (Layer 3)");
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying && groundCollider != null)
        {
            // Draw the collider in the editor
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position + (Vector3)groundCollider.offset, groundCollider.size);
        }
    }
} 