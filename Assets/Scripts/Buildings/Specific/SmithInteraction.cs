using UnityEngine;
public class SmithInteraction : MonoBehaviour
{
    [Header("Assign the E_Icon child here")]
    public GameObject eIcon;
    public GameObject smithCanvas;
    public SmithBuildManager smithBuildManager;
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
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused && !IsSmithMenuOpen())
        {
            return;
        }
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (smithCanvas != null)
            {
                smithCanvas.SetActive(true);
                if (PauseManager.Instance != null)
                {
                    PauseManager.Instance.PauseGame(pauseType: PauseManager.PauseType.SmithBuilder);
                }
                if (smithBuildManager != null)
                {
                    smithBuildManager.LoadCurrentBuild();
                }
                else
                {
                    Debug.LogWarning("SmithBuildManager not assigned! Cannot load current build.");
                }
            }
        }
        if (smithCanvas != null && smithCanvas.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            if (smithBuildManager != null)
            {
                smithBuildManager.CloseSmithMenu();
            }
            else
            {
                smithCanvas.SetActive(false);
                Debug.LogWarning("SmithBuildManager not assigned! Changes may not be reverted properly.");
            }
            if (PauseManager.Instance != null)
            {
                PauseManager.Instance.ResumeGame();
            }
        }
    }
    private bool IsSmithMenuOpen()
    {
        return smithCanvas != null && smithCanvas.activeSelf;
    }
    private void Start()
    {
        if (eIcon != null)
        {
            CompensateParentScale();
        }
        if (smithCanvas != null)
        {
            smithCanvas.SetActive(false);
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
            Vector3 worldPosition = eIcon.transform.position;
            worldPosition.y = 8f;
            eIcon.transform.position = worldPosition;
        }
    }
}