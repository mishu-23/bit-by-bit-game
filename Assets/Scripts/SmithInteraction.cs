using UnityEngine;

public class SmithInteraction : MonoBehaviour
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
            Debug.Log("Smith: BuildCharacter UI opened");
        }
    }

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
} 