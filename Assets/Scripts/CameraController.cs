using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    
    [Header("Camera Settings")]
    [SerializeField] private float fixedYPosition = 3f;
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    private Camera cam;
    
    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("CameraController requires a Camera component!");
        }
        
        // Auto-find player if not assigned
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                if (showDebugInfo)
                {
                    Debug.Log($"CameraController found player: {player.name}");
                }
            }
            else
            {
                Debug.LogError("No player found with tag 'Player'! Please assign target manually.");
            }
        }
    }
    
    private void Start()
    {
        // Set initial position
        if (target != null)
        {
            Vector3 targetPosition = new Vector3(target.position.x, fixedYPosition, target.position.z) + offset;
            transform.position = targetPosition;
            
            if (showDebugInfo)
            {
                Debug.Log($"Camera initialized at Y = {fixedYPosition}");
            }
        }
    }
    
    private void LateUpdate()
    {
        if (target == null) return;
        
        // Get the player's center position using the collider bounds
        Vector3 playerCenter = GetPlayerCenterPosition();
        
        // Calculate target position (only follow X, keep Y fixed)
        Vector3 targetPosition = new Vector3(playerCenter.x, fixedYPosition, playerCenter.z) + offset;
        
        // Smoothly move camera to target position
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        
        if (showDebugInfo && Time.frameCount % 60 == 0) // Log every 60 frames
        {
            Debug.Log($"Camera following player center - Target Y: {fixedYPosition}, Current Y: {transform.position.y:F2}");
        }
    }
    
    private Vector3 GetPlayerCenterPosition()
    {
        // Try to get the center from the player's collider
        BoxCollider2D playerCollider = target.GetComponent<BoxCollider2D>();
        if (playerCollider != null)
        {
            // Calculate the world center of the collider
            Vector3 colliderCenter = target.TransformPoint(playerCollider.offset);
            return colliderCenter;
        }
        
        // Fallback to target position if no collider found
        return target.position;
    }
    
    // Public methods for runtime control
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (showDebugInfo)
        {
            Debug.Log($"Camera target changed to: {newTarget?.name ?? "null"}");
        }
    }
    
    public void SetFixedYPosition(float newY)
    {
        fixedYPosition = newY;
        if (showDebugInfo)
        {
            Debug.Log($"Camera fixed Y position changed to: {newY}");
        }
    }
    
    public void SetFollowSpeed(float newSpeed)
    {
        followSpeed = newSpeed;
        if (showDebugInfo)
        {
            Debug.Log($"Camera follow speed changed to: {newSpeed}");
        }
    }
    
    // Getters for other systems
    public Transform GetTarget() => target;
    public float GetFixedYPosition() => fixedYPosition;
    public float GetFollowSpeed() => followSpeed;
} 