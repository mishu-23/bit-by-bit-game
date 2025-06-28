using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GathererEntity : MonoBehaviour
{
    #region Enums
    
    public enum GathererState
    {
        MovingToGather,
        Gathering,
        MovingToDeposit,
        Depositing,
        Wandering
    }
    
    #endregion

    #region Serialized Fields
    
    [Header("Points")]
    public Transform minePoint;
    public Transform treePoint;
    public Transform depositPoint;

    [Header("Movement")]
    public float moveForce = 10f;
    public float maxSpeed = 5f;
    public float groundY = 0.5f;
    public float arrivalDistance = 0.5f;

    [Header("Timing")]
    public float gatherTime = 5f;
    public float depositTime = 5f;
    public float wanderTime = 10f;
    
    #endregion

    #region Private Fields
    
    private Rigidbody2D rb;
    private GathererState state;
    private bool isMoving;
    private float targetX;
    
    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeComponents();
        ConfigurePhysics();
    }

    private void Start()
    {
        InitializePosition();
        AutoFindPoints();
        StartGatheringCycle();
    }

    private void FixedUpdate()
    {
        MaintainGroundLevel();
        ProcessMovement();
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void ConfigurePhysics()
    {
        gameObject.layer = 6; // Entity layer
        Physics2D.IgnoreLayerCollision(6, 3, true); // Entity vs Player
    }

    private void InitializePosition()
    {
        transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
    }

    private void AutoFindPoints()
    {
        FindPointIfNotAssigned(ref minePoint, "Mine");
        FindPointIfNotAssigned(ref treePoint, "Tree");
        FindPointIfNotAssigned(ref depositPoint, "Deposit");
    }

    private void FindPointIfNotAssigned(ref Transform point, string objectName)
    {
        if (point != null) return;

        GameObject foundObject = GameObject.Find(objectName);
        if (foundObject != null)
        {
            point = foundObject.transform;
            LogDebug($"GathererEntity {gameObject.name}: Auto-found {objectName} point");
        }
        else
        {
            Debug.LogError($"GathererEntity {gameObject.name}: Could not find {objectName} point! Please assign it manually.");
        }
    }

    #endregion

    #region Movement System

    private void ProcessMovement()
    {
        if (isMoving)
        {
            ApplyMovement();
        }
        else
        {
            ApplyBrakes();
        }
    }

    private void ApplyMovement()
    {
        float direction = Mathf.Sign(targetX - transform.position.x);
        rb.AddForce(new Vector2(direction * moveForce, 0f), ForceMode2D.Force);
        
        LimitSpeed();
        CheckArrival();
    }

    private void ApplyBrakes()
    {
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, 0.1f);
    }

    private void LimitSpeed()
    {
        if (Mathf.Abs(rb.linearVelocity.x) > maxSpeed)
        {
            rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * maxSpeed, 0f);
        }
    }

    private void CheckArrival()
    {
        if (Mathf.Abs(transform.position.x - targetX) <= arrivalDistance)
        {
            rb.linearVelocity = Vector2.zero;
            isMoving = false;
            OnReachedTarget();
        }
    }

    private void MoveTo(float x)
    {
        targetX = x;
        isMoving = true;
        LogDebug($"Entity {gameObject.name} moving to X: {x}");
    }

    private void MaintainGroundLevel()
    {
        if (Mathf.Abs(transform.position.y - groundY) > 0.1f)
        {
            transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
        }
    }

    #endregion

    #region AI State Machine

    public void StartGatheringCycle()
    {
        state = GathererState.MovingToGather;
        Transform target = SelectRandomGatheringPoint();
        string resourceType = GetResourceTypeName(target);
        
        LogDebug($"Entity {gameObject.name} moving to {resourceType} at {target.position.x}");
        MoveTo(target.position.x);
    }

    private void OnReachedTarget()
    {
        switch (state)
        {
            case GathererState.MovingToGather:
                LogDebug($"Entity {gameObject.name} reached gathering point, starting to gather...");
                StartCoroutine(ProcessGathering());
                break;
            case GathererState.MovingToDeposit:
                LogDebug($"Entity {gameObject.name} reached deposit point, starting to deposit...");
                StartCoroutine(ProcessDepositing());
                break;
        }
    }

    private Transform SelectRandomGatheringPoint()
    {
        return Random.value > 0.5f ? minePoint : treePoint;
    }

    private string GetResourceTypeName(Transform target)
    {
        return (target == minePoint) ? "Mine" : "Tree";
    }

    #endregion

    #region Activity Coroutines

    private IEnumerator ProcessGathering()
    {
        state = GathererState.Gathering;
        LogDebug($"Entity {gameObject.name} gathering for {gatherTime} seconds...");
        yield return new WaitForSeconds(gatherTime);
        
        state = GathererState.MovingToDeposit;
        LogDebug($"Entity {gameObject.name} finished gathering, moving to deposit at {depositPoint.position.x}");
        MoveTo(depositPoint.position.x);
    }

    private IEnumerator ProcessDepositing()
    {
        state = GathererState.Depositing;
        LogDebug($"Entity {gameObject.name} depositing for {depositTime} seconds...");
        yield return new WaitForSeconds(depositTime);
        
        TryDepositToBuild();
        
        LogDebug($"Entity {gameObject.name} finished depositing, starting wander phase...");
        StartCoroutine(ProcessWandering());
    }

    private IEnumerator ProcessWandering()
    {
        state = GathererState.Wandering;
        LogDebug($"Entity {gameObject.name} wandering for {wanderTime} seconds...");
        
        float endTime = Time.time + wanderTime;
        
        while (Time.time < endTime)
        {
            yield return StartCoroutine(WanderToRandomPoint(endTime));
            
            if (Time.time < endTime)
            {
                yield return StartCoroutine(PauseWandering());
            }
        }
        
        LogDebug($"Entity {gameObject.name} finished wandering, starting new gathering cycle...");
        StartGatheringCycle();
    }

    private IEnumerator WanderToRandomPoint(float endTime)
    {
        float randomX = transform.position.x + Random.Range(-3f, 3f);
        LogDebug($"Entity {gameObject.name} wandering to X: {randomX}");
        MoveTo(randomX);
        
        // Wait until we reach the random point
        while (isMoving && Time.time < endTime)
        {
            yield return null;
        }
    }

    private IEnumerator PauseWandering()
    {
        float pause = Random.Range(0.5f, 1.5f);
        LogDebug($"Entity {gameObject.name} pausing for {pause} seconds...");
        yield return new WaitForSeconds(pause);
    }

    #endregion

    #region Resource Management

    private void TryDepositToBuild()
    {
        DepositInteraction deposit = FindDepositInteraction();
        if (deposit != null)
        {
            deposit.AddCoreBitFromGatherer();
        }
        else
        {
            Debug.LogWarning("No DepositInteraction found for gatherer to deposit into!");
        }
    }

    private DepositInteraction FindDepositInteraction()
    {
        if (depositPoint == null) return null;

        // Try to get from the deposit point directly
        DepositInteraction deposit = depositPoint.GetComponent<DepositInteraction>();
        if (deposit != null) return deposit;

        // Try to get from children
        deposit = depositPoint.GetComponentInChildren<DepositInteraction>();
        if (deposit != null) return deposit;

        // Find any in scene as fallback
        return FindObjectOfType<DepositInteraction>();
    }

    #endregion

    #region Collision Handling

    private void OnCollisionEnter2D(Collision2D collision)
    {
        LogDebug($"Entity collided with: {collision.gameObject.name} on layer {collision.gameObject.layer}");
        
        if (ShouldIgnoreCollision(collision.gameObject))
        {
            Physics2D.IgnoreCollision(GetComponent<Collider2D>(), collision.collider, true);
        }
    }

    private bool ShouldIgnoreCollision(GameObject other)
    {
        return other.CompareTag("Player") || other.layer == 8;
    }

    #endregion

    #region Debug and Visualization

    private void OnDrawGizmos()
    {
        DrawMovementGizmos();
        DrawStateGizmos();
    }

    private void DrawMovementGizmos()
    {
        if (isMoving)
        {
            Gizmos.color = Color.red;
            Vector3 targetPosition = new Vector3(targetX, groundY, 0f);
            Gizmos.DrawLine(transform.position, targetPosition);
            Gizmos.DrawWireSphere(targetPosition, 0.3f);
        }
    }

    private void DrawStateGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }

    private void LogDebug(string message)
    {
        Debug.Log(message);
    }

    #endregion
} 