using UnityEngine;
using UnityEngine.UI;

public class CoreInteraction : MonoBehaviour
{
    [Header("Assign the E_Icon child here")]
    public GameObject eIcon;
    
    [Header("Programmable Pixel Settings")]
    [SerializeField] private int maxProgrammablePixels = 5;
    [SerializeField] private float longPressDuration = 2f;
    
    [Header("Visual Feedback")]
    [SerializeField] private Image progressBar; // Optional radial fill for progress
    [SerializeField] private Color progressColor = Color.green;
    [SerializeField] private Color fullColor = Color.red;
    
    private bool playerInRange = false;
    private bool isLongPressing = false;
    private float longPressTimer = 0f;
    private int programmablePixelsCount = 0;

    private void Start()
    {
        // Initialize progress bar if assigned
        if (progressBar != null)
        {
            progressBar.fillAmount = 0f;
            progressBar.color = progressColor;
        }
        
        UpdateVisualFeedback();

        if (eIcon != null)
        {
            // Compensate for parent scale to keep E_Icon consistent size
            CompensateParentScale();
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
            
            // Reset long press when leaving range
            ResetLongPress();
        }
    }

    private void Update()
    {
        if (!playerInRange) return;

        // Handle long press detection
        if (Input.GetKey(KeyCode.E))
        {
            if (!isLongPressing)
            {
                StartLongPress();
            }
            
            longPressTimer += Time.deltaTime;
            UpdateProgressBar();
            
            // Check if long press is complete
            if (longPressTimer >= longPressDuration)
            {
                CompleteLongPress();
            }
        }
        else
        {
            // Reset if E is released
            ResetLongPress();
        }
    }

    private void StartLongPress()
    {
        isLongPressing = true;
        longPressTimer = 0f;
        Debug.Log("Core: Long press started");
    }

    private void CompleteLongPress()
    {
        if (programmablePixelsCount >= maxProgrammablePixels)
        {
            Debug.Log("Core: Cannot add more pixels - Core is full!");
            // Optional: Show visual feedback that core is full
            if (progressBar != null)
            {
                progressBar.color = fullColor;
            }
            return;
        }

        programmablePixelsCount++;
        Debug.Log($"Core: Programmable pixel added! Current count: {programmablePixelsCount}/{maxProgrammablePixels}");
        
        UpdateVisualFeedback();
        ResetLongPress();
    }

    private void ResetLongPress()
    {
        isLongPressing = false;
        longPressTimer = 0f;
        UpdateProgressBar();
    }

    private void UpdateProgressBar()
    {
        if (progressBar != null)
        {
            if (isLongPressing)
            {
                progressBar.fillAmount = longPressTimer / longPressDuration;
                progressBar.color = progressColor;
            }
            else
            {
                progressBar.fillAmount = 0f;
            }
        }
    }

    private void UpdateVisualFeedback()
    {
        // Update any visual indicators based on current pixel count
        if (progressBar != null && !isLongPressing)
        {
            progressBar.fillAmount = (float)programmablePixelsCount / maxProgrammablePixels;
            progressBar.color = programmablePixelsCount >= maxProgrammablePixels ? fullColor : progressColor;
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

    // Public methods for external access
    public int GetProgrammablePixelsCount()
    {
        return programmablePixelsCount;
    }

    public int GetMaxProgrammablePixels()
    {
        return maxProgrammablePixels;
    }

    public bool IsCoreFull()
    {
        return programmablePixelsCount >= maxProgrammablePixels;
    }

    public void ResetPixels()
    {
        programmablePixelsCount = 0;
        UpdateVisualFeedback();
        Debug.Log("Core: All programmable pixels reset");
    }

    // Optional: Method to manually add pixels (for testing or external control)
    public bool AddProgrammablePixel()
    {
        if (programmablePixelsCount >= maxProgrammablePixels)
        {
            Debug.Log("Core: Cannot add pixel - Core is full!");
            return false;
        }

        programmablePixelsCount++;
        UpdateVisualFeedback();
        Debug.Log($"Core: Programmable pixel added manually! Current count: {programmablePixelsCount}/{maxProgrammablePixels}");
        return true;
    }
} 