using UnityEngine;
using System.Collections.Generic;
public class ParallaxManager : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayerConfig
    {
        [Header("Basic Settings")]
        public GameObject layerPrefab;
        public string layerName = "ParallaxLayer";
        public float parallaxSpeed = 0.5f;
        public float zDepth = -1f;
        public int sortingOrder = -1;
        [Header("Y Position Control")]
        public bool lockYPosition = true;
        public float referenceYPosition = 0f;
        [Header("Infinite Scrolling")]
        public bool enableInfiniteScrolling = true;
        public int numberOfCopies = 5;
        public float copySpacing = 0f;
        public float spacingPadding = 0.1f;
    }
    [Header("Parallax Configuration")]
    [SerializeField] private List<ParallaxLayerConfig> parallaxLayers = new List<ParallaxLayerConfig>();
    [Header("Auto Setup")]
    [SerializeField] private bool autoSetupLayers = true;
    private Transform cameraTransform;
    private List<GameObject> layerInstances = new List<GameObject>();
    private void Start()
    {
        cameraTransform = Camera.main.transform;
        if (autoSetupLayers)
        {
            SetupParallaxLayers();
        }
    }
    private void SetupParallaxLayers()
    {
        for (int i = 0; i < parallaxLayers.Count; i++)
        {
            ParallaxLayerConfig config = parallaxLayers[i];
            GameObject layerInstance = Instantiate(config.layerPrefab, transform);
            layerInstance.name = config.layerName;
            Vector3 position = new Vector3(0, config.referenceYPosition, config.zDepth);
            layerInstance.transform.position = position;
            ParallaxLayer parallaxComponent = layerInstance.GetComponent<ParallaxLayer>();
            if (parallaxComponent == null)
            {
                parallaxComponent = layerInstance.AddComponent<ParallaxLayer>();
            }
            ConfigureParallaxLayer(parallaxComponent, config);
            SpriteRenderer spriteRenderer = layerInstance.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = config.sortingOrder;
            }
            layerInstances.Add(layerInstance);
            Debug.Log($"Created parallax layer: {config.layerName} with speed {config.parallaxSpeed}");
        }
    }
    private void ConfigureParallaxLayer(ParallaxLayer parallaxComponent, ParallaxLayerConfig config)
    {
        parallaxComponent.SetParallaxSpeed(config.parallaxSpeed);
        parallaxComponent.SetReferenceYPosition(config.referenceYPosition);
        parallaxComponent.SetYPositionLock(config.lockYPosition);
        parallaxComponent.SetInfiniteScrolling(config.enableInfiniteScrolling);
        parallaxComponent.SetNumberOfCopies(config.numberOfCopies);
        parallaxComponent.SetCopySpacing(config.copySpacing);
        parallaxComponent.SetSpacingPadding(config.spacingPadding);
        parallaxComponent.InitializeInfiniteScrolling();
    }
    public void AddParallaxLayer(GameObject layerPrefab, float parallaxSpeed, float zDepth, string layerName = "RuntimeLayer", float yPos = 0f)
    {
        ParallaxLayerConfig newConfig = new ParallaxLayerConfig
        {
            layerPrefab = layerPrefab,
            layerName = layerName,
            parallaxSpeed = parallaxSpeed,
            zDepth = zDepth,
            referenceYPosition = yPos,
            lockYPosition = true,
            enableInfiniteScrolling = true,
            numberOfCopies = 5,
            sortingOrder = layerInstances.Count * -1 - 1
        };
        parallaxLayers.Add(newConfig);
        GameObject layerInstance = Instantiate(layerPrefab, transform);
        layerInstance.name = newConfig.layerName;
        layerInstance.transform.position = new Vector3(0, yPos, zDepth);
        ParallaxLayer parallaxComponent = layerInstance.GetComponent<ParallaxLayer>();
        if (parallaxComponent == null)
        {
            parallaxComponent = layerInstance.AddComponent<ParallaxLayer>();
        }
        ConfigureParallaxLayer(parallaxComponent, newConfig);
        SpriteRenderer spriteRenderer = layerInstance.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = newConfig.sortingOrder;
        }
        layerInstances.Add(layerInstance);
        Debug.Log($"Added runtime parallax layer: {layerName} with speed {parallaxSpeed}");
    }
    public void SetGlobalParallaxMultiplier(float multiplier)
    {
        for (int i = 0; i < layerInstances.Count; i++)
        {
            GameObject instance = layerInstances[i];
            if (instance != null)
            {
                ParallaxLayer parallaxComponent = instance.GetComponent<ParallaxLayer>();
                if (parallaxComponent != null)
                {
                    float originalSpeed = parallaxLayers[i].parallaxSpeed;
                    parallaxComponent.SetParallaxSpeed(originalSpeed * multiplier);
                }
            }
        }
    }
    public void ReconfigureLayer(int layerIndex, ParallaxLayerConfig newConfig)
    {
        if (layerIndex >= 0 && layerIndex < layerInstances.Count && layerIndex < parallaxLayers.Count)
        {
            parallaxLayers[layerIndex] = newConfig;
            GameObject instance = layerInstances[layerIndex];
            if (instance != null)
            {
                ParallaxLayer parallaxComponent = instance.GetComponent<ParallaxLayer>();
                if (parallaxComponent != null)
                {
                    ConfigureParallaxLayer(parallaxComponent, newConfig);
                }
            }
        }
    }
}