using UnityEngine;
using UnityEngine.EventSystems;

public class GridCell : MonoBehaviour, IDropHandler
{
    [HideInInspector] public int x;
    [HideInInspector] public int y;
    [HideInInspector] public BuildGridPanel gridPanel;
    public PixelCell currentPixel;

    void Awake()
    {
        // Try to find the grid panel in parent if not set
        if (gridPanel == null)
            gridPanel = GetComponentInParent<BuildGridPanel>();
    }

    public void Initialize(BuildGridPanel panel, int xPos, int yPos)
    {
        gridPanel = panel;
        x = xPos;
        y = yPos;
        name = $"GridCell_{x}_{y}";
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        PixelCell pixel = eventData.pointerDrag.GetComponent<PixelCell>();
        if (pixel != null && gridPanel != null)
        {
            gridPanel.TryPlacePixel(new Vector2Int(x, y), pixel);
        }
    }

    public void ClearCell()
    {
        if (currentPixel != null)
        {
            currentPixel = null;
        }
    }
}