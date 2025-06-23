using UnityEngine;

public class SmithInteraction : MonoBehaviour
{
    [Header("Assign the E_Icon child here")]
    public GameObject eIcon;
    // public BuildOverlayManager overlayManager; // Not needed for new menu

    public GameObject smithCanvas; // Assign in inspector
    public SmithBuildManager smithBuildManager; // Assign in inspector

    private bool playerInRange = false;

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
        }
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (smithCanvas != null)
            {
                smithCanvas.SetActive(true);
                
                // Load current build into the Smith UI
                if (smithBuildManager != null)
                {
                    smithBuildManager.LoadCurrentBuild();
                }
                else
                {
                    Debug.LogWarning("SmithBuildManager not assigned! Cannot load current build.");
                }
            }
            // Optionally: Time.timeScale = 0; // Pause game
        }

        if (smithCanvas != null && smithCanvas.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            // Call the proper close method to revert changes
            if (smithBuildManager != null)
            {
                smithBuildManager.CloseSmithMenu();
            }
            else
            {
                // Fallback: just hide the canvas if build manager is not assigned
                smithCanvas.SetActive(false);
                Debug.LogWarning("SmithBuildManager not assigned! Changes may not be reverted properly.");
            }
            // Optionally: Time.timeScale = 1; // Resume game
        }
    }

    private void Start()
    {
        if (eIcon != null)
        {
            CompensateParentScale();
        }
        if (smithCanvas != null)
        {
            smithCanvas.SetActive(false); // Hide by default
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
} 