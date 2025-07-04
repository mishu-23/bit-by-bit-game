using UnityEngine;
using System.Collections;
using BitByBit.Core;
public class CrawlingEntityOriginal : MonoBehaviour, IDamageable
{
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogging = true;
    [Header("Target")]
    public Transform playerTarget;
    [Header("Movement")]
    public float moveForce = 8f;
    public float maxSpeed = 4f;
    public float groundY = 0.5f;
    public float detectionRange = 10f;
    public float minFollowDistance = 1f;
    [Header("Acceleration")]
    public float accelerationMultiplier = 2f;
    public float initialBoostForce = 12f;
    public float accelerationTime = 0.5f;
    [Header("Combat")]
    public int maxHealth = 10;
    public int currentHealth;
    [Header("Behavior")]
    public bool isFollowing = false;
    public float followDelay = 0.5f;
    public float fleeDistance = 8f;
    public float fleeSpeed = 6f;
    public float playerOffset = 0.5f;
    [Header("Attached Bit Drop")]
    public Vector3 bitDropOffset = new Vector3(0f, 1.5f, 0f);
    public float bitDropBobAmount = 0.2f;
    public float bitDropBobSpeed = 2f;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private bool hasCollidedWithPlayer = false;
    private float lastMoveDirection = 1f;
    private float accelerationTimer = 0f;
    private bool wasMoving = false;
    private bool isFleeing = false;
    private Vector3 fleeTarget;
    private GameObject attachedBitDrop;
    private Bit attachedBitData;
    private Vector3 originalBitDropOffset;
    private float bobTimer = 0f;
    private enum StealMechanic
    {
        StealFromPlayer,
        StealFromDeposit,
        FollowGatherer
    }
    private StealMechanic chosenMechanic;
    private Transform depositTarget;
    private bool isMovingToDeposit = false;
    private Transform gathererTarget;
    private bool isFollowingGatherer = false;
    private float gathererDetectionRange = 15f;
    private float gathererSearchInterval = 2f;
    private float lastGathererSearchTime = 0f;
    private GameObject carriedGatherer;
    private bool isCarryingGatherer = false;
    private Vector3 gathererCarryOffset = new Vector3(0f, 1.2f, 0f);
    private float gathererTakeDistance = 1.5f;
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
        gameObject.layer = 6;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
        SetupTriggerCollider();
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
    private void Start()
    {
        transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
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
        if (playerTarget == null)
        {
            InitializePlayerTarget();
        }
        if (chosenMechanic == StealMechanic.StealFromDeposit)
        {
            InitializeDepositTarget();
        }
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
    }
    private void InitializePlayerTarget()
    {
        if (GameReferences.Instance != null && GameReferences.Instance.Player != null)
        {
            playerTarget = GameReferences.Instance.Player;
            DebugLog($"CrawlingEntity {gameObject.name} found player via GameReferences: {playerTarget.name}");
        }
        else
        {
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
        if (GameReferences.Instance != null && GameReferences.Instance.Deposit != null)
        {
            depositTarget = GameReferences.Instance.Deposit.transform;
            DebugLog($"CrawlingEntity {gameObject.name} found deposit via GameReferences at: {depositTarget.position}");
            StartMovingToDeposit();
        }
        else
        {
            GameObject deposit = GameObject.Find("Deposit");
            if (deposit != null)
            {
                depositTarget = deposit.transform;
                DebugLog($"CrawlingEntity {gameObject.name} found deposit via fallback at: {depositTarget.position}");
                DebugLogWarning("CrawlingEntity: Found deposit via fallback method. Please ensure GameReferences is properly configured.");
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
        if (BitManager.Instance != null)
        {
            attachedBitData = BitManager.Instance.GetRandomBit();
            if (attachedBitData != null)
            {
                attachedBitDrop = new GameObject($"AttachedBitDrop_{attachedBitData.BitName}");
                attachedBitDrop.transform.SetParent(transform);
                SpriteRenderer bitSprite = attachedBitDrop.AddComponent<SpriteRenderer>();
                bitSprite.sprite = attachedBitData.GetSprite();
                bitSprite.sortingOrder = 1;
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
            bobTimer += Time.deltaTime * bitDropBobSpeed;
            float bobOffset = Mathf.Sin(bobTimer) * bitDropBobAmount;
            Vector3 targetPosition = originalBitDropOffset + new Vector3(0f, bobOffset, 0f);
            attachedBitDrop.transform.localPosition = targetPosition;
        }
    }
    private void FixedUpdate()
    {
        if (Mathf.Abs(transform.position.y - groundY) > 0.1f)
        {
            transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
        }
        if (playerTarget == null) return;
        if (isFleeing)
        {
            HandleFleeing();
            return;
        }
        if (hasCollidedWithPlayer) return;
        float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);
        if (distanceToPlayer <= detectionRange && distanceToPlayer > minFollowDistance)
        {
            if (!isFollowing)
            {
                                        DebugLog($"CrawlingEntity {gameObject.name} detected player at distance {distanceToPlayer:F1}, starting to follow...");
                isFollowing = true;
                accelerationTimer = 0f;
            }
            float direction = Mathf.Sign(playerTarget.position.x - transform.position.x);
            Vector3 targetPosition = playerTarget.position + new Vector3(direction * playerOffset, 0, 0);
            float distanceToTarget = Vector2.Distance(transform.position, targetPosition);
            float currentMoveForce = moveForce;
            currentMoveForce *= accelerationMultiplier;
            if (!wasMoving || Mathf.Sign(direction) != Mathf.Sign(lastMoveDirection))
            {
                currentMoveForce += initialBoostForce;
                accelerationTimer = 0f;
            }
            accelerationTimer += Time.fixedDeltaTime;
            float accelerationProgress = Mathf.Clamp01(accelerationTimer / accelerationTime);
            currentMoveForce *= (1f + accelerationProgress * 0.5f);
            Vector2 moveDirection = (targetPosition - transform.position).normalized;
            rb.AddForce(moveDirection * currentMoveForce, ForceMode2D.Force);
            if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
            {
                lastMoveDirection = Mathf.Sign(rb.linearVelocity.x);
            }
            FlipSpriteToFaceDirection();
            if (Mathf.Abs(rb.linearVelocity.x) > maxSpeed)
            {
                rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * maxSpeed, 0f);
            }
            wasMoving = true;
                                DebugLog($"CrawlingEntity {gameObject.name} following player with offset, distance to target: {distanceToTarget:F1}");
        }
        else if (distanceToPlayer <= minFollowDistance + playerOffset)
        {
            rb.linearVelocity = Vector2.zero;
            isFollowing = false;
            wasMoving = false;
            accelerationTimer = 0f;
                            DebugLog($"CrawlingEntity {gameObject.name} reached player with offset, stopping at distance {distanceToPlayer:F1}");
        }
        else if (distanceToPlayer > detectionRange)
        {
            if (isFollowing)
            {
                DebugLog($"CrawlingEntity {gameObject.name} lost player, stopping (distance: {distanceToPlayer:F1})");
                isFollowing = false;
            }
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, 0.1f);
            wasMoving = false;
            accelerationTimer = 0f;
        }
    }
    private void HandleFleeing()
    {
        if (fleeTarget == Vector3.zero) return;
        float distanceToFleeTarget = Vector3.Distance(transform.position, fleeTarget);
        if (distanceToFleeTarget < 1f)
        {
            DropStolenBit();
            Destroy(gameObject);
            return;
        }
        MoveTowardsTarget(fleeTarget, fleeSpeed);
    }
    private void FlipSpriteToFaceDirection()
    {
        if (spriteRenderer != null)
        {
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
                StealBitFromPlayer();
                StartFleeing();
            }
            else if (chosenMechanic == StealMechanic.StealFromDeposit)
            {
                if (!isMovingToDeposit && depositTarget != null)
                {
                    StartMovingToDeposit();
                }
            }
        }
    }
    private void StealBitFromPlayer()
    {
        if (attachedBitData != null)
        {
            DebugLog($"CrawlingEntity {gameObject.name} already has a bit attached ({attachedBitData.BitName}), won't steal another one!");
            return;
        }
        if (playerTarget != null)
        {
            PowerBitPlayerController playerController = playerTarget.GetComponent<PowerBitPlayerController>();
            if (playerController != null)
            {
                Bit stolenBit = playerController.StealRandomBitFromBuild();
                if (stolenBit != null)
                {
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
            attachedBitDrop = new GameObject($"AttachedBitDrop_{attachedBitData.BitName}");
            attachedBitDrop.transform.SetParent(transform);
            SpriteRenderer bitSprite = attachedBitDrop.AddComponent<SpriteRenderer>();
            bitSprite.sprite = attachedBitData.GetSprite();
            bitSprite.sortingOrder = 1;
            originalBitDropOffset = bitDropOffset;
            UpdateAttachedBitDropPosition();
            DebugLog($"CrawlingEntity {gameObject.name} created attached bit drop: {attachedBitData.BitName}");
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.layer == 3)
        {
            DebugLog($"CrawlingEntity {gameObject.name} unexpected collision with player!");
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minFollowDistance);
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
        if (isFollowing && playerTarget != null && chosenMechanic == StealMechanic.StealFromPlayer)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, playerTarget.position);
        }
        if (isMovingToDeposit && depositTarget != null && chosenMechanic == StealMechanic.StealFromDeposit)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, depositTarget.position);
        }
        if (isFollowingGatherer && gathererTarget != null && chosenMechanic == StealMechanic.FollowGatherer)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, gathererTarget.position);
        }
        if (isCarryingGatherer)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(transform.position + gathererCarryOffset, Vector3.one * 0.5f);
        }
    }
    public void ResetEntity()
    {
        hasCollidedWithPlayer = false;
        isFollowing = false;
        isFollowingGatherer = false;
        isFleeing = false;
        isMovingToDeposit = false;
        if (isCarryingGatherer)
        {
            DropGatherer();
        }
        rb.linearVelocity = Vector2.zero;
        accelerationTimer = 0f;
        wasMoving = false;
        DebugLog($"CrawlingEntity {gameObject.name} has been reset");
    }
    public bool IsFollowing()
    {
        return isFollowing;
    }
    public bool HasCollidedWithPlayer()
    {
        return hasCollidedWithPlayer;
    }
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        DebugLog($"CrawlingEntity {gameObject.name} took {damage} damage! Health: {currentHealth}/{maxHealth}");
        if (currentHealth <= 0)
        {
            DebugLog($"CrawlingEntity {gameObject.name} destroyed!");
            if (isCarryingGatherer)
            {
                DropGatherer();
            }
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
            Vector3 dropPosition = transform.position + Vector3.up * 0.5f;
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
        if (BitManager.Instance != null)
        {
            Bit randomBit = BitManager.Instance.GetRandomBit();
            if (randomBit != null)
            {
                Vector3 dropPosition = transform.position + Vector3.up * 0.5f;
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
            HandleGathererFollowing();
        }
        else
        {
            HandleNormalBehavior();
        }
        UpdateAttachedBitDropPosition();
    }
    private void HandleMovingToDeposit()
    {
        if (depositTarget == null) return;
        Vector3 directionToDeposit = (depositTarget.position - transform.position).normalized;
        float distanceToDeposit = Vector3.Distance(transform.position, depositTarget.position);
        if (distanceToDeposit < 2.0f)
        {
            DebugLog($"CrawlingEntity {gameObject.name} reached deposit, stealing and fleeing");
            StealFromDeposit();
            StartFleeing();
            return;
        }
        MoveTowardsTarget(depositTarget.position, moveForce);
    }
    private void HandleGathererFollowing()
    {
        bool shouldSearchForGatherer = gathererTarget == null ||
                                       (Time.time - lastGathererSearchTime) > gathererSearchInterval;
        if (shouldSearchForGatherer)
        {
            lastGathererSearchTime = Time.time;
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
        if (gathererTarget != null && !isCarryingGatherer)
        {
            float distanceToGatherer = Vector3.Distance(transform.position, gathererTarget.position);
            if (distanceToGatherer <= gathererTakeDistance)
            {
                TakeGatherer(gathererTarget.gameObject);
            }
            else if (distanceToGatherer > minFollowDistance)
            {
                MoveTowardsTarget(gathererTarget.position, moveForce);
                if (!isFollowing)
                {
                    isFollowing = true;
                    DebugLog($"CrawlingEntity {gameObject.name} started following gatherer at distance {distanceToGatherer:F1}");
                }
            }
            else
            {
                StopMovement();
            }
        }
        else if (isCarryingGatherer)
        {
            UpdateCarriedGathererPosition();
            StopMovement();
            isFollowing = false;
        }
        else
        {
            StopMovement();
            isFollowing = false;
        }
    }
    private void HandleNormalBehavior()
    {
        if (playerTarget == null) return;
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        if (distanceToPlayer <= detectionRange && !hasCollidedWithPlayer)
        {
            if (!isFollowing)
            {
                StartCoroutine(StartFollowing());
            }
            if (isFollowing)
            {
                MoveTowardsTarget(playerTarget.position, moveForce);
            }
        }
        else
        {
            isFollowing = false;
            StopMovement();
        }
    }
    private void StealFromDeposit()
    {
        if (attachedBitDrop != null) return;
        DepositInteraction deposit = depositTarget?.GetComponent<DepositInteraction>();
        if (deposit != null && deposit.RemoveCoreBit())
        {
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
        rb.AddForce(new Vector2(direction.x * force, 0f), ForceMode2D.Force);
        if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
        {
            lastMoveDirection = Mathf.Sign(rb.linearVelocity.x);
        }
        FlipSpriteToFaceDirection();
        if (Mathf.Abs(rb.linearVelocity.x) > maxSpeed)
        {
            rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * maxSpeed, rb.linearVelocity.y);
        }
    }
    private void StartFleeing()
    {
        float fleeX = Random.value > 0.5f ? -40f : 40f;
        fleeTarget = new Vector3(fleeX, groundY, transform.position.z);
        isFleeing = true;
        isMovingToDeposit = false;
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
        gatherer.transform.SetParent(transform);
        UpdateCarriedGathererPosition();
        gathererTarget = null;
        isFollowingGatherer = false;
        StartFleeing();
        DebugLog($"CrawlingEntity {gameObject.name} successfully took gatherer {gatherer.name} and is now fleeing!");
    }
    private void UpdateCarriedGathererPosition()
    {
        if (carriedGatherer != null)
        {
            carriedGatherer.transform.localPosition = gathererCarryOffset;
        }
    }
    private void DropGatherer()
    {
        if (!isCarryingGatherer || carriedGatherer == null) return;
        DebugLog($"CrawlingEntity {gameObject.name} is dropping gatherer: {carriedGatherer.name}");
        carriedGatherer.transform.SetParent(null);
        Vector3 dropPosition = transform.position + Vector3.right * 2f;
        dropPosition.y = groundY;
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
        DebugLog($"CrawlingEntity {gameObject.name} dropped gatherer successfully!");
    }
}