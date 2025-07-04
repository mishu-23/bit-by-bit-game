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
    [SerializeField] private float copySpacing = 0f;
    [SerializeField] private float spacingPadding = 0.1f;
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
        if (lockYPosition)
        {
            Vector3 pos = transform.position;
            pos.y = referenceYPosition;
            transform.position = pos;
        }
        if (infiniteScrolling && originalSpriteRenderer != null && originalSpriteRenderer.sprite != null)
        {
            SetupInfiniteScrolling();
        }
    }
    private void SetupInfiniteScrolling()
    {
        Sprite sprite = originalSpriteRenderer.sprite;
        textureUnitSizeX = sprite.texture.width / sprite.pixelsPerUnit;
        if (copySpacing <= 0)
        {
            float actualSpriteWidth = textureUnitSizeX * transform.localScale.x;
            copySpacing = actualSpriteWidth + spacingPadding;
        }
        for (int i = 1; i < numberOfCopies; i++)
        {
            GameObject copy = new GameObject(gameObject.name + "_Copy" + i);
            copy.transform.parent = transform.parent;
            SpriteRenderer copySpriteRenderer = copy.AddComponent<SpriteRenderer>();
            copySpriteRenderer.sprite = originalSpriteRenderer.sprite;
            copySpriteRenderer.sortingOrder = originalSpriteRenderer.sortingOrder;
            copySpriteRenderer.sortingLayerName = originalSpriteRenderer.sortingLayerName;
            copySpriteRenderer.color = originalSpriteRenderer.color;
            copySpriteRenderer.material = originalSpriteRenderer.material;
            float offsetX = (i - (numberOfCopies / 2f)) * copySpacing;
            copy.transform.position = transform.position + new Vector3(offsetX, 0, 0);
            copy.transform.localScale = transform.localScale;
            layerCopies.Add(copy);
        }
        layerCopies.Add(gameObject);
        Debug.Log($"Created {numberOfCopies} parallax copies for {gameObject.name}");
    }
    private void LateUpdate()
    {
        if (cameraTransform == null) return;
        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;
        float yMovement = lockYPosition ? 0f : deltaMovement.y * parallaxSpeed;
        Vector3 movement = new Vector3(deltaMovement.x * parallaxSpeed, yMovement, 0);
        foreach (GameObject layerCopy in layerCopies)
        {
            if (layerCopy != null)
            {
                layerCopy.transform.position += movement;
                if (lockYPosition)
                {
                    Vector3 pos = layerCopy.transform.position;
                    pos.y = referenceYPosition;
                    layerCopy.transform.position = pos;
                }
            }
        }
        if (infiniteScrolling && textureUnitSizeX > 0)
        {
            foreach (GameObject layerCopy in layerCopies)
            {
                if (layerCopy != null)
                {
                    float distanceFromCamera = layerCopy.transform.position.x - cameraTransform.position.x;
                    if (distanceFromCamera > copySpacing * numberOfCopies / 2f)
                    {
                        Vector3 pos = layerCopy.transform.position;
                        pos.x -= copySpacing * numberOfCopies;
                        layerCopy.transform.position = pos;
                    }
                    else if (distanceFromCamera < -copySpacing * numberOfCopies / 2f)
                    {
                        Vector3 pos = layerCopy.transform.position;
                        pos.x += copySpacing * numberOfCopies;
                        layerCopy.transform.position = pos;
                    }
                }
            }
        }
        lastCameraPosition = cameraTransform.position;
    }
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
    public void SetInfiniteScrolling(bool enable) => infiniteScrolling = enable;
    public void SetNumberOfCopies(int copies) => numberOfCopies = copies;
    public void SetCopySpacing(float spacing) => copySpacing = spacing;
    public void SetSpacingPadding(float padding) => spacingPadding = padding;
    public void InitializeInfiniteScrolling()
    {
        ClearLayerCopies();
        if (infiniteScrolling && originalSpriteRenderer != null && originalSpriteRenderer.sprite != null)
        {
            SetupInfiniteScrolling();
        }
    }
    private void ClearLayerCopies()
    {
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