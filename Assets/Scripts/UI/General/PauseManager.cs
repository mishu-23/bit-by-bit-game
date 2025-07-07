using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    [Header("Pause UI References")]
    public GameObject pauseMenuCanvas;
    public Button resumeButton;
    public Button settingsButton;
    public Button quitButton;
    [Header("Main Menu Overlay")]
    public GameObject mainMenuCanvasPrefab; 
    private GameObject activeMainMenuOverlay;
    [Header("Pause Settings")]
    public KeyCode[] pauseKeys = { KeyCode.Escape, KeyCode.P };
    public bool muteAudioOnPause = true;
    public bool reduceMasterVolumeOnPause = false;
    [Range(0f, 1f)]
    public float pausedVolumeLevel = 0.1f;
    private bool isPaused = false;
    private float originalVolumeLevel = 1f;
    private bool originalCursorVisible;
    private CursorLockMode originalCursorLockState;
    private PauseType currentPauseType = PauseType.None;
    public static event Action OnGamePaused;
    public static event Action OnGameResumed;
    public enum PauseType
    {
        None,
        Manual,        
        SmithBuilder,  
        Menu,          
        Dialog         
    }
    public bool IsPaused => isPaused;
    public PauseType CurrentPauseType => currentPauseType;

    public bool IsSmithBuilderActive()
    {
        return isPaused && currentPauseType == PauseType.SmithBuilder;
    }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        originalVolumeLevel = AudioListener.volume;
    }
    private void Start()
    {
        SetupPauseUI();
        originalCursorVisible = Cursor.visible;
        originalCursorLockState = Cursor.lockState;
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(false);
        }
        
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void Update()
    {
        if (currentPauseType == PauseType.None || currentPauseType == PauseType.Manual)
        {
            foreach (KeyCode key in pauseKeys)
            {
                if (Input.GetKeyDown(key))
                {
                    if (!isPaused)
                    {
                        PauseGame(PauseType.Manual);
                    }
                    else if (currentPauseType == PauseType.Manual)
                    {
                        ResumeGame();
                    }
                    break;
                }
            }
        }
    }
    public void PauseGame(PauseType pauseType = PauseType.Manual)
    {
        if (isPaused)
        {
            Debug.LogWarning($"Game is already paused with type: {currentPauseType}. Cannot pause with type: {pauseType}");
            return;
        }
        isPaused = true;
        currentPauseType = pauseType;
        Time.timeScale = 0f;
        if (muteAudioOnPause)
        {
            AudioListener.volume = 0f;
        }
        else if (reduceMasterVolumeOnPause)
        {
            AudioListener.volume = pausedVolumeLevel;
        }
        if (pauseType == PauseType.Manual)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            ShowMainMenuOverlay();
        }
        else if (pauseType == PauseType.SmithBuilder)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (pauseMenuCanvas != null)
            {
                pauseMenuCanvas.SetActive(true);
            }
        }
        ClearInputStates();
        OnGamePaused?.Invoke();
        Debug.Log($"Game paused with type: {pauseType}");
    }
    public void ResumeGame()
    {
        if (!isPaused)
        {
            Debug.LogWarning("Game is not paused. Cannot resume.");
            return;
        }
        PauseType previousPauseType = currentPauseType;
        isPaused = false;
        currentPauseType = PauseType.None;
        Time.timeScale = 1f;
        AudioListener.volume = originalVolumeLevel;
        if (previousPauseType == PauseType.Manual || previousPauseType == PauseType.SmithBuilder)
        {
            Cursor.visible = originalCursorVisible;
            Cursor.lockState = originalCursorLockState;
        }
        if (previousPauseType == PauseType.Manual)
        {
            HideMainMenuOverlay();
        }
        else if (previousPauseType == PauseType.SmithBuilder || pauseMenuCanvas != null)
        {
            if (pauseMenuCanvas != null)
            {
                pauseMenuCanvas.SetActive(false);
            }
        }
        ClearInputStates();
        OnGameResumed?.Invoke();
        Debug.Log($"Game resumed from pause type: {previousPauseType}");
    }
    private void ShowMainMenuOverlay()
    {
        if (mainMenuCanvasPrefab != null && activeMainMenuOverlay == null)
        {
            activeMainMenuOverlay = Instantiate(mainMenuCanvasPrefab);
            Canvas overlayCanvas = activeMainMenuOverlay.GetComponent<Canvas>();
            if (overlayCanvas != null)
            {
                overlayCanvas.sortingOrder = 1000; 
                overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            SetupMainMenuOverlayButtons();
            Debug.Log("Main menu overlay shown");
        }
        else if (mainMenuCanvasPrefab == null)
        {
            Debug.LogWarning("Main Menu Canvas Prefab not assigned! Falling back to regular pause menu.");
            if (pauseMenuCanvas != null)
            {
                pauseMenuCanvas.SetActive(true);
            }
        }
    }
    private void HideMainMenuOverlay()
    {
        if (activeMainMenuOverlay != null)
        {
            Destroy(activeMainMenuOverlay);
            activeMainMenuOverlay = null;
            Debug.Log("Main menu overlay hidden");
        }
    }
    private void SetupMainMenuOverlayButtons()
    {
        if (activeMainMenuOverlay == null) return;
        var overlayManager = activeMainMenuOverlay.GetComponentInChildren<BitByBit.UI.MainMenuOverlayManager>();
        if (overlayManager != null)
        {
            Debug.Log("MainMenuOverlayManager found - buttons should be automatically configured");
            return; 
        }
        var mainMenuManager = activeMainMenuOverlay.GetComponentInChildren<BitByBit.UI.MainMenuManager>();
        if (mainMenuManager != null)
        {
            Debug.Log("Regular MainMenuManager found - overriding button functionality");
            SetupOverlayButtonsManually();
        }
        else
        {
            Debug.LogWarning("No menu manager found in overlay! Trying to find buttons manually.");
            SetupOverlayButtonsManually();
        }
    }
    private void SetupOverlayButtonsManually()
    {
        Button newGameButton = FindButtonByName("NewGameButton");
        Button resumeButton = FindButtonByName("ResumeButton");
        Button quitButton = FindButtonByName("QuitButton");
        if (newGameButton != null)
        {
            newGameButton.onClick.RemoveAllListeners();
            newGameButton.onClick.AddListener(OnNewGameFromOverlay);
            Debug.Log("New Game button configured for overlay");
        }
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(OnResumeFromOverlay);
            Debug.Log("Resume button configured for overlay");
        }
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
            Debug.Log("Quit button configured for overlay");
        }
    }
    private Button FindButtonByName(string buttonName)
    {
        if (activeMainMenuOverlay == null) return null;
        Transform buttonTransform = activeMainMenuOverlay.transform.Find(buttonName);
        if (buttonTransform == null)
        {
            buttonTransform = FindChildRecursive(activeMainMenuOverlay.transform, buttonName);
        }
        return buttonTransform?.GetComponent<Button>();
    }
    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            Transform found = FindChildRecursive(child, name);
            if (found != null)
                return found;
        }
        return null;
    }
    private void OnNewGameFromOverlay()
    {
        Debug.Log("New Game clicked from overlay - clearing save data and restarting");
        ClearAllSaveData();
        HideMainMenuOverlay();
        
        ResetPauseState();
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    private void ResetPauseState()
    {
        Debug.Log("Resetting PauseManager state for new game");
        
        isPaused = false;
        currentPauseType = PauseType.None;
        Time.timeScale = 1f;
        AudioListener.volume = originalVolumeLevel;
        
        Cursor.visible = originalCursorVisible;
        Cursor.lockState = originalCursorLockState;
        
        if (activeMainMenuOverlay != null)
        {
            Destroy(activeMainMenuOverlay);
            activeMainMenuOverlay = null;
        }
        
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(false);
        }
        
        ClearInputStates();
        
        Debug.Log("PauseManager state reset complete");
    }
    private void OnResumeFromOverlay()
    {
        Debug.Log("Resume clicked from overlay");
        ResumeGame();
    }
    public void TriggerNewGameFromOverlay()
    {
        OnNewGameFromOverlay();
    }
    private void ClearAllSaveData()
    {
        string basePath = Application.persistentDataPath;
        string[] saveFiles = {
            "smith_build.json",
            "smith_inventory.json", 
            "core_storage.json",
            "settlement_storage.json"
        };
        foreach (string fileName in saveFiles)
        {
            string filePath = System.IO.Path.Combine(basePath, fileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                Debug.Log($"Deleted save file: {fileName}");
            }
        }
    }
    public void ClearInputStates()
    {
        Input.ResetInputAxes();
    }
    private void SetupPauseUI()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(ResumeGame);
        }
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OpenSettings);
        }
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }
    }
    private void OpenSettings()
    {
        Debug.Log("Settings button clicked - Settings menu not implemented yet");
    }
    private void QuitGame()
    {
        Debug.Log("Quit button clicked - Quitting game");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        OnGamePaused = null;
        OnGameResumed = null;
        if (activeMainMenuOverlay != null)
        {
            Destroy(activeMainMenuOverlay);
        }
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeGame);
        }
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(OpenSettings);
        }
        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
        }
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}, mode: {mode}");
        
        if (mode == LoadSceneMode.Single)
        {
            StartCoroutine(ResetPauseStateOnSceneLoad());
        }
    }
    private System.Collections.IEnumerator ResetPauseStateOnSceneLoad()
    {
        yield return null;
        
        if (isPaused || currentPauseType != PauseType.None || Time.timeScale != 1f)
        {
            Debug.Log("Detected incorrect pause state after scene load - resetting");
            ResetPauseState();
        }
    }
}