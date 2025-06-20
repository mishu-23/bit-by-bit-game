using UnityEngine;
using System.Collections;

public class CrawlingEntity : MonoBehaviour, IDamageable
{
    [Header("Target")]
    public Transform playerTarget;

    [Header("Movement")]
    public float moveForce = 8f;
    public float maxSpeed = 4f;
    public float groundY = 0.5f;
    public float detectionRange = 10f;
    public float minFollowDistance = 1f;

    [Header("Combat")]
    public int maxHealth = 15;
    public int currentHealth;

    [Header("Behavior")]
    public bool isFollowing = false;
    public float followDelay = 0.5f; // Delay before starting to follow

    private Rigidbody2D rb;
    private bool hasCollidedWithPlayer = false;

    private void Awake()
    {
        gameObject.layer = 6; // Entity layer
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
        
        // Ignore collisions with player layer (assuming player is on layer 3)
        Physics2D.IgnoreLayerCollision(6, 3, true);  // Entity vs Player
    }

    private void Start()
    {
        transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
        
        // Find player if not assigned
        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
                Debug.Log($"CrawlingEntity {gameObject.name} found player: {player.name}");
            }
            else
            {
                Debug.LogError($"CrawlingEntity {gameObject.name} couldn't find player with tag 'Player'!");
            }
        }
    }

    private void FixedUpdate()
    {
        // Keep at ground level
        if (Mathf.Abs(transform.position.y - groundY) > 0.1f)
        {
            transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
        }

        if (playerTarget == null || hasCollidedWithPlayer) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);

        // Check if player is in detection range
        if (distanceToPlayer <= detectionRange && distanceToPlayer > minFollowDistance)
        {
            if (!isFollowing)
            {
                Debug.Log($"CrawlingEntity {gameObject.name} detected player at distance {distanceToPlayer:F1}, starting to follow...");
                isFollowing = true;
            }

            // Move towards player
            float direction = Mathf.Sign(playerTarget.position.x - transform.position.x);
            rb.AddForce(new Vector2(direction * moveForce, 0f), ForceMode2D.Force);
            
            // Limit speed
            if (Mathf.Abs(rb.linearVelocity.x) > maxSpeed)
            {
                rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * maxSpeed, 0f);
            }

            Debug.Log($"CrawlingEntity {gameObject.name} following player, distance: {distanceToPlayer:F1}");
        }
        else if (distanceToPlayer <= minFollowDistance)
        {
            // Stop when close enough to player
            rb.linearVelocity = Vector2.zero;
            isFollowing = false;
            Debug.Log($"CrawlingEntity {gameObject.name} reached player, stopping at distance {distanceToPlayer:F1}");
        }
        else if (distanceToPlayer > detectionRange)
        {
            // Player too far, stop following
            if (isFollowing)
            {
                Debug.Log($"CrawlingEntity {gameObject.name} lost player, stopping (distance: {distanceToPlayer:F1})");
                isFollowing = false;
            }
            
            // Apply brakes
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, 0.1f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if we hit the player
        if (other.CompareTag("Player") || other.gameObject.layer == 3)
        {
            hasCollidedWithPlayer = true;
            rb.linearVelocity = Vector2.zero;
            Debug.Log($"CrawlingEntity {gameObject.name} collided with player! Stopping movement.");
        }
    }

    private void OnDrawGizmos()
    {
        // Show detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Show minimum follow distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minFollowDistance);
        
        // Show current state
        if (isFollowing)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
        else
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, 0.2f);
        }

        // Draw line to player if following
        if (isFollowing && playerTarget != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, playerTarget.position);
        }
    }

    // Public method to reset the entity (useful for respawning)
    public void ResetEntity()
    {
        hasCollidedWithPlayer = false;
        isFollowing = false;
        rb.linearVelocity = Vector2.zero;
        Debug.Log($"CrawlingEntity {gameObject.name} has been reset");
    }

    // Public method to get current state
    public bool IsFollowing()
    {
        return isFollowing;
    }

    public bool HasCollidedWithPlayer()
    {
        return hasCollidedWithPlayer;
    }

    // IDamageable implementation
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"CrawlingEntity {gameObject.name} took {damage} damage! Health: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0)
        {
            Debug.Log($"CrawlingEntity {gameObject.name} destroyed!");
            Destroy(gameObject);
        }
    }
} 