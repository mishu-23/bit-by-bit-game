using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
namespace BitByBit.UI
{
    public class MainMenuOverlayManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button quitButton;
        
        [Header("Scene Management")]
        [SerializeField] private string gameSceneName = "MainScene";
        
        private bool isInMainMenuScene = false;
        
        private void Start()
        {
            // Check if we're in the main menu scene
            isInMainMenuScene = SceneManager.GetActiveScene().name == "MainMenu";
            
            InitializeButtons();
            CheckForSaveData();
        }
        
        private void InitializeButtons()
        {
            if (newGameButton != null)
            {
                newGameButton.onClick.AddListener(OnNewGameClicked);
            }
            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(OnResumeClicked);
            }
            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitClicked);
            }
        }
        
        private void CheckForSaveData()
        {
            bool hasSaveData = HasExistingSaveData();
            if (resumeButton != null)
            {
                resumeButton.interactable = hasSaveData;
                if (!hasSaveData)
                {
                    var colors = resumeButton.colors;
                    colors.normalColor = Color.gray;
                    colors.highlightedColor = Color.gray;
                    resumeButton.colors = colors;
                }
            }
            Debug.Log($"Overlay - Save data found: {hasSaveData}");
        }
        
        private bool HasExistingSaveData()
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
                string filePath = Path.Combine(basePath, fileName);
                if (File.Exists(filePath))
                {
                    Debug.Log($"Found save file: {fileName}");
                    return true;
                }
            }
            return false;
        }
        
        private void OnNewGameClicked()
        {
            Debug.Log("New Game clicked from overlay");
            
            if (isInMainMenuScene)
            {
                // We're in the main menu scene, start a new game
                Debug.Log("Starting new game from main menu - clearing all save data");
                ClearAllSaveData();
                LoadGameScene();
            }
            else
            {
                // We're in the game scene as an overlay, trigger new game through PauseManager
                if (PauseManager.Instance != null)
                {
                    PauseManager.Instance.TriggerNewGameFromOverlay();
                }
            }
        }
        
        private void OnResumeClicked()
        {
            Debug.Log("Resume clicked from overlay");
            
            if (isInMainMenuScene)
            {
                // We're in the main menu scene, load the game scene
                if (!HasExistingSaveData())
                {
                    Debug.LogWarning("No save data found for resume!");
                    return;
                }
                Debug.Log("Resuming game from main menu with existing save data");
                LoadGameScene();
            }
            else
            {
                // We're in the game scene as an overlay, resume through PauseManager
                if (PauseManager.Instance != null)
                {
                    PauseManager.Instance.ResumeGame();
                }
            }
        }
        
        private void OnQuitClicked()
        {
            Debug.Log("Quit clicked from overlay");
            Application.Quit();
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
        
        private void LoadGameScene()
        {
            Debug.Log($"Loading game scene: {gameSceneName}");
            SceneManager.LoadScene(gameSceneName);
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
                string filePath = Path.Combine(basePath, fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log($"Deleted save file: {fileName}");
                }
            }
        }
        
        private void OnDestroy()
        {
            if (newGameButton != null)
            {
                newGameButton.onClick.RemoveListener(OnNewGameClicked);
            }
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(OnResumeClicked);
            }
            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(OnQuitClicked);
            }
        }
    }
}