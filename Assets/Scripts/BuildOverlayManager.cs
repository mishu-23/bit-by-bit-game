using UnityEngine;

public class BuildOverlayManager : MonoBehaviour
{
    [Header("Assign your BuildOverlay prefab here")]
    public GameObject buildOverlay;

    public void ShowOverlay()
    {
        if (buildOverlay != null)
            buildOverlay.SetActive(true);
    }

    public void HideOverlay()
    {
        if (buildOverlay != null)
            buildOverlay.SetActive(false);
    }

    // For UI button hookup
    public void CloseOverlay()
    {
        HideOverlay();
    }
}