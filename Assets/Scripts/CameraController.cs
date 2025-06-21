using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    
    [Header("Camera Settings")]
    [SerializeField] private float fixedYPosition = 3f;
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
    
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
            transform.position = targetPosition;
        }
    }
    
    private void LateUpdate()
    {
        if (target == null) return;
        
        Vector3 playerCenter = GetPlayerCenterPosition();
        Vector3 targetPosition = new Vector3(playerCenter.x, fixedYPosition, playerCenter.z) + offset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
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
    
    // Public methods for runtime control
    public void SetTarget(Transform newTarget) => target = newTarget;
    public void SetFixedYPosition(float newY) => fixedYPosition = newY;
    public void SetFollowSpeed(float newSpeed) => followSpeed = newSpeed;
    
    // Getters
    public Transform GetTarget() => target;
    public float GetFixedYPosition() => fixedYPosition;
    public float GetFollowSpeed() => followSpeed;
} 