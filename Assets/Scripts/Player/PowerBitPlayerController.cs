using System.Collections.Generic;
using System.Linq;
using BitByBit.Items;
using UnityEngine;
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
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private bool isShooting = false;
    private float lastShootTime = 0f;
    private bool isOverheated = false;
    private float overheatLevel = 0f;
    private float overheatTimer = 0f;
    private bool isRollStaminaDepleted = false;
    private float rollStaminaLevel = 1f;
    private float rollStaminaTimer = 0f;
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
    private void ConfigureRigidbody()
    {
        rb.gravityScale = 1f;
        rb.constraints = enableRolling ? RigidbodyConstraints2D.None : RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }
    private void InitializeStamina()
    {
        rollStaminaLevel = rollStaminaMax;
        isRollStaminaDepleted = false;
        rollStaminaTimer = 0f;
        LogDebugInfo($"Rolling stamina initialized: {rollStaminaLevel}/{rollStaminaMax}");
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
    private bool IsPaused() => PauseManager.Instance != null && PauseManager.Instance.IsPaused;
    private void HandleStandardMovement(float moveInput)
        {
            Vector2 velocity = rb.linearVelocity;
            float speedMultiplier = GetMovementSpeedMultiplier();
            velocity.x = moveInput * moveSpeed * speedMultiplier;
        ApplyMovementBoundaries(ref velocity);
        rb.linearVelocity = velocity;
    }
    private void HandleRollingMovement(float moveInput)
    {
        float speedMultiplier = GetMovementSpeedMultiplier();
        ProcessRollingInput(moveInput, speedMultiplier);
        Vector2 velocity = CalculateRollingVelocity(speedMultiplier);
        ApplyRollingBoundaries(ref velocity);
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
    }
    private Vector2 CalculateRollingVelocity(float speedMultiplier)
    {
        float forwardSpeed = -rb.angularVelocity * (boxCollider.size.x / 2f) * Mathf.Deg2Rad;
        Vector2 velocity = rb.linearVelocity;
        velocity.x = forwardSpeed * speedMultiplier;
        return velocity;
    }
    private float GetMovementSpeedMultiplier()
    {
        if (powerBitCharacterRenderer == null) return 1f;
        int bitCount = powerBitCharacterRenderer.GetActiveBits().Count;
        int gridSize = powerBitCharacterRenderer.GetGridSize();
        int maxBits = gridSize * gridSize;
        if (bitCount == 0) return 1f;
        if (bitCount >= maxBits) return 0.6f;
        return Mathf.Lerp(1f, 0.6f, (float)bitCount / maxBits);
    }
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
    private void Shoot()
    {
        if (projectileSpawner == null) return;
        projectileSpawner.SpawnProjectile();
        LogDebugInfo("Shot fired - rarity determined by ProjectileSpawner");
    }
    private bool CanShoot() => Time.time - lastShootTime >= shootingCooldown;
    private void TriggerOverheat()
    {
        isOverheated = true;
        overheatTimer = overheatCooldownTime;
            isShooting = false;
        Debug.Log("=== PLAYER OVERHEATED! 5-second cooldown started ===");
    }
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
    private void DecayOverheat()
        {
            overheatLevel -= overheatDecayRate * Time.deltaTime;
            overheatLevel = Mathf.Max(0f, overheatLevel);
        }
    public void LoadLastSavedSmithBuild()
    {
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, "smith_build.json");
        if (!System.IO.File.Exists(filePath))
        {
            Debug.Log("No saved build found. Creating default 2x2 build for new game.");
            CreateAndLoadDefaultBuild();
            return;
        }
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
            Debug.Log("Creating default 2x2 build as fallback.");
            CreateAndLoadDefaultBuild();
        }
    }
    private void CreateAndLoadDefaultBuild()
    {
        SmithGridStateData defaultBuild = new SmithGridStateData(2);
        if (powerBitCharacterRenderer != null)
        {
            LoadSmithBuild(defaultBuild);
            Debug.Log("Default empty 2x2 build loaded successfully.");
        }
        SaveDefaultBuild(defaultBuild);
    }
    private void SaveDefaultBuild(SmithGridStateData defaultBuild)
    {
        try
        {
            string json = JsonUtility.ToJson(defaultBuild, true);
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, "smith_build.json");
            System.IO.File.WriteAllText(filePath, json);
            Debug.Log("Default build saved to file.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving default build: {e.Message}");
        }
    }
    public void LoadSmithBuild(SmithGridStateData gridState)
    {
        if (powerBitCharacterRenderer == null) return;
        powerBitCharacterRenderer.LoadCharacterFromSmithBuild(gridState);
        UpdateColliderSize();
        RefreshProjectileSpawner();
    }
    public void AddBitToBuild(Vector2Int position, SmithCellData cellData)
    {
        if (powerBitCharacterRenderer == null) return;
        powerBitCharacterRenderer.AddBit(position, cellData);
        UpdateColliderSize();
        RefreshProjectileSpawner();
        LogDebugInfo($"Added bit {cellData.bitName} at Unity coordinates({position.x},{position.y})");
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
    private void InvalidateBitCollectionCache()
            {
        if (BitCollectionManager.Instance != null)
        {
            BitCollectionManager.Instance.InvalidateCache();
            Debug.Log("BitCollectionManager cache invalidated after build update");
                    }
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
    private void RemoveBitFromBuild(Vector2Int position)
    {
        powerBitCharacterRenderer.RemoveBit(position);
        UpdateColliderSize();
        SaveUpdatedBuild();
        InvalidateBitCollectionCache();
        Debug.Log($"Bit removed from build at position {position} - build saved and cache invalidated");
    }
    private Vector2Int SelectRandomBit(List<Vector2Int> activeBits)
    {
        int randomIndex = Random.Range(0, activeBits.Count);
        return activeBits[randomIndex];
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
    private void RefreshProjectileSpawner()
    {
        if (projectileSpawner != null)
        {
            projectileSpawner.RefreshSpawnPointPosition();
        }
    }
    public float GetCurrentMovementSpeedMultiplier() => GetMovementSpeedMultiplier();
    public int GetTotalDamage() => powerBitCharacterRenderer?.GetTotalDamage() ?? 0;
    public bool IsShooting() => isShooting;
    public float GetAimingAngle() => projectileSpawner?.GetAimingAngle() ?? 0f;
    public Vector2 GetAimingDirection() => projectileSpawner?.GetAimingDirection() ?? Vector2.right;
    public bool IsValidAimingDirection() => projectileSpawner?.IsValidAimingDirection() ?? false;
    public int GetActiveProjectileCount() => projectileSpawner?.GetActiveProjectileCount() ?? 0;
    public ProjectileSpawner GetProjectileSpawner() => projectileSpawner;
    public float GetOverheatLevel() => overheatLevel;
    public float GetOverheatMax() => overheatMax;
    public float GetOverheatPercentage() => (overheatLevel / overheatMax) * 100f;
    public float GetOverheatBuildRate() => overheatBuildRate;
    public float GetOverheatDecayRate() => overheatDecayRate;
    public bool IsOverheated() => isOverheated;
    public float GetOverheatTimer() => overheatTimer;
    public float GetRollStaminaLevel() => rollStaminaLevel;
    public float GetRollStaminaMax() => rollStaminaMax;
    public float GetRollStaminaPercentage() => (rollStaminaLevel / rollStaminaMax) * 100f;
    public bool IsRollStaminaDepleted() => isRollStaminaDepleted;
    public float GetRollStaminaTimer() => rollStaminaTimer;
    public int GetPowerBitCount() => powerBitCharacterRenderer?.GetActiveBits().Count ?? 0;
    private void LogDebugInfo(string message)
    {
        if (showDebugInfo)
        {
            Debug.Log(message);
        }
    }
}