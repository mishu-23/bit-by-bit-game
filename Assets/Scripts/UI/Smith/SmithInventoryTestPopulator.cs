using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
public class SmithInventoryTestPopulator : MonoBehaviour
{
    [Header("Inventory UI")]
    public Transform inventoryContent; 
    public GameObject inventoryBitSlotPrefab; 
    [Header("Test Power Bits")] 
    public List<Bit> testPowerBits; 
    void Start()
    {
        PopulateInventory();
    }
    public void PopulateInventory()
    {
        foreach (Transform child in inventoryContent)
        {
            Destroy(child.gameObject);
        }
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