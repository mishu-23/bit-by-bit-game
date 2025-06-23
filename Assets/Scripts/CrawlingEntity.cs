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
    
    [Header("Acceleration")]
    public float accelerationMultiplier = 2f; // Multiplier for faster acceleration
    public float initialBoostForce = 12f; // Extra force when starting to move
    public float accelerationTime = 0.5f; // Time to reach full acceleration

    [Header("Combat")]
    public int maxHealth = 10;
    public int currentHealth;

    [Header("Behavior")]
    public bool isFollowing = false;
    public float followDelay = 0.5f; // Delay before starting to follow
    public float fleeDistance = 8f; // How far to run away from player
    public float fleeSpeed = 6f; // Speed when fleeing
    public float playerOffset = 0.5f; // Distance to maintain from player

    [Header("Attached Bit Drop")]
    public Vector3 bitDropOffset = new Vector3(0f, 1.5f, 0f); // Offset above the entity
    public float bitDropBobAmount = 0.2f; // How much the bit bobs up and down
    public float bitDropBobSpeed = 2f; // Speed of the bobbing motion

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private bool hasCollidedWithPlayer = false;
    private float lastMoveDirection = 1f; // 1 for right, -1 for left
    private float accelerationTimer = 0f;
    private bool wasMoving = false;
    private bool isFleeing = false;
    private Vector3 fleeTarget;
    
    // Attached bit drop
    private GameObject attachedBitDrop;
    private Bit attachedBitData;
    private Vector3 originalBitDropOffset;
    private float bobTimer = 0f;

    private void Awake()
    {
        gameObject.layer = 6; // Entity layer
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
        
        // Set up trigger collider for player detection
        SetupTriggerCollider();
    }

    private void SetupTriggerCollider()
    {
        // Create a trigger collider for player detection
        BoxCollider2D triggerCollider = gameObject.AddComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.size = new Vector2(1.5f, 1.5f); // Slightly larger than the main collider
        triggerCollider.offset = Vector2.zero;
        
        // Set the trigger to only detect the player layer
        triggerCollider.includeLayers = 1 << 3; // Player layer (3)
        triggerCollider.excludeLayers = 0;
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
        
        // Don't create attached bit drop initially - will steal one when fleeing
        // CreateAttachedBitDrop();
    }
    
    private void CreateAttachedBitDrop()
    {
        // Get a random bit for the attached drop
        if (BitManager.Instance != null)
        {
            attachedBitData = BitManager.Instance.GetRandomBit();
            if (attachedBitData != null)
            {
                // Create a simple bit drop object (without physics or collection)
                attachedBitDrop = new GameObject($"AttachedBitDrop_{attachedBitData.bitName}");
                attachedBitDrop.transform.SetParent(transform);
                
                // Add sprite renderer
                SpriteRenderer bitSprite = attachedBitDrop.AddComponent<SpriteRenderer>();
                bitSprite.sprite = attachedBitData.GetSprite();
                bitSprite.sortingOrder = 1; // Render above the entity
                
                // Set initial position
                originalBitDropOffset = bitDropOffset;
                UpdateAttachedBitDropPosition();
                
                Debug.Log($"CrawlingEntity {gameObject.name} created attached bit drop: {attachedBitData.bitName}");
            }
        }
    }
    
    private void UpdateAttachedBitDropPosition()
    {
        if (attachedBitDrop != null)
        {
            // Calculate bobbing motion
            bobTimer += Time.deltaTime * bitDropBobSpeed;
            float bobOffset = Mathf.Sin(bobTimer) * bitDropBobAmount;
            
            // Set position with offset and bobbing
            Vector3 targetPosition = originalBitDropOffset + new Vector3(0f, bobOffset, 0f);
            attachedBitDrop.transform.localPosition = targetPosition;
        }
    }

    private void FixedUpdate()
    {
        // Keep at ground level
        if (Mathf.Abs(transform.position.y - groundY) > 0.1f)
        {
            transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
        }

        if (playerTarget == null) return;

        // Handle fleeing behavior
        if (isFleeing)
        {
            HandleFleeing();
            return;
        }

        // Don't follow if we've collided with player (we'll flee instead)
        if (hasCollidedWithPlayer) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);

        // Check if player is in detection range
        if (distanceToPlayer <= detectionRange && distanceToPlayer > minFollowDistance)
        {
            if (!isFollowing)
            {
                Debug.Log($"CrawlingEntity {gameObject.name} detected player at distance {distanceToPlayer:F1}, starting to follow...");
                isFollowing = true;
                accelerationTimer = 0f; // Reset acceleration timer when starting to follow
            }

            // Calculate target position with offset
            float direction = Mathf.Sign(playerTarget.position.x - transform.position.x);
            Vector3 targetPosition = playerTarget.position + new Vector3(direction * playerOffset, 0, 0);
            float distanceToTarget = Vector2.Distance(transform.position, targetPosition);
            
            // Move towards target position (player position + offset)
            float currentMoveForce = moveForce;
            
            // Apply acceleration multiplier
            currentMoveForce *= accelerationMultiplier;
            
            // Apply initial boost if just started moving or changed direction
            if (!wasMoving || Mathf.Sign(direction) != Mathf.Sign(lastMoveDirection))
            {
                currentMoveForce += initialBoostForce;
                accelerationTimer = 0f;
            }
            
            // Gradually increase force over time for smoother acceleration
            accelerationTimer += Time.fixedDeltaTime;
            float accelerationProgress = Mathf.Clamp01(accelerationTimer / accelerationTime);
            currentMoveForce *= (1f + accelerationProgress * 0.5f); // Up to 50% more force over time
            
            // Apply the calculated force towards the target position
            Vector2 moveDirection = (targetPosition - transform.position).normalized;
            rb.AddForce(moveDirection * currentMoveForce, ForceMode2D.Force);
            
            // Update last move direction for sprite flipping
            if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
            {
                lastMoveDirection = Mathf.Sign(rb.linearVelocity.x);
            }
            
            // Flip sprite to face movement direction
            FlipSpriteToFaceDirection();
            
            // Limit speed
            if (Mathf.Abs(rb.linearVelocity.x) > maxSpeed)
            {
                rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * maxSpeed, 0f);
            }

            wasMoving = true;
            Debug.Log($"CrawlingEntity {gameObject.name} following player with offset, distance to target: {distanceToTarget:F1}");
        }
        else if (distanceToPlayer <= minFollowDistance + playerOffset)
        {
            // Stop when close enough to player (accounting for offset)
            rb.linearVelocity = Vector2.zero;
            isFollowing = false;
            wasMoving = false;
            accelerationTimer = 0f;
            Debug.Log($"CrawlingEntity {gameObject.name} reached player with offset, stopping at distance {distanceToPlayer:F1}");
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
            wasMoving = false;
            accelerationTimer = 0f;
        }
    }

    private void HandleFleeing()
    {
        // Calculate distance to flee target
        float distanceToFleeTarget = Mathf.Abs(transform.position.x - fleeTarget.x);
        
        if (distanceToFleeTarget > 0.5f)
        {
            // Move towards flee target
            float direction = Mathf.Sign(fleeTarget.x - transform.position.x);
            
            // Use flee speed for faster movement
            float currentMoveForce = fleeSpeed * accelerationMultiplier;
            
            // Apply the force
            rb.AddForce(new Vector2(direction * currentMoveForce, 0f), ForceMode2D.Force);
            
            // Update last move direction for sprite flipping
            if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
            {
                lastMoveDirection = Mathf.Sign(rb.linearVelocity.x);
            }
            
            // Flip sprite to face movement direction
            FlipSpriteToFaceDirection();
            
            // Limit speed to flee speed
            if (Mathf.Abs(rb.linearVelocity.x) > fleeSpeed)
            {
                rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * fleeSpeed, 0f);
            }
            
            Debug.Log($"CrawlingEntity {gameObject.name} fleeing to x = {fleeTarget.x:F1}, distance: {distanceToFleeTarget:F1}");
        }
        else
        {
            // Reached flee target, despawn the entity
            Debug.Log($"CrawlingEntity {gameObject.name} reached flee target at x = {fleeTarget.x:F1}, despawning");
            Destroy(gameObject);
        }
    }

    private void FlipSpriteToFaceDirection()
    {
        if (spriteRenderer != null)
        {
            // Flip sprite based on movement direction
            // When moving right (positive), don't flip (flipX = false)
            // When moving left (negative), flip horizontally (flipX = true)
            spriteRenderer.flipX = lastMoveDirection < 0;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if we hit the player
        if (other.CompareTag("Player") || other.gameObject.layer == 3)
        {
            hasCollidedWithPlayer = true;
            isFollowing = false;
            
            // Try to steal a bit from the player's build
            StealBitFromPlayer();
            
            // Choose random flee target: either x = -40 or x = 40
            float fleeX = Random.value > 0.5f ? -40f : 40f;
            fleeTarget = new Vector3(fleeX, groundY, transform.position.z);
            
            // Start fleeing
            isFleeing = true;
            accelerationTimer = 0f;
            
            Debug.Log($"CrawlingEntity {gameObject.name} detected player! Fleeing to x = {fleeX}");
        }
    }
    
    private void StealBitFromPlayer()
    {
        // Don't steal if we already have a bit attached
        if (attachedBitData != null)
        {
            Debug.Log($"CrawlingEntity {gameObject.name} already has a bit attached ({attachedBitData.bitName}), won't steal another one!");
            return;
        }
        
        // Try to get the player controller to access the build
        if (playerTarget != null)
        {
            PowerBitPlayerController playerController = playerTarget.GetComponent<PowerBitPlayerController>();
            if (playerController != null)
            {
                // Try to steal a random bit from the player's build
                Bit stolenBit = playerController.StealRandomBitFromBuild();
                if (stolenBit != null)
                {
                    // Create the attached bit drop with the stolen bit
                    CreateAttachedBitDropWithBit(stolenBit);
                    Debug.Log($"CrawlingEntity {gameObject.name} stole {stolenBit.bitName} from player's build!");
                }
                else
                {
                    Debug.Log($"CrawlingEntity {gameObject.name} tried to steal a bit but player's build is empty!");
                }
            }
            else
            {
                Debug.LogWarning($"CrawlingEntity {gameObject.name} couldn't find PowerBitPlayerController on player!");
            }
        }
    }
    
    private void CreateAttachedBitDropWithBit(Bit bit)
    {
        if (bit != null)
        {
            attachedBitData = bit;
            
            // Create a simple bit drop object (without physics or collection)
            attachedBitDrop = new GameObject($"AttachedBitDrop_{attachedBitData.bitName}");
            attachedBitDrop.transform.SetParent(transform);
            
            // Add sprite renderer
            SpriteRenderer bitSprite = attachedBitDrop.AddComponent<SpriteRenderer>();
            bitSprite.sprite = attachedBitData.GetSprite();
            bitSprite.sortingOrder = 1; // Render above the entity
            
            // Set initial position
            originalBitDropOffset = bitDropOffset;
            UpdateAttachedBitDropPosition();
            
            Debug.Log($"CrawlingEntity {gameObject.name} created attached bit drop: {attachedBitData.bitName}");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Only handle non-player collisions here (like walls, ground, etc.)
        // Player detection is now handled by the trigger collider
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.layer == 3)
        {
            // This shouldn't happen anymore since we're using a trigger for player detection
            Debug.Log($"CrawlingEntity {gameObject.name} unexpected collision with player!");
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
        isFleeing = false;
        rb.linearVelocity = Vector2.zero;
        accelerationTimer = 0f;
        wasMoving = false;
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
            
            // Drop the stolen bit if we have one, otherwise drop a random bit
            if (attachedBitData != null)
            {
                DropStolenBit();
                DropRandomBit();

            }
            else
            {
                DropRandomBit();
            }
            
            Destroy(gameObject);
        }
    }
    
    private void DropStolenBit()
    {
        if (attachedBitData != null)
        {
            // Create a real bit drop at entity's position
            Vector3 dropPosition = transform.position + Vector3.up * 0.5f; // Slightly above the entity
            BitDrop.CreateBitDrop(attachedBitData, dropPosition);
            Debug.Log($"CrawlingEntity {gameObject.name} dropped stolen bit: {attachedBitData.bitName} (Type: {attachedBitData.bitType}, Rarity: {attachedBitData.rarity})!");
        }
    }
    
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
                BitDrop.CreateBitDrop(randomBit, dropPosition);
                Debug.Log($"CrawlingEntity {gameObject.name} dropped {randomBit.bitName} (Type: {randomBit.bitType}, Rarity: {randomBit.rarity})!");
            }
            else
            {
                Debug.LogWarning("BitManager returned null bit for drop!");
            }
        }
        else
        {
            Debug.LogWarning("BitManager not found! Cannot drop bit.");
        }
    }

    private void Update()
    {
        // Update attached bit drop position
        UpdateAttachedBitDropPosition();
    }
} 