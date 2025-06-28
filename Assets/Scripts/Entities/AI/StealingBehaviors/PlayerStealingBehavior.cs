using UnityEngine;

[System.Serializable]
public class PlayerStealingBehavior : IStealingBehavior
{
    #region Configuration
    
    private float detectionRange;
    private float minFollowDistance;
    private float followDelay;
    
    #endregion

    #region Private Fields
    
    private CrawlingEntity entity;
    private CrawlingEntityMovement movement;
    private BitCarrier bitCarrier;
    
    private Transform playerTarget;
    private bool isActive;
    private bool isComplete;
    private bool isFollowing;
    private bool hasCollidedWithPlayer;
    private float followTimer;
    
    #endregion

    #region IStealingBehavior Implementation
    
    public bool IsActive => isActive;
    public bool IsComplete => isComplete;
    public string BehaviorName => "Player Stealing";
    
    public void Initialize(CrawlingEntity entity)
    {
        this.entity = entity;
        movement = entity.GetComponent<CrawlingEntityMovement>();
        bitCarrier = entity.GetComponent<BitCarrier>();
        
        FindPlayerTarget();
        Reset();
    }
    
    public void Configure(float detectionRange, float minFollowDistance, float followDelay)
    {
        this.detectionRange = detectionRange;
        this.minFollowDistance = minFollowDistance;
        this.followDelay = followDelay;
    }
    
    public void ExecuteBehavior()
    {
        if (!CanExecute()) return;
        
        isActive = true;
        
        if (playerTarget == null)
        {
            OnBehaviorFailed();
            return;
        }
        
        HandlePlayerStealing();
    }
    
    public bool CanExecute()
    {
        return !isComplete && playerTarget != null && !bitCarrier.IsCarryingBit;
    }
    
    public void OnBehaviorComplete()
    {
        isActive = false;
        isComplete = true;
        isFollowing = false;
        
        // Start fleeing after successful steal
        entity.StartFleeing();
    }
    
    public void OnBehaviorFailed()
    {
        isActive = false;
        isComplete = true;
        isFollowing = false;
        movement?.StopMovement();
    }
    
    public void Reset()
    {
        isActive = false;
        isComplete = false;
        isFollowing = false;
        hasCollidedWithPlayer = false;
        followTimer = 0f;
    }
    
    public void Stop()
    {
        isActive = false;
        isFollowing = false;
        movement?.StopMovement();
    }
    
    #endregion

    #region Player Stealing Logic
    
    private void HandlePlayerStealing()
    {
        if (hasCollidedWithPlayer && bitCarrier.IsCarryingBit)
        {
            OnBehaviorComplete();
            return;
        }
        
        float distanceToPlayer = movement.GetDistanceToTarget(playerTarget.position);
        
        // Check if player is in detection range
        if (distanceToPlayer <= detectionRange && !hasCollidedWithPlayer)
        {
            HandlePlayerDetection(distanceToPlayer);
        }
        else if (distanceToPlayer > detectionRange)
        {
            // Player out of range, stop following
            isFollowing = false;
            movement.StopMovement();
        }
    }
    
    private void HandlePlayerDetection(float distanceToPlayer)
    {
        if (!isFollowing)
        {
            StartFollowing();
        }
        
        if (isFollowing)
        {
            // Check if close enough to steal
            if (distanceToPlayer <= minFollowDistance)
            {
                entity.DebugLog($"CrawlingEntity {entity.name} close enough to steal! Distance: {distanceToPlayer:F2}");
                AttemptSteal();
            }
            else
            {
                // Move towards player
                movement.MoveTowardsTarget(playerTarget.position);
            }
        }
    }
    
    private void StartFollowing()
    {
        followTimer += Time.deltaTime;
        
        if (followTimer >= followDelay)
        {
            isFollowing = true;
            entity.DebugLog($"CrawlingEntity {entity.name} started following player");
        }
    }
    
    private void AttemptSteal()
    {
        if (!hasCollidedWithPlayer)
        {
            entity.DebugLog($"CrawlingEntity {entity.name} attempting to steal from player!");
            hasCollidedWithPlayer = true;
            StealBitFromPlayer();
        }
        else
        {
            entity.DebugLog($"CrawlingEntity {entity.name} already attempted steal, checking if bit was stolen...");
            // Check if we successfully got a bit
            if (bitCarrier.IsCarryingBit)
            {
                entity.DebugLog($"CrawlingEntity {entity.name} successfully has bit, completing behavior!");
                OnBehaviorComplete();
            }
        }
    }
    
    #endregion

    #region Player Interaction
    
    private void StealBitFromPlayer()
    {
        entity.DebugLog($"CrawlingEntity {entity.name} executing steal from player!");
        
        // Try to steal from player's inventory
        PowerBitPlayerController playerController = playerTarget.GetComponent<PowerBitPlayerController>();
        if (playerController != null)
        {
            entity.DebugLog($"CrawlingEntity {entity.name} found PlayerController, attempting to steal bit from build...");
            
            // Actually steal a bit from the player's build
            Bit stolenBit = playerController.StealRandomBitFromBuild();
            
            if (stolenBit != null)
            {
                entity.DebugLog($"CrawlingEntity {entity.name} successfully stole bit: {stolenBit.BitName}");
                bitCarrier.AttachBit(stolenBit);
                
                entity.DebugLog($"CrawlingEntity {entity.name} attached stolen bit! IsCarryingBit: {bitCarrier.IsCarryingBit}");
                
                // Complete the behavior since we got the bit
                if (bitCarrier.IsCarryingBit)
                {
                    OnBehaviorComplete();
                }
                else
                {
                    entity.DebugLogWarning($"CrawlingEntity {entity.name} failed to attach stolen bit!");
                    OnBehaviorFailed();
                }
            }
            else
            {
                entity.DebugLog($"CrawlingEntity {entity.name} couldn't steal from player - no bits available!");
                OnBehaviorFailed();
            }
        }
        else
        {
            entity.DebugLogWarning($"CrawlingEntity {entity.name} couldn't find PlayerController to steal from!");
            OnBehaviorFailed();
        }
    }
    
    #endregion

    #region Initialization Helpers
    
    private void FindPlayerTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTarget = player.transform;
            entity.DebugLog($"PlayerStealingBehavior found player: {player.name}");
        }
        else
        {
            entity.DebugLogError("PlayerStealingBehavior couldn't find player with tag 'Player'!");
        }
    }
    
    #endregion

    #region Collision Detection
    
    public void OnPlayerCollision(Collider2D playerCollider)
    {
        if (isActive && !hasCollidedWithPlayer)
        {
            AttemptSteal();
        }
    }
    
    #endregion
} 