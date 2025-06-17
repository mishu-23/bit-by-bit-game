using UnityEngine;

public class HouseInteraction : MonoBehaviour
{
    [Header("Assign the E_Icon child here")]
    public GameObject eIcon;
    
    [Header("Entity Spawning")]
    [SerializeField] private GameObject gatheringEntityPrefab;
    [SerializeField] private float spawnDistance = 2f; // Distance from house to spawn entity
    [SerializeField] private Transform spawnPoint; // Optional specific spawn point
    
    [Header("Spawn Settings")]
    [SerializeField] private bool canSpawnMultiple = false; // Whether multiple entities can be spawned
    [SerializeField] private int maxEntities = 3; // Maximum number of entities if multiple spawning is enabled
    
    private bool playerInRange = false;
    private int spawnedEntityCount = 0;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (eIcon != null)
                eIcon.SetActive(true);
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (eIcon != null)
                eIcon.SetActive(false);
            playerInRange = false;
        }
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            SpawnGatheringEntity();
        }
    }

    private void SpawnGatheringEntity()
    {
        // Check if we can spawn more entities
        if (!canSpawnMultiple && spawnedEntityCount > 0)
        {
            Debug.Log("House: Cannot spawn multiple entities. Only one allowed.");
            return;
        }
        
        if (canSpawnMultiple && spawnedEntityCount >= maxEntities)
        {
            Debug.Log($"House: Maximum entities ({maxEntities}) already spawned.");
            return;
        }

        // Determine spawn position
        Vector3 spawnPosition = GetSpawnPosition();
        
        // Spawn the entity
        if (gatheringEntityPrefab != null)
        {
            GameObject spawnedEntity = Instantiate(gatheringEntityPrefab, spawnPosition, Quaternion.identity);
            spawnedEntityCount++;
            
            Debug.Log($"House: Spawned gathering entity at {spawnPosition}. Total spawned: {spawnedEntityCount}");
        }
        else
        {
            Debug.LogError("House: No gathering entity prefab assigned!");
        }
    }

    private Vector3 GetSpawnPosition()
    {
        // If a specific spawn point is assigned, use it
        if (spawnPoint != null)
        {
            return spawnPoint.position;
        }
        
        // Otherwise, spawn at a random position near the house
        Vector3 housePosition = transform.position;
        float randomAngle = Random.Range(0f, 360f);
        Vector3 spawnOffset = Quaternion.Euler(0, 0, randomAngle) * Vector3.right * spawnDistance;
        
        return housePosition + spawnOffset;
    }

    // Public method to reset entity count (useful for debugging or game reset)
    public void ResetEntityCount()
    {
        spawnedEntityCount = 0;
        Debug.Log("House: Entity count reset to 0");
    }

    // Public method to get current entity count
    public int GetSpawnedEntityCount()
    {
        return spawnedEntityCount;
    }

    // Public method to manually spawn an entity (for testing or external control)
    public void ForceSpawnEntity()
    {
        SpawnGatheringEntity();
    }

    private void Start()
    {
        if (eIcon != null)
        {
            // Compensate for parent scale to keep E_Icon consistent size
            CompensateParentScale();
        }
    }

    private void CompensateParentScale()
    {
        if (eIcon != null)
        {
            Vector3 parentScale = transform.localScale;
            Vector3 compensationScale = new Vector3(
                parentScale.x != 0 ? 1f / parentScale.x : 1f,
                parentScale.y != 0 ? 1f / parentScale.y : 1f,
                parentScale.z != 0 ? 1f / parentScale.z : 1f
            );
            eIcon.transform.localScale = compensationScale;
            
            // Set E_Icon to always be at y = 8 in world space
            Vector3 worldPosition = eIcon.transform.position;
            worldPosition.y = 8f;
            eIcon.transform.position = worldPosition;
        }
    }
} 