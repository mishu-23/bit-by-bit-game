using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryBitSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Image iconImage; // Assign in inspector
    [HideInInspector] public Bit bitData; // Set when populating inventory

    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private bool droppedOnGridCell = false;

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
            transform.SetParent(originalParent);
            rectTransform.anchoredPosition = Vector2.zero;
        }
        canvasGroup.blocksRaycasts = true;
    }

    // Called from BuildGridCellUI.OnDrop
    public void MarkDroppedOnGridCell()
    {
        droppedOnGridCell = true;
    }
} 