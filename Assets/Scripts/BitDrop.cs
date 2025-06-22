using UnityEngine;

public class BitDrop : MonoBehaviour
{
    [Header("Bit Data")]
    public Bit bitData;
    
    [Header("Collection Settings")]
    public float collectionRange = 2f;
    public float collectionSpeed = 5f;
    public LayerMask groundLayer = 1; // Default layer
    
    [Header("Q Prompt")]
    public GameObject qIcon; // Assign the Q_Icon child here
    
    private Rigidbody2D rb;
    private bool isGrounded = false;
    private bool isBeingCollected = false;
    private bool playerInRange = false;
    private Transform playerTransform;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Set the sprite based on bit data
        if (bitData != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = bitData.GetSprite();
        }
        
        // Set up Q icon
        SetupQIcon();
    }
    
    private void SetupQIcon()
    {
        if (qIcon != null)
        {
            // Compensate for parent scale to keep Q_Icon consistent size
            CompensateParentScale();
            qIcon.SetActive(false); // Hide by default
        }
    }
    
    private void CompensateParentScale()
    {
        if (qIcon != null)
        {
            Vector3 parentScale = transform.localScale;
            Vector3 compensationScale = new Vector3(
                parentScale.x != 0 ? 1f / parentScale.x : 1f,
                parentScale.y != 0 ? 1f / parentScale.y : 1f,
                parentScale.z != 0 ? 1f / parentScale.z : 1f
            );
            qIcon.transform.localScale = compensationScale;
            
            // Set Q_Icon to always be at y = 8 in world space
            Vector3 worldPosition = qIcon.transform.position;
            worldPosition.y = 8f;
            qIcon.transform.position = worldPosition;
        }
    }
    
    private void Start()
    {
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }
    
    private void Update()
    {
        if (isBeingCollected)
        {
            HandleCollection();
        }
        else
        {
            // Check distance to player for Q prompt
            CheckPlayerDistance();
        }
        
        // Handle Q key press for collection
        if (playerInRange && Input.GetKeyDown(KeyCode.Q))
        {
            StartCollection();
        }
    }
    
    private void HandleCollection()
    {
        if (playerTransform == null) return;
        
        // Move towards player
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        transform.position += direction * collectionSpeed * Time.deltaTime;
        
        // Destroy when very close to player
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (distance < 0.5f)
        {
            CollectBit();
        }
    }
    
    private void CheckPlayerDistance()
    {
        if (playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance <= collectionRange)
            {
                // Show Q prompt when player is in range
                if (!playerInRange)
                {
                    playerInRange = true;
                    if (qIcon != null)
                        qIcon.SetActive(true);
                    Debug.Log($"Player entered {bitData.bitName} collection range");
                }
            }
            else
            {
                // Hide Q prompt when player is out of range
                if (playerInRange)
                {
                    playerInRange = false;
                    if (qIcon != null)
                        qIcon.SetActive(false);
                    Debug.Log($"Player left {bitData.bitName} collection range");
                }
            }
        }
    }
    
    private void StartCollection()
    {
        isBeingCollected = true;
        playerInRange = false;
        if (qIcon != null)
            qIcon.SetActive(false);
        rb.simulated = false; // Disable physics during collection
    }
    
    private void CollectBit()
    {
        Debug.Log($"Attempting to collect {bitData.bitName}...");
        
        // Use BitCollectionManager to add bit to player's build
        if (BitCollectionManager.Instance != null)
        {
            bool success = BitCollectionManager.Instance.CollectBit(bitData);
            if (success)
            {
                Debug.Log($"Collected {bitData.bitName} successfully!");
                Destroy(gameObject);
            }
            else
            {
                Debug.Log($"Inventory is full! Cannot collect {bitData.bitName}.");
                // Reset collection state and return to normal behavior
                isBeingCollected = false;
                rb.simulated = true; // Re-enable physics
                // Don't destroy the bit, let it stay in the scene
            }
        }
        else
        {
            Debug.LogWarning("BitCollectionManager not found! Cannot add bit to build.");
            // Reset collection state on error too
            isBeingCollected = false;
            rb.simulated = true;
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if we hit the ground using layer mask
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            isGrounded = true;
        }
    }
    
    private void OnCollisionExit2D(Collision2D collision)
    {
        // Check if we left the ground using layer mask
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            isGrounded = false;
        }
    }
    
    // Public method to set bit data
    public void SetBitData(Bit bit)
    {
        bitData = bit;
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && bit != null)
        {
            spriteRenderer.sprite = bit.GetSprite();
        }
    }
    
    // Public method to drop the bit at a specific position
    public static BitDrop CreateBitDrop(Bit bit, Vector3 position)
    {
        // Load the prefab
        GameObject dropPrefab = Resources.Load<GameObject>("Prefabs/BitDrop");
        if (dropPrefab == null)
        {
            Debug.LogError("BitDrop prefab not found in Resources/Prefabs/");
            return null;
        }
        
        GameObject dropObject = Instantiate(dropPrefab, position, Quaternion.identity);
        BitDrop bitDrop = dropObject.GetComponent<BitDrop>();
        
        if (bitDrop != null)
        {
            bitDrop.SetBitData(bit);
        }
        
        return bitDrop;
    }
} 