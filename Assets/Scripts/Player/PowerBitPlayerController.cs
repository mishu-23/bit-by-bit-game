using System.Collections.Generic;
using System.Linq;
using BitByBit.Items;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PowerBitPlayerController : MonoBehaviour
{
    #region Serialized Fields
    
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
    [SerializeField] private float rollStaminaDepleteRate = 0.2f;
    [SerializeField] private float rollStaminaRecoverRate = 0.1f;
    [SerializeField] private float rollStaminaMax = 1f;
    [SerializeField] private float rollStaminaCooldownTime = 5f;

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
    
    #endregion

    #region Private Fields
    
    private BoxCollider2D boxCollider;
    private bool isOverheated = false;
    private bool isRollStaminaDepleted = false;
    private bool isShooting = false;
    private float lastShootTime = 0f;
    private float overheatLevel = 0f;
    private float overheatTimer = 0f;
    private Rigidbody2D rb;
    private float rollStaminaLevel = 1f;
    private float rollStaminaTimer = 0f;
    
    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeComponents();
        ConfigureRigidbody();
    }

    private void Start()
    {
        InitializeStamina();
        LoadLastSavedSmithBuild();
    }

    private void Update()
    {
        if (IsPaused()) return;

        ProcessInput();
        HandleOverheat();
    }

    #endregion

    #region Public Properties

    public float GetAimingAngle() => projectileSpawner?.GetAimingAngle() ?? 0f;
    
    public Vector2 GetAimingDirection() => projectileSpawner?.GetAimingDirection() ?? Vector2.right;
    
    public int GetActiveProjectileCount() => projectileSpawner?.GetActiveProjectileCount() ?? 0;
    
    public float GetCurrentMovementSpeedMultiplier() => GetMovementSpeedMultiplier();
    
    public float GetOverheatBuildRate() => overheatBuildRate;
    
    public float GetOverheatDecayRate() => overheatDecayRate;
    
    public float GetOverheatLevel() => overheatLevel;
    
    public float GetOverheatMax() => overheatMax;
    
    public float GetOverheatPercentage() => (overheatLevel / overheatMax) * 100f;
    
    public float GetOverheatTimer() => overheatTimer;
    
    public int GetPowerBitCount() => powerBitCharacterRenderer?.GetActiveBits().Count ?? 0;
    
    public float GetRollStaminaLevel() => rollStaminaLevel;
    
    public float GetRollStaminaMax() => rollStaminaMax;
    
    public float GetRollStaminaPercentage() => (rollStaminaLevel / rollStaminaMax) * 100f;
    
    public float GetRollStaminaTimer() => rollStaminaTimer;
    
    public float GetShootingProbability() => powerBitCharacterRenderer?.GetAverageShootingProbability() ?? 0f;
    
    public int GetTotalDamage() => powerBitCharacterRenderer?.GetTotalDamage() ?? 0;
    
    public bool IsOverheated() => isOverheated;
    
    public bool IsRollStaminaDepleted() => isRollStaminaDepleted;
    
    public bool IsShooting() => isShooting;
    
    public bool IsValidAimingDirection() => projectileSpawner?.IsValidAimingDirection() ?? false;

    #endregion

    #region Public Methods

    public void AddBitToBuild(Vector2Int position, SmithCellData cellData)
    {
        if (powerBitCharacterRenderer == null) return;

        powerBitCharacterRenderer.AddBit(position, cellData);
        UpdateColliderSize();
        RefreshProjectileSpawner();

        LogDebugInfo($"Added bit {cellData.bitName} at Unity coordinates({position.x},{position.y})");
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

    public void LoadSmithBuild(SmithGridStateData gridState)
    {
        if (powerBitCharacterRenderer == null) return;

        powerBitCharacterRenderer.LoadCharacterFromSmithBuild(gridState);
        UpdateColliderSize();
        RefreshProjectileSpawner();
    }

    public void SaveUpdatedBuild()
    {
        if (powerBitCharacterRenderer == null) return;
        
        SmithGridStateData updatedBuild = CreateBuildStateFromRenderer();
        
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, "smith_build.json");
        try
        {
            string json = JsonUtility.ToJson(updatedBuild, true);
            System.IO.File.WriteAllText(filePath, json);
            Debug.Log($"Updated build saved after stealing bit. Remaining bits: {updatedBuild.cells.Count}");
            
            InvalidateBitCollectionCache();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving updated build: {e.Message}");
        }
    }

    public Bit StealRandomBitFromBuild()
    {
        if (powerBitCharacterRenderer == null)
        {
            Debug.LogWarning("PowerBitCharacterRenderer is null! Cannot steal bit.");
            return null;
        }
        
        var activeBits = powerBitCharacterRenderer.GetActiveBits();
        if (activeBits.Count == 0)
        {
            Debug.Log("No bits in player's build to steal!");
            return null;
        }
        
        Vector2Int bitToSteal = SelectRandomBit(activeBits);
        SmithCellData stolenBitData = powerBitCharacterRenderer.GetBitAt(bitToSteal);
        
        if (stolenBitData == null)
        {
            Debug.LogWarning("Failed to get bit data for stealing!");
            return null;
        }
        
        RemoveBitFromBuild(bitToSteal);
        Bit stolenBit = CreateBitFromData(stolenBitData);
        SaveUpdatedBuild();
        
        Debug.Log($"Stole {stolenBit.BitName} from player's build at position ({bitToSteal.x}, {bitToSteal.y})");
        return stolenBit;
    }

    public void TakeDamage(int damage)
    {
        if (powerBitCharacterRenderer == null) return;

        var outerBits = powerBitCharacterRenderer.GetOuterBits();
        if (outerBits.Count > 0)
        {
            int randomIndex = Random.Range(0, outerBits.Count);
            Vector2Int bitToRemove = outerBits[randomIndex];
            RemoveBitFromBuild(bitToRemove);
        }
    }

    #endregion

    #region Private Methods - Combat

    private void HandleOverheat()
    {
        if (isOverheated)
        {
            ProcessOverheatCooldown();
        }
        else if (overheatLevel > 0f)
        {
            DecayOverheat();
        }
    }

    private void HandleShooting()
    {
        if (Input.GetMouseButton(0))
        {
            ProcessShooting();
        }
        else
        {
            isShooting = false;
        }
    }

    private void ProcessOverheatCooldown()
    {
        overheatTimer -= Time.deltaTime;
        
        if (overheatTimer <= 0f)
        {
            isOverheated = false;
            overheatLevel = 0f;
            Debug.Log("=== OVERHEAT COOLDOWN FINISHED! Can shoot again ===");
        }
    }

    private void ProcessShooting()
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
                TriggerOverheat();
            }
            else if (CanShoot())
            {
                Shoot();
                lastShootTime = Time.time;
            }
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

    private void TriggerOverheat()
    {
        isOverheated = true;
        overheatTimer = overheatCooldownTime;
        isShooting = false;
        Debug.Log("=== PLAYER OVERHEATED! 5-second cooldown started ===");
    }

    #endregion

    #region Private Methods - Data Management

    private SmithGridStateData CreateBuildStateFromRenderer()
    {
        SmithGridStateData updatedBuild = new SmithGridStateData(powerBitCharacterRenderer.GetGridSize());
        
        var activeBits = powerBitCharacterRenderer.GetActiveBits();
        foreach (var bitPos in activeBits)
        {
            SmithCellData bitData = powerBitCharacterRenderer.GetBitAt(bitPos);
            if (bitData != null)
            {
                updatedBuild.cells.Add(bitData);
            }
        }
        
        return updatedBuild;
    }

    private Bit CreateBitFromData(SmithCellData bitData)
    {
        return Bit.CreateBit(
            bitData.bitName,
            bitData.bitType,
            bitData.rarity,
            bitData.damage,
            bitData.shootingProbability
        );
    }

    private void InvalidateBitCollectionCache()
    {
        if (BitCollectionManager.Instance != null)
        {
            BitCollectionManager.Instance.InvalidateCache();
            Debug.Log("BitCollectionManager cache invalidated after build update");
        }
    }

    private void RemoveBitFromBuild(Vector2Int position)
    {
        powerBitCharacterRenderer.RemoveBit(position);
        UpdateColliderSize();
    }

    private Vector2Int SelectRandomBit(List<Vector2Int> activeBits)
    {
        int randomIndex = Random.Range(0, activeBits.Count);
        return activeBits[randomIndex];
    }

    #endregion

    #region Private Methods - Initialization

    private void ConfigureRigidbody()
    {
        rb.gravityScale = 1f;
        rb.constraints = enableRolling ? RigidbodyConstraints2D.None : RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void InitializeComponents()
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
    }

    private void InitializeStamina()
    {
        rollStaminaLevel = rollStaminaMax;
        isRollStaminaDepleted = false;
        rollStaminaTimer = 0f;
        
        LogDebugInfo($"Rolling stamina initialized: {rollStaminaLevel}/{rollStaminaMax}");
    }

    #endregion

    #region Private Methods - Movement

    private void ApplyMovementBoundaries(ref Vector2 velocity)
    {
        if (!enableXBoundaries) return;

        Vector3 nextPosition = transform.position + new Vector3(velocity.x * Time.fixedDeltaTime, 0, 0);
        if (nextPosition.x < minX || nextPosition.x > maxX)
        {
            velocity.x = 0f;
            ClampPositionToBoundaries();
        }
    }

    private void ApplyRollingBoundaries(ref Vector2 velocity)
    {
        if (!enableXBoundaries) return;

        Vector3 nextPosition = transform.position + new Vector3(velocity.x * Time.fixedDeltaTime, 0, 0);
        if (nextPosition.x < minX || nextPosition.x > maxX)
        {
            velocity.x = 0f;
            rb.angularVelocity = 0f;
            ClampPositionToBoundaries();
        }
    }

    private void ClampPositionToBoundaries()
    {
        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, minX, maxX);
        transform.position = clampedPosition;
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

    private void HandleRollingMovement(float moveInput)
    {
        float speedMultiplier = GetMovementSpeedMultiplier();
        
        ProcessRollingInput(moveInput, speedMultiplier);
        
        Vector2 velocity = CalculateRollingVelocity(speedMultiplier);
        ApplyRollingBoundaries(ref velocity);
        
        rb.linearVelocity = velocity;
    }

    private void HandleRollingStamina(bool isRolling)
    {
        LogDebugInfo($"Rolling Stamina - IsRolling: {isRolling}, Level: {rollStaminaLevel:F2}, Depleted: {isRollStaminaDepleted}, Timer: {rollStaminaTimer:F1}");
        
        if (isRollStaminaDepleted)
        {
            ProcessStaminaCooldown();
        }
        else if (isRolling)
        {
            ProcessStaminaDepletion();
        }
        else if (rollStaminaLevel < rollStaminaMax)
        {
            ProcessStaminaRecovery();
        }
    }

    private void HandleStandardMovement(float moveInput)
    {
        Vector2 velocity = rb.linearVelocity;
        float speedMultiplier = GetMovementSpeedMultiplier();
        velocity.x = moveInput * moveSpeed * speedMultiplier;
        
        ApplyMovementBoundaries(ref velocity);
        rb.linearVelocity = velocity;
    }

    private void ProcessRollingInput(float moveInput, float speedMultiplier)
    {
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
            ProcessRollingDamping();
        }
    }

    private void ProcessRollingDamping()
    {
        if (Mathf.Abs(rb.angularVelocity) > stopThreshold)
        {
            float oldVelocity = rb.angularVelocity;
            rb.angularVelocity *= rollDamping;
            
            LogDebugInfo($"Damping applied: {oldVelocity:F1} -> {rb.angularVelocity:F1} (damping: {rollDamping})");
        }
        else
        {
            LogDebugInfo($"Stopping rotation: {rb.angularVelocity:F1} (threshold: {stopThreshold})");
            rb.angularVelocity = 0f;
        }
    }

    private void ProcessStaminaCooldown()
    {
        rollStaminaTimer -= Time.deltaTime;
        
        if (rollStaminaTimer <= 0f)
        {
            isRollStaminaDepleted = false;
            rollStaminaLevel = rollStaminaMax;
            Debug.Log("=== ROLLING STAMINA RECOVERED! Can roll again ===");
        }
    }

    private void ProcessStaminaDepletion()
    {
        rollStaminaLevel -= rollStaminaDepleteRate * Time.deltaTime;
        
        LogDebugInfo($"Stamina depleting: {rollStaminaLevel:F2} (build rate: {rollStaminaDepleteRate})");
        
        if (rollStaminaLevel <= 0f)
        {
            isRollStaminaDepleted = true;
            rollStaminaTimer = rollStaminaCooldownTime;
            rollStaminaLevel = 0f;
            Debug.Log("=== ROLLING STAMINA DEPLETED! 5-second cooldown started ===");
        }
    }

    private void ProcessStaminaRecovery()
    {
        rollStaminaLevel += rollStaminaRecoverRate * Time.deltaTime;
        rollStaminaLevel = Mathf.Min(rollStaminaMax, rollStaminaLevel);
        
        LogDebugInfo($"Stamina recovering: {rollStaminaLevel:F2} (decay rate: {rollStaminaRecoverRate})");
    }

    #endregion

    #region Private Methods - Utilities

    private Vector2 CalculateRollingVelocity(float speedMultiplier)
    {
        float forwardSpeed = -rb.angularVelocity * (boxCollider.size.x / 2f) * Mathf.Deg2Rad;
        Vector2 velocity = rb.linearVelocity;
        velocity.x = forwardSpeed * speedMultiplier;
        return velocity;
    }

    private bool CanShoot() => Time.time - lastShootTime >= shootingCooldown;

    private void DecayOverheat()
    {
        overheatLevel -= overheatDecayRate * Time.deltaTime;
        overheatLevel = Mathf.Max(0f, overheatLevel);
    }

    private bool IsPaused() => PauseManager.Instance != null && PauseManager.Instance.IsPaused;

    private void LogDebugInfo(string message)
    {
        if (showDebugInfo)
        {
            Debug.Log(message);
        }
    }

    private void ProcessInput()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        bool isRolling = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        
        LogDebugInfo($"Rolling detected - Input: {moveInput}, Stamina: {rollStaminaLevel:F2}, Depleted: {isRollStaminaDepleted}");
        
        if (enableRolling && isRolling && !isRollStaminaDepleted)
        {
            HandleRollingMovement(moveInput);
        }
        else
        {
            HandleStandardMovement(moveInput);
        }

        HandleShooting();
        HandleRollingStamina(isRolling);
    }

    private void RefreshProjectileSpawner()
    {
        if (projectileSpawner != null)
        {
            projectileSpawner.RefreshSpawnPointPosition();
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

    #endregion
} 