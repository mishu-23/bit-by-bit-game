using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GathererEntity : MonoBehaviour
{
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
    private float targetX;
    private bool isMoving;
    private GathererState state;

    public enum GathererState
    {
        MovingToGather,
        Gathering,
        MovingToDeposit,
        Depositing,
        Wandering
    }

    private void Awake()
    {
        gameObject.layer = 6; // Entity layer
        rb = GetComponent<Rigidbody2D>();
        
        // Ignore collisions with player layer (assuming player is on layer 3)
        Physics2D.IgnoreLayerCollision(6, 3, true);  // Entity vs Player
    }

    private void Start()
    {
        transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
        
        // Auto-find points if not assigned (for prefab compatibility)
        FindPointsIfNotAssigned();
        
        StartGathering();
    }

    private void FindPointsIfNotAssigned()
    {
        // Find points by name if not assigned
        if (minePoint == null)
        {
            GameObject mine = GameObject.Find("Mine");
            if (mine != null)
            {
                minePoint = mine.transform;
                Debug.Log($"GathererEntity {gameObject.name}: Auto-found Mine point");
            }
            else
            {
                Debug.LogError($"GathererEntity {gameObject.name}: Could not find Mine point! Please assign it manually.");
            }
        }
        
        if (treePoint == null)
        {
            GameObject tree = GameObject.Find("Tree");
            if (tree != null)
            {
                treePoint = tree.transform;
                Debug.Log($"GathererEntity {gameObject.name}: Auto-found Tree point");
            }
            else
            {
                Debug.LogError($"GathererEntity {gameObject.name}: Could not find Tree point! Please assign it manually.");
            }
        }
        
        if (depositPoint == null)
        {
            GameObject deposit = GameObject.Find("Deposit");
            if (deposit != null)
            {
                depositPoint = deposit.transform;
                Debug.Log($"GathererEntity {gameObject.name}: Auto-found Deposit point");
            }
            else
            {
                Debug.LogError($"GathererEntity {gameObject.name}: Could not find Deposit point! Please assign it manually.");
            }
        }
    }

    private void FixedUpdate()
    {
        // Keep at ground level
        if (Mathf.Abs(transform.position.y - groundY) > 0.1f)
        {
            transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
        }

        // Simple movement
        if (isMoving)
        {
            float direction = Mathf.Sign(targetX - transform.position.x);
            rb.AddForce(new Vector2(direction * moveForce, 0f), ForceMode2D.Force);
            
            // Limit speed
            if (Mathf.Abs(rb.linearVelocity.x) > maxSpeed)
            {
                rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * maxSpeed, 0f);
            }

            // Stop if close enough
            if (Mathf.Abs(transform.position.x - targetX) <= arrivalDistance)
            {
                rb.linearVelocity = Vector2.zero;
                isMoving = false;
                OnReachedTarget();
            }
        }
        else
        {
            // Apply brakes
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, 0.1f);
        }
    }

    private void StartGathering()
    {
        state = GathererState.MovingToGather;
        Transform target = Random.value > 0.5f ? minePoint : treePoint;
        string resourceType = (target == minePoint) ? "Mine" : "Tree";
        Debug.Log($"Entity {gameObject.name} moving to {resourceType} at {target.position.x}");
        MoveTo(target.position.x);
    }

    private void MoveTo(float x)
    {
        targetX = x;
        isMoving = true;
        Debug.Log($"Entity {gameObject.name} moving to X: {x}");
    }

    private void OnReachedTarget()
    {
        switch (state)
        {
            case GathererState.MovingToGather:
                Debug.Log($"Entity {gameObject.name} reached gathering point, starting to gather...");
                StartCoroutine(Gather());
                break;
            case GathererState.MovingToDeposit:
                Debug.Log($"Entity {gameObject.name} reached deposit point, starting to deposit...");
                StartCoroutine(Deposit());
                break;
        }
    }

    private IEnumerator Gather()
    {
        state = GathererState.Gathering;
        Debug.Log($"Entity {gameObject.name} gathering for {gatherTime} seconds...");
        yield return new WaitForSeconds(gatherTime);
        
        state = GathererState.MovingToDeposit;
        Debug.Log($"Entity {gameObject.name} finished gathering, moving to deposit at {depositPoint.position.x}");
        MoveTo(depositPoint.position.x);
    }

    private IEnumerator Deposit()
    {
        state = GathererState.Depositing;
        Debug.Log($"Entity {gameObject.name} depositing for {depositTime} seconds...");
        yield return new WaitForSeconds(depositTime);
        
        Debug.Log($"Entity {gameObject.name} finished depositing, starting wander phase...");
        StartCoroutine(Wander());
    }

    private IEnumerator Wander()
    {
        state = GathererState.Wandering;
        Debug.Log($"Entity {gameObject.name} wandering for {wanderTime} seconds...");
        float endTime = Time.time + wanderTime;
        
        while (Time.time < endTime)
        {
            float randomX = transform.position.x + Random.Range(-3f, 3f);
            Debug.Log($"Entity {gameObject.name} wandering to X: {randomX}");
            MoveTo(randomX);
            
            // Wait until we reach the random point
            while (isMoving && Time.time < endTime)
            {
                yield return null;
            }
            
            float pause = Random.Range(0.5f, 1.5f);
            Debug.Log($"Entity {gameObject.name} pausing for {pause} seconds...");
            yield return new WaitForSeconds(pause);
        }
        
        Debug.Log($"Entity {gameObject.name} finished wandering, starting new gathering cycle...");
        StartGathering(); // Start new cycle
    }

    private void OnDrawGizmos()
    {
        // Show current target
        if (isMoving)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, new Vector3(targetX, groundY, 0f));
            Gizmos.DrawWireSphere(new Vector3(targetX, groundY, 0f), 0.3f);
        }

        // Show state
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"Entity collided with: {collision.gameObject.name} on layer {collision.gameObject.layer}");
        
        // If it's the player, ignore the collision
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.layer == 8)
        {
            Physics2D.IgnoreCollision(GetComponent<Collider2D>(), collision.collider, true);
        }
    }
} 