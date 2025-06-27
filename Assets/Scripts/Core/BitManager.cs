using UnityEngine;

public class BitManager : MonoBehaviour
{
    public static BitManager Instance { get; private set; }
    
    [Header("Bit Sprites")]
    public Sprite coreBitSprite;
    public Sprite rarePowerBitSprite;
    public Sprite epicPowerBitSprite;
    public Sprite legendaryPowerBitSprite;
    
    [Header("Test Bits")]
    public Bit testCoreBit;
    public Bit testRarePowerBit;
    public Bit testEpicPowerBit;
    public Bit testLegendaryPowerBit;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void TestBitSystem()
    {
        Debug.Log("=== Testing Bit System ===");
        
        if (testCoreBit != null)
        {
            Debug.Log($"Core Bit: {testCoreBit.BitName}");
            Debug.Log($"Type: {testCoreBit.BitType}, Rarity: {testCoreBit.Rarity}");
            Debug.Log($"Damage: {testCoreBit.Damage}, Sprite: {testCoreBit.GetSprite() != null}");
        }
        
        if (testRarePowerBit != null)
        {
            Debug.Log($"Rare Power Bit: {testRarePowerBit.BitName}");
            Debug.Log($"Type: {testRarePowerBit.BitType}, Rarity: {testRarePowerBit.Rarity}");
            Debug.Log($"Damage: {testRarePowerBit.Damage}, Probability: {testRarePowerBit.ShootingProbability}");
            Debug.Log($"Sprite: {testRarePowerBit.GetSprite() != null}");
        }
        
        if (testEpicPowerBit != null)
        {
            Debug.Log($"Epic Power Bit: {testEpicPowerBit.BitName}");
            Debug.Log($"Type: {testEpicPowerBit.BitType}, Rarity: {testEpicPowerBit.Rarity}");
            Debug.Log($"Damage: {testEpicPowerBit.Damage}, Probability: {testEpicPowerBit.ShootingProbability}");
            Debug.Log($"Sprite: {testEpicPowerBit.GetSprite() != null}");
        }
        
        if (testLegendaryPowerBit != null)
        {
            Debug.Log($"Legendary Power Bit: {testLegendaryPowerBit.BitName}");
            Debug.Log($"Type: {testLegendaryPowerBit.BitType}, Rarity: {testLegendaryPowerBit.Rarity}");
            Debug.Log($"Damage: {testLegendaryPowerBit.Damage}, Probability: {testLegendaryPowerBit.ShootingProbability}");
            Debug.Log($"Sprite: {testLegendaryPowerBit.GetSprite() != null}");
        }
        
        Debug.Log("=== Bit System Test Complete ===");
    }
    
    public Bit GetRandomBit()
    {
        // Create an array of possible bit types
        System.Array rarityValues = System.Enum.GetValues(typeof(Rarity));
        Rarity randomRarity = (Rarity)rarityValues.GetValue(Random.Range(0, rarityValues.Length));
        
        // Core bits are always Common, Power bits can be any other rarity
        BitType bitType;
        if (randomRarity == Rarity.Common)
        {
            bitType = BitType.CoreBit;
        }
        else
        {
            bitType = BitType.PowerBit;
        }
        
        // Create a random bit using the factory method
        string bitName = $"{randomRarity} {bitType}";
        
        // Get appropriate stats for the rarity
        int damage = 0;
        float shootingProbability = 0f;
        
        if (bitType == BitType.PowerBit)
        {
            damage = randomRarity switch
            {
                Rarity.Rare => 2,
                Rarity.Epic => 4,
                Rarity.Legendary => 8,
                _ => 0
            };
            
            shootingProbability = randomRarity switch
            {
                Rarity.Rare => Random.Range(0.6f, 0.8f),
                Rarity.Epic => Random.Range(0.4f, 0.5f),
                Rarity.Legendary => Random.Range(0.2f, 0.3f),
                _ => 0f
            };
        }
        
        return Bit.CreateBit(bitName, bitType, randomRarity, damage, shootingProbability);
    }
} 