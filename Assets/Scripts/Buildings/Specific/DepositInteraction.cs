using UnityEngine;
using UnityEngine.UI;
using BitByBit.Items;
public class DepositInteraction : MonoBehaviour
{
    [Header("Assign the Q_Icon and E_Icon children here")]
    public GameObject qIcon;
    public GameObject eIcon;
    [Header("Core Bit Deposit Settings")]
    [SerializeField] private int coreBitCount = 0;
    [Header("Visual Feedback")]
    [SerializeField] private Image progressBar;
    [SerializeField] private Color progressColor = Color.blue;
    [SerializeField] private Color emptyColor = Color.gray;
    private bool playerInRange = false;
    private PowerBitPlayerController playerController;
    private void Start()
    {
        if (qIcon != null) qIcon.SetActive(false);
        if (eIcon != null) eIcon.SetActive(false);
        LoadDepositState();
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (qIcon != null) qIcon.SetActive(true);
            if (eIcon != null) eIcon.SetActive(true);
            playerInRange = true;
            playerController = other.GetComponent<PowerBitPlayerController>();
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (qIcon != null) qIcon.SetActive(false);
            if (eIcon != null) eIcon.SetActive(false);
            playerInRange = false;
            playerController = null;
        }
    }
    private void Update()
    {
        if (!playerInRange || playerController == null) return;
        if (Input.GetKeyDown(KeyCode.Q))
        {
            TryGiveCoreBitToPlayer();
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryTakeCoreBitFromPlayer();
        }
    }
    private void TryGiveCoreBitToPlayer()
    {
        if (coreBitCount <= 0)
        {
            Debug.Log("Deposit is empty! No Core Bits to give.");
            return;
        }
        if (!PlayerHasBuildSpace())
        {
            Debug.Log("Player's build is full! Cannot add Core Bit.");
            return;
        }
        Bit coreBit = Bit.CreateBit("Common CoreBit", BitType.CoreBit, Rarity.Common, 0, 0f);
        bool added = BitCollectionManager.Instance.CollectBit(coreBit);
        if (added)
        {
            coreBitCount--;
            SaveDepositState();
            Debug.Log($"Player took a Core Bit from deposit. Core Bits left: {coreBitCount}");
        }
        else
        {
            Debug.Log("Failed to add Core Bit to player's build (maybe full).");
        }
    }
    private void TryTakeCoreBitFromPlayer()
    {
        if (!PlayerHasCoreBit())
        {
            Debug.Log("Player has no Core Bit to deposit!");
            return;
        }
        Bit removed = RemoveCoreBitFromPlayer();
        if (removed != null)
        {
            coreBitCount++;
            SaveDepositState();
            Debug.Log($"Player deposited a Core Bit. Core Bits in deposit: {coreBitCount}");
        }
        else
        {
            Debug.Log("Failed to remove Core Bit from player's build.");
        }
    }
    private bool PlayerHasBuildSpace()
    {
        return BitCollectionManager.Instance != null && BitCollectionManager.Instance.HasEmptySpace();
    }
    private bool PlayerHasCoreBit()
    {
        if (playerController == null || playerController.powerBitCharacterRenderer == null) return false;
        var activeBits = playerController.powerBitCharacterRenderer.GetActiveBits();
        foreach (var pos in activeBits)
        {
            var bit = playerController.powerBitCharacterRenderer.GetBitAt(pos);
            if (bit != null && bit.bitType == BitType.CoreBit)
                return true;
        }
        return false;
    }
    private Bit RemoveCoreBitFromPlayer()
    {
        if (playerController == null || playerController.powerBitCharacterRenderer == null) return null;
        var activeBits = playerController.powerBitCharacterRenderer.GetActiveBits();
        foreach (var pos in activeBits)
        {
            var bitData = playerController.powerBitCharacterRenderer.GetBitAt(pos);
            if (bitData != null && bitData.bitType == BitType.CoreBit)
            {
                playerController.powerBitCharacterRenderer.RemoveBit(pos);
                Bit bit = Bit.CreateBit(bitData.bitName, bitData.bitType, bitData.rarity, bitData.damage, bitData.shootingProbability);
                playerController.SaveUpdatedBuild();
                return bit;
            }
        }
        return null;
    }
    public void AddCoreBitFromGatherer()
    {
        coreBitCount++;
        SaveDepositState();
        Debug.Log($"Gatherer deposited a Core Bit. Core Bits in deposit: {coreBitCount}");
    }
    public bool RemoveCoreBit()
    {
        if (coreBitCount > 0)
        {
            coreBitCount--;
            SaveDepositState();
            Debug.Log($"Core Bit removed from deposit. Core Bits left: {coreBitCount}");
            return true;
        }
        Debug.Log("Cannot remove Core Bit - deposit is empty!");
        return false;
    }
    private void LoadDepositState()
    {
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, "settlement_storage.json");
        if (!System.IO.File.Exists(filePath))
        {
            Debug.Log("No settlement storage file found. Starting with empty deposit (0 Core Bits).");
            coreBitCount = 0;
            return;
        }
        try
        {
            string json = System.IO.File.ReadAllText(filePath);
            SettlementSaveData saveData = JsonUtility.FromJson<SettlementSaveData>(json);
            if (saveData != null)
            {
                coreBitCount = saveData.depositCoreBitCount;
                Debug.Log($"Deposit loaded with {coreBitCount} Core Bits from save file.");
            }
            else
            {
                Debug.LogWarning("Failed to parse settlement save data. Starting with empty deposit.");
                coreBitCount = 0;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading settlement storage: {e.Message}. Starting with empty deposit.");
            coreBitCount = 0;
        }
    }
    private void SaveDepositState()
    {
        SettlementSaveData saveData = LoadExistingSettlementData();
        saveData.depositCoreBitCount = coreBitCount;
        try
        {
            string json = JsonUtility.ToJson(saveData, true);
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, "settlement_storage.json");
            System.IO.File.WriteAllText(filePath, json);
            Debug.Log($"Deposit state saved: {coreBitCount} Core Bits");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving settlement storage: {e.Message}");
        }
    }
    private SettlementSaveData LoadExistingSettlementData()
    {
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, "settlement_storage.json");
        if (!System.IO.File.Exists(filePath))
        {
            return new SettlementSaveData();
        }
        try
        {
            string json = System.IO.File.ReadAllText(filePath);
            SettlementSaveData saveData = JsonUtility.FromJson<SettlementSaveData>(json);
            return saveData ?? new SettlementSaveData();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading settlement storage: {e.Message}");
            return new SettlementSaveData();
        }
    }
    public int GetCoreBitCount()
    {
        return coreBitCount;
    }
}
