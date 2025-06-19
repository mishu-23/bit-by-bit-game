using UnityEngine;
using System;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    private const int MIN_STAT_VALUE = 0;
    private const int MAX_STAT_VALUE = 100;

    [Header("Base Stats")]
    [SerializeField] private int armor;
    [SerializeField] private float criticalChance;
    [SerializeField] private int damage;
    [SerializeField] private int maxHealth;
    [SerializeField] private int currentHealth;
    [SerializeField] private float lifesteal;
    [SerializeField] private int luck;

    public event Action<PixelType, int> OnStatChanged;
    public event Action<int> OnHealthChanged;

    private StatsPanel statsPanel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeStats();
    }

    private void InitializeStats()
    {
        statsPanel = FindObjectOfType<StatsPanel>();
        if (statsPanel == null)
        {
            Debug.LogWarning($"[{nameof(PlayerStats)}] StatsPanel not found in scene!");
        }

        // Initialize all stats to 0
        armor = 0;
        criticalChance = 0;
        damage = 0;
        maxHealth = 0;
        currentHealth = 0;
        lifesteal = 0;
        luck = 0;
    }

    public void UpdateStatsFromPixels(PixelType pixelType, int count)
    {
        if (count < MIN_STAT_VALUE || count > MAX_STAT_VALUE)
        {
            Debug.LogWarning($"[{nameof(PlayerStats)}] Invalid stat value: {count}. Must be between {MIN_STAT_VALUE} and {MAX_STAT_VALUE}");
            return;
        }

        int oldValue = 0;
        switch (pixelType)
        {
            case PixelType.Armor:
                oldValue = armor;
                armor = count;
                break;
            case PixelType.Critical:
                oldValue = (int)criticalChance;
                criticalChance = count;
                break;
            case PixelType.Damage:
                oldValue = damage;
                damage = count;
                break;
            case PixelType.Health:
                oldValue = maxHealth;
                maxHealth = count;
                currentHealth = Mathf.Min(currentHealth, maxHealth);
                OnHealthChanged?.Invoke(currentHealth);
                break;
            case PixelType.Lifesteal:
                oldValue = (int)lifesteal;
                lifesteal = count;
                break;
            case PixelType.Luck:
                oldValue = luck;
                luck = count;
                break;
        }

        if (oldValue != count)
        {
            OnStatChanged?.Invoke(pixelType, count);
            if (statsPanel != null)
            {
                statsPanel.UpdateAllStats();
            }
        }
    }

    // Getters for stats
    public int GetArmor() => armor;
    public float GetCriticalChance() => criticalChance;
    public int GetDamage() => damage;
    public int GetMaxHealth() => maxHealth;
    public int GetCurrentHealth() => currentHealth;
    public float GetLifesteal() => lifesteal;
    public int GetLuck() => luck;

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}