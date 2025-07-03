using UnityEngine;

[System.Serializable]
public class GathererStealingBehavior : IStealingBehavior
{
    private float detectionRange;
    private float takeDistance;
    private float searchInterval;
    private Vector3 carryOffset;
    private float minFollowDistance;

    private CrawlingEntity entity;
    private CrawlingEntityMovement movement;
    private BitCarrier bitCarrier;
    
    private Transform gathererTarget;
    private GameObject carriedGatherer;
    private bool isActive;
    private bool isComplete;
    private bool isFollowingGatherer;
    private bool isCarryingGatherer;
    private float lastSearchTime;

    public bool IsActive => isActive;
    public bool IsComplete => isComplete;
    public string BehaviorName => "Gatherer Stealing";

    public void Initialize(CrawlingEntity entity)
    {
        this.entity = entity;
        this.movement = entity.GetComponent<CrawlingEntityMovement>();
        this.bitCarrier = entity.GetComponent<BitCarrier>();
        
        FindNearestGatherer();
    }

    public void Configure(float detectionRange, float takeDistance, float searchInterval, Vector3 carryOffset, float minFollowDistance)
    {
        this.detectionRange = detectionRange;
        this.takeDistance = takeDistance;
        this.searchInterval = searchInterval;
        this.carryOffset = carryOffset;
        this.minFollowDistance = minFollowDistance;
    }

    public void ExecuteBehavior()
    {
        if (isComplete) return;
        
        HandleGathererStealing();
    }

    public bool CanExecute()
    {
        return !isComplete && (gathererTarget != null || !isCarryingGatherer);
    }

    public void OnBehaviorComplete()
    {
        isActive = false;
        isComplete = true;
        isFollowingGatherer = false;
        
        // Start fleeing after capturing gatherer
        entity.StartFleeing();
    }

    public void OnBehaviorFailed()
    {
        isActive = false;
        isComplete = true;
        isFollowingGatherer = false;
        movement?.StopMovement();
    }

    public void Reset()
    {
        isActive = false;
        isComplete = false;
        isFollowingGatherer = false;
        isCarryingGatherer = false;
        gathererTarget = null;
        carriedGatherer = null;
        FindNearestGatherer();
    }

    public void Stop()
    {
        isActive = false;
        isFollowingGatherer = false;
        movement?.StopMovement();
        
        // Drop gatherer if carrying one
        if (isCarryingGatherer)
        {
            DropGatherer();
        }
    }

    private void HandleGathererStealing()
    {
        // Periodic search for gatherers
        bool shouldSearchForGatherer = gathererTarget == null || 
                                       (Time.time - lastSearchTime) > searchInterval;
        
        if (shouldSearchForGatherer)
        {
            lastSearchTime = Time.time;
            FindNearestGatherer();
        }
        
        // If we have a target, follow it (unless we're already carrying one)
        if (gathererTarget != null && !isCarryingGatherer)
        {
            float distanceToGatherer = movement.GetDistanceToTarget(gathererTarget.position);
            
            // Debug: Log distance every frame when following
            if (isFollowingGatherer)
            {
                entity.DebugLog($"Distance to gatherer: {distanceToGatherer:F2}, takeDistance: {takeDistance:F2}");
            }
            
            // Check if close enough to take the gatherer
            if (distanceToGatherer <= takeDistance)
            {
                entity.DebugLog($"Close enough to take gatherer! Distance: {distanceToGatherer:F2} <= {takeDistance:F2}");
                TakeGatherer(gathererTarget.gameObject);
            }
            else if (distanceToGatherer <= detectionRange)
            {
                if (distanceToGatherer > minFollowDistance)
                {
                    // Move towards gatherer (only if further than minFollowDistance)
                    if (!isFollowingGatherer)
                    {
                        StartFollowingGatherer(distanceToGatherer);
                    }
                    movement.MoveTowardsTarget(gathererTarget.position);
                }
                else
                {
                    // Close to gatherer but not close enough to take, stop moving
                    entity.DebugLog($"Close to gatherer, stopping movement. Distance: {distanceToGatherer:F2}, minFollow: {minFollowDistance:F2}, takeDistance: {takeDistance:F2}");
                    movement.StopMovement();
                }
            }
            else
            {
                // Gatherer too far away
                entity.DebugLog("Gatherer moved too far away, stopping follow");
                isFollowingGatherer = false;
                movement.StopMovement();
            }
        }
        else if (isCarryingGatherer)
        {
            // We're carrying a gatherer, just update its position and stop following
            UpdateCarriedGathererPosition();
            movement.StopMovement();
            isFollowingGatherer = false;
            
            // Complete the behavior since we have the gatherer
            if (!isComplete)
            {
                OnBehaviorComplete();
            }
        }
        else
        {
            // No gatherer found, stop movement and wait for next search
            movement.StopMovement();
            isFollowingGatherer = false;
            
            // If we've searched long enough without finding a gatherer, fail
            if (Time.time - lastSearchTime > searchInterval * 3f)
            {
                entity.DebugLog("No gatherer found after extended search, behavior failed");
                OnBehaviorFailed();
            }
        }
    }
    
