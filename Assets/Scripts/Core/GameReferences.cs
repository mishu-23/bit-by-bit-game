using UnityEngine;
namespace BitByBit.Core
{
    public class GameReferences : MonoBehaviour
    {
        public static GameReferences Instance { get; private set; }
        [Header("UI References")]
        [SerializeField] private GameObject smithCanvas;
        [SerializeField] private Transform inventoryContent;
        [SerializeField] private Transform buildGridPanel;
        [Header("Player References")]
        [SerializeField] private Transform player;
        [SerializeField] private GameObject deposit;
        [Header("Gathering Points")]
        [SerializeField] private GameObject mine;
        [SerializeField] private GameObject tree;
        [Header("Camera References")]
        [SerializeField] private Camera mainCamera;
        [Header("Component References")]
        [SerializeField] private PowerBitPlayerController playerController;
        [SerializeField] private SmithBuildManager smithBuildManager;
        [SerializeField] private PowerBitCharacterRenderer characterRenderer;
        [SerializeField] private DepositInteraction depositInteraction;
        [Header("Auto-Find Settings")]
        [SerializeField] private bool autoFindReferences = true;
        [SerializeField] private bool showDebugInfo = true;
        public GameObject SmithCanvas => smithCanvas;
        public Transform InventoryContent => inventoryContent;
        public Transform BuildGridPanel => buildGridPanel;
        public Transform Player => player;
        public GameObject Deposit => deposit;
        public GameObject Mine => mine;
        public GameObject Tree => tree;
        public Camera MainCamera => mainCamera;
        public PowerBitPlayerController PlayerController => playerController;
        public SmithBuildManager SmithBuildManager => smithBuildManager;
        public PowerBitCharacterRenderer CharacterRenderer => characterRenderer;
        public DepositInteraction DepositInteraction => depositInteraction;
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                if (autoFindReferences)
                {
                    FindMissingReferences();
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
        private void Start()
        {
            ValidateReferences();
        }
        private void FindMissingReferences()
        {
            if (showDebugInfo)
            {
                Debug.Log("GameReferences: Auto-finding missing references...");
            }
            if (smithCanvas == null)
            {
                smithCanvas = GameObject.Find("SmithCanvas");
                if (smithCanvas != null && showDebugInfo)
                {
                    Debug.Log($"GameReferences: Found SmithCanvas - {smithCanvas.name}");
                }
            }
            if (inventoryContent == null)
            {
                GameObject inventoryObj = GameObject.Find("InventoryContent");
                if (inventoryObj != null)
                {
                    inventoryContent = inventoryObj.transform;
                    if (showDebugInfo)
                    {
                        Debug.Log($"GameReferences: Found InventoryContent - {inventoryContent.name}");
                    }
                }
                else
                {
                    SmithInventoryTestPopulator populator = FindObjectOfType<SmithInventoryTestPopulator>();
                    if (populator != null && populator.inventoryContent != null)
                    {
                        inventoryContent = populator.inventoryContent;
                        if (showDebugInfo)
                        {
                            Debug.Log($"GameReferences: Found InventoryContent via SmithInventoryTestPopulator - {inventoryContent.name}");
                        }
                    }
                }
            }
            if (buildGridPanel == null)
            {
                GameObject gridObj = GameObject.Find("BuildGridPanel");
                if (gridObj != null)
                {
                    buildGridPanel = gridObj.transform;
                    if (showDebugInfo)
                    {
                        Debug.Log($"GameReferences: Found BuildGridPanel - {buildGridPanel.name}");
                    }
                }
            }
            if (player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    player = playerObj.transform;
                    if (showDebugInfo)
                    {
                        Debug.Log($"GameReferences: Found Player - {player.name}");
                    }
                }
            }
            if (deposit == null)
            {
                deposit = GameObject.Find("Deposit");
                if (deposit != null && showDebugInfo)
                {
                    Debug.Log($"GameReferences: Found Deposit - {deposit.name}");
                }
            }
            if (mine == null)
            {
                mine = GameObject.Find("Mine");
                if (mine != null && showDebugInfo)
                {
                    Debug.Log($"GameReferences: Found Mine - {mine.name}");
                }
            }
            if (tree == null)
            {
                tree = GameObject.Find("Tree");
                if (tree != null && showDebugInfo)
                {
                    Debug.Log($"GameReferences: Found Tree - {tree.name}");
                }
            }
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    mainCamera = FindObjectOfType<Camera>();
                }
                if (mainCamera != null && showDebugInfo)
                {
                    Debug.Log($"GameReferences: Found MainCamera - {mainCamera.name}");
                }
            }
            FindComponentReferences();
        }
        private void FindComponentReferences()
        {
            if (playerController == null)
            {
                playerController = FindObjectOfType<PowerBitPlayerController>();
                if (playerController != null && showDebugInfo)
                {
                    Debug.Log($"GameReferences: Found PowerBitPlayerController - {playerController.name}");
                }
            }
            if (smithBuildManager == null)
            {
                smithBuildManager = FindObjectOfType<SmithBuildManager>();
                if (smithBuildManager != null && showDebugInfo)
                {
                    Debug.Log($"GameReferences: Found SmithBuildManager - {smithBuildManager.name}");
                }
            }
            if (characterRenderer == null)
            {
                characterRenderer = FindObjectOfType<PowerBitCharacterRenderer>();
                if (characterRenderer != null && showDebugInfo)
                {
                    Debug.Log($"GameReferences: Found PowerBitCharacterRenderer - {characterRenderer.name}");
                }
            }
            if (depositInteraction == null)
            {
                depositInteraction = FindObjectOfType<DepositInteraction>();
                if (depositInteraction != null && showDebugInfo)
                {
                    Debug.Log($"GameReferences: Found DepositInteraction - {depositInteraction.name}");
                }
            }
        }
        private void ValidateReferences()
        {
            bool hasErrors = false;
            if (smithCanvas == null)
            {
                Debug.LogWarning("GameReferences: SmithCanvas reference is not assigned!");
                hasErrors = true;
            }
            if (inventoryContent == null)
            {
                Debug.LogWarning("GameReferences: InventoryContent reference is not assigned!");
                hasErrors = true;
            }
            if (player == null)
            {
                Debug.LogWarning("GameReferences: Player reference is not assigned!");
                hasErrors = true;
            }
            if (mainCamera == null)
            {
                Debug.LogWarning("GameReferences: MainCamera reference is not assigned!");
                hasErrors = true;
            }
            if (playerController == null)
            {
                Debug.LogWarning("GameReferences: PowerBitPlayerController reference is not assigned!");
            }
            if (smithBuildManager == null)
            {
                Debug.LogWarning("GameReferences: SmithBuildManager reference is not assigned!");
            }
            if (characterRenderer == null)
            {
                Debug.LogWarning("GameReferences: PowerBitCharacterRenderer reference is not assigned!");
            }
            if (depositInteraction == null)
            {
                Debug.LogWarning("GameReferences: DepositInteraction reference is not assigned!");
            }
            if (hasErrors)
            {
                Debug.LogWarning("GameReferences: Some references are missing. Please assign them in the inspector or ensure auto-find is enabled.");
            }
            else if (showDebugInfo)
            {
                Debug.Log("GameReferences: All references validated successfully!");
            }
        }
        [ContextMenu("Refresh References")]
        public void RefreshReferences()
        {
            FindMissingReferences();
            ValidateReferences();
        }
        public T GetReference<T>(T cachedReference, System.Func<T> findFunction = null) where T : class
        {
            if (cachedReference != null)
            {
                return cachedReference;
            }
            if (findFunction != null && autoFindReferences)
            {
                T found = findFunction();
                if (found != null && showDebugInfo)
                {
                    Debug.Log($"GameReferences: Auto-found reference of type {typeof(T).Name}");
                }
                return found;
            }
            return null;
        }
        public bool IsSmithCanvasActive()
        {
            return smithCanvas != null && smithCanvas.activeInHierarchy;
        }
        public void SetSmithCanvasActive(bool active)
        {
            if (smithCanvas != null)
            {
                smithCanvas.SetActive(active);
            }
            else if (showDebugInfo)
            {
                Debug.LogWarning("GameReferences: Cannot set SmithCanvas active - reference is null!");
            }
        }
    }
}