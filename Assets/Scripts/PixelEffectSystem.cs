using UnityEngine;

public static class PixelEffectSystem
{
    public static void ApplyEffect(PixelType type, PlayerStats stats)
    {
        if (stats == null)
        {
            Debug.LogError($"[{nameof(PixelEffectSystem)}] PlayerStats reference is null!");
            return;
        }

        // Get current values
        int currentArmor = stats.GetArmor();
        float currentCritical = stats.GetCriticalChance();
        int currentDamage = stats.GetDamage();
        int currentHealth = stats.GetMaxHealth();
        float currentLifesteal = stats.GetLifesteal();
        int currentLuck = stats.GetLuck();

        // Apply effects
        switch (type)
        {
            case PixelType.Armor:
                stats.UpdateStatsFromPixels(type, currentArmor + 5);
                break;
            case PixelType.Critical:
                stats.UpdateStatsFromPixels(type, Mathf.RoundToInt((currentCritical + 0.1f) * 100));
                break;
            case PixelType.Damage:
                stats.UpdateStatsFromPixels(type, currentDamage + 3);
                break;
            case PixelType.Health:
                stats.UpdateStatsFromPixels(type, currentHealth + 15);
                break;
            case PixelType.Lifesteal:
                stats.UpdateStatsFromPixels(type, Mathf.RoundToInt((currentLifesteal + 0.05f) * 100));
                break;
            case PixelType.Luck:
                stats.UpdateStatsFromPixels(type, currentLuck + 1);
                break;
        }
        Debug.Log($"[{nameof(PixelEffectSystem)}] Applied {type} effect");
    }
}