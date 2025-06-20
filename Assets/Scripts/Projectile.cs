using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask targetLayers = 1; // Default to everything
    [SerializeField] private bool destroyOnHit = true;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    private Vector2 direction;
    private float timer;
    private bool hasHit = false;
    
    // Properties for different projectile types
    public int Damage { get => damage; set => damage = value; }
    public Rarity ProjectileRarity { get; set; } = Rarity.Common;
    public string ProjectileType { get; set; } = "Default";
    
    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        timer = lifetime;
    }
    
    private void Start()
    {
        if (showDebugInfo)
        {
            Debug.Log($"Projectile spawned: {ProjectileType} (Damage: {damage}, Rarity: {ProjectileRarity})");
        }
    }
    
    private void Update()
    {
        if (hasHit) return;
        
        // Move projectile
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
        
        // Lifetime check
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            if (showDebugInfo)
            {
                Debug.Log("Projectile destroyed due to lifetime");
            }
            DestroyProjectile();
        }
    }
    
    public void Initialize(Vector2 direction, int damage = 1, Rarity rarity = Rarity.Common, string projectileType = "Default")
    {
        this.direction = direction.normalized;
        this.damage = damage;
        this.ProjectileRarity = rarity;
        this.ProjectileType = projectileType;
        
        // Rotate projectile to face direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        if (showDebugInfo)
        {
            Debug.Log($"Projectile initialized: Direction={direction}, Damage={damage}, Rarity={rarity}, Type={projectileType}");
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        
        // Check if we hit a valid target
        if (((1 << other.gameObject.layer) & targetLayers) != 0)
        {
            hasHit = true;
            
            // Try to damage the target
            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                if (showDebugInfo)
                {
                    Debug.Log($"Projectile hit {other.name} for {damage} damage");
                }
            }
            else if (showDebugInfo)
            {
                Debug.Log($"Projectile hit {other.name} but it's not damageable");
            }
            
            // Spawn hit effect
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, transform.rotation);
            }
            
            // Destroy projectile
            if (destroyOnHit)
            {
                DestroyProjectile();
            }
        }
    }
    
    private void DestroyProjectile()
    {
        if (showDebugInfo)
        {
            Debug.Log($"Destroying projectile: {ProjectileType}");
        }
        Destroy(gameObject);
    }
    
    // Public method to set projectile appearance based on rarity
    public void SetAppearance(Rarity rarity)
    {
        if (spriteRenderer == null) return;
        
        // You can set different colors or sprites based on rarity
        switch (rarity)
        {
            case Rarity.Common:
                spriteRenderer.color = Color.white;
                break;
            case Rarity.Rare:
                spriteRenderer.color = Color.blue;
                break;
            case Rarity.Epic:
                spriteRenderer.color = Color.magenta;
                break;
            case Rarity.Legendary:
                spriteRenderer.color = Color.yellow;
                break;
        }
    }
}

// Interface for objects that can take damage
public interface IDamageable
{
    void TakeDamage(int damage);
} 