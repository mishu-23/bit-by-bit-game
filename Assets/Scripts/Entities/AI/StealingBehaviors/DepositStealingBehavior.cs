using UnityEngine;

[System.Serializable]
public class DepositStealingBehavior : IStealingBehavior
{
    private float detectionRange;
    private float approachDistance;

    private CrawlingEntity entity;
    private CrawlingEntityMovement movement;
    private BitCarrier bitCarrier;
    
    private Transform depositTarget;
    private bool isActive;
    private bool isComplete;
    private bool isMovingToDeposit;

    public bool IsActive => isActive;
    public bool IsComplete => isComplete;
    public string BehaviorName => "Deposit Stealing";

    public void Initialize(CrawlingEntity entity)
    {
        this.entity = entity;
        this.movement = entity.GetComponent<CrawlingEntityMovement>();
        this.bitCarrier = entity.GetComponent<BitCarrier>();
        
        FindNearestDeposit();
    }

    public void Configure(float detectionRange, float approachDistance)
    {
        this.detectionRange = detectionRange;
        this.approachDistance = approachDistance;
    }

    public void ExecuteBehavior()
    {
        if (isComplete) return;
        
        HandleDepositStealing();
    }

    public bool CanExecute()
    {
        return !isComplete && depositTarget != null && !bitCarrier.IsCarryingBit;
    }

    public void OnBehaviorComplete()
    {
        isActive = false;
        isComplete = true;
        isMovingToDeposit = false;
        
        // Start fleeing after successful steal
        entity.StartFleeing();
    }

    public void OnBehaviorFailed()
    {
        isActive = false;
        isComplete = true;
        isMovingToDeposit = false;
        movement?.StopMovement();
    }

    public void Reset()
    {
        isActive = false;
        isComplete = false;
        isMovingToDeposit = false;
        FindNearestDeposit();
    }

    public void Stop()
    {
        isActive = false;
        isMovingToDeposit = false;
        movement?.StopMovement();
    }

    private void HandleDepositStealing()
    {
        if (depositTarget == null)
        {
            FindNearestDeposit();
            if (depositTarget == null)
            {
                entity.DebugLog("No deposit found, behavior failed");
                OnBehaviorFailed();
                return;
            }
        }
        
        float distanceToDeposit = movement.GetDistanceToTarget(depositTarget.position);
        
        // Check if we're close enough to steal
        if (distanceToDeposit <= approachDistance)
        {
            AttemptStealFromDeposit();
        }
        else if (distanceToDeposit <= detectionRange)
        {
            // Move towards deposit
            if (!isMovingToDeposit)
            {
                StartMovingToDeposit();
            }
            movement.MoveTowardsTarget(depositTarget.position);
        }
        else
        {
            // Deposit too far away
            entity.DebugLog("Deposit too far away, behavior failed");
            OnBehaviorFailed();
        }
    }
    
    private void StartMovingToDeposit()
    {
        isActive = true;
        isMovingToDeposit = true;
        entity.DebugLog($"Starting to move to deposit at {depositTarget.position}");
    }
    
    private void AttemptStealFromDeposit()
    {
        entity.DebugLog("Attempting to steal from deposit");
        
        if (bitCarrier.IsCarryingBit)
        {
            entity.DebugLog("Already carrying a bit, cannot steal from deposit");
            OnBehaviorFailed();
            return;
        }
        
        // Try to steal from the deposit
        DepositInteraction deposit = depositTarget?.GetComponent<DepositInteraction>();
        if (deposit != null && deposit.RemoveCoreBit())
        {
            // Create a core bit to carry
            Bit coreBit = Bit.CreateBit("Common CoreBit", BitType.CoreBit, Rarity.Common, 0, 0f);
            bitCarrier.AttachBit(coreBit);
            
            entity.DebugLog("Successfully stole core bit from deposit!");
            OnBehaviorComplete();
        }
        else
        {
            entity.DebugLog("Failed to steal from deposit - no core bits available");
            OnBehaviorFailed();
        }
    }

    private void FindNearestDeposit()
    {
        // Find all deposit interactions in the scene
        DepositInteraction[] deposits = Object.FindObjectsOfType<DepositInteraction>();
        
        if (deposits.Length == 0)
        {
            entity.DebugLog("No deposits found in scene");
            return;
        }
        
        float closestDistance = float.MaxValue;
        DepositInteraction closestDeposit = null;
        
        foreach (var deposit in deposits)
        {
            if (deposit == null) continue;
            
            float distance = Vector3.Distance(entity.transform.position, deposit.transform.position);
            if (distance < closestDistance && distance <= detectionRange)
            {
                closestDistance = distance;
                closestDeposit = deposit;
            }
        }
        
        if (closestDeposit != null)
        {
            depositTarget = closestDeposit.transform;
            entity.DebugLog($"Found deposit target: {closestDeposit.name} at distance {closestDistance:F1}");
        }
        else
        {
            entity.DebugLog("No deposits within detection range");
        }
    }
} 