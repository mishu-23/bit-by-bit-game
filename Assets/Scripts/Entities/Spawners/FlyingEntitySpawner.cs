using UnityEngine;
using System.Collections;
using BitByBit.Core;
public class FlyingEntitySpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    [SerializeField] private GameObject flyingEntityPrefab;
    [SerializeField] private float spawnInterval = 15f; 
    [SerializeField] private bool enableSpawning = true;
    [Header("Spawn Area")]
    [SerializeField] private float spawnHeight = 15f; 
    [SerializeField] private float spawnXMin = -20f; 
    [SerializeField] private float spawnXMax = 20f; 
    [Header("Entity Limits")]
    [SerializeField] private int maxActiveEntities = 3; 
    [SerializeField] private bool limitEntityCount = true;
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showSpawnGizmos = true;
    private Transform playerTransform;
    private Coroutine spawnCoroutine;
    private int activeEntityCount = 0;
    private void Awake()
    {
        InitializeReferences();
    }
    private void InitializeReferences()
    {
        if (GameReferences.Instance != null && GameReferences.Instance.Player != null)
        {
            playerTransform = GameReferences.Instance.Player;
        }
        else
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                Debug.LogWarning("FlyingEntitySpawner: Found player via fallback method. Please ensure GameReferences is properly configured.");
            }
        }
    }
    private void Start()
    {
        if (flyingEntityPrefab == null)
        {
            Debug.LogError("FlyingEntitySpawner: No flying entity prefab assigned!");
            enableSpawning = false;
        }
        if (playerTransform == null)
        {
            Debug.LogError("FlyingEntitySpawner: No player found!");
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
            Debug.Log("FlyingEntitySpawner: Started spawning flying entities");
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
            Debug.Log("FlyingEntitySpawner: Stopped spawning flying entities");
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
                Debug.Log($"FlyingEntitySpawner: Skipping spawn (Active: {activeEntityCount}/{maxActiveEntities})");
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
        GameObject spawnedEntity = Instantiate(flyingEntityPrefab, spawnPosition, Quaternion.identity);
        activeEntityCount++;
        StartCoroutine(TrackEntityLifetime(spawnedEntity));
        if (showDebugInfo)
        {
            Debug.Log($"FlyingEntitySpawner: Spawned flying entity at {spawnPosition} (Active: {activeEntityCount}/{maxActiveEntities})");
        }
    }
    private Vector3 GetRandomSpawnPosition()
    {
        float spawnX = Random.Range(spawnXMin, spawnXMax);
        Vector3 spawnPosition = new Vector3(spawnX, spawnHeight, 0f);
        return spawnPosition;
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
            Debug.Log($"FlyingEntitySpawner: Flying entity destroyed (Active: {activeEntityCount}/{maxActiveEntities})");
        }
    }
    public void SetSpawnInterval(float interval)
    {
        spawnInterval = Mathf.Max(1f, interval);
        if (showDebugInfo)
        {
            Debug.Log($"FlyingEntitySpawner: Spawn interval set to {spawnInterval}s");
        }
    }
    public void SetMaxActiveEntities(int maxEntities)
    {
        maxActiveEntities = Mathf.Max(1, maxEntities);
        if (showDebugInfo)
        {
            Debug.Log($"FlyingEntitySpawner: Max active entities set to {maxActiveEntities}");
        }
    }
    public void EnableSpawning(bool enable)
    {
        enableSpawning = enable;
        if (enable && spawnCoroutine == null)
        {
            StartSpawning();
        }
        else if (!enable && spawnCoroutine != null)
        {
            StopSpawning();
        }
        if (showDebugInfo)
        {
            Debug.Log($"FlyingEntitySpawner: Spawning {(enable ? "enabled" : "disabled")}");
        }
    }
    [ContextMenu("Spawn Flying Entity Now")]
    public void SpawnEntityNow()
    {
        if (flyingEntityPrefab != null)
        {
            SpawnEntity();
        }
    }
    [ContextMenu("Clear All Flying Entities")]
    public void ClearAllEntities()
    {
        FlyingEntity[] entities = FindObjectsOfType<FlyingEntity>();
        foreach (FlyingEntity entity in entities)
        {
            if (entity != null)
            {
                Destroy(entity.gameObject);
            }
        }
        activeEntityCount = 0;
        if (showDebugInfo)
        {
            Debug.Log($"FlyingEntitySpawner: Cleared all flying entities");
        }
    }
    public int GetActiveEntityCount() => activeEntityCount;
    public float GetSpawnInterval() => spawnInterval;
    public bool IsSpawningEnabled() => enableSpawning;
    private void OnDrawGizmos()
    {
        if (!showSpawnGizmos) return;
        Gizmos.color = Color.cyan;
        Vector3 spawnAreaCenter = new Vector3((spawnXMin + spawnXMax) / 2f, spawnHeight, 0f);
        Vector3 spawnAreaSize = new Vector3(spawnXMax - spawnXMin, 1f, 0f);
        Gizmos.DrawWireCube(spawnAreaCenter, spawnAreaSize);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(spawnXMin, spawnHeight, 0f), new Vector3(spawnXMax, spawnHeight, 0f));
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(new Vector3(spawnXMin, spawnHeight, 0f), 0.5f);
        Gizmos.DrawWireSphere(new Vector3(spawnXMax, spawnHeight, 0f), 0.5f);
        if (Application.isPlaying)
        {
            UnityEditor.Handles.Label(spawnAreaCenter + Vector3.up, $"Flying Entities: {activeEntityCount}/{maxActiveEntities}");
        }
    }
    private void OnDestroy()
    {
        StopSpawning();
    }
}