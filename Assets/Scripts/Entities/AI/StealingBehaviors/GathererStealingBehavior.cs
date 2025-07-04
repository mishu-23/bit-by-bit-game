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
        if (isCarryingGatherer)
        {
            DropGatherer();
        }
    }
    private void HandleGathererStealing()
    {
        bool shouldSearchForGatherer = gathererTarget == null || 
                                       (Time.time - lastSearchTime) > searchInterval;
        if (shouldSearchForGatherer)
        {
            lastSearchTime = Time.time;
            FindNearestGatherer();
        }
        if (gathererTarget != null && !isCarryingGatherer)
        {
            float distanceToGatherer = movement.GetDistanceToTarget(gathererTarget.position);
            if (isFollowingGatherer)
            {
                entity.DebugLog($"Distance to gatherer: {distanceToGatherer:F2}, takeDistance: {takeDistance:F2}");
            }
            if (distanceToGatherer <= takeDistance)
            {
                entity.DebugLog($"Close enough to take gatherer! Distance: {distanceToGatherer:F2} <= {takeDistance:F2}");
                TakeGatherer(gathererTarget.gameObject);
            }
            else if (distanceToGatherer <= detectionRange)
            {
                if (distanceToGatherer > minFollowDistance)
                {
                    if (!isFollowingGatherer)
                    {
                        StartFollowingGatherer(distanceToGatherer);
                    }
                    movement.MoveTowardsTarget(gathererTarget.position);
                }
                else
                {
                    entity.DebugLog($"Close to gatherer, stopping movement. Distance: {distanceToGatherer:F2}, minFollow: {minFollowDistance:F2}, takeDistance: {takeDistance:F2}");
                    movement.StopMovement();
                }
            }
            else
            {
                entity.DebugLog("Gatherer moved too far away, stopping follow");
                isFollowingGatherer = false;
                movement.StopMovement();
            }
        }
        else if (isCarryingGatherer)
        {
            UpdateCarriedGathererPosition();
            movement.StopMovement();
            isFollowingGatherer = false;
            if (!isComplete)
            {
                OnBehaviorComplete();
            }
        }
        else
        {
            movement.StopMovement();
            isFollowingGatherer = false;
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
        carriedGatherer = gatherer;
        isCarryingGatherer = true;
        GathererEntity gathererEntity = gatherer.GetComponent<GathererEntity>();
        if (gathererEntity != null)
        {
            gathererEntity.enabled = false; 
        }
        Rigidbody2D gathererRb = gatherer.GetComponent<Rigidbody2D>();
        if (gathererRb != null)
        {
            gathererRb.simulated = false; 
        }
        gatherer.transform.SetParent(entity.transform);
        UpdateCarriedGathererPosition();
        gathererTarget = null;
        isFollowingGatherer = false;
        entity.DebugLog($"Successfully captured gatherer {gatherer.name}!");
        OnBehaviorComplete();
    }
    private void DropGatherer()
    {
        if (!isCarryingGatherer || carriedGatherer == null) return;
        entity.DebugLog($"Dropping gatherer: {carriedGatherer.name}");
        carriedGatherer.transform.SetParent(null);
        Vector3 dropPosition = entity.transform.position + Vector3.right * 2f; 
        dropPosition.y = movement.GroundY; 
        carriedGatherer.transform.position = dropPosition;
        Rigidbody2D gathererRb = carriedGatherer.GetComponent<Rigidbody2D>();
        if (gathererRb != null)
        {
            gathererRb.simulated = true; 
        }
        GathererEntity gathererEntity = carriedGatherer.GetComponent<GathererEntity>();
        if (gathererEntity != null)
        {
            gathererEntity.enabled = true; 
        }
        carriedGatherer = null;
        isCarryingGatherer = false;
        entity.DebugLog("Gatherer dropped successfully!");
    }
    private void FindNearestGatherer()
    {
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
            carriedGatherer.transform.localPosition = carryOffset;
        }
    }
}