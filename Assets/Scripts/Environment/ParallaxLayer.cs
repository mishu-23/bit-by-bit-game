using UnityEngine;
using System.Collections.Generic;

public class ParallaxLayer : MonoBehaviour
{
    [Header("Parallax Settings")]
    [SerializeField] private float parallaxSpeed = 0.5f;
    [SerializeField] private bool infiniteScrolling = true;
    
    [Header("Y Position Control")]
    [SerializeField] private bool lockYPosition = true;
    [SerializeField] private float referenceYPosition = 0f;
    
    [Header("Infinite Scrolling Setup")]
    [SerializeField] private int numberOfCopies = 5;
    [SerializeField] private float copySpacing = 0f; // Auto-calculated if 0
    [SerializeField] private float spacingPadding = 0.1f; // Extra padding to prevent overlaps
    
    private Transform cameraTransform;
    private Vector3 lastCameraPosition;
    private float textureUnitSizeX;
    private List<GameObject> layerCopies = new List<GameObject>();
    private SpriteRenderer originalSpriteRenderer;
    
    private void Start()
    {
        cameraTransform = Camera.main.transform;
        lastCameraPosition = cameraTransform.position;
        originalSpriteRenderer = GetComponent<SpriteRenderer>();
        
        // Set initial Y position if lock is enabled
        if (lockYPosition)
        {
            Vector3 pos = transform.position;
            pos.y = referenceYPosition;
            transform.position = pos;
        }
        
        // Calculate the texture unit size and setup infinite scrolling
        if (infiniteScrolling && originalSpriteRenderer != null && originalSpriteRenderer.sprite != null)
        {
            SetupInfiniteScrolling();
        }
    }
    
    private void SetupInfiniteScrolling()
    {
        Sprite sprite = originalSpriteRenderer.sprite;
        textureUnitSizeX = sprite.texture.width / sprite.pixelsPerUnit;
        
        // Auto-calculate spacing if not set, accounting for transform scale
        if (copySpacing <= 0)
        {
            // Calculate the actual visual width considering the transform scale
            float actualSpriteWidth = textureUnitSizeX * transform.localScale.x;
            copySpacing = actualSpriteWidth + spacingPadding;
        }
        
        // Create copies of this layer for seamless scrolling
        for (int i = 1; i < numberOfCopies; i++)
        {
            GameObject copy = new GameObject(gameObject.name + "_Copy" + i);
            copy.transform.parent = transform.parent;
            
            // Copy the sprite renderer
            SpriteRenderer copySpriteRenderer = copy.AddComponent<SpriteRenderer>();
            copySpriteRenderer.sprite = originalSpriteRenderer.sprite;
            copySpriteRenderer.sortingOrder = originalSpriteRenderer.sortingOrder;
            copySpriteRenderer.sortingLayerName = originalSpriteRenderer.sortingLayerName;
            copySpriteRenderer.color = originalSpriteRenderer.color;
            copySpriteRenderer.material = originalSpriteRenderer.material;
            
            // Position the copy
            float offsetX = (i - (numberOfCopies / 2f)) * copySpacing;
            copy.transform.position = transform.position + new Vector3(offsetX, 0, 0);
            copy.transform.localScale = transform.localScale;
            
            layerCopies.Add(copy);
        }
        
        // Add the original to the list for easier management
        layerCopies.Add(gameObject);
        
        Debug.Log($"Created {numberOfCopies} parallax copies for {gameObject.name}");
    }
    
    private void LateUpdate()
    {
        if (cameraTransform == null) return;
        
        // Calculate camera movement delta
        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;
        
        // Move all layer copies based on parallax speed
        float yMovement = lockYPosition ? 0f : deltaMovement.y * parallaxSpeed;
        Vector3 movement = new Vector3(deltaMovement.x * parallaxSpeed, yMovement, 0);
        
        foreach (GameObject layerCopy in layerCopies)
        {
            if (layerCopy != null)
            {
                layerCopy.transform.position += movement;
                
                // Ensure Y position stays locked if enabled
                if (lockYPosition)
                {
                    Vector3 pos = layerCopy.transform.position;
                    pos.y = referenceYPosition;
                    layerCopy.transform.position = pos;
                }
            }
        }
        
        // Handle infinite scrolling repositioning for all copies
        if (infiniteScrolling && textureUnitSizeX > 0)
        {
            foreach (GameObject layerCopy in layerCopies)
            {
                if (layerCopy != null)
                {
                    // If a copy is too far from camera, move it to the other side
                    float distanceFromCamera = layerCopy.transform.position.x - cameraTransform.position.x;
                    
                    if (distanceFromCamera > copySpacing * numberOfCopies / 2f)
                    {
                        // Move to the left side
                        Vector3 pos = layerCopy.transform.position;
                        pos.x -= copySpacing * numberOfCopies;
                        layerCopy.transform.position = pos;
                    }
                    else if (distanceFromCamera < -copySpacing * numberOfCopies / 2f)
                    {
                        // Move to the right side
                        Vector3 pos = layerCopy.transform.position;
                        pos.x += copySpacing * numberOfCopies;
                        layerCopy.transform.position = pos;
                    }
                }
            }
        }
        
        lastCameraPosition = cameraTransform.position;
    }
    
    // Public methods for runtime adjustment
    public void SetParallaxSpeed(float speed) => parallaxSpeed = speed;
    public float GetParallaxSpeed() => parallaxSpeed;
    
    public void SetReferenceYPosition(float yPos) 
    { 
        referenceYPosition = yPos;
        if (lockYPosition)
        {
            Vector3 pos = transform.position;
            pos.y = referenceYPosition;
            transform.position = pos;
        }
    }
    
    public void SetYPositionLock(bool lockY) => lockYPosition = lockY;
    public float GetReferenceYPosition() => referenceYPosition;
    public bool IsYPositionLocked() => lockYPosition;
    
    // Public methods for infinite scrolling configuration
    public void SetInfiniteScrolling(bool enable) => infiniteScrolling = enable;
    public void SetNumberOfCopies(int copies) => numberOfCopies = copies;
    public void SetCopySpacing(float spacing) => copySpacing = spacing;
    public void SetSpacingPadding(float padding) => spacingPadding = padding;
    
    // Method to initialize/reinitialize the infinite scrolling system
    public void InitializeInfiniteScrolling()
    {
        // Clear existing copies first
        ClearLayerCopies();
        
        if (infiniteScrolling && originalSpriteRenderer != null && originalSpriteRenderer.sprite != null)
        {
            SetupInfiniteScrolling();
        }
    }
    
    private void ClearLayerCopies()
    {
        // Remove existing copies (except the original)
        for (int i = layerCopies.Count - 1; i >= 0; i--)
        {
            if (layerCopies[i] != null && layerCopies[i] != gameObject)
            {
                if (Application.isPlaying)
                    Destroy(layerCopies[i]);
                else
                    DestroyImmediate(layerCopies[i]);
            }
        }
        layerCopies.Clear();
    }
} 