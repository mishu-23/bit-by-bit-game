using UnityEngine;
using UnityEngine.UI;
public class SmithButtonHandler : MonoBehaviour
{
    [Header("Button References")]
    public Button closeButton;
    public Button buildButton;
    [Header("Build Manager")]
    public SmithBuildManager buildManager;
    void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClick);
        }
        if (buildButton != null)
        {
            buildButton.onClick.AddListener(OnBuildButtonClick);
        }
    }
    void OnCloseButtonClick()
    {
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused && 
            PauseManager.Instance.CurrentPauseType != PauseManager.PauseType.SmithBuilder &&
            !IsSmithBuilderActive())
        {
            Debug.Log("Close button blocked - game is paused for non-SmithBuilder reason");
            return;
        }
        
        Debug.Log("Close button clicked - Closing Smith menu");
        if (buildManager != null)
        {
            buildManager.CloseSmithMenu(); 
        }
    }
    void OnBuildButtonClick()
    {
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused && 
            PauseManager.Instance.CurrentPauseType != PauseManager.PauseType.SmithBuilder &&
            !IsSmithBuilderActive())
        {
            Debug.Log("Build button blocked - game is paused for non-SmithBuilder reason");
            return;
        }
        
        Debug.Log("Build button clicked - Saving and applying build");
        if (buildManager != null)
        {
            buildManager.SaveBuild();
            buildManager.CloseSmithMenu(); 
        }
    }
    private bool IsSmithBuilderActive()
    {
        if (buildManager != null)
        {
            var canvas = buildManager.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.gameObject.activeInHierarchy)
            {
                return true;
            }
        }
        
        if (PauseManager.Instance != null && PauseManager.Instance.IsSmithBuilderActive())
        {
            return true;
        }
        
        return false;
    }
    void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseButtonClick);
        }
        if (buildButton != null)
        {
            buildButton.onClick.RemoveListener(OnBuildButtonClick);
        }
    }
}