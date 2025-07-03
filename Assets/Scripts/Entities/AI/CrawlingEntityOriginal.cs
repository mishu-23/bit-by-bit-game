using UnityEngine;
using System.Collections;
using BitByBit.Core;

public class CrawlingEntityOriginal : MonoBehaviour, IDamageable
{
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogging = true; // Toggle to control debug messages
    
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
    
    // Mechanic selection
    private enum StealMechanic
    {
        StealFromPlayer,
        StealFromDeposit,
        FollowGatherer
    }
    private StealMechanic chosenMechanic;
    private Transform depositTarget;
    private bool isMovingToDeposit = false;
    
    // Gatherer following mechanic
    private Transform gathererTarget;
    private bool isFollowingGatherer = false;
    private float gathererDetectionRange = 15f;
    private float gathererSearchInterval = 2f; // Search for gatherers every 2 seconds
    private float lastGathererSearchTime = 0f;
    
    // Gatherer carrying mechanic
    private GameObject carriedGatherer;
    private bool isCarryingGatherer = false;
    private Vector3 gathererCarryOffset = new Vector3(0f, 1.2f, 0f); // Offset for carried gatherer
    private float gathererTakeDistance = 1.5f; // Distance at which to take the gatherer

    // Helper method for conditional debug logging
    private void DebugLog(string message)
    {
        if (enableDebugLogging)
        {
            Debug.Log(message);
        }
    }
    
    private void DebugLogWarning(string message)
    {
        if (enableDebugLogging)
        {
            Debug.LogWarning(message);
        }
    }
    
    private void DebugLogError(string message)
    {
        if (enableDebugLogging)
        {
            Debug.LogError(message);
        }
    }

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
        
        // Choose which mechanic to use - random between all 3
        float randomValue = Random.value;
        if (randomValue < 0.33f)
        {
            chosenMechanic = StealMechanic.StealFromPlayer;
        }
        else if (randomValue < 0.66f)
        {
            chosenMechanic = StealMechanic.StealFromDeposit;
        }
        else
        {
            chosenMechanic = StealMechanic.FollowGatherer;
        }
        
        DebugLog($"CrawlingEntity {gameObject.name} chose mechanic: {chosenMechanic}");
        
        // Find player if not assigned
        if (playerTarget == null)
        {
            InitializePlayerTarget();
        }
        
        // Find deposit if needed for deposit stealing mechanic
        if (chosenMechanic == StealMechanic.StealFromDeposit)
        {
            InitializeDepositTarget();
        }
        
        // Find gatherer for following mechanic
        GathererEntity gatherer = FindObjectOfType<GathererEntity>();
        if (gatherer != null)
        {
            gathererTarget = gatherer.transform;
            isFollowingGatherer = true;
            DebugLog($"CrawlingEntity {gameObject.name} found gatherer: {gatherer.name} at position {gatherer.transform.position}");
        }
        else
        {
            DebugLogError($"CrawlingEntity {gameObject.name} couldn't find any GathererEntity in the scene!");
        }
        
