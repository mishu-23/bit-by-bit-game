using UnityEngine;
using UnityEngine.Events;
public class CrawlingEntityCombat : MonoBehaviour, IDamageable
{
    [Header("Combat Settings")]
    [SerializeField] private int maxHealth = 10;
    private int currentHealth;
    [System.Serializable]
    public class HealthChangeEvent : UnityEvent<int, int> { } 
    [System.Serializable]
    public class DeathEvent : UnityEvent { }
    public HealthChangeEvent OnHealthChanged = new HealthChangeEvent();
    public DeathEvent OnDeath = new DeathEvent();
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;
    public float HealthPercentage => (float)currentHealth / maxHealth;
    private void Awake()
    {
        InitializeHealth();
    }
    private void InitializeHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
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
    private void HandleDeath()
    {
        OnDeath?.Invoke();
    }
}