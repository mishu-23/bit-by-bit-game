using UnityEngine;
using System.Collections;

public class FlyingEntity : MonoBehaviour
{
    [Header("Target")]
    public Transform playerTarget;

    [Header("Movement")]
    public float moveForce = 6f;
    public float maxSpeed = 3f;
    public float detectionRange = 8f;
    public float minFollowDistance = 1.5f;
    public float maxHeight = 5f;
    public float minHeight = 0.5f;

    [Header("Behavior")]
    public bool isFollowing = false;
    public float followDelay = 0.3f;

    private Rigidbody2D rb;
    private bool hasCollidedWithPlayer = false;

    private void Awake()
    {
        gameObject.layer = 6; // Entity layer
        rb = GetComponent<Rigidbody2D>();
        
        // Ignore collisions with player layer (assuming player is on layer 3)
        Physics2D.IgnoreLayerCollision(6, 3, true);  // Entity vs Player
    }

    private void Start()
    {
        // Find player if not assigned
        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
                Debug.Log($"FlyingEntity {gameObject.name} found player: {player.name}");
            }
            else
            {
                Debug.LogError($"FlyingEntity {gameObject.name} couldn't find player with tag 'Player'!");
            }
        }
    }

    private void FixedUpdate()
    {
        if (playerTarget == null || hasCollidedWithPlayer) return;

        Vector2 distanceToPlayer = playerTarget.position - transform.position;
        float distanceMagnitude = distanceToPlayer.magnitude;

        // Check if player is in detection range
        if (distanceMagnitude <= detectionRange && distanceMagnitude > minFollowDistance)
        {
            if (!isFollowing)
            {
                Debug.Log($"FlyingEntity {gameObject.name} detected player at distance {distanceMagnitude:F1}, starting to follow...");
                isFollowing = true;
            }

            // Calculate movement direction (both X and Y)
            Vector2 direction = distanceToPlayer.normalized;
            
            // Apply force in both X and Y directions
            rb.AddForce(direction * moveForce, ForceMode2D.Force);
            
            // Limit speed
            if (rb.linearVelocity.magnitude > maxSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
            }

            // Constrain height
            Vector3 currentPos = transform.position;
            if (currentPos.y > maxHeight)
            {
                currentPos.y = maxHeight;
                transform.position = currentPos;
                // Stop upward movement
                Vector2 velocity = rb.linearVelocity;
                if (velocity.y > 0) velocity.y = 0;
                rb.linearVelocity = velocity;
            }
            else if (currentPos.y < minHeight)
            {
                currentPos.y = minHeight;
                transform.position = currentPos;
                // Stop downward movement
                Vector2 velocity = rb.linearVelocity;
                if (velocity.y < 0) velocity.y = 0;
                rb.linearVelocity = velocity;
            }

            Debug.Log($"FlyingEntity {gameObject.name} following player, distance: {distanceMagnitude:F1}");
        }
        else if (distanceMagnitude <= minFollowDistance)
        {
            // Stop when close enough to player
            rb.linearVelocity = Vector2.zero;
            isFollowing = false;
            Debug.Log($"FlyingEntity {gameObject.name} reached player, stopping at distance {distanceMagnitude:F1}");
        }
        else if (distanceMagnitude > detectionRange)
        {
            // Player too far, stop following
            if (isFollowing)
            {
                Debug.Log($"FlyingEntity {gameObject.name} lost player, stopping (distance: {distanceMagnitude:F1})");
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
            Debug.Log($"FlyingEntity {gameObject.name} collided with player! Stopping movement.");
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
        
        // Show height constraints
        Gizmos.color = Color.cyan;
        Vector3 topPos = transform.position;
        topPos.y = maxHeight;
        Gizmos.DrawWireSphere(topPos, 0.2f);
        
        Vector3 bottomPos = transform.position;
        bottomPos.y = minHeight;
        Gizmos.DrawWireSphere(bottomPos, 0.2f);
        
        // Draw height constraint lines
        Gizmos.DrawLine(new Vector3(transform.position.x - 2f, maxHeight, 0f), 
                       new Vector3(transform.position.x + 2f, maxHeight, 0f));
        Gizmos.DrawLine(new Vector3(transform.position.x - 2f, minHeight, 0f), 
                       new Vector3(transform.position.x + 2f, minHeight, 0f));
        
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

        // Show velocity vector
        if (rb != null && rb.linearVelocity.magnitude > 0.1f)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, rb.linearVelocity);
        }
    }

    // Public method to reset the entity (useful for respawning)
    public void ResetEntity()
    {
        hasCollidedWithPlayer = false;
        isFollowing = false;
        rb.linearVelocity = Vector2.zero;
        Debug.Log($"FlyingEntity {gameObject.name} has been reset");
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

    // Public method to get current velocity
    public Vector2 GetVelocity()
    {
        return rb != null ? rb.linearVelocity : Vector2.zero;
    }
} 