    private void StartFollowingGatherer(float distance)
    {
        isActive = true;
        isFollowingGatherer = true;
        entity.DebugLog($"Started following gatherer at distance {distance:F1}");
    }
    
    private void TakeGatherer(GameObject gatherer)
    {
        if (gatherer == null || isCarryingGatherer) return;
        
        entity.DebugLog($"Taking gatherer: {gatherer.name}");
        
        // Store reference to the gatherer
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
        gatherer.transform.SetParent(entity.transform);
        
        // Position the gatherer above the crawling entity
        UpdateCarriedGathererPosition();
        
        // Clear the target since we've taken it
        gathererTarget = null;
        isFollowingGatherer = false;
        
        entity.DebugLog($"Successfully captured gatherer {gatherer.name}!");
        
        // Complete the behavior
        OnBehaviorComplete();
    }

    private void DropGatherer()
    {
        if (!isCarryingGatherer || carriedGatherer == null) return;
        
        entity.DebugLog($"Dropping gatherer: {carriedGatherer.name}");
        
        // Remove from parent
        carriedGatherer.transform.SetParent(null);
        
        // Position the gatherer at ground level near the crawling entity
        Vector3 dropPosition = entity.transform.position + Vector3.right * 2f; // Drop 2 units to the right
        dropPosition.y = movement.GroundY; // Set to ground level
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
        
        entity.DebugLog("Gatherer dropped successfully!");
    }

    private void FindNearestGatherer()
    {
        // Find all gatherer entities in the scene
        GathererEntity[] gatherers = Object.FindObjectsOfType<GathererEntity>();
        
        if (gatherers.Length == 0)
        {
            entity.DebugLog("No gatherers found in scene");
            return;
        }
        
        float closestDistance = float.MaxValue;
        GathererEntity closestGatherer = null;
        
        foreach (var gatherer in gatherers)
        {
            if (gatherer == null || !gatherer.enabled) continue;
            
            float distance = Vector3.Distance(entity.transform.position, gatherer.transform.position);
            if (distance < closestDistance && distance <= detectionRange)
            {
                closestDistance = distance;
                closestGatherer = gatherer;
            }
        }
        
        if (closestGatherer != null)
        {
            // Check if we found a different gatherer than before
            if (gathererTarget == null || gathererTarget != closestGatherer.transform)
            {
                gathererTarget = closestGatherer.transform;
                entity.DebugLog($"Found gatherer target: {closestGatherer.name} at distance {closestDistance:F1}");
            }
        }
        else
        {
            if (gathererTarget != null)
            {
                entity.DebugLogWarning("Lost gatherer target, will keep searching...");
                gathererTarget = null;
                isFollowingGatherer = false;
            }
        }
    }

    public bool IsCarryingGatherer => isCarryingGatherer;
    public GameObject CarriedGatherer => carriedGatherer;
    
    public void UpdateCarriedGathererPosition()
    {
        if (carriedGatherer != null)
        {
            // Set the carried gatherer's position relative to the crawling entity
            carriedGatherer.transform.localPosition = carryOffset;
        }
    }
} 