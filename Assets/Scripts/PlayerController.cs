using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private bool isGrounded;
    private float horizontalInput;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Configure Rigidbody2D for platformer movement
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Configure BoxCollider2D
        boxCollider.size = new Vector2(0.8f, 0.8f);
        boxCollider.offset = new Vector2(0f, 0f);

        // Debug layer setup
        Debug.Log($"Player Layer: {gameObject.layer}");
        Debug.Log($"Ground Layer Mask: {groundLayer.value}");
        int playerLayerMask = 1 << gameObject.layer;
        bool canCollide = (Physics2D.GetLayerCollisionMask(gameObject.layer) & groundLayer.value) != 0;
        Debug.Log($"Can collide with ground: {canCollide}");
    }

    private void Update()
    {
        // Get horizontal input
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // Flip sprite based on movement direction
        if (horizontalInput != 0)
        {
            spriteRenderer.flipX = horizontalInput < 0;
        }

        // Check if grounded using multiple raycasts for better detection
        isGrounded = CheckGrounded();
        
        // Debug ground state
        if (isGrounded)
        {
            Debug.Log("Player is grounded!");
        }
    }

    private bool CheckGrounded()
    {
        // Get the bottom center of the collider
        Vector2 boxCenter = (Vector2)transform.position + boxCollider.offset;
        Vector2 boxSize = boxCollider.size;
        float boxBottom = boxCenter.y - (boxSize.y / 2f);

        // Cast multiple rays across the bottom of the collider
        Vector2 rayStart = new Vector2(boxCenter.x - (boxSize.x / 2f), boxBottom);
        Vector2 rayEnd = new Vector2(boxCenter.x + (boxSize.x / 2f), boxBottom);
        int rayCount = 3;

        for (int i = 0; i < rayCount; i++)
        {
            float t = i / (float)(rayCount - 1);
            Vector2 rayOrigin = Vector2.Lerp(rayStart, rayEnd, t);
            
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, groundCheckDistance, groundLayer);
            if (hit.collider != null)
            {
                Debug.DrawRay(rayOrigin, Vector2.down * hit.distance, Color.green);
                return true;
            }
            else
            {
                Debug.DrawRay(rayOrigin, Vector2.down * groundCheckDistance, Color.red);
            }
        }

        return false;
    }

    private void FixedUpdate()
    {
        // Move the player
        Vector2 movement = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        rb.linearVelocity = movement;

        // Debug velocity
        Debug.Log($"Player Velocity: {rb.linearVelocity}");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"Collision with: {collision.gameObject.name} on layer {collision.gameObject.layer}");
        foreach (ContactPoint2D contact in collision.contacts)
        {
            Debug.Log($"Contact point: {contact.point}, Normal: {contact.normal}");
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw the player's collider
        Gizmos.color = Color.yellow;
        Vector2 boxCenter = (Vector2)transform.position + (boxCollider != null ? boxCollider.offset : Vector2.zero);
        Vector2 boxSize = boxCollider != null ? boxCollider.size : Vector2.one;
        Gizmos.DrawWireCube(boxCenter, boxSize);

        // Draw ground check rays
        Gizmos.color = Color.red;
        float boxBottom = boxCenter.y - (boxSize.y / 2f);
        Vector2 rayStart = new Vector2(boxCenter.x - (boxSize.x / 2f), boxBottom);
        Vector2 rayEnd = new Vector2(boxCenter.x + (boxSize.x / 2f), boxBottom);
        int rayCount = 3;

        for (int i = 0; i < rayCount; i++)
        {
            float t = i / (float)(rayCount - 1);
            Vector2 rayOrigin = Vector2.Lerp(rayStart, rayEnd, t);
            Gizmos.DrawRay(rayOrigin, Vector2.down * groundCheckDistance);
        }
    }
}