using UnityEngine;
using UnityEngine.UI;
using System;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    
    [Header("Pause UI References")]
    public GameObject pauseMenuCanvas;
    public Button resumeButton;
    public Button settingsButton;
    public Button quitButton;
    
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
        Manual,        // ESC/P key pause
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
        
        // Show cursor and unlock it for menu interaction (only for manual pause)
        if (pauseType == PauseType.Manual)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            // Show pause menu
            if (pauseMenuCanvas != null)
            {
                pauseMenuCanvas.SetActive(true);
            }
        }
        else if (pauseType == PauseType.SmithBuilder)
        {
            // For smith builder, we want cursor visible but don't show the pause menu
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
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
        
        // Hide pause menu
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(false);
        }
        
        // Clear any input states to prevent queuing
        ClearInputStates();
        
        // Trigger event
        OnGameResumed?.Invoke();
        
        Debug.Log($"Game resumed from pause type: {previousPauseType}");
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