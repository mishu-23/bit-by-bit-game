using UnityEngine;

public class BitDrop : MonoBehaviour
{
    [Header("Bit Data")]
    public Bit bitData;
    
    [Header("Collection Settings")]
    public float collectionRange = 2f;
    public float collectionSpeed = 5f;
    public LayerMask groundLayer = 1; // Default layer
    
    [Header("F Prompt")]
    public GameObject fIcon; // Assign the F_Icon child here
    
    private Rigidbody2D rb;
    private bool isGrounded = false;
    private bool isBeingCollected = false;
    private bool playerInRange = false;
    private Transform playerTransform;
    
    // Static reference to the prefab - assign this in the inspector of any BitDrop in the scene
    private static GameObject bitDropPrefab;
    
    // Public static reference that can be assigned from anywhere
    public static GameObject BitDropPrefab { get; set; }
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Set the sprite based on bit data
        if (bitData != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = bitData.GetSprite();
        }
        
        // Set up F icon
        SetupFIcon();
    }
    
    private void SetupFIcon()
    {
        if (fIcon != null)
        {
            fIcon.SetActive(false); // Hide by default
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
            // Check distance to player for F prompt
            CheckPlayerDistance();
        }
        
        // Handle F key press for collection (but not when paused)
        if (playerInRange && Input.GetKeyDown(KeyCode.F) && 
            (PauseManager.Instance == null || !PauseManager.Instance.IsPaused))
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
                // Show F prompt when player is in range
                if (!playerInRange)
                {
                    playerInRange = true;
                    if (fIcon != null)
                        fIcon.SetActive(true);
                    Debug.Log($"Player entered {bitData.BitName} collection range");
                }
            }
            else
            {
                // Hide F prompt when player is out of range
                if (playerInRange)
                {
                    playerInRange = false;
                    if (fIcon != null)
                        fIcon.SetActive(false);
                    Debug.Log($"Player left {bitData.BitName} collection range");
                }
            }
        }
    }
    
    private void StartCollection()
    {
        isBeingCollected = true;
        playerInRange = false;
        if (fIcon != null)
            fIcon.SetActive(false);
        rb.simulated = false; // Disable physics during collection
    }
    
    private void CollectBit()
    {
        Debug.Log($"Attempting to collect {bitData.BitName}...");
        
        // Use BitCollectionManager to add bit to player's build
        if (BitCollectionManager.Instance != null)
        {
            bool success = BitCollectionManager.Instance.CollectBit(bitData);
            if (success)
            {
                Debug.Log($"Collected {bitData.BitName} successfully!");
                Destroy(gameObject);
            }
            else
            {
                Debug.Log($"Inventory is full! Cannot collect {bitData.BitName}.");
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
        if (BitDropPrefab == null)
        {
            Debug.LogError("BitDropPrefab not assigned! Please assign the BitDrop prefab to BitDrop.BitDropPrefab from any script or inspector.");
            return null;
        }
        
        GameObject dropObject = Instantiate(BitDropPrefab, position, Quaternion.identity);
        BitDrop bitDrop = dropObject.GetComponent<BitDrop>();
        
        if (bitDrop != null)
        {
            bitDrop.SetBitData(bit);
        }
        
        return bitDrop;
    }
} 