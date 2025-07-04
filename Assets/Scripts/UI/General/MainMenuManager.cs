using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEngine.UI;
using BitByBit.Items;
namespace BitByBit.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button quitButton;
        [Header("Scene Management")]
        [SerializeField] private string gameSceneName = "MainScene";
        private void Start()
        {
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
            Debug.Log($"Save data found: {hasSaveData}");
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
            Debug.Log("Starting new game - clearing all save data");
            ClearAllSaveData();
            LoadGameScene();
        }
        private void OnResumeClicked()
        {
            if (!HasExistingSaveData())
            {
                Debug.LogWarning("No save data found for resume!");
                return;
            }
            Debug.Log("Resuming game with existing save data");
            LoadGameScene();
        }
        private void OnQuitClicked()
        {
            Debug.Log("Quitting game");
            Application.Quit();
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
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
                    try
                    {
                        File.Delete(filePath);
                        Debug.Log($"Deleted save file: {fileName}");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Failed to delete {fileName}: {e.Message}");
                    }
                }
            }
            ClearGameCaches();
        }
        private void ClearGameCaches()
        {
            if (BitCollectionManager.Instance != null)
            {
                BitCollectionManager.Instance.InvalidateCache();
                Debug.Log("BitCollectionManager cache cleared");
            }
        }
        private void LoadGameScene()
        {
            Debug.Log($"Loading game scene: {gameSceneName}");
            SceneManager.LoadScene(gameSceneName);
        }
        public void NewGame()
        {
            OnNewGameClicked();
        }
        public void Resume()
        {
            OnResumeClicked();
        }
        public void QuitGame()
        {
            OnQuitClicked();
        }
    }
}