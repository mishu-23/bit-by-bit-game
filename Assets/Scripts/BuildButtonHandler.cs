using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BuildButtonHandler : MonoBehaviour
{
    private Button buildButton;
    private BuildGridPanel gridPanel;

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
            // For now, just save and print the grid state locally
            gridPanel.SaveGridState();
            Debug.Log("Build button clicked - Grid state saved locally");
        }
    }

    private void OnDestroy()
    {
        if (buildButton != null)
        {
            buildButton.onClick.RemoveListener(OnBuildButtonClick);
        }
    }
} 