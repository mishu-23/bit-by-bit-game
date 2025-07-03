using UnityEngine;
using BitByBit.Core;
using System.Collections;

public class FlyingEntity : MonoBehaviour, IDamageable
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

    [Header("Combat")]
    public int maxHealth = 10;
    public int currentHealth;

    [Header("Behavior")]
    public bool isFollowing = false;
    public float followDelay = 0.3f;
    
    [Header("Stealing & Fleeing")]
    public float fleeSpeed = 6f;
    public float fleeDistance = 40f;

    [Header("Visual")]
    public bool enableSpriteFlipping = true;
    
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private bool hasCollidedWithPlayer = false;
    private bool facingRight = true;
    private bool hasStolenBit = false;
    private bool isFleeing = false;
    private Vector3 fleeTarget;

    private void Awake()
    {
        gameObject.layer = 6; // Entity layer
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
        
        // Ignore collisions with player layer (assuming player is on layer 3)
        Physics2D.IgnoreLayerCollision(6, 3, true);  // Entity vs Player
        
        // Validate sprite renderer for flipping
        if (spriteRenderer == null && enableSpriteFlipping)
        {
            Debug.LogWarning($"FlyingEntity {gameObject.name}: SpriteRenderer not found! Sprite flipping will be disabled.");
            enableSpriteFlipping = false;
        }
        

    }

    private void Start()
    {
        InitializePlayerTarget();
    }
    
    private void InitializePlayerTarget()
    {
        // Find player if not assigned
        if (playerTarget == null)
        {
            // Use GameReferences for better performance
            if (GameReferences.Instance != null && GameReferences.Instance.Player != null)
            {
                playerTarget = GameReferences.Instance.Player;
                Debug.Log($"FlyingEntity {gameObject.name} found player via GameReferences: {playerTarget.name}");
            }
            else
            {
                // Fallback: try to find player with tag if GameReferences fails
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerTarget = player.transform;
                    Debug.Log($"FlyingEntity {gameObject.name} found player via fallback: {player.name}");
                    Debug.LogWarning("FlyingEntity: Found player via fallback method. Please ensure GameReferences is properly configured.");
                }
                else
                {
                    Debug.LogError($"FlyingEntity {gameObject.name} couldn't find player with tag 'Player'!");
                }
            }
        }
    }
    


    private void FixedUpdate()
    {
        if (playerTarget == null) return;

        // Handle fleeing behavior if fleeing
        if (isFleeing)
        {
            HandleFleeing();
            return;
        }

        // If already collided with player or stolen bit, don't move towards player anymore
        if (hasCollidedWithPlayer || hasStolenBit) return;

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
            
            // Handle sprite flipping based on horizontal movement direction
            if (enableSpriteFlipping && spriteRenderer != null)
            {
                HandleSpriteFlipping(direction.x);
            }
            
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
            // Close enough to player - attempt to steal
            rb.linearVelocity = Vector2.zero;
            isFollowing = false;
            
            if (!hasStolenBit)
            {
                StealBitFromPlayer();
            }
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
            Debug.Log($"FlyingEntity {gameObject.name} collided with player!");
            
            // Attempt to steal if we haven't already
            if (!hasStolenBit)
            {
                StealBitFromPlayer();
            }
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
        hasStolenBit = false;
        isFleeing = false;
        fleeTarget = Vector3.zero;
        rb.linearVelocity = Vector2.zero;
        
        // Reset facing direction to default (right)
        if (enableSpriteFlipping && spriteRenderer != null)
        {
            facingRight = true;
            spriteRenderer.flipX = false;
        }
        
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

    // Sprite flipping logic
    private void HandleSpriteFlipping(float horizontalDirection)
    {
        // Only flip if there's significant horizontal movement
        if (Mathf.Abs(horizontalDirection) > 0.1f)
        {
            bool shouldFaceRight = horizontalDirection > 0;
            
            // Only flip if the direction has changed
            if (shouldFaceRight != facingRight)
            {
                facingRight = shouldFaceRight;
                spriteRenderer.flipX = !facingRight; // Flip sprite when facing left
                
                Debug.Log($"FlyingEntity {gameObject.name} flipped to face {(facingRight ? "right" : "left")}");
            }
        }
    }

    // Public method to manually set facing direction
    public void SetFacingDirection(bool faceRight)
    {
        if (enableSpriteFlipping && spriteRenderer != null)
        {
            facingRight = faceRight;
            spriteRenderer.flipX = !facingRight;
        }
    }

    // Public method to get current facing direction
    public bool IsFacingRight()
    {
        return facingRight;
    }

    // Stealing and fleeing functionality
    private void StealBitFromPlayer()
    {
        if (hasStolenBit) return; // Already stolen
        
        // Try to get the player controller to access the build
        if (playerTarget != null)
        {
            PowerBitPlayerController playerController = playerTarget.GetComponent<PowerBitPlayerController>();
            if (playerController != null)
            {
                // Try to steal (eat) a random bit from the player's build
                Bit stolenBit = playerController.StealRandomBitFromBuild();
                if (stolenBit != null)
                {
                    // Bit is "eaten" - just destroyed, not carried
                    hasStolenBit = true;
                    Debug.Log($"FlyingEntity {gameObject.name} ate {stolenBit.BitName} from player's build!");
                    
                    // Start fleeing after eating the bit
                    StartFleeing();
                }
                else
                {
                    Debug.Log($"FlyingEntity {gameObject.name} tried to eat a bit but player's build is empty!");
                    // Still flee even if no bit to steal
                    StartFleeing();
                }
            }
            else
            {
                Debug.LogWarning($"FlyingEntity {gameObject.name} couldn't find PowerBitPlayerController on player!");
                StartFleeing();
            }
        }
    }
    
    private void StartFleeing()
    {
        // Choose random flee direction: either left or right
        float horizontalDirection = Random.value > 0.5f ? -1f : 1f;
        
        // Calculate flee target at 30 degrees upward angle
        float fleeAngleRadians = 30f * Mathf.Deg2Rad; // Convert 30 degrees to radians
        float fleeX = transform.position.x + (horizontalDirection * fleeDistance * Mathf.Cos(fleeAngleRadians));
        float fleeY = transform.position.y + (fleeDistance * Mathf.Sin(fleeAngleRadians));
        
        fleeTarget = new Vector3(fleeX, fleeY, transform.position.z);
        
        isFleeing = true;
        isFollowing = false;
        
        Debug.Log($"FlyingEntity {gameObject.name} fleeing at 30° angle to ({fleeX:F1}, {fleeY:F1})");
    }
    
    private void HandleFleeing()
    {
        if (fleeTarget == Vector3.zero) return;
        
        Vector2 directionToFleeTarget = (fleeTarget - transform.position).normalized;
        float distanceToFleeTarget = Vector2.Distance(transform.position, fleeTarget);
        
        // Check if we've moved far enough to despawn
        float currentX = transform.position.x;
        if (Mathf.Abs(currentX) >= fleeDistance * 0.9f) // 90% of flee distance
        {
            Debug.Log($"FlyingEntity {gameObject.name} despawning at x = {currentX}");
            Destroy(gameObject);
            return;
        }
        
        // Move towards flee target
        if (distanceToFleeTarget > 1f)
        {
            rb.AddForce(directionToFleeTarget * fleeSpeed, ForceMode2D.Force);
            
            // Handle sprite flipping while fleeing
            if (enableSpriteFlipping && spriteRenderer != null)
            {
                HandleSpriteFlipping(directionToFleeTarget.x);
            }
            
            // Limit flee speed
            if (rb.linearVelocity.magnitude > fleeSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * fleeSpeed;
            }
        }
        else
        {
            // Reached flee target, despawn
            Debug.Log($"FlyingEntity {gameObject.name} reached flee target and despawning");
            Destroy(gameObject);
        }
    }

    // Bit dropping functionality
    private void DropRandomBit()
    {
        // Get a random bit from BitManager
        if (BitManager.Instance != null)
        {
            Bit randomBit = BitManager.Instance.GetRandomBit();
            if (randomBit != null)
            {
                // Create bit drop at entity's position
                Vector3 dropPosition = transform.position + Vector3.up * 0.5f; // Slightly above the entity
                
                // Use direct instantiation to create bit drop
                if (BitByBit.Items.BitDrop.PrefabReference != null)
                {
                    GameObject bitDropObj = Instantiate(BitByBit.Items.BitDrop.PrefabReference, dropPosition, Quaternion.identity);
                    BitByBit.Items.BitDrop bitDropComponent = bitDropObj.GetComponent<BitByBit.Items.BitDrop>();
                    if (bitDropComponent != null)
                    {
                        bitDropComponent.SetBitData(randomBit);
                        Debug.Log($"FlyingEntity {gameObject.name} dropped {randomBit.BitName} (Type: {randomBit.BitType}, Rarity: {randomBit.Rarity})!");
                    }
                    else
                    {
                        Debug.LogError("BitDrop prefab doesn't have BitDrop component!");
                        Destroy(bitDropObj);
                    }
                }
                else
                {
                    Debug.LogError("BitDrop prefab reference not set! Cannot create bit drop.");
                }
            }
            else
            {
                Debug.LogWarning("BitManager returned null bit for drop!");
            }
        }
        else
        {
            Debug.LogWarning("BitManager not found! Cannot drop bit!");
        }
    }

    // IDamageable implementation
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"FlyingEntity {gameObject.name} took {damage} damage! Health: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0)
        {
            Debug.Log($"FlyingEntity {gameObject.name} died!");
            
            // Drop a random bit on death
            DropRandomBit();
            
            // Destroy the entity
            Destroy(gameObject);
        }
    }
} 