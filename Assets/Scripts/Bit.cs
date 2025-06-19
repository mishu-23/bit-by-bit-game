using UnityEngine;

public enum BitType
{
    CoreBit,    // Currency bits
    PowerBit    // Combat bits
}

public enum Rarity
{
    Common,     // For Core bits (no combat stats)
    Rare,       // 2 damage, 60-80% probability
    Epic,       // 4 damage, 40-50% probability  
    Legendary   // 8 damage, 20-30% probability
}

[CreateAssetMenu(fileName = "New Bit", menuName = "BitByBit/Bit")]
public class Bit : ScriptableObject
{
    [Header("Basic Info")]
    public string bitName;
    public BitType bitType;
    public Rarity rarity;
    
    [Header("Combat Stats (Power Bits Only)")]
    public int damage;
    public float shootingProbability; // 0-1 range
    
    // Static sprites - one per type/rarity combination
    private static Sprite coreBitSprite;
    private static Sprite rarePowerBitSprite;
    private static Sprite epicPowerBitSprite;
    private static Sprite legendaryPowerBitSprite;
    
    public static Sprite GetSprite(BitType bitType, Rarity rarity)
    {
        if (bitType == BitType.CoreBit)
        {
            return coreBitSprite;
        }
        else // PowerBit
        {
            switch (rarity)
            {
                case Rarity.Rare:
                    return rarePowerBitSprite;
                case Rarity.Epic:
                    return epicPowerBitSprite;
                case Rarity.Legendary:
                    return legendaryPowerBitSprite;
                default:
                    return rarePowerBitSprite; // Fallback
            }
        }
    }
    
    public Sprite GetSprite()
    {
        return GetSprite(bitType, rarity);
    }
    
    private void OnValidate()
    {
        // Ensure Power Bits are not Common rarity
        if (bitType == BitType.PowerBit && rarity == Rarity.Common)
        {
            rarity = Rarity.Rare;
            Debug.LogWarning($"Power Bit '{bitName}' cannot be Common rarity. Changed to Rare.");
        }
        
        // Ensure Core Bits are Common rarity
        if (bitType == BitType.CoreBit && rarity != Rarity.Common)
        {
            rarity = Rarity.Common;
            Debug.LogWarning($"Core Bit '{bitName}' should be Common rarity. Changed to Common.");
        }
        
        // Set damage values based on rarity for Power Bits
        if (bitType == BitType.PowerBit)
        {
            switch (rarity)
            {
                case Rarity.Rare:
                    damage = 2;
                    break;
                case Rarity.Epic:
                    damage = 4;
                    break;
                case Rarity.Legendary:
                    damage = 8;
                    break;
            }
        }
        else
        {
            // Core Bits have no damage
            damage = 0;
        }
    }
} 