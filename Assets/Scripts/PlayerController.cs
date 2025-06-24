using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundLayer;
    
    [Header("Movement Boundaries")]
    [SerializeField] private bool enableXBoundaries = true;
    [SerializeField] private float minX = -40f;
    [SerializeField] private float maxX = 40f;

    [Header("Character Settings")]
    [SerializeField] private CharacterRenderer characterRenderer;

    [Header("Debug")]
    [SerializeField] private bool showGroundCheckDebug = false;
    [SerializeField] private bool showDebugInfo = false;
    private bool wasGrounded = false;

    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private float horizontalInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        
        if (characterRenderer == null)
        {
            characterRenderer = GetComponentInChildren<CharacterRenderer>();
            if (characterRenderer == null)
            {
                Debug.LogError("CharacterRenderer not found! Please assign it in the inspector or ensure it exists as a child GameObject.");
            }
        }

        // Configure Rigidbody2D
        rb.gravityScale = 1f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Removed LoadLastSavedCharacter() from here
    }

    private void Start()
    {
        // Load the last saved character data after all Awake() calls
        LoadLastSavedCharacter();
    }

    public void LoadLastSavedCharacter()
    {
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, "last_build.json");
        Debug.Log($"Attempting to load character data from: {filePath}");
        
        if (!System.IO.File.Exists(filePath))
        {
            Debug.LogWarning("No saved character data found at: " + filePath);
            return;
        }

        try
        {
            string json = System.IO.File.ReadAllText(filePath);
            Debug.Log($"Read JSON data: {json}");
            
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("JSON file is empty!");
                return;
            }

            GridStateData gridState = JsonUtility.FromJson<GridStateData>(json);
            
            if (gridState == null)
            {
                Debug.LogError("Failed to parse character data from JSON - gridState is null");
                return;
            }

            if (gridState.cells == null)
            {
                Debug.LogError("Grid state cells collection is null!");
                return;
            }

            Debug.Log($"Successfully parsed grid state. Grid size: {gridState.gridSize}, Cells count: {gridState.cells.Count}");
            
            if (characterRenderer == null)
            {
                Debug.LogError("CharacterRenderer component is not assigned!");
                return;
            }

            LoadCharacterFromGridState(gridState);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading character data: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }

    private void Update()
    {
        // Check if player is grounded
        bool isGrounded = CheckGrounded();
        
        // Only log ground state changes if debug is enabled
        if (showGroundCheckDebug && isGrounded != wasGrounded)
        {
            Debug.Log($"Player {(isGrounded ? "landed on" : "left")} the ground");
            wasGrounded = isGrounded;
        }

        // Handle movement
        float moveInput = Input.GetAxisRaw("Horizontal");
        Vector2 velocity = rb.linearVelocity;
        velocity.x = moveInput * moveSpeed;
        
        // Apply movement boundaries
        if (enableXBoundaries)
        {
            Vector3 nextPosition = transform.position + new Vector3(velocity.x * Time.fixedDeltaTime, 0, 0);
            if (nextPosition.x < minX || nextPosition.x > maxX)
            {
                velocity.x = 0f; // Stop horizontal movement if it would go beyond boundaries
                
                // Clamp current position to boundaries if somehow went beyond
                Vector3 clampedPosition = transform.position;
                clampedPosition.x = Mathf.Clamp(clampedPosition.x, minX, maxX);
                transform.position = clampedPosition;
            }
        }
        
        rb.linearVelocity = velocity;
    }

    private bool CheckGrounded()
    {
        // Get the bottom center of the collider
        Vector2 boxCenter = boxCollider.bounds.center;
        Vector2 boxSize = boxCollider.bounds.size;
        Vector2 rayStart = new Vector2(boxCenter.x, boxCenter.y - boxSize.y/2);

        // Cast a ray downward
        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, groundCheckDistance, groundLayer);
        
        if (showGroundCheckDebug)
        {
            Debug.DrawRay(rayStart, Vector2.down * groundCheckDistance, hit.collider != null ? Color.green : Color.red);
        }

        return hit.collider != null;
    }

    public void LoadCharacterFromGridState(GridStateData gridState)
    {
        if (characterRenderer != null)
        {
            characterRenderer.LoadCharacterFromGridState(gridState);
            UpdateColliderSize();

            // Debug print all pixels
            Debug.Log("Player character loaded with the following pixels:");
            var activePixels = characterRenderer.GetActivePixels();
            foreach (var pixelPos in activePixels)
            {
                string pixelType = characterRenderer.GetPixelTypeAt(pixelPos);
                Debug.Log($"Pixel at position ({pixelPos.x}, {pixelPos.y}): {pixelType}");
            }
            Debug.Log($"Total pixels: {activePixels.Count}");
        }
        else
        {
            Debug.LogWarning("CharacterRenderer not found! Cannot load character.");
        }
    }

    public void TakeDamage(int damage)
    {
        if (characterRenderer != null)
        {
            // Get outer pixels that can be damaged
            var outerPixels = characterRenderer.GetOuterPixels();
            if (outerPixels.Count > 0)
            {
                // Remove a random outer pixel
                int randomIndex = Random.Range(0, outerPixels.Count);
                Vector2Int pixelToRemove = outerPixels[randomIndex];
                string pixelType = characterRenderer.GetPixelTypeAt(pixelToRemove);
                Debug.Log($"Removing pixel at ({pixelToRemove.x}, {pixelToRemove.y}) of type {pixelType}");
                
                characterRenderer.RemovePixel(pixelToRemove);
                UpdateColliderSize();

                // Print remaining pixels
                var remainingPixels = characterRenderer.GetActivePixels();
                Debug.Log($"Remaining pixels after damage: {remainingPixels.Count}");
                foreach (var pixelPos in remainingPixels)
                {
                    string type = characterRenderer.GetPixelTypeAt(pixelPos);
                    Debug.Log($"Remaining pixel at ({pixelPos.x}, {pixelPos.y}): {type}");
                }
            }
            else
            {
                Debug.Log("No outer pixels available to damage!");
            }
        }
    }

    private void UpdateColliderSize()
    {
        if (characterRenderer == null || boxCollider == null) return;

        // Get the bounds of the character from the renderer
        Bounds characterBounds = characterRenderer.GetCharacterBounds();
        
        // Update the collider size to match the character bounds
        boxCollider.size = new Vector2(characterBounds.size.x, characterBounds.size.y);
        // Set offset to the center of the bounds
        boxCollider.offset = new Vector2(characterBounds.center.x, characterBounds.center.y);

        if (showDebugInfo)
        {
            Debug.Log($"Collider size set to: {boxCollider.size}, offset: {boxCollider.offset}");
        }
    }
}