        // Don't create attached bit drop initially - will steal one when fleeing
        // CreateAttachedBitDrop();
    }
    
    private void InitializePlayerTarget()
    {
        // Use GameReferences for better performance
        if (GameReferences.Instance != null && GameReferences.Instance.Player != null)
        {
            playerTarget = GameReferences.Instance.Player;
            DebugLog($"CrawlingEntity {gameObject.name} found player via GameReferences: {playerTarget.name}");
        }
        else
        {
            // Fallback: try to find player with tag if GameReferences fails
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
                DebugLog($"CrawlingEntity {gameObject.name} found player via fallback: {player.name}");
                DebugLogWarning("CrawlingEntity: Found player via fallback method. Please ensure GameReferences is properly configured.");
            }
            else
            {
                DebugLogError($"CrawlingEntity {gameObject.name} couldn't find player with tag 'Player'!");
            }
        }
    }
    
    private void InitializeDepositTarget()
    {
        // Use GameReferences for better performance
        if (GameReferences.Instance != null && GameReferences.Instance.Deposit != null)
        {
            depositTarget = GameReferences.Instance.Deposit.transform;
            DebugLog($"CrawlingEntity {gameObject.name} found deposit via GameReferences at: {depositTarget.position}");
            // Start moving to deposit immediately
            StartMovingToDeposit();
        }
        else
        {
            // Fallback: try to find deposit if GameReferences fails
            GameObject deposit = GameObject.Find("Deposit");
            if (deposit != null)
            {
                depositTarget = deposit.transform;
                DebugLog($"CrawlingEntity {gameObject.name} found deposit via fallback at: {depositTarget.position}");
                DebugLogWarning("CrawlingEntity: Found deposit via fallback method. Please ensure GameReferences is properly configured.");
                // Start moving to deposit immediately
                StartMovingToDeposit();
            }
            else
            {
                DebugLogWarning($"CrawlingEntity {gameObject.name} couldn't find deposit! Falling back to player stealing.");
                chosenMechanic = StealMechanic.StealFromPlayer;
            }
        }
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
                attachedBitDrop = new GameObject($"AttachedBitDrop_{attachedBitData.BitName}");
                attachedBitDrop.transform.SetParent(transform);
                
                // Add sprite renderer
                SpriteRenderer bitSprite = attachedBitDrop.AddComponent<SpriteRenderer>();
                bitSprite.sprite = attachedBitData.GetSprite();
                bitSprite.sortingOrder = 1; // Render above the entity
                
                // Set initial position
                originalBitDropOffset = bitDropOffset;
                UpdateAttachedBitDropPosition();
                
                DebugLog($"CrawlingEntity {gameObject.name} created attached bit drop: {attachedBitData.BitName}");
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
                                        DebugLog($"CrawlingEntity {gameObject.name} detected player at distance {distanceToPlayer:F1}, starting to follow...");
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
                                DebugLog($"CrawlingEntity {gameObject.name} following player with offset, distance to target: {distanceToTarget:F1}");
        }
        else if (distanceToPlayer <= minFollowDistance + playerOffset)
        {
            // Stop when close enough to player (accounting for offset)
            rb.linearVelocity = Vector2.zero;
            isFollowing = false;
            wasMoving = false;
            accelerationTimer = 0f;
                            DebugLog($"CrawlingEntity {gameObject.name} reached player with offset, stopping at distance {distanceToPlayer:F1}");
        }
        else if (distanceToPlayer > detectionRange)
        {
            // Player too far, stop following
            if (isFollowing)
            {
                DebugLog($"CrawlingEntity {gameObject.name} lost player, stopping (distance: {distanceToPlayer:F1})");
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
        if (fleeTarget == Vector3.zero) return;
        
        float distanceToFleeTarget = Vector3.Distance(transform.position, fleeTarget);
        
        // Check if we've reached the flee target
        if (distanceToFleeTarget < 1f)
        {
            // Drop the stolen bit and destroy entity
            DropStolenBit();
            Destroy(gameObject);
            return;
        }
        
        // Move towards flee target
        MoveTowardsTarget(fleeTarget, fleeSpeed);
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
        if (other.CompareTag("Player") && !hasCollidedWithPlayer)
        {
            hasCollidedWithPlayer = true;
            
            if (chosenMechanic == StealMechanic.StealFromPlayer)
            {
                // Original mechanic: steal from player's build
                StealBitFromPlayer();
                // Start fleeing after stealing
                StartFleeing();
            }
            else if (chosenMechanic == StealMechanic.StealFromDeposit)
            {
                // If we haven't reached the deposit yet, start moving to it
                if (!isMovingToDeposit && depositTarget != null)
                {
                    StartMovingToDeposit();
                }
            }
        }
    }
    
    private void StealBitFromPlayer()
    {
        // Don't steal if we already have a bit attached
        if (attachedBitData != null)
        {
            DebugLog($"CrawlingEntity {gameObject.name} already has a bit attached ({attachedBitData.BitName}), won't steal another one!");
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
                    DebugLog($"CrawlingEntity {gameObject.name} stole {stolenBit.BitName} from player's build!");
                }
                else
                {
                    DebugLog($"CrawlingEntity {gameObject.name} tried to steal a bit but player's build is empty!");
                }
            }
            else
            {
                DebugLogWarning($"CrawlingEntity {gameObject.name} couldn't find PowerBitPlayerController on player!");
            }
        }
    }
    
    private void CreateAttachedBitDropWithBit(Bit bit)
    {
        if (bit != null)
        {
            attachedBitData = bit;
            
            // Create a simple bit drop object (without physics or collection)
            attachedBitDrop = new GameObject($"AttachedBitDrop_{attachedBitData.BitName}");
            attachedBitDrop.transform.SetParent(transform);
            
            // Add sprite renderer
            SpriteRenderer bitSprite = attachedBitDrop.AddComponent<SpriteRenderer>();
            bitSprite.sprite = attachedBitData.GetSprite();
            bitSprite.sortingOrder = 1; // Render above the entity
            
            // Set initial position
            originalBitDropOffset = bitDropOffset;
            UpdateAttachedBitDropPosition();
            
            DebugLog($"CrawlingEntity {gameObject.name} created attached bit drop: {attachedBitData.BitName}");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Only handle non-player collisions here (like walls, ground, etc.)
        // Player detection is now handled by the trigger collider
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.layer == 3)
        {
            // This shouldn't happen anymore since we're using a trigger for player detection
            DebugLog($"CrawlingEntity {gameObject.name} unexpected collision with player!");
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
        if (isFollowing || isFollowingGatherer)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
        else
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, 0.2f);
        }

        // Draw line to player if following player
        if (isFollowing && playerTarget != null && chosenMechanic == StealMechanic.StealFromPlayer)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, playerTarget.position);
        }
        
        // Draw line to deposit if moving to deposit
        if (isMovingToDeposit && depositTarget != null && chosenMechanic == StealMechanic.StealFromDeposit)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, depositTarget.position);
        }
        
        // Draw line to gatherer if following gatherer
        if (isFollowingGatherer && gathererTarget != null && chosenMechanic == StealMechanic.FollowGatherer)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, gathererTarget.position);
        }
        
        // Show if carrying a gatherer
        if (isCarryingGatherer)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(transform.position + gathererCarryOffset, Vector3.one * 0.5f);
        }
    }

    // Public method to reset the entity (useful for respawning)
    public void ResetEntity()
    {
        hasCollidedWithPlayer = false;
        isFollowing = false;
        isFollowingGatherer = false;
        isFleeing = false;
        isMovingToDeposit = false;
        
        // Drop gatherer if carrying one
        if (isCarryingGatherer)
        {
            DropGatherer();
        }
        
        rb.linearVelocity = Vector2.zero;
        accelerationTimer = 0f;
        wasMoving = false;
        DebugLog($"CrawlingEntity {gameObject.name} has been reset");
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
        DebugLog($"CrawlingEntity {gameObject.name} took {damage} damage! Health: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0)
        {
            DebugLog($"CrawlingEntity {gameObject.name} destroyed!");
            
            // Drop the carried gatherer if we have one
            if (isCarryingGatherer)
            {
                DropGatherer();
            }
            
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
            
            // Use direct instantiation instead of static method to avoid timing issues
            if (BitByBit.Items.BitDrop.PrefabReference != null)
            {
                GameObject bitDropObj = Instantiate(BitByBit.Items.BitDrop.PrefabReference, dropPosition, Quaternion.identity);
                BitByBit.Items.BitDrop bitDropComponent = bitDropObj.GetComponent<BitByBit.Items.BitDrop>();
                if (bitDropComponent != null)
                {
                    bitDropComponent.SetBitData(attachedBitData);
                    DebugLog($"CrawlingEntity {gameObject.name} dropped stolen bit: {attachedBitData.BitName} (Type: {attachedBitData.BitType}, Rarity: {attachedBitData.Rarity})!");
                }
                else
                {
                    DebugLogError("BitDrop prefab doesn't have BitDrop component!");
                    Destroy(bitDropObj);
                }
            }
            else
            {
                DebugLogError("BitDrop prefab reference not set! Cannot create bit drop.");
            }
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
                
                // Use direct instantiation instead of static method to avoid timing issues
                if (BitByBit.Items.BitDrop.PrefabReference != null)
                {
                    GameObject bitDropObj = Instantiate(BitByBit.Items.BitDrop.PrefabReference, dropPosition, Quaternion.identity);
                    BitByBit.Items.BitDrop bitDropComponent = bitDropObj.GetComponent<BitByBit.Items.BitDrop>();
                    if (bitDropComponent != null)
                    {
                        bitDropComponent.SetBitData(randomBit);
                        DebugLog($"CrawlingEntity {gameObject.name} dropped {randomBit.BitName} (Type: {randomBit.BitType}, Rarity: {randomBit.Rarity})!");
                    }
                    else
                    {
                        DebugLogError("BitDrop prefab doesn't have BitDrop component!");
                        Destroy(bitDropObj);
                    }
                }
                else
                {
                    DebugLogError("BitDrop prefab reference not set! Cannot create bit drop.");
                }
            }
            else
            {
                DebugLogWarning("BitManager returned null bit for drop!");
            }
        }
        else
        {
            DebugLogWarning("BitManager not found! Cannot drop bit!");
        }
    }

    private void StartMovingToDeposit()
    {
        if (depositTarget != null)
        {
            isMovingToDeposit = true;
            DebugLog($"CrawlingEntity {gameObject.name} starting to move to deposit");
        }
    }

    private void Update()
    {
        if (isFleeing)
        {
            HandleFleeing();
        }
        else if (isMovingToDeposit)
        {
            HandleMovingToDeposit();
        }
        else if (chosenMechanic == StealMechanic.FollowGatherer)
        {
            // Always try to handle gatherer following for this mechanic, regardless of current state
            HandleGathererFollowing();
        }
        else
        {
            // Handle normal behavior (player following for StealFromPlayer mechanic)
            HandleNormalBehavior();
        }
        
        // Update attached bit drop bobbing
        UpdateAttachedBitDropPosition();
    }

    private void HandleMovingToDeposit()
    {
        if (depositTarget == null) return;
        
        Vector3 directionToDeposit = (depositTarget.position - transform.position).normalized;
        float distanceToDeposit = Vector3.Distance(transform.position, depositTarget.position);
        
        // Check if we've reached the deposit
        if (distanceToDeposit < 2.0f)
        {
            // Steal from deposit and start fleeing
            DebugLog($"CrawlingEntity {gameObject.name} reached deposit, stealing and fleeing");
            StealFromDeposit();
            StartFleeing();
            return;
        }
        
        // Move towards deposit
        MoveTowardsTarget(depositTarget.position, moveForce);
    }
    
    private void HandleGathererFollowing()
    {
        // Periodic search for gatherers (especially useful for gatherers spawned later)
        bool shouldSearchForGatherer = gathererTarget == null || 
                                       (Time.time - lastGathererSearchTime) > gathererSearchInterval;
        
        if (shouldSearchForGatherer)
        {
            lastGathererSearchTime = Time.time;
            
            // Try to find a gatherer
            GathererEntity gatherer = FindObjectOfType<GathererEntity>();
            if (gatherer != null)
            {
                if (gathererTarget == null)
                {
                    gathererTarget = gatherer.transform;
                    isFollowingGatherer = true;
                    DebugLog($"CrawlingEntity {gameObject.name} found new gatherer: {gatherer.name} at {gatherer.transform.position}");
                }
                else if (gathererTarget != gatherer.transform)
                {
                    // Found a different gatherer, switch to it
                    gathererTarget = gatherer.transform;
                    DebugLog($"CrawlingEntity {gameObject.name} switched to gatherer: {gatherer.name}");
                }
            }
            else
            {
                if (gathererTarget != null)
                {
                    DebugLogWarning($"CrawlingEntity {gameObject.name} lost gatherer target, will keep searching...");
                    gathererTarget = null;
                    isFollowingGatherer = false;
                }
            }
        }
        
        // If we have a target, follow it (unless we're already carrying one)
        if (gathererTarget != null && !isCarryingGatherer)
        {
            float distanceToGatherer = Vector3.Distance(transform.position, gathererTarget.position);
            
            // Check if close enough to take the gatherer
            if (distanceToGatherer <= gathererTakeDistance)
            {
                TakeGatherer(gathererTarget.gameObject);
            }
            else if (distanceToGatherer > minFollowDistance)
            {
                // Move towards gatherer
                MoveTowardsTarget(gathererTarget.position, moveForce);
                
                // Visual feedback
                if (!isFollowing)
                {
                    isFollowing = true;
                    DebugLog($"CrawlingEntity {gameObject.name} started following gatherer at distance {distanceToGatherer:F1}");
                }
            }
            else
            {
                // Close to gatherer but not close enough to take, stop moving
                StopMovement();
            }
        }
        else if (isCarryingGatherer)
        {
            // We're carrying a gatherer, just update its position and stop following
            UpdateCarriedGathererPosition();
            StopMovement();
            isFollowing = false;
        }
        else
        {
            // No gatherer found, stop movement and wait for next search
            StopMovement();
            isFollowing = false;
        }
    }
    
    private void HandleNormalBehavior()
    {
        if (playerTarget == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        
        // Check if player is in detection range
        if (distanceToPlayer <= detectionRange && !hasCollidedWithPlayer)
        {
            if (!isFollowing)
            {
                StartCoroutine(StartFollowing());
            }
            
            if (isFollowing)
            {
                // Move towards player
                MoveTowardsTarget(playerTarget.position, moveForce);
            }
        }
        else
        {
            // Player out of range, stop following
            isFollowing = false;
            StopMovement();
        }
    }

    private void StealFromDeposit()
    {
        if (attachedBitDrop != null) return; // Already carrying a bit
        
        // Try to steal a core bit from the deposit
        DepositInteraction deposit = depositTarget?.GetComponent<DepositInteraction>();
        if (deposit != null && deposit.RemoveCoreBit())
        {
            // Create a core bit to carry
                            Bit coreBit = Bit.CreateBit("Common CoreBit", BitType.CoreBit, Rarity.Common, 0, 0f);
            
            CreateAttachedBitDropWithBit(coreBit);
            DebugLog($"CrawlingEntity {gameObject.name} stole a core bit from deposit!");
        }
        else
        {
            DebugLog($"CrawlingEntity {gameObject.name} couldn't steal from deposit - no core bits available");
        }
    }
    
    private void MoveTowardsTarget(Vector3 targetPosition, float force)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        
        // Apply movement force
        rb.AddForce(new Vector2(direction.x * force, 0f), ForceMode2D.Force);
        
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
            rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * maxSpeed, rb.linearVelocity.y);
        }
    }

    private void StartFleeing()
    {
        // Choose random flee target: either x = -40 or x = 40
        float fleeX = Random.value > 0.5f ? -40f : 40f;
        fleeTarget = new Vector3(fleeX, groundY, transform.position.z);
        
        // Start fleeing
        isFleeing = true;
        isMovingToDeposit = false; // Stop moving to deposit
        accelerationTimer = 0f;
        
        DebugLog($"CrawlingEntity {gameObject.name} fleeing to x = {fleeX}");
    }
    
    private IEnumerator StartFollowing()
    {
        yield return new WaitForSeconds(followDelay);
        isFollowing = true;
        DebugLog($"CrawlingEntity {gameObject.name} started following player");
    }
    
    private void StopMovement()
    {
        // Gradually slow down the entity
        if (rb.linearVelocity.magnitude > 0.1f)
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.deltaTime * 5f);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
    
    private void TakeGatherer(GameObject gatherer)
    {
        if (gatherer == null || isCarryingGatherer) return;
        
        DebugLog($"CrawlingEntity {gameObject.name} is taking gatherer: {gatherer.name}");
        
        // Store reference to the gathered object
        carriedGatherer = gatherer;
        isCarryingGatherer = true;
        
        // Disable the gatherer's behavior
        GathererEntity gathererEntity = gatherer.GetComponent<GathererEntity>();
        if (gathererEntity != null)
        {
            gathererEntity.enabled = false; // Disable the gatherer's AI
        }
        
        // Disable gatherer's physics
        Rigidbody2D gathererRb = gatherer.GetComponent<Rigidbody2D>();
        if (gathererRb != null)
        {
            gathererRb.simulated = false; // Disable physics simulation
        }
        
        // Set gatherer as child of crawling entity (so it moves with us)
        gatherer.transform.SetParent(transform);
        
        // Position the gatherer above the crawling entity
        UpdateCarriedGathererPosition();
        
        // Clear the target since we've taken it
        gathererTarget = null;
        isFollowingGatherer = false;
        
        // Start fleeing with the captured gatherer (like the other stealing mechanics)
        StartFleeing();
        
        DebugLog($"CrawlingEntity {gameObject.name} successfully took gatherer {gatherer.name} and is now fleeing!");
    }
    
    private void UpdateCarriedGathererPosition()
    {
        if (carriedGatherer != null)
        {
            // Set the carried gatherer's position relative to the crawling entity
            carriedGatherer.transform.localPosition = gathererCarryOffset;
        }
    }
    
    private void DropGatherer()
    {
        if (!isCarryingGatherer || carriedGatherer == null) return;
        
        DebugLog($"CrawlingEntity {gameObject.name} is dropping gatherer: {carriedGatherer.name}");
        
        // Remove from parent
        carriedGatherer.transform.SetParent(null);
        
        // Position the gatherer at ground level near the crawling entity
        Vector3 dropPosition = transform.position + Vector3.right * 2f; // Drop 2 units to the right
        dropPosition.y = groundY; // Set to ground level
        carriedGatherer.transform.position = dropPosition;
        
        // Re-enable gatherer's physics
        Rigidbody2D gathererRb = carriedGatherer.GetComponent<Rigidbody2D>();
        if (gathererRb != null)
        {
            gathererRb.simulated = true; // Re-enable physics simulation
        }
        
        // Re-enable the gatherer's behavior
        GathererEntity gathererEntity = carriedGatherer.GetComponent<GathererEntity>();
        if (gathererEntity != null)
        {
            gathererEntity.enabled = true; // Re-enable the gatherer's AI
        }
        
        // Clear carrying state
        carriedGatherer = null;
        isCarryingGatherer = false;
        
        DebugLog($"CrawlingEntity {gameObject.name} dropped gatherer successfully!");
    }
} 