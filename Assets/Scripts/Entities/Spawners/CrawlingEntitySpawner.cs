using UnityEngine;
using System.Collections;
using BitByBit.Core;
public class CrawlingEntitySpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    [SerializeField] private GameObject crawlingEntityPrefab;
    [SerializeField] private float spawnInterval = 10f; 
    [SerializeField] private bool enableSpawning = true;
    [Header("Spawn Area")]
    [SerializeField] private float spawnOffscreenDistance = 15f; 
    [SerializeField] private float groundY = 2f; 
    [SerializeField] private float maxSpawnDistanceFromPlayer = 50f; 
    [Header("World Boundaries")]
    [SerializeField] private float worldMinX = -40f; 
    [SerializeField] private float worldMaxX = 40f; 
    [SerializeField] private bool enforceWorldBoundaries = true;
    [Header("Entity Limits")]
    [SerializeField] private int maxActiveEntities = 5; 
    [SerializeField] private bool limitEntityCount = true;
    [Header("Spawn Zones")]
    [SerializeField] private bool spawnOnLeft = true;
    [SerializeField] private bool spawnOnRight = true;
    [SerializeField] private bool spawnAbove = false; 
    [SerializeField] private bool spawnBelow = false;
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showSpawnGizmos = true;
    private Camera mainCamera;
    private Transform playerTransform;
    private Coroutine spawnCoroutine;
    private int activeEntityCount = 0;
    private void Awake()
    {
        InitializeReferences();
    }
    private void InitializeReferences()
    {
        if (GameReferences.Instance != null)
        {
            if (GameReferences.Instance.MainCamera != null)
            {
                mainCamera = GameReferences.Instance.MainCamera;
            }
            if (GameReferences.Instance.Player != null)
            {
                playerTransform = GameReferences.Instance.Player;
            }
        }
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
            if (mainCamera != null)
            {
                Debug.LogWarning("CrawlingEntitySpawner: Found camera via fallback method. Please ensure GameReferences is properly configured.");
            }
        }
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                Debug.LogWarning("CrawlingEntitySpawner: Found player via fallback method. Please ensure GameReferences is properly configured.");
            }
        }
    }
    private void Start()
    {
        if (crawlingEntityPrefab == null)
        {
            Debug.LogError("CrawlingEntitySpawner: No crawling entity prefab assigned!");
            enableSpawning = false;
        }
        if (mainCamera == null)
        {
            Debug.LogError("CrawlingEntitySpawner: No camera found!");
            enableSpawning = false;
        }
        if (playerTransform == null)
        {
            Debug.LogError("CrawlingEntitySpawner: No player found!");
            enableSpawning = false;
        }
        if (enableSpawning)
        {
            StartSpawning();
        }
    }
    public void StartSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
        spawnCoroutine = StartCoroutine(SpawnRoutine());
        if (showDebugInfo)
        {
            Debug.Log("CrawlingEntitySpawner: Started spawning entities");
        }
    }
    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        if (showDebugInfo)
        {
            Debug.Log("CrawlingEntitySpawner: Stopped spawning entities");
        }
    }
    private IEnumerator SpawnRoutine()
    {
        while (enableSpawning)
        {
            yield return new WaitForSeconds(spawnInterval);
            if (ShouldSpawn())
            {
                SpawnEntity();
            }
            else if (showDebugInfo)
            {
                Debug.Log($"CrawlingEntitySpawner: Skipping spawn (Active: {activeEntityCount}/{maxActiveEntities})");
            }
        }
    }
    private bool ShouldSpawn()
    {
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            return false;
        }
        if (limitEntityCount && activeEntityCount >= maxActiveEntities)
        {
            return false;
        }
        if (playerTransform == null)
        {
            return false;
        }
        return true;
    }
    private void SpawnEntity()
    {
        Vector3 spawnPosition = GetRandomSpawnPosition();
        if (spawnPosition != Vector3.zero)
        {
            GameObject spawnedEntity = Instantiate(crawlingEntityPrefab, spawnPosition, Quaternion.identity);
            activeEntityCount++;
            CrawlingEntity entityScript = spawnedEntity.GetComponent<CrawlingEntity>();
            if (entityScript != null)
            {
                StartCoroutine(TrackEntityLifetime(spawnedEntity));
            }
            if (showDebugInfo)
            {
                Debug.Log($"CrawlingEntitySpawner: Spawned entity at {spawnPosition} (Active: {activeEntityCount})");
            }
        }
        else if (showDebugInfo)
        {
            Debug.LogWarning("CrawlingEntitySpawner: Failed to find valid spawn position");
        }
    }
    private Vector3 GetRandomSpawnPosition()
    {
        if (mainCamera == null || playerTransform == null)
        {
            return Vector3.zero;
        }
        float cameraHeight = mainCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        Vector3 cameraPos = mainCamera.transform.position;
        float leftEdge = cameraPos.x - cameraWidth / 2f;
        float rightEdge = cameraPos.x + cameraWidth / 2f;
        float topEdge = cameraPos.y + cameraHeight / 2f;
        float bottomEdge = cameraPos.y - cameraHeight / 2f;
        var availableSides = new System.Collections.Generic.List<SpawnSide>();
        if (spawnOnLeft) availableSides.Add(SpawnSide.Left);
        if (spawnOnRight) availableSides.Add(SpawnSide.Right);
        if (spawnAbove) availableSides.Add(SpawnSide.Above);
        if (spawnBelow) availableSides.Add(SpawnSide.Below);
        if (availableSides.Count == 0)
        {
            Debug.LogWarning("CrawlingEntitySpawner: No spawn sides enabled!");
            return Vector3.zero;
        }
        SpawnSide chosenSide = availableSides[Random.Range(0, availableSides.Count)];
        Vector3 spawnPos = Vector3.zero;
        switch (chosenSide)
        {
            case SpawnSide.Left:
                spawnPos = new Vector3(
                    leftEdge - spawnOffscreenDistance,
                    groundY,
                    0f
                );
                break;
            case SpawnSide.Right:
                spawnPos = new Vector3(
                    rightEdge + spawnOffscreenDistance,
                    groundY,
                    0f
                );
                break;
            case SpawnSide.Above:
                spawnPos = new Vector3(
                    Random.Range(leftEdge, rightEdge),
                    topEdge + spawnOffscreenDistance,
                    0f
                );
                break;
            case SpawnSide.Below:
                spawnPos = new Vector3(
                    Random.Range(leftEdge, rightEdge),
                    bottomEdge - spawnOffscreenDistance,
                    0f
                );
                break;
        }
        if (enforceWorldBoundaries)
        {
            spawnPos.x = Mathf.Clamp(spawnPos.x, worldMinX, worldMaxX);
            if (spawnPos.x == worldMinX || spawnPos.x == worldMaxX)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"CrawlingEntitySpawner: Spawn position clamped to world boundary at x = {spawnPos.x}");
                }
            }
        }
        spawnPos.y = groundY;
        float distanceFromPlayer = Vector3.Distance(spawnPos, playerTransform.position);
        if (distanceFromPlayer > maxSpawnDistanceFromPlayer)
        {
            Vector3 directionFromPlayer = (spawnPos - playerTransform.position).normalized;
            spawnPos = playerTransform.position + directionFromPlayer * maxSpawnDistanceFromPlayer;
            if (enforceWorldBoundaries)
            {
                spawnPos.x = Mathf.Clamp(spawnPos.x, worldMinX, worldMaxX);
            }
            spawnPos.y = groundY; 
        }
        if (enforceWorldBoundaries && (spawnPos.x < worldMinX || spawnPos.x > worldMaxX))
        {
            Debug.LogWarning($"CrawlingEntitySpawner: Spawn position {spawnPos.x} outside world boundaries [{worldMinX}, {worldMaxX}]");
            return Vector3.zero; 
        }
        return spawnPos;
    }
    private IEnumerator TrackEntityLifetime(GameObject entity)
    {
        while (entity != null)
        {
            yield return new WaitForSeconds(1f);
        }
        activeEntityCount = Mathf.Max(0, activeEntityCount - 1);
        if (showDebugInfo)
        {
            Debug.Log($"CrawlingEntitySpawner: Entity destroyed (Active: {activeEntityCount})");
        }
    }
    public void SetSpawnInterval(float interval)
    {
        spawnInterval = interval;
        if (showDebugInfo)
        {
            Debug.Log($"CrawlingEntitySpawner: Spawn interval set to {interval} seconds");
        }
    }
    public void SetMaxActiveEntities(int maxEntities)
    {
        maxActiveEntities = maxEntities;
        if (showDebugInfo)
        {
            Debug.Log($"CrawlingEntitySpawner: Max active entities set to {maxEntities}");
        }
    }
    public void EnableSpawning(bool enable)
    {
        enableSpawning = enable;
        if (enable)
        {
            StartSpawning();
        }
        else
        {
            StopSpawning();
        }
    }
    [ContextMenu("Spawn Entity Now")]
    public void SpawnEntityNow()
    {
        if (ShouldSpawn())
        {
            SpawnEntity();
        }
    }
    [ContextMenu("Clear All Entities")]
    public void ClearAllEntities()
    {
        CrawlingEntity[] allEntities = FindObjectsOfType<CrawlingEntity>();
        foreach (var entity in allEntities)
        {
            if (entity != null)
            {
                Destroy(entity.gameObject);
            }
        }
        activeEntityCount = 0;
        if (showDebugInfo)
        {
            Debug.Log("CrawlingEntitySpawner: Cleared all entities");
        }
    }
    public int GetActiveEntityCount() => activeEntityCount;
    public float GetSpawnInterval() => spawnInterval;
    public bool IsSpawningEnabled() => enableSpawning;
    private enum SpawnSide
    {
        Left,
        Right,
        Above,
        Below
    }
    private void OnDrawGizmos()
    {
        if (!showSpawnGizmos) return;
        if (enforceWorldBoundaries)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(new Vector3(worldMinX, -5f, 0f), new Vector3(worldMinX, 10f, 0f));
            Gizmos.DrawLine(new Vector3(worldMaxX, -5f, 0f), new Vector3(worldMaxX, 10f, 0f));
            Gizmos.color = Color.green;
            Gizmos.DrawLine(new Vector3(worldMinX, groundY, 0f), new Vector3(worldMaxX, groundY, 0f));
        }
        if (mainCamera == null) return;
        float cameraHeight = mainCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        Vector3 cameraPos = mainCamera.transform.position;
        float leftEdge = cameraPos.x - cameraWidth / 2f;
        float rightEdge = cameraPos.x + cameraWidth / 2f;
        float topEdge = cameraPos.y + cameraHeight / 2f;
        float bottomEdge = cameraPos.y - cameraHeight / 2f;
        Gizmos.color = Color.red;
        if (spawnOnLeft)
        {
            float leftSpawnX = leftEdge - spawnOffscreenDistance;
            if (enforceWorldBoundaries)
            {
                leftSpawnX = Mathf.Clamp(leftSpawnX, worldMinX, worldMaxX);
            }
            Vector3 leftSpawnPos = new Vector3(leftSpawnX, groundY, 0f);
            Gizmos.DrawWireSphere(leftSpawnPos, 2f);
            Gizmos.DrawLine(new Vector3(leftEdge, bottomEdge, 0f), new Vector3(leftEdge, topEdge, 0f));
        }
        if (spawnOnRight)
        {
            float rightSpawnX = rightEdge + spawnOffscreenDistance;
            if (enforceWorldBoundaries)
            {
                rightSpawnX = Mathf.Clamp(rightSpawnX, worldMinX, worldMaxX);
            }
            Vector3 rightSpawnPos = new Vector3(rightSpawnX, groundY, 0f);
            Gizmos.DrawWireSphere(rightSpawnPos, 2f);
            Gizmos.DrawLine(new Vector3(rightEdge, bottomEdge, 0f), new Vector3(rightEdge, topEdge, 0f));
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(cameraPos, new Vector3(cameraWidth, cameraHeight, 0f));
    }
    private void OnDestroy()
    {
        StopSpawning();
    }
}