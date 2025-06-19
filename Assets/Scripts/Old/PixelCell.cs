using UnityEngine;
using UnityEngine.EventSystems;

public class PixelCell : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public PixelType pixelType { get; private set; }

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private bool isPlaced = false;

    public void Initialize(PixelType type)
    {
        this.pixelType = type;
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        originalParent = transform.parent;

        // Set up rect transform for proper centering
        rectTransform.anchorMin = Vector2.one * 0.5f;
        rectTransform.anchorMax = Vector2.one * 0.5f;
        rectTransform.pivot = Vector2.one * 0.5f;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        transform.SetParent(GetComponentInParent<Canvas>().transform);
        isPlaced = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / GetComponentInParent<Canvas>().scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        var hitObject = eventData.pointerCurrentRaycast.gameObject;
        if (hitObject == null)
        {
            ReturnToInventory();
            return;
        }
        
        var cell = hitObject.GetComponent<GridCell>();
        if (cell == null)
        {
            Debug.LogWarning("Obiectul nu este o celulă validă");
            ReturnToInventory();
            return;
        }

        if (cell.gridPanel == null)
        {
            Debug.LogError($"GridCell la ({cell.x},{cell.y}) nu are gridPanel setat!");
            ReturnToInventory();
            return;
        }

        // 5. Încercăm plasarea
        if (!cell.gridPanel.TryPlacePixel(new Vector2Int(cell.x, cell.y), this))
        {
            Debug.Log($"Plasarea la ({cell.x},{cell.y}) a eșuat");
            ReturnToInventory();
        }
    }

    public void PlaceInCell(Transform newParent)
    {
        Debug.Log($"Placing pixel. New parent: {newParent.name}");

        isPlaced = true;

        // Set the new parent
        transform.SetParent(newParent);

        // Reset position and scale
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localScale = Vector3.one;

        // Ensure it's properly centered
        rectTransform.anchorMin = Vector2.one * 0.5f;
        rectTransform.anchorMax = Vector2.one * 0.5f;
        rectTransform.pivot = Vector2.one * 0.5f;

        transform.SetAsLastSibling(); 
    }

    public void ReturnToInventory(bool forceReturn = false)
    {
        if(!forceReturn && isPlaced)
        {
            return;
        }
        Debug.Log("Returning to inventory");
        transform.SetParent(originalParent);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        isPlaced = false;
    }
}