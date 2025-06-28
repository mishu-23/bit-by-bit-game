using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ProjectileRaritySystem : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Base Chances (%)")]
    [SerializeField] private float baseLegendaryChance = 10f;
    [SerializeField] private float baseEpicChance = 20f;
    [SerializeField] private float baseRareChance = 30f;
    [SerializeField] private float baseDefaultChance = 40f;
    
    [Header("Weight Per Bit (%)")]
    [SerializeField] private float legendaryBitWeight = 2.0f;
    [SerializeField] private float epicBitWeight = 1.5f;
    [SerializeField] private float rareBitWeight = 1.0f;
    [SerializeField] private float defaultBitWeight = 0.5f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    #endregion
    
    #region Private Fields
    
    private PowerBitCharacterRenderer characterRenderer;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        FindCharacterRenderer();
    }
    
    #endregion
    
    #region Initialization
    
    private void FindCharacterRenderer()
    {
        characterRenderer = GetComponentInParent<PowerBitCharacterRenderer>();
        if (characterRenderer == null)
        {
            characterRenderer = FindObjectOfType<PowerBitCharacterRenderer>();
        }
        
        if (characterRenderer == null)
        {
            Debug.LogError("ProjectileRaritySystem: No PowerBitCharacterRenderer found!");
        }
    }
    
    #endregion
    
    #region Public Interface
    
    public Rarity DetermineProjectileRarity()
    {
        if (characterRenderer == null)
        {
            Debug.LogWarning("ProjectileRaritySystem: No character renderer available, using Default rarity");
            return Rarity.Common;
        }
        
        // Get bit counts from player build
        var bitCounts = GetPlayerBitCounts();
        
        // Calculate raw chances
        var rawChances = CalculateRawChances(bitCounts);
        
        // Filter out rarities not present in player build
        var filteredChances = FilterByPlayerBuild(rawChances, bitCounts);
        
        // Normalize to 100%
        var normalizedChances = NormalizeChances(filteredChances);
        
        // Perform weighted random selection
        Rarity selectedRarity = PerformWeightedSelection(normalizedChances);
        
        LogRarityCalculation(bitCounts, rawChances, filteredChances, normalizedChances, selectedRarity);
        
        return selectedRarity;
    }
    
    #endregion
    
    #region Bit Count Analysis
    
    private Dictionary<Rarity, int> GetPlayerBitCounts()
    {
        var bitCounts = new Dictionary<Rarity, int>
        {
            { Rarity.Legendary, 0 },
            { Rarity.Epic, 0 },
            { Rarity.Rare, 0 },
            { Rarity.Common, 0 }
        };
        
        if (characterRenderer == null) return bitCounts;
        
        // Get all active bits from the character renderer
        var activeBits = characterRenderer.GetActiveBits();
        
        // Count bits by rarity
        foreach (var bitPos in activeBits)
        {
            var bitData = characterRenderer.GetBitAt(bitPos);
            if (bitData != null)
            {
                bitCounts[bitData.rarity]++;
            }
        }
        
        // Calculate default bits (empty grid spaces)
        int totalGridSpaces = characterRenderer.GetGridSize() * characterRenderer.GetGridSize();
        int occupiedSpaces = activeBits.Count;
        bitCounts[Rarity.Common] = totalGridSpaces - occupiedSpaces;
        
        return bitCounts;
    }
    
    #endregion
    
    #region Chance Calculation
    
    private Dictionary<Rarity, float> CalculateRawChances(Dictionary<Rarity, int> bitCounts)
    {
        var rawChances = new Dictionary<Rarity, float>
        {
            { Rarity.Legendary, baseLegendaryChance + (bitCounts[Rarity.Legendary] * legendaryBitWeight) },
            { Rarity.Epic, baseEpicChance + (bitCounts[Rarity.Epic] * epicBitWeight) },
            { Rarity.Rare, baseRareChance + (bitCounts[Rarity.Rare] * rareBitWeight) },
            { Rarity.Common, baseDefaultChance + (bitCounts[Rarity.Common] * defaultBitWeight) }
        };
        
        return rawChances;
    }
    
    private Dictionary<Rarity, float> FilterByPlayerBuild(Dictionary<Rarity, float> rawChances, Dictionary<Rarity, int> bitCounts)
    {
        var filteredChances = new Dictionary<Rarity, float>();
        
        foreach (var kvp in rawChances)
        {
            // If player has 0 bits of this rarity (excluding Common which represents empty spaces), set chance to 0
            if (kvp.Key != Rarity.Common && bitCounts[kvp.Key] == 0)
            {
                filteredChances[kvp.Key] = 0f;
            }
            else
            {
                filteredChances[kvp.Key] = kvp.Value;
            }
        }
        
        return filteredChances;
    }
    
    private Dictionary<Rarity, float> NormalizeChances(Dictionary<Rarity, float> chances)
    {
        float totalChance = chances.Values.Sum();
        
        if (totalChance <= 0f)
        {
            // Fallback: if no valid chances, return 100% Common
            return new Dictionary<Rarity, float>
            {
                { Rarity.Legendary, 0f },
                { Rarity.Epic, 0f },
                { Rarity.Rare, 0f },
                { Rarity.Common, 100f }
            };
        }
        
        var normalizedChances = new Dictionary<Rarity, float>();
        foreach (var kvp in chances)
        {
            normalizedChances[kvp.Key] = (kvp.Value / totalChance) * 100f;
        }
        
        return normalizedChances;
    }
    
    #endregion
    
    #region Random Selection
    
    private Rarity PerformWeightedSelection(Dictionary<Rarity, float> normalizedChances)
    {
        float randomValue = Random.Range(0f, 100f);
        float cumulativeChance = 0f;
        
        // Check in order: Legendary, Epic, Rare, Common
        var orderedRarities = new[] { Rarity.Legendary, Rarity.Epic, Rarity.Rare, Rarity.Common };
        
        foreach (var rarity in orderedRarities)
        {
            cumulativeChance += normalizedChances[rarity];
            if (randomValue <= cumulativeChance)
            {
                return rarity;
            }
        }
        
        // Fallback to Common if something goes wrong
        return Rarity.Common;
    }
    
    #endregion
    
    #region Debug and Logging
    
    private void LogRarityCalculation(Dictionary<Rarity, int> bitCounts, Dictionary<Rarity, float> rawChances, 
                                    Dictionary<Rarity, float> filteredChances, Dictionary<Rarity, float> normalizedChances, 
                                    Rarity selectedRarity)
    {
        if (!showDebugInfo) return;
        
        Debug.Log("=== PROJECTILE RARITY CALCULATION ===");
        Debug.Log($"Player Build - Legendary: {bitCounts[Rarity.Legendary]}, Epic: {bitCounts[Rarity.Epic]}, Rare: {bitCounts[Rarity.Rare]}, Common: {bitCounts[Rarity.Common]}");
        Debug.Log($"Raw Chances - Legendary: {rawChances[Rarity.Legendary]:F1}%, Epic: {rawChances[Rarity.Epic]:F1}%, Rare: {rawChances[Rarity.Rare]:F1}%, Common: {rawChances[Rarity.Common]:F1}%");
        Debug.Log($"Filtered Chances - Legendary: {filteredChances[Rarity.Legendary]:F1}%, Epic: {filteredChances[Rarity.Epic]:F1}%, Rare: {filteredChances[Rarity.Rare]:F1}%, Common: {filteredChances[Rarity.Common]:F1}%");
        Debug.Log($"Final Chances - Legendary: {normalizedChances[Rarity.Legendary]:F1}%, Epic: {normalizedChances[Rarity.Epic]:F1}%, Rare: {normalizedChances[Rarity.Rare]:F1}%, Common: {normalizedChances[Rarity.Common]:F1}%");
        Debug.Log($"Selected Rarity: {selectedRarity}");
        Debug.Log("=====================================");
    }
    
    #endregion
} 