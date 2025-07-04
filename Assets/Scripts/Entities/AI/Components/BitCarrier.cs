using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using BitByBit.Items;
public class BitCarrier : MonoBehaviour
{
    [Header("Bit Drop Settings")]
    [SerializeField] private GameObject bitDropPrefab;
    [SerializeField] private Vector3 bitDropOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private float bitDropBobAmount = 0.2f;
    [SerializeField] private float bitDropBobSpeed = 2f;
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugInfo = false;
    private GameObject attachedBitDrop;
    private Bit attachedBitData;
    private Vector3 originalBitDropOffset;
    private float bobTimer = 0f;
    [System.Serializable]
    public class BitAttachedEvent : UnityEvent<Bit> { }
    [System.Serializable]
    public class BitDroppedEvent : UnityEvent<Bit> { }
    public BitAttachedEvent OnBitAttached = new BitAttachedEvent();
    public BitDroppedEvent OnBitDropped = new BitDroppedEvent();
    public bool IsCarryingBit => attachedBitDrop != null;
    public Bit CarriedBit => attachedBitData;
    public GameObject CarriedBitDrop => attachedBitDrop;
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
        if (BitManager.Instance != null)
        {
            if (enableDebugInfo)
            {
                Debug.Log("BitCarrier: Calling BitManager.GetRandomBit()...");
            }
            Bit randomBit = BitManager.Instance.GetRandomBit();
            if (enableDebugInfo)
            {
                Debug.Log($"BitCarrier: BitManager returned: {randomBit?.BitName ?? "NULL"}");
            }
            CreateBitDropAtPosition(randomBit, transform.position);
        }
        else
        {
            Debug.LogError("BitCarrier: BitManager.Instance not found! Cannot drop random bit.");
        }
    }
    private void CreateAttachedBitDrop()
    {
        if (BitManager.Instance != null)
        {
            Bit randomBit = BitManager.Instance.GetRandomBit();
            CreateAttachedBitDropWithBit(randomBit);
        }
        else
        {
            Debug.LogError("BitCarrier: BitManager.Instance not found! Cannot create random bit.");
        }
    }
    private void CreateAttachedBitDropWithBit(Bit bit)
    {
        if (bit == null) return;
        attachedBitData = bit;
        if (bitDropPrefab == null)
        {
            Debug.LogError("BitCarrier: BitDrop prefab not assigned! Please assign the BitDrop prefab in the inspector.");
            return;
        }
        {
                BitDrop createdBitDrop = BitDrop.CreateBitDrop(bit, transform.position + bitDropOffset);
                if (createdBitDrop != null)
                {
                    attachedBitDrop = createdBitDrop.gameObject;
                    createdBitDrop.enabled = false;
                    if (enableDebugInfo)
                    {
                        Debug.Log($"BitCarrier: Created attached BitDrop with bit: {bit.BitName}");
                    }
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
        Rarity[] powerBitRarities = { Rarity.Rare, Rarity.Epic, Rarity.Legendary };
        Rarity randomRarity = powerBitRarities[Random.Range(0, powerBitRarities.Length)];
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
        return Bit.CreateBit($"{randomRarity} PowerBit", BitType.PowerBit, randomRarity, damage, shootingProbability);
    }
    private void UpdateAttachedBitDropPosition()
    {
        if (attachedBitDrop == null) return;
        bobTimer += Time.deltaTime * bitDropBobSpeed;
        float bobOffset = Mathf.Sin(bobTimer) * bitDropBobAmount;
        Vector3 targetPosition = transform.position + originalBitDropOffset + Vector3.up * bobOffset;
        attachedBitDrop.transform.position = targetPosition;
    }
    private void DropStolenBit()
    {
        if (attachedBitDrop == null || attachedBitData == null) return;
        attachedBitDrop.transform.SetParent(null);
        Vector3 dropPosition = transform.position;
        dropPosition.y = 0.5f;
        attachedBitDrop.transform.position = dropPosition;
        BitDrop bitDropComponent = attachedBitDrop.GetComponent<BitDrop>();
        if (bitDropComponent != null)
        {
            bitDropComponent.SetBitData(attachedBitData);
            bitDropComponent.enabled = true;
            bool isEnabledAfterSet = bitDropComponent.enabled;
            if (enableDebugInfo)
            {
                Debug.Log($"BitCarrier: Dropped bit '{attachedBitData.BitName}' at position {dropPosition}");
                Debug.Log($"BitCarrier: BitDrop component enabled after setting: {isEnabledAfterSet}, Has bit data: {bitDropComponent.BitData != null}");
                if (!isEnabledAfterSet)
                {
                    Debug.LogError($"BitCarrier: BitDrop component failed to enable! Trying again...");
                    bitDropComponent.enabled = true;
                    Debug.Log($"BitCarrier: Second attempt - BitDrop enabled: {bitDropComponent.enabled}");
                }
                Collider2D collider = attachedBitDrop.GetComponent<Collider2D>();
                SpriteRenderer spriteRenderer = attachedBitDrop.GetComponent<SpriteRenderer>();
                Debug.Log($"BitCarrier: BitDrop has Collider2D: {collider != null && collider.enabled}, SpriteRenderer: {spriteRenderer != null}");
            }
            Collider2D collider2D = attachedBitDrop.GetComponent<Collider2D>();
            if (collider2D != null && !collider2D.enabled)
            {
                collider2D.enabled = true;
                if (enableDebugInfo)
                {
                    Debug.Log($"BitCarrier: Re-enabled Collider2D on dropped bit");
                }
            }
            StartCoroutine(VerifyBitDropAfterFrame(bitDropComponent));
            StartCoroutine(VerifyBitDropAfterDelay(bitDropComponent, 1f));
        }
        else
        {
            Debug.LogError("BitCarrier: Dropped bit doesn't have BitDrop component!");
        }
        OnBitDropped?.Invoke(attachedBitData);
        attachedBitDrop = null;
        attachedBitData = null;
        bobTimer = 0f;
    }
    private void CreateBitDropAtPosition(Bit bit, Vector3 position)
    {
        BitByBit.Items.BitDrop bitDropComponent = BitByBit.Items.BitDrop.CreateBitDrop(bit, position);
        if (bitDropComponent != null)
        {
            if (enableDebugInfo)
            {
                Debug.Log($"BitCarrier: Successfully created BitDrop for {bit.BitName} at {position}");
            }
        }
        else
        {
            Debug.LogError("BitCarrier: Failed to create BitDrop using static factory method!");
        }
    }
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
        yield return null;
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
                MonoBehaviour[] allComponents = bitDropComponent.GetComponents<MonoBehaviour>();
                Debug.Log($"BitCarrier: Components on BitDrop GameObject: {string.Join(", ", System.Array.ConvertAll(allComponents, c => c.GetType().Name))}");
            }
        }
        else
        {
            Debug.LogWarning($"BitCarrier: Delayed Verification ({delay}s) - BitDrop component or GameObject is null!");
        }
    }
}