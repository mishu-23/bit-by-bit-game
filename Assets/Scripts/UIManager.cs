using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Overheat Bar")]
    [SerializeField] private OverheatBar overheatBar;
    [SerializeField] private Canvas gameCanvas;
    
    [Header("Overheat Bar Prefab")]
    [SerializeField] private GameObject overheatBarPrefab;
    
    private void Awake()
    {
        // Auto-find canvas if not assigned
        if (gameCanvas == null)
        {
            gameCanvas = FindObjectOfType<Canvas>();
            if (gameCanvas == null)
            {
                Debug.LogError("No Canvas found in scene! UIManager needs a Canvas for the overheat bar.");
                return;
            }
        }
        
        // Create overheat bar if not assigned
        if (overheatBar == null)
        {
            CreateOverheatBar();
        }
    }
    
    private void CreateOverheatBar()
    {
        if (overheatBarPrefab != null)
        {
            // Instantiate from prefab
            GameObject barObj = Instantiate(overheatBarPrefab, gameCanvas.transform);
            overheatBar = barObj.GetComponent<OverheatBar>();
        }
        else
        {
            // Create manually
            overheatBar = CreateOverheatBarManually();
        }
        
        if (overheatBar != null)
        {
            Debug.Log("Overheat bar created successfully!");
        }
        else
        {
            Debug.LogError("Failed to create overheat bar!");
        }
    }
    
    private OverheatBar CreateOverheatBarManually()
    {
        // Create the main bar container
        GameObject barContainer = new GameObject("OverheatBar");
        barContainer.transform.SetParent(gameCanvas.transform, false);
        
        // Add RectTransform and OverheatBar component
        RectTransform containerRect = barContainer.AddComponent<RectTransform>();
        OverheatBar overheatBarComponent = barContainer.AddComponent<OverheatBar>();
        
        // Set up container
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(100f, 20f);
        containerRect.anchoredPosition = Vector2.zero;
        
        // Create background bar
        GameObject backgroundObj = new GameObject("Background");
        backgroundObj.transform.SetParent(barContainer.transform, false);
        
        RectTransform bgRect = backgroundObj.AddComponent<RectTransform>();
        Image bgImage = backgroundObj.AddComponent<Image>();
        
        // Set up background
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Dark gray with transparency
        
        // Create fill bar
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(barContainer.transform, false);
        
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        Image fillImage = fillObj.AddComponent<Image>();
        
        // Set up fill bar
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;
        fillImage.color = Color.green;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillAmount = 0f;
        
        // Assign references to the OverheatBar component
        overheatBarComponent.backgroundBar = bgImage;
        overheatBarComponent.fillBar = fillImage;
        
        return overheatBarComponent;
    }
    
    // Public method to get the overheat bar
    public OverheatBar GetOverheatBar()
    {
        return overheatBar;
    }
    
    // Public method to show/hide the overheat bar
    public void SetOverheatBarVisible(bool visible)
    {
        if (overheatBar != null)
        {
            overheatBar.gameObject.SetActive(visible);
        }
    }
} 