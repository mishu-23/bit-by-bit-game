using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    
    [Header("Camera Settings")]
    [SerializeField] private float fixedYPosition = 3f;
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
    
    [Header("Camera Boundaries")]
    [SerializeField] private bool enableXBoundaries = true;
    [SerializeField] private float minX = -40f;
    [SerializeField] private float maxX = 40f;
    
    private Camera cam;
    
    private void Awake()
    {
        cam = GetComponent<Camera>();
        
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }
    
    private void Start()
    {
        if (target != null)
        {
            Vector3 targetPosition = new Vector3(target.position.x, fixedYPosition, target.position.z) + offset;
            targetPosition = ApplyBoundaries(targetPosition);
            transform.position = targetPosition;
        }
        

    }
    
    private void LateUpdate()
    {
        if (target == null) return;
        
        Vector3 playerCenter = GetPlayerCenterPosition();
        Vector3 desiredPosition = new Vector3(playerCenter.x, fixedYPosition, playerCenter.z) + offset;
        
        // Apply boundaries before lerping
        Vector3 clampedPosition = ApplyBoundaries(desiredPosition);
        
        // Lerp to the clamped position
        transform.position = Vector3.Lerp(transform.position, clampedPosition, followSpeed * Time.deltaTime);
    }
    
    private Vector3 GetPlayerCenterPosition()
    {
        BoxCollider2D playerCollider = target.GetComponent<BoxCollider2D>();
        if (playerCollider != null)
        {
            return target.TransformPoint(playerCollider.offset);
        }
        return target.position;
    }
    
    private Vector3 ApplyBoundaries(Vector3 targetPosition)
    {
        if (enableXBoundaries)
        {
            // Make sure min and max are in correct order
            float actualMinX = Mathf.Min(minX, maxX);
            float actualMaxX = Mathf.Max(minX, maxX);
            
            targetPosition.x = Mathf.Clamp(targetPosition.x, actualMinX, actualMaxX);
        }
        return targetPosition;
    }
    
    // Public methods for runtime control
    public void SetTarget(Transform newTarget) => target = newTarget;
    public void SetFixedYPosition(float newY) => fixedYPosition = newY;
    public void SetFollowSpeed(float newSpeed) => followSpeed = newSpeed;
    
    // Boundary control methods
    public void SetXBoundaries(float min, float max)
    {
        minX = min;
        maxX = max;
    }
    
    public void EnableXBoundaries(bool enable) => enableXBoundaries = enable;
    
    // Getters
    public Transform GetTarget() => target;
    public float GetFixedYPosition() => fixedYPosition;
    public float GetFollowSpeed() => followSpeed;
    public float GetMinX() => minX;
    public float GetMaxX() => maxX;
    public bool AreXBoundariesEnabled() => enableXBoundaries;
} 