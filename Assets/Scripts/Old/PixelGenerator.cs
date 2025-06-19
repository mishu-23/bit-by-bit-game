using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public enum PixelType {  Armor, Critical, Damage, Health, Lifesteal, Luck }
public class PixelGenerator : MonoBehaviour
{
    [System.Serializable]
    public class PixelTypeData
    {
        public PixelType type;
        public Sprite icon;
        public Color baseColor;
        [UnityEngine.Range(0, 1)] public float spawnWeight = 1f;
    }

    [SerializeField] private GameObject pixelPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private int initialPixelCount = 20;
    [SerializeField] private List<PixelTypeData> pixelTypes = new List<PixelTypeData>();

    private void Start()
    {
        LoadDefaultPixelTypesIfEmpty();
        GenerateInitialPixels();
    }

    private void LoadDefaultPixelTypesIfEmpty()
    {
        if (pixelTypes.Count == 0)
        {
            foreach (PixelType type in System.Enum.GetValues(typeof(PixelType)))
            {
                Sprite loadedIcon = Resources.Load<Sprite>($"PixelTypes/{type}/{type.ToString().ToLower()}");
                pixelTypes.Add(new PixelTypeData
                {
                    type = type,
                    icon = loadedIcon,
                    baseColor = GetDefaultColorForType(type),
                    spawnWeight = 1f
                });
            }
        }
    }

    private Color GetDefaultColorForType(PixelType type)
    {
        return type switch { 
            PixelType.Armor => Color.blue,
            PixelType.Critical => Color.yellow,
            PixelType.Damage => Color.red,
            PixelType.Health => Color.green,
            PixelType.Lifesteal => Color.magenta,
            PixelType.Luck => Color.cyan,
            _ => Color.white
        };
    }

    public void GenerateInitialPixels()
    {
        //Debug.Log($"[PixelGenerator] Generating {initialPixelCount} random pixels");

        for (int i = 0; i < initialPixelCount; i++)
        {
            CreateRandomTypedPixel();
        }
    }

    private PixelType GetRandomWeightedType()
    {
        float totalWeight = pixelTypes.Sum(t => t.spawnWeight);
        float randomPoint = Random.Range(0, totalWeight);

        foreach (var typeData in pixelTypes)
        {
            if (randomPoint < typeData.spawnWeight)
                return typeData.type;
            randomPoint -= typeData.spawnWeight;
        }

        return PixelType.Damage; // Fallback
    }

    public void CreateRandomTypedPixel()
    {
        if (pixelPrefab == null || contentParent == null) return;

        PixelType randomType = GetRandomWeightedType();
        PixelTypeData typeData = pixelTypes.Find(t => t.type == randomType);

        GameObject newPixel = Instantiate(pixelPrefab, contentParent);
        SetupPixel(newPixel, typeData);
    }

    private void SetupPixel(GameObject pixelObj, PixelTypeData typeData)
    {
        // Set visual appearance
        Image pixelImage = pixelObj.GetComponent<Image>();
        if (pixelImage != null)
        {
            if (typeData.icon != null)
            {
                pixelImage.sprite = typeData.icon;
                pixelImage.color = Color.white; // nu colorează sprite-ul
            }
            else
            {
                pixelImage.sprite = null;
                pixelImage.color = typeData.baseColor; // fallback doar culoare
            }
        }

        // Initialize PixelCell component
        PixelCell pixelCell = pixelObj.GetComponent<PixelCell>();
        if (pixelCell == null) pixelCell = pixelObj.AddComponent<PixelCell>();
        pixelCell.Initialize(typeData.type);
    }
}