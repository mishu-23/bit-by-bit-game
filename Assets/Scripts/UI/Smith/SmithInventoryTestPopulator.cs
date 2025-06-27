using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SmithInventoryTestPopulator : MonoBehaviour
{
    [Header("Inventory UI")]
    public Transform inventoryContent; // Assign the Content object of the ScrollView
    public GameObject inventoryBitSlotPrefab; // Assign the InventoryBitSlot prefab

    [Header("Test Power Bits")] 
    public List<Bit> testPowerBits; // Assign Power Bit ScriptableObjects here

    void Start()
    {
        PopulateInventory();
    }

    public void PopulateInventory()
    {
        // Clear existing children
        foreach (Transform child in inventoryContent)
        {
            Destroy(child.gameObject);
        }

        // Instantiate a slot for each test Power Bit
        foreach (Bit bit in testPowerBits)
        {
            if (bit == null || bit.BitType != BitType.PowerBit) continue;
            GameObject slotGO = Instantiate(inventoryBitSlotPrefab, inventoryContent);
            InventoryBitSlotUI slotUI = slotGO.GetComponent<InventoryBitSlotUI>();
            if (slotUI != null)
            {
                slotUI.bitData = bit;
                if (slotUI.iconImage != null)
                    slotUI.iconImage.sprite = bit.GetSprite();
            }
        }
    }
} 