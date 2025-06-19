using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PowerBitPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Character Settings")]
    [SerializeField] private PowerBitCharacterRenderer powerBitCharacterRenderer;

    [Header("Combat Settings")]
    [SerializeField] private float shootingCooldown = 0.1f;
    [SerializeField] private float overheatBuildRate = 0.1f;
    [SerializeField] private float overheatMax = 1f;
    [SerializeField] private float overheatCooldownTime = 5f;

    [Header("Debug")]
    [SerializeField] private bool showGroundCheckDebug = false;
    [SerializeField] private bool showDebugInfo = false;
    private bool wasGrounded = false;

    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private float horizontalInput;
    private float overheatLevel = 0f;
    private bool isOverheated = false;
    private float overheatTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        
        if (powerBitCharacterRenderer == null)
        {
            powerBitCharacterRenderer = GetComponentInChildren<PowerBitCharacterRenderer>();
            if (powerBitCharacterRenderer == null)
            {
                Debug.LogError("PowerBitCharacterRenderer not found! Please assign it in the inspector or ensure it exists as a child GameObject.");
            }
        }

        // Configure Rigidbody2D
        rb.gravityScale = 1f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void Start()
    {
        // Load the last saved Smith build
        LoadLastSavedSmithBuild();
    }

    public void LoadLastSavedSmithBuild()
    {
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, "smith_build.json");
        Debug.Log($"Attempting to load Smith build from: {filePath}");
        
        if (!System.IO.File.Exists(filePath))
        {
            Debug.LogWarning("No saved Smith build found at: " + filePath);
            return;
        }

        try
        {
            string json = System.IO.File.ReadAllText(filePath);
            Debug.Log($"Read JSON data: {json}");
            
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("JSON file is empty!");
                return;
            }

            SmithGridStateData gridState = JsonUtility.FromJson<SmithGridStateData>(json);
            
            if (gridState == null)
            {
                Debug.LogError("Failed to parse Smith build from JSON - gridState is null");
                return;
            }

            if (gridState.cells == null)
            {
                Debug.LogError("Grid state cells collection is null!");
                return;
            }

            Debug.Log($"Successfully parsed Smith grid state. Grid size: {gridState.gridSize}, Cells count: {gridState.cells.Count}");
            
            if (powerBitCharacterRenderer == null)
            {
                Debug.LogError("PowerBitCharacterRenderer component is not assigned!");
                return;
            }

            LoadSmithBuild(gridState);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading Smith build: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }

    private void Update()
    {
        // Check if player is grounded
        bool isGrounded = CheckGrounded();
        
        // Only log ground state changes if debug is enabled
        if (showGroundCheckDebug && isGrounded != wasGrounded)
        {
            Debug.Log($"Player {(isGrounded ? "landed on" : "left")} the ground");
            wasGrounded = isGrounded;
        }

        // Handle movement
        float moveInput = Input.GetAxisRaw("Horizontal");
        Vector2 velocity = rb.linearVelocity;
        
        // Apply movement speed penalty based on number of Power Bits
        float speedMultiplier = GetMovementSpeedMultiplier();
        velocity.x = moveInput * moveSpeed * speedMultiplier;
        rb.linearVelocity = velocity;

        // Handle shooting
        HandleShooting();

        // Handle overheat
        HandleOverheat();
    }

    private float GetMovementSpeedMultiplier()
    {
        if (powerBitCharacterRenderer == null) return 1f;
        
        int bitCount = powerBitCharacterRenderer.GetActiveBits().Count;
        int maxBits = 4; // 2x2 grid for now
        
        if (bitCount == 0) return 1f; // No bits = 100% speed
        if (bitCount >= maxBits) return 0.3f; // Full build = 30% speed
        
        // Linear interpolation between 100% and 30%
        return Mathf.Lerp(1f, 0.3f, (float)bitCount / maxBits);
    }

    private void HandleShooting()
    {
        if (isOverheated) return;

        if (Input.GetMouseButton(0)) // Left click to shoot
        {
            // Build up overheat
            overheatLevel += overheatBuildRate * Time.deltaTime;
            
            if (overheatLevel >= overheatMax)
            {
                isOverheated = true;
                overheatTimer = overheatCooldownTime;
                Debug.Log("Player overheated! 5-second cooldown.");
            }
            else
            {
                // Shoot based on Power Bits
                Shoot();
            }
        }
    }

    private void HandleOverheat()
    {
        if (isOverheated)
        {
            overheatTimer -= Time.deltaTime;
            if (overheatTimer <= 0f)
            {
                isOverheated = false;
                overheatLevel = 0f;
                Debug.Log("Overheat cooldown finished!");
            }
        }
        else if (overheatLevel > 0f)
        {
            // Gradually reduce overheat when not shooting
            overheatLevel -= overheatBuildRate * Time.deltaTime * 0.5f;
            overheatLevel = Mathf.Max(0f, overheatLevel);
        }
    }

    private void Shoot()
    {
        if (powerBitCharacterRenderer == null) return;

        int totalDamage = powerBitCharacterRenderer.GetTotalDamage();
        float shootingProbability = powerBitCharacterRenderer.GetAverageShootingProbability();

        // Determine if this shot uses Power Bit damage or basic damage
        bool usePowerBitDamage = Random.value < shootingProbability;
        
        int damage = usePowerBitDamage ? totalDamage : 1;
        string damageType = usePowerBitDamage ? "Power Bit" : "Basic";

        Debug.Log($"Shot fired! Damage: {damage} ({damageType}) - Probability: {shootingProbability:P1}");
        
        // TODO: Implement actual projectile spawning and enemy damage
    }

    private bool CheckGrounded()
    {
        // Get the bottom center of the collider
        Vector2 boxCenter = boxCollider.bounds.center;
        Vector2 boxSize = boxCollider.bounds.size;
        Vector2 rayStart = new Vector2(boxCenter.x, boxCenter.y - boxSize.y/2);

        // Cast a ray downward
        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, groundCheckDistance, groundLayer);
        
        if (showGroundCheckDebug)
        {
            Debug.DrawRay(rayStart, Vector2.down * groundCheckDistance, hit.collider != null ? Color.green : Color.red);
        }

        return hit.collider != null;
    }

    public void LoadSmithBuild(SmithGridStateData gridState)
    {
        if (powerBitCharacterRenderer != null)
        {
            powerBitCharacterRenderer.LoadCharacterFromSmithBuild(gridState);
            UpdateColliderSize();

            Debug.Log("Smith build loaded into character!");
            Debug.Log($"Total Power Bits: {gridState.cells.Count}");
            
            // Debug print all Power Bits
            foreach (var cell in gridState.cells)
            {
                Debug.Log($"Power Bit at ({cell.x}, {cell.y}): {cell.bitName} ({cell.rarity}) - Damage: {cell.damage}");
            }
            
            // Print total stats
            Debug.Log($"Total Damage: {powerBitCharacterRenderer.GetTotalDamage()}");
            Debug.Log($"Average Shooting Probability: {powerBitCharacterRenderer.GetAverageShootingProbability():P1}");
            Debug.Log($"Movement Speed Multiplier: {GetMovementSpeedMultiplier():P1}");
        }
        else
        {
            Debug.LogWarning("PowerBitCharacterRenderer not found! Cannot load Smith build.");
        }
    }

    public void TakeDamage(int damage)
    {
        if (powerBitCharacterRenderer != null)
        {
            // Get outer bits that can be damaged
            var outerBits = powerBitCharacterRenderer.GetOuterBits();
            if (outerBits.Count > 0)
            {
                // Remove a random outer bit
                int randomIndex = Random.Range(0, outerBits.Count);
                Vector2Int bitToRemove = outerBits[randomIndex];
                var bitData = powerBitCharacterRenderer.GetBitAt(bitToRemove);
                
                Debug.Log($"Removing Power Bit at ({bitToRemove.x}, {bitToRemove.y}): {bitData?.bitName} ({bitData?.rarity})");
                
                powerBitCharacterRenderer.RemoveBit(bitToRemove);
                UpdateColliderSize();

                // Print remaining bits
                var remainingBits = powerBitCharacterRenderer.GetActiveBits();
                Debug.Log($"Remaining Power Bits: {remainingBits.Count}");
                Debug.Log($"Total Damage: {powerBitCharacterRenderer.GetTotalDamage()}");
                Debug.Log($"Movement Speed Multiplier: {GetMovementSpeedMultiplier():P1}");
            }
            else
            {
                Debug.Log("No outer Power Bits available to damage!");
            }
        }
    }

    private void UpdateColliderSize()
    {
        if (powerBitCharacterRenderer == null || boxCollider == null) return;

        // Get the bounds of the character from the renderer
        Bounds characterBounds = powerBitCharacterRenderer.GetCharacterBounds();
        
        // The character renderer is a child, so its bounds are already in local space relative to its own transform.
        // Assuming the renderer's transform is at (0,0,0) relative to the player, we can use the bounds directly.
        Vector3 localCenter = characterBounds.center;
        Vector3 localSize = characterBounds.size;

        // Ensure minimum collider size in case grid size is zero
        localSize.x = Mathf.Max(localSize.x, 0.25f);
        localSize.y = Mathf.Max(localSize.y, 0.25f);

        // Update the collider size and offset
        boxCollider.size = new Vector2(localSize.x, localSize.y);
        boxCollider.offset = new Vector2(localCenter.x, localCenter.y);

        if (showDebugInfo)
        {
            Debug.Log($"Collider size set to: {boxCollider.size}");
            Debug.Log($"Collider offset set to: {boxCollider.offset}");
            Debug.Log($"Using fixed grid bounds. Local center: {localCenter}, Local size: {localSize}");
        }
    }

    // Public getters for UI or other systems
    public float GetOverheatLevel() => overheatLevel;
    public float GetOverheatMax() => overheatMax;
    public bool IsOverheated() => isOverheated;
    public float GetOverheatTimer() => overheatTimer;
    public int GetTotalDamage() => powerBitCharacterRenderer?.GetTotalDamage() ?? 0;
    public float GetShootingProbability() => powerBitCharacterRenderer?.GetAverageShootingProbability() ?? 0f;
    public int GetPowerBitCount() => powerBitCharacterRenderer?.GetActiveBits().Count ?? 0;
    public float GetCurrentMovementSpeedMultiplier() => GetMovementSpeedMultiplier();
} 