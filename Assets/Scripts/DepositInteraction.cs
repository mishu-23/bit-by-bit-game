using UnityEngine;
using UnityEngine.UI;

public class DepositInteraction : MonoBehaviour
{
    [Header("Assign the E_Icon child here")]
    public GameObject eIcon;
    
    [Header("Pixel Retrieval Settings")]
    [SerializeField] private float longPressDuration = 2f;
    [SerializeField] private int availablePixels = 10; // Number of pixels available to retrieve
    
    [Header("Visual Feedback")]
    [SerializeField] private Image progressBar; // Optional radial fill for progress
    [SerializeField] private Color progressColor = Color.blue;
    [SerializeField] private Color emptyColor = Color.gray;
    
    private bool playerInRange = false;
    private bool isLongPressing = false;
    private float longPressTimer = 0f;
    
    private void Start()
    {
        if (eIcon != null)
        {
            // Compensate for parent scale to keep E_Icon consistent size
            CompensateParentScale();
        }
    }

    private void CompensateParentScale()
    {
        if (eIcon != null)
        {
            Vector3 parentScale = transform.localScale;
            Vector3 compensationScale = new Vector3(
                parentScale.x != 0 ? 1f / parentScale.x : 1f,
                parentScale.y != 0 ? 1f / parentScale.y : 1f,
                parentScale.z != 0 ? 1f / parentScale.z : 1f
            );
            eIcon.transform.localScale = compensationScale;
            
            // Set E_Icon to always be at y = 8 in world space
            Vector3 worldPosition = eIcon.transform.position;
            worldPosition.y = 8f;
            eIcon.transform.position = worldPosition;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (eIcon != null)
                eIcon.SetActive(true);
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (eIcon != null)
                eIcon.SetActive(false);
            playerInRange = false;
            
            // Reset long press when leaving
            isLongPressing = false;
            longPressTimer = 0f;
            UpdateVisualFeedback();
        }
    }

    private void Update()
    {
        if (playerInRange)
        {
            HandleLongPress();
        }
    }

    private void HandleLongPress()
    {
        if (Input.GetKey(KeyCode.E))
        {
            if (!isLongPressing)
            {
                isLongPressing = true;
                longPressTimer = 0f;
            }

            longPressTimer += Time.deltaTime;
            UpdateVisualFeedback();

            if (longPressTimer >= longPressDuration)
            {
                RetrievePixel();
                isLongPressing = false;
                longPressTimer = 0f;
                UpdateVisualFeedback();
            }
        }
        else
        {
            if (isLongPressing)
            {
                isLongPressing = false;
                longPressTimer = 0f;
                UpdateVisualFeedback();
            }
        }
    }

    private void RetrievePixel()
    {
        if (availablePixels > 0)
        {
            availablePixels--;
            Debug.Log($"Pixel retrieved from Deposit! Available pixels: {availablePixels}");
            
            // TODO: Add pixel to player's inventory here
            // For now, just show in console
        }
        else
        {
            Debug.Log("Deposit is empty! No more pixels available.");
        }
    }

    private void UpdateVisualFeedback()
    {
        if (progressBar != null)
        {
            if (availablePixels > 0)
            {
                progressBar.gameObject.SetActive(true);
                progressBar.color = progressColor;
                progressBar.fillAmount = isLongPressing ? longPressTimer / longPressDuration : 0f;
            }
            else
            {
                progressBar.color = emptyColor;
                progressBar.fillAmount = 1f;
            }
        }
    }

    // Public methods for external access
    public int GetAvailablePixels()
    {
        return availablePixels;
    }

    public void AddPixels(int amount)
    {
        availablePixels += amount;
        Debug.Log($"Added {amount} pixels to Deposit. Total available: {availablePixels}");
    }
} 