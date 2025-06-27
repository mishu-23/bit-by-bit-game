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
        public float copySpacing = 0f; // 0 = auto-calculate
        public float spacingPadding = 0.1f; // Extra padding to prevent overlaps
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
            
            // Create one instance - it will handle its own copies
            GameObject layerInstance = Instantiate(config.layerPrefab, transform);
            layerInstance.name = config.layerName;
            
            // Position the layer
            Vector3 position = new Vector3(0, config.referenceYPosition, config.zDepth);
            layerInstance.transform.position = position;
            
            // Add and configure ParallaxLayer component
            ParallaxLayer parallaxComponent = layerInstance.GetComponent<ParallaxLayer>();
            if (parallaxComponent == null)
            {
                parallaxComponent = layerInstance.AddComponent<ParallaxLayer>();
            }
            
            // Configure all ParallaxLayer settings from the config
            ConfigureParallaxLayer(parallaxComponent, config);
            
            // Set sorting order on the sprite renderer
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
        // Configure all properties using public methods
        parallaxComponent.SetParallaxSpeed(config.parallaxSpeed);
        parallaxComponent.SetReferenceYPosition(config.referenceYPosition);
        parallaxComponent.SetYPositionLock(config.lockYPosition);
        
        // Configure infinite scrolling settings
        parallaxComponent.SetInfiniteScrolling(config.enableInfiniteScrolling);
        parallaxComponent.SetNumberOfCopies(config.numberOfCopies);
        parallaxComponent.SetCopySpacing(config.copySpacing);
        parallaxComponent.SetSpacingPadding(config.spacingPadding);
        
        // Initialize the infinite scrolling system after all settings are configured
        parallaxComponent.InitializeInfiniteScrolling();
    }
    
    // Method to add a new parallax layer at runtime
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
        
        // Create the layer instance
        GameObject layerInstance = Instantiate(layerPrefab, transform);
        layerInstance.name = newConfig.layerName;
        layerInstance.transform.position = new Vector3(0, yPos, zDepth);
        
        ParallaxLayer parallaxComponent = layerInstance.GetComponent<ParallaxLayer>();
        if (parallaxComponent == null)
        {
            parallaxComponent = layerInstance.AddComponent<ParallaxLayer>();
        }
        
        ConfigureParallaxLayer(parallaxComponent, newConfig);
        
        // Set sorting order
        SpriteRenderer spriteRenderer = layerInstance.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = newConfig.sortingOrder;
        }
        
        layerInstances.Add(layerInstance);
        
        Debug.Log($"Added runtime parallax layer: {layerName} with speed {parallaxSpeed}");
    }
    
    // Method to adjust parallax speed of all layers
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
    
    // Public method to reconfigure a layer
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