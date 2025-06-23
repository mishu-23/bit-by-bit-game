using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuildGridCellUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Image highlightImage; // Assign in inspector (child highlight image, not background)

    private InventoryBitSlotUI currentBitSlot; // Track the bit in this cell

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (highlightImage != null)
            highlightImage.enabled = true; // Show highlight
        Debug.Log("Pointer entered grid cell");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (highlightImage != null)
            highlightImage.enabled = false; // Hide highlight
        Debug.Log("Pointer exited grid cell");
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (highlightImage != null)
            highlightImage.enabled = false;

        var draggedSlot = eventData.pointerDrag?.GetComponent<InventoryBitSlotUI>();
        if (draggedSlot != null)
        {
            // If there's already a bit in this cell, swap them
            if (currentBitSlot != null)
            {
                // Return the existing bit to inventory
                ReturnBitToInventory(currentBitSlot);
                
                // Clear the current bit slot reference
                currentBitSlot = null;
            }

            // Place the new bit in this grid cell
            draggedSlot.transform.SetParent(transform);

            RectTransform slotRect = draggedSlot.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = Vector2.zero;
            currentBitSlot = draggedSlot;
            draggedSlot.MarkDroppedOnGridCell();
            Debug.Log($"Dropped {draggedSlot.bitData.bitName} into grid cell! (Swapped with previous bit)");
        }
    }

    // Method to return a bit to the inventory
    private void ReturnBitToInventory(InventoryBitSlotUI bitSlot)
    {
        // Find the inventory content area
        Transform inventoryContent = FindInventoryContent();
        if (inventoryContent != null)
        {
            // Move the bit to the inventory
            bitSlot.transform.SetParent(inventoryContent);
            
            // Reset position and scale
            RectTransform rectTransform = bitSlot.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;
            bitSlot.transform.localScale = Vector3.one;
            
            // Reset anchors to match inventory slots
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 0);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            
            Debug.Log($"Returned {bitSlot.bitData?.bitName} to inventory (swapped out)");
        }
        else
        {
            Debug.LogWarning("Could not find inventory content area! Destroying swapped bit.");
            // Fallback: destroy the bit if we can't find inventory
            Destroy(bitSlot.gameObject);
        }
    }

    // Find the inventory content area
    private Transform FindInventoryContent()
    {
        // Look for the inventory content in the scene
        Transform inventoryContent = GameObject.Find("InventoryContent")?.transform;
        if (inventoryContent == null)
        {
            // Alternative: look for SmithInventoryTestPopulator and get its inventoryContent
            SmithInventoryTestPopulator populator = FindObjectOfType<SmithInventoryTestPopulator>();
            if (populator != null)
            {
                inventoryContent = populator.inventoryContent;
            }
        }
        return inventoryContent;
    }

    // Method to set the current bit slot (used when loading saved data)
    public void SetCurrentBitSlot(InventoryBitSlotUI bitSlot)
    {
        currentBitSlot = bitSlot;
    }

    // Method to get the current bit slot
    public InventoryBitSlotUI GetCurrentBitSlot()
    {
        return currentBitSlot;
    }

    // Method to clear the cell when a bit is dragged out
    public void ClearCurrentBitSlot()
    {
        currentBitSlot = null;
    }

    // Optional: method to clear the cell and return bit to inventory
    public void ClearCell()
    {
        if (currentBitSlot != null)
        {
            // You can implement logic to return the bit to inventory here
            currentBitSlot = null;
        }
    }
} 