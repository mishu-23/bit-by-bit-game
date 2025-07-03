using UnityEngine;
using System.IO;

public class GathererManager : MonoBehaviour
{
    public static GathererManager Instance { get; private set; }

    [Header("Gatherer Spawning")]
    public GameObject gathererPrefab;
    public int defaultGathererCount = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        int gathererCount = LoadGathererCountFromSave();
        SpawnGatherers(gathererCount);
    }

    private int LoadGathererCountFromSave()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "settlement_storage.json");
        if (!File.Exists(filePath))
        {
            Debug.Log("No settlement save found, using default gatherer count: " + defaultGathererCount);
            return defaultGathererCount;
        }
        try
        {
            string json = File.ReadAllText(filePath);
            SettlementSaveData saveData = JsonUtility.FromJson<SettlementSaveData>(json);
            if (saveData != null && saveData.gathererCount > 0)
            {
                Debug.Log($"Loaded gatherer count from save: {saveData.gathererCount}");
                return saveData.gathererCount;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error reading gatherer count from save: {e.Message}");
        }
        return defaultGathererCount;
    }

    private void SpawnGatherers(int count)
    {
        if (gathererPrefab == null)
        {
            Debug.LogError("GathererManager: gathererPrefab is not assigned!");
            return;
        }
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = GetSpawnPosition();
            Instantiate(gathererPrefab, spawnPos, Quaternion.identity);
        }
        // Don't update count here since we're spawning based on saved count
    }
    
    public void SpawnSingleGatherer()
    {
        if (gathererPrefab == null)
        {
            Debug.LogError("GathererManager: gathererPrefab is not assigned!");
            return;
        }
        Vector3 spawnPos = GetSpawnPosition();
        Instantiate(gathererPrefab, spawnPos, Quaternion.identity);
        
        // Increment gatherer count by 1
        IncrementGathererCount();
        Debug.Log("Spawned new gatherer, count incremented");
    }

    private Vector3 GetSpawnPosition()
    {
        // Always spawn near manager with random X offset and Z = 0
        Vector3 basePos = transform.position;
        float offsetX = Random.Range(-5f, 5f);
        return new Vector3(basePos.x + offsetX, basePos.y, 0f);
    }

    public void IncrementGathererCount()
    {
        ModifyGathererCount(1);
    }
    
    public void DecrementGathererCount()
    {
        ModifyGathererCount(-1);
    }
    
    private void ModifyGathererCount(int change)
    {
        string filePath = Path.Combine(Application.persistentDataPath, "settlement_storage.json");
        SettlementSaveData saveData;
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            saveData = JsonUtility.FromJson<SettlementSaveData>(json) ?? new SettlementSaveData();
        }
        else
        {
            saveData = new SettlementSaveData();
        }
        
        saveData.gathererCount += change;
        // Ensure count doesn't go below 0
        if (saveData.gathererCount < 0) saveData.gathererCount = 0;
        
        File.WriteAllText(filePath, JsonUtility.ToJson(saveData, true));
        Debug.Log($"Gatherer count modified by {change}, new count: {saveData.gathererCount}");
    }
} 