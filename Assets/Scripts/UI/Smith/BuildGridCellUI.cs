using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BitByBit.Core;
public class BuildGridCellUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Image highlightImage;
    private InventoryBitSlotUI currentBitSlot;
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (highlightImage != null)
            highlightImage.enabled = true;
        Debug.Log("Pointer entered grid cell");
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if (highlightImage != null)
            highlightImage.enabled = false;
        Debug.Log("Pointer exited grid cell");
    }
    public void OnDrop(PointerEventData eventData)
    {
        if (highlightImage != null)
            highlightImage.enabled = false;
        var draggedSlot = eventData.pointerDrag?.GetComponent<InventoryBitSlotUI>();
        if (draggedSlot != null)
        {
            if (currentBitSlot != null)
            {
                ReturnBitToInventory(currentBitSlot);
                currentBitSlot = null;
            }
            draggedSlot.transform.SetParent(transform);
            RectTransform slotRect = draggedSlot.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = Vector2.zero;
            currentBitSlot = draggedSlot;
            draggedSlot.MarkDroppedOnGridCell();
            Debug.Log($"Dropped {draggedSlot.bitData.BitName} into grid cell! (Swapped with previous bit)");
        }
    }
    private void ReturnBitToInventory(InventoryBitSlotUI bitSlot)
    {
        Transform inventoryContent = FindInventoryContent();
        if (inventoryContent != null)
        {
            bitSlot.transform.SetParent(inventoryContent);
            RectTransform rectTransform = bitSlot.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;
            bitSlot.transform.localScale = Vector3.one;
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 0);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Debug.Log($"Returned {bitSlot.bitData?.BitName} to inventory (swapped out)");
        }
        else
        {
            Debug.LogWarning("Could not find inventory content area! Destroying swapped bit.");
            Destroy(bitSlot.gameObject);
        }
    }
    private Transform FindInventoryContent()
    {
        if (GameReferences.Instance != null && GameReferences.Instance.InventoryContent != null)
        {
            return GameReferences.Instance.InventoryContent;
        }
        SmithInventoryTestPopulator populator = FindObjectOfType<SmithInventoryTestPopulator>();
        if (populator != null && populator.inventoryContent != null)
        {
            return populator.inventoryContent;
        }
        Debug.LogWarning("BuildGridCellUI: Could not find InventoryContent via GameReferences. Please ensure GameReferences is properly configured.");
        return null;
    }
    public void SetCurrentBitSlot(InventoryBitSlotUI bitSlot)
    {
        currentBitSlot = bitSlot;
    }
    public InventoryBitSlotUI GetCurrentBitSlot()
    {
        return currentBitSlot;
    }
    public void ClearCurrentBitSlot()
    {
        currentBitSlot = null;
    }
    public void ClearCell()
    {
        if (currentBitSlot != null)
        {
            currentBitSlot = null;
        }
    }
}