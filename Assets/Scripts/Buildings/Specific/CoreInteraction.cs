using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CoreInteraction : MonoBehaviour
{
    [Header("Assign the E_Icon child here")]
    public GameObject eIcon;
    
    [Header("Progress Display")]
    [SerializeField] private TextMeshPro progressText; // Simple text component to show percentage
    
    [Header("Core Bit Storage")]
    [SerializeField] private int maxCoreBits = 15; // Maximum Core Bits the Core can store
    [SerializeField] private List<Bit> storedCoreBits = new List<Bit>(); // Core Bits stored in the Core
    [SerializeField] private float longPressDuration = 2f;
    
    [Header("Core Upgrade Thresholds")]
    [SerializeField] private int firstThreshold = 5; // First upgrade at 5 Core Bits
    [SerializeField] private int secondThreshold = 10; // Second upgrade at 10 Core Bits
    [SerializeField] private int finalThreshold = 15; // Final upgrade at 15 Core Bits
    
    [Header("Visual Feedback")]
    [SerializeField] private Image progressBar; // Optional radial fill for progress
    [SerializeField] private Color progressColor = Color.green;
    [SerializeField] private Color fullColor = Color.red;
    [SerializeField] private Color upgradeColor = Color.blue; // Color for upgrade thresholds
    
    [Header("Grid Upgrade System")]
    [SerializeField] private SmithBuildManager smithBuildManager; // Reference to manage grid size upgrades
    
    private bool playerInRange = false;
    private bool isLongPressing = false;
    private float longPressTimer = 0f;
    private PowerBitPlayerController playerController;
    private int lastUpgradeLevel = 0; // Track the last upgrade level to prevent duplicate upgrades

    private void Start()
    {
        // Initialize progress bar if assigned
        if (progressBar != null)
        {
            progressBar.fillAmount = 0f;
            progressBar.color = progressColor;
        }
        
        // Load stored Core Bits from save file
        LoadStoredCoreBits();
        
        // Initialize upgrade level based on current progress
        InitializeUpgradeLevel();
        
        UpdateVisualFeedback();

        if (eIcon != null)
        {
            // Compensate for parent scale to keep E_Icon consistent size
            CompensateParentScale();
        }
        
        if (storedCoreBits.Count > 0)
        {
            Debug.Log($"Core loaded with {storedCoreBits.Count} stored Core Bits");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (eIcon != null)
                eIcon.SetActive(true);
            playerInRange = true;
            playerController = other.GetComponent<PowerBitPlayerController>();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (eIcon != null)
                eIcon.SetActive(false);
            playerInRange = false;
            playerController = null;
            
            // Reset long press when leaving range
            ResetLongPress();
        }
    }

    private void Update()
    {
        // Don't process input if game is paused
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            return;
        }
        
        if (!playerInRange || playerController == null) return;

        // Handle long press detection
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
                    Debug.Log("Core: Player has no Core Bits to deposit!");
                }
            }
            
            if (isLongPressing)
            {
                longPressTimer += Time.deltaTime;
                UpdateProgressBar();
                
                // Check if long press is complete
                if (longPressTimer >= longPressDuration)
                {
                    CompleteLongPress();
                }
            }
        }
        else
        {
            // Reset if E is released
            ResetLongPress();
        }
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
        
        // Remove the first Core Bit found
        foreach (var pos in activeBits)
        {
            var bitData = playerController.powerBitCharacterRenderer.GetBitAt(pos);
            if (bitData != null && bitData.bitType == BitType.CoreBit)
            {
                playerController.powerBitCharacterRenderer.RemoveBit(pos);
                
                // Create a Bit object to return
                Bit bit = Bit.CreateBit(bitData.bitName, bitData.bitType, bitData.rarity, bitData.damage, bitData.shootingProbability);
                
                // Save updated build
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
        Debug.Log("Core: Long press started - depositing Core Bit");
    }

    private void CompleteLongPress()
    {
        if (storedCoreBits.Count >= maxCoreBits)
        {
            Debug.Log("Core: Cannot add more Core Bits - Core is full!");
            if (progressBar != null)
            {
                progressBar.color = fullColor;
            }
            ResetLongPress();
            return;
        }

        // Try to take a Core Bit from the player
        Bit coreBit = RemoveCoreBitFromPlayer();
        if (coreBit != null)
        {
            storedCoreBits.Add(coreBit);
            
            Debug.Log($"Core: Core Bit deposited! Total stored: {storedCoreBits.Count}/{maxCoreBits}");
            Debug.Log($"Core: Before CheckForUpgrades - lastUpgradeLevel: {lastUpgradeLevel}");
            
            // Check for upgrades
            CheckForUpgrades();
            
            Debug.Log($"Core: After CheckForUpgrades - lastUpgradeLevel: {lastUpgradeLevel}");
            
            SaveStoredCoreBits();
            UpdateVisualFeedback();
        }
        else
        {
            Debug.Log("Core: Failed to remove Core Bit from player!");
        }
        
        ResetLongPress();
    }

    private void CheckForUpgrades()
    {
        int currentCount = storedCoreBits.Count;
        int currentUpgradeLevel = GetCurrentUpgradeLevel();
        
        Debug.Log($"CheckForUpgrades: currentCount={currentCount}, currentUpgradeLevel={currentUpgradeLevel}, lastUpgradeLevel={lastUpgradeLevel}");
        Debug.Log($"Thresholds: first={firstThreshold}, second={secondThreshold}, final={finalThreshold}");
        
        // Only trigger upgrade if we've reached a new level
        if (currentUpgradeLevel > lastUpgradeLevel)
        {
            Debug.Log($"Upgrade level increased from {lastUpgradeLevel} to {currentUpgradeLevel}!");
            
            if (currentCount >= firstThreshold && lastUpgradeLevel < 1)
            {
                Debug.Log($"🎉 CORE UPGRADE 1! Grid expanded to 3x3! ({currentCount}/{firstThreshold})");
                UpgradePlayerGrid(3);
                lastUpgradeLevel = 1;
            }
            else if (currentCount >= secondThreshold && lastUpgradeLevel < 2)
            {
                Debug.Log($"🎉 CORE UPGRADE 2! Grid expanded to 4x4! ({currentCount}/{secondThreshold})");
                UpgradePlayerGrid(4);
                lastUpgradeLevel = 2;
            }
            else if (currentCount >= finalThreshold && lastUpgradeLevel < 3)
            {
                Debug.Log($"🎉 CORE FULLY REPAIRED! The Core is complete! ({currentCount}/{finalThreshold})");
                lastUpgradeLevel = 3;
                // Grid is already at max size (4x4), so no further expansion
            }
        }
        else
        {
            Debug.Log($"No upgrade triggered. Already at level {lastUpgradeLevel}");
        }
    }
    
        private void UpgradePlayerGrid(int newSize)
    {
        Debug.Log($"UpgradePlayerGrid called with newSize={newSize}");
        
        if (smithBuildManager != null)
        {
            Debug.Log($"SmithBuildManager found! Current grid size: {smithBuildManager.gridSize}");
            
            // Use the special upgrade method that preserves the existing build
            smithBuildManager.UpgradeGridSizeAndSaveBuild(newSize);
            Debug.Log($"Player's build automatically upgraded and saved to {newSize}x{newSize}!");
        }
        else
        {
            Debug.LogError("SmithBuildManager not assigned to CoreInteraction! Cannot upgrade grid size.");
        }
    }
    
    private void InitializeUpgradeLevel()
    {
        int currentCount = storedCoreBits.Count;
        
        Debug.Log($"InitializeUpgradeLevel: currentCount={currentCount}, thresholds=({firstThreshold},{secondThreshold},{finalThreshold})");
        
        // Set the upgrade level based on current Core Bit count
        if (currentCount >= finalThreshold)
        {
            lastUpgradeLevel = 3;
            Debug.Log($"Initialize: Set upgrade level to 3 (final)");
        }
        else if (currentCount >= secondThreshold)
        {
            lastUpgradeLevel = 2;
            Debug.Log($"Initialize: Set upgrade level to 2 (second)");
        }
        else if (currentCount >= firstThreshold)
        {
            lastUpgradeLevel = 1;
            Debug.Log($"Initialize: Set upgrade level to 1 (first)");
        }
        else
        {
            lastUpgradeLevel = 0;
            Debug.Log($"Initialize: Set upgrade level to 0 (none)");
        }
        
        // Set the appropriate grid size for the current level
        if (smithBuildManager != null)
        {
            int targetGridSize = GetGridSizeForLevel(lastUpgradeLevel);
            if (smithBuildManager.gridSize != targetGridSize)
            {
                smithBuildManager.gridSize = targetGridSize;
                Debug.Log($"Grid size initialized to {targetGridSize}x{targetGridSize} for upgrade level {lastUpgradeLevel}");
            }
            else
            {
                Debug.Log($"Grid size already correct: {smithBuildManager.gridSize}x{smithBuildManager.gridSize} for level {lastUpgradeLevel}");
            }
        }
        else
        {
            Debug.LogWarning("SmithBuildManager is null in InitializeUpgradeLevel!");
        }
    }
    
    private int GetGridSizeForLevel(int upgradeLevel)
    {
        switch (upgradeLevel)
        {
            case 0: return 2; // No upgrades: 2x2
            case 1: return 3; // First upgrade: 3x3
            case 2:
            case 3: return 4; // Second/Third upgrade: 4x4
            default: return 2;
        }
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
        // Calculate percentage based on current progress to next threshold
        float percentage = GetCurrentProgressPercentage();
        
        // Update progress text
        if (progressText != null)
        {
            progressText.text = $"{percentage:F0}%";
            
            // Change text color based on progress and thresholds
            if (storedCoreBits.Count >= finalThreshold)
            {
                progressText.color = Color.magenta; // Completed - magenta
            }
            else if (IsNearUpgradeThreshold())
            {
                progressText.color = upgradeColor; // Near upgrade - blue
            }
            else if (percentage >= 75f)
            {
                progressText.color = Color.yellow; // High progress - yellow
            }
            else if (percentage >= 25f)
            {
                progressText.color = Color.white; // Medium progress - white
            }
            else
            {
                progressText.color = Color.gray; // Low progress - gray
            }
        }
        
        // Update progress bar
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
        
        Debug.Log($"Core progress updated: {percentage:F1}% ({storedCoreBits.Count}/{GetCurrentThreshold()}) - Total: {storedCoreBits.Count}/{maxCoreBits}");
    }

    private float GetCurrentProgressPercentage()
    {
        int currentCount = storedCoreBits.Count;
        
        if (currentCount >= finalThreshold)
        {
            return 100f; // Fully complete
        }
        else if (currentCount >= secondThreshold)
        {
            // Progress from second to final threshold
            int progress = currentCount - secondThreshold;
            int range = finalThreshold - secondThreshold;
            return 66.67f + (33.33f * progress / range); // 66.67% to 100%
        }
        else if (currentCount >= firstThreshold)
        {
            // Progress from first to second threshold
            int progress = currentCount - firstThreshold;
            int range = secondThreshold - firstThreshold;
            return 33.33f + (33.34f * progress / range); // 33.33% to 66.67%
        }
        else
        {
            // Progress to first threshold
            return (float)currentCount / firstThreshold * 33.33f; // 0% to 33.33%
        }
    }

    private int GetCurrentThreshold()
    {
        int currentCount = storedCoreBits.Count;
        
        if (currentCount >= finalThreshold) return finalThreshold;
        if (currentCount >= secondThreshold) return finalThreshold;
        if (currentCount >= firstThreshold) return secondThreshold;
        return firstThreshold;
    }

    private bool IsNearUpgradeThreshold()
    {
        int currentCount = storedCoreBits.Count;
        return (currentCount == firstThreshold - 1) || 
               (currentCount == secondThreshold - 1) || 
               (currentCount == finalThreshold - 1);
    }

    private void SaveStoredCoreBits()
    {
        // Create a simple data structure to save
        var saveData = new CoreSaveData();
        saveData.coreBitCount = storedCoreBits.Count;
        saveData.lastUpgradeLevel = lastUpgradeLevel;
        saveData.coreBitNames = new List<string>();
        
        foreach (var bit in storedCoreBits)
        {
            saveData.coreBitNames.Add(bit.BitName);
        }
        
        string json = JsonUtility.ToJson(saveData, true);
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, "core_storage.json");
        System.IO.File.WriteAllText(filePath, json);
        
        Debug.Log($"Core storage saved: {storedCoreBits.Count} Core Bits, upgrade level: {lastUpgradeLevel}");
    }

    private void LoadStoredCoreBits()
    {
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, "core_storage.json");
        
        if (!System.IO.File.Exists(filePath)) 
        {
            Debug.Log("No core storage file found, starting fresh");
            return;
        }

        try
        {
            string json = System.IO.File.ReadAllText(filePath);
            var saveData = JsonUtility.FromJson<CoreSaveData>(json);
            
            storedCoreBits.Clear();
            
            // Load the upgrade level (default to 0 if not present in save)
            lastUpgradeLevel = saveData.lastUpgradeLevel;
            Debug.Log($"Loaded lastUpgradeLevel from save: {lastUpgradeLevel}");
            
            // Recreate Core Bits (simplified - just create basic Core Bits)
            for (int i = 0; i < saveData.coreBitCount; i++)
            {
                string bitName = i < saveData.coreBitNames.Count ? saveData.coreBitNames[i] : "Core Bit";
                Bit coreBit = Bit.CreateBit(bitName, BitType.CoreBit, Rarity.Common, 0, 0f);
                storedCoreBits.Add(coreBit);
            }
            
            Debug.Log($"Core storage loaded: {storedCoreBits.Count} Core Bits, upgrade level: {lastUpgradeLevel}");
            
            // Force recalculate upgrade level to ensure consistency with new thresholds
            int calculatedLevel = GetCurrentUpgradeLevel();
            if (calculatedLevel != lastUpgradeLevel)
            {
                Debug.LogWarning($"Upgrade level mismatch! Saved: {lastUpgradeLevel}, Calculated: {calculatedLevel}. Using calculated value.");
                lastUpgradeLevel = calculatedLevel;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading Core storage: {e.Message}");
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

    // Public methods for external access
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

    public bool IsCoreFull()
    {
        return storedCoreBits.Count >= maxCoreBits;
    }

    public void ResetCore()
    {
        storedCoreBits.Clear();
        lastUpgradeLevel = 0;
        SaveStoredCoreBits();
        UpdateVisualFeedback();
        Debug.Log("Core: All stored Core Bits cleared, upgrade level reset to 0");
    }
    
    // Debug method to test the SmithBuildManager connection
    [ContextMenu("Test SmithBuildManager Connection")]
    public void TestSmithBuildManagerConnection()
    {
        if (smithBuildManager != null)
        {
            Debug.Log($"SmithBuildManager found! Current grid size: {smithBuildManager.gridSize}");
            Debug.Log("Testing manual grid size change to 3x3...");
            smithBuildManager.ChangeGridSize(3);
        }
        else
        {
            Debug.LogError("SmithBuildManager is NULL!");
        }
    }
    
    // Debug method to force recalculate upgrade level
    public void RecalculateUpgradeLevel()
    {
        int oldLevel = lastUpgradeLevel;
        lastUpgradeLevel = 0; // Reset to force recalculation
        InitializeUpgradeLevel();
        Debug.Log($"Upgrade level recalculated: {oldLevel} -> {lastUpgradeLevel}");
        SaveStoredCoreBits(); // Save the corrected level
    }

    // Method to manually add Core Bits (for testing or external control)
    public bool AddCoreBit(Bit coreBit)
    {
        if (storedCoreBits.Count >= maxCoreBits)
        {
            Debug.Log("Core: Cannot add Core Bit - Core is full!");
            return false;
        }

        storedCoreBits.Add(coreBit);
        SaveStoredCoreBits();
        CheckForUpgrades();
        UpdateVisualFeedback();
        Debug.Log($"Core: Core Bit added manually! Total stored: {storedCoreBits.Count}/{maxCoreBits}");
        return true;
    }

    // Get current upgrade status
    public int GetCurrentUpgradeLevel()
    {
        int count = storedCoreBits.Count;
        int level;
        if (count >= finalThreshold) level = 3;
        else if (count >= secondThreshold) level = 2;
        else if (count >= firstThreshold) level = 1;
        else level = 0;
        
        Debug.Log($"GetCurrentUpgradeLevel: count={count}, thresholds=({firstThreshold},{secondThreshold},{finalThreshold}), returning level={level}");
        return level;
    }

    public int GetNextThreshold()
    {
        int count = storedCoreBits.Count;
        if (count < firstThreshold) return firstThreshold;
        if (count < secondThreshold) return secondThreshold;
        if (count < finalThreshold) return finalThreshold;
        return finalThreshold; // Already at max
    }
}

[System.Serializable]
public class CoreSaveData
{
    public int coreBitCount;
    public int lastUpgradeLevel;
    public List<string> coreBitNames;
} 