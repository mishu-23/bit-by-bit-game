using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CrawlingEntityMovement), typeof(CrawlingEntityCombat), typeof(BitCarrier))]
public class CrawlingEntity : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogging = true;
    
    [Header("Behavior Selection")]
    [SerializeField] private StealMechanic chosenMechanic;
    
    [Header("Player Stealing Settings")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float minFollowDistance = 1f;
    [SerializeField] private float followDelay = 0.5f;
    
    [Header("Deposit Stealing Settings")]
    [SerializeField] private float depositDetectionRange = 15f;
    [SerializeField] private float depositApproachDistance = 2f;
    
    [Header("Gatherer Stealing Settings")]
    [SerializeField] private float gathererDetectionRange = 15f;
    [SerializeField] private float gathererTakeDistance = 2f; // Increased for easier capture
    [SerializeField] private float gathererMinFollowDistance = 0.5f; // Should be smaller than takeDistance
    [SerializeField] private float gathererSearchInterval = 2f;
    [SerializeField] private Vector3 gathererCarryOffset = new Vector3(0f, 1.2f, 0f);
    
    [Header("Flee Settings")]
    [SerializeField] private float fleeSpeed = 6f;
    [SerializeField] private float fleeDistance = 8f;
    
    #endregion

    #region Private Fields - Components
    
    private CrawlingEntityMovement movement;
    private CrawlingEntityCombat combat;
    private BitCarrier bitCarrier;
    
    #endregion

    #region Private Fields - Behaviors
    
    private IStealingBehavior currentBehavior;
    private PlayerStealingBehavior playerStealingBehavior;
    private DepositStealingBehavior depositStealingBehavior;
    private GathererStealingBehavior gathererStealingBehavior;
    
    #endregion

    #region Private Fields - State
    
    private bool isFleeing;
    private Vector3 fleeTarget;
    
    #endregion

    #region Enums
    
    private enum StealMechanic
    {
        StealFromPlayer,
        StealFromDeposit,
        FollowGatherer
    }
    
    #endregion

    #region Unity Lifecycle
    
    private void Awake()
    {
        InitializeComponents();
        SetupEntityLayer();
        SetupTriggerCollider();
    }
    
    private void Start()
    {
        InitializeBehaviors();
        ChooseRandomMechanic();
        SetupEventListeners();
    }
    
    private void Update()
    {
        if (isFleeing)
        {
            HandleFleeing();
            
            // Continue updating carried gatherer position while fleeing
            if (currentBehavior is GathererStealingBehavior gathererBehavior && gathererBehavior.IsCarryingGatherer)
            {
                // Just update the position, don't execute the full behavior
                gathererBehavior.UpdateCarriedGathererPosition();
            }
        }
        else if (currentBehavior != null && !currentBehavior.IsComplete)
        {
            currentBehavior.ExecuteBehavior();
        }
    }
    
    #endregion

    #region Initialization
    
    private void InitializeComponents()
    {
        movement = GetComponent<CrawlingEntityMovement>();
        combat = GetComponent<CrawlingEntityCombat>();
        bitCarrier = GetComponent<BitCarrier>();
        
        if (movement == null || combat == null || bitCarrier == null)
        {
            DebugLogError("Missing required components! Ensure CrawlingEntityMovement, CrawlingEntityCombat, and BitCarrier are attached.");
        }
    }
    
    private void SetupEntityLayer()
    {
        gameObject.layer = 6; // Entity layer
    }

    private void SetupTriggerCollider()
    {
        BoxCollider2D triggerCollider = gameObject.AddComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.size = new Vector2(1.5f, 1.5f);
        triggerCollider.offset = Vector2.zero;
        triggerCollider.includeLayers = 1 << 3; // Player layer (3)
        triggerCollider.excludeLayers = 0;
    }

    private void InitializeBehaviors()
    {
        playerStealingBehavior = new PlayerStealingBehavior();
        playerStealingBehavior.Initialize(this);
        playerStealingBehavior.Configure(detectionRange, minFollowDistance, followDelay);
        
        depositStealingBehavior = new DepositStealingBehavior();
        depositStealingBehavior.Initialize(this);
        depositStealingBehavior.Configure(depositDetectionRange, depositApproachDistance);
        
        gathererStealingBehavior = new GathererStealingBehavior();
        gathererStealingBehavior.Initialize(this);
        gathererStealingBehavior.Configure(gathererDetectionRange, gathererTakeDistance, gathererSearchInterval, gathererCarryOffset, gathererMinFollowDistance);
    }
    
    private void ChooseRandomMechanic()
    {
        if (chosenMechanic == StealMechanic.StealFromPlayer || Random.value < 0.33f)
        {
            chosenMechanic = StealMechanic.StealFromPlayer;
            currentBehavior = playerStealingBehavior;
        }
        else if (chosenMechanic == StealMechanic.StealFromDeposit || Random.value < 0.66f)
        {
            chosenMechanic = StealMechanic.StealFromDeposit;
            currentBehavior = depositStealingBehavior;
        }
        else
        {
            chosenMechanic = StealMechanic.FollowGatherer;
            currentBehavior = gathererStealingBehavior;
        }
        
        DebugLog($"CrawlingEntity {gameObject.name} chose mechanic: {chosenMechanic}");
    }
    
    private void SetupEventListeners()
    {
        if (combat != null)
        {
            combat.OnDeath.AddListener(HandleDeath);
        }
        
        if (bitCarrier != null)
        {
            bitCarrier.OnBitAttached.AddListener(OnBitStolen);
        }
    }
    
    #endregion

    #region Behavior Management
    
    public void StartFleeing()
    {
        // Don't stop gatherer behavior if it's carrying a gatherer - let it continue carrying while fleeing
        if (currentBehavior != null && !(currentBehavior is GathererStealingBehavior gathererBehavior && gathererBehavior.IsCarryingGatherer))
        {
            currentBehavior.Stop();
        }
        
        // Choose random flee target: go well beyond the boundaries to ensure despawning
        float fleeX = Random.value > 0.5f ? -50f : 50f;
        fleeTarget = new Vector3(fleeX, movement.GroundY, transform.position.z);
        
        isFleeing = true;
        DebugLog($"CrawlingEntity {gameObject.name} fleeing to x = {fleeX}");
    }

    private void HandleFleeing()
    {
        if (!isFleeing) return;
        
        // Check if entity has moved far enough to despawn
        float currentX = transform.position.x;
        if (Mathf.Abs(currentX) >= 45f)
        {
            DebugLog($"CrawlingEntity {gameObject.name} despawning at x = {currentX}");
            Destroy(gameObject);
            return;
        }
        
        float distanceToFleeTarget = movement.GetDistanceToTarget(fleeTarget);
        
        if (distanceToFleeTarget > 1f)
        {
            movement.MoveTowardsTarget(fleeTarget, fleeSpeed);
        }
        else
        {
            // Reached flee target, despawn
            DebugLog($"CrawlingEntity {gameObject.name} reached flee target and despawning");
            Destroy(gameObject);
        }
    }
    
    #endregion

    #region Event Handlers
    
    private void HandleDeath()
    {
        DebugLog($"CrawlingEntity {gameObject.name} died!");
        
        // Drop any carried bit first
        if (bitCarrier.IsCarryingBit)
        {
            bitCarrier.DropBit();
        }
        
        // Drop any carried gatherer
        if (gathererStealingBehavior != null && gathererStealingBehavior.IsCarryingGatherer)
        {
            gathererStealingBehavior.Stop(); // This will drop the gatherer
        }
        
        // Always drop a random bit as well
        bitCarrier.DropRandomBit();
        
        // Destroy the entity
        Destroy(gameObject);
    }
    
    private void OnBitStolen(Bit stolenBit)
    {
        DebugLog($"CrawlingEntity {gameObject.name} successfully stole bit: {stolenBit.name}");
    }
    
    #endregion

    #region Collision Detection

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            DebugLog($"CrawlingEntity {gameObject.name} detected player: {other.name}");
            
            // Notify current behavior about player collision
            if (currentBehavior is PlayerStealingBehavior playerBehavior)
            {
                playerBehavior.OnPlayerCollision(other);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            DebugLog($"CrawlingEntity {gameObject.name} collided with player: {collision.gameObject.name}");
        }
    }
    
    #endregion

    #region Public Interface
    
    public void ResetEntity()
    {
        isFleeing = false;
        movement.StopMovement();
        
        if (bitCarrier.IsCarryingBit)
        {
            bitCarrier.DropBit();
        }
        
        currentBehavior?.Reset();
        combat.ResetHealth();
        
        DebugLog($"CrawlingEntity {gameObject.name} has been reset");
    }

    public bool IsFollowing()
    {
        return currentBehavior?.IsActive ?? false;
    }

    public bool HasCollidedWithPlayer()
    {
        // This would need to be tracked by the behavior
        return currentBehavior?.IsActive ?? false;
    }
    
    #endregion

    #region Debug Utilities
    
    public void DebugLog(string message)
    {
        if (enableDebugLogging)
        {
            Debug.Log($"[CrawlingEntity] {message}");
        }
    }
    
    public void DebugLogWarning(string message)
    {
        if (enableDebugLogging)
        {
            Debug.LogWarning($"[CrawlingEntity] {message}");
        }
    }
    
    public void DebugLogError(string message)
    {
        if (enableDebugLogging)
        {
            Debug.LogError($"[CrawlingEntity] {message}");
        }
    }
    
    #endregion

    #region Gizmos
    
    private void OnDrawGizmos()
    {
        if (movement == null) return;
        
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 10f); // Default detection range
        
        // Draw flee target if fleeing
        if (isFleeing)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(fleeTarget, 1f);
            Gizmos.DrawLine(transform.position, fleeTarget);
        }
        
        // Draw ground position
        Gizmos.color = Color.green;
        Vector3 groundPos = new Vector3(transform.position.x, movement.GroundY, transform.position.z);
        Gizmos.DrawWireCube(groundPos, Vector3.one * 0.5f);
    }
    
    #endregion
} 