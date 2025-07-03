using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BitByBit.Core;

public class GathererEntity : MonoBehaviour
{
    public enum GathererState
    {
        MovingToGather,
        Gathering,
        MovingToDeposit,
        Depositing,
        Wandering
    }

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

    private Rigidbody2D rb;
    private GathererState state;
    private bool isMoving;
    private float targetX;

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

    private void OnDestroy()
    {
        // Only decrement count if the gatherer is being destroyed during gameplay,
        // not when exiting play mode or quitting the application
        if (GathererManager.Instance != null && !isQuitting)
        {
            GathererManager.Instance.DecrementGathererCount();
        }
    }
    
    private static bool isQuitting = false;
    
    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

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

        GameObject foundObject = null;
        
        // Use GameReferences for better performance
        if (GameReferences.Instance != null)
        {
            switch (objectName)
            {
                case "Mine":
                    foundObject = GameReferences.Instance.Mine;
                    break;
                case "Tree":
                    foundObject = GameReferences.Instance.Tree;
                    break;
                case "Deposit":
                    foundObject = GameReferences.Instance.Deposit;
                    break;
            }
        }
        
        // Fallback: try to find object if GameReferences fails or doesn't have it
        if (foundObject == null)
        {
            foundObject = GameObject.Find(objectName);
            if (foundObject != null)
            {
                Debug.LogWarning($"GathererEntity: Found {objectName} via fallback method. Please ensure GameReferences is properly configured.");
            }
        }
        
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

        // Use GameReferences for better performance
        if (GameReferences.Instance != null && GameReferences.Instance.DepositInteraction != null)
        {
            return GameReferences.Instance.DepositInteraction;
        }

        // Fallback: find any in scene
        DepositInteraction fallback = FindObjectOfType<DepositInteraction>();
        if (fallback != null)
        {
            Debug.LogWarning("GathererEntity: Found DepositInteraction via fallback method. Please ensure GameReferences is properly configured.");
        }
        return fallback;
    }

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

    [Header("Debug Controls")]
    [SerializeField] private bool enableDebugInfo = false;
    [SerializeField] private KeyCode debugKey = KeyCode.G;

    private void Update()
    {
        if (Input.GetKeyDown(debugKey))
        {
            PrintDebugInfo();
        }
        
        if (enableDebugInfo)
        {
            // Show debug info every few seconds
            if (Time.time % 3f < 0.1f)
            {
                PrintDebugInfo();
            }
        }
    }

    private void OnDrawGizmos()
    {
        DrawMovementGizmos();
        DrawStateGizmos();
    }
    
    public void PrintDebugInfo()
    {
        string debugInfo = "=== GATHERER ENTITY DEBUG INFO ===\n";
        debugInfo += $"Name: {gameObject.name}\n";
        debugInfo += $"Position: {transform.position}\n";
        debugInfo += $"Current State: {state}\n";
        debugInfo += $"Is Moving: {isMoving}\n";
        debugInfo += $"Target X: {targetX}\n";
        debugInfo += $"Enabled: {enabled}\n";
        debugInfo += $"GameObject Active: {gameObject.activeInHierarchy}\n";
        
        // Component checks
        debugInfo += "\n--- COMPONENTS ---\n";
        debugInfo += $"Rigidbody2D: {(rb != null ? "OK" : "MISSING")}\n";
        if (rb != null)
        {
            debugInfo += $"  - Velocity: {rb.linearVelocity}\n";
            debugInfo += $"  - Simulated: {rb.simulated}\n";
        }
        
        Collider2D col = GetComponent<Collider2D>();
        debugInfo += $"Collider2D: {(col != null ? "OK" : "MISSING")}\n";
        if (col != null)
        {
            debugInfo += $"  - Enabled: {col.enabled}\n";
            debugInfo += $"  - IsTrigger: {col.isTrigger}\n";
        }
        
        // Reference points
        debugInfo += "\n--- REFERENCE POINTS ---\n";
        debugInfo += $"Mine Point: {(minePoint != null ? minePoint.position.ToString() : "NULL")}\n";
        debugInfo += $"Tree Point: {(treePoint != null ? treePoint.position.ToString() : "NULL")}\n";
        debugInfo += $"Deposit Point: {(depositPoint != null ? depositPoint.position.ToString() : "NULL")}\n";
        
        // Settings
        debugInfo += "\n--- SETTINGS ---\n";
        debugInfo += $"Move Force: {moveForce}\n";
        debugInfo += $"Max Speed: {maxSpeed}\n";
        debugInfo += $"Ground Y: {groundY}\n";
        debugInfo += $"Arrival Distance: {arrivalDistance}\n";
        debugInfo += $"Gather Time: {gatherTime}s\n";
        debugInfo += $"Deposit Time: {depositTime}s\n";
        debugInfo += $"Wander Time: {wanderTime}s\n";
        
        // Manager reference
        debugInfo += "\n--- MANAGER ---\n";
        debugInfo += $"GathererManager Instance: {(GathererManager.Instance != null ? "OK" : "NULL")}\n";
        
        // Parent/child info
        debugInfo += "\n--- TRANSFORM INFO ---\n";
        debugInfo += $"Parent: {(transform.parent != null ? transform.parent.name : "None")}\n";
        debugInfo += $"Layer: {gameObject.layer}\n";
        debugInfo += $"Tag: {gameObject.tag}\n";
        
        Debug.Log(debugInfo);
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
        if (enableDebugInfo)
        {
            Debug.Log(message);
        }
    }
} 