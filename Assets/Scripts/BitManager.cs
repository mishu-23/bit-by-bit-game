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
            SetupBitSprites();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void SetupBitSprites()
    {
        // Set the static sprites in the Bit class
        var bitType = typeof(Bit);
        var coreSpriteField = bitType.GetField("coreBitSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var rareSpriteField = bitType.GetField("rarePowerBitSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var epicSpriteField = bitType.GetField("epicPowerBitSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var legendarySpriteField = bitType.GetField("legendaryPowerBitSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        coreSpriteField.SetValue(null, coreBitSprite);
        rareSpriteField.SetValue(null, rarePowerBitSprite);
        epicSpriteField.SetValue(null, epicPowerBitSprite);
        legendarySpriteField.SetValue(null, legendaryPowerBitSprite);
    }
    
    public void TestBitSystem()
    {
        Debug.Log("=== Testing Bit System ===");
        
        if (testCoreBit != null)
        {
            Debug.Log($"Core Bit: {testCoreBit.bitName}");
            Debug.Log($"Type: {testCoreBit.bitType}, Rarity: {testCoreBit.rarity}");
            Debug.Log($"Damage: {testCoreBit.damage}, Sprite: {testCoreBit.GetSprite() != null}");
        }
        
        if (testRarePowerBit != null)
        {
            Debug.Log($"Rare Power Bit: {testRarePowerBit.bitName}");
            Debug.Log($"Type: {testRarePowerBit.bitType}, Rarity: {testRarePowerBit.rarity}");
            Debug.Log($"Damage: {testRarePowerBit.damage}, Probability: {testRarePowerBit.shootingProbability}");
            Debug.Log($"Sprite: {testRarePowerBit.GetSprite() != null}");
        }
        
        if (testEpicPowerBit != null)
        {
            Debug.Log($"Epic Power Bit: {testEpicPowerBit.bitName}");
            Debug.Log($"Type: {testEpicPowerBit.bitType}, Rarity: {testEpicPowerBit.rarity}");
            Debug.Log($"Damage: {testEpicPowerBit.damage}, Probability: {testEpicPowerBit.shootingProbability}");
            Debug.Log($"Sprite: {testEpicPowerBit.GetSprite() != null}");
        }
        
        if (testLegendaryPowerBit != null)
        {
            Debug.Log($"Legendary Power Bit: {testLegendaryPowerBit.bitName}");
            Debug.Log($"Type: {testLegendaryPowerBit.bitType}, Rarity: {testLegendaryPowerBit.rarity}");
            Debug.Log($"Damage: {testLegendaryPowerBit.damage}, Probability: {testLegendaryPowerBit.shootingProbability}");
            Debug.Log($"Sprite: {testLegendaryPowerBit.GetSprite() != null}");
        }
        
        Debug.Log("=== Bit System Test Complete ===");
    }
} 