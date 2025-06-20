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
    [SerializeField] private ProjectileSpawner projectileSpawner;

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
    private float lastShootTime = 0f;
    private bool isShooting = false;

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

        if (projectileSpawner == null)
        {
            projectileSpawner = GetComponentInChildren<ProjectileSpawner>();
            if (projectileSpawner == null)
            {
                Debug.LogError("ProjectileSpawner not found! Please assign it in the inspector or ensure it exists as a child GameObject.");
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
        // Check if we can shoot
        if (isOverheated) return;

        // Check shooting cooldown
        if (Time.time - lastShootTime < shootingCooldown) return;

        // Handle mouse input for shooting
        if (Input.GetMouseButton(0)) // Left click to shoot
        {
            if (!isShooting)
            {
                isShooting = true;
                if (showDebugInfo)
                {
                    Debug.Log("Started shooting");
                }
            }

            // Build up overheat
            overheatLevel += overheatBuildRate * Time.deltaTime;
            
            if (overheatLevel >= overheatMax)
            {
                isOverheated = true;
                overheatTimer = overheatCooldownTime;
                isShooting = false;
                Debug.Log("Player overheated! 5-second cooldown.");
            }
            else
            {
                // Shoot based on Power Bits
                Shoot();
                lastShootTime = Time.time;
            }
        }
        else
        {
            if (isShooting)
            {
                isShooting = false;
                if (showDebugInfo)
                {
                    Debug.Log("Stopped shooting");
                }
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
        if (powerBitCharacterRenderer == null || projectileSpawner == null) return;

        // Get aiming direction
        Vector2 aimDirection = projectileSpawner.GetAimingDirection();
        
        // Select which bit to use for this shot
        SmithCellData selectedBit = SelectBitForShot();
        
        if (selectedBit != null)
        {
            // Use Power Bit for shooting
            int damage = selectedBit.damage;
            Rarity rarity = selectedBit.rarity;
            string bitName = selectedBit.bitName;

            // Spawn projectile
            Projectile projectile = projectileSpawner.SpawnProjectile(rarity, damage, bitName);
            
            if (showDebugInfo)
            {
                Debug.Log($"Shot fired with Power Bit: {bitName} ({rarity}) - Damage: {damage}");
            }
        }
        else
        {
            // Use default bit (no Power Bits available or none triggered)
            Projectile projectile = projectileSpawner.SpawnProjectile(Rarity.Common, 1, "Default");
            
            if (showDebugInfo)
            {
                Debug.Log("Shot fired with default bit - Damage: 1");
            }
        }
    }
    
    private SmithCellData SelectBitForShot()
    {
        if (powerBitCharacterRenderer == null) return null;
        
        var activeBits = powerBitCharacterRenderer.GetActiveBits();
        if (activeBits.Count == 0) return null;
        
        // Create a list of bits with their probabilities
        List<SmithCellData> availableBits = new List<SmithCellData>();
        List<float> probabilities = new List<float>();
        
        foreach (var bitPos in activeBits)
        {
            SmithCellData bitData = powerBitCharacterRenderer.GetBitAt(bitPos);
            if (bitData != null)
            {
                availableBits.Add(bitData);
                probabilities.Add(bitData.shootingProbability);
            }
        }
        
        if (availableBits.Count == 0) return null;
        
        // Check if any bit triggers based on its probability
        for (int i = 0; i < availableBits.Count; i++)
        {
            if (Random.value < probabilities[i])
            {
                return availableBits[i];
            }
        }
        
        // No bit triggered, return null (will use default bit)
        return null;
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
            
            // Update spawn point position based on new grid size
            if (projectileSpawner != null)
            {
                projectileSpawner.RefreshSpawnPointPosition();
            }
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
    
    // Shooting-related getters
    public bool IsShooting() => isShooting;
    public float GetAimingAngle() => projectileSpawner?.GetAimingAngle() ?? 0f;
    public Vector2 GetAimingDirection() => projectileSpawner?.GetAimingDirection() ?? Vector2.right;
    public bool IsValidAimingDirection() => projectileSpawner?.IsValidAimingDirection() ?? false;
    public int GetActiveProjectileCount() => projectileSpawner?.GetActiveProjectileCount() ?? 0;
    
    // Get current shooting stats
    public string GetCurrentShootingStats()
    {
        if (powerBitCharacterRenderer == null) return "No Power Bits";
        
        var activeBits = powerBitCharacterRenderer.GetActiveBits();
        if (activeBits.Count == 0) return "No Power Bits";
        
        int totalDamage = powerBitCharacterRenderer.GetTotalDamage();
        float avgProbability = powerBitCharacterRenderer.GetAverageShootingProbability();
        
        return $"Power Bits: {activeBits.Count}, Total Damage: {totalDamage}, Avg Probability: {avgProbability:P1}";
    }
} 