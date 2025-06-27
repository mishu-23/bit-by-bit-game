using UnityEngine;

public enum BitType
{
    CoreBit,    // Currency bits - used for economy
    PowerBit    // Combat bits - used for defense
}

public enum Rarity
{
    Common,     // For Core bits (no combat stats)
    Rare,       // Low damage, medium probability
    Epic,       // Medium damage, low probability  
    Legendary   // High damage, very low probability
}

[System.Serializable]
public struct BitStats
{
    public int damage;
    public float minShootingProbability;
    public float maxShootingProbability;
    
    public BitStats(int damage, float minProb, float maxProb)
    {
        this.damage = damage;
        this.minShootingProbability = minProb;
        this.maxShootingProbability = maxProb;
    }
}

[CreateAssetMenu(fileName = "New Bit", menuName = "BitByBit/Bit")]
public class Bit : ScriptableObject
{
    [Header("Basic Information")]
    [SerializeField] private string bitName;
    [SerializeField] private BitType bitType;
    [SerializeField] private Rarity rarity;
    
    [Header("Combat Stats (Power Bits Only)")]
    [SerializeField] private int damage;
    [SerializeField, Range(0f, 1f)] private float shootingProbability;
    
    // Static configuration for bit stats based on rarity
    private static readonly System.Collections.Generic.Dictionary<Rarity, BitStats> RarityStats = 
        new System.Collections.Generic.Dictionary<Rarity, BitStats>
        {
            { Rarity.Common, new BitStats(0, 0f, 0f) },
            { Rarity.Rare, new BitStats(2, 0.6f, 0.8f) },
            { Rarity.Epic, new BitStats(4, 0.4f, 0.5f) },
            { Rarity.Legendary, new BitStats(8, 0.2f, 0.3f) }
        };
    
    #region Properties
    
    public string BitName => bitName;
    public BitType BitType => bitType;
    public Rarity Rarity => rarity;
    public int Damage => damage;
    public float ShootingProbability => shootingProbability;
    
    #endregion
    
    #region Public Methods
    
    public Sprite GetSprite()
    {
        return GetSpriteForTypeAndRarity(bitType, rarity);
    }
    
    public Sprite GetSpriteForTypeAndRarity(BitType bitType, Rarity rarity)
    {
        // Get sprites from BitManager if available
        if (BitManager.Instance != null)
        {
            if (bitType == BitType.CoreBit)
            {
                return BitManager.Instance.coreBitSprite;
            }
            
            // PowerBit sprites based on rarity
            return rarity switch
            {
                Rarity.Rare => BitManager.Instance.rarePowerBitSprite,
                Rarity.Epic => BitManager.Instance.epicPowerBitSprite,
                Rarity.Legendary => BitManager.Instance.legendaryPowerBitSprite,
                _ => BitManager.Instance.rarePowerBitSprite // Fallback to rare
            };
        }
        
        // Fallback: return null if BitManager is not available
        Debug.LogWarning($"BitManager instance not found! Sprite for {bitType} {rarity} will be null.");
        return null;
    }
    
    public Bit CreateRandomizedCopy()
    {
        var copy = CreateInstance<Bit>();
        copy.bitName = this.bitName;
        copy.bitType = this.bitType;
        copy.rarity = this.rarity;
        
        if (bitType == BitType.PowerBit && RarityStats.TryGetValue(rarity, out var stats))
        {
            copy.damage = stats.damage;
            copy.shootingProbability = Random.Range(stats.minShootingProbability, stats.maxShootingProbability);
        }
        else
        {
            copy.damage = 0;
            copy.shootingProbability = 0f;
        }
        
        return copy;
    }
    
    public static Bit CreateBit(string name, BitType type, Rarity rarity, int damage, float shootingProbability)
    {
        var bit = CreateInstance<Bit>();
        bit.bitName = name;
        bit.bitType = type;
        bit.rarity = rarity;
        bit.damage = damage;
        bit.shootingProbability = shootingProbability;
        return bit;
    }
    
    #endregion
    
    #region Unity Editor
    
    private void OnValidate()
    {
        ValidateBitConfiguration();
        ApplyRarityBasedStats();
    }
    
    private void ValidateBitConfiguration()
    {
        // Ensure Power Bits are not Common rarity
        if (bitType == BitType.PowerBit && rarity == Rarity.Common)
        {
            rarity = Rarity.Rare;
            Debug.LogWarning($"Power Bit '{bitName}' cannot be Common rarity. Changed to Rare.", this);
        }
        
        // Ensure Core Bits are Common rarity
        if (bitType == BitType.CoreBit && rarity != Rarity.Common)
        {
            rarity = Rarity.Common;
            Debug.LogWarning($"Core Bit '{bitName}' should be Common rarity. Changed to Common.", this);
        }
        
        // Validate shooting probability range
        shootingProbability = Mathf.Clamp01(shootingProbability);
    }
    
    private void ApplyRarityBasedStats()
    {
        if (RarityStats.TryGetValue(rarity, out var stats))
        {
            if (bitType == BitType.PowerBit)
            {
                damage = stats.damage;
                // Keep shooting probability within valid range for this rarity
                if (shootingProbability < stats.minShootingProbability || 
                    shootingProbability > stats.maxShootingProbability)
                {
                    shootingProbability = (stats.minShootingProbability + stats.maxShootingProbability) / 2f;
                }
            }
            else
            {
                // Core Bits have no combat stats
                damage = 0;
                shootingProbability = 0f;
            }
        }
    }
    
    #endregion
} 