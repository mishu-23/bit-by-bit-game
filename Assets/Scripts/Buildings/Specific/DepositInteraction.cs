using UnityEngine;
using UnityEngine.UI;

public class DepositInteraction : MonoBehaviour
{
    [Header("Assign the Q_Icon and E_Icon children here")]
    public GameObject qIcon;
    public GameObject eIcon;
    
    [Header("Core Bit Deposit Settings")]
    [SerializeField] private int coreBitCount = 10; // Number of Core Bits in the deposit
    
    [Header("Visual Feedback")]
    [SerializeField] private Image progressBar; // Optional radial fill for progress
    [SerializeField] private Color progressColor = Color.blue;
    [SerializeField] private Color emptyColor = Color.gray;
    
    private bool playerInRange = false;
    private PowerBitPlayerController playerController;
    
    private void Start()
    {
        if (qIcon != null) qIcon.SetActive(false);
        if (eIcon != null) eIcon.SetActive(false);
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
        
        // Q: Give Core Bit to player
        if (Input.GetKeyDown(KeyCode.Q))
        {
            TryGiveCoreBitToPlayer();
        }
        // E: Take Core Bit from player
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
        // Give Core Bit
        Bit coreBit = BitManager.Instance?.testCoreBit;
        if (coreBit == null)
        {
            Debug.LogWarning("No Core Bit asset assigned in BitManager!");
            return;
        }
        bool added = BitCollectionManager.Instance.CollectBit(coreBit);
        if (added)
        {
            coreBitCount--;
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
        // Remove Core Bit from player
        Bit removed = RemoveCoreBitFromPlayer();
        if (removed != null)
        {
            coreBitCount++;
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
        // Remove the first Core Bit found
        foreach (var pos in activeBits)
        {
            var bitData = playerController.powerBitCharacterRenderer.GetBitAt(pos);
            if (bitData != null && bitData.bitType == BitType.CoreBit)
            {
                playerController.powerBitCharacterRenderer.RemoveBit(pos);
                // Create a Bit object to return
                Bit bit = Bit.CreateBit(bitData.bitName, bitData.bitType, bitData.rarity, bitData.damage, bitData.shootingProbability);
                // Save updated build
                playerController.SaveUpdatedBuild();
                return bit;
            }
        }
        return null;
    }

    public void AddCoreBitFromGatherer()
    {
        coreBitCount++;
        Debug.Log($"Gatherer deposited a Core Bit. Core Bits in deposit: {coreBitCount}");
    }
    
    public bool RemoveCoreBit()
    {
        if (coreBitCount > 0)
        {
            coreBitCount--;
            Debug.Log($"Core Bit removed from deposit. Core Bits left: {coreBitCount}");
            return true;
        }
        Debug.Log("Cannot remove Core Bit - deposit is empty!");
        return false;
    }
} 
