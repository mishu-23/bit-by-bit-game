using UnityEngine;
using BitByBit.Core;
using System.Collections;
public class FlyingEntity : MonoBehaviour, IDamageable
{
    [Header("Target")]
    public Transform playerTarget;
    [Header("Movement")]
    public float moveForce = 6f;
    public float maxSpeed = 3f;
    public float detectionRange = 8f;
    public float minFollowDistance = 1.5f;
    public float maxHeight = 5f;
    public float minHeight = 0.5f;
    [Header("Combat")]
    public int maxHealth = 10;
    public int currentHealth;
    [Header("Behavior")]
    public bool isFollowing = false;
    public float followDelay = 0.3f;
    [Header("Stealing & Fleeing")]
    public float fleeSpeed = 6f;
    public float fleeDistance = 40f;
    [Header("Visual")]
    public bool enableSpriteFlipping = true;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private bool hasCollidedWithPlayer = false;
    private bool facingRight = true;
    private bool hasStolenBit = false;
    private bool isFleeing = false;
    private Vector3 fleeTarget;
    private void Awake()
    {
        gameObject.layer = 6;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
        Physics2D.IgnoreLayerCollision(6, 3, true);
        if (spriteRenderer == null && enableSpriteFlipping)
        {
            Debug.LogWarning($"FlyingEntity {gameObject.name}: SpriteRenderer not found! Sprite flipping will be disabled.");
            enableSpriteFlipping = false;
        }
    }
    private void Start()
    {
        InitializePlayerTarget();
    }
    private void InitializePlayerTarget()
    {
        if (playerTarget == null)
        {
            if (GameReferences.Instance != null && GameReferences.Instance.Player != null)
            {
                playerTarget = GameReferences.Instance.Player;
                Debug.Log($"FlyingEntity {gameObject.name} found player via GameReferences: {playerTarget.name}");
            }
            else
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerTarget = player.transform;
                    Debug.Log($"FlyingEntity {gameObject.name} found player via fallback: {player.name}");
                    Debug.LogWarning("FlyingEntity: Found player via fallback method. Please ensure GameReferences is properly configured.");
                }
                else
                {
                    Debug.LogError($"FlyingEntity {gameObject.name} couldn't find player with tag 'Player'!");
                }
            }
        }
    }
    private void FixedUpdate()
    {
        if (playerTarget == null) return;
        if (isFleeing)
        {
            HandleFleeing();
            return;
        }
        if (hasCollidedWithPlayer || hasStolenBit) return;
        Vector2 distanceToPlayer = playerTarget.position - transform.position;
        float distanceMagnitude = distanceToPlayer.magnitude;
        if (distanceMagnitude <= detectionRange && distanceMagnitude > minFollowDistance)
        {
            if (!isFollowing)
            {
                Debug.Log($"FlyingEntity {gameObject.name} detected player at distance {distanceMagnitude:F1}, starting to follow...");
                isFollowing = true;
            }
            Vector2 direction = distanceToPlayer.normalized;
            rb.AddForce(direction * moveForce, ForceMode2D.Force);
            if (enableSpriteFlipping && spriteRenderer != null)
            {
                HandleSpriteFlipping(direction.x);
            }
            if (rb.linearVelocity.magnitude > maxSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
            }
            Vector3 currentPos = transform.position;
            if (currentPos.y > maxHeight)
            {
                currentPos.y = maxHeight;
                transform.position = currentPos;
                Vector2 velocity = rb.linearVelocity;
                if (velocity.y > 0) velocity.y = 0;
                rb.linearVelocity = velocity;
            }
            else if (currentPos.y < minHeight)
            {
                currentPos.y = minHeight;
                transform.position = currentPos;
                Vector2 velocity = rb.linearVelocity;
                if (velocity.y < 0) velocity.y = 0;
                rb.linearVelocity = velocity;
            }
            Debug.Log($"FlyingEntity {gameObject.name} following player, distance: {distanceMagnitude:F1}");
        }
        else if (distanceMagnitude <= minFollowDistance)
        {
            rb.linearVelocity = Vector2.zero;
            isFollowing = false;
            if (!hasStolenBit)
            {
                StealBitFromPlayer();
            }
        }
        else if (distanceMagnitude > detectionRange)
        {
            if (isFollowing)
            {
                Debug.Log($"FlyingEntity {gameObject.name} lost player, stopping (distance: {distanceMagnitude:F1})");
                isFollowing = false;
            }
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, 0.1f);
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.gameObject.layer == 3)
        {
            hasCollidedWithPlayer = true;
            rb.linearVelocity = Vector2.zero;
            Debug.Log($"FlyingEntity {gameObject.name} collided with player!");
            if (!hasStolenBit)
            {
                StealBitFromPlayer();
            }
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minFollowDistance);
        Gizmos.color = Color.cyan;
        Vector3 topPos = transform.position;
        topPos.y = maxHeight;
        Gizmos.DrawWireSphere(topPos, 0.2f);
        Vector3 bottomPos = transform.position;
        bottomPos.y = minHeight;
        Gizmos.DrawWireSphere(bottomPos, 0.2f);
        Gizmos.DrawLine(new Vector3(transform.position.x - 2f, maxHeight, 0f),
                       new Vector3(transform.position.x + 2f, maxHeight, 0f));
        Gizmos.DrawLine(new Vector3(transform.position.x - 2f, minHeight, 0f),
                       new Vector3(transform.position.x + 2f, minHeight, 0f));
        if (isFollowing)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
        else
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, 0.2f);
        }
        if (isFollowing && playerTarget != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, playerTarget.position);
        }
        if (rb != null && rb.linearVelocity.magnitude > 0.1f)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, rb.linearVelocity);
        }
    }
    public void ResetEntity()
    {
        hasCollidedWithPlayer = false;
        isFollowing = false;
        hasStolenBit = false;
        isFleeing = false;
        fleeTarget = Vector3.zero;
        rb.linearVelocity = Vector2.zero;
        if (enableSpriteFlipping && spriteRenderer != null)
        {
            facingRight = true;
            spriteRenderer.flipX = false;
        }
        Debug.Log($"FlyingEntity {gameObject.name} has been reset");
    }
    public bool IsFollowing()
    {
        return isFollowing;
    }
    public bool HasCollidedWithPlayer()
    {
        return hasCollidedWithPlayer;
    }
    public Vector2 GetVelocity()
    {
        return rb != null ? rb.linearVelocity : Vector2.zero;
    }
    private void HandleSpriteFlipping(float horizontalDirection)
    {
        if (Mathf.Abs(horizontalDirection) > 0.1f)
        {
            bool shouldFaceRight = horizontalDirection > 0;
            if (shouldFaceRight != facingRight)
            {
                facingRight = shouldFaceRight;
                spriteRenderer.flipX = !facingRight;
                Debug.Log($"FlyingEntity {gameObject.name} flipped to face {(facingRight ? "right" : "left")}");
            }
        }
    }
    public void SetFacingDirection(bool faceRight)
    {
        if (enableSpriteFlipping && spriteRenderer != null)
        {
            facingRight = faceRight;
            spriteRenderer.flipX = !facingRight;
        }
    }
    public bool IsFacingRight()
    {
        return facingRight;
    }
    private void StealBitFromPlayer()
    {
        if (hasStolenBit) return;
        if (playerTarget != null)
        {
            PowerBitPlayerController playerController = playerTarget.GetComponent<PowerBitPlayerController>();
            if (playerController != null)
            {
                Bit stolenBit = playerController.StealRandomBitFromBuild();
                if (stolenBit != null)
                {
                    hasStolenBit = true;
                    Debug.Log($"FlyingEntity {gameObject.name} ate {stolenBit.BitName} from player's build!");
                    StartFleeing();
                }
                else
                {
                    Debug.Log($"FlyingEntity {gameObject.name} tried to eat a bit but player's build is empty!");
                    StartFleeing();
                }
            }
            else
            {
                Debug.LogWarning($"FlyingEntity {gameObject.name} couldn't find PowerBitPlayerController on player!");
                StartFleeing();
            }
        }
    }
    private void StartFleeing()
    {
        float horizontalDirection = Random.value > 0.5f ? -1f : 1f;
        float fleeAngleRadians = 30f * Mathf.Deg2Rad;
        float fleeX = transform.position.x + (horizontalDirection * fleeDistance * Mathf.Cos(fleeAngleRadians));
        float fleeY = transform.position.y + (fleeDistance * Mathf.Sin(fleeAngleRadians));
        fleeTarget = new Vector3(fleeX, fleeY, transform.position.z);
        isFleeing = true;
        isFollowing = false;
        Debug.Log($"FlyingEntity {gameObject.name} fleeing at 30° angle to ({fleeX:F1}, {fleeY:F1})");
    }
    private void HandleFleeing()
    {
        if (fleeTarget == Vector3.zero) return;
        Vector2 directionToFleeTarget = (fleeTarget - transform.position).normalized;
        float distanceToFleeTarget = Vector2.Distance(transform.position, fleeTarget);
        float currentX = transform.position.x;
        if (Mathf.Abs(currentX) >= fleeDistance * 0.9f)
        {
            Debug.Log($"FlyingEntity {gameObject.name} despawning at x = {currentX}");
            Destroy(gameObject);
            return;
        }
        if (distanceToFleeTarget > 1f)
        {
            rb.AddForce(directionToFleeTarget * fleeSpeed, ForceMode2D.Force);
            if (enableSpriteFlipping && spriteRenderer != null)
            {
                HandleSpriteFlipping(directionToFleeTarget.x);
            }
            if (rb.linearVelocity.magnitude > fleeSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * fleeSpeed;
            }
        }
        else
        {
            Debug.Log($"FlyingEntity {gameObject.name} reached flee target and despawning");
            Destroy(gameObject);
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
                        Debug.Log($"FlyingEntity {gameObject.name} dropped {randomBit.BitName} (Type: {randomBit.BitType}, Rarity: {randomBit.Rarity})!");
                    }
                    else
                    {
                        Debug.LogError("BitDrop prefab doesn't have BitDrop component!");
                        Destroy(bitDropObj);
                    }
                }
                else
                {
                    Debug.LogError("BitDrop prefab reference not set! Cannot create bit drop.");
                }
            }
            else
            {
                Debug.LogWarning("BitManager returned null bit for drop!");
            }
        }
        else
        {
            Debug.LogWarning("BitManager not found! Cannot drop bit!");
        }
    }
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"FlyingEntity {gameObject.name} took {damage} damage! Health: {currentHealth}/{maxHealth}");
        if (currentHealth <= 0)
        {
            Debug.Log($"FlyingEntity {gameObject.name} died!");
            DropRandomBit();
            Destroy(gameObject);
        }
    }
}