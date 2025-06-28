using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using BitByBit.Items;

public class BitCarrier : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Bit Drop Settings")]
    [SerializeField] private GameObject bitDropPrefab;
    [SerializeField] private Vector3 bitDropOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private float bitDropBobAmount = 0.2f;
    [SerializeField] private float bitDropBobSpeed = 2f;
    
    #endregion

    #region Private Fields
    
    private GameObject attachedBitDrop;
    private Bit attachedBitData;
    private Vector3 originalBitDropOffset;
    private float bobTimer = 0f;
    
    #endregion

    #region Events
    
    [System.Serializable]
    public class BitAttachedEvent : UnityEvent<Bit> { }
    
    [System.Serializable]
    public class BitDroppedEvent : UnityEvent<Bit> { }
    
    public BitAttachedEvent OnBitAttached = new BitAttachedEvent();
    public BitDroppedEvent OnBitDropped = new BitDroppedEvent();
    
    #endregion

    #region Public Properties
    
    public bool IsCarryingBit => attachedBitDrop != null;
    public Bit CarriedBit => attachedBitData;
    public GameObject CarriedBitDrop => attachedBitDrop;
    
    #endregion

    #region Unity Lifecycle
    
    private void Awake()
    {
        originalBitDropOffset = bitDropOffset;
    }
    
    private void Update()
    {
        if (IsCarryingBit)
        {
            UpdateAttachedBitDropPosition();
        }
    }
    
    #endregion

    #region Bit Management
    
    public void AttachBit(Bit bit)
    {
        if (IsCarryingBit)
        {
            DropBit();
        }
        
        CreateAttachedBitDropWithBit(bit);
    }
    
    public void AttachRandomBit()
    {
        if (IsCarryingBit)
        {
            DropBit();
        }
        
        CreateAttachedBitDrop();
    }
    
    public void DropBit()
    {
        if (!IsCarryingBit) return;
        
        DropStolenBit();
    }
    
    public void DropRandomBit()
    {
        if (IsCarryingBit)
        {
            DropBit();
            return;
        }
        
        // Create and immediately drop a random bit
        Bit randomBit = CreateRandomBit();
        CreateBitDropAtPosition(randomBit, transform.position);
    }
    
    #endregion

    #region Bit Creation and Setup
    
    private void CreateAttachedBitDrop()
    {
        Bit randomBit = CreateRandomBit();
        CreateAttachedBitDropWithBit(randomBit);
    }
    
    private void CreateAttachedBitDropWithBit(Bit bit)
    {
        if (bit == null) return;
        
        attachedBitData = bit;
        
        // Create bit drop GameObject
        if (bitDropPrefab == null)
        {
            Debug.LogError("BitCarrier: BitDrop prefab not assigned! Please assign the BitDrop prefab in the inspector.");
            return;
        }
        {
            // Create the BitDrop using the static factory method which handles timing properly
            BitDrop createdBitDrop = BitDrop.CreateBitDrop(bit, transform.position + bitDropOffset);
            
            if (createdBitDrop != null)
            {
                attachedBitDrop = createdBitDrop.gameObject;
                
                // Disable the component while attached to prevent collection
                createdBitDrop.enabled = false;
                
                Debug.Log($"BitCarrier: Created attached BitDrop with bit: {bit.BitName}");
                OnBitAttached?.Invoke(bit);
            }
            else
            {
                Debug.LogError("BitCarrier: Failed to create BitDrop!");
            }
        }
    }
    
    private Bit CreateRandomBit()
    {
        // PowerBits can only be Rare, Epic, or Legendary (Common is reserved for CoreBits)
        Rarity[] powerBitRarities = { Rarity.Rare, Rarity.Epic, Rarity.Legendary };
        Rarity randomRarity = powerBitRarities[Random.Range(0, powerBitRarities.Length)];
        
        // Get appropriate stats for the rarity
        int damage = randomRarity switch
        {
            Rarity.Rare => 2,
            Rarity.Epic => 4,
            Rarity.Legendary => 8,
            _ => 0
        };
        
        float shootingProbability = randomRarity switch
        {
            Rarity.Rare => Random.Range(0.6f, 0.8f),
            Rarity.Epic => Random.Range(0.4f, 0.5f),
            Rarity.Legendary => Random.Range(0.2f, 0.3f),
            _ => 0f
        };
        
        // Use the same naming convention as the BitManager to ensure compatibility
        return Bit.CreateBit($"{randomRarity} PowerBit", BitType.PowerBit, randomRarity, damage, shootingProbability);
    }
    
    #endregion

    #region Visual Effects
    
    private void UpdateAttachedBitDropPosition()
    {
        if (attachedBitDrop == null) return;
        
        // Update bobbing motion
        bobTimer += Time.deltaTime * bitDropBobSpeed;
        float bobOffset = Mathf.Sin(bobTimer) * bitDropBobAmount;
        
        // Calculate final position with bobbing
        Vector3 targetPosition = transform.position + originalBitDropOffset + Vector3.up * bobOffset;
        attachedBitDrop.transform.position = targetPosition;
    }
    
    #endregion

    #region Bit Dropping
    
    private void DropStolenBit()
    {
        if (attachedBitDrop == null || attachedBitData == null) return;
        
        // Unparent the bit drop so it's independent
        attachedBitDrop.transform.SetParent(null);
        
        // Position the bit drop on the ground
        Vector3 dropPosition = transform.position;
        dropPosition.y = 0.5f; // Ground level
        attachedBitDrop.transform.position = dropPosition;
        
        // Re-enable collision and collection
        BitDrop bitDropComponent = attachedBitDrop.GetComponent<BitDrop>();
        if (bitDropComponent != null)
        {
            // Make sure the bit data is properly set
            bitDropComponent.SetBitData(attachedBitData);
            // Re-enable the component for collection
            bitDropComponent.enabled = true;
            
            // Force immediate verification
            bool isEnabledAfterSet = bitDropComponent.enabled;
            
            // Verify the bit drop is set up correctly
            Debug.Log($"BitCarrier: Dropped bit '{attachedBitData.BitName}' at position {dropPosition}");
            Debug.Log($"BitCarrier: BitDrop component enabled after setting: {isEnabledAfterSet}, Has bit data: {bitDropComponent.BitData != null}");
            
            // Double-check that it stays enabled
            if (!isEnabledAfterSet)
            {
                Debug.LogError($"BitCarrier: BitDrop component failed to enable! Trying again...");
                bitDropComponent.enabled = true;
                Debug.Log($"BitCarrier: Second attempt - BitDrop enabled: {bitDropComponent.enabled}");
            }
            
            // Check if the BitDrop has required components
            Collider2D collider = attachedBitDrop.GetComponent<Collider2D>();
            SpriteRenderer spriteRenderer = attachedBitDrop.GetComponent<SpriteRenderer>();
            Debug.Log($"BitCarrier: BitDrop has Collider2D: {collider != null && collider.enabled}, SpriteRenderer: {spriteRenderer != null}");
            
            // Ensure collider is enabled
            if (collider != null && !collider.enabled)
            {
                collider.enabled = true;
                Debug.Log($"BitCarrier: Re-enabled Collider2D on dropped bit");
            }
            
            // Wait a frame and then verify the BitDrop is working
            StartCoroutine(VerifyBitDropAfterFrame(bitDropComponent));
            
            // Also check after a longer delay to see if something disables it
            StartCoroutine(VerifyBitDropAfterDelay(bitDropComponent, 1f));
        }
        else
        {
            Debug.LogError("BitCarrier: Dropped bit doesn't have BitDrop component!");
        }
        
        OnBitDropped?.Invoke(attachedBitData);
        
        // Clear references
        attachedBitDrop = null;
        attachedBitData = null;
        bobTimer = 0f;
    }
    
    private void CreateBitDropAtPosition(Bit bit, Vector3 position)
    {
        if (bitDropPrefab == null)
        {
            Debug.LogError("BitCarrier: BitDrop prefab not assigned! Cannot create bit drop.");
            return;
        }
        
        GameObject droppedBit = Instantiate(bitDropPrefab, position, Quaternion.identity);
        
        BitDrop bitDropComponent = droppedBit.GetComponent<BitDrop>();
        if (bitDropComponent != null)
        {
            bitDropComponent.SetBitData(bit);
            bitDropComponent.enabled = true;
        }
    }
    
    #endregion

    #region Public Utilities
    
    public void SetBitDropOffset(Vector3 offset)
    {
        bitDropOffset = offset;
        originalBitDropOffset = offset;
    }
    
    public void SetBobSettings(float bobAmount, float bobSpeed)
    {
        bitDropBobAmount = bobAmount;
        bitDropBobSpeed = bobSpeed;
    }
    
    private System.Collections.IEnumerator VerifyBitDropAfterFrame(BitDrop bitDropComponent)
    {
        yield return null; // Wait one frame
        
        if (bitDropComponent != null && bitDropComponent.gameObject != null)
        {
            Debug.Log($"BitCarrier: Frame Verification - BitDrop component active: {bitDropComponent.enabled}");
            Debug.Log($"BitCarrier: Frame Verification - GameObject active: {bitDropComponent.gameObject.activeInHierarchy}");
            Debug.Log($"BitCarrier: Frame Verification - Has bit data: {bitDropComponent.BitData != null}");
            
            if (bitDropComponent.BitData != null)
            {
                Debug.Log($"BitCarrier: Frame Verification - Bit name: {bitDropComponent.BitData.BitName}");
            }
            
            if (!bitDropComponent.enabled)
            {
                Debug.LogError("BitCarrier: Frame Verification - BitDrop was disabled after one frame! Re-enabling...");
                bitDropComponent.enabled = true;
            }
        }
        else
        {
            Debug.LogWarning("BitCarrier: Frame Verification - BitDrop component or GameObject is null!");
        }
    }
    
    private System.Collections.IEnumerator VerifyBitDropAfterDelay(BitDrop bitDropComponent, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (bitDropComponent != null && bitDropComponent.gameObject != null)
        {
            Debug.Log($"BitCarrier: Delayed Verification ({delay}s) - BitDrop component active: {bitDropComponent.enabled}");
            Debug.Log($"BitCarrier: Delayed Verification ({delay}s) - GameObject active: {bitDropComponent.gameObject.activeInHierarchy}");
            Debug.Log($"BitCarrier: Delayed Verification ({delay}s) - Has bit data: {bitDropComponent.BitData != null}");
            
            if (!bitDropComponent.enabled)
            {
                Debug.LogError($"BitCarrier: Delayed Verification ({delay}s) - BitDrop was disabled! Something is disabling it after drop. Re-enabling...");
                bitDropComponent.enabled = true;
                
                // Try to find what might be disabling it
                MonoBehaviour[] allComponents = bitDropComponent.GetComponents<MonoBehaviour>();
                Debug.Log($"BitCarrier: Components on BitDrop GameObject: {string.Join(", ", System.Array.ConvertAll(allComponents, c => c.GetType().Name))}");
            }
        }
        else
        {
            Debug.LogWarning($"BitCarrier: Delayed Verification ({delay}s) - BitDrop component or GameObject is null!");
        }
    }
    
    #endregion
} 