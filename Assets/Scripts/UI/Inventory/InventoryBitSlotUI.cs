using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BitByBit.Core;

public class InventoryBitSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Image iconImage; // Assign in inspector
    [HideInInspector] public Bit bitData; // Set when populating inventory

    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private bool droppedOnGridCell = false;
    private bool wasInGrid = false; // Track if this bit was originally in the grid

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        droppedOnGridCell = false;
        originalParent = transform.parent;
        
        // Check if this bit is currently in a grid cell
        wasInGrid = IsInGridCell();
        
        // If it was in a grid cell, notify the cell that it's being dragged out
        if (wasInGrid)
        {
            BuildGridCellUI gridCell = GetComponentInParent<BuildGridCellUI>();
            if (gridCell != null)
            {
                gridCell.ClearCurrentBitSlot();
            }
        }
        
        transform.SetParent(transform.root); // Move to top of UI hierarchy
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!droppedOnGridCell)
        {
            // If this bit was in the grid and wasn't dropped on another grid cell, return it to inventory
            if (wasInGrid)
            {
                ReturnToInventory();
            }
            else
            {
                // If it was from inventory, return to original position
                transform.SetParent(originalParent);
                rectTransform.anchoredPosition = Vector2.zero;
            }
        }
        canvasGroup.blocksRaycasts = true;
    }

    // Called from BuildGridCellUI.OnDrop
    public void MarkDroppedOnGridCell()
    {
        droppedOnGridCell = true;
    }

    // Check if this bit is currently in a grid cell
    private bool IsInGridCell()
    {
        Transform parent = transform.parent;
        while (parent != null)
        {
            if (parent.GetComponent<BuildGridCellUI>() != null)
            {
                return true;
            }
            parent = parent.parent;
        }
        return false;
    }

    // Return the bit to the inventory
    private void ReturnToInventory()
    {
        // Find the inventory content area
        Transform inventoryContent = FindInventoryContent();
        if (inventoryContent != null)
        {
            // Move the bit to the inventory
            transform.SetParent(inventoryContent);
            
            // Reset position and scale
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            
            // Reset anchors to match inventory slots
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 0);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            
            Debug.Log($"Returned {bitData?.BitName} to inventory");
        }
        else
        {
            Debug.LogWarning("Could not find inventory content area!");
            // Fallback: destroy the bit if we can't find inventory
            Destroy(gameObject);
        }
    }

    // Find the inventory content area
    private Transform FindInventoryContent()
    {
        // Use GameReferences for better performance
        if (GameReferences.Instance != null && GameReferences.Instance.InventoryContent != null)
        {
            return GameReferences.Instance.InventoryContent;
        }
        
        // Fallback: look for SmithInventoryTestPopulator if GameReferences fails
        SmithInventoryTestPopulator populator = FindObjectOfType<SmithInventoryTestPopulator>();
        if (populator != null && populator.inventoryContent != null)
        {
            return populator.inventoryContent;
        }
        
        // Last resort fallback (should not be needed if GameReferences is set up correctly)
        Debug.LogWarning("InventoryBitSlotUI: Could not find InventoryContent via GameReferences. Please ensure GameReferences is properly configured.");
        return null;
    }

    // Public method to clear the bit data and icon
    public void ClearBit()
    {
        bitData = null;
        if (iconImage != null)
        {
            iconImage.sprite = null;
        }
    }
} 