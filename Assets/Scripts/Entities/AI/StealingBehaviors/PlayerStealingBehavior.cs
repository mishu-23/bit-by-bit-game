using UnityEngine;
using BitByBit.Core;
[System.Serializable]
public class PlayerStealingBehavior : IStealingBehavior
{
    private float detectionRange;
    private float minFollowDistance;
    private float followDelay;
    private CrawlingEntity entity;
    private CrawlingEntityMovement movement;
    private BitCarrier bitCarrier;
    private Transform playerTarget;
    private bool isActive;
    private bool isComplete;
    private bool isFollowing;
    private bool hasCollidedWithPlayer;
    private float followTimer;
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
    private void HandlePlayerStealing()
    {
        if (hasCollidedWithPlayer && bitCarrier.IsCarryingBit)
        {
            OnBehaviorComplete();
            return;
        }
        float distanceToPlayer = movement.GetDistanceToTarget(playerTarget.position);
        if (distanceToPlayer <= detectionRange && !hasCollidedWithPlayer)
        {
            HandlePlayerDetection(distanceToPlayer);
        }
        else if (distanceToPlayer > detectionRange)
        {
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
            if (distanceToPlayer <= minFollowDistance)
            {
                entity.DebugLog($"CrawlingEntity {entity.name} close enough to steal! Distance: {distanceToPlayer:F2}");
                AttemptSteal();
            }
            else
            {
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
            if (bitCarrier.IsCarryingBit)
            {
                entity.DebugLog($"CrawlingEntity {entity.name} successfully has bit, completing behavior!");
                OnBehaviorComplete();
            }
        }
    }
    private void StealBitFromPlayer()
    {
        entity.DebugLog($"CrawlingEntity {entity.name} executing steal from player!");
        PowerBitPlayerController playerController = playerTarget.GetComponent<PowerBitPlayerController>();
        if (playerController != null)
        {
            entity.DebugLog($"CrawlingEntity {entity.name} found PlayerController, attempting to steal bit from build...");
            Bit stolenBit = playerController.StealRandomBitFromBuild();
            if (stolenBit != null)
            {
                entity.DebugLog($"CrawlingEntity {entity.name} successfully stole bit: {stolenBit.BitName}");
                bitCarrier.AttachBit(stolenBit);
                entity.DebugLog($"CrawlingEntity {entity.name} attached stolen bit! IsCarryingBit: {bitCarrier.IsCarryingBit}");
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
    private void FindPlayerTarget()
    {
        if (GameReferences.Instance != null && GameReferences.Instance.Player != null)
        {
            playerTarget = GameReferences.Instance.Player;
            entity.DebugLog($"PlayerStealingBehavior found player via GameReferences: {playerTarget.name}");
        }
        else
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
                entity.DebugLog($"PlayerStealingBehavior found player via fallback: {player.name}");
                entity.DebugLogWarning("PlayerStealingBehavior: Found player via fallback method. Please ensure GameReferences is properly configured.");
            }
            else
            {
                entity.DebugLogError("PlayerStealingBehavior couldn't find player with tag 'Player'!");
            }
        }
    }
    public void OnPlayerCollision(Collider2D playerCollider)
    {
        if (isActive && !hasCollidedWithPlayer)
        {
            AttemptSteal();
        }
    }
}