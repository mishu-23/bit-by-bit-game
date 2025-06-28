using UnityEngine;
using UnityEngine.Events;

public class CrawlingEntityCombat : MonoBehaviour, IDamageable
{
    #region Serialized Fields
    
    [Header("Combat Settings")]
    [SerializeField] private int maxHealth = 10;
    
    #endregion

    #region Private Fields
    
    private int currentHealth;
    
    #endregion

    #region Events
    
    [System.Serializable]
    public class HealthChangeEvent : UnityEvent<int, int> { } // currentHealth, maxHealth
    
    [System.Serializable]
    public class DeathEvent : UnityEvent { }
    
    public HealthChangeEvent OnHealthChanged = new HealthChangeEvent();
    public DeathEvent OnDeath = new DeathEvent();
    
    #endregion

    #region Public Properties
    
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;
    public float HealthPercentage => (float)currentHealth / maxHealth;
    
    #endregion

    #region Unity Lifecycle
    
    private void Awake()
    {
        InitializeHealth();
    }
    
    #endregion

    #region Initialization
    
    private void InitializeHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    #endregion

    #region IDamageable Implementation
    
    public void TakeDamage(int damage)
    {
        if (!IsAlive) return;
        
        currentHealth = Mathf.Max(0, currentHealth - damage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        if (currentHealth <= 0)
        {
            HandleDeath();
        }
    }
    
    #endregion

    #region Health Management
    
    public void Heal(int healAmount)
    {
        if (!IsAlive) return;
        
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    public void SetHealth(int health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        if (currentHealth <= 0)
        {
            HandleDeath();
        }
    }
    
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    #endregion

    #region Death Handling
    
    private void HandleDeath()
    {
        OnDeath?.Invoke();
    }
    
    #endregion
} 