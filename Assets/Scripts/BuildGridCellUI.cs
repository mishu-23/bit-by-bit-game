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
            // Prevent multiple bits in one cell
            if (currentBitSlot != null)
            {
                // Optionally: return the existing bit to inventory or swap
                Debug.Log("Grid cell already occupied. Drop ignored.");
                return;
            }

            // Reparent the slot to this grid cell
            draggedSlot.transform.SetParent(transform);

            RectTransform slotRect = draggedSlot.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = Vector2.zero;
            currentBitSlot = draggedSlot;
            draggedSlot.MarkDroppedOnGridCell();
            Debug.Log($"Dropped {draggedSlot.bitData.bitName} into grid cell!");
        }
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