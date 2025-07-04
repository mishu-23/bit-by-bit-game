using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BitByBit.Core;
public class InventoryBitSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Image iconImage;
    [HideInInspector] public Bit bitData;
    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private bool droppedOnGridCell = false;
    private bool wasInGrid = false;
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
        wasInGrid = IsInGridCell();
        if (wasInGrid)
        {
            BuildGridCellUI gridCell = GetComponentInParent<BuildGridCellUI>();
            if (gridCell != null)
            {
                gridCell.ClearCurrentBitSlot();
            }
        }
        transform.SetParent(transform.root);
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
            if (wasInGrid)
            {
                ReturnToInventory();
            }
            else
            {
                transform.SetParent(originalParent);
                rectTransform.anchoredPosition = Vector2.zero;
            }
        }
        canvasGroup.blocksRaycasts = true;
    }
    public void MarkDroppedOnGridCell()
    {
        droppedOnGridCell = true;
    }
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
    private void ReturnToInventory()
    {
        Transform inventoryContent = FindInventoryContent();
        if (inventoryContent != null)
        {
            transform.SetParent(inventoryContent);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 0);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Debug.Log($"Returned {bitData?.BitName} to inventory");
        }
        else
        {
            Debug.LogWarning("Could not find inventory content area!");
            Destroy(gameObject);
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
        Debug.LogWarning("InventoryBitSlotUI: Could not find InventoryContent via GameReferences. Please ensure GameReferences is properly configured.");
        return null;
    }
    public void ClearBit()
    {
        bitData = null;
        if (iconImage != null)
        {
            iconImage.sprite = null;
        }
    }
}