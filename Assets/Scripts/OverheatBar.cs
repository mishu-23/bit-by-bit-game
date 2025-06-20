using UnityEngine;
using UnityEngine.UI;

public class OverheatBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] public Image backgroundBar;
    [SerializeField] public Image fillBar;
    
    [Header("Player Reference")]
    [SerializeField] private PowerBitPlayerController playerController;
    
    [Header("Position Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, 0f);
    [SerializeField] private bool followPlayer = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    private Camera mainCamera;
    private Canvas canvas;
    private RectTransform rectTransform;
    
    private void Awake()
    {
        // Get components
        canvas = GetComponentInParent<Canvas>();
        rectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;
        
        if (canvas == null)
        {
            Debug.LogError("OverheatBar must be a child of a Canvas!");
        }
        
        if (mainCamera == null)
        {
            Debug.LogError("No main camera found! OverheatBar needs a camera to follow the player.");
        }
        
        // Auto-find player if not assigned
        if (playerController == null)
        {
            playerController = FindObjectOfType<PowerBitPlayerController>();
            if (playerController == null)
            {
                Debug.LogError("No PowerBitPlayerController found in scene!");
            }
        }
        
        // Initialize bar
        if (fillBar != null)
        {
            fillBar.fillAmount = 0f;
        }
    }
    
    private void Update()
    {
        if (playerController == null || !followPlayer) return;
        
        // Update bar fill amount
        UpdateBarFill();
        
        // Update position to follow player
        UpdatePosition();
    }
    
    private void UpdateBarFill()
    {
        if (fillBar == null) return;
        
        float overheatLevel = playerController.GetOverheatLevel();
        float overheatMax = playerController.GetOverheatMax();
        float fillAmount = overheatLevel / overheatMax;
        
        fillBar.fillAmount = fillAmount;
        
        // Change color based on overheat level
        if (playerController.IsOverheated())
        {
            fillBar.color = Color.red;
        }
        else if (fillAmount > 0.7f)
        {
            fillBar.color = Color.yellow;
        }
        else
        {
            fillBar.color = Color.green;
        }
        
        if (showDebugInfo && Time.frameCount % 60 == 0) // Log every 60 frames
        {
            Debug.Log($"Overheat: {overheatLevel:F2}/{overheatMax:F2} ({fillAmount:P1})");
        }
    }
    
    private void UpdatePosition()
    {
        if (playerController == null || canvas == null || mainCamera == null) return;
        
        // Get player world position
        Vector3 playerWorldPos = playerController.transform.position + offset;
        
        // Convert world position to screen position
        Vector3 screenPos = mainCamera.WorldToScreenPoint(playerWorldPos);
        
        // Convert screen position to canvas position
        Vector2 canvasPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPos,
            canvas.worldCamera,
            out canvasPos
        );
        
        // Update bar position
        rectTransform.anchoredPosition = canvasPos;
    }
    
    // Public method to set the player controller reference
    public void SetPlayerController(PowerBitPlayerController controller)
    {
        playerController = controller;
    }
    
    // Public method to toggle following
    public void SetFollowPlayer(bool follow)
    {
        followPlayer = follow;
    }
    
    // Public method to set offset
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }
} 