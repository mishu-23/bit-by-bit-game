using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
public class HouseInteraction : MonoBehaviour
{
    [Header("Assign the Q_Icon and E_Icon children here")]
    public GameObject qIcon;
    public GameObject eIcon;
    [Header("Entity Spawning")]
    [SerializeField] private GameObject gatheringEntityPrefab;
    [SerializeField] private float spawnDistance = 2f;
    [SerializeField] private Transform spawnPoint;
    [Header("Spawn Settings")]
    [SerializeField] private bool canSpawnMultiple = false;
    [SerializeField] private int maxEntities = 3;
    [Header("Progress Display")]
    [SerializeField] private TextMeshPro progressText;
    [Header("Settlement Core Bit Storage")]
    [SerializeField] private int maxCoreBits = 75;
    [SerializeField] private List<Bit> storedCoreBits = new List<Bit>();
    [SerializeField] private float longPressDuration = 2f;
    [Header("Settlement Upgrade Thresholds")]
    [SerializeField] private int villageThreshold = 20;
    [SerializeField] private int townThreshold = 45;
    [SerializeField] private int cityThreshold = 75;
    [Header("Visual Feedback")]
    [SerializeField] private Image progressBar;
    [SerializeField] private Color progressColor = Color.green;
    [SerializeField] private Color fullColor = Color.yellow;
    [SerializeField] private Color upgradeColor = Color.cyan;
    private bool playerInRange = false;
    private int spawnedEntityCount = 0;
    private bool isLongPressing = false;
    private float longPressTimer = 0f;
    private PowerBitPlayerController playerController;
    private SettlementLevel currentLevel = SettlementLevel.Outpost;
    public enum SettlementLevel
    {
        Outpost,
        Village,
        Town,
        City
    }
    private void Start()
    {
        if (progressBar != null)
        {
            progressBar.fillAmount = 0f;
            progressBar.color = progressColor;
        }
        LoadStoredCoreBits();
        UpdateSettlementLevel();
        UpdateVisualFeedback();
        if (qIcon != null)
        {
            qIcon.SetActive(false);
        }
        if (eIcon != null)
        {
            eIcon.SetActive(false);
        }
        if (storedCoreBits.Count > 0)
        {
            Debug.Log($"Settlement loaded with {storedCoreBits.Count} stored Core Bits - Level: {currentLevel}");
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (qIcon != null) qIcon.SetActive(true);
            if (eIcon != null) eIcon.SetActive(true);
            playerInRange = true;
            playerController = other.GetComponent<PowerBitPlayerController>();
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (qIcon != null) qIcon.SetActive(false);
            if (eIcon != null) eIcon.SetActive(false);
            playerInRange = false;
            playerController = null;
            ResetLongPress();
        }
    }
    private void Update()
    {
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            return;
        }
        if (!playerInRange) return;
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SpawnGatheringEntity();
        }
        if (Input.GetKey(KeyCode.E))
        {
            if (!isLongPressing)
            {
                if (PlayerHasCoreBit())
                {
                    StartLongPress();
                }
                else
                {
                    Debug.Log("House: Player has no Core Bits to invest in settlement!");
                }
            }
            if (isLongPressing)
            {
                longPressTimer += Time.deltaTime;
                UpdateProgressBar();
                if (longPressTimer >= longPressDuration)
                {
                    CompleteLongPress();
                }
            }
        }
        else
        {
            ResetLongPress();
        }
    }
    private void SpawnGatheringEntity()
    {
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
        int maxAllowedByLevel = GetMaxGatherersForLevel();
        if (spawnedEntityCount >= maxAllowedByLevel)
        {
            Debug.Log($"House: Settlement level {currentLevel} only allows {maxAllowedByLevel} gatherers. Upgrade settlement to spawn more!");
            return;
        }
        if (GathererManager.Instance != null)
        {
            GathererManager.Instance.SpawnSingleGatherer();
            spawnedEntityCount++;
            Debug.Log($"House: Spawned gathering entity via GathererManager. Total spawned: {spawnedEntityCount}/{maxAllowedByLevel}");
        }
        else
        {
            Debug.LogError("House: GathererManager.Instance not found!");
        }
    }
    private int GetMaxGatherersForLevel()
    {
        switch (currentLevel)
        {
            case SettlementLevel.Outpost: return 1;
            case SettlementLevel.Village: return 3;
            case SettlementLevel.Town: return 6;
            case SettlementLevel.City: return 10;
            default: return 1;
        }
    }
    private Vector3 GetSpawnPosition()
    {
        if (spawnPoint != null)
        {
            return spawnPoint.position;
        }
        Vector3 housePosition = transform.position;
        float randomAngle = Random.Range(0f, 360f);
        Vector3 spawnOffset = Quaternion.Euler(0, 0, randomAngle) * Vector3.right * spawnDistance;
        return housePosition + spawnOffset;
    }
    private bool PlayerHasCoreBit()
    {
        if (playerController == null || playerController.powerBitCharacterRenderer == null) return false;
        var activeBits = playerController.powerBitCharacterRenderer.GetActiveBits();
        foreach (var pos in activeBits)
        {
            var bit = playerController.powerBitCharacterRenderer.GetBitAt(pos);
            if (bit != null && bit.bitType == BitType.CoreBit)
                return true;
        }
        return false;
    }
    private Bit RemoveCoreBitFromPlayer()
    {
        if (playerController == null || playerController.powerBitCharacterRenderer == null) return null;
        var activeBits = playerController.powerBitCharacterRenderer.GetActiveBits();
        foreach (var pos in activeBits)
        {
            var bitData = playerController.powerBitCharacterRenderer.GetBitAt(pos);
            if (bitData != null && bitData.bitType == BitType.CoreBit)
            {
                playerController.powerBitCharacterRenderer.RemoveBit(pos);
                Bit bit = Bit.CreateBit(bitData.bitName, bitData.bitType, bitData.rarity, bitData.damage, bitData.shootingProbability);
                playerController.SaveUpdatedBuild();
                return bit;
            }
        }
        return null;
    }
    private void StartLongPress()
    {
        isLongPressing = true;
        longPressTimer = 0f;
        Debug.Log("House: Long press started - investing Core Bit in settlement");
    }
    private void CompleteLongPress()
    {
        if (storedCoreBits.Count >= maxCoreBits)
        {
            Debug.Log("House: Cannot invest more Core Bits - Settlement is fully developed!");
            if (progressBar != null)
            {
                progressBar.color = fullColor;
            }
            ResetLongPress();
            return;
        }
        Bit coreBit = RemoveCoreBitFromPlayer();
        if (coreBit != null)
        {
            storedCoreBits.Add(coreBit);
            SaveStoredCoreBits();
            Debug.Log($"House: Core Bit invested in settlement! Total invested: {storedCoreBits.Count}/{maxCoreBits}");
            CheckForUpgrades();
            UpdateSettlementLevel();
            UpdateVisualFeedback();
        }
        else
        {
            Debug.Log("House: Failed to remove Core Bit from player!");
        }
        ResetLongPress();
    }
    private void CheckForUpgrades()
    {
        int currentCount = storedCoreBits.Count;
        SettlementLevel oldLevel = currentLevel;
        if (currentCount == villageThreshold && currentLevel == SettlementLevel.Outpost)
        {
            currentLevel = SettlementLevel.Village;
            Debug.Log($"SETTLEMENT UPGRADED TO VILLAGE! Can now spawn {GetMaxGatherersForLevel()} gatherers, new buildings unlocked!");
        }
        else if (currentCount == townThreshold && currentLevel == SettlementLevel.Village)
        {
            currentLevel = SettlementLevel.Town;
            Debug.Log($"SETTLEMENT UPGRADED TO TOWN! Can now spawn {GetMaxGatherersForLevel()} gatherers, trading post unlocked!");
        }
        else if (currentCount == cityThreshold && currentLevel == SettlementLevel.Town)
        {
            currentLevel = SettlementLevel.City;
            Debug.Log($"SETTLEMENT UPGRADED TO CITY! Can now spawn {GetMaxGatherersForLevel()} gatherers, legendary structures available!");
        }
        if (oldLevel != currentLevel)
        {
            SaveStoredCoreBits();
        }
    }
    private void UpdateSettlementLevel()
    {
        int count = storedCoreBits.Count;
        if (count >= cityThreshold)
            currentLevel = SettlementLevel.City;
        else if (count >= townThreshold)
            currentLevel = SettlementLevel.Town;
        else if (count >= villageThreshold)
            currentLevel = SettlementLevel.Village;
        else
            currentLevel = SettlementLevel.Outpost;
    }
    private void ResetLongPress()
    {
        isLongPressing = false;
        longPressTimer = 0f;
        UpdateProgressBar();
    }
    private void UpdateProgressBar()
    {
        if (progressBar != null)
        {
            if (isLongPressing)
            {
                progressBar.fillAmount = longPressTimer / longPressDuration;
                progressBar.color = progressColor;
            }
            else
            {
                progressBar.fillAmount = 0f;
            }
        }
    }
    private void UpdateVisualFeedback()
    {
        float percentage = GetCurrentProgressPercentage();
        if (progressText != null)
        {
            string levelText = GetSettlementLevelText();
            progressText.text = $"{levelText}\n{percentage:F0}%";
            if (storedCoreBits.Count >= cityThreshold)
            {
                progressText.color = Color.yellow;
            }
            else if (IsNearUpgradeThreshold())
            {
                progressText.color = upgradeColor;
            }
            else if (currentLevel == SettlementLevel.Town)
            {
                progressText.color = Color.blue;
            }
            else if (currentLevel == SettlementLevel.Village)
            {
                progressText.color = Color.green;
            }
            else if (percentage >= 50f)
            {
                progressText.color = Color.white;
            }
            else
            {
                progressText.color = Color.gray;
            }
        }
        if (progressBar != null && !isLongPressing)
        {
            progressBar.fillAmount = percentage / 100f;
            if (storedCoreBits.Count >= maxCoreBits)
            {
                progressBar.color = fullColor;
            }
            else if (IsNearUpgradeThreshold())
            {
                progressBar.color = upgradeColor;
            }
            else
            {
                progressBar.color = progressColor;
            }
        }
        Debug.Log($"Settlement progress updated: {percentage:F1}% ({storedCoreBits.Count}/{GetCurrentThreshold()}) - Level: {currentLevel} - Total: {storedCoreBits.Count}/{maxCoreBits}");
    }
    private string GetSettlementLevelText()
    {
        switch (currentLevel)
        {
            case SettlementLevel.Outpost: return "Outpost";
            case SettlementLevel.Village: return "Village";
            case SettlementLevel.Town: return "Town";
            case SettlementLevel.City: return "City";
            default: return "Unknown";
        }
    }
    private float GetCurrentProgressPercentage()
    {
        int currentCount = storedCoreBits.Count;
        if (currentCount >= cityThreshold)
        {
            return 100f;
        }
        else if (currentCount >= townThreshold)
        {
            int progress = currentCount - townThreshold;
            int range = cityThreshold - townThreshold;
            return 75f + (25f * progress / range);
        }
        else if (currentCount >= villageThreshold)
        {
            int progress = currentCount - villageThreshold;
            int range = townThreshold - villageThreshold;
            return 50f + (25f * progress / range);
        }
        else
        {
            return (float)currentCount / villageThreshold * 50f;
        }
    }
    private int GetCurrentThreshold()
    {
        int currentCount = storedCoreBits.Count;
        if (currentCount >= cityThreshold) return cityThreshold;
        if (currentCount >= townThreshold) return cityThreshold;
        if (currentCount >= villageThreshold) return townThreshold;
        return villageThreshold;
    }
    private bool IsNearUpgradeThreshold()
    {
        int currentCount = storedCoreBits.Count;
        return (currentCount == villageThreshold - 1) ||
               (currentCount == townThreshold - 1) ||
               (currentCount == cityThreshold - 1);
    }
    private void SaveStoredCoreBits()
    {
        var saveData = new SettlementSaveData();
        saveData.coreBitCount = storedCoreBits.Count;
        saveData.settlementLevel = (int)currentLevel;
        saveData.coreBitNames = new List<string>();
        foreach (var bit in storedCoreBits)
        {
            saveData.coreBitNames.Add(bit.BitName);
        }
        string json = JsonUtility.ToJson(saveData, true);
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, "settlement_storage.json");
        System.IO.File.WriteAllText(filePath, json);
        Debug.Log($"Settlement storage saved: {storedCoreBits.Count} Core Bits - Level: {currentLevel}");
    }
    private void LoadStoredCoreBits()
    {
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, "settlement_storage.json");
        if (!System.IO.File.Exists(filePath)) return;
        try
        {
            string json = System.IO.File.ReadAllText(filePath);
            var saveData = JsonUtility.FromJson<SettlementSaveData>(json);
            storedCoreBits.Clear();
            if (saveData.settlementLevel >= 0 && saveData.settlementLevel <= 3)
            {
                currentLevel = (SettlementLevel)saveData.settlementLevel;
            }
            for (int i = 0; i < saveData.coreBitCount; i++)
            {
                string bitName = i < saveData.coreBitNames.Count ? saveData.coreBitNames[i] : "Core Bit";
                Bit coreBit = Bit.CreateBit(bitName, BitType.CoreBit, Rarity.Common, 0, 0f);
                storedCoreBits.Add(coreBit);
            }
            Debug.Log($"Settlement storage loaded: {storedCoreBits.Count} Core Bits - Level: {currentLevel}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading Settlement storage: {e.Message}");
        }
    }
    public void ResetEntityCount()
    {
        spawnedEntityCount = 0;
        Debug.Log("House: Entity count reset to 0");
    }
    public int GetSpawnedEntityCount()
    {
        return spawnedEntityCount;
    }
    public void ForceSpawnEntity()
    {
        SpawnGatheringEntity();
    }
    public int GetStoredCoreBitsCount()
    {
        return storedCoreBits.Count;
    }
    public int GetMaxCoreBits()
    {
        return maxCoreBits;
    }
    public float GetProgressPercentage()
    {
        return GetCurrentProgressPercentage();
    }
    public SettlementLevel GetCurrentLevel()
    {
        return currentLevel;
    }
    public bool IsSettlementFullyDeveloped()
    {
        return storedCoreBits.Count >= maxCoreBits;
    }
    public void ResetSettlement()
    {
        storedCoreBits.Clear();
        currentLevel = SettlementLevel.Outpost;
        SaveStoredCoreBits();
        UpdateVisualFeedback();
        Debug.Log("House: All invested Core Bits cleared, settlement reset to Outpost");
    }
    public bool InvestCoreBit(Bit coreBit)
    {
        if (storedCoreBits.Count >= maxCoreBits)
        {
            Debug.Log("House: Cannot invest Core Bit - Settlement is fully developed!");
            return false;
        }
        storedCoreBits.Add(coreBit);
        SaveStoredCoreBits();
        CheckForUpgrades();
        UpdateSettlementLevel();
        UpdateVisualFeedback();
        Debug.Log($"House: Core Bit invested manually! Total invested: {storedCoreBits.Count}/{maxCoreBits}");
        return true;
    }
    public int GetCurrentUpgradeLevel()
    {
        return (int)currentLevel;
    }
    public int GetNextThreshold()
    {
        int count = storedCoreBits.Count;
        if (count < villageThreshold) return villageThreshold;
        if (count < townThreshold) return townThreshold;
        if (count < cityThreshold) return cityThreshold;
        return cityThreshold;
    }
    public string GetSettlementBenefits()
    {
        switch (currentLevel)
        {
            case SettlementLevel.Outpost:
                return $"Basic shelter and storage. Max gatherers: {GetMaxGatherersForLevel()}";
            case SettlementLevel.Village:
                return $"Increased population, basic gatherers. Max gatherers: {GetMaxGatherersForLevel()}";
            case SettlementLevel.Town:
                return $"Trading post, advanced gatherers, workshops. Max gatherers: {GetMaxGatherersForLevel()}";
            case SettlementLevel.City:
                return $"Legendary structures, maximum efficiency. Max gatherers: {GetMaxGatherersForLevel()}";
            default:
                return "Unknown benefits";
        }
    }
}
[System.Serializable]
public class SettlementSaveData
{
    public int coreBitCount;
    public int settlementLevel;
    public List<string> coreBitNames;
    public int depositCoreBitCount = 0;
    public int gathererCount = 0;
}