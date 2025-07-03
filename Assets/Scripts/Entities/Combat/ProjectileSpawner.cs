using UnityEngine;
using System.Collections.Generic;

public class ProjectileSpawner : MonoBehaviour
{
    [Header("Projectile Prefabs")]
    [SerializeField] private GameObject defaultProjectilePrefab;
    [SerializeField] private GameObject rareProjectilePrefab;
    [SerializeField] private GameObject epicProjectilePrefab;
    [SerializeField] private GameObject legendaryProjectilePrefab;
    
    [Header("Rarity System")]
    [SerializeField] private bool useRaritySystem = true;
    [SerializeField] private ProjectileRaritySystem raritySystem;
    
    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnOffset = 0.5f;
    [SerializeField] private PowerBitCharacterRenderer characterRenderer;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private Camera mainCamera;
    private List<Projectile> activeProjectiles = new List<Projectile>();

    private void Awake()
    {
        InitializeCamera();
        InitializeSpawnPoint();
        FindCharacterRenderer();
        InitializeRaritySystem();
        SetupSpawnPosition();
    }

    private void Update()
    {
        CleanupDestroyedProjectiles();
    }

    private void InitializeCamera()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("No main camera found! ProjectileSpawner needs a camera for mouse aiming.");
        }
    }

    private void InitializeSpawnPoint()
    {
        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }
    }

    private void FindCharacterRenderer()
    {
        if (characterRenderer == null)
        {
            characterRenderer = GetComponentInParent<PowerBitCharacterRenderer>();
        }
    }

    private void InitializeRaritySystem()
    {
        if (raritySystem == null)
        {
            raritySystem = GetComponent<ProjectileRaritySystem>();
            if (raritySystem == null && useRaritySystem)
            {
                // Create rarity system component if it doesn't exist
                raritySystem = gameObject.AddComponent<ProjectileRaritySystem>();
                LogDebugInfo("Created ProjectileRaritySystem component automatically");
            }
        }
    }

    private void SetupSpawnPosition()
    {
        UpdateSpawnPointPosition();
    }

    public Vector2 GetAimingDirection()
    {
        if (mainCamera == null) return Vector2.right;
        
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector2 direction = CalculateAimingDirection(mouseWorldPos);
        
        return ClampToUpperArc(direction);
    }

    public float GetAimingAngle()
    {
        Vector2 direction = GetAimingDirection();
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    public bool IsValidAimingDirection()
    {
        Vector2 direction = GetAimingDirection();
        return direction.y >= 0; // Only allow shooting in upper arc
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        return mouseWorldPos;
    }

    private Vector2 CalculateAimingDirection(Vector3 mouseWorldPos)
    {
        return (mouseWorldPos - spawnPoint.position).normalized;
    }

    private Vector2 ClampToUpperArc(Vector2 direction)
    {
        // Clamp to upper arc (90° to 270°): only allow if direction.y >= 0
        if (direction.y < 0)
        {
            direction.y = 0;
            direction = direction.normalized;
        }
        return direction;
    }

    public Projectile SpawnProjectile()
    {
        Rarity projectileRarity = DetermineProjectileRarity();
        GameObject prefabToUse = GetProjectilePrefabForRarity(projectileRarity);
        int damage = GetDamageForRarity(projectileRarity);
        
        LogDebugInfo($"--- PROJECTILE: Spawning {projectileRarity} projectile (Damage: {damage})");
        
        if (prefabToUse == null)
        {
            Debug.LogError($"No projectile prefab assigned for rarity: {projectileRarity}!");
            return null;
        }
        
        Vector3 spawnPosition = CalculateSpawnPosition();
        GameObject projectileObj = InstantiateProjectile(prefabToUse, spawnPosition);
        
        return SetupProjectile(projectileObj, damage, projectileRarity);
    }

    private Vector3 CalculateSpawnPosition()
    {
        Vector2 direction = GetAimingDirection();
        return spawnPoint.position + (Vector3)(direction * spawnOffset);
    }

    private GameObject InstantiateProjectile(GameObject prefab, Vector3 position)
    {
        return Instantiate(prefab, position, Quaternion.identity);
    }

    private Projectile SetupProjectile(GameObject projectileObj, int damage, Rarity rarity)
    {
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        
        if (projectile != null)
        {
            LogDebugInfo($"--- PROJECTILE: Before Init Damage={projectile.Damage}");
            
            InitializeProjectile(projectile, damage, rarity);
            
            LogDebugInfo($"--- PROJECTILE: Final Damage={projectile.Damage}, Rarity={projectile.Rarity}");
            
            RegisterProjectile(projectile);
        }
        else
        {
            HandleInvalidProjectile(projectileObj);
        }
        
        return projectile;
    }

    private void InitializeProjectile(Projectile projectile, int damage, Rarity rarity)
    {
        Vector2 direction = GetAimingDirection();
        projectile.Initialize(direction, damage, rarity);
    }

    private void RegisterProjectile(Projectile projectile)
    {
        activeProjectiles.Add(projectile);
    }

    private void HandleInvalidProjectile(GameObject projectileObj)
    {
        Debug.LogError("Spawned projectile object doesn't have Projectile component!");
        Destroy(projectileObj);
    }

    private Rarity DetermineProjectileRarity()
    {
        if (!useRaritySystem || raritySystem == null)
        {
            return Rarity.Common; // Default to Common if rarity system is disabled
        }
        
        return raritySystem.DetermineProjectileRarity();
    }

    private GameObject GetProjectilePrefabForRarity(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Legendary => legendaryProjectilePrefab ?? defaultProjectilePrefab,
            Rarity.Epic => epicProjectilePrefab ?? defaultProjectilePrefab,
            Rarity.Rare => rareProjectilePrefab ?? defaultProjectilePrefab,
            Rarity.Common => defaultProjectilePrefab,
            _ => defaultProjectilePrefab
        };
    }

    private int GetDamageForRarity(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Legendary => 8,
            Rarity.Epic => 4,
            Rarity.Rare => 2,
            Rarity.Common => 1,
            _ => 1
        };
    }

    public void ClearAllProjectiles()
    {
        DestroyAllActiveProjectiles();
        ClearProjectileList();
    }

    public int GetActiveProjectileCount()
    {
        return activeProjectiles.Count;
    }

    private void CleanupDestroyedProjectiles()
    {
        activeProjectiles.RemoveAll(p => p == null);
    }

    private void DestroyAllActiveProjectiles()
    {
        foreach (var projectile in activeProjectiles)
        {
            if (projectile != null)
            {
                Destroy(projectile.gameObject);
            }
        }
    }

    private void ClearProjectileList()
    {
        activeProjectiles.Clear();
    }

    public void RefreshSpawnPointPosition()
    {
        UpdateSpawnPointPosition();
    }

    private void UpdateSpawnPointPosition()
    {
        if (characterRenderer == null) return;
        
        int gridSize = characterRenderer.GetGridSize();
        if (gridSize <= 0) return;
        
        Vector3 centerPosition = CalculateCenterPosition(gridSize);
        SetSpawnPointPosition(centerPosition);
        
        LogSpawnPointUpdate(centerPosition, gridSize);
    }

    private Vector3 CalculateCenterPosition(int gridSize)
    {
        float centerPos = gridSize / 2f;
        return new Vector3(centerPos, centerPos, 0f);
    }

    private void SetSpawnPointPosition(Vector3 position)
    {
        spawnPoint.localPosition = position;
    }

    private void LogSpawnPointUpdate(Vector3 position, int gridSize)
    {
        if (showDebugInfo)
        {
            Debug.Log($"Updated spawn point local position to ({position.x}, {position.y}) for grid size {gridSize}");
        }
    }

    private void LogDebugInfo(string message)
    {
        if (showDebugInfo)
        {
            Debug.Log($"ProjectileSpawner: {message}");
        }
    }

    private void OnDrawGizmos()
    {
        if (spawnPoint == null) return;
        
        DrawSpawnPoint();
        
        if (Application.isPlaying)
        {
            DrawAimingDirection();
            DrawShootingArc();
        }
    }

    private void DrawSpawnPoint()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(spawnPoint.position, 0.1f);
    }

    private void DrawAimingDirection()
    {
        Vector2 direction = GetAimingDirection();
        Gizmos.color = Color.red;
        Gizmos.DrawRay(spawnPoint.position, direction * 2f);
    }

    private void DrawShootingArc()
    {
        Gizmos.color = Color.yellow;
        
        // Draw 180° arc from 12 to 6 o'clock (upper arc)
        for (int i = 0; i <= 18; i++) // 180 degrees in 10-degree increments
        {
            float angle = i * 10f * Mathf.Deg2Rad; // Start from 0° (12 o'clock) to 180° (6 o'clock)
            Vector2 arcPoint = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Gizmos.DrawWireSphere(spawnPoint.position + (Vector3)(arcPoint * 1.5f), 0.05f);
        }
    }
} 