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
    
    [Header("Movement Boundaries")]
    [SerializeField] private bool enableXBoundaries = true;
    [SerializeField] private float minX = -40f;
    [SerializeField] private float maxX = 40f;
    
    [Header("Rolling Settings")]
    [SerializeField] private bool enableRolling = true;
    [SerializeField] private float rollTorque = 10f;
    [SerializeField] private float maxRollSpeed = 720f;
    [SerializeField] private float rollDamping = 0.8f;
    [SerializeField] private float stopThreshold = 50f;
    
    [Header("Rolling Stamina")]
    [SerializeField] private float rollStaminaDepleteRate = 0.2f; // How fast stamina depletes while rolling
    [SerializeField] private float rollStaminaRecoverRate = 0.1f; // How fast stamina recovers when not rolling
    [SerializeField] private float rollStaminaMax = 1f; // Maximum stamina
    [SerializeField] private float rollStaminaCooldownTime = 5f; // Cooldown time when stamina is depleted

    [Header("Character Settings")]
    [SerializeField] public PowerBitCharacterRenderer powerBitCharacterRenderer;

    [Header("Combat Settings")]
    [SerializeField] private float shootingCooldown = 0.1f;
    [SerializeField] private float overheatBuildRate = 0.1f;
    [SerializeField] private float overheatDecayRate = 0.05f;
    [SerializeField] private float overheatMax = 1f;
    [SerializeField] private float overheatCooldownTime = 5f;
    [SerializeField] private ProjectileSpawner projectileSpawner;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private float overheatLevel = 0f;
    private bool isOverheated = false;
    private float overheatTimer = 0f;
    private float lastShootTime = 0f;
    private bool isShooting = false;
    
    // Rolling stamina variables
    private float rollStaminaLevel = 1f;
    private bool isRollStaminaDepleted = false;
    private float rollStaminaTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        
        if (powerBitCharacterRenderer == null)
        {
            powerBitCharacterRenderer = GetComponentInChildren<PowerBitCharacterRenderer>();
        }

        if (projectileSpawner == null)
        {
            projectileSpawner = GetComponentInChildren<ProjectileSpawner>();
        }

        rb.gravityScale = 1f;
        rb.constraints = enableRolling ? RigidbodyConstraints2D.None : RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void Start()
    {
        // Initialize rolling stamina to full
        rollStaminaLevel = rollStaminaMax;
        isRollStaminaDepleted = false;
        rollStaminaTimer = 0f;
        
        if (showDebugInfo)
        {
            Debug.Log($"Rolling stamina initialized: {rollStaminaLevel}/{rollStaminaMax}");
        }
        
        LoadLastSavedSmithBuild();
    }

    public void LoadLastSavedSmithBuild()
    {
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, "smith_build.json");
        
        if (!System.IO.File.Exists(filePath)) return;

        try
        {
            string json = System.IO.File.ReadAllText(filePath);
            SmithGridStateData gridState = JsonUtility.FromJson<SmithGridStateData>(json);
            
            if (gridState?.cells != null && powerBitCharacterRenderer != null)
            {
                LoadSmithBuild(gridState);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading Smith build: {e.Message}");
        }
    }

    private void Update()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        bool isRolling = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        
        if (showDebugInfo && isRolling)
        {
            Debug.Log($"Rolling detected - Input: {moveInput}, Stamina: {rollStaminaLevel:F2}, Depleted: {isRollStaminaDepleted}");
        }
        
        if (enableRolling && isRolling && !isRollStaminaDepleted)
        {
            HandleRollingMovement(moveInput);
        }
        else
        {
            Vector2 velocity = rb.linearVelocity;
            float speedMultiplier = GetMovementSpeedMultiplier();
            velocity.x = moveInput * moveSpeed * speedMultiplier;
            
            // Apply movement boundaries
            if (enableXBoundaries)
            {
                Vector3 nextPosition = transform.position + new Vector3(velocity.x * Time.fixedDeltaTime, 0, 0);
                if (nextPosition.x < minX || nextPosition.x > maxX)
                {
                    velocity.x = 0f; // Stop horizontal movement if it would go beyond boundaries
                    
                    // Clamp current position to boundaries if somehow went beyond
                    Vector3 clampedPosition = transform.position;
                    clampedPosition.x = Mathf.Clamp(clampedPosition.x, minX, maxX);
                    transform.position = clampedPosition;
                }
            }
            
            rb.linearVelocity = velocity;
        }

        HandleShooting();
        HandleOverheat();
        HandleRollingStamina(isRolling);
    }

    private float GetMovementSpeedMultiplier()
    {
        if (powerBitCharacterRenderer == null) return 1f;
        
        int bitCount = powerBitCharacterRenderer.GetActiveBits().Count;
        int maxBits = 4;
        
        if (bitCount == 0) return 1f;
        if (bitCount >= maxBits) return 0.6f;
        
        return Mathf.Lerp(1f, 0.6f, (float)bitCount / maxBits);
    }

    private void HandleShooting()
    {
        if (Input.GetMouseButton(0))
        {
            if (!isShooting)
            {
                isShooting = true;
            }

            if (!isOverheated)
            {
                overheatLevel += overheatBuildRate * Time.deltaTime;
                
                if (overheatLevel >= overheatMax)
                {
                    isOverheated = true;
                    overheatTimer = overheatCooldownTime;
                    isShooting = false;
                    Debug.Log("=== PLAYER OVERHEATED! 5-second cooldown started ===");
                }
                else if (Time.time - lastShootTime >= shootingCooldown)
                {
                    Shoot();
                    lastShootTime = Time.time;
                }
            }
        }
        else
        {
            isShooting = false;
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
                Debug.Log("=== OVERHEAT COOLDOWN FINISHED! Can shoot again ===");
            }
        }
        else if (overheatLevel > 0f)
        {
            overheatLevel -= overheatDecayRate * Time.deltaTime;
            overheatLevel = Mathf.Max(0f, overheatLevel);
        }
    }

    private void HandleRollingMovement(float moveInput)
    {
        float speedMultiplier = GetMovementSpeedMultiplier();
        
        if (Mathf.Abs(moveInput) > 0.1f)
        {
            float torque = -moveInput * rollTorque * speedMultiplier;
            rb.AddTorque(torque, ForceMode2D.Force);
            
            if (Mathf.Abs(rb.angularVelocity) > maxRollSpeed)
            {
                rb.angularVelocity = Mathf.Sign(rb.angularVelocity) * maxRollSpeed;
            }
        }
        else
        {
            if (Mathf.Abs(rb.angularVelocity) > stopThreshold)
            {
                float oldVelocity = rb.angularVelocity;
                rb.angularVelocity *= rollDamping;
                
                if (showDebugInfo)
                {
                    Debug.Log($"Damping applied: {oldVelocity:F1} -> {rb.angularVelocity:F1} (damping: {rollDamping})");
                }
            }
            else
            {
                if (showDebugInfo && Mathf.Abs(rb.angularVelocity) > 0.01f)
                {
                    Debug.Log($"Stopping rotation: {rb.angularVelocity:F1} (threshold: {stopThreshold})");
                }
                rb.angularVelocity = 0f;
            }
        }
        
        float forwardSpeed = -rb.angularVelocity * (boxCollider.size.x / 2f) * Mathf.Deg2Rad;
        Vector2 velocity = rb.linearVelocity;
        velocity.x = forwardSpeed * speedMultiplier;
        
        // Apply movement boundaries for rolling
        if (enableXBoundaries)
        {
            Vector3 nextPosition = transform.position + new Vector3(velocity.x * Time.fixedDeltaTime, 0, 0);
            if (nextPosition.x < minX || nextPosition.x > maxX)
            {
                velocity.x = 0f; // Stop horizontal movement if it would go beyond boundaries
                rb.angularVelocity = 0f; // Also stop rolling
                
                // Clamp current position to boundaries if somehow went beyond
                Vector3 clampedPosition = transform.position;
                clampedPosition.x = Mathf.Clamp(clampedPosition.x, minX, maxX);
                transform.position = clampedPosition;
            }
        }
        
        rb.linearVelocity = velocity;
    }

    private void Shoot()
    {
        if (powerBitCharacterRenderer == null || projectileSpawner == null) return;

        Vector2 aimDirection = projectileSpawner.GetAimingDirection();
        SmithCellData selectedBit = SelectBitForShot();
        
        if (selectedBit != null)
        {
            projectileSpawner.SpawnProjectile(selectedBit.rarity, selectedBit.damage, selectedBit.bitName);
        }
        else
        {
            projectileSpawner.SpawnProjectile(Rarity.Common, 1, "Default");
        }
    }
    
    private SmithCellData SelectBitForShot()
    {
        if (powerBitCharacterRenderer == null) return null;
        
        var activeBits = powerBitCharacterRenderer.GetActiveBits();
        if (activeBits.Count == 0) return null;
        
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
        
        for (int i = 0; i < availableBits.Count; i++)
        {
            if (Random.value < probabilities[i])
            {
                return availableBits[i];
            }
        }
        
        return null;
    }

    public void LoadSmithBuild(SmithGridStateData gridState)
    {
        if (powerBitCharacterRenderer != null)
        {
            powerBitCharacterRenderer.LoadCharacterFromSmithBuild(gridState);
            UpdateColliderSize();

            if (projectileSpawner != null)
            {
                projectileSpawner.RefreshSpawnPointPosition();
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (powerBitCharacterRenderer != null)
        {
            var outerBits = powerBitCharacterRenderer.GetOuterBits();
            if (outerBits.Count > 0)
            {
                int randomIndex = Random.Range(0, outerBits.Count);
                Vector2Int bitToRemove = outerBits[randomIndex];
                powerBitCharacterRenderer.RemoveBit(bitToRemove);
                UpdateColliderSize();
            }
        }
    }

    private void UpdateColliderSize()
    {
        if (powerBitCharacterRenderer == null || boxCollider == null) return;

        Bounds characterBounds = powerBitCharacterRenderer.GetCharacterBounds();
        Vector3 localCenter = characterBounds.center;
        Vector3 localSize = characterBounds.size;

        localSize.x = Mathf.Max(localSize.x, 0.25f);
        localSize.y = Mathf.Max(localSize.y, 0.25f);

        boxCollider.size = new Vector2(localSize.x, localSize.y);
        boxCollider.offset = new Vector2(localCenter.x, localCenter.y);
    }

    private void HandleRollingStamina(bool isRolling)
    {
        if (showDebugInfo)
        {
            Debug.Log($"Rolling Stamina - IsRolling: {isRolling}, Level: {rollStaminaLevel:F2}, Depleted: {isRollStaminaDepleted}, Timer: {rollStaminaTimer:F1}");
        }
        
        if (isRollStaminaDepleted)
        {
            rollStaminaTimer -= Time.deltaTime;
            
            if (rollStaminaTimer <= 0f)
            {
                isRollStaminaDepleted = false;
                rollStaminaLevel = rollStaminaMax;
                Debug.Log("=== ROLLING STAMINA RECOVERED! Can roll again ===");
            }
        }
        else if (isRolling)
        {
            // Deplete stamina while rolling (build rate = how fast it depletes)
            rollStaminaLevel -= rollStaminaDepleteRate * Time.deltaTime;
            
            if (showDebugInfo)
            {
                Debug.Log($"Stamina depleting: {rollStaminaLevel:F2} (build rate: {rollStaminaDepleteRate})");
            }
            
            if (rollStaminaLevel <= 0f)
            {
                isRollStaminaDepleted = true;
                rollStaminaTimer = rollStaminaCooldownTime;
                rollStaminaLevel = 0f;
                Debug.Log("=== ROLLING STAMINA DEPLETED! 5-second cooldown started ===");
            }
        }
        else if (rollStaminaLevel < rollStaminaMax)
        {
            // Recover stamina when not rolling (decay rate = how fast it recovers)
            rollStaminaLevel += rollStaminaRecoverRate * Time.deltaTime;
            rollStaminaLevel = Mathf.Min(rollStaminaMax, rollStaminaLevel);
            
            if (showDebugInfo)
            {
                Debug.Log($"Stamina recovering: {rollStaminaLevel:F2} (decay rate: {rollStaminaRecoverRate})");
            }
        }
    }

    // Method for crawling entities to steal a random bit from the player's build
    public Bit StealRandomBitFromBuild()
    {
        if (powerBitCharacterRenderer == null)
        {
            Debug.LogWarning("PowerBitCharacterRenderer is null! Cannot steal bit.");
            return null;
        }
        
        // Get all active bits from the character renderer
        var activeBits = powerBitCharacterRenderer.GetActiveBits();
        if (activeBits.Count == 0)
        {
            Debug.Log("No bits in player's build to steal!");
            return null;
        }
        
        // Select a random bit to steal
        int randomIndex = Random.Range(0, activeBits.Count);
        Vector2Int bitToSteal = activeBits[randomIndex];
        
        // Get the bit data before removing it
        SmithCellData stolenBitData = powerBitCharacterRenderer.GetBitAt(bitToSteal);
        if (stolenBitData == null)
        {
            Debug.LogWarning("Failed to get bit data for stealing!");
            return null;
        }
        
        // Remove the bit from the player's build
        powerBitCharacterRenderer.RemoveBit(bitToSteal);
        UpdateColliderSize();
        
        // Create a Bit object from the stolen data
        Bit stolenBit = ScriptableObject.CreateInstance<Bit>();
        stolenBit.bitName = stolenBitData.bitName;
        stolenBit.bitType = stolenBitData.bitType;
        stolenBit.rarity = stolenBitData.rarity;
        stolenBit.damage = stolenBitData.damage;
        stolenBit.shootingProbability = stolenBitData.shootingProbability;
        
        // Save the updated build (without the stolen bit)
        SaveUpdatedBuild();
        
        Debug.Log($"Stole {stolenBit.bitName} from player's build at position ({bitToSteal.x}, {bitToSteal.y})");
        return stolenBit;
    }
    
    // Save the updated build after stealing a bit
    public void SaveUpdatedBuild()
    {
        if (powerBitCharacterRenderer == null) return;
        
        // Create a new build state from the current character renderer
        SmithGridStateData updatedBuild = new SmithGridStateData(powerBitCharacterRenderer.GetGridSize());
        
        // Get all active bits and their data
        var activeBits = powerBitCharacterRenderer.GetActiveBits();
        foreach (var bitPos in activeBits)
        {
            SmithCellData bitData = powerBitCharacterRenderer.GetBitAt(bitPos);
            if (bitData != null)
            {
                updatedBuild.cells.Add(bitData);
            }
        }
        
        // Save to file
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, "smith_build.json");
        try
        {
            string json = JsonUtility.ToJson(updatedBuild, true);
            System.IO.File.WriteAllText(filePath, json);
            Debug.Log($"Updated build saved after stealing bit. Remaining bits: {updatedBuild.cells.Count}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving updated build: {e.Message}");
        }
    }

    // Public getters
    public float GetOverheatLevel() => overheatLevel;
    public float GetOverheatMax() => overheatMax;
    public float GetOverheatPercentage() => (overheatLevel / overheatMax) * 100f;
    public float GetOverheatBuildRate() => overheatBuildRate;
    public float GetOverheatDecayRate() => overheatDecayRate;
    public bool IsOverheated() => isOverheated;
    public float GetOverheatTimer() => overheatTimer;
    public int GetTotalDamage() => powerBitCharacterRenderer?.GetTotalDamage() ?? 0;
    public float GetShootingProbability() => powerBitCharacterRenderer?.GetAverageShootingProbability() ?? 0f;
    public int GetPowerBitCount() => powerBitCharacterRenderer?.GetActiveBits().Count ?? 0;
    public float GetCurrentMovementSpeedMultiplier() => GetMovementSpeedMultiplier();
    public bool IsShooting() => isShooting;
    public float GetAimingAngle() => projectileSpawner?.GetAimingAngle() ?? 0f;
    public Vector2 GetAimingDirection() => projectileSpawner?.GetAimingDirection() ?? Vector2.right;
    public bool IsValidAimingDirection() => projectileSpawner?.IsValidAimingDirection() ?? false;
    public int GetActiveProjectileCount() => projectileSpawner?.GetActiveProjectileCount() ?? 0;
    
    // Rolling stamina getters
    public float GetRollStaminaLevel() => rollStaminaLevel;
    public float GetRollStaminaMax() => rollStaminaMax;
    public float GetRollStaminaPercentage() => (rollStaminaLevel / rollStaminaMax) * 100f;
    public bool IsRollStaminaDepleted() => isRollStaminaDepleted;
    public float GetRollStaminaTimer() => rollStaminaTimer;
} 