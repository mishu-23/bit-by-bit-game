using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BuildButtonHandler : MonoBehaviour
{
    private Button buildButton;
    private BuildGridPanel gridPanel;
    [Header("Overlay Manager")]
    // public BuildOverlayManager overlayManager; // Assign in Inspector - Class not found
    [Header("Player Controller")]
    public PlayerController playerController; // Assign in Inspector

    private void Awake()
    {
        buildButton = GetComponent<Button>();
        gridPanel = FindObjectOfType<BuildGridPanel>();
        
        if (gridPanel == null)
        {
            Debug.LogError("BuildGridPanel not found in scene!");
            return;
        }

        buildButton.onClick.AddListener(OnBuildButtonClick);
    }

    private void OnBuildButtonClick()
    {
        if (gridPanel != null)
        {
            gridPanel.SaveGridState();
            Debug.Log("Build button clicked - Grid state saved locally");
        }
        if (playerController != null)
        {
            playerController.LoadLastSavedCharacter();
        }
        // if (overlayManager != null)
        // {
        //     overlayManager.HideOverlay();
        // }
    }

    private void OnDestroy()
    {
        if (buildButton != null)
        {
            buildButton.onClick.RemoveListener(OnBuildButtonClick);
        }
    }
} 