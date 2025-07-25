using UnityEngine;
using BitByBit.Core;
namespace BitByBit.Player
{
    [System.Serializable]
    public class CameraSettings
    {
        [Header("Movement")]
        public float fixedYPosition = 3f;
        public float followSpeed = 5f;
        public Vector3 offset = new Vector3(0f, 0f, -10f);
        [Header("Boundaries")]
        public bool enableXBoundaries = true;
        public float minX = -40f;
        public float maxX = 40f;
        [Header("Performance")]
        public bool enableRateLimiting = false;
        [Range(0.001f, 0.05f)]
        public float updateInterval = 0.016f;
    }
    public class CameraController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [Header("Settings")]
        [SerializeField] private CameraSettings settings = new CameraSettings();
        private Camera cameraComponent;
        private BoxCollider2D targetCollider;
        private Vector3 cachedPlayerCenter;
        private float lastUpdateTime;
        private bool isInitialized;
        public Transform Target => target;
        public CameraSettings Settings => settings;
        public Camera CameraComponent => cameraComponent;
        public bool IsFollowingTarget => target != null && isInitialized;
        private void Awake()
        {
            InitializeComponents();
            InitializeTarget();
        }
        private void Start()
        {
            ValidateConfiguration();
            SetInitialPosition();
            isInitialized = true;
        }
        private void LateUpdate()
        {
            if (!ShouldUpdateCamera()) return;
            UpdateCameraPosition();
            lastUpdateTime = Time.time;
        }
        private void InitializeComponents()
        {
            cameraComponent = GetComponent<Camera>();
            if (cameraComponent == null)
            {
                Debug.LogError("CameraController: Camera component not found!", this);
            }
        }
        private void InitializeTarget()
        {
            if (target == null)
            {
                target = FindPlayerTarget();
            }
            if (target != null)
            {
                CacheTargetComponents();
            }
            else
            {
                Debug.LogWarning("CameraController: No target assigned and no Player found!");
            }
        }
        private Transform FindPlayerTarget()
        {
            if (GameReferences.Instance != null && GameReferences.Instance.Player != null)
            {
                return GameReferences.Instance.Player;
            }
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Debug.LogWarning("CameraController: Found player via fallback method. Please ensure GameReferences is properly configured.");
                return player.transform;
            }
            return null;
        }
        private void CacheTargetComponents()
        {
            if (target != null)
            {
                targetCollider = target.GetComponent<BoxCollider2D>();
            }
        }
        private void ValidateConfiguration()
        {
            if (settings.enableXBoundaries && settings.minX > settings.maxX)
            {
                Debug.LogWarning($"CameraController: minX ({settings.minX}) is greater than maxX ({settings.maxX}). Swapping values.");
                (settings.minX, settings.maxX) = (settings.maxX, settings.minX);
            }
            if (settings.followSpeed <= 0f)
            {
                Debug.LogWarning("CameraController: Follow speed must be positive. Setting to default value.");
                settings.followSpeed = 5f;
            }
            settings.updateInterval = Mathf.Clamp(settings.updateInterval, 0.001f, 0.05f);
        }
        private void SetInitialPosition()
        {
            if (target == null) return;
            Vector3 initialPosition = CalculateDesiredPosition();
            transform.position = initialPosition;
        }
        private bool ShouldUpdateCamera()
        {
            if (target == null || !isInitialized) return false;
            if (!settings.enableRateLimiting) return true;
            return Time.time - lastUpdateTime >= settings.updateInterval;
        }
        private void UpdateCameraPosition()
        {
            Vector3 desiredPosition = CalculateDesiredPosition();
            Vector3 smoothedPosition = CalculateSmoothedPosition(desiredPosition);
            transform.position = smoothedPosition;
        }
        private Vector3 CalculateDesiredPosition()
        {
            Vector3 playerCenter = GetPlayerCenterPosition();
            Vector3 basePosition = new Vector3(playerCenter.x, settings.fixedYPosition, playerCenter.z) + settings.offset;
            return ApplyBoundaryConstraints(basePosition);
        }
        private Vector3 CalculateSmoothedPosition(Vector3 desiredPosition)
        {
            return Vector3.Lerp(
                transform.position,
                desiredPosition,
                settings.followSpeed * Time.deltaTime
            );
        }
        private Vector3 GetPlayerCenterPosition()
        {
            if (targetCollider != null)
            {
                Vector3 newCenter = target.TransformPoint(targetCollider.offset);
                if (Vector3.Distance(newCenter, cachedPlayerCenter) > 0.01f)
                {
                    cachedPlayerCenter = newCenter;
                }
                return cachedPlayerCenter;
            }
            return target.position;
        }
        private Vector3 ApplyBoundaryConstraints(Vector3 position)
        {
            if (settings.enableXBoundaries)
            {
                position.x = Mathf.Clamp(position.x, settings.minX, settings.maxX);
            }
            return position;
        }
        private bool IsWithinBoundaries(Vector3 position)
        {
            if (!settings.enableXBoundaries) return true;
            return position.x >= settings.minX && position.x <= settings.maxX;
        }
        public void SetTarget(Transform newTarget)
        {
            if (newTarget == target) return;
            target = newTarget;
            CacheTargetComponents();
            if (target != null && isInitialized)
            {
                SetInitialPosition();
            }
        }
        public void UpdateSettings(CameraSettings newSettings)
        {
            if (newSettings == null)
            {
                Debug.LogWarning("CameraController: Cannot update with null settings!");
                return;
            }
            settings = newSettings;
            ValidateConfiguration();
        }
        public void SetFixedYPosition(float newY)
        {
            settings.fixedYPosition = newY;
        }
        public void SetFollowSpeed(float newSpeed)
        {
            if (newSpeed > 0f)
            {
                settings.followSpeed = newSpeed;
            }
            else
            {
                Debug.LogWarning("CameraController: Follow speed must be positive!");
            }
        }
        public void SetXBoundaries(float min, float max)
        {
            settings.minX = Mathf.Min(min, max);
            settings.maxX = Mathf.Max(min, max);
        }
        public void EnableXBoundaries(bool enable)
        {
            settings.enableXBoundaries = enable;
        }
        public void ForceUpdatePosition()
        {
            if (target != null)
            {
                lastUpdateTime = 0f;
            }
        }
        public float GetDistanceToTarget()
        {
            if (target == null) return float.MaxValue;
            Vector3 cameraPos = transform.position;
            Vector3 targetPos = target.position;
            cameraPos.z = targetPos.z = 0f;
            return Vector3.Distance(cameraPos, targetPos);
        }
        public bool IsTargetVisible()
        {
            if (target == null || cameraComponent == null) return false;
            Vector3 viewportPoint = cameraComponent.WorldToViewportPoint(target.position);
            return viewportPoint.x >= 0f && viewportPoint.x <= 1f &&
                   viewportPoint.y >= 0f && viewportPoint.y <= 1f;
        }
        public Vector3 GetPredictedPosition(float deltaTime)
        {
            if (target == null) return transform.position;
            Vector3 currentDesired = CalculateDesiredPosition();
            return Vector3.Lerp(transform.position, currentDesired, settings.followSpeed * deltaTime);
        }
        private void OnDrawGizmosSelected()
        {
            if (!settings.enableXBoundaries) return;
            Gizmos.color = Color.yellow;
            float gizmoHeight = 10f;
            Vector3 minPoint = new Vector3(settings.minX, settings.fixedYPosition - gizmoHeight/2, 0);
            Vector3 maxPoint = new Vector3(settings.maxX, settings.fixedYPosition - gizmoHeight/2, 0);
            Gizmos.DrawLine(minPoint, minPoint + Vector3.up * gizmoHeight);
            Gizmos.DrawLine(maxPoint, maxPoint + Vector3.up * gizmoHeight);
            Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
            Vector3 center = new Vector3((settings.minX + settings.maxX) / 2, settings.fixedYPosition, 0);
            Vector3 size = new Vector3(settings.maxX - settings.minX, gizmoHeight, 1f);
            Gizmos.DrawCube(center, size);
        }
    }
}