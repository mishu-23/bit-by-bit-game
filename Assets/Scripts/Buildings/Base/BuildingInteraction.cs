using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
public class BuildingInteraction : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Building Settings")]
    [SerializeField] private string buildingName;
    [SerializeField] private string targetSceneName;
    [SerializeField] private string hoverMessage;
    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 1f, 0.8f);
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError($"BuildingInteraction: No SpriteRenderer found on {gameObject.name}");
            enabled = false;
            return;
        }
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            Debug.LogError($"BuildingInteraction: BoxCollider2D component is required on {gameObject.name}. Please add it manually in the Unity Editor.");
            enabled = false;
            return;
        }
        Debug.Log($"Home is on {gameObject.name}");
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"BuildingInteraction: Clicked on {buildingName}");
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            Debug.Log($"BuildingInteraction: Loading scene {targetSceneName}");
            SceneManager.LoadScene(targetSceneName);
        }
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"BuildingInteraction: Hovering over {buildingName}");
        spriteRenderer.color = hoverColor;
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"BuildingInteraction: Exiting hover on {buildingName}");
        spriteRenderer.color = normalColor;
    }
}