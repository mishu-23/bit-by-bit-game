using UnityEngine;
using UnityEngine.Events;

namespace BitByBit.Items
{
    [System.Serializable]
    public class BitDropSettings
    {
        [Header("Collection")]
        public float collectionRange = 2f;
        public float collectionSpeed = 5f;
        public float collectionDistance = 0.5f;
        
        [Header("Input")]
        public KeyCode collectionKey = KeyCode.F;
        
        [Header("Performance")]
        public float distanceCheckInterval = 0.1f;
    }

    [System.Serializable]
    public class BitDropEvents
    {
        public UnityEvent<BitDrop> OnPlayerEnterRange;
        public UnityEvent<BitDrop> OnPlayerExitRange;
        public UnityEvent<BitDrop> OnCollectionStarted;
        public UnityEvent<BitDrop, bool> OnCollectionCompleted;
    }

    public class BitDrop : MonoBehaviour
    {
        #region Configuration
        
        [Header("Bit Data")]
        [SerializeField] private Bit bitData;
        
        [Header("Settings")]
        [SerializeField] private BitDropSettings settings = new BitDropSettings();
        
        [Header("UI References")]
        [SerializeField] private GameObject collectionPrompt;
        
        [Header("Events")]
        [SerializeField] private BitDropEvents events = new BitDropEvents();
        
        #endregion
        
        #region Private Fields
        
        private SpriteRenderer spriteRenderer;
        private Transform playerTransform;
        private float lastDistanceCheck;
        private bool playerInRange;
        private bool isBeingCollected;
        
        // Static prefab management
        private static GameObject prefabReference;
        
        #endregion
        
        #region Properties
        
        public Bit BitData => bitData;
        public bool PlayerInRange => playerInRange;
        public BitDropSettings Settings => settings;
        
        public static GameObject PrefabReference 
        { 
            get => prefabReference; 
            set => prefabReference = value; 
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeComponents();
            ValidateConfiguration();
        }
        
        private void Start()
        {
            InitializePlayerReference();
            UpdateVisualRepresentation();
        }
        
        private void OnEnable()
        {
            // Reinitialize when component is re-enabled (e.g., after being dropped by entity)
            if (gameObject.scene.isLoaded)
            {
                InitializePlayerReference();
                UpdateVisualRepresentation();
                
                // Reset collection state
                isBeingCollected = false;
                playerInRange = false;
                ShowCollectionPrompt(false);
                
                Debug.Log($"BitDrop '{name}' - Component re-enabled, reinitialized with bit: {bitData?.BitName ?? "NULL"}");
            }
        }
        
        private void Update()
        {
            if (!isBeingCollected)
            {
                CheckPlayerDistance();
                HandleInput();
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeComponents()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            if (spriteRenderer == null)
            {
                Debug.LogError($"BitDrop '{name}' missing SpriteRenderer component!", this);
            }
            
            // Try to find collection prompt if not assigned
            if (collectionPrompt == null)
            {
                Transform fIconTransform = transform.Find("F_Icon");
                
                if (fIconTransform != null)
                {
                    collectionPrompt = fIconTransform.gameObject;
                    Debug.Log($"BitDrop '{name}' found collection prompt: {fIconTransform.name}");
                }
                else
                {
                    Debug.LogWarning($"BitDrop '{name}' could not find collection prompt (F_Icon). Collection prompts will not be shown.");
                }
            }
        }
        
        private void ValidateConfiguration()
        {
            // Only validate if the object is fully loaded and not during instantiation
            // Skip validation if we're likely in the middle of setup (component just instantiated)
            if (bitData == null && gameObject.scene.isLoaded && Time.time > 0f && Time.timeSinceLevelLoad > 0.1f)
            {
                Debug.LogWarning($"BitDrop '{name}' has no bit data assigned!", this);
            }
            
            if (settings.collectionRange <= 0f)
            {
                Debug.LogWarning($"BitDrop '{name}' has invalid collection range!", this);
                settings.collectionRange = 2f;
            }
            
            if (settings.collectionSpeed <= 0f)
            {
                Debug.LogWarning($"BitDrop '{name}' has invalid collection speed!", this);
                settings.collectionSpeed = 5f;
            }
        }
        
        private void InitializePlayerReference()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogWarning($"BitDrop '{name}' could not find player object!", this);
            }
        }
        
        #endregion
        
        #region Collection Logic
        
        private void CheckPlayerDistance()
        {
            if (playerTransform == null || Time.time - lastDistanceCheck < settings.distanceCheckInterval)
                return;
            
            lastDistanceCheck = Time.time;
            
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            bool shouldBeInRange = distance <= settings.collectionRange;
            
            if (shouldBeInRange && !playerInRange)
            {
                playerInRange = true;
                ShowCollectionPrompt(true);
                events.OnPlayerEnterRange?.Invoke(this);
            }
            else if (!shouldBeInRange && playerInRange)
            {
                playerInRange = false;
                ShowCollectionPrompt(false);
                events.OnPlayerExitRange?.Invoke(this);
            }
        }
        
