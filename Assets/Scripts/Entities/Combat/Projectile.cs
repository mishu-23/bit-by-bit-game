using UnityEngine;
public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask targetLayers = 1;
    [SerializeField] private bool destroyOnHit = true;
    [Header("Visual Effects")]
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    private Vector2 direction;
    private bool hasHit = false;
    private float timer;
    private Rarity rarity = Rarity.Common;
    public int Damage { get => damage; set => damage = value; }
    public Rarity Rarity { get => rarity; set => rarity = value; }
    private void Awake()
    {
        InitializeComponents();
        InitializeTimer();
    }
    private void Start()
    {
        LogSpawnInfo();
    }
    private void Update()
    {
        if (hasHit) return;
        ProcessMovement();
        ProcessLifetime();
    }
    private void InitializeComponents()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
    private void InitializeTimer()
    {
        timer = lifetime;
    }
    public void Initialize(Vector2 direction, int damage = 1, Rarity rarity = Rarity.Common)
    {
        SetDirection(direction);
        SetDamage(damage);
        SetRarity(rarity);
        SetRotation(direction);
        LogInitializationInfo();
    }
    private void ProcessMovement()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }
    private void ProcessLifetime()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            LogDebugInfo("Projectile destroyed due to lifetime");
            DestroyProjectile();
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        if (ShouldIgnoreCollision(other)) return;
        if (CanDamageTarget(other))
        {
            ProcessHit(other);
        }
    }
    private void ProcessHit(Collider2D other)
    {
        hasHit = true;
        DealDamageToTarget(other);
        SpawnHitEffect();
        if (destroyOnHit)
        {
            DestroyProjectile();
        }
    }
    private bool ShouldIgnoreCollision(Collider2D other)
    {
        if (other.GetComponent<Projectile>() != null)
            return true;
        if (other.GetComponent<GathererEntity>() != null)
            return true;
        return false;
    }
    private bool CanDamageTarget(Collider2D other)
    {
        return ((1 << other.gameObject.layer) & targetLayers) != 0;
    }
    private void DealDamageToTarget(Collider2D other)
    {
        var damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            LogDebugInfo($"Projectile hit {other.name} for {damage} damage");
        }
        else
        {
            LogDebugInfo($"Projectile hit {other.name} but it's not damageable");
        }
    }
    private void SpawnHitEffect()
    {
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, transform.rotation);
        }
    }
    private void SetDirection(Vector2 newDirection)
    {
        direction = newDirection.normalized;
    }
    private void SetDamage(int newDamage)
    {
        damage = newDamage;
    }
    private void SetRarity(Rarity newRarity)
    {
        rarity = newRarity;
    }
    private void SetRotation(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
    private void DestroyProjectile()
    {
        LogDebugInfo("Destroying projectile");
        Destroy(gameObject);
    }
    private void LogDebugInfo(string message)
    {
        if (showDebugInfo)
        {
            Debug.Log(message);
        }
    }
    private void LogInitializationInfo()
    {
    }
    private void LogSpawnInfo()
    {
    }
}
public interface IDamageable
{
    void TakeDamage(int damage);
}