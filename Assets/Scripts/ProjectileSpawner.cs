using UnityEngine;
using System.Collections.Generic;

public class ProjectileSpawner : MonoBehaviour
{
    [Header("Projectile Prefabs")]
    [SerializeField] private GameObject defaultProjectilePrefab;
    [SerializeField] private GameObject rareProjectilePrefab;
    [SerializeField] private GameObject epicProjectilePrefab;
    [SerializeField] private GameObject legendaryProjectilePrefab;
    
    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnOffset = 0.5f;
    [SerializeField] private PowerBitCharacterRenderer characterRenderer;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    private Camera mainCamera;
    private List<Projectile> activeProjectiles = new List<Projectile>();
    
    private void Awake()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("No main camera found! ProjectileSpawner needs a camera for mouse aiming.");
        }
        
        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }
        
        // Auto-find character renderer if not assigned
        if (characterRenderer == null)
        {
            characterRenderer = GetComponentInParent<PowerBitCharacterRenderer>();
        }
        
        // Automatically position spawn point based on grid size
        UpdateSpawnPointPosition();
    }
    
    private void Update()
    {
        // Clean up destroyed projectiles from the list
        activeProjectiles.RemoveAll(p => p == null);
    }
    
    public Vector2 GetAimingDirection()
    {
        if (mainCamera == null) return Vector2.right; // Default to right if no camera
        
        // Get mouse position in world space
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        
        // Calculate direction from player to mouse
        Vector2 direction = (mouseWorldPos - spawnPoint.position).normalized;
        
        // Clamp to upper arc (90° to 270°): only allow if direction.y >= 0
        if (direction.y < 0)
        {
            direction.y = 0;
            direction = direction.normalized;
        }
        
        return direction;
    }
    
    public Projectile SpawnProjectile(Rarity rarity = Rarity.Common, int damage = 1, string projectileType = "Default")
    {
        GameObject prefabToSpawn = GetProjectilePrefab(rarity);
        if (prefabToSpawn == null)
        {
            Debug.LogError($"No projectile prefab found for rarity: {rarity}");
            return null;
        }
        
        // Calculate spawn position
        Vector2 direction = GetAimingDirection();
        Vector3 spawnPosition = spawnPoint.position + (Vector3)(direction * spawnOffset);
        
        // Spawn the projectile
        GameObject projectileObj = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        
        if (projectile != null)
        {
            // Initialize the projectile
            projectile.Initialize(direction, damage, rarity, projectileType);
            projectile.SetAppearance(rarity);
            
            // Add to active projectiles list
            activeProjectiles.Add(projectile);
            
            if (showDebugInfo)
            {
                Debug.Log($"Spawned {rarity} projectile: {projectileType} (Damage: {damage}) at {spawnPosition}");
            }
        }
        else
        {
            Debug.LogError("Spawned projectile object doesn't have Projectile component!");
            Destroy(projectileObj);
        }
        
        return projectile;
    }
    
    private GameObject GetProjectilePrefab(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:
                return defaultProjectilePrefab;
            case Rarity.Rare:
                return rareProjectilePrefab ?? defaultProjectilePrefab;
            case Rarity.Epic:
                return epicProjectilePrefab ?? rareProjectilePrefab ?? defaultProjectilePrefab;
            case Rarity.Legendary:
                return legendaryProjectilePrefab ?? epicProjectilePrefab ?? rareProjectilePrefab ?? defaultProjectilePrefab;
            default:
                return defaultProjectilePrefab;
        }
    }
    
    public void ClearAllProjectiles()
    {
        foreach (var projectile in activeProjectiles)
        {
            if (projectile != null)
            {
                Destroy(projectile.gameObject);
            }
        }
        activeProjectiles.Clear();
    }
    
    public int GetActiveProjectileCount()
    {
        return activeProjectiles.Count;
    }
    
    // Public method to get aiming angle for UI or other systems
    public float GetAimingAngle()
    {
        Vector2 direction = GetAimingDirection();
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }
    
    // Public method to check if aiming direction is valid (within upper arc)
    public bool IsValidAimingDirection()
    {
        Vector2 direction = GetAimingDirection();
        // Only allow shooting if aiming in the upper arc (direction.y >= 0)
        return direction.y >= 0;
    }
    
    // Update spawn point position based on grid size
    private void UpdateSpawnPointPosition()
    {
        if (characterRenderer == null) return;
        
        int gridSize = characterRenderer.GetGridSize();
        if (gridSize <= 0) return;
        
        // Calculate center position: gridSize / 2
        float centerPos = gridSize / 2f;
        Vector3 newPosition = new Vector3(centerPos, centerPos, 0f);
        
        // Set local position relative to the character renderer
        spawnPoint.localPosition = newPosition;
        
        if (showDebugInfo)
        {
            Debug.Log($"Updated spawn point local position to ({centerPos}, {centerPos}) for grid size {gridSize}");
        }
    }
    
    // Public method to update position (call this when grid size changes)
    public void RefreshSpawnPointPosition()
    {
        UpdateSpawnPointPosition();
    }
    
    private void OnDrawGizmos()
    {
        if (spawnPoint == null) return;
        
        // Draw spawn point
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(spawnPoint.position, 0.1f);
        
        // Draw aiming direction
        if (Application.isPlaying)
        {
            Vector2 direction = GetAimingDirection();
            Gizmos.color = Color.red;
            Gizmos.DrawRay(spawnPoint.position, direction * 2f);
            
            // Draw 180° arc from 12 to 6 o'clock (upper arc)
            Gizmos.color = Color.yellow;
            for (int i = 0; i <= 18; i++) // 180 degrees in 10-degree increments
            {
                float angle = i * 10f * Mathf.Deg2Rad; // Start from 0° (12 o'clock) to 180° (6 o'clock)
                Vector2 arcPoint = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Gizmos.DrawWireSphere(spawnPoint.position + (Vector3)(arcPoint * 1.5f), 0.05f);
            }
        }
    }
} 