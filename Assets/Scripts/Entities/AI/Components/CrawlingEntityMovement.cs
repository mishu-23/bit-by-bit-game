using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class CrawlingEntityMovement : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Movement Settings")]
    [SerializeField] private float moveForce = 8f;
    [SerializeField] private float maxSpeed = 4f;
    [SerializeField] private float groundY = 0.5f;
    [SerializeField] private float fleeSpeed = 6f;
    
    [Header("Acceleration")]
    [SerializeField] private float accelerationMultiplier = 2f;
    [SerializeField] private float initialBoostForce = 12f;
    [SerializeField] private float accelerationTime = 0.5f;
    
    #endregion

    #region Private Fields
    
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float lastMoveDirection = 1f; // 1 for right, -1 for left
    private float accelerationTimer = 0f;
    private bool wasMoving = false;
    
    #endregion

    #region Public Properties
    
    public float GroundY => groundY;
    public bool IsMoving => Mathf.Abs(rb.linearVelocity.x) > 0.1f;
    public Vector2 Velocity => rb.linearVelocity;
    public float MoveDirection => lastMoveDirection;
    
    #endregion

    #region Unity Lifecycle
    
    private void Awake()
    {
        InitializeComponents();
        SetGroundPosition();
    }
    
    private void FixedUpdate()
    {
        UpdateAcceleration();
        FlipSpriteToFaceDirection();
    }
    
    #endregion

    #region Initialization
    
    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    private void SetGroundPosition()
    {
        transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
    }
    
    #endregion

    #region Movement Control
    
    public void MoveTowardsTarget(Vector3 targetPosition, float customForce = 0f)
    {
        float forceToUse = customForce > 0f ? customForce : moveForce;
        Vector3 direction = (targetPosition - transform.position).normalized;
        
        ApplyMovementForce(direction, forceToUse);
        UpdateMovementDirection();
        LimitSpeed();
    }
    
    public void MoveTowardsTarget(Vector3 targetPosition)
    {
        MoveTowardsTarget(targetPosition, 0f);
    }
    
    public void MoveInDirection(Vector2 direction, float customForce = 0f)
    {
        float forceToUse = customForce > 0f ? customForce : moveForce;
        ApplyMovementForce(direction, forceToUse);
        UpdateMovementDirection();
        LimitSpeed();
    }
    
    public void StopMovement()
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
    
    public void SetVelocity(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
    }
    
    #endregion

    #region Movement Physics
    
    private void ApplyMovementForce(Vector3 direction, float force)
    {
        // Apply acceleration boost if just starting to move
        float finalForce = force;
        if (!wasMoving && accelerationTimer < accelerationTime)
        {
            finalForce += initialBoostForce * (1f - accelerationTimer / accelerationTime);
        }
        
        rb.AddForce(new Vector2(direction.x * finalForce * accelerationMultiplier, 0f), ForceMode2D.Force);
    }
    
    private void UpdateMovementDirection()
    {
        if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
        {
            lastMoveDirection = Mathf.Sign(rb.linearVelocity.x);
        }
    }
    
    private void LimitSpeed()
    {
        if (Mathf.Abs(rb.linearVelocity.x) > maxSpeed)
        {
            rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * maxSpeed, rb.linearVelocity.y);
        }
    }
    
    private void UpdateAcceleration()
    {
        bool isCurrentlyMoving = IsMoving;
        
        if (isCurrentlyMoving && !wasMoving)
        {
            // Just started moving
            accelerationTimer = 0f;
        }
        else if (isCurrentlyMoving)
        {
            // Continue moving, update acceleration timer
            accelerationTimer += Time.fixedDeltaTime;
        }
        
        wasMoving = isCurrentlyMoving;
    }
    
    #endregion

    #region Visual Updates
    
    private void FlipSpriteToFaceDirection()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = lastMoveDirection < 0f;
        }
    }
    
    #endregion

    #region Public Utilities
    
    public float GetDistanceToTarget(Vector3 targetPosition)
    {
        return Vector3.Distance(transform.position, targetPosition);
    }
    
    public Vector3 GetDirectionToTarget(Vector3 targetPosition)
    {
        return (targetPosition - transform.position).normalized;
    }
    
    public bool IsWithinDistance(Vector3 targetPosition, float distance)
    {
        return GetDistanceToTarget(targetPosition) <= distance;
    }
    
    #endregion
} 