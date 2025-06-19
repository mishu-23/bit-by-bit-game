using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatsPanel : MonoBehaviour
{
    [System.Serializable]
    public class StatRow
    {
        public Image statIcon;
        public TextMeshProUGUI statValue;
        public PixelType statType;
    }

    [Header("Left Panel Stats")]
    public StatRow[] leftPanelStats;

    [Header("Right Panel Stats")]
    public StatRow[] rightPanelStats;

    private PlayerStats playerStats;

    private void Start()
    {
        playerStats = PlayerStats.Instance;
        if (playerStats == null)
        {
            Debug.LogError($"[{nameof(StatsPanel)}] PlayerStats not found in scene!");
            return;
        }

        UpdateAllStats();
    }

    public void UpdateAllStats()
    {
        if (playerStats == null) return;

        // Update left panel stats
        foreach (var stat in leftPanelStats)
        {
            UpdateStatDisplay(stat);
        }

        // Update right panel stats
        foreach (var stat in rightPanelStats)
        {
            UpdateStatDisplay(stat);
        }
    }

    private void UpdateStatDisplay(StatRow statRow)
    {
        if (statRow.statValue == null) return;

        float value = 0;
        switch (statRow.statType)
        {
            case PixelType.Armor:
                value = playerStats.GetArmor();
                statRow.statValue.text = $"{value}%";
                break;
            case PixelType.Critical:
                value = playerStats.GetCriticalChance();
                statRow.statValue.text = $"{value:F1}%";
                break;
            case PixelType.Damage:
                value = playerStats.GetDamage();
                statRow.statValue.text = $"{value}%";
                break;
            case PixelType.Health:
                value = playerStats.GetMaxHealth();
                statRow.statValue.text = $"{value}%";
                break;
            case PixelType.Lifesteal:
                value = playerStats.GetLifesteal();
                statRow.statValue.text = $"{value:F1}%";
                break;
            case PixelType.Luck:
                value = playerStats.GetLuck();
                statRow.statValue.text = $"{value}%";
                break;
        }
    }
} 