using UnityEngine;
using System.Collections;
[RequireComponent(typeof(CrawlingEntityMovement), typeof(CrawlingEntityCombat), typeof(BitCarrier))]
public class CrawlingEntity : MonoBehaviour
{
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
    [SerializeField] private float gathererTakeDistance = 2f;
    [SerializeField] private float gathererMinFollowDistance = 0.5f;
    [SerializeField] private float gathererSearchInterval = 2f;
    [SerializeField] private Vector3 gathererCarryOffset = new Vector3(0f, 1.2f, 0f);
    [Header("Flee Settings")]
    [SerializeField] private float fleeSpeed = 6f;
    [SerializeField] private float fleeDistance = 8f;
    private CrawlingEntityMovement movement;
    private CrawlingEntityCombat combat;
    private BitCarrier bitCarrier;
    private IStealingBehavior currentBehavior;
    private PlayerStealingBehavior playerStealingBehavior;
    private DepositStealingBehavior depositStealingBehavior;
    private GathererStealingBehavior gathererStealingBehavior;
    private bool isFleeing;
    private Vector3 fleeTarget;
    private enum StealMechanic
    {
        StealFromPlayer,
        StealFromDeposit,
        FollowGatherer
    }
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
            if (currentBehavior is GathererStealingBehavior gathererBehavior && gathererBehavior.IsCarryingGatherer)
            {
                gathererBehavior.UpdateCarriedGathererPosition();
            }
        }
        else if (currentBehavior != null && !currentBehavior.IsComplete)
        {
            currentBehavior.ExecuteBehavior();
        }
    }
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
        gameObject.layer = 6;
    }
    private void SetupTriggerCollider()
    {
        BoxCollider2D triggerCollider = gameObject.AddComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.size = new Vector2(1.5f, 1.5f);
        triggerCollider.offset = Vector2.zero;
        triggerCollider.includeLayers = 1 << 3;
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
    public void StartFleeing()
    {
        if (currentBehavior != null && !(currentBehavior is GathererStealingBehavior gathererBehavior && gathererBehavior.IsCarryingGatherer))
        {
            currentBehavior.Stop();
        }
        float fleeX = Random.value > 0.5f ? -50f : 50f;
        fleeTarget = new Vector3(fleeX, movement.GroundY, transform.position.z);
        isFleeing = true;
        DebugLog($"CrawlingEntity {gameObject.name} fleeing to x = {fleeX}");
    }
    private void HandleFleeing()
    {
        if (!isFleeing) return;
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
            DebugLog($"CrawlingEntity {gameObject.name} reached flee target and despawning");
            Destroy(gameObject);
        }
    }
    private void HandleDeath()
    {
        DebugLog($"CrawlingEntity {gameObject.name} died!");
        if (bitCarrier.IsCarryingBit)
        {
            bitCarrier.DropBit();
        }
        if (gathererStealingBehavior != null && gathererStealingBehavior.IsCarryingGatherer)
        {
            gathererStealingBehavior.Stop();
        }
        bitCarrier.DropRandomBit();
        Destroy(gameObject);
    }
    private void OnBitStolen(Bit stolenBit)
    {
        DebugLog($"CrawlingEntity {gameObject.name} successfully stole bit: {stolenBit.name}");
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            DebugLog($"CrawlingEntity {gameObject.name} detected player: {other.name}");
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
        return currentBehavior?.IsActive ?? false;
    }
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
    private void OnDrawGizmos()
    {
        if (movement == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 10f);
        if (isFleeing)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(fleeTarget, 1f);
            Gizmos.DrawLine(transform.position, fleeTarget);
        }
        Gizmos.color = Color.green;
        Vector3 groundPos = new Vector3(transform.position.x, movement.GroundY, transform.position.z);
        Gizmos.DrawWireCube(groundPos, Vector3.one * 0.5f);
    }
}