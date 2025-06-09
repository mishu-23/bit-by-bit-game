using UnityEngine;
using UnityEngine.SceneManagement;

public class CoreInteraction : MonoBehaviour
{
    [Header("Assign the E_Icon child here")]
    public GameObject eIcon;
    public BuildOverlayManager overlayManager;

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
            if (overlayManager != null)
                overlayManager.ShowOverlay();
            // Optionally, disable player movement here
        }
    }
} 