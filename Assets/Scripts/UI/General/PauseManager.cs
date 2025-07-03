using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    
    [Header("Pause UI References")]
    public GameObject pauseMenuCanvas;
    public Button resumeButton;
    public Button settingsButton;
    public Button quitButton;
    
    [Header("Main Menu Overlay")]
    public GameObject mainMenuCanvasPrefab; // Assign the MainMenuCanvas prefab here
    private GameObject activeMainMenuOverlay;
    
    [Header("Pause Settings")]
    public KeyCode[] pauseKeys = { KeyCode.Escape, KeyCode.P };
    public bool muteAudioOnPause = true;
    public bool reduceMasterVolumeOnPause = false;
    [Range(0f, 1f)]
    public float pausedVolumeLevel = 0.1f;
    
    // Pause state
    private bool isPaused = false;
    private float originalVolumeLevel = 1f;
    private bool originalCursorVisible;
    private CursorLockMode originalCursorLockState;
    private PauseType currentPauseType = PauseType.None;
    
    // Events
    public static event Action OnGamePaused;
    public static event Action OnGameResumed;
    
    // Pause types for different sources
    public enum PauseType
    {
        None,
        Manual,        // ESC/P key pause - now shows main menu overlay
        SmithBuilder,  // When in smith builder
        Menu,          // Other menus
        Dialog         // Dialog systems
    }
    
    public bool IsPaused => isPaused;
    public PauseType CurrentPauseType => currentPauseType;
    
    private void Awake()
    {
        // Singleton pattern
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
        
        // Store original audio settings
        originalVolumeLevel = AudioListener.volume;
    }
    
    private void Start()
    {
        // Setup UI buttons
        SetupPauseUI();
        
        // Store original cursor state
        originalCursorVisible = Cursor.visible;
        originalCursorLockState = Cursor.lockState;
        
        // Hide pause menu initially
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(false);
        }
    }
    
    private void Update()
    {
        // Handle manual pause input (ESC/P keys) - only if not already paused by another source
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
        
        // Handle audio
        if (muteAudioOnPause)
        {
            AudioListener.volume = 0f;
        }
        else if (reduceMasterVolumeOnPause)
        {
            AudioListener.volume = pausedVolumeLevel;
        }
        
        // Show cursor and unlock it for menu interaction
        if (pauseType == PauseType.Manual)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            // Show main menu overlay for manual pause (ESC key)
            ShowMainMenuOverlay();
        }
        else if (pauseType == PauseType.SmithBuilder)
        {
            // For smith builder, we want cursor visible but show traditional pause menu
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            // Show the traditional pause menu for smith builder
            if (pauseMenuCanvas != null)
            {
                pauseMenuCanvas.SetActive(true);
            }
        }
        
        // Clear any input states to prevent queuing
        ClearInputStates();
        
        // Trigger event
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
        
        // Restore audio
        AudioListener.volume = originalVolumeLevel;
        
        // Restore cursor state (only for manual pause or smith builder)
        if (previousPauseType == PauseType.Manual || previousPauseType == PauseType.SmithBuilder)
        {
            Cursor.visible = originalCursorVisible;
            Cursor.lockState = originalCursorLockState;
        }
        
        // Hide the appropriate menu based on pause type
        if (previousPauseType == PauseType.Manual)
        {
            // Hide main menu overlay for manual pause
            HideMainMenuOverlay();
        }
        else if (previousPauseType == PauseType.SmithBuilder || pauseMenuCanvas != null)
        {
            // Hide traditional pause menu for smith builder or other pause types
            if (pauseMenuCanvas != null)
            {
                pauseMenuCanvas.SetActive(false);
            }
        }
        
        // Clear any input states to prevent queuing
        ClearInputStates();
        
        // Trigger event
        OnGameResumed?.Invoke();
        
        Debug.Log($"Game resumed from pause type: {previousPauseType}");
    }
    
    private void ShowMainMenuOverlay()
    {
        if (mainMenuCanvasPrefab != null && activeMainMenuOverlay == null)
        {
            // Instantiate the main menu canvas as an overlay
            activeMainMenuOverlay = Instantiate(mainMenuCanvasPrefab);
            
            // Make sure it renders on top of everything
            Canvas overlayCanvas = activeMainMenuOverlay.GetComponent<Canvas>();
            if (overlayCanvas != null)
            {
                overlayCanvas.sortingOrder = 1000; // High sorting order to appear on top
                overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            
            // Setup the main menu overlay buttons to work with pause system
            SetupMainMenuOverlayButtons();
            
            Debug.Log("Main menu overlay shown");
        }
        else if (mainMenuCanvasPrefab == null)
        {
            Debug.LogWarning("Main Menu Canvas Prefab not assigned! Falling back to regular pause menu.");
            // Fallback to regular pause menu
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
        
        // Check if we have the new MainMenuOverlayManager
        var overlayManager = activeMainMenuOverlay.GetComponentInChildren<BitByBit.UI.MainMenuOverlayManager>();
        if (overlayManager != null)
        {
            Debug.Log("MainMenuOverlayManager found - buttons should be automatically configured");
            return; // The overlay manager handles its own buttons
        }
        
        // Fallback: Find the regular MainMenuManager and override it
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
        // Find buttons by name in the overlay
        Button newGameButton = FindButtonByName("NewGameButton");
        Button resumeButton = FindButtonByName("ResumeButton");
        Button quitButton = FindButtonByName("QuitButton");
        
        // Clear existing listeners and add new ones
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
            // Try to find it recursively
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
        
        // Clear all save data (similar to MainMenuManager)
        ClearAllSaveData();
        
        // Hide the overlay
        HideMainMenuOverlay();
        
        // Resume game time for scene transition
        Time.timeScale = 1f;
        
        // Restart the current scene (or load main scene)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
        // This method helps prevent input from being "stuck" or queued during pause/unpause
        // Override in derived classes if you need to clear specific input states
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
        // TODO: Implement settings menu
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
        // Clean up events
        OnGamePaused = null;
        OnGameResumed = null;
        
        // Clean up main menu overlay
        if (activeMainMenuOverlay != null)
        {
            Destroy(activeMainMenuOverlay);
        }
        
        // Clean up button listeners
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
} 