        private void HandleInput()
        {
            if (playerInRange && 
                Input.GetKeyDown(settings.collectionKey) && 
                !IsPaused())
            {
                Debug.Log($"BitDrop '{name}' - {settings.collectionKey} key pressed, starting collection!");
                StartCollection();
            }
        }
        
        private bool IsPaused()
        {
            return PauseManager.Instance != null && PauseManager.Instance.IsPaused;
        }
        
        private void StartCollection()
        {
            isBeingCollected = true;
            ShowCollectionPrompt(false);
            events.OnCollectionStarted?.Invoke(this);
        }
        
        private void FixedUpdate()
        {
            if (isBeingCollected && playerTransform != null)
            {
                HandleCollection();
            }
        }
        
        private void HandleCollection()
        {
            if (playerTransform == null)
            {
                Debug.LogWarning($"BitDrop '{name}' lost player reference during collection!", this);
                isBeingCollected = false;
                return;
            }
            
            // Move towards player
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            transform.position += direction * settings.collectionSpeed * Time.fixedDeltaTime;
            
            // Check if close enough to collect
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance <= settings.collectionDistance)
            {
                AttemptCollection();
            }
        }
        
        private void AttemptCollection()
        {
            Debug.Log($"BitDrop '{name}' - Attempting to collect bit: {bitData?.BitName ?? "NULL"}");
            
            if (BitCollectionManager.Instance == null)
            {
                Debug.LogError($"BitDrop '{name}' cannot find BitCollectionManager!", this);
                HandleCollectionFailure();
                return;
            }
            
            Debug.Log($"BitDrop '{name}' - Found BitCollectionManager, calling CollectBit()");
            bool success = BitCollectionManager.Instance.CollectBit(bitData);
            
            Debug.Log($"BitDrop '{name}' - Collection result: {(success ? "SUCCESS" : "FAILED")}");
            
            if (success)
            {
                HandleCollectionSuccess();
            }
            else
            {
                HandleCollectionFailure();
            }
            
            events.OnCollectionCompleted?.Invoke(this, success);
        }
        
        private void HandleCollectionSuccess()
        {
            Debug.Log($"BitDrop '{name}' - Successfully collected!");
            Destroy(gameObject);
        }
        
        private void HandleCollectionFailure()
        {
            Debug.Log($"BitDrop '{name}' - Collection failed, returning to idle state");
            isBeingCollected = false;
        }
        
        #endregion
        
        #region Visual Management
        
        private void UpdateVisualRepresentation()
        {
            if (bitData != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = bitData.GetSprite();
            }
        }
        
        private void ShowCollectionPrompt(bool show)
        {
            if (collectionPrompt != null)
            {
                collectionPrompt.SetActive(show);
            }
        }
        
        #endregion
        
        #region Public Interface
        
        public void SetBitData(Bit bit)
        {
            if (bit == null)
            {
                Debug.LogWarning($"BitDrop '{name}' - Attempted to set null bit data!", this);
                return;
            }
            
            bitData = bit;
            UpdateVisualRepresentation();
            Debug.Log($"BitDrop '{name}' - Bit data set to: {bit.BitName}");
        }
        
        public void ForceCollection()
        {
            if (!isBeingCollected)
            {
                StartCollection();
            }
        }
        
        public float GetDistanceToPlayer()
        {
            if (playerTransform == null) return float.MaxValue;
            return Vector3.Distance(transform.position, playerTransform.position);
        }
        
        #endregion
        
        #region Factory Methods
        
        public static BitDrop CreateBitDrop(Bit bit, Vector3 position)
        {
            return CreateBitDrop(bit, position, Quaternion.identity);
        }
        
        public static BitDrop CreateBitDrop(Bit bit, Vector3 position, Quaternion rotation)
        {
            if (prefabReference == null)
            {
                Debug.LogError("BitDrop prefab reference not set! Cannot create BitDrop.");
                return null;
            }
            
            if (bit == null)
            {
                Debug.LogError("Cannot create BitDrop with null bit data!");
                return null;
            }
            
            Debug.Log($"Creating BitDrop for bit: {bit.BitName} ({bit.BitType}, {bit.Rarity})");
            
            GameObject instance = Instantiate(prefabReference, position, rotation);
            BitDrop bitDropComponent = instance.GetComponent<BitDrop>();
            
            if (bitDropComponent == null)
            {
                Debug.LogError("BitDrop prefab does not have BitDrop component!");
                Destroy(instance);
                return null;
            }
            
            // Set bit data immediately to avoid validation warnings
            bitDropComponent.SetBitData(bit);
            
            Debug.Log($"BitDrop created successfully with bit: {bit.BitName}");
            return bitDropComponent;
        }
        
        #endregion
    }
} 